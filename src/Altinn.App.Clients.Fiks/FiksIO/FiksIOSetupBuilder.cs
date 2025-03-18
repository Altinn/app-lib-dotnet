using Altinn.App.Clients.Fiks.Extensions;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.DependencyInjection;

namespace Altinn.App.Clients.Fiks.FiksIO;

internal sealed class FiksIOSetupBuilder(IServiceCollection serviceCollection) : IFiksIOSetupBuilder
{
    /// <inheritdoc />
    public IFiksIOSetupBuilder WithConfig(Action<FiksIOSettings> configureOptions)
    {
        serviceCollection.ConfigureFiksIOClient(configureOptions);
        return this;
    }

    /// <inheritdoc />
    public IFiksIOSetupBuilder WithConfig(string configSectionPath)
    {
        serviceCollection.ConfigureFiksIOClient(configSectionPath);
        return this;
    }

    /// <inheritdoc />
    public IFiksIOSetupBuilder WithResiliencePipeline(
        Action<ResiliencePipelineBuilder<FiksIOMessageResponse>, AddResiliencePipelineContext<string>> configure
    )
    {
        serviceCollection.AddResiliencePipeline<string, FiksIOMessageResponse>(
            FiksIOConstants.ResiliencePipelineId,
            configure
        );

        return this;
    }

    /// <inheritdoc />
    public IServiceCollection CompleteSetup()
    {
        return serviceCollection;
    }
}
