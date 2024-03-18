using Altinn.App.Core.Features.Payment;
using Altinn.App.Core.Features.Payment.Exceptions;
using Altinn.App.Core.Features.Payment.Services;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process.ProcessTasks
{
    /// <summary>
    /// Represents the process task responsible for collecting user payment.
    /// </summary>
    public class PaymentProcessTask : IProcessTask
    {
        /// <inheritdoc/>
        public string Type => "payment";

        /// <inheritdoc/>
        public Task Start(string taskId, Instance instance)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task End(string taskId, Instance instance)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task Abandon(string taskId, Instance instance)
        {
            return Task.CompletedTask;
        }
    }
}