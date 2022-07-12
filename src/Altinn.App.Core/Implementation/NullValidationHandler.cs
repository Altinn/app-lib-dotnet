using Altinn.App.Core.Interface;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Altinn.App.Core.Implementation;

/// <summary>
/// Default implementation of the IValidationHandler interface.
/// This implementation does not do any validation and always returns true.
/// </summary>
public class NullValidationHandler: IValidationHandler
{
    /// <inheritdoc />
    public async Task ValidateData(object data, ModelStateDictionary validationResults)
    {
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task ValidateTask(Instance instance, string taskId, ModelStateDictionary validationResults)
    {
        await Task.CompletedTask;
    }
}