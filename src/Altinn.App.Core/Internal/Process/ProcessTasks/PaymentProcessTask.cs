using Altinn.App.Core.Features.Payment;
using Altinn.App.Core.Features.Payment.Exceptions;
using Altinn.App.Core.Features.Payment.Services;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Pdf;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process.ProcessTasks
{
    /// <summary>
    /// Represents the process task responsible for collecting user payment.
    /// </summary>
    public class PaymentProcessTask : IProcessTask
    {
        private readonly IPdfService _pdfService;
        private readonly IDataClient _dataClient;
        private readonly IProcessReader _processReader;

        private const string PdfContentType = "application/pdf";
        private const string ReceiptFileName = "Betalingskvittering.pdf";

        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentProcessTask"/> class.
        /// </summary>
        public PaymentProcessTask(IPdfService pdfService, IDataClient dataClient, IProcessReader processReader)
        {
            _pdfService = pdfService;
            _dataClient = dataClient;
            _processReader = processReader;
        }
        
        /// <inheritdoc/>
        public string Type => "payment";

        /// <inheritdoc/>
        public Task Start(string taskId, Instance instance)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task End(string taskId, Instance instance)
        {
            AltinnPaymentConfiguration? paymentConfiguration =
                _processReader.GetAltinnTaskExtension(taskId)?.PaymentConfiguration;

            if (paymentConfiguration == null)
            {
                throw new ApplicationConfigException("PaymentConfig is missing in the payment process task configuration.");
            }

            if (string.IsNullOrWhiteSpace(paymentConfiguration.PaymentDataType))
            {
                throw new ApplicationConfigException("PaymentDataType is missing in the payment process task configuration.");
            }

            //TODO: Try to move this into PDF service without making breaking changes. Just needed a quick working demo.

            Stream pdfStream = await _pdfService.GeneratePdf(instance, taskId, CancellationToken.None);

            await _dataClient.InsertBinaryData(
                instance.Id,
                paymentConfiguration.PaymentDataType,
                PdfContentType,
                ReceiptFileName,
                pdfStream,
                taskId);
        }

        /// <inheritdoc/>
        public Task Abandon(string taskId, Instance instance)
        {
            return Task.CompletedTask;
        }
    }
}