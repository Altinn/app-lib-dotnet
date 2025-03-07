using Altinn.App.Clients.Fiks.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.App.Clients.Fiks.FiksIO;

internal sealed class FiksIOSetupBuilder(IServiceCollection serviceCollection) : IFiksIOSetupBuilder
{
    public IFiksIOSetupBuilder WithConfig(Action<FiksIOSettings> configureOptions)
    {
        serviceCollection.ConfigureFiksIOClient(configureOptions);
        return this;
    }

    public IFiksIOSetupBuilder WithConfig(string configSectionPath)
    {
        serviceCollection.ConfigureFiksIOClient(configSectionPath);
        return this;
    }

    public IServiceCollection CompleteSetup()
    {
        return serviceCollection;
    }
}
