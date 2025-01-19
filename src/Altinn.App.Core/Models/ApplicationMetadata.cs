using System.Diagnostics;
using Altinn.Platform.Storage.Interface.Models;
using Newtonsoft.Json;

namespace Altinn.App.Core.Models;

/// <summary>
/// Extension of Application model from Storage. Adds app specific attributes to the model
/// </summary>
public class ApplicationMetadata : Application
{
    /// <summary>
    /// Create new instance of ApplicationMetadata
    /// </summary>
    /// <param name="id"></param>
    public ApplicationMetadata(string id)
    {
        base.Id = id;
        AppIdentifier = new AppIdentifier(id);
    }

    /// <summary>
    /// Override Id from base to ensure AppIdentifier is set
    /// </summary>
    public new string Id
    {
        get { return base.Id; }
        set
        {
            AppIdentifier = new AppIdentifier(value);
            base.Id = value;
        }
    }

    /// <summary>
    /// List of features and status (enabled/disabled)
    /// </summary>
    [JsonProperty(PropertyName = "features")]
    public Dictionary<string, bool>? Features { get; set; }

    /// <summary>
    /// Configure options for handling what happens when entering the application
    /// </summary>
    [JsonProperty(PropertyName = "onEntry")]
    public new OnEntry? OnEntry { get; set; }

    /// <summary>
    /// Get AppIdentifier based on ApplicationMetadata.Id
    /// Updated by setting ApplicationMetadata.Id
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public AppIdentifier AppIdentifier { get; private set; }

    /// <summary>
    /// Configure options for setting organisation logo
    /// </summary>
    [JsonProperty(PropertyName = "logo")]
    public Logo? Logo { get; set; }

    /// <summary>
    /// Frontend sometimes need to have knowledge of the nuget package version for backwards compatibility
    /// </summary>
    [JsonProperty(PropertyName = "altinnNugetVersion")]
    public string AltinnNugetVersion { get; set; } = LibVersion ?? throw new Exception("Couldn't get library version");

    internal static readonly string? LibVersion;

    static ApplicationMetadata()
    {
        var assembly = typeof(ApplicationMetadata).Assembly;
        var versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

        // Version from the reflected assembly name doesn't always contain all the version information (it comes from the AssemblyVersion property),
        // ProductVersion from FileVersionInfo starts out by being parsed from the AssemblyVersion: https://github.com/dotnet/runtime/blob/8a2e93486920436613fb4d3bce30f135933d91c6/src/libraries/System.Diagnostics.FileVersionInfo/src/System/Diagnostics/FileVersionInfo.Unix.cs#L93-L94
        // But it is overridden by any "InformationalVersion" attributes in the assembly: https://github.com/dotnet/runtime/blob/8a2e93486920436613fb4d3bce30f135933d91c6/src/libraries/System.Diagnostics.FileVersionInfo/src/System/Diagnostics/FileVersionInfo.Unix.cs#L149C63-L154
        // MinVer seems to set the informational version to the whole version string regardless if the MinVerVersion is computed or overridden form CI (which is the case for PR builds).
        // So for AltinnNugetVersion it is important that we prefer getting the version based on the InformationalVersion attribute (through FileVersionInfo).
        var productVersion = versionInfo.ProductVersion;
        if (!string.IsNullOrWhiteSpace(productVersion))
        {
            var plusIndex = productVersion.LastIndexOf('+');
            if (plusIndex > 0)
                productVersion = new string(productVersion.AsSpan(0, plusIndex));

            LibVersion = productVersion;
        }
        else
        {
            LibVersion = assembly.GetName().Version?.ToString();
        }
    }

    /// <summary>
    /// Holds properties that are not mapped to other properties
    /// </summary>
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, object>? UnmappedProperties { get; set; }

    /// <summary>
    /// List of ids for the external APIs registered in the application
    /// </summary>
    [JsonProperty(PropertyName = "externalApiIds")]
    public string[]? ExternalApiIds { get; set; }
}
