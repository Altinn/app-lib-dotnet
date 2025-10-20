using Altinn.App.Clients.Fiks.Constants;
using Altinn.App.Clients.Fiks.Extensions;
using Altinn.App.Clients.Fiks.FiksArkiv;
using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.App.Core.Features.Maskinporten.Extensions;
using Altinn.App.Core.Features.Maskinporten.Models;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.DependencyInjection;

namespace Altinn.App.Clients.Fiks.Configuration;

internal abstract class FiksSetupBuilder(IServiceCollection services)
{
    protected FiksSetupBuilder ConfigureFiksIO(Action<FiksIOSettings> configureOptions)
    {
        services.ConfigureFiksIOClient(configureOptions);
        return this;
    }

    protected FiksSetupBuilder ConfigureFiksIO(string configSectionPath)
    {
        services.ConfigureFiksIOClient(configSectionPath);
        return this;
    }

    protected FiksSetupBuilder ConfigureFiksArkiv(Action<FiksArkivSettings> configureOptions)
    {
        services.ConfigureFiksArkiv(configureOptions);
        return this;
    }

    protected FiksSetupBuilder ConfigureFiksArkiv(string configSectionPath)
    {
        services.ConfigureFiksArkiv(configSectionPath);
        return this;
    }

    protected FiksSetupBuilder ConfigureMaskinporten(Action<MaskinportenSettings> configureOptions)
    {
        services.ConfigureMaskinportenClient(configureOptions);
        return this;
    }

    protected FiksSetupBuilder ConfigureMaskinporten(string configSectionPath)
    {
        services.ConfigureMaskinportenClient(configSectionPath);
        return this;
    }

    protected FiksSetupBuilder ConfigureMessageResponseHandler<TMessageHandler>()
        where TMessageHandler : IFiksArkivResponseHandler
    {
        services.AddTransient(typeof(IFiksArkivResponseHandler), typeof(TMessageHandler));
        return this;
    }

    protected FiksSetupBuilder ConfigureMessagePayloadGenerator<TMessageHandler>()
        where TMessageHandler : IFiksArkivPayloadGenerator
    {
        services.AddTransient(typeof(IFiksArkivPayloadGenerator), typeof(TMessageHandler));
        return this;
    }

    protected FiksSetupBuilder ConfigureAutoDecision<TAutoSendDecision>()
        where TAutoSendDecision : IFiksArkivAutoSendDecision
    {
        services.AddTransient(typeof(IFiksArkivAutoSendDecision), typeof(TAutoSendDecision));
        return this;
    }

    protected FiksSetupBuilder ConfigureResiliencePipeline(
        Action<ResiliencePipelineBuilder<FiksIOMessageResponse>, AddResiliencePipelineContext<string>> configure
    )
    {
        services.AddResiliencePipeline(FiksIOConstants.UserDefinedResiliencePipelineId, configure);
        return this;
    }

    public IServiceCollection CompleteSetup() => services;
}

internal sealed class FiksIOSetupBuilder(IServiceCollection services) : FiksSetupBuilder(services), IFiksIOSetupBuilder
{
    /// <inheritdoc />
    public IFiksIOSetupBuilder WithFiksIOConfig(Action<FiksIOSettings> configureOptions) =>
        (IFiksIOSetupBuilder)ConfigureFiksIO(configureOptions);

    /// <inheritdoc />
    public IFiksIOSetupBuilder WithFiksIOConfig(string configSectionPath) =>
        (IFiksIOSetupBuilder)ConfigureFiksIO(configSectionPath);

    /// <inheritdoc />
    public IFiksIOSetupBuilder WithMaskinportenConfig(Action<MaskinportenSettings> configureOptions) =>
        (IFiksIOSetupBuilder)ConfigureMaskinporten(configureOptions);

    /// <inheritdoc />
    public IFiksIOSetupBuilder WithMaskinportenConfig(string configSectionPath) =>
        (IFiksIOSetupBuilder)ConfigureMaskinporten(configSectionPath);

    /// <inheritdoc />
    public IFiksIOSetupBuilder WithResiliencePipeline(
        Action<ResiliencePipelineBuilder<FiksIOMessageResponse>, AddResiliencePipelineContext<string>> configure
    ) => (IFiksIOSetupBuilder)ConfigureResiliencePipeline(configure);
}

internal sealed class FiksArkivSetupBuilder(IServiceCollection services)
    : FiksSetupBuilder(services),
        IFiksArkivSetupBuilder
{
    /// <inheritdoc />
    public IFiksArkivSetupBuilder WithFiksIOConfig(string configSectionPath) =>
        (IFiksArkivSetupBuilder)ConfigureFiksIO(configSectionPath);

    /// <inheritdoc />
    public IFiksArkivSetupBuilder WithFiksIOConfig(Action<FiksIOSettings> configureOptions) =>
        (IFiksArkivSetupBuilder)ConfigureFiksIO(configureOptions);

    /// <inheritdoc />
    public IFiksArkivSetupBuilder WithMaskinportenConfig(Action<MaskinportenSettings> configureOptions) =>
        (IFiksArkivSetupBuilder)ConfigureMaskinporten(configureOptions);

    /// <inheritdoc />
    public IFiksArkivSetupBuilder WithMaskinportenConfig(string configSectionPath) =>
        (IFiksArkivSetupBuilder)ConfigureMaskinporten(configSectionPath);

    /// <inheritdoc />
    public IFiksArkivSetupBuilder WithResiliencePipeline(
        Action<ResiliencePipelineBuilder<FiksIOMessageResponse>, AddResiliencePipelineContext<string>> configure
    ) => (IFiksArkivSetupBuilder)ConfigureResiliencePipeline(configure);

    /// <inheritdoc />
    public IFiksArkivSetupBuilder WithFiksArkivConfig(Action<FiksArkivSettings> configureOptions) =>
        (IFiksArkivSetupBuilder)ConfigureFiksArkiv(configureOptions);

    /// <inheritdoc />
    public IFiksArkivSetupBuilder WithFiksArkivConfig(string configSectionPath) =>
        (IFiksArkivSetupBuilder)ConfigureFiksArkiv(configSectionPath);

    /// <inheritdoc />
    public IFiksArkivSetupBuilder WithResponseHandler<TMessageHandler>()
        where TMessageHandler : IFiksArkivResponseHandler =>
        (IFiksArkivSetupBuilder)ConfigureMessageResponseHandler<TMessageHandler>();

    /// <inheritdoc />
    public IFiksArkivSetupBuilder WithPayloadGenerator<TMessageHandler>()
        where TMessageHandler : IFiksArkivPayloadGenerator =>
        (IFiksArkivSetupBuilder)ConfigureMessagePayloadGenerator<TMessageHandler>();

    /// <inheritdoc />
    public IFiksArkivSetupBuilder WithAutoSendDecision<TAutoSendDecision>()
        where TAutoSendDecision : IFiksArkivAutoSendDecision =>
        (IFiksArkivSetupBuilder)ConfigureAutoDecision<TAutoSendDecision>();
}
