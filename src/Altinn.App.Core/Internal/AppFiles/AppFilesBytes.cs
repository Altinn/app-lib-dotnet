namespace Altinn.App.Core.Internal.AppFiles;

/// <summary>
/// Collection class for all the json files in an app
/// </summary>
public class AppFilesBytes
{
    private static AppFilesBytes? _bytes;
    /// <summary>
    /// Access to the <see cref="AppFilesBytes" /> that are statically loaded
    /// </summary>
    public static AppFilesBytes Bytes
    {
        get
        {
            return _bytes ?? throw new InvalidOperationException($"{nameof(AppFilesBytes.Bytes)} not initialized");
        }
        set
        {
            _bytes = value;
        }
    }
    /// <summary>
    /// Content of the App/config/authorization/policy.xml
    /// </summary>
    public byte[] Policy { get; init; } = default!;
    /// <summary>
    /// Content of App/config/process/process.bpmn
    /// </summary>
    public byte[] Process { get; init; } = default!;
    /// <summary>
    /// Language resources in /app/config/texts/resource.{lang}.json
    /// dictionary key is language
    /// </summary>
    public Dictionary<string, byte[]> Texts { get; init; } = default!;
    /// <summary>
    /// Content of app/config/applicationmetadata.json
    /// </summary>
    public byte[] ApplicationMetadata { get; init; } = default!;
    /// <summary>
    /// Content of app/ui/layout-sets.json or null, if a single layout is used.
    /// </summary>
    public byte[]? LayoutSetsSettings {get; init; }
    /// <summary>
    /// Content of the app/ui folder (optionally) separated into layout sets.
    /// </summary>
    /// <remarks>
    /// String.Empty is used as key, if there are no layout sets in the app
    /// </remarks>
    public Dictionary<string, LayoutSetFiles> LayoutSetFiles { get; init; } = default!;

    /// <summary>
    /// Content of the app/models/*.metadata.json fiels keyed on the fileneame without .metadata.json
    /// </summary>
    public Dictionary<string, byte[]> ModelMetadata { get; init; } = default!;
    /// <summary>
    /// Content of the app/models/*.prefill.json fiels keyed on the fileneame without .prefill.json
    /// </summary>
    public Dictionary<string, byte[]> ModelPrefill { get; init; } = default!;
    /// <summary>
    /// Content of the app/models/*.schema.json fiels keyed on the fileneame without .schema.json
    /// </summary>
    public Dictionary<string, byte[]> ModelSchemas { get; init; } = default!;
    /// <summary>
    /// Content of the app/models/*.xsd fiels keyed on the fileneame without .xsd
    /// </summary>
    public Dictionary<string, byte[]> ModelXsds { get; init; } = default!;
}

/// <summary>
/// Utility class for storing multiple layout sets
/// </summary>
public class LayoutSetFiles
{
    /// <summary>
    /// Content of the json files that consists of the pages in the layout set
    /// </summary>
    /// <remarks>
    /// The key is [fileanme].json without the .json part
    /// </remarks>
    public Dictionary<string, byte[]> Pages { get; init; } = default!;
    /// <summary>
    /// Content of the settings.json file for the layout set
    /// </summary>
    public byte[]? Settings { get; init; } = default!;
    /// <summary>
    /// Content of the RuleHandler.js file for the layout set
    /// </summary>
    public byte[]? RuleHandler { get; init; } = default!;
    /// <summary>
    /// Content of the RuleConfiguration.json file for the layout set
    /// </summary>
    public byte[]? RuleConfiguration { get; init; } = default!;
}