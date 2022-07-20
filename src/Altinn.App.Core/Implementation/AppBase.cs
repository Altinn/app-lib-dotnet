using Altinn.App.Core.Interface;
using Altinn.App.Services.Interface;
using Altinn.App.Services.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Altinn.App.Services.Implementation
{
    /// <summary>
    /// Default implementation of the core Altinn App interface.
    /// </summary>
    public class AppBase : IAltinnApp
    {
        private readonly IInstanceValidator _instanceValidator;
        private readonly IInstantiation _instantiation;
        private readonly IDataProcessor _dataProcessor;

        /// <summary>
        /// Initialize a new instance of <see cref="AppBase"/> class with the given services.
        /// </summary>
        /// <param name="resourceService">The service giving access to local resources.</param>
        /// <param name="logger">A logging service.</param>
        /// <param name="dataClient">The data client.</param>
        /// <param name="pdfService">The pdf service responsible for creating the pdf.</param>
        /// <param name="prefillService">The service giving access to prefill mechanisms.</param>
        /// <param name="instanceClient">The instance client</param>
        /// <param name="httpContextAccessor">The httpContextAccessor</param>
        /// <param name="eFormidlingClient">The eFormidling client</param>
        /// <param name="appSettings">The appsettings</param>
        /// <param name="platformSettings">The platform settings</param>
        /// <param name="tokenGenerator">The access token generator</param>
        public AppBase(
            IInstantiation instantiation,
            IInstanceValidator instanceValidator,
            IDataProcessor dataProcessor)
        {
            _instanceValidator = instanceValidator;
            _instantiation = instantiation;
            _dataProcessor = dataProcessor;
        }

        /// <inheritdoc />
        public async Task RunDataValidation(object data, ModelStateDictionary validationResults)
        {
            await _instanceValidator.ValidateData(data, validationResults);
        }

        /// <inheritdoc />
        public async Task RunTaskValidation(Instance instance, string taskId, ModelStateDictionary validationResults)
        {
            await _instanceValidator.ValidateTask(instance, taskId, validationResults);
        }

        /// <inheritdoc />
        public async Task<bool> RunProcessDataRead(Instance instance, Guid? dataId, object data)
        {
            return await _dataProcessor.ProcessDataRead(instance, dataId, data);
        }

        /// <inheritdoc />
        public async Task<bool> RunProcessDataWrite(Instance instance, Guid? dataId, object data)
        {
            return await _dataProcessor.ProcessDataWrite(instance, dataId, data);
        }

        /// <inheritdoc />
        public async Task<InstantiationValidationResult> RunInstantiationValidation(Instance instance)
        {
            return await _instantiation.Validation(instance);
        }

        /// <inheritdoc />
        public async Task RunDataCreation(Instance instance, object data, Dictionary<string, string> prefill)
        {
            await _instantiation.DataCreation(instance, data, prefill);
        }

        /// <inheritdoc />
        public async Task<bool> CanEndProcessTask(string taskId, Instance instance,
            List<ValidationIssue> validationIssues)
        {
            // check if the task is validated
            if (instance.Process?.CurrentTask?.Validated != null)
            {
                ValidationStatus validationStatus = instance.Process.CurrentTask.Validated;

                if (validationStatus.CanCompleteTask)
                {
                    return true;
                }
            }
            else
            {
                if (validationIssues.Count == 0)
                {
                    return true;
                }
            }

            return await Task.FromResult(false);
        }
    }
}