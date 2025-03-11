using Altinn.App.Clients.Fiks.Extensions;
using Altinn.App.Clients.Fiks.FiksIO;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal sealed class FiksArkivSetupBuilder(IServiceCollection serviceCollection) : IFiksArkivSetupBuilder
{
    // public IFiksArkivSetupBuilder WithErrorHandler<TErrorHandler>()
    //     where TErrorHandler : IFiksArkivErrorHandler
    // {
    //     serviceCollection.AddTransient(typeof(IFiksArkivErrorHandler), typeof(TErrorHandler));
    //     return this;
    // }

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

    public IFiksArkivSetupBuilder WithMessageProvider<TMessageBuilder>()
        where TMessageBuilder : IFiksArkivMessageProvider
    {
        serviceCollection.AddTransient(typeof(IFiksArkivMessageProvider), typeof(TMessageBuilder));
        return this;
    }

    public IServiceCollection CompleteSetup()
    {
        return serviceCollection;
    }
}
