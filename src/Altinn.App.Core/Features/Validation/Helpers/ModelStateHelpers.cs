using System.Collections;
using System.Text.Json.Serialization;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Altinn.App.Core.Features.Validation.Helpers;

public static class ModelStateHelpers
{
    public static List<ValidationIssue> ModelStateToIssueList(ModelStateDictionary modelState, Instance instance,
        DataElement dataElement, GeneralSettings generalSettings, Type objectType, string source)
    {
        var validationIssues = new List<ValidationIssue>();

        foreach (var modelKey in modelState.Keys)
        {
            modelState.TryGetValue(modelKey, out var entry);

            if (entry is { ValidationState: ModelValidationState.Invalid })
            {
                foreach (var error in entry.Errors)
                {
                    var severityAndMessage = GetSeverityFromMessage(error.ErrorMessage, generalSettings);
                    validationIssues.Add(new ValidationIssue
                    {
                        InstanceId = instance.Id,
                        DataElementId = dataElement.Id,
                        Source = source,
                        Code = severityAndMessage.Message,
                        Field = ModelKeyToField(modelKey, objectType)!,
                        Severity = severityAndMessage.Severity,
                        Description = severityAndMessage.Message
                    });
                }
            }
        }

        return validationIssues;
    }

    private static (ValidationIssueSeverity Severity, string Message) GetSeverityFromMessage(string originalMessage,
        GeneralSettings generalSettings)
    {
        if (originalMessage.StartsWith(generalSettings.SoftValidationPrefix))
        {
            return (ValidationIssueSeverity.Warning,
                originalMessage.Remove(0, generalSettings.SoftValidationPrefix.Length));
        }

        if (generalSettings.FixedValidationPrefix != null
            && originalMessage.StartsWith(generalSettings.FixedValidationPrefix))
        {
            return (ValidationIssueSeverity.Fixed,
                originalMessage.Remove(0, generalSettings.FixedValidationPrefix.Length));
        }

        if (originalMessage.StartsWith(generalSettings.InfoValidationPrefix))
        {
            return (ValidationIssueSeverity.Informational,
                originalMessage.Remove(0, generalSettings.InfoValidationPrefix.Length));
        }

        if (originalMessage.StartsWith(generalSettings.SuccessValidationPrefix))
        {
            return (ValidationIssueSeverity.Success,
                originalMessage.Remove(0, generalSettings.SuccessValidationPrefix.Length));
        }

        return (ValidationIssueSeverity.Error, originalMessage);
    }

    /// <summary>
    ///     Translate the ModelKey from validation to a field that respects [JsonPropertyName] annotations
    /// </summary>
    /// <remarks>
    ///     Will be obsolete when updating to net70 or higher and activating
    ///     https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-7.0#use-json-property-names-in-validation-errors
    /// </remarks>
    public static string? ModelKeyToField(string? modelKey, Type data)
    {
        var keyParts = modelKey?.Split('.', 2);
        var keyWithIndex = keyParts?.ElementAtOrDefault(0)?.Split('[', 2);
        var key = keyWithIndex?.ElementAtOrDefault(0);
        var index = keyWithIndex?.ElementAtOrDefault(1); // with traling ']', eg: "3]"
        var rest = keyParts?.ElementAtOrDefault(1);

        var property = data?.GetProperties()?.FirstOrDefault(p => p.Name == key);
        var jsonPropertyName = property
            ?.GetCustomAttributes(true)
            .OfType<JsonPropertyNameAttribute>()
            .FirstOrDefault()
            ?.Name;
        if (jsonPropertyName is null)
        {
            jsonPropertyName = key;
        }

        if (index is not null)
        {
            jsonPropertyName = jsonPropertyName + '[' + index;
        }

        if (rest is null)
        {
            return jsonPropertyName;
        }

        var childType = property?.PropertyType;

        // Get the Parameter of IEnumerable properties, if they are not string
        if (childType is not null && childType != typeof(string) && childType.IsAssignableTo(typeof(IEnumerable)))
        {
            childType = childType.GetInterfaces()
                .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                .Select(t => t.GetGenericArguments()[0]).FirstOrDefault();
        }

        if (childType is null)
        {
            // Give up and return rest, if the child type is not found.
            return $"{jsonPropertyName}.{rest}";
        }

        return $"{jsonPropertyName}.{ModelKeyToField(rest, childType)}";
    }

    public static List<ValidationIssue> MapModelStateToIssueList(ModelStateDictionary modelState, Instance instance,
        GeneralSettings generalSettings)
    {
        var validationIssues = new List<ValidationIssue>();

        foreach (var modelKey in modelState.Keys)
        {
            modelState.TryGetValue(modelKey, out var entry);

            if (entry != null && entry.ValidationState == ModelValidationState.Invalid)
            {
                foreach (var error in entry.Errors)
                {
                    var severityAndMessage = GetSeverityFromMessage(error.ErrorMessage, generalSettings);
                    validationIssues.Add(new ValidationIssue
                    {
                        InstanceId = instance.Id,
                        Code = severityAndMessage.Message,
                        Severity = severityAndMessage.Severity,
                        Description = severityAndMessage.Message
                    });
                }
            }
        }

        return validationIssues;
    }
}