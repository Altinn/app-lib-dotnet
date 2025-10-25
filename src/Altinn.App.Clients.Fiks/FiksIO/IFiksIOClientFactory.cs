using Microsoft.Extensions.Logging;
using ExternalConfiguration = KS.Fiks.IO.Client.Configuration.FiksIOConfiguration;
using IExternalFiksIOClient = KS.Fiks.IO.Client.IFiksIOClient;
using IFiksMaskinportenClient = Ks.Fiks.Maskinporten.Client.IMaskinportenClient;

namespace Altinn.App.Clients.Fiks.FiksIO;

internal interface IFiksIOClientFactory
{
    Task<IExternalFiksIOClient> CreateClient(ExternalConfiguration fiksConfiguration);
}
