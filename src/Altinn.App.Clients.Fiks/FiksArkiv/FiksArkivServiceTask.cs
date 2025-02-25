using Altinn.App.Clients.Fiks.FiksIO;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal class FiksArkivServiceTask : IFiksArkivServiceTask
{
    private readonly FiksArkivSettings _fiksArkivSettings;
    private readonly IFiksArkivMessageProvider _fiksArkivMessageProvider;
    private readonly IFiksIOClient _fiksIOClient;
    private readonly ILogger<FiksArkivServiceTask> _logger;

    public FiksArkivServiceTask(
        IFiksArkivMessageProvider fiksArkivMessageProvider,
        IOptions<FiksArkivSettings> fiksArkivSettings,
        IFiksIOClient fiksIOClient,
        ILogger<FiksArkivServiceTask> logger
    )
    {
        _fiksArkivSettings = fiksArkivSettings.Value;
        _fiksArkivMessageProvider = fiksArkivMessageProvider;
        _fiksIOClient = fiksIOClient;
        _logger = logger;
    }

    public async Task Execute(string taskId, Instance instance)
    {
        if (IsEnabledForTask(taskId) is false)
            return;

        _logger.LogInformation("Sending Fiks Arkiv message for instance {InstanceId}", instance.Id);

        FiksIOMessageRequest request = await _fiksArkivMessageProvider.CreateMessageRequest(taskId, instance);
        FiksIOMessageResponse response = await _fiksIOClient.SendMessage(request);

        _logger.LogInformation("Fiks Arkiv responded with message-ID {MessageId}", response.MessageId);
    }

    private bool IsEnabledForTask(string taskId) => _fiksArkivSettings.AutoSend?.AfterTaskId == taskId;

    public void ValidateConfiguration() { }
}
