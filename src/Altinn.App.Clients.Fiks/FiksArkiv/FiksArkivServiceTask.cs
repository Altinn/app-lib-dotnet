using Altinn.App.Clients.Fiks.Constants;
using Altinn.App.Clients.Fiks.Exceptions;
using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Clients.Fiks.FiksIO;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Process.ServiceTasks;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal sealed class FiksArkivServiceTask : IFiksArkivServiceTask, IFiksArkivConfigValidation
{
    private readonly IFiksIOClient _fiksIOClient;
    private readonly ILogger<FiksArkivServiceTask> _logger;
    private readonly AppImplementationFactory _appImplementationFactory;
    private readonly FiksArkivSettings _fiksArkivSettings;
    private readonly IFiksArkivInstanceClient _fiksArkivInstanceClient;
    private readonly IFiksArkivMessageHandler _fiksArkivMessageHandler;

    private IFiksArkivAutoSendDecision _fiksArkivAutoSendDecision =>
        _appImplementationFactory.GetRequired<IFiksArkivAutoSendDecision>();

    public FiksArkivServiceTask(
        AppImplementationFactory appImplementationFactory,
        IOptions<FiksArkivSettings> fiksArkivSettings,
        IFiksIOClient fiksIOClient,
        IFiksArkivInstanceClient fiksArkivInstanceClient,
        IFiksArkivMessageHandler fiksArkivMessageHandler,
        ILogger<FiksArkivServiceTask> logger
    )
    {
        _appImplementationFactory = appImplementationFactory;
        _fiksArkivMessageHandler = fiksArkivMessageHandler;
        _fiksArkivSettings = fiksArkivSettings.Value;
        _fiksArkivInstanceClient = fiksArkivInstanceClient;
        _fiksIOClient = fiksIOClient;
        _logger = logger;
    }

    public async Task Execute(string taskId, Instance instance)
    {
        var shouldSendDecision = await _fiksArkivAutoSendDecision.ShouldSend(taskId, instance);
        if (shouldSendDecision is false)
            return;

        _logger.LogInformation("Sending Fiks Arkiv message for instance {InstanceId}", instance.Id);

        FiksIOMessageRequest request = await _fiksArkivMessageHandler.CreateMessageRequest(taskId, instance);

        // Persist archive record on the instance
        ArgumentNullException.ThrowIfNull(_fiksArkivSettings.Receipt);
        await _fiksArkivInstanceClient.InsertBinaryData(
            new InstanceIdentifier(instance),
            _fiksArkivSettings.Receipt.ArchiveRecord.DataType,
            "application/json",
            _fiksArkivSettings.Receipt.ArchiveRecord.GetFilenameOrDefault(".xml"),
            request.Payload.Single(x => x.Filename == FiksArkivConstants.ArchiveRecordFilename)
        );

        FiksIOMessageResponse response = await _fiksIOClient.SendMessage(request);

        _logger.LogInformation("Fiks Arkiv responded with message ID {MessageId}", response.MessageId);
    }

    public Task ValidateConfiguration(
        IReadOnlyList<DataType> configuredDataTypes,
        IReadOnlyList<ProcessTask> configuredProcessTasks
    )
    {
        if (_fiksArkivSettings.Receipt is null)
            throw new FiksArkivConfigurationException(
                $"{nameof(FiksArkivSettings.Receipt)} configuration is required for default handler {GetType().Name}."
            );

        _fiksArkivSettings.Receipt.Validate(nameof(_fiksArkivSettings.Receipt), configuredDataTypes);

        return Task.CompletedTask;
    }
}
