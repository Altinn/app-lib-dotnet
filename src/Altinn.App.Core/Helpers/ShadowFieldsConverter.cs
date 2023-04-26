using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Altinn.App.Core.Helpers
{
    public class IgnorePropertiesWithPrefix
    {
        private readonly string _ignorePrefix;

        public IgnorePropertiesWithPrefix(string prefix)
            => _ignorePrefix = prefix;

        public void ModifyPrefixInfo(JsonTypeInfo ti)
        {
            if (ti.Kind != JsonTypeInfoKind.Object)
                return;

            ti.Properties.RemoveAll(prop => prop.Name.StartsWith(_ignorePrefix));
        }
    }

    public static class ListHelpers
    {
        // IList<T> implementation of List<T>.RemoveAll method.
        public static void RemoveAll<T>(this IList<T> list, Predicate<T> predicate)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (predicate(list[i]))
                {
                    list.RemoveAt(i--);
                }
            }
        }
    }
}