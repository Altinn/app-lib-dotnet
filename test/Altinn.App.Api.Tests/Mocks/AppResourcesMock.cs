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

namespace App.IntegrationTests.Mocks.Services;

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

    // Use an in-memory dictionary or anything else for "prefill" data
    // that you might want to modify in a test with a Hook.
    public Dictionary<string, string> _prefillByDataType = new();

    //public Dictionary<string, string> prefill OnEntry { get; set; }

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

        // Optionally load some initial data for each known dataType from test files:
        // For example:
        // _prefillByDataType["SomeDataType"] = File.ReadAllText(TestData.GetPrefillJsonPath("SomeDataType"));
        // ...

        // Finally, apply any dynamic modifications from your test.
        foreach (var hook in _mutationHooks)
        {
            hook.Action(this);
        }
    }

    /// <summary>
    /// Lets your tests add or modify prefill JSON for certain data types.
    /// Example usage in test: <c>services.AddSingleton(new AppResourcesMutationHook(mock => {
    ///   mock.AddOrUpdatePrefill("SomeDataType", "{\"foo\":\"bar\"}");
    /// }))</c>
    /// </summary>
    public void AddOrUpdatePrefill(string dataType, string json)
    {
        _prefillByDataType[dataType] = json;
    }

    public byte[] GetText(string org, string app, string textResource)
    {
        throw new NotImplementedException();
    }

    public Task<TextResource?> GetTexts(string org, string app, string language)
    {
        throw new NotImplementedException();
    }

    public string GetModelJsonSchema(string modelId)
    {
        throw new NotImplementedException();
    }

    public Application GetApplication()
    {
        throw new NotImplementedException();
    }

    public string? GetApplicationXACMLPolicy()
    {
        throw new NotImplementedException();
    }

    public string? GetApplicationBPMNProcess()
    {
        throw new NotImplementedException();
    }

    public string? GetPrefillJson(string dataModelName = "ServiceModel")
    {
        // If test has set a custom prefill in memory, return it
        if (_prefillByDataType.TryGetValue(dataModelName, out var fromTest))
        {
            return fromTest;
        }
        return null;
    }

    public string GetClassRefForLogicDataType(string dataType)
    {
        throw new NotImplementedException();
    }

    public string GetLayouts()
    {
        throw new NotImplementedException();
    }

    public string? GetLayoutSettingsString()
    {
        throw new NotImplementedException();
    }

    public LayoutSettings GetLayoutSettings()
    {
        throw new NotImplementedException();
    }

    public string GetLayoutSets()
    {
        throw new NotImplementedException();
    }

    public Task<string?> GetFooter()
    {
        throw new NotImplementedException();
    }

    public LayoutSets? GetLayoutSet()
    {
        throw new NotImplementedException();
    }

    public LayoutSet? GetLayoutSetForTask(string taskId)
    {
        throw new NotImplementedException();
    }

    public string GetLayoutsForSet(string layoutSetId)
    {
        throw new NotImplementedException();
    }

    public LayoutModel? GetLayoutModelForTask(string taskId)
    {
        throw new NotImplementedException();
    }

    public LayoutModel GetLayoutModel(string? layoutSetId = null)
    {
        throw new NotImplementedException();
    }

    public string? GetLayoutSettingsStringForSet(string layoutSetId)
    {
        throw new NotImplementedException();
    }

    public LayoutSettings? GetLayoutSettingsForSet(string? layoutSetId)
    {
        throw new NotImplementedException();
    }

    public byte[] GetRuleConfigurationForSet(string id)
    {
        throw new NotImplementedException();
    }

    public byte[] GetRuleHandlerForSet(string id)
    {
        throw new NotImplementedException();
    }

    public string? GetValidationConfiguration(string dataTypeId)
    {
        throw new NotImplementedException();
    }
}
