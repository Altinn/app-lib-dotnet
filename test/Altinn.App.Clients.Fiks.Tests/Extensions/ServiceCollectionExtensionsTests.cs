using Altinn.App.Clients.Fiks.Extensions;
using Altinn.App.Clients.Fiks.FiksArkiv;
using Altinn.App.Clients.Fiks.FiksIO;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.App.Core.Internal.AltinnCdn;
using Polly;
using Polly.DependencyInjection;
using Polly.Retry;
using Polly.Testing;
using Polly.Timeout;

namespace Altinn.App.Clients.Fiks.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFiksIOClient_AddsRequiredServicesWithDefaultValues()
    {
        // Arrange
        using var fixture = TestFixture.Create(services => services.AddFiksIOClient());

        // Act
        var fiksIOClient = fixture.FiksIOClient;
        var fiksIOSettings = fixture.FiksIOSettings;
        var resiliencePipeline = fixture.FiksIOResiliencePipeline;

        // Assert
        Assert.NotNull(fiksIOClient);
        Assert.NotNull(fiksIOSettings);
        Assert.NotNull(resiliencePipeline);
        Assert.IsType<FiksIOClient>(fiksIOClient);
        Assert.Equal(TestFixture.GetDefaultFiksIOSettings(), fiksIOSettings);

        AssertDefaultResiliencePipeline(resiliencePipeline);
    }

    [Fact]
    public void AddFiksIOClient_OverridesResiliencePipeline()
    {
        // Arrange
        var pipelineOverride = (
            ResiliencePipelineBuilder<FiksIOMessageResponse> builder,
            AddResiliencePipelineContext<string> context
        ) =>
        {
            builder.AddRetry(new RetryStrategyOptions<FiksIOMessageResponse> { MaxRetryAttempts = 1 });
        };

        using var fixture = TestFixture.Create(services =>
        {
            services.AddFiksIOClient().WithResiliencePipeline(pipelineOverride);
        });

        // Act
        var resiliencePipeline = fixture.FiksIOResiliencePipeline;
        var resiliencePipelineDescriptor = resiliencePipeline.GetPipelineDescriptor();

        // Assert
        Assert.NotNull(resiliencePipeline);
        Assert.Single(resiliencePipelineDescriptor.Strategies);
        var retryOptions = Assert.IsType<RetryStrategyOptions<FiksIOMessageResponse>>(
            resiliencePipelineDescriptor.Strategies[0].Options
        );
        Assert.Equal(1, retryOptions.MaxRetryAttempts);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddFiksIOClient_OverridesConfig_Delegates(bool provideDefaultSettings)
    {
        // Arrange
        var fiksIOSettingsOverride = TestFixture.GetRandomFiksIOSettings();
        var maskinportenSettingsOverride = TestFixture.GetRandomMaskinportenSettings();
        using var fixture = TestFixture.Create(
            services =>
            {
                services
                    .AddFiksIOClient()
                    .WithFiksIOConfig(x =>
                    {
                        x.AccountId = fiksIOSettingsOverride.AccountId;
                        x.IntegrationId = fiksIOSettingsOverride.IntegrationId;
                        x.IntegrationPassword = fiksIOSettingsOverride.IntegrationPassword;
                        x.AccountPrivateKeyBase64 = fiksIOSettingsOverride.AccountPrivateKeyBase64;
                        x.AsicePrivateKeyBase64 = fiksIOSettingsOverride.AsicePrivateKeyBase64;
                    })
                    .WithMaskinportenConfig(x =>
                    {
                        x.Authority = maskinportenSettingsOverride.Authority;
                        x.ClientId = maskinportenSettingsOverride.ClientId;
                        x.JwkBase64 = maskinportenSettingsOverride.JwkBase64;
                    });
            },
            useDefaultFiksIOSettings: provideDefaultSettings,
            useDefaultMaskinportenSettings: provideDefaultSettings
        );

        // Act
        var fiksIOSettings = fixture.FiksIOSettings;
        var maskinportenSettings = fixture.MaskinportenSettings;

        // Assert
        Assert.NotNull(fiksIOSettings);
        Assert.NotNull(maskinportenSettings);
        Assert.Equal(fiksIOSettingsOverride, fiksIOSettings);
        Assert.Equal(maskinportenSettingsOverride, maskinportenSettings);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddFiksIOClient_OverridesConfig_JsonPaths(bool provideDefaultSettings)
    {
        // Arrange
        var fiksIOSettingsOverride = TestFixture.GetRandomFiksIOSettings();
        var maskinportenSettingsOverride = TestFixture.GetRandomMaskinportenSettings();
        using var fixture = TestFixture.Create(
            services =>
            {
                services
                    .AddFiksIOClient()
                    .WithFiksIOConfig("SuperCustomFiksIOSettings")
                    .WithMaskinportenConfig("SuperCustomMaskinportenSettings");
            },
            [
                ("SuperCustomFiksIOSettings", fiksIOSettingsOverride),
                ("SuperCustomMaskinportenSettings", maskinportenSettingsOverride),
            ],
            useDefaultFiksIOSettings: provideDefaultSettings,
            useDefaultMaskinportenSettings: provideDefaultSettings
        );

        // Act
        var fiksIOSettings = fixture.FiksIOSettings;
        var maskinportenSettings = fixture.MaskinportenSettings;

        // Assert
        Assert.NotNull(fiksIOSettings);
        Assert.NotNull(maskinportenSettings);
        Assert.Equal(fiksIOSettingsOverride, fiksIOSettings);
        Assert.Equal(maskinportenSettingsOverride, maskinportenSettings);
    }

    [Fact]
    public void AddFiksArkiv_AddsRequiredServicesWithDefaultValues()
    {
        // Arrange
        using var fixture = TestFixture.Create(services => services.AddFiksArkiv());

        // Act
        var fiksIOClient = fixture.FiksIOClient;
        var fiksIOSettings = fixture.FiksIOSettings;
        var resiliencePipeline = fixture.FiksIOResiliencePipeline;
        var altinnCdnClient = fixture.AltinnCdnClient;
        var fiksArkivMessageHandler = fixture.FiksArkivMessageHandler;
        var fiksArkivServiceTask = fixture.FiksArkivServiceTask;
        var fiksArkivConfigValidationService = fixture.FiksArkivConfigValidationService;
        var fiksArkivEventService = fixture.FiksArkivEventService;

        // Assert
        Assert.NotNull(fiksIOClient);
        Assert.NotNull(fiksIOSettings);
        Assert.NotNull(resiliencePipeline);
        Assert.NotNull(altinnCdnClient);
        Assert.NotNull(fiksArkivMessageHandler);
        Assert.NotNull(fiksArkivServiceTask);
        Assert.NotNull(fiksArkivConfigValidationService);
        Assert.NotNull(fiksArkivEventService);
        Assert.Equal(TestFixture.GetDefaultFiksIOSettings(), fiksIOSettings);
        Assert.IsType<FiksIOClient>(fiksIOClient);
        Assert.IsType<FiksArkivDefaultMessageHandler>(fiksArkivMessageHandler);
        Assert.IsType<FiksArkivServiceTask>(fiksArkivServiceTask);
        Assert.IsType<AltinnCdnClient>(altinnCdnClient);

        AssertDefaultResiliencePipeline(resiliencePipeline);
    }

    [Fact]
    public void AddFiksArkiv_OverridesResiliencePipeline()
    {
        // Arrange
        var pipelineOverride = (
            ResiliencePipelineBuilder<FiksIOMessageResponse> builder,
            AddResiliencePipelineContext<string> context
        ) =>
        {
            builder.AddRetry(new RetryStrategyOptions<FiksIOMessageResponse> { MaxRetryAttempts = 1 });
        };

        using var fixture = TestFixture.Create(services =>
        {
            services.AddFiksArkiv().WithResiliencePipeline(pipelineOverride);
        });

        // Act
        var resiliencePipeline = fixture.FiksIOResiliencePipeline;
        var resiliencePipelineDescriptor = resiliencePipeline.GetPipelineDescriptor();

        // Assert
        Assert.NotNull(resiliencePipeline);
        Assert.Single(resiliencePipelineDescriptor.Strategies);
        var retryOptions = Assert.IsType<RetryStrategyOptions<FiksIOMessageResponse>>(
            resiliencePipelineDescriptor.Strategies[0].Options
        );
        Assert.Equal(1, retryOptions.MaxRetryAttempts);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddFiksArkiv_OverridesConfig_Delegates(bool provideDefaultSettings)
    {
        // Arrange
        var fiksIOSettingsOverride = TestFixture.GetRandomFiksIOSettings();
        var fiksArkivSettingsOverride = TestFixture.GetRandomFiksArkivSettings();
        var maskinportenSettingsOverride = TestFixture.GetRandomMaskinportenSettings();
        using var fixture = TestFixture.Create(
            services =>
                services
                    .AddFiksArkiv()
                    .WithFiksIOConfig(x =>
                    {
                        x.AccountId = fiksIOSettingsOverride.AccountId;
                        x.IntegrationId = fiksIOSettingsOverride.IntegrationId;
                        x.IntegrationPassword = fiksIOSettingsOverride.IntegrationPassword;
                        x.AccountPrivateKeyBase64 = fiksIOSettingsOverride.AccountPrivateKeyBase64;
                        x.AsicePrivateKeyBase64 = fiksIOSettingsOverride.AsicePrivateKeyBase64;
                    })
                    .WithFiksArkivConfig(x =>
                    {
                        x.AutoSend = fiksArkivSettingsOverride.AutoSend;
                        x.ErrorHandling = fiksArkivSettingsOverride.ErrorHandling;
                        x.Documents = fiksArkivSettingsOverride.Documents;
                        x.Recipient = fiksArkivSettingsOverride.Recipient;
                        x.Receipt = fiksArkivSettingsOverride.Receipt;
                    })
                    .WithMaskinportenConfig(x =>
                    {
                        x.Authority = maskinportenSettingsOverride.Authority;
                        x.ClientId = maskinportenSettingsOverride.ClientId;
                        x.JwkBase64 = maskinportenSettingsOverride.JwkBase64;
                    }),
            useDefaultFiksIOSettings: provideDefaultSettings,
            useDefaultFiksArkivSettings: provideDefaultSettings,
            useDefaultMaskinportenSettings: provideDefaultSettings
        );

        // Act
        var fiksIOSettings = fixture.FiksIOSettings;
        var fiksArkivSettings = fixture.FiksArkivSettings;
        var maskinportenSettings = fixture.MaskinportenSettings;

        // Assert
        Assert.NotNull(fiksIOSettings);
        Assert.NotNull(fiksArkivSettings);
        Assert.NotNull(maskinportenSettings);
        Assert.Equivalent(fiksArkivSettingsOverride, fiksArkivSettings);
        Assert.Equal(fiksIOSettingsOverride, fiksIOSettings);
        Assert.Equal(maskinportenSettingsOverride, maskinportenSettings);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddFiksArkiv_OverridesConfig_JsonPaths(bool provideDefaultSettings)
    {
        // Arrange
        var fiksIOSettingsOverride = TestFixture.GetRandomFiksIOSettings();
        var fiksArkivSettingsOverride = TestFixture.GetRandomFiksArkivSettings();
        var maskinportenSettingsOverride = TestFixture.GetRandomMaskinportenSettings();
        using var fixture = TestFixture.Create(
            services =>
                services
                    .AddFiksArkiv()
                    .WithFiksIOConfig("SuperCustomFiksIOSettings")
                    .WithFiksArkivConfig("SuperCustomFiksArkivSettings")
                    .WithMaskinportenConfig("SuperCustomMaskinportenSettings"),
            [
                ("SuperCustomFiksIOSettings", fiksIOSettingsOverride),
                ("SuperCustomFiksArkivSettings", fiksArkivSettingsOverride),
                ("SuperCustomMaskinportenSettings", maskinportenSettingsOverride),
            ],
            useDefaultFiksIOSettings: provideDefaultSettings,
            useDefaultFiksArkivSettings: provideDefaultSettings,
            useDefaultMaskinportenSettings: provideDefaultSettings
        );

        // Act
        var fiksIOSettings = fixture.FiksIOSettings;
        var fiksArkivSettings = fixture.FiksArkivSettings;
        var maskinportenSettings = fixture.MaskinportenSettings;

        // Assert
        Assert.NotNull(fiksIOSettings);
        Assert.NotNull(fiksArkivSettings);
        Assert.NotNull(maskinportenSettings);
        Assert.Equivalent(fiksArkivSettingsOverride, fiksArkivSettings);
        Assert.Equal(fiksIOSettingsOverride, fiksIOSettings);
        Assert.Equal(maskinportenSettingsOverride, maskinportenSettings);
    }

    [Fact]
    public void AddFiksArkiv_OverridesMessageHandler()
    {
        // Arrange
        using var fixture = TestFixture.Create(services =>
            services.AddFiksArkiv().WithMessageHandler<TestFixture.CustomFiksArkivMessageHandler>()
        );

        // Act
        var fiksArkivMessageHandler = fixture.FiksArkivMessageHandler;

        // Assert
        Assert.NotNull(fiksArkivMessageHandler);
        Assert.IsType<TestFixture.CustomFiksArkivMessageHandler>(fiksArkivMessageHandler);
    }

    [Fact]
    public void AddFiksArkiv_OverridesAutoSendDecision()
    {
        // Arrange
        using var fixture = TestFixture.Create(services =>
            services.AddFiksArkiv().WithAutoSendDecision<TestFixture.CustomAutoSendDecision>()
        );

        // Act
        var fiksArkivAutoSendDecisionHandler = fixture.FiksArkivAutoSendDecisionHandler;

        // Assert
        Assert.NotNull(fiksArkivAutoSendDecisionHandler);
        Assert.IsType<TestFixture.CustomAutoSendDecision>(fiksArkivAutoSendDecisionHandler);
    }

    private static void AssertDefaultResiliencePipeline(ResiliencePipeline<FiksIOMessageResponse> pipeline)
    {
        var pipelineDescriptor = pipeline.GetPipelineDescriptor();

        Assert.Equal(2, pipelineDescriptor.Strategies.Count);
        var retryOptions = Assert.IsType<RetryStrategyOptions<FiksIOMessageResponse>>(
            pipelineDescriptor.Strategies[0].Options
        );
        var timeoutOptions = Assert.IsType<TimeoutStrategyOptions>(pipelineDescriptor.Strategies[1].Options);
        Assert.Equal(3, retryOptions.MaxRetryAttempts);
        Assert.Equal(TimeSpan.FromSeconds(1), retryOptions.Delay);
        Assert.Equal(DelayBackoffType.Exponential, retryOptions.BackoffType);
        Assert.Equal(TimeSpan.FromSeconds(2), timeoutOptions.Timeout);
    }
}
