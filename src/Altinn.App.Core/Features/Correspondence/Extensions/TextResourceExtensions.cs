using Altinn.Platform.Storage.Interface.Models;

internal static class TextResourceExtensions
{
    /// <summary>
    /// Gets the value for the specified key from the text resource.
    /// </summary>
    /// <param name="resource">The text resource.</param>
    /// <param name="key">The key to look up.</param>
    /// <returns>The value if found; otherwise, null.</returns>
    public static string? GetText(this TextResource resource, string? key)
    {
        if (key is null)
        {
            return null;
        }
        return resource.Resources?.FirstOrDefault(x => x.Id.Equals(key, StringComparison.Ordinal))?.Value;
    }

    /// <summary>
    /// Gets the value for the first matching key from the text resource.
    /// </summary>
    /// <param name="resource">The text resource.</param>
    /// <param name="keys">An array of keys to try in order.</param>
    /// <returns>The first matching value if found; otherwise, null.</returns>
    public static string? GetFirstMatchingText(this TextResource resource, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = resource.GetText(key);
            if (value is not null)
            {
                return value;
            }
        }
        return null;
    }
}
