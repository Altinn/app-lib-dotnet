using Altinn.App.Core.Helpers.DataModel;
using Altinn.App.Core.Interface;
using Altinn.App.Core.Internal.Expressions;
using Altinn.App.Core.Models.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;


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

            var validationConfig = JObject.Parse(rawValidationConfig);
            if (validationConfig == null)
            {
                throw new ArgumentException($"Could not parse validation configuration for data type {dataType}");
            }

            var expressionValidations = ParseExpressionValidationConfig(validationConfig, logger);

            foreach (var validationObject in expressionValidations)
            {
                var baseField = validationObject.Key;
                var resolvedFields = dataModel.GetResolvedKeys(baseField);
                var validations = validationObject.Value;
                foreach (var resolvedField in resolvedFields)
                {
                    foreach (var validation in validations)
                    {
                        var isInvalid = ExpressionEvaluator.EvaluateExpression(evaluatorState, validation.Condition, null);
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

        private static RawExpressionValidation? ResolveValidationDefinition(string name, JObject definition, Dictionary<string, RawExpressionValidation> resolvedDefinitions, ILogger logger)
        {
            var resolvedDefinition = new RawExpressionValidation();
            var rawDefinition = definition.ToObject<RawExpressionValidation>();
            if (rawDefinition == null)
            {
                logger.LogWarning($"Validation definition {name} could not be parsed");
                return null;
            }
            if (rawDefinition.Ref != null)
            {
                RawExpressionValidation reference = resolvedDefinitions[rawDefinition.Ref];
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

        private static ExpressionValidation? ResolveExpressionValidation(string field, JObject definition, Dictionary<string, RawExpressionValidation> resolvedDefinitions, ILogger logger)
        {

            var rawExpressionValidatıon = new RawExpressionValidation();

            var stringReference = definition.ToString();
            if (stringReference != null)
            {
                var reference = resolvedDefinitions[stringReference];
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
                var expressionDefinition = definition.ToObject<RawExpressionValidation>();
                if (expressionDefinition == null)
                {
                    logger.LogWarning($"Validation for field {field} could not be parsed");
                    return null;
                }

                if (expressionDefinition.Ref != null)
                {
                    RawExpressionValidation reference = resolvedDefinitions[expressionDefinition.Ref];
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

        private static Dictionary<string, ExpressionValidation[]> ParseExpressionValidationConfig(JObject expressionValidationConfig, ILogger logger)
        {
            var expressionValidationDefinitions = new Dictionary<string, RawExpressionValidation>();
            var definitionsObject = expressionValidationConfig["defintions"]?.ToObject<JObject>();
            if (definitionsObject != null)
            {
                foreach (var definitionObject in definitionsObject)
                {
                    var name = definitionObject.Key;
                    var definition = definitionObject.Value?.ToObject<JObject>();
                    if (definition == null)
                    {
                        logger.LogWarning($"Validation definition {name} is not an object");
                        continue;
                    }
                    var resolvedDefinition = ResolveValidationDefinition(name, definition, expressionValidationDefinitions, logger);
                    if (resolvedDefinition == null)
                    {
                        logger.LogWarning($"Validation definition {name} could not be resolved");
                        continue;
                    }
                    expressionValidationDefinitions[name] = resolvedDefinition;
                }
            }
            var expressionValidations = new Dictionary<string, ExpressionValidation[]>();
            var validationsObject = expressionValidationConfig["validations"]?.ToObject<JObject>();
            if (validationsObject != null)
            {
                foreach (var validationArray in validationsObject)
                {
                    var field = validationArray.Key;
                    var validations = validationArray.Value?.ToObject<JObject[]>();
                    if (validations == null)
                    {
                        logger.LogWarning($"Validation for field {field} is not an array");
                        continue;
                    }
                    foreach (var validation in validations)
                    {
                        if (!expressionValidations.ContainsKey(field))
                        {
                            expressionValidations[field] = new ExpressionValidation[0];
                        }
                        var resolvedExpressionValidation = ResolveExpressionValidation(field, validation, expressionValidationDefinitions, logger);
                        if (resolvedExpressionValidation == null)
                        {
                            logger.LogWarning($"Validation for field {field} could not be resolved");
                            continue;
                        }
                        expressionValidations[field].Append(resolvedExpressionValidation);
                    }
                }
            }
            return expressionValidations;
        }
    }
}
