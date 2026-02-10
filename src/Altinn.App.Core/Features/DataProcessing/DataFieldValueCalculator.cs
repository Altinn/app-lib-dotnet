using System.Text.Json;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Expressions;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Layout;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ComponentContext = Altinn.App.Core.Models.Expressions.ComponentContext;

namespace Altinn.App.Core.Features.DataProcessing;

public class DataFieldValueCalculator
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

    public DataFieldValueCalculator(
        ILogger<DataFieldValueCalculator> logger,
        ILayoutEvaluatorStateInitializer layoutEvaluatorStateInitializer,
        IAppResources appResourceService,
        IServiceProvider serviceProvider
    )
    {
        _logger = logger;
        _appResourceService = appResourceService;
        _layoutEvaluatorStateInitializer = layoutEvaluatorStateInitializer;
        _dataElementAccessChecker = serviceProvider.GetRequiredService<IDataElementAccessChecker>();
    }

    public async Task Calculate(IInstanceDataAccessor dataAccessor, string taskId)
    {
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

        foreach (var (baseField, calculations) in dataFieldCalculations)
        {
            var resolvedFields = await evaluatorState.GetResolvedKeys(
                new DataReference() { Field = baseField, DataElementIdentifier = dataElementIdentifier }
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
                    var formDataWrapper = await dataAccessor.GetFormDataWrapper(dataElement);
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
            formDataWrapper.Set(resolvedField.Field.ToArray(), calculationResult);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while evaluating calculation for field {field}", resolvedField.Field);
            throw;
        }
    }

    private Dictionary<string, List<DataFieldCalculation>> ParseDataFieldCalculationConfig(
        string rawCalculationConfig,
        ILogger<DataFieldValueCalculator> logger
    )
    {
        using var calculationConfigDocument = JsonDocument.Parse(rawCalculationConfig);
        var calculationDefinitions = new Dictionary<string, RawDataFieldValueCalculation>();
        var hasDefinitions = calculationConfigDocument.RootElement.TryGetProperty(
            "definitions",
            out JsonElement definitionsObject
        );
        if (hasDefinitions)
        {
            foreach (var definitionProperty in definitionsObject.EnumerateObject())
            {
                var resolvedDefinition = ResolveCalculationDefinition(
                    definitionProperty,
                    calculationDefinitions,
                    logger
                );
                if (resolvedDefinition == null)
                {
                    logger.LogError("Calculation definition {name} could not be resolved", definitionProperty.Name);
                    continue;
                }
                calculationDefinitions[definitionProperty.Name] = resolvedDefinition;
            }
        }

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
                    var resolvedDataFieldCalculation = ResolveDataFieldCalculation(
                        field,
                        calculation,
                        calculationDefinitions,
                        logger
                    );
                    if (resolvedDataFieldCalculation == null)
                    {
                        logger.LogError("Calculation for field {field} could not be resolved", field);
                        continue;
                    }
                    dataFieldCalculation.Add(resolvedDataFieldCalculation);
                }
            }
        }
        return dataFieldCalculations;
    }

    private DataFieldCalculation? ResolveDataFieldCalculation(
        string field,
        JsonElement definition,
        Dictionary<string, RawDataFieldValueCalculation> resolvedDefinitions,
        ILogger logger
    )
    {
        var rawDataFieldValueCalculation = new RawDataFieldValueCalculation();

        if (definition.ValueKind == JsonValueKind.String)
        {
            var stringReference = definition.GetString();
            if (stringReference == null)
            {
                logger.LogError("Could not resolve null reference for calculation for field {field}", field);
                return null;
            }

            var reference = resolvedDefinitions.GetValueOrDefault(stringReference);
            if (reference == null)
            {
                logger.LogError(
                    "Could not resolve reference {stringReference} for calculation for field {field}",
                    stringReference,
                    field
                );
                return null;
            }
            rawDataFieldValueCalculation.Condition = reference.Condition;
        }
        else
        {
            var dataFieldCalculationDefinition = definition.Deserialize<RawDataFieldValueCalculation>(
                _jsonSerializerOptions
            );
            if (dataFieldCalculationDefinition == null)
            {
                logger.LogError("Calculation for field {field} could not be parsed", field);
                return null;
            }

            if (dataFieldCalculationDefinition.Ref != null)
            {
                var reference = resolvedDefinitions.GetValueOrDefault(dataFieldCalculationDefinition.Ref);
                if (reference == null)
                {
                    logger.LogError(
                        "Could not resolve reference {expressionDefinitionRef} for calculation for field {field}",
                        dataFieldCalculationDefinition.Ref,
                        field
                    );
                    return null;
                }
                rawDataFieldValueCalculation.Condition = reference.Condition;
            }

            if (dataFieldCalculationDefinition.Condition != null)
            {
                rawDataFieldValueCalculation.Condition = dataFieldCalculationDefinition.Condition;
            }
        }

        if (rawDataFieldValueCalculation.Condition == null)
        {
            logger.LogError("Calculation for field {field} is missing condition", field);
            return null;
        }

        var dataFieldCalculation = new DataFieldCalculation
        {
            Condition = rawDataFieldValueCalculation.Condition.Value,
        };

        return dataFieldCalculation;
    }

    private static RawDataFieldValueCalculation? ResolveCalculationDefinition(
        JsonProperty definitionProperty,
        Dictionary<string, RawDataFieldValueCalculation> resolvedDefinitions,
        ILogger logger
    )
    {
        var resolvedDefinition = new RawDataFieldValueCalculation();
        var rawDefinition = definitionProperty.Value.Deserialize<RawDataFieldValueCalculation>(_jsonSerializerOptions);
        if (rawDefinition == null)
        {
            logger.LogError("Calculation definition {name} could not be parsed", definitionProperty.Name);
            return null;
        }

        if (rawDefinition.Ref != null)
        {
            var reference = resolvedDefinitions.GetValueOrDefault(rawDefinition.Ref);
            if (reference == null)
            {
                logger.LogError(
                    "Could not resolve reference {rawDefinitionRef} for calculation {name}",
                    rawDefinition.Ref,
                    definitionProperty.Name
                );
                return null;
            }

            resolvedDefinition.Condition = reference.Condition;
        }

        if (rawDefinition.Condition != null)
        {
            resolvedDefinition.Condition = rawDefinition.Condition;
        }

        if (resolvedDefinition.Condition == null)
        {
            logger.LogError("Calculation {name} is missing condition", definitionProperty.Name);
            return null;
        }

        return resolvedDefinition;
    }
}
