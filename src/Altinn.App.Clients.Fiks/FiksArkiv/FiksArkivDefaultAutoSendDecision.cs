using Altinn.App.Clients.Fiks.Exceptions;
using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Options;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal class FiksArkivDefaultAutoSendDecision : IFiksArkivAutoSendDecision, IFiksArkivConfigValidation
{
    private readonly FiksArkivSettings _fiksArkivSettings;

    public FiksArkivDefaultAutoSendDecision(IOptions<FiksArkivSettings> fiksArkivSettings)
    {
        _fiksArkivSettings = fiksArkivSettings.Value;
    }

    public Task<bool> ShouldSend(string taskId, Instance instance, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_fiksArkivSettings.AutoSend?.AfterTaskId == taskId);
    }

    public Task ValidateConfiguration(
        IReadOnlyList<DataType> configuredDataTypes,
        IReadOnlyList<ProcessTask> configuredProcessTasks
    )
    {
        if (_fiksArkivSettings.AutoSend is null)
            throw new FiksArkivConfigurationException(
                $"{nameof(FiksArkivSettings.AutoSend)} configuration is required for auto-send with default handler {GetType().Name}."
            );

        _fiksArkivSettings.AutoSend.Validate(configuredProcessTasks);

        return Task.CompletedTask;
    }
}
