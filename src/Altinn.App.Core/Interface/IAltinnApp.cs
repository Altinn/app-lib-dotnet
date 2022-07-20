using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Altinn.App.Services.Models.Validation;
using Altinn.Common.EFormidlingClient.Models.SBD;
using Altinn.Platform.Storage.Interface.Models;

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Altinn.App.Services.Interface
{
    /// <summary>
    /// This interface defines all events a service possible can experience
    /// runtime in Altinn Services 3.0. A Service does only need to implement
    /// the relevant methods. All other methods should be empty.
    /// </summary>
    public interface IAltinnApp
    {

        /// <summary>
        /// AppLogic must set the start event of the process model.
        /// </summary>
        /// <returns>the id of the start event</returns>
        //Task<string> OnInstantiateGetStartEvent();

        /// <summary>
        ///  Called before a process task is ended. App can do extra validation logic and add validation issues to collection which will be returned by the controller.
        /// </summary>
        /// <returns>true task can be ended, false otherwise</returns>
        Task<bool> CanEndProcessTask(string taskId, Instance instance, List<ValidationIssue> validationIssues);

        /// <summary>
        /// Is called to run custom data validation events defined by app developer.
        /// </summary>
        /// <param name="data">The data to validate</param>
        /// <param name="validationResults">Object containing any validation errors/warnings</param>
        /// <returns>Task to indicate when validation is completed</returns>
        Task RunDataValidation(object data, ModelStateDictionary validationResults);

        /// <summary>
        /// Is called to run custom task validation events defined by app developer.
        /// </summary>
        /// <param name="instance">Instance to be validated.</param>
        /// <param name="taskId">Task id for the current process task.</param>
        /// <param name="validationResults">Object containing any validation errors/warnings</param>
        /// <returns>Task to indicate when validation is completed</returns>
        Task RunTaskValidation(Instance instance, string taskId, ModelStateDictionary validationResults);

        /// <summary>
        /// Is called to run custom calculation events defined by app developer when data is read from app
        /// </summary>
        /// <param name="instance">Instance that data belongs to</param>
        /// <param name="dataId">Data id for the  data</param>
        /// <param name="data">The data to perform calculations on</param>
        Task<bool> RunProcessDataRead(Instance instance, Guid? dataId, object data);

        /// <summary>
        /// Is called to run custom calculation events defined by app developer when data is written to app
        /// </summary>
        /// <param name="instance">Instance that data belongs to</param>
        /// <param name="dataId">Data id for the  data</param>
        /// <param name="data">The data to perform calculations on</param>
        Task<bool> RunProcessDataWrite(Instance instance, Guid? dataId, object data);

        /// <summary>
        /// Is called to run custom instantiation validation defined by app developer.
        /// </summary>
        /// <returns>Task with validation results</returns>
        Task<InstantiationValidationResult> RunInstantiationValidation(Instance instance);

        /// <summary>
        /// Is called to run data creation (custom prefill) defined by app developer. Includes external prefill
        /// </summary>
        Task RunDataCreation(Instance instance, object data, Dictionary<string, string> prefill);
    }
}
