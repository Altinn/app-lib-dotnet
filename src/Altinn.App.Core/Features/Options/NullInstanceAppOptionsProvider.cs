using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Options
{
    /// <summary>
    /// Nullobject for cases where there is no match on the requested <see cref="IInstanceAppOptionsProvider"/>
    /// Always returns null so that the controller can return a 404
    /// </summary>
    public class NullInstanceAppOptionsProvider : IInstanceAppOptionsProvider
    {
        /// <inheritdoc/>
        public string Id => string.Empty;

        /// <inheritdoc/>
        public Task<AppOptions?> GetInstanceAppOptionsAsync(InstanceIdentifier instanceIdentifier, string language, Dictionary<string, string> keyValuePairs)
        {
            return Task.FromResult<AppOptions?>(null);
        }
    }
}
