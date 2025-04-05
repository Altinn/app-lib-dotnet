using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Process.ServiceTasks;
using Altinn.App.Core.Models;
using Microsoft.Extensions.Hosting;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal sealed class FiksArkivConfigValidationService : IHostedService
{
    private readonly IFiksArkivMessageHandler _fiksArkivMessageHandler;
    private readonly IFiksArkivServiceTask _fiksArkivServiceTask;
    private readonly IProcessReader _processReader;
    private readonly IAppMetadata _appMetadata;
    private readonly IFiksArkivAutoSendDecision _fiksArkivAutoSendDecision;

    public FiksArkivConfigValidationService(
        IFiksArkivMessageHandler fiksArkivMessageHandler,
        IFiksArkivServiceTask fiksArkivServiceTask,
        IFiksArkivAutoSendDecision fiksArkivAutoSendDecision,
        IProcessReader processReader,
        IAppMetadata appMetadata
    )
    {
        _fiksArkivMessageHandler = fiksArkivMessageHandler;
        _fiksArkivServiceTask = fiksArkivServiceTask;
        _fiksArkivAutoSendDecision = fiksArkivAutoSendDecision;
        _processReader = processReader;
        _appMetadata = appMetadata;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ApplicationMetadata appMetadata = await _appMetadata.GetApplicationMetadata();
        IReadOnlyList<ProcessTask> processTasks = _processReader.GetProcessTasks();

        await _fiksArkivMessageHandler.ValidateConfiguration(appMetadata.DataTypes, processTasks);

        if (_fiksArkivServiceTask is IFiksArkivConfigValidation fiksArkivConfigValidation)
            await fiksArkivConfigValidation.ValidateConfiguration(appMetadata.DataTypes, processTasks);

        if (_fiksArkivAutoSendDecision is IFiksArkivConfigValidation fiksArkivAutoSendConfigValidation)
            await fiksArkivAutoSendConfigValidation.ValidateConfiguration(appMetadata.DataTypes, processTasks);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
