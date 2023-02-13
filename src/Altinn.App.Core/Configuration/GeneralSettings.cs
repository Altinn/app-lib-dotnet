namespace Altinn.App.Core.Configuration
{
    /// <summary>
    /// General configuration settings
    /// </summary>
    public class GeneralSettings
    {
        /// <summary>
        /// Gets or sets the soft validation prefix.
        /// </summary>
        public string SoftValidationPrefix { get; set; } = "*WARNING*";

        /// <summary>
        /// Gets or sets the fixed validation prefix.
        /// </summary>
        public string FixedValidationPrefix { get; set; } = "*FIXED*";

        /// <summary>
        /// Gets or sets the info validation prefix.
        /// </summary>
        public string InfoValidationPrefix { get; set; } = "*INFO*";

        /// <summary>
        /// Gets or sets the success validation prefix.
        /// </summary>
        public string SuccessValidationPrefix { get; set; } = "*SUCCESS*";

        /// <summary>
        /// Gets or sets the host name. This is used for cookes,
        /// and might not be the full url you can access the app on.
        /// </summary>
        public string HostName { get; set; } = "local.altinn.cloud";

        /// <summary>
        /// Gets or sets the AltinnParty cookie name.
        /// </summary>
        public string AltinnPartyCookieName { get; set; } = "AltinnPartyId";

        /// <summary>
        /// Gets the altinn party cookie from kubernetes environment variables or appSettings if environment variable is missing.
        /// </summary>
        public string GetAltinnPartyCookieName
        {
            get
            {
                return Environment.GetEnvironmentVariable("GeneralSettings__AltinnPartyCookieName") ?? AltinnPartyCookieName;
            }
        }
    }
}
