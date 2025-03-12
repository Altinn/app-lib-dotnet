namespace Altinn.App.Clients.Fiks.FiksIO.Models;

public interface IFiksIOAccountSettings
{
    Guid AccountId { get; }
    Guid IntegrationId { get; }
}
