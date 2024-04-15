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

        /// <summary>
        /// Get a <see cref="JsonSerializerOptions"/> instance that ignores properties with a specific prefix.
        /// These instances are cached using the prefix as a key.
        /// </summary>
        public static JsonSerializerOptions GetOptionsWithIgnorePrefix(string prefix)
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
    }
}