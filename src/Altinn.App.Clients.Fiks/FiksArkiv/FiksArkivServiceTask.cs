using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Clients.Fiks.FiksIO;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Process.ServiceTasks;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal sealed class FiksArkivServiceTask : IFiksArkivServiceTask, IFiksArkivConfigValidation
{
    private readonly FiksArkivSettings _fiksArkivSettings;
    private readonly IFiksArkivMessageHandler _fiksArkivMessageHandler;
    private readonly IFiksIOClient _fiksIOClient;
    private readonly ILogger<FiksArkivServiceTask> _logger;
    private readonly IFiksArkivAutoSendDecision _fiksArkivAutoSendDecision;

    public FiksArkivServiceTask(
        IFiksArkivMessageHandler fiksArkivMessageHandler,
        IFiksArkivAutoSendDecision fiksArkivAutoSendDecision,
        IOptions<FiksArkivSettings> fiksArkivSettings,
        IFiksIOClient fiksIOClient,
        ILogger<FiksArkivServiceTask> logger
    )
    {
        _fiksArkivSettings = fiksArkivSettings.Value;
        _fiksArkivMessageHandler = fiksArkivMessageHandler;
        _fiksArkivAutoSendDecision = fiksArkivAutoSendDecision;
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
        FiksIOMessageResponse response = await _fiksIOClient.SendMessage(request);

        _logger.LogInformation("Fiks Arkiv responded with message ID {MessageId}", response.MessageId);
    }

    public async Task ValidateConfiguration(
        IReadOnlyList<DataType> configuredDataTypes,
        IReadOnlyList<ProcessTask> configuredProcessTasks
    )
    {
        await Task.CompletedTask;

        // FiksArkivDefaultMessageHandler has already validated the settings
        if (_fiksArkivMessageHandler is FiksArkivDefaultMessageHandler)
            return;

        _fiksArkivSettings.Validate(configuredDataTypes, configuredProcessTasks);
    }
}
