namespace Altinn.App.Clients.Fiks.FiksIO;

public interface IFiksIOClient : IDisposable
{
    Guid AccountId { get; }
    Guid IntegrationId { get; }
    RetryStrategy RetryStrategy { get; set; }
    bool IsHealthy();
    Task Reconnect();
    Task OnMessageReceived(EventHandler<FiksIOReceivedMessageArgs> listener);
    Task<FiksIOMessageResponse> SendMessage(
        FiksIOMessageRequest request,
        CancellationToken cancellationToken = default
    );
}
