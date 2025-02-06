using System.Text.Json;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features;
using Altinn.App.Core.Implementation;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Layout;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Api.Tests.Mocks;

/// <summary>
/// A hook for mutating the <see cref="AppResourcesMock"/> instance in tests.
/// Add this to the DI container for a given test to perform custom changes on the mock.
/// </summary>
public sealed record AppResourcesMutationHook(Action<AppResourcesMock> Action);

/// <summary>
/// Mock implementation of <see cref="IAppResources"/> for integration tests.
/// Mirrors <see cref="AppResourcesSI"/> but loads data from test files or
/// can be mutated via <see cref="AppResourcesMutationHook"/>.
/// </summary>
public class AppResourcesMock : IAppResources
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        PropertyNameCaseInsensitive = true,
    };

    private readonly AppSettings _settings;
    private readonly IAppMetadata _appMetadata;
    private readonly ILogger<AppResourcesSI> _logger;
    private readonly Telemetry? _telemetry;
    private readonly IEnumerable<AppResourcesMutationHook> _mutationHooks;

    public Dictionary<string, string> _prefillByDataType = new();

    public AppResourcesMock(
        IOptions<AppSettings> settings,
        IAppMetadata appMetadata,
        IWebHostEnvironment hostingEnvironment,
        ILogger<AppResourcesSI> logger,
        IEnumerable<AppResourcesMutationHook> mutationHooks,
        Telemetry? telemetry = null
    )
    {
        _settings = settings.Value;
        _appMetadata = appMetadata;
        _logger = logger;
        _mutationHooks = mutationHooks;
        _telemetry = telemetry;

        foreach (var hook in _mutationHooks)
        {
            hook.Action(this);
        }
    }

    public void AddOrUpdatePrefill(string dataType, string json)
    {
        _prefillByDataType[dataType] = json;
    }

    public byte[] GetText(string org, string app, string textResource)
    {
        return Array.Empty<byte>();
    }

    public Task<TextResource?> GetTexts(string org, string app, string language)
    {
        return Task.FromResult<TextResource?>(null);
    }

    public string GetModelJsonSchema(string modelId)
    {
        return string.Empty;
    }

    public Application GetApplication()
    {
        return new Application();
    }

    public string? GetApplicationXACMLPolicy()
    {
        return null;
    }

    public string? GetApplicationBPMNProcess()
    {
        return null;
    }

    public string? GetPrefillJson(string dataModelName = "ServiceModel")
    {
        if (_prefillByDataType.TryGetValue(dataModelName, out var fromTest))
        {
            return fromTest;
        }
        return null;
    }

    public string GetClassRefForLogicDataType(string dataType)
    {
        return string.Empty;
    }

    public string GetLayouts()
    {
        return string.Empty;
    }

    public string? GetLayoutSettingsString()
    {
        return null;
    }

    public LayoutSettings GetLayoutSettings()
    {
        return new LayoutSettings();
    }

    public string GetLayoutSets()
    {
        return string.Empty;
    }

    public Task<string?> GetFooter()
    {
        return Task.FromResult<string?>(null);
    }

    public LayoutSets? GetLayoutSet()
    {
        return null;
    }

    public LayoutSet? GetLayoutSetForTask(string taskId)
    {
        return null;
    }

    public string GetLayoutsForSet(string layoutSetId)
    {
        return string.Empty;
    }

    public LayoutModel? GetLayoutModelForTask(string taskId)
    {
        return null;
    }

    public LayoutModel GetLayoutModel(string? layoutSetId = null)
    {
        return new LayoutModel(new List<LayoutSetComponent>(), new LayoutSet { Id = "", DataType = "" });
    }

    public string? GetLayoutSettingsStringForSet(string layoutSetId)
    {
        return null;
    }

    public LayoutSettings? GetLayoutSettingsForSet(string? layoutSetId)
    {
        return null;
    }

    public byte[] GetRuleConfigurationForSet(string id)
    {
        return Array.Empty<byte>();
    }

    public byte[] GetRuleHandlerForSet(string id)
    {
        return [];
    }

    public string? GetValidationConfiguration(string dataTypeId)
    {
        return null;
    }
}
