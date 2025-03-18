using Altinn.App.Clients.Fiks.Exceptions;
using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Clients.Fiks.FiksIO;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.App.Core.Internal.Process.ServiceTasks;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal sealed class FiksArkivServiceTask : IFiksArkivServiceTask
{
    private readonly FiksArkivSettings _fiksArkivSettings;
    private readonly IFiksArkivMessageHandler _fiksArkivMessageHandler;
    private readonly IFiksIOClient _fiksIOClient;
    private readonly ILogger<FiksArkivServiceTask> _logger;

    public string Id => ServiceTaskIdentifiers.FiksArkiv;

    public FiksArkivServiceTask(
        IFiksArkivMessageHandler fiksArkivMessageHandler,
        IOptions<FiksArkivSettings> fiksArkivSettings,
        IFiksIOClient fiksIOClient,
        ILogger<FiksArkivServiceTask> logger
    )
    {
        _fiksArkivSettings = fiksArkivSettings.Value;
        _fiksArkivMessageHandler = fiksArkivMessageHandler;
        _fiksIOClient = fiksIOClient;
        _logger = logger;
    }

    public async Task Execute(string taskId, Instance instance)
    {
        if (IsEnabledForTask(taskId) is false)
            return;

        _logger.LogInformation("Sending Fiks Arkiv message for instance {InstanceId}", instance.Id);

        FiksIOMessageRequest request = await _fiksArkivMessageHandler.CreateMessageRequest(taskId, instance);
        FiksIOMessageResponse response = await _fiksIOClient.SendMessage(request);

        _logger.LogInformation("Fiks Arkiv responded with message ID {MessageId}", response.MessageId);
    }

    private bool IsEnabledForTask(string taskId) => _fiksArkivSettings.AutoSend?.AfterTaskId == taskId;

    public Task ValidateConfiguration()
    {
        if (_fiksArkivSettings.AutoSend is null)
            return Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(_fiksArkivSettings.AutoSend?.AfterTaskId))
            throw new FiksArkivConfigurationException("AfterTaskId configuration is required for auto-send.");

        return Task.CompletedTask;
    }
}
