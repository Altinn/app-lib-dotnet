using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Altinn.App.Core.Features.Options;
using Altinn.App.Core.Features.Options.Altinn3LibraryCodeList;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Expressions;
using Altinn.App.Core.Internal.Language;
using Altinn.App.Core.Internal.Texts;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Expressions;
using Altinn.App.Core.Models.Layout;
using Altinn.App.Core.Models.Layout.Components;
using Altinn.App.Core.Models.Validation;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Features.Validation.Default;

/// <summary>
/// Validator that checks that the values of visible option components (checkboxes, radio buttons, dropdowns and
/// multiple selects) are among the component's options, as the frontend only lets the user pick those.
/// </summary>
/// <remarks>
/// The options are resolved the way the frontend resolves them: listed in the layout (<c>options</c>), from rows in
/// the data model (<c>source</c>), or from <c>optionsId</c> through the same options endpoint logic as
/// <c>OptionsController</c>, with the query parameters from <c>mapping</c> and <c>queryParameters</c>.
/// <para>
/// Components the user cannot change are not checked, because a value the user cannot fix should not stop the
/// submission: read-only components and components with <c>renderAsSummary</c>. Neither are components that store
/// the selection in a repeating group (<c>group</c> binding), nor components whose options cannot be resolved (for
/// example when an options provider fails, which is logged as a warning). The value only has to be among all the
/// options, so <c>optionFilter</c> is not evaluated.
/// </para>
/// <para>
/// Each distinct options list is fetched once per validation, up to four at a time, so an app's options provider
/// can be called concurrently with different query parameters.
/// </para>
/// </remarks>
internal sealed partial class OptionsValidator : IValidator
{
    private const int MaxConcurrentOptionsRequests = 4;
    private const string InvalidOptionTextKey = "backend.validation_errors.invalid_option";

    private readonly IAppResources _appResources;
    private readonly IAppOptionsService _appOptionsService;
    private readonly IAltinn3LibraryCodeListService _altinn3LibraryCodeListService;
    private readonly ITranslationService _translationService;
    private readonly ILogger<OptionsValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OptionsValidator"/> class.
    /// </summary>
    public OptionsValidator(
        IAppResources appResources,
        IAppOptionsService appOptionsService,
        IAltinn3LibraryCodeListService altinn3LibraryCodeListService,
        ITranslationService translationService,
        ILogger<OptionsValidator> logger
    )
    {
        _appResources = appResources;
        _appOptionsService = appOptionsService;
        _altinn3LibraryCodeListService = altinn3LibraryCodeListService;
        _translationService = translationService;
        _logger = logger;
    }

    /// <summary>
    /// We implement <see cref="ShouldRunForTask"/> instead
    /// </summary>
    public string TaskId => "*";

    /// <summary>
    /// Only run for tasks that specifies a layout set
    /// </summary>
    public bool ShouldRunForTask(string taskId) =>
        _appResources.GetLayoutSet()?.Sets.SelectMany(s => s.Tasks ?? []).Any(t => t == taskId) ?? false;

    /// <inheritdoc />
    public string ValidationSource => "Options";

    /// <summary>
    /// The frontend only lets the user pick among the options, so there is nothing to show while the user fills in
    /// the form, and resolving options on every save could call external options providers. Like other validators
    /// that do not run incrementally, it runs on full validation, which the frontend also requests after each save
    /// once a submission has failed.
    /// </summary>
    public bool NoIncrementalValidation => true;

    /// <summary>
    /// This is not used for incremental validation
    /// </summary>
    public Task<bool> HasRelevantChanges(
        IInstanceDataAccessor dataAccessor,
        string taskId,
        DataElementChanges changes
    ) => Task.FromResult(false);

    /// <inheritdoc />
    public async Task<List<ValidationIssue>> Validate(
        IInstanceDataAccessor dataAccessor,
        string taskId,
        string? language
    )
    {
        // Like OptionsController, which the frontend always calls with a language
        language ??= LanguageConst.Nb;
        var state = dataAccessor.GetLayoutEvaluatorState();
        var selections = new List<Selection>();
        foreach (var context in await state.GetComponentContexts())
        {
            await CollectSelectionsRecurs(selections, dataAccessor, state, context, language);
        }

        var optionsLists = await GetOptionsLists(selections, dataAccessor, language);
        var invalidSelections = selections
            .Select(selection => (Selection: selection, InvalidValues: GetInvalidValues(selection, optionsLists)))
            .Where(s => s.InvalidValues.Count > 0)
            .ToList();
        if (invalidSelections.Count == 0)
        {
            return [];
        }

        // The frontend shows a customTextKey as it is when the app has no text for it, so the text goes in the
        // description. An app can still change it by adding the key to its texts.
        var description = await _translationService.TranslateTextKey(InvalidOptionTextKey, language);
        return invalidSelections.Select(s => CreateIssue(s.Selection, s.InvalidValues, description)).ToList();
    }

    /// <summary>
    /// Finds the visible, editable option components with a value, and their options, or the request for their
    /// options when they come from an options provider.
    /// </summary>
    private async Task CollectSelectionsRecurs(
        List<Selection> selections,
        IInstanceDataAccessor dataAccessor,
        LayoutEvaluatorState state,
        ComponentContext context,
        string language
    )
    {
        if (await context.IsHidden(evaluateRemoveWhenHidden: false))
        {
            return;
        }

        foreach (var childContext in context.ChildContexts)
        {
            await CollectSelectionsRecurs(selections, dataAccessor, state, childContext, language);
        }

        if (
            context.Component is not OptionsComponent component
            || !component.DataModelBindings.TryGetValue("simpleBinding", out var binding)
            || component.DataModelBindings.ContainsKey("group")
            || component.RenderAsSummary
        )
        {
            return;
        }

        try
        {
            if (await CreateSelection(component, binding, dataAccessor, state, context, language) is { } selection)
            {
                selections.Add(selection);
            }
        }
        catch (Exception e)
        {
            // A component that cannot be checked, for example because its options cannot be resolved or its
            // readOnly expression uses a function only the frontend has, should not stop the submission
            _logger.LogWarning(e, "Could not validate the options of component {ComponentId}", component.Id);
        }
    }

    /// <summary>
    /// The component's value with its options, or the request for them when they come from an options provider,
    /// or null when the component is read-only or has no value.
    /// </summary>
    private static async Task<Selection?> CreateSelection(
        OptionsComponent component,
        ModelBinding binding,
        IInstanceDataAccessor dataAccessor,
        LayoutEvaluatorState state,
        ComponentContext context,
        string language
    )
    {
        var readOnly = await ExpressionEvaluator.EvaluateExpressionToExpressionValue(
            dataAccessor,
            component.ReadOnly,
            context
        );
        if (readOnly.ToBoolLoose() is true)
        {
            return null;
        }

        var value = ToOptionValue(await state.GetModelData(binding, context.DataElementIdentifier, context.RowIndices));
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        var field = await state.AddInidicies(binding, context);
        return component switch
        {
            { Options: { } options } => new Selection(component, field, value, ToOptionValueSet(options), null),
            { OptionsSource: { } source } => new Selection(
                component,
                field,
                value,
                await GetSourceOptionValues(source, state, context, field),
                null
            ),
            _ => new Selection(
                component,
                field,
                value,
                null,
                await CreateOptionsListRequest(component, state, context, language)
            ),
        };
    }

    private static List<string> GetInvalidValues(
        Selection selection,
        IReadOnlyDictionary<string, HashSet<string>?> optionsLists
    )
    {
        var options = selection.OptionsListRequest is { } request ? optionsLists[request.Key] : selection.Options;
        if (options is null)
        {
            return [];
        }

        // Like the frontend: multi-select values are joined with a comma, and nothing is trimmed
        var selectedValues = IsMultiSelect(selection.Component) ? selection.Value.Split(',') : [selection.Value];
        return selectedValues.Where(v => !options.Contains(v)).ToList();
    }

    private static ValidationIssue CreateIssue(Selection selection, List<string> invalidValues, string? description) =>
        new()
        {
            Severity = ValidationIssueSeverity.Error,
            DataElementId = selection.Field.DataElementIdentifier.ToString(),
            Field = selection.Field.Field,
            Code = "InvalidOption",
            Description = description,
            CustomTextParameters = new Dictionary<string, string>
            {
                ["field"] = selection.Field.Field,
                ["componentId"] = selection.Component.Id,
                ["invalidValues"] = string.Join(",", invalidValues),
            },
        };

    private static bool IsMultiSelect(OptionsComponent component) =>
        component.Type.Equals("checkboxes", StringComparison.OrdinalIgnoreCase)
        || component.Type.Equals("multipleselect", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Fetches each distinct options list once, a few at a time.
    /// </summary>
    private async Task<IReadOnlyDictionary<string, HashSet<string>?>> GetOptionsLists(
        List<Selection> selections,
        IInstanceDataAccessor dataAccessor,
        string language
    )
    {
        var optionsLists = new ConcurrentDictionary<string, HashSet<string>?>();
        var requests = selections.Select(s => s.OptionsListRequest).OfType<OptionsListRequest>().DistinctBy(r => r.Key);
        await Parallel.ForEachAsync(
            requests,
            new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrentOptionsRequests },
            async (request, _) => optionsLists[request.Key] = await GetOptionsList(request, dataAccessor, language)
        );

        return optionsLists;
    }

    /// <summary>
    /// The options list the frontend gets from the options endpoint: the instance endpoint for <c>secure</c>
    /// components, which falls back to the other options like <c>OptionsController</c> does.
    /// </summary>
    private async Task<HashSet<string>?> GetOptionsList(
        OptionsListRequest request,
        IInstanceDataAccessor dataAccessor,
        string language
    )
    {
        try
        {
            AppOptions? appOptions = null;
            if (request.Secure)
            {
                appOptions = await _appOptionsService.GetOptionsAsync(
                    new InstanceIdentifier(dataAccessor.Instance),
                    request.OptionsId,
                    language,
                    request.QueryParameters
                );
            }

            if (appOptions?.Options is null)
            {
                appOptions = await GetNonInstanceOptions(request, language);
            }

            return appOptions.Options is { } options ? ToOptionValueSet(options) : null;
        }
        catch (Exception e)
        {
            // An options provider that fails should not stop the submission
            _logger.LogWarning(e, "Could not get the options {OptionsId} to validate against", request.OptionsId);
            return null;
        }
    }

    private async Task<AppOptions> GetNonInstanceOptions(OptionsListRequest request, string language)
    {
        var libraryRefMatch = LibraryRefRegex().Match(request.OptionsId);
        return libraryRefMatch.Success
            ? await _altinn3LibraryCodeListService.GetAppOptionsAsync(
                libraryRefMatch.Groups["org"].Value,
                libraryRefMatch.Groups["codeListId"].Value,
                libraryRefMatch.Groups["version"].Value,
                language,
                // IValidator.Validate has no cancellation token
                CancellationToken.None
            )
            : await _appOptionsService.GetOptionsAsync(request.OptionsId, language, request.QueryParameters);
    }

    /// <summary>
    /// The request the frontend sends for the options: <c>language</c>, then the values from <c>mapping</c>, then the
    /// evaluated <c>queryParameters</c>, each overriding the ones before when a name is in both.
    /// </summary>
    private static async Task<OptionsListRequest> CreateOptionsListRequest(
        OptionsComponent component,
        LayoutEvaluatorState state,
        ComponentContext context,
        string language
    )
    {
        // OptionsComponent.Parse requires one of options, source and optionsId
        ArgumentNullException.ThrowIfNull(component.OptionsId);
        var queryParameters = new Dictionary<string, string> { ["language"] = language };
        foreach (var (path, parameterName) in component.Mapping ?? new Dictionary<string, string>())
        {
            var value = await state.GetModelData(
                new ModelBinding { Field = WithRowIndexes(path, context.RowIndices) },
                context.DataElementIdentifier,
                null
            );
            queryParameters[parameterName] = ToQueryParameterValue(value);
        }

        foreach (var (parameterName, expression) in component.QueryParameters ?? new Dictionary<string, Expression>())
        {
            var value = await ExpressionEvaluator.EvaluateExpression(state, expression, context);
            queryParameters[parameterName] = ToOptionValue(value) ?? "";
        }

        return new OptionsListRequest(component.OptionsId, component.Secure, queryParameters);
    }

    /// <summary>
    /// The values of the rows in the <c>source</c> group, read like the frontend: the group is moved to the row the
    /// component is in, the value path must start with the group path, and a missing value is an empty option.
    /// </summary>
    private static async Task<HashSet<string>?> GetSourceOptionValues(
        OptionsSource source,
        LayoutEvaluatorState state,
        ComponentContext context,
        DataReference field
    )
    {
        if (source.Group is null || source.Value is null)
        {
            return null;
        }

        var groupPath = WithoutRowIndexes(source.Group);
        var valuePath = WithoutRowIndexes(source.Value);
        if (!valuePath.StartsWith(groupPath + ".", StringComparison.Ordinal))
        {
            return null;
        }

        var valuePathInRow = valuePath[groupPath.Length..];

        // Like the frontend, which moves the group to the component's row only when both are in the same data type
        var groupDataType = source.DataType ?? context.DataElementIdentifier.GetValueOrDefault().DataTypeId;
        if (groupDataType == field.DataElementIdentifier.DataTypeId)
        {
            groupPath = TransposeToRow(groupPath, field.Field);
        }

        var rows = await state.GetModelData(
            new ModelBinding { Field = groupPath, DataType = source.DataType },
            context.DataElementIdentifier,
            null
        );
        if (rows is not ICollection collection)
        {
            return null;
        }

        var values = new HashSet<string>(StringComparer.Ordinal);
        for (var row = 0; row < collection.Count; row++)
        {
            var value = await state.GetModelData(
                new ModelBinding { Field = $"{groupPath}[{row}]{valuePathInRow}", DataType = source.DataType },
                context.DataElementIdentifier,
                null
            );
            values.Add(ToOptionValue(value) ?? "");
        }

        return values;
    }

    /// <summary>
    /// Adds the row indexes from the component's binding to the start of a path they share, like the frontend's
    /// <c>transposeDataBinding</c>: <c>persons</c> stays <c>persons</c> for <c>tasks[1].assignee</c>, and
    /// <c>tasks.persons</c> becomes <c>tasks[1].persons</c>.
    /// </summary>
    private static string TransposeToRow(string path, string indexedField)
    {
        var pathParts = path.Split('.');
        var fieldParts = indexedField.Split('.');
        for (var i = 0; i < Math.Min(pathParts.Length, fieldParts.Length); i++)
        {
            var match = IndexedPartRegex().Match(fieldParts[i]);
            if (match.Groups["name"].Value != pathParts[i])
            {
                break;
            }

            pathParts[i] = fieldParts[i];
        }

        return string.Join('.', pathParts);
    }

    private static HashSet<string> ToOptionValueSet(IEnumerable<AppOption> options) =>
        options.Select(ToOptionValue).ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// The option value as the frontend gets it: <see cref="AppOptionConverter"/> writes number options as JSON
    /// numbers, and the frontend reads every value as a string.
    /// </summary>
    private static string ToOptionValue(AppOption option) =>
        option.ValueType == AppOptionValueType.Number && double.TryParse(option.Value, out var number)
            ? FormatNumber(number)
            : option.Value ?? "null";

    /// <summary>
    /// The value as the frontend compares it with the option values.
    /// </summary>
    private static string? ToOptionValue(object? value) => value is null ? null : FormatValue(value);

    private static string FormatValue(object value) =>
        value switch
        {
            string s => s,
            bool b => b ? "true" : "false",
            IConvertible number when IsNumber(number) => FormatNumber(
                Convert.ToDouble(number, CultureInfo.InvariantCulture)
            ),
            // Dates, enums and the like reach the frontend as JSON
            _ => JsonSerializer.SerializeToElement(value).ToString(),
        };

    private static string FormatNumber(double number) => number.ToString(CultureInfo.InvariantCulture);

    private static bool IsNumber(IConvertible value) =>
        value.GetTypeCode()
            is TypeCode.Byte
                or TypeCode.SByte
                or TypeCode.Int16
                or TypeCode.UInt16
                or TypeCode.Int32
                or TypeCode.UInt32
                or TypeCode.Int64
                or TypeCode.UInt64
                or TypeCode.Single
                or TypeCode.Double
                or TypeCode.Decimal;

    /// <summary>
    /// A mapped value as the frontend sends it: empty for null, and otherwise the value as it reads it from the
    /// data model JSON, which is JSON text for objects and lists.
    /// </summary>
    private static string ToQueryParameterValue(object? value) => value is null ? "" : FormatValue(value);

    /// <summary>
    /// Replaces the <c>[{0}]</c> placeholders in a mapped path with the row indexes of the component, like the
    /// frontend does inside repeating groups.
    /// </summary>
    private static string WithRowIndexes(string path, int[]? rowIndexes) =>
        RowIndexPlaceholderRegex()
            .Replace(
                path,
                match =>
                {
                    var depth = int.Parse(match.Groups["depth"].Value, CultureInfo.InvariantCulture);
                    return rowIndexes is not null && depth < rowIndexes.Length ? $"[{rowIndexes[depth]}]" : "";
                }
            );

    private static string WithoutRowIndexes(string path) => RowIndexPlaceholderRegex().Replace(path, "");

    [GeneratedRegex(@"\[\{(?<depth>\d+)\}\]")]
    private static partial Regex RowIndexPlaceholderRegex();

    [GeneratedRegex(@"^(?<name>[^\[]+)(\[\d+\])?$")]
    private static partial Regex IndexedPartRegex();

    [GeneratedRegex(@"^lib\*\*(?<org>[a-zA-Z0-9]+)\*\*(?<codeListId>[a-zA-Z0-9_-]+)\*\*(?<version>[a-zA-Z0-9._-]+)$")]
    private static partial Regex LibraryRefRegex();

    /// <summary>
    /// An option component's value, with its options, or the request for them when they come from an options provider
    /// </summary>
    private sealed record Selection(
        OptionsComponent Component,
        DataReference Field,
        string Value,
        HashSet<string>? Options,
        OptionsListRequest? OptionsListRequest
    );

    /// <summary>
    /// A request to the options endpoint. Requests with the same options id, endpoint and query parameters have the
    /// same key, so each options list is fetched once.
    /// </summary>
    private sealed record OptionsListRequest(string OptionsId, bool Secure, Dictionary<string, string> QueryParameters)
    {
        public string Key { get; } =
            $"{OptionsId}|{Secure}|{JsonSerializer.Serialize(QueryParameters.OrderBy(p => p.Key, StringComparer.Ordinal))}";
    }
}
