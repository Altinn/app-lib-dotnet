using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Clients.Fiks.FiksIO;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.DependencyInjection;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

/// <summary>
/// Builder for configuring the Fiks Arkiv client behavior.
/// </summary>
public interface IFiksArkivSetupBuilder
{
    /// <summary>
    /// Configures the underlying Fiks IO client with the provided options.
    /// </summary>
    /// <param name="configureOptions">Configuration delegate.</param>
    /// <returns>The <see cref="IFiksArkivSetupBuilder"/> builder instance.</returns>
    IFiksArkivSetupBuilder WithFiksIOConfig(Action<FiksIOSettings> configureOptions);

    /// <summary>
    /// Configures the underlying Fiks IO client with the options from the specified configuration section.
    /// </summary>
    /// <param name="configSectionPath">Configuration section path.</param>
    /// <returns>The <see cref="IFiksArkivSetupBuilder"/> builder instance.</returns>
    IFiksArkivSetupBuilder WithFiksIOConfig(string configSectionPath);

    /// <summary>
    /// Configures the Fiks Arkiv client with the provided options.
    /// </summary>
    /// <param name="configureOptions">Configuration delegate.</param>
    /// <returns>The <see cref="IFiksArkivSetupBuilder"/> builder instance.</returns>
    IFiksArkivSetupBuilder WithFiksArkivConfig(Action<FiksArkivSettings> configureOptions);

    /// <summary>
    /// Configures the Fiks Arkiv client with the options from the specified configuration section.
    /// </summary>
    /// <param name="configSectionPath">Configuration section path.</param>
    /// <returns>The <see cref="IFiksArkivSetupBuilder"/> builder instance.</returns>
    IFiksArkivSetupBuilder WithFiksArkivConfig(string configSectionPath);

    /// <summary>
    /// Configures the message handler for the Fiks Arkiv client.
    /// The message handler is responsible for composing message requests and handling received messages.
    /// </summary>
    /// <typeparam name="TMessageHandler">The message handler type you wish to register for use.</typeparam>
    /// <returns>The <see cref="IFiksArkivSetupBuilder"/> builder instance.</returns>
    IFiksArkivSetupBuilder WithMessageHandler<TMessageHandler>()
        where TMessageHandler : IFiksArkivMessageHandler;

    /// <summary>
    /// Configures the resilience pipeline (retry behavior) for the Fiks IO client.
    /// </summary>
    /// <param name="configure">Configuration delegate.</param>
    /// <returns>The <see cref="IFiksIOSetupBuilder"/> builder instance.</returns>
    IFiksArkivSetupBuilder WithResiliencePipeline(
        Action<ResiliencePipelineBuilder<FiksIOMessageResponse>, AddResiliencePipelineContext<string>> configure
    );

    /// <summary>
    /// Completes the setup and returns the service collection.
    /// </summary>
    IServiceCollection CompleteSetup();
}
