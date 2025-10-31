using Altinn.App.Clients.Fiks.Constants;
using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Core.Internal.Process.ProcessTasks.ServiceTasks;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal sealed class FiksArkivServiceTask : IFiksArkivServiceTask
{
    private readonly ILogger<FiksArkivServiceTask> _logger;
    private readonly IFiksArkivHost _fiksArkivHost;
    private readonly FiksArkivSettings _fiksArkivSettings;

    internal const string TaskIdentifier = "fiksArkiv";
    public string Type => TaskIdentifier;

    public FiksArkivServiceTask(
        IFiksArkivHost fiksArkivHost,
        IOptions<FiksArkivSettings> fiksArkivSettings,
        ILogger<FiksArkivServiceTask> logger
    )
    {
        _fiksArkivHost = fiksArkivHost;
        _fiksArkivSettings = fiksArkivSettings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ServiceTaskResult> Execute(ServiceTaskContext context)
    {
        try
        {
            Instance instance = context.InstanceDataMutator.Instance;
            string taskId = instance.Process.CurrentTask.ElementId;

            _logger.LogInformation(
                "FiksArkivServiceTask is executing for instance {InstanceId} and task {TaskId}",
                instance.Id,
                taskId
            );

            var response = await _fiksArkivHost.GenerateAndSendMessage(
                taskId,
                instance,
                FiksArkivConstants.MessageTypes.Create
            );

            _logger.LogInformation(
                "FiksArkivServiceTask completed for instance {InstanceId} with response: {Response}",
                instance.Id,
                response
            );

            return ServiceTaskResult.Success();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error occurred while executing FiksArkivServiceTask: {ErrorMessage}", e.Message);

            string? action = !string.IsNullOrWhiteSpace(_fiksArkivSettings.ErrorHandling?.Action)
                ? _fiksArkivSettings.ErrorHandling?.Action
                : null;

            return _fiksArkivSettings.ErrorHandling?.MoveToNextTask is true
                ? ServiceTaskResult.FailedContinueProcessNext(action)
                : ServiceTaskResult.FailedAbortProcessNext();
        }
    }
}
