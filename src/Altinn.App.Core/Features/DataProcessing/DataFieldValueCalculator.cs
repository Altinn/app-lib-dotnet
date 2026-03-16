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

internal sealed class DataFieldValueCalculator
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ILogger<DataFieldValueCalculator> _logger;
    private readonly IAppResources _appResourceService;
    private readonly ILayoutEvaluatorStateInitializer _layoutEvaluatorStateInitializer;
    private readonly IDataElementAccessChecker _dataElementAccessChecker;
    private readonly Telemetry? _telemetry;

    public DataFieldValueCalculator(
        ILogger<DataFieldValueCalculator> logger,
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
        var dataFieldCalculations = ParseDataFieldCalculationConfig(rawCalculationConfig, _logger);
        var formDataWrapper = await dataAccessor.GetFormDataWrapper(dataElement);

        foreach (var (baseField, calculations) in dataFieldCalculations)
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
                foreach (var calculation in calculations)
                {
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
    }

    private async Task RunCalculation(
        IFormDataWrapper formDataWrapper,
        LayoutEvaluatorState evaluatorState,
        DataReference resolvedField,
        ComponentContext context,
        object[] positionalArguments,
        DataFieldCalculation calculation
    )
    {
        try
        {
            var calculationResult = await ExpressionEvaluator.EvaluateExpressionToExpressionValue(
                evaluatorState,
                calculation.Condition,
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

    private Dictionary<string, List<DataFieldCalculation>> ParseDataFieldCalculationConfig(
        string rawCalculationConfig,
        ILogger<DataFieldValueCalculator> logger
    )
    {
        using var calculationConfigDocument = JsonDocument.Parse(rawCalculationConfig);

        var dataFieldCalculations = new Dictionary<string, List<DataFieldCalculation>>();
        var hasCalculations = calculationConfigDocument.RootElement.TryGetProperty(
            "calculations",
            out JsonElement calculationsObject
        );
        if (hasCalculations)
        {
            foreach (var calculationArray in calculationsObject.EnumerateObject())
            {
                var field = calculationArray.Name;
                var calculations = calculationArray.Value;
                foreach (var calculation in calculations.EnumerateArray())
                {
                    if (!dataFieldCalculations.TryGetValue(field, out var dataFieldCalculation))
                    {
                        dataFieldCalculation = new List<DataFieldCalculation>();
                        dataFieldCalculations[field] = dataFieldCalculation;
                    }
                    var resolvedDataFieldCalculation = ResolveDataFieldCalculation(field, calculation, logger);
                    if (resolvedDataFieldCalculation == null)
                    {
                        logger.LogError("Calculation for field {Field} could not be resolved", field);
                        continue;
                    }
                    dataFieldCalculation.Add(resolvedDataFieldCalculation);
                }
            }
        }
        return dataFieldCalculations;
    }

    private static DataFieldCalculation? ResolveDataFieldCalculation(
        string field,
        JsonElement definition,
        ILogger logger
    )
    {
        var rawDataFieldValueCalculation = new RawDataFieldValueCalculation();

        if (definition.ValueKind == JsonValueKind.String)
        {
            var stringReference = definition.GetString();
            if (stringReference == null)
            {
                logger.LogError("Could not resolve null reference for calculation for field {Field}", field);
                return null;
            }
        }
        else
        {
            var dataFieldCalculationDefinition = definition.Deserialize<RawDataFieldValueCalculation>(
                _jsonSerializerOptions
            );
            if (dataFieldCalculationDefinition == null)
            {
                logger.LogError("Calculation for field {Field} could not be parsed", field);
                return null;
            }

            if (dataFieldCalculationDefinition.Condition != null)
            {
                rawDataFieldValueCalculation.Condition = dataFieldCalculationDefinition.Condition;
            }
        }

        if (rawDataFieldValueCalculation.Condition == null)
        {
            logger.LogError("Calculation for field {Field} is missing condition", field);
            return null;
        }

        var dataFieldCalculation = new DataFieldCalculation
        {
            Condition = rawDataFieldValueCalculation.Condition.Value,
        };

        return dataFieldCalculation;
    }
}
