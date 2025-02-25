using Altinn.App.Clients.Fiks.FiksIO;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal sealed class FiksArkivSetupBuilder(IServiceCollection ServiceCollection) : IFiksArkivSetupBuilder
{
    public IFiksArkivSetupBuilder WithErrorHandler<TErrorHandler>()
        where TErrorHandler : IFiksArkivErrorHandler
    {
        ServiceCollection.AddTransient(typeof(IFiksArkivErrorHandler), typeof(TErrorHandler));
        return this;
    }

    public IFiksArkivSetupBuilder WithFiksIOConfig(Action<FiksIOSettings> configureOptions)
    {
        ServiceCollection.ConfigureFiksIOClient(configureOptions);
        return this;
    }

    public IFiksArkivSetupBuilder WithFiksIOConfig(string configSectionPath)
    {
        ServiceCollection.ConfigureFiksIOClient(configSectionPath);
        return this;
    }

    public IFiksArkivSetupBuilder WithFiksArkivConfig(Action<FiksArkivSettings> configureOptions)
    {
        ServiceCollection.ConfigureFiksArkiv(configureOptions);
        return this;
    }

    public IFiksArkivSetupBuilder WithFiksArkivConfig(string configSectionPath)
    {
        ServiceCollection.ConfigureFiksArkiv(configSectionPath);
        return this;
    }

    public IFiksArkivSetupBuilder WithMessageProvider<TMessageBuilder>()
        where TMessageBuilder : IFiksArkivMessageProvider
    {
        ServiceCollection.AddTransient(typeof(IFiksArkivMessageProvider), typeof(TMessageBuilder));
        return this;
    }

    public IServiceCollection CompleteSetup()
    {
        return ServiceCollection;
    }
}
