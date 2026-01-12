namespace Altinn.App.Core.Configuration;

/// <summary>
/// Settings for DanClient
/// </summary>
public class DanSettings
{
    /// <summary>
    /// Base url for Dan API
    /// </summary>
    public required string BaseUrl { get; set; }

    /// <summary>
    /// Api subscription keys
    /// </summary>
    public required string SubscriptionKey { get; set; }

    /// <summary>
    /// Maskinporten scope
    /// </summary>
    public required string Scope { get; set; }
}
