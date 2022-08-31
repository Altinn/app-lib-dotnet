using System;
using System.Threading;
using System.Threading.Tasks;

using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.PlatformServices.Interface
{
    /// <summary>
    /// Interface for handling generation and storing of PDF's
    /// </summary>
    public interface IPdfService
    {
        /// <summary>
        /// Generates the PDF based on the current data and stores it
        /// </summary>
        /// <param name="instance">The instance the PDF is based on.</param>
        /// <param name="taskId">The task id matching the </param>
        /// <param name="dataElement">Reference to the data element.</param>
        /// <param name="dataElementModelType">Type of data referenced</param>
        Task GenerateAndStoreReceiptPDF(Instance instance, string taskId, DataElement dataElement, Type dataElementModelType);

        /// <summary>
        /// Generate a PDF of what the user can currently see from the given instance of an app. Saves the PDF
        /// to storage as a new binary file associated with the predefined PDF data type in most apps.
        /// </summary>
        /// <remarks>
        /// This is implemented for testing purposes. Do not use this in your app. It is unlikely to work for
        /// anyone except Testdirektoratet and there will be breaking changes in future versions of this system.
        /// </remarks>
        /// <param name="instance">The instance details.</param>
        /// <param name="ct">Cancellation Token for when a request should be stopped before it's completed.</param>
        Task GenerateAndStorePdf(Instance instance, CancellationToken ct);
    }
}