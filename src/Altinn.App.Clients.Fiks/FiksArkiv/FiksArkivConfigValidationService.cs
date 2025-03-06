using Microsoft.Extensions.Hosting;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal class FiksArkivConfigValidationService : IHostedService
{
    private readonly IFiksArkivErrorHandler _fiksArkivErrorHandler;
    private readonly IFiksArkivMessageProvider _fiksArkivMessageProvider;
    private readonly IFiksArkivServiceTask _fiksArkivServiceTask;

    public FiksArkivConfigValidationService(
        IFiksArkivErrorHandler fiksArkivErrorHandler,
        IFiksArkivMessageProvider fiksArkivMessageProvider,
        IFiksArkivServiceTask fiksArkivServiceTask
    )
    {
        _fiksArkivErrorHandler = fiksArkivErrorHandler;
        _fiksArkivMessageProvider = fiksArkivMessageProvider;
        _fiksArkivServiceTask = fiksArkivServiceTask;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_fiksArkivErrorHandler is FiksArkivDefaultErrorHandler defaultErrorHandler)
            defaultErrorHandler.ValidateConfiguration();

        if (_fiksArkivMessageProvider is FiksArkivDefaultMessageProvider defaultMessageProvider)
            defaultMessageProvider.ValidateConfiguration();

        if (_fiksArkivServiceTask is FiksArkivServiceTask serviceTask)
            serviceTask.ValidateConfiguration();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
