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
    private readonly IProcessReader _processReader;
    private readonly IAppMetadata _appMetadata;
    private readonly AppImplementationFactory _appImplementationFactory;

    private readonly IFiksArkivMessageHandler _fiksArkivMessageHandler;
    private readonly IFiksArkivInstanceClient _fiksArkivInstanceClient;

    private IFiksArkivConfigValidation _fiksArkivAutoSendDecisionValidator =>
        _appImplementationFactory.GetAll<IFiksArkivAutoSendDecision>().OfType<IFiksArkivConfigValidation>().First();
    private IFiksArkivConfigValidation _fiksArkivServiceTaskValidator =>
        _appImplementationFactory
            .GetAll<IServiceTask>()
            .OfType<IFiksArkivServiceTask>()
            .OfType<IFiksArkivConfigValidation>()
            .First();

    public FiksArkivConfigValidationService(
        AppImplementationFactory appImplementationFactory,
        IFiksArkivMessageHandler fiksArkivMessageHandler,
        IFiksArkivInstanceClient fiksArkivInstanceClient,
        IProcessReader processReader,
        IAppMetadata appMetadata
    )
    {
        _appImplementationFactory = appImplementationFactory;
        _fiksArkivMessageHandler = fiksArkivMessageHandler;
        _fiksArkivInstanceClient = fiksArkivInstanceClient;
        _processReader = processReader;
        _appMetadata = appMetadata;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ApplicationMetadata appMetadata = await _appMetadata.GetApplicationMetadata();
        IReadOnlyList<ProcessTask> processTasks = _processReader.GetProcessTasks();

        await _fiksArkivMessageHandler.ValidateConfiguration(appMetadata.DataTypes, processTasks);
        await _fiksArkivServiceTaskValidator.ValidateConfiguration(appMetadata.DataTypes, processTasks);
        await _fiksArkivAutoSendDecisionValidator.ValidateConfiguration(appMetadata.DataTypes, processTasks);
        await _fiksArkivInstanceClient.GetServiceOwnerToken();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
