using Altinn.App.Clients.Fiks.FiksIO.Models;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.DependencyInjection;

namespace Altinn.App.Clients.Fiks.FiksIO;

/// <summary>
/// Builder for configuring the Fiks IO client behavior.
/// </summary>
public interface IFiksIOSetupBuilder
{
    /// <summary>
    /// Configures the Fiks IO client with the provided options.
    /// </summary>
    /// <param name="configureOptions">Configuration delegate.</param>
    /// <returns>The <see cref="IFiksIOSetupBuilder"/> builder instance.</returns>
    IFiksIOSetupBuilder WithConfig(Action<FiksIOSettings> configureOptions);

    /// <summary>
    /// Configures the Fiks IO client with the options from the specified configuration section.
    /// </summary>
    /// <param name="configSectionPath">Configuration section path.</param>
    /// <returns>The <see cref="IFiksIOSetupBuilder"/> builder instance.</returns>
    IFiksIOSetupBuilder WithConfig(string configSectionPath);

    /// <summary>
    /// Configures the resilience pipeline (retry behavior) for the Fiks IO client.
    /// </summary>
    /// <param name="configure">Configuration delegate.</param>
    /// <returns>The <see cref="IFiksIOSetupBuilder"/> builder instance.</returns>
    IFiksIOSetupBuilder WithResiliencePipeline(
        Action<ResiliencePipelineBuilder<FiksIOMessageResponse>, AddResiliencePipelineContext<string>> configure
    );

    /// <summary>
    /// Completes the setup and returns the service collection.
    /// </summary>
    IServiceCollection CompleteSetup();
}
