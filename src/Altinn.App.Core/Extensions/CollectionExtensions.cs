namespace Altinn.App.Core.Extensions;

/// <summary>
/// Extensions for Collections.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Checks whether enumerable is null or empty.
    /// </summary>
    /// <typeparam name="T">The type of the enumerable.</typeparam>
    /// <param name="enumerable">The System.Collections.Generic.IEnumerable`1 to be checked.</param>
    /// <returns>True if enumerable is null or empty, false otherwise.</returns>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? enumerable)
    {
        if (enumerable is not null)
        {
            return !enumerable.Any();
        }

        return true;
    }
}
