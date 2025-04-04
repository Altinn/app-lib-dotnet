namespace Altinn.App.Core.Constants;

/// <summary>
/// app token
/// </summary>
public static class General
{
    /// <summary>
    /// App token name
    /// </summary>
    public const string AppTokenName = "AltinnToken";

    /// <summary>
    /// The name of the authorization token header
    /// </summary>
    public const string AuthorizationTokenHeaderName = "Authorization";

    /// <summary>
    /// The name of the cookie used for asp authentication in runtime application
    /// </summary>
    public const string RuntimeCookieName = "AltinnStudioRuntime";

    /// <summary>
    /// The name of the cookie used for asp authentication in designer application
    /// </summary>
    public const string DesignerCookieName = "AltinnStudioDesigner";

    /// <summary>
    /// The name of the API management subscription key header
    /// </summary>
    public const string SubscriptionKeyHeaderName = "Ocp-Apim-Subscription-Key";

    /// <summary>
    /// The name of the platform access token header
    /// </summary>
    public const string PlatformAccessTokenHeaderName = "PlatformAccessToken";

    /// <summary>
    /// The name of the eFormidling Integration Point token header
    /// </summary>
    public const string EFormidlingAccessTokenHeaderName = "AltinnIntegrationPointToken";
}
