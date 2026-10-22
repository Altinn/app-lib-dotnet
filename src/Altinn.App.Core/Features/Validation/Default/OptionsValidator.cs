using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Altinn.App.Core.Features.Options;
using Altinn.App.Core.Features.Options.Altinn3LibraryCodeList;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Expressions;
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
/// The options are resolved the way the frontend resolves them: listed in the layout (<c>options</c>), from the
/// data model (<c>source</c>), or from <c>optionsId</c> through the app's options providers or the code list
/// library, with the query parameters from <c>mapping</c> and <c>queryParameters</c>. Read-only components are
/// not checked, because the frontend keeps their values as they are. Components that store the selection in a
/// repeating group (<c>group</c> binding) and options that cannot be resolved are not checked either.
/// </remarks>
internal sealed partial class OptionsValidator : IValidator
{
    private readonly IAppResources _appResources;
    private readonly IAppOptionsService _appOptionsService;
    private readonly IAltinn3LibraryCodeListService _altinn3LibraryCodeListService;
    private readonly ILogger<OptionsValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OptionsValidator"/> class.
    /// </summary>
    public OptionsValidator(
        IAppResources appResources,
        IAppOptionsService appOptionsService,
        IAltinn3LibraryCodeListService altinn3LibraryCodeListService,
        ILogger<OptionsValidator> logger
    )
    {
        _appResources = appResources;
        _appOptionsService = appOptionsService;
        _altinn3LibraryCodeListService = altinn3LibraryCodeListService;
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
    /// the form, and resolving options on every save could call external options providers.
    /// </summary>
    public bool NoIncrementalValidation => true;

    /// <inheritdoc />
    public async Task<List<ValidationIssue>> Validate(
        IInstanceDataAccessor dataAccessor,
        string taskId,
        string? language
    )
    {
        var evaluationState = dataAccessor.GetLayoutEvaluatorState();
        var validationIssues = new List<ValidationIssue>();
        var optionsCache = new Dictionary<string, HashSet<string>?>();
        foreach (var context in await evaluationState.GetComponentContexts())
        {
            await ValidateOptionsRecurs(
                validationIssues,
                evaluationState,
                context,
                dataAccessor,
                language,
                optionsCache
            );
        }

        return validationIssues;
    }

    /// <summary>
    /// Not used, as <see cref="NoIncrementalValidation"/> is true
    /// </summary>
    public Task<bool> HasRelevantChanges(
        IInstanceDataAccessor dataAccessor,
        string taskId,
        DataElementChanges changes
    ) => Task.FromResult(true);

    private async Task ValidateOptionsRecurs(
        List<ValidationIssue> validationIssues,
        LayoutEvaluatorState state,
        ComponentContext context,
        IInstanceDataAccessor dataAccessor,
        string? language,
        Dictionary<string, HashSet<string>?> optionsCache
    )
    {
        if (await context.IsHidden(evaluateRemoveWhenHidden: false))
        {
            return;
        }

        foreach (var childContext in context.ChildContexts)
        {
            await ValidateOptionsRecurs(validationIssues, state, childContext, dataAccessor, language, optionsCache);
        }

        if (
            context.Component is not OptionsComponent component
            || !component.DataModelBindings.TryGetValue("simpleBinding", out var binding)
            || component.DataModelBindings.ContainsKey("group")
            || await ExpressionEvaluator.EvaluateExpression(state, component.ReadOnly, context) is true
        )
        {
            return;
        }

        var value = ToOptionValue(await state.GetModelData(binding, context.DataElementIdentifier, context.RowIndices));
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        var options = await GetOptionValues(component, state, context, dataAccessor, language, optionsCache);
        if (options is null)
        {
            return;
        }

        // Like the frontend: multi-select values are joined with a comma, and nothing is trimmed
        var selectedValues = IsMultiSelect(component) ? value.Split(',') : [value];
        var invalidValues = selectedValues.Where(v => !options.Contains(v)).ToList();
        if (invalidValues.Count == 0)
        {
            return;
        }

        var field = await state.AddInidicies(binding, context);
        validationIssues.Add(
            new ValidationIssue
            {
                Severity = ValidationIssueSeverity.Error,
                DataElementId = field.DataElementIdentifier.ToString(),
                Field = field.Field,
                Code = "invalidOption",
                Description =
                    $"{string.Join(", ", invalidValues.Select(v => $"\"{v}\""))} is not among the options of component {component.Id}",
            }
        );
    }

    private static bool IsMultiSelect(OptionsComponent component) =>
        component.Type.Equals("checkboxes", StringComparison.OrdinalIgnoreCase)
        || component.Type.Equals("multipleselect", StringComparison.OrdinalIgnoreCase);

    private async Task<HashSet<string>?> GetOptionValues(
        OptionsComponent component,
        LayoutEvaluatorState state,
        ComponentContext context,
        IInstanceDataAccessor dataAccessor,
        string? language,
        Dictionary<string, HashSet<string>?> optionsCache
    )
    {
        if (component.Options is not null)
        {
            return ToOptionValueSet(component.Options);
        }

        if (component.OptionsSource is not null)
        {
            return await GetSourceOptionValues(component.OptionsSource, state, context);
        }

        // OptionsComponent.Parse requires one of options, source and optionsId
        ArgumentNullException.ThrowIfNull(component.OptionsId);
        var queryParameters = await GetQueryParameters(component, state, context);
        var cacheKey =
            $"{component.OptionsId}|{component.Secure}|{JsonSerializer.Serialize(queryParameters.OrderBy(p => p.Key))}";
        if (!optionsCache.TryGetValue(cacheKey, out var optionValues))
        {
            var appOptions = await GetAppOptions(
                component,
                component.OptionsId,
                dataAccessor,
                language,
                queryParameters
            );
            optionValues = appOptions?.Options is { } options ? ToOptionValueSet(options) : null;
            optionsCache[cacheKey] = optionValues;
        }

        return optionValues;
    }

    private async Task<AppOptions?> GetAppOptions(
        OptionsComponent component,
        string optionsId,
        IInstanceDataAccessor dataAccessor,
        string? language,
        Dictionary<string, string> queryParameters
    )
    {
        try
        {
            var libraryRefMatch = LibraryRefRegex().Match(optionsId);
            if (libraryRefMatch.Success)
            {
                return await _altinn3LibraryCodeListService.GetAppOptionsAsync(
                    libraryRefMatch.Groups["org"].Value,
                    libraryRefMatch.Groups["codeListId"].Value,
                    libraryRefMatch.Groups["version"].Value,
                    language,
                    CancellationToken.None
                );
            }

            if (component.Secure || _appOptionsService.IsInstanceAppOptionsProviderRegistered(optionsId))
            {
                return await _appOptionsService.GetOptionsAsync(
                    new InstanceIdentifier(dataAccessor.Instance),
                    optionsId,
                    language,
                    queryParameters
                );
            }

            return await _appOptionsService.GetOptionsAsync(optionsId, language, queryParameters);
        }
        catch (HttpRequestException e)
        {
            // An options provider that is down should not stop the submission
            _logger.LogWarning(
                e,
                "Could not get the options {OptionsId} to validate component {ComponentId}",
                optionsId,
                component.Id
            );
            return null;
        }
    }

    /// <summary>
    /// The query parameters the frontend sends to the options provider: the values from <c>mapping</c>, then the
    /// evaluated <c>queryParameters</c>, which win when a name is in both.
    /// </summary>
    private static async Task<Dictionary<string, string>> GetQueryParameters(
        OptionsComponent component,
        LayoutEvaluatorState state,
        ComponentContext context
    )
    {
        var queryParameters = new Dictionary<string, string>();
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

        return queryParameters;
    }

    /// <summary>
    /// The values of the rows in the <c>source</c> group, read like the frontend: the value path must start with
    /// the group path, and a missing value is an empty option.
    /// </summary>
    private static async Task<HashSet<string>?> GetSourceOptionValues(
        OptionsSource source,
        LayoutEvaluatorState state,
        ComponentContext context
    )
    {
        var groupPath = WithoutRowIndexes(source.Group);
        var valuePath = WithoutRowIndexes(source.Value);
        if (!valuePath.StartsWith(groupPath + ".", StringComparison.Ordinal))
        {
            return null;
        }

        var group = await state.AddInidicies(new ModelBinding { Field = groupPath }, context);
        var rows = await state.GetModelData(
            new ModelBinding { Field = group.Field },
            group.DataElementIdentifier,
            null
        );
        var rowCount = rows is System.Collections.ICollection collection ? collection.Count : 0;
        var values = new HashSet<string>(StringComparer.Ordinal);
        for (var row = 0; row < rowCount; row++)
        {
            var value = await state.GetModelData(
                new ModelBinding { Field = $"{group.Field}[{row}]{valuePath[groupPath.Length..]}" },
                group.DataElementIdentifier,
                null
            );
            values.Add(ToOptionValue(value) ?? "");
        }

        return values;
    }

    private static HashSet<string> ToOptionValueSet(IEnumerable<AppOption> options) =>
        options.Select(o => o.Value ?? "null").ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// The value as the frontend compares it with the option values, which are read as strings with numbers
    /// formatted like <see cref="AppOptionConverter"/> does.
    /// </summary>
    private static string? ToOptionValue(object? value) => value is null ? null : FormatValue(value);

    private static string FormatValue(object value) =>
        value switch
        {
            string s => s,
            bool b => b ? "true" : "false",
            IConvertible number when IsNumber(number) => Convert
                .ToDouble(number, CultureInfo.InvariantCulture)
                .ToString(CultureInfo.InvariantCulture),
            _ => string.Create(CultureInfo.InvariantCulture, $"{value}"),
        };

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
    /// A mapped value as the frontend sends it: empty for null, the value itself for strings, numbers and
    /// booleans, and JSON for anything else.
    /// </summary>
    private static string ToQueryParameterValue(object? value) =>
        value switch
        {
            null => "",
            string or bool => FormatValue(value),
            IConvertible number when IsNumber(number) => FormatValue(number),
            _ => JsonSerializer.Serialize(value),
        };

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

    [GeneratedRegex(@"^lib\*\*(?<org>[a-zA-Z0-9]+)\*\*(?<codeListId>[a-zA-Z0-9_-]+)\*\*(?<version>[a-zA-Z0-9._-]+)$")]
    private static partial Regex LibraryRefRegex();
}
