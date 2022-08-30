using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Interface;

/// <summary>
/// IInstantiation defines the methods that must be implemented by a class that handles custom logic during instantiation of a new instance.
/// </summary>
public interface IInstantiation
{
    /// <summary>
    /// Run validations related to instantiation
    /// </summary>
    /// <example>
    /// if ([some condition])
    /// {
    ///     return new ValidationResult("[error message]");
    /// }
    /// return null;
    /// </example>
    /// <param name="instance">The instance being validated</param>
    /// <returns>The validation result object (null if no errors) </returns>
    public Task<InstantiationValidationResult> Validation(Instance instance);
    
    /// <summary>
    /// Run events related to instantiation
    /// </summary>
    /// <remarks>
    /// For example custom prefill.
    /// </remarks>
    /// <param name="instance">Instance information</param>
    /// <param name="data">The data object created</param>
    /// <param name="prefill">External prefill available under instansiation if supplied</param>
    public Task DataCreation(Instance instance, object data, Dictionary<string, string> prefill);
}