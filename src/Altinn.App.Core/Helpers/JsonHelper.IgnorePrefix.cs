using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Altinn.App.Core.Helpers
{
    /// <summary>
    /// Helper class for processing JSON objects
    /// </summary>
    public static partial class JsonHelper
    {
        private static readonly Dictionary<string, JsonSerializerOptions> _ignorePrefixOptions = new();

        internal static JsonSerializerOptions GetOptionsWithIgnorePrefix(string prefix)
        {
            lock (_ignorePrefixOptions)
            {
                if (_ignorePrefixOptions.TryGetValue(prefix, out var options))
                {
                    return options;
                }

                var modifier = (JsonTypeInfo ti) =>
                {
                    if (ti.Kind != JsonTypeInfoKind.Object)
                        return;

                    ti.Properties.RemoveAll(prop => prop.Name.StartsWith(prefix));
                };

                JsonSerializerOptions newOptions = new()
                {
                    TypeInfoResolver = new DefaultJsonTypeInfoResolver
                    {
                        Modifiers = { modifier }
                    },
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                };
                _ignorePrefixOptions.Add(prefix, newOptions);
                return newOptions;
            }
        }

        internal static string SerializeIgnorePrefix(object obj, string prefix)
        {
            ArgumentNullException.ThrowIfNull(prefix);

            var options = GetOptionsWithIgnorePrefix(prefix);
            return JsonSerializer.Serialize(obj, options);
        }
    }
}