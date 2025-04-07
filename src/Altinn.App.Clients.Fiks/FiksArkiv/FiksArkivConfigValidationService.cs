using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Process.ServiceTasks;
using Altinn.App.Core.Models;
using Microsoft.Extensions.Hosting;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal sealed class FiksArkivConfigValidationService : IHostedService
{
    private readonly IFiksArkivServiceTask _fiksArkivServiceTask;
    private readonly IProcessReader _processReader;
    private readonly IAppMetadata _appMetadata;
    private readonly AppImplementationFactory _appImplementationFactory;

    private IFiksArkivMessageHandler _fiksArkivMessageHandler =>
        _appImplementationFactory.GetRequired<IFiksArkivMessageHandler>();
    private IFiksArkivAutoSendDecision _fiksArkivAutoSendDecision =>
        _appImplementationFactory.GetRequired<IFiksArkivAutoSendDecision>();

    public FiksArkivConfigValidationService(
        AppImplementationFactory appImplementationFactory,
        IFiksArkivServiceTask fiksArkivServiceTask,
        IProcessReader processReader,
        IAppMetadata appMetadata
    )
    {
        _appImplementationFactory = appImplementationFactory;
        _fiksArkivServiceTask = fiksArkivServiceTask;
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
