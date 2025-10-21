using Altinn.App.Clients.Fiks.Exceptions;
using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Clients.Fiks.FiksIO;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Process.ServiceTasks;
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
    private readonly IFiksArkivMessageHandler _fiksArkivMessageHandler;

    private IFiksArkivAutoSendDecision _fiksArkivAutoSendDecision =>
        _appImplementationFactory.GetRequired<IFiksArkivAutoSendDecision>();

    public FiksArkivServiceTask(
        AppImplementationFactory appImplementationFactory,
        IOptions<FiksArkivSettings> fiksArkivSettings,
        IFiksIOClient fiksIOClient,
        IFiksArkivMessageHandler fiksArkivMessageHandler,
        ILogger<FiksArkivServiceTask> logger
    )
    {
        _appImplementationFactory = appImplementationFactory;
        _fiksArkivMessageHandler = fiksArkivMessageHandler;
        _fiksArkivSettings = fiksArkivSettings.Value;
        _fiksIOClient = fiksIOClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Execute(string taskId, Instance instance)
    {
        var shouldSendDecision = await _fiksArkivAutoSendDecision.ShouldSend(taskId, instance);
        if (shouldSendDecision is false)
            return;

        _logger.LogInformation("Sending Fiks Arkiv message for instance {InstanceId}", instance.Id);

        FiksIOMessageRequest request = await _fiksArkivMessageHandler.CreateMessageRequest(taskId, instance);
        await _fiksArkivMessageHandler.SaveArchiveRecord(instance, request);
        FiksIOMessageResponse response = await _fiksIOClient.SendMessage(request);

        _logger.LogInformation("Fiks Arkiv responded with message ID {MessageId}", response.MessageId);
    }

    /// <inheritdoc />
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
