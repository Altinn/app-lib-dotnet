using System.Text.Json;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Expressions;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Layout;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using ComponentContext = Altinn.App.Core.Models.Expressions.ComponentContext;

namespace Altinn.App.Core.Features.DataProcessing;

internal sealed class DataModelFieldCalculator
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ILogger<DataModelFieldCalculator> _logger;
    private readonly IAppResources _appResourceService;
    private readonly ILayoutEvaluatorStateInitializer _layoutEvaluatorStateInitializer;
    private readonly IDataElementAccessChecker _dataElementAccessChecker;
    private readonly Telemetry? _telemetry;

    public DataModelFieldCalculator(
        ILogger<DataModelFieldCalculator> logger,
        ILayoutEvaluatorStateInitializer layoutEvaluatorStateInitializer,
        IAppResources appResourceService,
        IDataElementAccessChecker dataElementAccessChecker,
        Telemetry? telemetry = null
    )
    {
        _logger = logger;
        _appResourceService = appResourceService;
        _layoutEvaluatorStateInitializer = layoutEvaluatorStateInitializer;
        _dataElementAccessChecker = dataElementAccessChecker;
        _telemetry = telemetry;
    }

    public async Task Calculate(IInstanceDataAccessor dataAccessor, string taskId)
    {
        using var activity = _telemetry?.StartCalculateActivity(dataAccessor.Instance.Id, taskId);
        foreach (var (dataType, dataElement) in dataAccessor.GetDataElementsWithFormDataForTask(taskId))
        {
            if (await _dataElementAccessChecker.CanRead(dataAccessor.Instance, dataType) is false)
            {
                continue;
            }

            var calculationConfig = _appResourceService.GetCalculationConfiguration(dataType.Id);
            if (!string.IsNullOrEmpty(calculationConfig))
            {
                await CalculateFormData(dataAccessor, dataElement, taskId, calculationConfig);
            }
        }
    }

    internal async Task CalculateFormData(
        IInstanceDataAccessor dataAccessor,
        DataElement dataElement,
        string taskId,
        string rawCalculationConfig
    )
    {
        var evaluatorState = await _layoutEvaluatorStateInitializer.Init(dataAccessor, taskId);
        var hiddenFields = await LayoutEvaluator.GetHiddenFieldsForRemoval(
            evaluatorState,
            evaluateRemoveWhenHidden: false
        );
        DataElementIdentifier dataElementIdentifier = dataElement;
        var dataModelFieldCalculations = ParseDataModelFieldCalculationConfig(rawCalculationConfig);
        var formDataWrapper = await dataAccessor.GetFormDataWrapper(dataElement);

        foreach (var (baseField, calculation) in dataModelFieldCalculations)
        {
            var resolvedFields = await evaluatorState.GetResolvedKeys(
                new DataReference() { Field = baseField, DataElementIdentifier = dataElementIdentifier },
                true
            );
            foreach (var resolvedField in resolvedFields)
            {
                if (
                    hiddenFields.Exists(d =>
                        d.DataElementIdentifier == resolvedField.DataElementIdentifier
                        && resolvedField.Field.StartsWith(d.Field, StringComparison.InvariantCulture)
                    )
                )
                {
                    continue;
                }

                var context = new ComponentContext(
                    evaluatorState,
                    component: null,
                    rowIndices: ExpressionHelper.GetRowIndices(resolvedField.Field),
                    dataElementIdentifier: resolvedField.DataElementIdentifier
                );
                var positionalArguments = new object[] { resolvedField.Field };

                await RunCalculation(
                    formDataWrapper,
                    evaluatorState,
                    resolvedField,
                    context,
                    positionalArguments,
                    calculation
                );
            }
        }
    }

    private async Task RunCalculation(
        IFormDataWrapper formDataWrapper,
        LayoutEvaluatorState evaluatorState,
        DataReference resolvedField,
        ComponentContext context,
        object[] positionalArguments,
        DataModelFieldCalculation calculation
    )
    {
        try
        {
            var calculationResult = await ExpressionEvaluator.EvaluateExpressionToExpressionValue(
                evaluatorState,
                calculation.Expression,
                context,
                positionalArguments
            );
            if (!formDataWrapper.Set(resolvedField.Field, calculationResult))
            {
                _logger.LogWarning(
                    "Could not set calculated value for field {Field} in data element {DataElementId}. "
                        + "This is because the type conversion failed.",
                    resolvedField.Field,
                    resolvedField.DataElementIdentifier.Id
                );
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while evaluating calculation for field {Field}", resolvedField.Field);
            throw;
        }
    }

    private Dictionary<string, DataModelFieldCalculation> ParseDataModelFieldCalculationConfig(
        string rawCalculationConfig
    )
    {
        using var calculationConfigDocument = JsonDocument.Parse(rawCalculationConfig);

        var dataModelFieldCalculations = new Dictionary<string, DataModelFieldCalculation>();
        var hasCalculations = calculationConfigDocument.RootElement.TryGetProperty(
            "calculations",
            out JsonElement calculationsObject
        );
        if (hasCalculations)
        {
            foreach (var calculationArray in calculationsObject.EnumerateObject())
            {
                var field = calculationArray.Name;
                var calculation = calculationArray.Value;
                var resolvedDataModelFieldCalculation = ResolveDataModelFieldCalculation(field, calculation);
                if (resolvedDataModelFieldCalculation == null)
                {
                    _logger.LogError("Calculation for field {Field} could not be resolved", field);
                    continue;
                }
                dataModelFieldCalculations[field] = resolvedDataModelFieldCalculation;
            }
        }
        return dataModelFieldCalculations;
    }

    private DataModelFieldCalculation? ResolveDataModelFieldCalculation(string field, JsonElement definition)
    {
        var dataModelFieldCalculationDefinition = definition.Deserialize<RawDataModelFieldCalculation>(
            _jsonSerializerOptions
        );
        if (dataModelFieldCalculationDefinition == null)
        {
            _logger.LogError("Calculation for field {Field} could not be parsed", field);
            return null;
        }

        if (dataModelFieldCalculationDefinition.Expression == null)
        {
            _logger.LogError("Calculation for field {Field} is missing condition", field);
            return null;
        }

        var dataModelFieldCalculation = new DataModelFieldCalculation
        {
            Expression = dataModelFieldCalculationDefinition.Expression.Value,
        };

        return dataModelFieldCalculation;
    }
}
