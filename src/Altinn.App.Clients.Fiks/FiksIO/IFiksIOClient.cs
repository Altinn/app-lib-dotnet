using Altinn.App.Clients.Fiks.FiksIO.Models;

namespace Altinn.App.Clients.Fiks.FiksIO;

public interface IFiksIOClient : IDisposable
{
    IFiksIOAccountSettings AccountSettings { get; }
    bool IsHealthy();
    Task Reconnect();
    Task OnMessageReceived(EventHandler<FiksIOReceivedMessageArgs> listener);
    Task<FiksIOMessageResponse> SendMessage(
        FiksIOMessageRequest request,
        CancellationToken cancellationToken = default
    );
}
