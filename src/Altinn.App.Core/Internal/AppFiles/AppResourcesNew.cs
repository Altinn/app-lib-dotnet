using System.Text;
using System.Text.Json;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Helpers.Extensions;
using Altinn.App.Core.Interface;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Layout;
using Altinn.App.Core.Models.Layout.Components;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Internal.AppFiles;

/// <summary>
/// App implementation of the execution service needed for executing an Altinn Core Application (Functional term).
/// </summary>
public class AppResourcesNew : IAppResources
{
    private static readonly JsonSerializerOptions JSON_OPTIONS = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    private static readonly JsonDocumentOptions JSON_DOCUMENT_OPTIONS = new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };

    private readonly AppSettings _settings;
    private readonly ILogger _logger;
    private readonly AppFilesBytes? _bytes;
    private AppFilesBytes Bytes
    {
        get 
        {
            return _bytes ?? AppFilesBytes.Bytes;
        }
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="AppResourcesNew"/> class.
    /// </summary>
    /// <param name="bytes">AppFilesBytes that should be used (remove when used in dependency injection)</param>
    /// <param name="settings">The app repository settings.</param>
    /// <param name="logger">A logger from the built in logger factory.</param>
    public AppResourcesNew(
        IOptions<AppSettings> settings,
        ILogger<AppResourcesNew> logger,
        AppFilesBytes? bytes = null)
    {
        _settings = settings.Value;
        _logger = logger;
        _bytes = bytes;
    }

    /// <inheritdoc/>
    public byte[] GetAppResource(string org, string app, string resource)
    {
        byte[] fileContent;

        if (resource == _settings.RuleHandlerFileName)
        {
            fileContent = ReadFileContentsFromLegalPath(_settings.AppBasePath + _settings.UiFolder, resource);
        }
        else if (resource == _settings.FormLayoutJSONFileName)
        {
            fileContent = ReadFileContentsFromLegalPath(_settings.AppBasePath + _settings.UiFolder, resource);
        }
        else if (resource == _settings.RuleConfigurationJSONFileName)
        {
            fileContent = ReadFileContentsFromLegalPath(_settings.AppBasePath + _settings.UiFolder, resource);

            if (fileContent == null)
            {
                fileContent = new byte[0];
            }
        }
        else
        {
            fileContent = ReadFileContentsFromLegalPath(_settings.AppBasePath + _settings.GetResourceFolder(), resource);
        }

        return fileContent;
    }

    /// <inheritdoc />
    public byte[]? GetText(string org, string app, string textResource)
    {
        return Bytes.Texts.TryGetValue(textResource, out var value) ? value : null;
    }

    /// <inheritdoc />
    public Task<TextResource?> GetTexts(string org, string app, string language)
    {
        var bytes = GetText(org, app, language);
        if (bytes is null)
        {
            return Task.FromResult<TextResource?>(null);
        }
        var textResource = System.Text.Json.JsonSerializer.Deserialize<TextResource>(bytes, JSON_OPTIONS);
        if (textResource is null)
        {
            return Task.FromResult<TextResource?>(null);
        }
        textResource.Id = $"{org}-{app}-{language}";
        textResource.Org = org;
        textResource.Language = language;

        return Task.FromResult<TextResource?>(textResource);
    }

    /// <inheritdoc />
    public Application GetApplication()
    {
        return System.Text.Json.JsonSerializer.Deserialize<Application>(Bytes.ApplicationMetadata, JSON_OPTIONS)!;
    }

    /// <inheritdoc/>
    public string GetApplicationXACMLPolicy()
    {
        return Encoding.UTF8.GetString(Bytes.Policy);
    }

    /// <inheritdoc/>
    public string GetApplicationBPMNProcess()
    {
        return Encoding.UTF8.GetString(Bytes.Process);
    }

    /// <inheritdoc/>
    public string GetModelMetaDataJSON(string org, string app)
    {
        Application applicationMetadata = GetApplication();

        string dataTypeId = applicationMetadata
            .DataTypes
            .FirstOrDefault(data=>data.AppLogic != null && !string.IsNullOrEmpty(data.AppLogic.ClassRef))
            ?.Id
            ?? string.Empty;

        return GetModelJsonSchema(dataTypeId);
    }

    /// <inheritdoc/>
    public string GetModelJsonSchema(string modelId)
    {
        if (Bytes.ModelSchemas.TryGetValue(modelId, out var value))
        {
            return Encoding.UTF8.GetString(value);
        }

        // Keep current behaviour
        throw new FileNotFoundException(null, $"models/{modelId}.schema.json");
    }

    /// <inheritdoc/>
    public byte[]? GetRuntimeResource(string resource)
    {
        byte[]? fileContent = null;
        string path;
        if (resource == _settings.RuntimeAppFileName)
        {
            path = Path.Join(_settings.AppBasePath, "www", "runtime", "js", "react", _settings.RuntimeAppFileName);
        }
        else if (resource == _settings.ServiceStylesConfigFileName)
        {
            return Encoding.UTF8.GetBytes(_settings.GetStylesConfig());
        }
        else
        {
            path = Path.Join(_settings.AppBasePath, "www", "runtime", "css", "react", _settings.RuntimeCssFileName);
        }

        if (File.Exists(path))
        {
            fileContent = File.ReadAllBytes(path);
        }

        return fileContent;
    }

    /// <inheritdoc />
    public string? GetPrefillJson(string dataModelName = "ServiceModel")
    {
        if (Bytes.ModelPrefill.TryGetValue(dataModelName, out var data))
        {
            return Encoding.UTF8.GetString(data);
        }

        return null;
    }

    /// <inheritdoc />
    public string? GetLayoutSettingsString()
    {
        return GetLayoutSettingsStringForSet("");
    }

    /// <inheritdoc />
    public LayoutSettings GetLayoutSettings()
    {
        var defaultSet = Bytes.LayoutSetFiles.TryGetValue(string.Empty, out var value) ? value : null;
        var data = defaultSet?.Settings;

        return JsonSerializer.Deserialize<LayoutSettings>(data, JSON_OPTIONS)!;
    }

    /// <inheritdoc />
    public string GetClassRefForLogicDataType(string dataType)
    {
        Application application = GetApplication();
        string classRef = string.Empty;

        DataType? element = application.DataTypes.SingleOrDefault(d => d.Id.Equals(dataType));

        if (element != null)
        {
            classRef = element.AppLogic.ClassRef;
        }

        return classRef;
    }

    /// <inheritdoc />
    public string GetLayouts()
    {
        return GetLayoutsForSet(string.Empty);
    }

    /// <inheritdoc />
    public string? GetLayoutSets()
    {
        if (Bytes.LayoutSetsSettings is not null)
        {
            return Encoding.UTF8.GetString(Bytes.LayoutSetsSettings);
        }
        return null;
    }

    /// <inheritdoc />
    public LayoutSets? GetLayoutSet()
    {
        if (Bytes.LayoutSetsSettings is not null)
        {
            return JsonSerializer.Deserialize<LayoutSets>(Bytes.LayoutSetsSettings, JSON_OPTIONS);
        }
        return null;
    }

    /// <inheritdoc />
    public LayoutSet? GetLayoutSetForTask(string taskId)
    {
        var sets = GetLayoutSet();
        return sets?.Sets?.FirstOrDefault(s => s?.Tasks?.Contains(taskId) ?? false);
    }

    /// <inheritdoc />
    public string GetLayoutsForSet(string layoutSetId)
    {
        Dictionary<string, object> layouts = new Dictionary<string, object>();
        try
        {
            if (Bytes.LayoutSetFiles.TryGetValue(layoutSetId, out var value))
            {
                foreach (var (pageName, page) in value.Pages)
                {
                    layouts[pageName] = JsonDocument.Parse(page, JSON_DOCUMENT_OPTIONS);
                }
            }

            return JsonSerializer.Serialize(layouts);
        }
        finally
        {
            // We can't dispose the normal way with a using statement,
            // Becuase I need all the documents serialized first.
            foreach (IDisposable? disposableDoc in layouts.Values)
            {
                disposableDoc?.Dispose();
            }
        }
    }

    /// <inheritdoc />
    public LayoutModel GetLayoutModel(string? layoutSetId = null)
    {
        var order = GetLayoutSettingsForSet(layoutSetId)?.Pages?.Order;
        if (order is null)
        {
            throw new InvalidDataException("No $Pages.Order field found" + (layoutSetId is null ? "" : $" for layoutSet {layoutSetId}"));
        }

        var layoutModel = new LayoutModel();
        foreach (var (page, pageBytes) in Bytes.LayoutSetFiles[layoutSetId ?? ""].Pages)
        {
            // Somewhat ugly (but thread safe) way to pass the page name to the deserializer compoent by associating it with a specific options instance.
            // That way we need a new options instance here
            PageComponentConverter.SetAsyncLocalPageName(page);
            layoutModel.Pages[page] = System.Text.Json.JsonSerializer.Deserialize<PageComponent>(pageBytes.RemoveBom(), JSON_OPTIONS) ?? throw new InvalidDataException(page + ".json is \"null\"");
        }

        return layoutModel;
    }

    /// <inheritdoc />
    public string? GetLayoutSettingsStringForSet(string layoutSetId)
    {
        var defaultSet = Bytes.LayoutSetFiles.TryGetValue(layoutSetId, out var value) ? value : null;
        var data = defaultSet?.Settings;
        return data is not null ? Encoding.UTF8.GetString(data) : null;
    }

    /// <inheritdoc />
    public LayoutSettings? GetLayoutSettingsForSet(string? layoutSetId)
    {
        var defaultSet = Bytes.LayoutSetFiles.TryGetValue(string.Empty, out var value) ? value : null;
        var data = defaultSet?.Settings;
        return data is not null ? JsonSerializer.Deserialize<LayoutSettings>(data, JSON_OPTIONS) : null;
    }

    /// <inheritdoc />
    public byte[]? GetRuleConfigurationForSet(string id)
    {
        var defaultSet = Bytes.LayoutSetFiles.TryGetValue(id, out var value) ? value : null;
        return defaultSet?.RuleConfiguration;
    }

    /// <inheritdoc />
    public byte[]? GetRuleHandlerForSet(string id)
    {
        var defaultSet = Bytes.LayoutSetFiles.TryGetValue(id, out var value) ? value : null;
        return defaultSet?.RuleHandler;
    }


    private byte[] ReadFileContentsFromLegalPath(string legalPath, string filePath)
    {
        var fullFileName = legalPath + filePath;
        if (!PathHelper.ValidateLegalFilePath(legalPath, fullFileName))
        {
            throw new ArgumentException("Invalid argument", nameof(filePath));
        }

        if (File.Exists(fullFileName))
        {
            return File.ReadAllBytes(fullFileName);
        }

        return null;
    }
}
