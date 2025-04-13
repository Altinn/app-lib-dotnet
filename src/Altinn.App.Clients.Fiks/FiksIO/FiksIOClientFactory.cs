using Altinn.App.Core.Features.Maskinporten;
using Microsoft.Extensions.Logging;
using ExternalConfiguration = KS.Fiks.IO.Client.Configuration.FiksIOConfiguration;
using ExternalFiksIOClient = KS.Fiks.IO.Client.FiksIOClient;
using IExternalFiksIOClient = KS.Fiks.IO.Client.IFiksIOClient;
using IFiksMaskinportenClient = Ks.Fiks.Maskinporten.Client.IMaskinportenClient;

namespace Altinn.App.Clients.Fiks.FiksIO;

internal class FiksIOClientFactory : IFiksIOClientFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IMaskinportenClient _maskinportenClient;

    public FiksIOClientFactory(ILoggerFactory loggerFactory, IMaskinportenClient maskinportenClient)
    {
        _loggerFactory = loggerFactory;
        _maskinportenClient = maskinportenClient;
    }

    public async Task<IExternalFiksIOClient> CreateClient(
        ExternalConfiguration fiksConfiguration,
        IFiksMaskinportenClient maskinportenClient,
        ILoggerFactory loggerFactory
    )
    {
        return await ExternalFiksIOClient.CreateAsync(
            configuration: fiksConfiguration,
            maskinportenClient: new FiksIOMaskinportenClient(_maskinportenClient),
            loggerFactory: _loggerFactory
        );
    }
}
