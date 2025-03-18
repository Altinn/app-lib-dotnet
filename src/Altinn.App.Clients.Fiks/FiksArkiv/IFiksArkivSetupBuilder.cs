using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

public interface IFiksArkivSetupBuilder
{
    IFiksArkivSetupBuilder WithFiksIOConfig(Action<FiksIOSettings> configureOptions);
    IFiksArkivSetupBuilder WithFiksIOConfig(string configSectionPath);
    IFiksArkivSetupBuilder WithFiksArkivConfig(Action<FiksArkivSettings> configureOptions);
    IFiksArkivSetupBuilder WithFiksArkivConfig(string configSectionPath);
    IFiksArkivSetupBuilder WithMessageHandler<TMessageHandler>()
        where TMessageHandler : IFiksArkivMessageHandler;
    IServiceCollection CompleteSetup();
}
