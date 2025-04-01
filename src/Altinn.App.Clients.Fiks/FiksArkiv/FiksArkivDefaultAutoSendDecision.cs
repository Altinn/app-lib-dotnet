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

    public Task<bool> ShouldSend(string taskId, Instance instance)
    {
        return Task.FromResult(_fiksArkivSettings.AutoSend?.AfterTaskId == taskId);
    }

    public Task ValidateConfiguration(
        IReadOnlyList<DataType> configuredDataTypes,
        IReadOnlyList<ProcessTask> configuredProcessTasks
    )
    {
        string? afterTaskId = _fiksArkivSettings.AutoSend?.AfterTaskId;
        const string propertyName =
            $"{nameof(FiksArkivSettings.AutoSend)}.{nameof(FiksArkivSettings.AutoSend.AfterTaskId)}";

        if (string.IsNullOrWhiteSpace(afterTaskId))
            throw new FiksArkivConfigurationException(
                $"{propertyName} configuration is required for auto-send with default handler {nameof(FiksArkivDefaultAutoSendDecision)}."
            );

        if (configuredProcessTasks.FirstOrDefault(x => x.Id == afterTaskId) is null)
            throw new FiksArkivConfigurationException(
                $"{propertyName} mismatch with application process tasks: {afterTaskId}"
            );

        return Task.CompletedTask;
    }
}
