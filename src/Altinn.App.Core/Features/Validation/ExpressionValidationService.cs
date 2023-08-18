using Altinn.App.Core.Models.Expressions;
using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;


namespace Altinn.App.Core.Features.Validation
{
    /// <summary>
    /// Validates form data against expression validations
    /// </summary>
    public class ExpressionValidationService : IExpressionValidationService
    {
        /// <inheritdoc />
        public Task<(bool Success, List<ValidationIssue> Errors)> Validate(DataType dataType)
        {
            throw new NotImplementedException();
        }

        private ValidationIssueSeverity? MapSeverity(string? severity)
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

        private RawExpressionValidation? ResolveValidationDefinition(string name, JObject definition, Dictionary<string, RawExpressionValidation> resolvedDefinitions, ILogger logger)
        {
            var resolvedDefinition = new RawExpressionValidation();
            var referenceName = definition["ref"]?.ToString();
            if (referenceName != null)
            {
                RawExpressionValidation reference = resolvedDefinitions[referenceName];
                if (reference == null)
                {
                    logger.LogWarning($"Could not resolve reference {referenceName} for validation {name}");
                    return null;

                }
                resolvedDefinition.Message = reference.Message;
                resolvedDefinition.Condition = reference.Condition;
                resolvedDefinition.Severity = reference.Severity;
            }

            var message = definition["message"]?.ToString();
            if (message != null)
            {
                resolvedDefinition.Message = message;
            }

            var condition = definition["condition"]?.ToObject<Expression>();
            if (condition != null)
            {
                resolvedDefinition.Condition = condition;
            }

            var severity = MapSeverity(definition["severity"]?.ToString());
            if (severity != null)
            {
                resolvedDefinition.Severity = severity;
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

        private ExpressionValidation? ResolveExpressionValidation(string field, JObject definition, Dictionary<string, RawExpressionValidation> resolvedDefinitions, ILogger logger)
        {
            var expressionValidation = new ExpressionValidation();

            var stringReference = definition.ToString();
            if (stringReference != null)
            {
                var reference = resolvedDefinitions[stringReference];
                if (reference == null)
                {
                    logger.LogWarning($"Could not resolve reference {stringReference} for validation for field {field}");
                    return null;
                }
                expressionValidation.Message = reference.Message;
                expressionValidation.Condition = reference.Condition;
                expressionValidation.Severity = reference.Severity ?? ValidationIssueSeverity.Error;
            }
            else
            {
                var referenceName = definition["ref"]?.ToString();
                if (referenceName != null)
                {
                    RawExpressionValidation reference = resolvedDefinitions[referenceName];
                    if (reference == null)
                    {
                        logger.LogWarning($"Could not resolve reference {referenceName} for validation for field {field}");
                        return null;

                    }
                    expressionValidation.Message = reference.Message;
                    expressionValidation.Condition = reference.Condition;
                    if (reference.Severity != null)
                    {
                        expressionValidation.Severity = (ValidationIssueSeverity)reference.Severity;
                    }
                }

                var message = definition["message"]?.ToString();
                if (message != null)
                {
                    expressionValidation.Message = message;
                }

                var condition = definition["condition"]?.ToObject<Expression>();
                if (condition != null)
                {
                    expressionValidation.Condition = condition;
                }

                var severity = MapSeverity(definition["severity"]?.ToString());
                if (severity != null)
                {
                    expressionValidation.Severity = (ValidationIssueSeverity)severity;
                }
            }

            if (expressionValidation.Message == null)
            {
                logger.LogWarning($"Validation for field {field} is missing message");
                return null;
            }

            if (expressionValidation.Condition == null)
            {
                logger.LogWarning($"Validation for field {field} is missing condition");
                return null;
            }
            if (expressionValidation.Severity == null)
            {
                expressionValidation.Severity = ValidationIssueSeverity.Error;
            }

            return expressionValidation;
        }

        private Dictionary<string, ExpressionValidation[]> ParseExpressionValidationConfig(JObject expressionValidationConfig, ILogger logger)
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
