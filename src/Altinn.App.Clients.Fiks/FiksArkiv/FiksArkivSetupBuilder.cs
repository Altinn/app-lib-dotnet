using Altinn.App.Clients.Fiks.Extensions;
using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal sealed class FiksArkivSetupBuilder(IServiceCollection serviceCollection) : IFiksArkivSetupBuilder
{
    public IFiksArkivSetupBuilder WithFiksIOConfig(Action<FiksIOSettings> configureOptions)
    {
        serviceCollection.ConfigureFiksIOClient(configureOptions);
        return this;
    }

    public IFiksArkivSetupBuilder WithFiksIOConfig(string configSectionPath)
    {
        serviceCollection.ConfigureFiksIOClient(configSectionPath);
        return this;
    }

    public IFiksArkivSetupBuilder WithFiksArkivConfig(Action<FiksArkivSettings> configureOptions)
    {
        serviceCollection.ConfigureFiksArkiv(configureOptions);
        return this;
    }

    public IFiksArkivSetupBuilder WithFiksArkivConfig(string configSectionPath)
    {
        serviceCollection.ConfigureFiksArkiv(configSectionPath);
        return this;
    }

    public IFiksArkivSetupBuilder WithMessageHandler<TMessageHandler>()
        where TMessageHandler : IFiksArkivMessageHandler
    {
        serviceCollection.AddTransient(typeof(IFiksArkivMessageHandler), typeof(TMessageHandler));
        return this;
    }

    public IServiceCollection CompleteSetup()
    {
        return serviceCollection;
    }
}
