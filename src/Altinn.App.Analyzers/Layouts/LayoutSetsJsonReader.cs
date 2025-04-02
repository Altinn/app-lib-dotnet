// using Altinn.App.Analyzers.Json;
// using Newtonsoft.Json;
// using Newtonsoft.Json.Linq;

// namespace Altinn.App.Analyzers.Layouts;

// internal sealed record LayoutSetsInfo(ParsedJsonToken<IReadOnlyList<LayoutSetInfo>> Sets);

// internal sealed record LayoutSetInfo(
//     ParsedJsonToken<string> Id,
//     ParsedJsonToken<string> DataType,
//     ParsedJsonToken<IReadOnlyList<ParsedJsonToken<string>>> Tasks
// );

// internal abstract record LayoutSetsParseResult
// {
//     private LayoutSetsParseResult() { }

//     internal sealed record Ok(LayoutSetsInfo Value) : LayoutSetsParseResult;

//     internal sealed record FailedToParse(JsonTokenDescriptor Token) : LayoutSetsParseResult;

//     internal sealed record Error(Exception Exception) : LayoutSetsParseResult;

//     internal sealed record Cancelled : LayoutSetsParseResult;
// }

// internal static class LayoutSetsJsonReader
// {
//     public static LayoutSetsParseResult Read(string json, CancellationToken cancellationToken)
//     {
//         try
//         {
//             if (cancellationToken.IsCancellationRequested)
//                 return new LayoutSetsParseResult.Cancelled();

//             var setsInfo = new List<LayoutSetInfo>();

//             using var reader = new JsonTextReader(new StringReader(json));

//             JsonLoadSettings loadSettings = new JsonLoadSettings()
//             {
//                 CommentHandling = CommentHandling.Ignore,
//                 LineInfoHandling = LineInfoHandling.Load,
//             };

//             var layoutSets = JObject.Load(reader, loadSettings);

//             if (!layoutSets.GetValue<JArray>("sets").IsValid(out _, out var setsToken))
//                 return new LayoutSetsParseResult.FailedToParse(setsToken);

//             foreach (var layoutSetToken in setsToken.EnumerateAs<JObject>())
//             {
//                 if (cancellationToken.IsCancellationRequested)
//                     return new LayoutSetsParseResult.Cancelled();

//                 var layoutSetResult = ProcessLayoutSet(setsInfo, layoutSetToken);
//                 if (layoutSetResult is not null)
//                     return layoutSetResult;
//             }

//             var result = new LayoutSetsInfo(setsToken.Parsed<IReadOnlyList<LayoutSetInfo>>(setsInfo));
//             return new LayoutSetsParseResult.Ok(result);
//         }
//         catch (Exception ex)
//         {
//             return new LayoutSetsParseResult.Error(ex);
//         }
//     }

//     private static LayoutSetsParseResult? ProcessLayoutSet(
//         List<LayoutSetInfo> setsInfo,
//         JsonToken<JObject> layoutSetToken
//     )
//     {
//         if (layoutSetToken.Value is not { } layoutSet)
//             return new LayoutSetsParseResult.FailedToParse(layoutSetToken);

//         if (!layoutSet.GetValue<JValue>("id").IsValidNonNullString(out var idToken))
//             return new LayoutSetsParseResult.FailedToParse(idToken);

//         if (!layoutSet.GetValue<JValue>("dataType").IsValidNonNullString(out var dataTypeToken))
//             return new LayoutSetsParseResult.FailedToParse(dataTypeToken);

//         List<ParsedJsonToken<string>> tasks = [];
//         if (layoutSet.GetValue<JArray>("tasks").IsValid(out _, out var tasksToken))
//         {
//             foreach (var taskToken in tasksToken.EnumerateAs<JValue>())
//             {
//                 if (!taskToken.IsValidNonNullString(out var task))
//                     return new LayoutSetsParseResult.FailedToParse(taskToken);

//                 tasks.Add(task);
//             }
//         }

//         var layoutSetInfo = new LayoutSetInfo(
//             idToken,
//             dataTypeToken,
//             tasksToken.Parsed<IReadOnlyList<ParsedJsonToken<string>>>(tasks)
//         );
//         setsInfo.Add(layoutSetInfo);

//         return null;
//     }
// }
