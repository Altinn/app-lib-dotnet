using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Services.Interface
{
    /// <summary>
    /// This interface defines methods for data processing in the altinn application. 
    /// </summary>
    public interface IDataProcessing
    {
        /// <summary>
        /// Is called to run custom calculation events defined by app developer when data is written to app
        /// </summary>
        /// <param name="instance">Instance that data belongs to</param>
        /// <param name="dataId">Data id for the  data</param>
        /// <param name="data">The data to perform calculations on</param>
        /// <param name="currentFields">The changed field that triggered the method</param>
        Task<bool> RunProcessDataWriteNEW(Instance instance, Guid? dataId, object data, Dictionary<string, object> currentFields);

    }
}
