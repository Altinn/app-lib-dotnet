using Altinn.App.Core.Internal.Process.ServiceTasks;
using Microsoft.Extensions.Hosting;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal sealed class FiksArkivConfigValidationService : IHostedService
{
    private readonly IFiksArkivMessageHandler _fiksArkivMessageHandler;
    private readonly IFiksArkivServiceTask _fiksArkivServiceTask;

    public FiksArkivConfigValidationService(
        IFiksArkivMessageHandler fiksArkivMessageHandler,
        IFiksArkivServiceTask fiksArkivServiceTask
    )
    {
        _fiksArkivMessageHandler = fiksArkivMessageHandler;
        _fiksArkivServiceTask = fiksArkivServiceTask;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _fiksArkivMessageHandler.ValidateConfiguration();

        if (_fiksArkivServiceTask is IFiksArkivConfigValidation fiksArkivConfigValidation)
            await fiksArkivConfigValidation.ValidateConfiguration();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
