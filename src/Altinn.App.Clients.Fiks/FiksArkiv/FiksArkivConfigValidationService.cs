using Microsoft.Extensions.Hosting;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal sealed class FiksArkivConfigValidationService : IHostedService
{
    // private readonly IFiksArkivErrorHandler _fiksArkivErrorHandler;
    private readonly IFiksArkivMessageProvider _fiksArkivMessageProvider;
    private readonly IFiksArkivServiceTask _fiksArkivServiceTask;

    public FiksArkivConfigValidationService(
        // IFiksArkivErrorHandler fiksArkivErrorHandler,
        IFiksArkivMessageProvider fiksArkivMessageProvider,
        IFiksArkivServiceTask fiksArkivServiceTask
    )
    {
        // _fiksArkivErrorHandler = fiksArkivErrorHandler;
        _fiksArkivMessageProvider = fiksArkivMessageProvider;
        _fiksArkivServiceTask = fiksArkivServiceTask;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // await _fiksArkivErrorHandler.ValidateConfiguration();
        await _fiksArkivMessageProvider.ValidateConfiguration();
        await _fiksArkivServiceTask.ValidateConfiguration();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
