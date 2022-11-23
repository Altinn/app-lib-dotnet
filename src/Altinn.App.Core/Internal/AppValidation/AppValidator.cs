using System.Text.Json;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Options;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Interface;
using Altinn.App.Core.Internal.AppFiles;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Layout;
using Altinn.App.Core.Models.Layout.Components;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Internal.AppValidation;

/// <summary>
/// App validator that can be run on app startup to verify that the json files are valid
/// </summary>
public class AppValidator
{
    private readonly FrontEndSettings _frontEndSettings;
    private readonly IOptions<AppSettings> _appSettings;
    private readonly IEnumerable<IAppOptionsProvider> _appOptionsProviders;
    private readonly IEnumerable<IInstanceAppOptionsProvider> _instanceAppOptionsProviders;
    private readonly IAppModel _appModel;
    private readonly ILogger<AppResourcesNew> _resourceLogger;
    private readonly ILogger<AppValidator> _logger;



    /// <summary>
    /// Initializes a new instance of the <see cref="AppValidator"/> class.
    /// </summary>
    public AppValidator(
        IOptions<FrontEndSettings> frontEndSettings,
        IOptions<AppSettings> settings,
        IEnumerable<IAppOptionsProvider> appOptionsProviders,
        IEnumerable<IInstanceAppOptionsProvider> instanceAppOptionsProviders,
        IAppModel appModel,
        ILogger<AppResourcesNew> resourceLogger,
        ILogger<AppValidator> logger)
    {
        _frontEndSettings = frontEndSettings.Value;
        _appSettings = settings;
        _appOptionsProviders = appOptionsProviders;
        _instanceAppOptionsProviders = instanceAppOptionsProviders;
        _appModel = appModel;
        _resourceLogger = resourceLogger;
        _logger = logger;
    }

    /// <summary>
    /// Run validations on the json files of the app
    /// </summary>
    public async Task<List<AppValidationError>> Validate(AppFilesBytes appFilesBytes)
    {
        var errors = new List<AppValidationError>();

        // Using AppResourcesNew directly, because it is registrerd as <see cref="IAppResources" /> and
        // other implementations won't work.
        var appResources = new AppResourcesNew(_appSettings, _resourceLogger, appFilesBytes);

        var application = appResources.GetApplication();
        ValidateApplication(errors, application);
        var sets = appResources.GetLayoutSet();

        if (sets is not null && sets.Sets?.Count > 0)
        {
            foreach (var set in sets.Sets)
            {
                var dataType = application.DataTypes.FirstOrDefault(d => d.Id == set.DataType);
                if (string.IsNullOrWhiteSpace(dataType?.AppLogic?.ClassRef))
                {
                    errors.Add(new()
                    {
                        Message = $"No datatype in applicationmetadata.json for layout-set {set.Id} with dataType {set.DataType}",
                    });
                }

                var settings = appResources.GetLayoutSettingsForSet(set.Id);
                if (settings is null)
                {
                    errors.Add(new()
                    {
                        Message = $"No Settings.json file found for layout-set {set.Id}",
                    });
                    return errors;
                }

                await ValidateLayoutSet(errors, set, settings, appFilesBytes, appResources);
            }
        }
        else
        {
            var settings = appResources.GetLayoutSettings();
            await ValidateLayoutSet(errors, null, settings, appFilesBytes, appResources);
        }
        return errors;
    }

    private void ValidateApplication(List<AppValidationError> errors, Application application)
    {
        var classRefs = application.DataTypes.Select((d, index) => (index, d?.AppLogic?.ClassRef)).Where(d => d.ClassRef is not null);
        foreach (var (index, classRef) in classRefs)
        {
            _appModel.GetModelType(classRef!);
        }
    }

    private async Task ValidateLayoutSet(List<AppValidationError> errors, LayoutSet? set, LayoutSettings settings, AppFilesBytes appFilesBytes, IAppResources appResources)
    {
        if (!appFilesBytes.LayoutSetFiles.TryGetValue(set?.Id ?? string.Empty, out var files))
        {
            errors.Add(new AppValidationError
            {
                Message = $"layout-sets.json specifies a layout set \"{set?.Id}\" that does not exist in \"App/ui/{set?.Id}\"",
            });
            return;
        }

        if (settings?.Pages?.Order?.Count > 0)
        {
            foreach (var pageOrderName in settings.Pages.Order)
            {
                if (!files.Pages.ContainsKey(pageOrderName))
                {
                    errors.Add(new AppValidationError
                    {
                        Message = $"Page \"{pageOrderName}\" was specified in \"{Path.Join("app", "ui", set?.Id, "settings.json")}\", but it does not exist in \"{Path.Join("app", "ui", set?.Id, "layouts", pageOrderName + ".json")}\"",
                    });
                }
            }
        }
        var model = new LayoutModel();
        foreach (var (pageName, pageBytes) in files.Pages)
        {
            try
            {
                var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                };
                PageComponentConverter.SetAsyncLocalPageName(pageName);
                var page = JsonSerializer.Deserialize<PageComponent>(pageBytes, options);
                if (page is null)
                {
                    errors.Add(new AppValidationError
                    {
                        Message = $"Page {set?.Id} {pageName}.json is literal `null`"
                    });
                }
                else
                {
                    model.Pages.Add(pageName, page);
                }
            }
            catch (JsonException e)
            {
                errors.Add(AppValidationError.FromJsonError(Path.Join("App", "ui", set?.Id, "layouts", pageName + ".json"), e));
                return; // Don't try to validate pages, when one fails to load
            }
        }

        //Do validations on single pages
        foreach (var page in model.Pages.Values.Where(p => p is not null))
        {
            await VerifyOptionIdInOptionComponets(errors, page, set, appFilesBytes);
            await VerifyDataModelBindings(errors, page, set, appFilesBytes);
        }

        // Do more validations if all pages loaded successfully
        if (model.Pages.Values.All(p => p is not null))
        {
            VerifySummaryComponentReferences(errors, model, set, appFilesBytes);
            VerifyUniqueComponentIdsInLayout(errors, model, set, appFilesBytes);

            //TODO: run more validations on page
        }
    }

    private void VerifyUniqueComponentIdsInLayout(List<AppValidationError> errors, LayoutModel model, LayoutSet? set, AppFilesBytes appFilesBytes)
    {
        var ids = new Dictionary<string, List<BaseComponent>>();

        foreach (var component in model.GetComponents())
        {
            var componentId = component.Id.ToLowerInvariant();
            if (!ids.ContainsKey(componentId))
            {
                ids[componentId] = new();
            }
            ids[componentId].Add(component);
        }

        foreach (var duplicate in ids.Values.Where(l => l.Count > 1))
        {
            foreach (var component in duplicate)
            {
                errors.Add(new AppValidationError
                {
                    Message = $"Duplicate ID \"{component.Id}\" for components on page \"{component.PageId}\"",
                    ErrorLocation = AppValidationError.FromJsonComponent(set, component, appFilesBytes)
                });
            }
        }
    }

    private async Task VerifyDataModelBindings(List<AppValidationError> errors, PageComponent page, LayoutSet? set, AppFilesBytes appFilesBytes)
    {
        foreach (var component in page.ComponentLookup.Values)
        {
            foreach (var binding in component.DataModelBindings.Values)
            {

            }
        }
        return;
    }

    private static void VerifySummaryComponentReferences(List<AppValidationError> errors, LayoutModel model, LayoutSet? set, AppFilesBytes appFilesBytes)
    {
        foreach (var summaryComponent in model.GetComponents().OfType<SummaryComponent>())
        {
            if (!model.Pages.TryGetValue(summaryComponent.PageRef, out var page))
            {
                errors.Add(new()
                {
                    ErrorLocation = AppValidationError.FromJsonComponent(set, summaryComponent, appFilesBytes),
                    Message = $"Summary with id {summaryComponent.Id} on page {summaryComponent.PageId} references a page {summaryComponent.PageRef} that does not exist",
                });
            }
            else if (!page.ComponentLookup.TryGetValue(summaryComponent.ComponentRef, out var component))
            {
                errors.Add(new()
                {
                    Message = $"Summary with id {summaryComponent.Id} on page {summaryComponent.PageId} references a component {summaryComponent.ComponentRef} that does not exist on page {summaryComponent.PageRef}",
                });
            }
        }
    }

    private async Task VerifyOptionIdInOptionComponets(List<AppValidationError> errors, PageComponent page, LayoutSet? layoutSet, AppFilesBytes appFilesBytes)
    {
        var options = page.ComponentLookup.Values.OfType<OptionsComponent>().Where(opt => opt.OptionId is not null);
        foreach (var option in options)
        {
            if (option.Secure)
            {
                if (!_instanceAppOptionsProviders.Any(iaop => iaop.Id.Equals(option.OptionId, StringComparison.InvariantCultureIgnoreCase)))
                {
                    errors.Add(new()
                    {
                        ErrorLocation = AppValidationError.FromJsonComponent(layoutSet, option, appFilesBytes),
                        Message = $"Could not find an implementation of {nameof(IInstanceAppOptionsProvider)} for secure option with optionId \"{option.OptionId}\" in component {option.PageId}.{option.Id}",
                    });
                }
            }
            else
            {
                if (!_appOptionsProviders.Any(aop => aop.Id.Equals(option.OptionId, StringComparison.InvariantCultureIgnoreCase)))
                {
                    var defaultAppOptionsProvider = _appOptionsProviders.OfType<DefaultAppOptionsProvider>().FirstOrDefault()?.CloneDefaultTo(option.OptionId!);
                    if (defaultAppOptionsProvider is null)
                    {
                        errors.Add(new()
                        {
                            Message = $"Could not find an implementation of {nameof(IAppOptionsProvider)} nor a {nameof(DefaultAppOptionsProvider)}",
                        });
                    }
                    else
                    {
                        var optionsList = await defaultAppOptionsProvider.GetAppOptionsAsync("nb", new Dictionary<string, string>());
                        if (optionsList?.Options?.Count == 0)
                        {
                            errors.Add(new()
                            {
                                ErrorLocation = AppValidationError.FromJsonComponent(layoutSet, option, appFilesBytes),
                                Message = $"Could not find an implementation of {nameof(IAppOptionsProvider)}, nor a json file for option with optionId \"{option.OptionId}\" in component {option.PageId}.{option.Id}",
                            });

                        }
                    }
                }
            }
        }
    }
}