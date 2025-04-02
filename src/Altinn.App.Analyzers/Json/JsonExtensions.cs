// using System.Diagnostics.CodeAnalysis;
// using Newtonsoft.Json;
// using Newtonsoft.Json.Linq;

// namespace Altinn.App.Analyzers.Json;

// internal readonly record struct JsonToken<T>(string PropertyName, T? Value, int? LineNumber, int? LinePosition)
//     where T : JToken, IJsonLineInfo
// {
//     public static implicit operator JsonTokenDescriptor(JsonToken<T> token) =>
//         new JsonTokenDescriptor(token.PropertyName, token.LineNumber, token.LinePosition);

//     public bool IsValid([NotNullWhen(true)] out T? value, out JsonToken<T> self)
//     {
//         value = Value;
//         self = this;
//         return value is not null;
//     }

//     public bool IsValid([NotNullWhen(true)] out T? value)
//     {
//         value = Value;
//         return value is not null;
//     }

//     public ParsedJsonToken<TR> Parsed<TR>(TR value) =>
//         new ParsedJsonToken<TR>(PropertyName, value, LineNumber, LinePosition);
// }

// internal readonly record struct JsonTokenDescriptor(string PropertyName, int? LineNumber, int? LinePosition);

// internal readonly record struct ParsedJsonToken<T>(string PropertyName, T Value, int? LineNumber, int? LinePosition)
// {
//     public static implicit operator JsonTokenDescriptor(ParsedJsonToken<T> token) =>
//         new JsonTokenDescriptor(token.PropertyName, token.LineNumber, token.LinePosition);
// }

// internal static class JsonExtensions
// {
//     public static JsonToken<T> GetValue<T>(this JObject obj, string propertyName)
//         where T : JToken, IJsonLineInfo
//     {
//         var token = obj.GetValue(propertyName);
//         var lineInfo = token as IJsonLineInfo;
//         var linePosition = lineInfo?.LinePosition;
//         if (linePosition is not null && token is JValue jValue)
//         {
//             var decr = jValue.Value switch
//             {
//                 string v => v.Length,
//                 int v => v == 0 ? 1 : (int)Math.Floor(Math.Log10(v) + 1),
//                 long v => v == 0 ? 1 : (int)Math.Floor(Math.Log10(v) + 1),
//                 uint v => v == 0 ? 1 : (int)Math.Floor(Math.Log10(v) + 1),
//                 ulong v => v == 0 ? 1 : (int)Math.Floor(Math.Log10(v) + 1),
//                 float v => v == 0 ? 1 : (int)Math.Floor(Math.Log10(v) + 1),
//                 double v => v == 0 ? 1 : (int)Math.Floor(Math.Log10(v) + 1),
//                 _ => 1,
//             };
//             linePosition -= decr;
//         }
//         return new JsonToken<T>(propertyName, token as T, lineInfo?.LineNumber, linePosition);
//     }

//     public static IEnumerable<JsonToken<T>> EnumerateAs<T>(this JsonToken<JArray> arrayToken)
//         where T : JToken, IJsonLineInfo
//     {
//         var array = arrayToken.Value;
//         if (array is null)
//             throw new ArgumentException(
//                 $"Can't iterate array-token '{arrayToken.PropertyName}', array was null",
//                 nameof(arrayToken)
//             );

//         for (int i = 0; i < array.Count; i++)
//         {
//             var token = array[i];
//             var lineInfo = token as IJsonLineInfo;
//             yield return new JsonToken<T>(
//                 $"{arrayToken.PropertyName}[{i}]",
//                 token as T,
//                 lineInfo?.LineNumber,
//                 lineInfo?.LinePosition
//             );
//         }
//     }

//     public static bool IsValidString(this JsonToken<JValue> token, out string? value, out ParsedJsonToken<string?> self)
//     {
//         self = new ParsedJsonToken<string?>(token.PropertyName, null, token.LineNumber, token.LinePosition);
//         value = null;

//         if (token.Value is null) // Must be a resolved property
//             return false;

//         if (token.Value.Value is null or string) // Null is also valid here
//         {
//             value = (string?)token.Value.Value;
//             self = new ParsedJsonToken<string?>(token.PropertyName, value, token.LineNumber, token.LinePosition);
//             return true;
//         }

//         return false;
//     }

//     public static bool IsValidNonNullString(this JsonToken<JValue> token, out ParsedJsonToken<string> self)
//     {
//         // ! TODO: find a way to model this?
//         self = new ParsedJsonToken<string>(token.PropertyName, null!, token.LineNumber, token.LinePosition);

//         if (token.Value is null) // Must be a resolved property
//             return false;

//         if (token.Value.Value is string) // Null is also valid here
//         {
//             var value = (string)token.Value.Value;
//             self = new ParsedJsonToken<string>(token.PropertyName, value, token.LineNumber, token.LinePosition);
//             return true;
//         }

//         return false;
//     }
// }
