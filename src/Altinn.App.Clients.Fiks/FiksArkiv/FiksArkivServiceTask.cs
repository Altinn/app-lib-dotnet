using Altinn.App.Clients.Fiks.Constants;
using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.Process.ServiceTasks;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal sealed class FiksArkivServiceTask : IFiksArkivServiceTask
{
    private readonly ILogger<FiksArkivServiceTask> _logger;
    private readonly AppImplementationFactory _appImplementationFactory;
    private readonly IFiksArkivHost _fiksArkivHost;

    private IFiksArkivAutoSendDecision _fiksArkivAutoSendDecision =>
        _appImplementationFactory.GetRequired<IFiksArkivAutoSendDecision>();

    public FiksArkivServiceTask(
        AppImplementationFactory appImplementationFactory,
        IFiksArkivHost fiksArkivHost,
        ILogger<FiksArkivServiceTask> logger
    )
    {
        _appImplementationFactory = appImplementationFactory;
        _fiksArkivHost = fiksArkivHost;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Execute(string taskId, Instance instance)
    {
        var shouldSendDecision = await _fiksArkivAutoSendDecision.ShouldSend(taskId, instance);
        if (shouldSendDecision is false)
            return;

        _logger.LogInformation(
            $"{nameof(FiksArkivServiceTask)} is executing for instance {instance.Id} and task {taskId}"
        );

        await _fiksArkivHost.GenerateAndSendMessage(taskId, instance, FiksArkivConstants.MessageTypes.Create);
    }
}
