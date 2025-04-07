using Altinn.App.Clients.Fiks.Constants;
using Altinn.App.Clients.Fiks.Extensions;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.App.Core.Features.Maskinporten.Extensions;
using Altinn.App.Core.Features.Maskinporten.Models;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.DependencyInjection;

namespace Altinn.App.Clients.Fiks.FiksIO;

internal sealed class FiksIOSetupBuilder(IServiceCollection services) : IFiksIOSetupBuilder
{
    /// <inheritdoc />
    public IFiksIOSetupBuilder WithFiksIOConfig(Action<FiksIOSettings> configureOptions)
    {
        services.ConfigureFiksIOClient(configureOptions);
        return this;
    }

    /// <inheritdoc />
    public IFiksIOSetupBuilder WithFiksIOConfig(string configSectionPath)
    {
        services.ConfigureFiksIOClient(configSectionPath);
        return this;
    }

    /// <inheritdoc />
    public IFiksIOSetupBuilder WithMaskinportenConfig(Action<MaskinportenSettings> configureOptions)
    {
        services.ConfigureMaskinportenClient(configureOptions);
        return this;
    }

    /// <inheritdoc />
    public IFiksIOSetupBuilder WithMaskinportenConfig(string configSectionPath)
    {
        services.ConfigureMaskinportenClient(configSectionPath);
        return this;
    }

    /// <inheritdoc />
    public IFiksIOSetupBuilder WithResiliencePipeline(
        Action<ResiliencePipelineBuilder<FiksIOMessageResponse>, AddResiliencePipelineContext<string>> configure
    )
    {
        services.AddResiliencePipeline<string, FiksIOMessageResponse>(
            FiksIOConstants.UserDefinedResiliencePipelineId,
            configure
        );
        return this;
    }

    /// <inheritdoc />
    public IServiceCollection CompleteSetup()
    {
        return services;
    }
}
