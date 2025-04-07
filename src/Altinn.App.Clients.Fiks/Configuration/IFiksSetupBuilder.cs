using Altinn.App.Clients.Fiks.FiksArkiv;
using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.App.Core.Features.Maskinporten.Models;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.DependencyInjection;

namespace Altinn.App.Clients.Fiks.Configuration;

/// <summary>
/// Builder for configuring common Fiks behavior.
/// </summary>
public interface IFiksSetupBuilder<out T>
{
    /// <summary>
    /// Configures the Fiks IO client with the provided options.
    /// </summary>
    /// <param name="configureOptions">Configuration delegate.</param>
    /// <returns>The builder instance.</returns>
    T WithFiksIOConfig(Action<FiksIOSettings> configureOptions);

    /// <summary>
    /// Configures the Fiks IO client with the options from the specified configuration section.
    /// </summary>
    /// <param name="configSectionPath">Configuration section path.</param>
    /// <returns>The builder instance.</returns>
    T WithFiksIOConfig(string configSectionPath);

    /// <summary>
    /// Configures the underlying Maskinporten client with the provided options.
    /// </summary>
    /// <param name="configureOptions">Configuration delegate.</param>
    /// <returns>The builder instance.</returns>
    T WithMaskinportenConfig(Action<MaskinportenSettings> configureOptions);

    /// <summary>
    /// Configures the underlying Maskinporten client with the options from the specified configuration section.
    /// </summary>
    /// <param name="configSectionPath">Configuration section path.</param>
    /// <returns>The builder instance.</returns>
    T WithMaskinportenConfig(string configSectionPath);

    /// <summary>
    /// Configures the resilience pipeline (retry behavior) for the Fiks IO client.
    /// </summary>
    /// <param name="configure">Configuration delegate.</param>
    /// <returns>The builder instance.</returns>
    T WithResiliencePipeline(
        Action<ResiliencePipelineBuilder<FiksIOMessageResponse>, AddResiliencePipelineContext<string>> configure
    );

    /// <summary>
    /// Completes the setup and returns the service collection.
    /// </summary>
    IServiceCollection CompleteSetup();
}

public interface IFiksSetupBuilderBase : IFiksSetupBuilder<IFiksIOSetupBuilder> { }

/// <summary>
/// Builder for configuring the Fiks IO client behavior.
/// </summary>
public interface IFiksIOSetupBuilder : IFiksSetupBuilder<IFiksIOSetupBuilder> { }

/// <summary>
/// Builder for configuring the Fiks Arkiv client behavior.
/// </summary>
public interface IFiksArkivSetupBuilder : IFiksSetupBuilder<IFiksArkivSetupBuilder>
{
    /// <summary>
    /// Configures the Fiks Arkiv client with the provided options.
    /// </summary>
    /// <param name="configureOptions">Configuration delegate.</param>
    /// <returns>The builder instance.</returns>
    IFiksArkivSetupBuilder WithFiksArkivConfig(Action<FiksArkivSettings> configureOptions);

    /// <summary>
    /// Configures the Fiks Arkiv client with the options from the specified configuration section.
    /// </summary>
    /// <param name="configSectionPath">Configuration section path.</param>
    /// <returns>The builder instance.</returns>
    IFiksArkivSetupBuilder WithFiksArkivConfig(string configSectionPath);

    /// <summary>
    /// Configures the message handler for the Fiks Arkiv client.
    /// The message handler is responsible for composing message requests and handling received messages.
    /// </summary>
    /// <typeparam name="TMessageHandler">The message handler type you wish to register for use.</typeparam>
    /// <returns>The builder instance.</returns>
    IFiksArkivSetupBuilder WithMessageHandler<TMessageHandler>()
        where TMessageHandler : IFiksArkivMessageHandler;

    /// <summary>
    /// Configures the auto-send decision handler for the Fiks Arkiv client.
    /// This handler is responsible for determining whether a Fiks Arkiv message should be sent or not.
    /// </summary>
    /// <typeparam name="TAutoSendDecision">The auto-send decision handler you wish to register for use.</typeparam>
    /// <returns>The builder instance.</returns>
    IFiksArkivSetupBuilder WithAutoSendDecision<TAutoSendDecision>()
        where TAutoSendDecision : IFiksArkivAutoSendDecision;
}
