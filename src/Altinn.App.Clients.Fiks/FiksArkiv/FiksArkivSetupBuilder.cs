using Altinn.App.Clients.Fiks.Constants;
using Altinn.App.Clients.Fiks.Extensions;
using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.DependencyInjection;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal sealed class FiksArkivSetupBuilder(IServiceCollection services) : IFiksArkivSetupBuilder
{
    /// <inheritdoc />
    public IFiksArkivSetupBuilder WithFiksIOConfig(Action<FiksIOSettings> configureOptions)
    {
        services.ConfigureFiksIOClient(configureOptions);
        return this;
    }

    /// <inheritdoc />
    public IFiksArkivSetupBuilder WithFiksIOConfig(string configSectionPath)
    {
        services.ConfigureFiksIOClient(configSectionPath);
        return this;
    }

    /// <inheritdoc />
    public IFiksArkivSetupBuilder WithFiksArkivConfig(Action<FiksArkivSettings> configureOptions)
    {
        services.ConfigureFiksArkiv(configureOptions);
        return this;
    }

    /// <inheritdoc />
    public IFiksArkivSetupBuilder WithFiksArkivConfig(string configSectionPath)
    {
        services.ConfigureFiksArkiv(configSectionPath);
        return this;
    }

    /// <inheritdoc />
    public IFiksArkivSetupBuilder WithMessageHandler<TMessageHandler>()
        where TMessageHandler : IFiksArkivMessageHandler
    {
        services.AddTransient(typeof(IFiksArkivMessageHandler), typeof(TMessageHandler));
        return this;
    }

    /// <inheritdoc />
    public IFiksArkivSetupBuilder WithResiliencePipeline(
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
