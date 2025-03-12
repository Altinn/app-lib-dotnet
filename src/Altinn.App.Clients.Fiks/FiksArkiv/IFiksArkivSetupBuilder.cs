using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

public interface IFiksArkivSetupBuilder
{
    // IFiksArkivSetupBuilder WithErrorHandler<TErrorHandler>()
    //     where TErrorHandler : IFiksArkivErrorHandler;
    IFiksArkivSetupBuilder WithFiksIOConfig(Action<FiksIOSettings> configureOptions);
    IFiksArkivSetupBuilder WithFiksIOConfig(string configSectionPath);
    IFiksArkivSetupBuilder WithFiksArkivConfig(Action<FiksArkivSettings> configureOptions);
    IFiksArkivSetupBuilder WithFiksArkivConfig(string configSectionPath);
    IFiksArkivSetupBuilder WithMessageProvider<TMessageBuilder>()
        where TMessageBuilder : IFiksArkivMessageProvider;
    IServiceCollection CompleteSetup();
}
