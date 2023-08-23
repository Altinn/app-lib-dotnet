using System.Text.Json;
using Altinn.App.Core.Helpers.DataModel;
using Altinn.App.Core.Interface;
using Altinn.App.Core.Internal.Expressions;
using Altinn.App.Core.Models.Validation;
using Microsoft.Extensions.Logging;


namespace Altinn.App.Core.Features.Validation
{
    /// <summary>
    /// Validates form data against expression validations
    /// </summary>
    public static class ExpressionValidator
    {
        /// <inheritdoc />
        public static IEnumerable<ValidationIssue> Validate(string dataType, IAppResources appResourceService, object data, LayoutEvaluatorState evaluatorState, ILogger logger)
        {
            var dataModel = new DataModel(data);
            var validationIssues = new List<ValidationIssue>();

            var rawValidationConfig = appResourceService.GetValidationConfiguration(dataType);
            if (rawValidationConfig == null)
            {
                // No validation configuration exists for this data type
                return validationIssues;
            }

            var validationConfig = JsonDocument.Parse(rawValidationConfig).RootElement;
            var expressionValidations = ParseExpressionValidationConfig(validationConfig, logger);

            foreach (var validationObject in expressionValidations)
            {
                var baseField = validationObject.Key;
                var resolvedFields = dataModel.GetResolvedKeys(baseField);
                var validations = validationObject.Value;
                foreach (var resolvedField in resolvedFields)
                {
                    var positionalArguments = new[] { resolvedField };
                    foreach (var validation in validations)
                    {
                        var isInvalid = ExpressionEvaluator.EvaluateExpression(evaluatorState, validation.Condition, null, positionalArguments);
                        if (isInvalid is not bool)
                        {
                            throw new ArgumentException($"Validation condition for {resolvedField} did not evaluate to a boolean");
                        }
                        if ((bool)isInvalid)
                        {
                            var validationIssue = new ValidationIssue
                            {
                                Field = resolvedField,
                                Severity = validation.Severity,
                                CustomTextKey = validation.Message,
                                Code = validation.Message,
                                Source = "Expression" // TODO: Add source to ValidationIssueSources
                            };
                            validationIssues.Add(validationIssue);
                        }
                    }
                }
            }


            return validationIssues;
        }

        private static ValidationIssueSeverity? MapSeverity(string? severity)
        {
            switch (severity)
            {
                case "errors":
                    return ValidationIssueSeverity.Error;
                case "warnings":
                    return ValidationIssueSeverity.Warning;
                case "info":
                    return ValidationIssueSeverity.Informational;
                case "success":
                    return ValidationIssueSeverity.Success;
                default:
                    return null;
            }
        }

        private static RawExpressionValidation? ResolveValidationDefinition(string name, JsonElement definition, Dictionary<string, RawExpressionValidation> resolvedDefinitions, ILogger logger)
        {
            var resolvedDefinition = new RawExpressionValidation();
            var rawDefinition = definition.Deserialize<RawExpressionValidation>();
            if (rawDefinition == null)
            {
                logger.LogWarning($"Validation definition {name} could not be parsed");
                return null;
            }
            if (rawDefinition.Ref != null)
            {
                var reference = resolvedDefinitions.GetValueOrDefault(rawDefinition.Ref);
                if (reference == null)
                {
                    logger.LogWarning($"Could not resolve reference {rawDefinition.Ref} for validation {name}");
                    return null;

                }
                resolvedDefinition.Message = reference.Message;
                resolvedDefinition.Condition = reference.Condition;
                resolvedDefinition.Severity = reference.Severity;
            }

            if (rawDefinition.Message != null)
            {
                resolvedDefinition.Message = rawDefinition.Message;
            }

            if (rawDefinition.Condition != null)
            {
                resolvedDefinition.Condition = rawDefinition.Condition;
            }

            if (rawDefinition.Severity != null)
            {
                resolvedDefinition.Severity = rawDefinition.Severity;
            }

            if (resolvedDefinition.Message == null)
            {
                logger.LogWarning($"Validation {name} is missing message");
                return null;
            }

            if (resolvedDefinition.Condition == null)
            {
                logger.LogWarning($"Validation {name} is missing condition");
                return null;
            }

            return resolvedDefinition;
        }

        private static ExpressionValidation? ResolveExpressionValidation(string field, JsonElement definition, Dictionary<string, RawExpressionValidation> resolvedDefinitions, ILogger logger)
        {

            var rawExpressionValidatıon = new RawExpressionValidation();

            if (definition.ValueKind == JsonValueKind.String)
            {
                var stringReference = definition.GetString();
                var reference = resolvedDefinitions.GetValueOrDefault(stringReference);
                if (reference == null)
                {
                    logger.LogWarning($"Could not resolve reference {stringReference} for validation for field {field}");
                    return null;
                }
                rawExpressionValidatıon.Message = reference.Message;
                rawExpressionValidatıon.Condition = reference.Condition;
                rawExpressionValidatıon.Severity = reference.Severity;
            }
            else
            {
                var expressionDefinition = definition.Deserialize<RawExpressionValidation>();
                if (expressionDefinition == null)
                {
                    logger.LogWarning($"Validation for field {field} could not be parsed");
                    return null;
                }

                if (expressionDefinition.Ref != null)
                {
                    var reference = resolvedDefinitions.GetValueOrDefault(expressionDefinition.Ref);
                    if (reference == null)
                    {
                        logger.LogWarning($"Could not resolve reference {expressionDefinition.Ref} for validation for field {field}");
                        return null;

                    }
                    rawExpressionValidatıon.Message = reference.Message;
                    rawExpressionValidatıon.Condition = reference.Condition;
                    rawExpressionValidatıon.Severity = reference.Severity;
                }

                if (expressionDefinition.Message != null)
                {
                    rawExpressionValidatıon.Message = expressionDefinition.Message;
                }

                if (expressionDefinition.Condition != null)
                {
                    rawExpressionValidatıon.Condition = expressionDefinition.Condition;
                }

                if (expressionDefinition.Severity != null)
                {
                    rawExpressionValidatıon.Severity = expressionDefinition.Severity;
                }
            }

            if (rawExpressionValidatıon.Message == null)
            {
                logger.LogWarning($"Validation for field {field} is missing message");
                return null;
            }

            if (rawExpressionValidatıon.Condition == null)
            {
                logger.LogWarning($"Validation for field {field} is missing condition");
                return null;
            }
            if (rawExpressionValidatıon.Severity == null || MapSeverity(rawExpressionValidatıon.Severity) == null)
            {
                rawExpressionValidatıon.Severity = "errors";
            }

            var expressionValidation = new ExpressionValidation
            {
                Message = rawExpressionValidatıon.Message,
                Condition = rawExpressionValidatıon.Condition,
                Severity = (ValidationIssueSeverity)MapSeverity(rawExpressionValidatıon.Severity)
            };

            return expressionValidation;
        }

        private static Dictionary<string, List<ExpressionValidation>> ParseExpressionValidationConfig(JsonElement expressionValidationConfig, ILogger logger)
        {
            var expressionValidationDefinitions = new Dictionary<string, RawExpressionValidation>();
            JsonElement definitionsObject;
            var hasDefinitions = expressionValidationConfig.TryGetProperty("definitions", out definitionsObject);
            if (hasDefinitions)
            {
                foreach (var definitionObject in definitionsObject.EnumerateObject())
                {
                    var name = definitionObject.Name;
                    var definition = definitionObject.Value;
                    var resolvedDefinition = ResolveValidationDefinition(name, definition, expressionValidationDefinitions, logger);
                    if (resolvedDefinition == null)
                    {
                        logger.LogWarning($"Validation definition {name} could not be resolved");
                        continue;
                    }
                    expressionValidationDefinitions[name] = resolvedDefinition;
                }
            }
            var expressionValidations = new Dictionary<string, List<ExpressionValidation>>();
            JsonElement validationsObject;
            var hasValidations = expressionValidationConfig.TryGetProperty("validations", out validationsObject);
            if (hasValidations)
            {
                foreach (var validationArray in validationsObject.EnumerateObject())
                {
                    var field = validationArray.Name;
                    var validations = validationArray.Value;
                    foreach (var validation in validations.EnumerateArray())
                    {
                        if (!expressionValidations.ContainsKey(field))
                        {
                            expressionValidations[field] = new List<ExpressionValidation>();
                        }
                        var resolvedExpressionValidation = ResolveExpressionValidation(field, validation, expressionValidationDefinitions, logger);
                        if (resolvedExpressionValidation == null)
                        {
                            logger.LogWarning($"Validation for field {field} could not be resolved");
                            continue;
                        }
                        expressionValidations[field].Add(resolvedExpressionValidation);
                    }
                }
            }
            return expressionValidations;
        }
    }
}
