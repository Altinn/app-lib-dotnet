using System.Diagnostics;
using System.Linq.Expressions;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Validation;

/// <summary>
/// Simple wrapper for validation of form data that does the type checking for you.
/// </summary>
/// <typeparam name="TModel">The type of the model this class will validate</typeparam>
public abstract class GenericFormDataValidator<TModel> : IFormDataValidator
{
    /// <summary>
    /// Constructor to force the DataType to be set.
    /// </summary>
    /// <param name="dataType"></param>
    protected GenericFormDataValidator(string dataType)
    {
        DataType = dataType;
    }
    /// <inheritdoc />
    public string DataType { get; private init; }

    private readonly List<string> _runForPrefixes = new List<string>();
    // ReSharper disable once StaticMemberInGenericType
    private static readonly AsyncLocal<List<ValidationIssue>> ValidationIssues = new();

    /// <summary>
    /// Default implementation that respects the runFor prefixes.
    /// </summary>
    public bool ShouldRunForIncrementalValidation(List<string>? changedFields = null)
    {
        if (changedFields == null)
        {
            return true;
        }

        if (_runForPrefixes.Count == 0)
        {
            return true;
        }

        foreach (var prefix in _runForPrefixes)
        {
            foreach (var changedField in changedFields)
            {
                if (IsMatch(changedField, prefix))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsMatch(string changedField, string prefix)
    {
        return changedField.StartsWith(prefix) || prefix.StartsWith(changedField);
    }

    /// <summary>
    /// Easy way to configure <see cref="ShouldRunForIncrementalValidation"/> to only run for fields that start with the given prefix.
    /// </summary>
    /// <param name="selector">A selector that will be translated into a prefix</param>
    /// <typeparam name="T1">The type of the selected element (only for making the compiler happy)</typeparam>
    protected void RunFor<T1>(Expression<Func<TModel, T1>> selector)
    {
        _runForPrefixes.Add(LinqExpressionHelpers.GetJsonPath(selector));
    }

    /// <summary>
    /// Convenience method to create a validation issue for a field using a linq expression instead of a key
    /// </summary>
    protected void CreateValidationIssue<T>(Expression<Func<TModel,T>> selector, string textKey, ValidationIssueSeverity severity = ValidationIssueSeverity.Error)
    {
        Debug.Assert(ValidationIssues.Value is not null);
        AddValidationIssue(new ValidationIssue
        {
            Field = LinqExpressionHelpers.GetJsonPath(selector),
            CustomTextKey = textKey,
            Severity = severity
        });
    }

    /// <summary>
    /// Allows inheriting classes to add validation issues.
    /// </summary>
    protected void AddValidationIssue(ValidationIssue issue)
    {
        Debug.Assert(ValidationIssues.Value is not null);
        ValidationIssues.Value.Add(issue);
    }

    /// <summary>
    /// Implementation of the generic <see cref="IFormDataValidator"/> interface to call the correctly typed
    /// validation method implemented by the inheriting class.
    /// </summary>
    public async Task<List<ValidationIssue>> ValidateFormData(Instance instance, DataElement dataElement, object data)
    {
        if (data is not TModel model)
        {
            throw new ArgumentException($"Data is not of type {typeof(TModel)}");
        }

        ValidationIssues.Value = new List<ValidationIssue>();
        await ValidateFormData(instance, dataElement, model);
        return ValidationIssues.Value;

    }

    /// <summary>
    /// Implement this method to validate the data.
    /// </summary>
    protected abstract Task ValidateFormData(Instance instance, DataElement dataElement, TModel data);
}