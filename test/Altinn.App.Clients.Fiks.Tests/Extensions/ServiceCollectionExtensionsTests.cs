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
    public void AddFiksIOClient_OverridesConfig_Delegate(bool provideDefaultSettings)
    {
        // Arrange
        var settingsOverride = TestFixture.GetRandomFiksIOSettings();
        using var fixture = TestFixture.Create(
            services =>
            {
                services
                    .AddFiksIOClient()
                    .WithConfig(x =>
                    {
                        x.AccountId = settingsOverride.AccountId;
                        x.IntegrationId = settingsOverride.IntegrationId;
                        x.IntegrationPassword = settingsOverride.IntegrationPassword;
                        x.AccountPrivateKeyBase64 = settingsOverride.AccountPrivateKeyBase64;
                        x.AsicePrivateKeyBase64 = settingsOverride.AsicePrivateKeyBase64;
                    });
            },
            useDefaultFiksIOSettings: provideDefaultSettings
        );

        // Act
        var fiksIOSettings = fixture.FiksIOSettings;

        // Assert
        Assert.NotNull(fiksIOSettings);
        Assert.Equal(settingsOverride, fiksIOSettings);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddFiksIOClient_OverridesConfig_JsonPath(bool provideDefaultSettings)
    {
        // Arrange
        var settingsOverride = TestFixture.GetRandomFiksIOSettings();
        using var fixture = TestFixture.Create(
            services =>
            {
                services.AddFiksIOClient().WithConfig("SuperCustomFiksIOSettings");
            },
            [("SuperCustomFiksIOSettings", settingsOverride)],
            useDefaultFiksIOSettings: provideDefaultSettings
        );

        // Act
        var fiksIOSettings = fixture.FiksIOSettings;

        // Assert
        Assert.NotNull(fiksIOSettings);
        Assert.Equal(settingsOverride, fiksIOSettings);
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
    public void AddFiksArkiv_OverridesFiksIOConfig_Delegate(bool provideDefaultSettings)
    {
        // Arrange
        var settingsOverride = TestFixture.GetRandomFiksIOSettings();
        using var fixture = TestFixture.Create(
            services =>
                services
                    .AddFiksArkiv()
                    .WithFiksIOConfig(x =>
                    {
                        x.AccountId = settingsOverride.AccountId;
                        x.IntegrationId = settingsOverride.IntegrationId;
                        x.IntegrationPassword = settingsOverride.IntegrationPassword;
                        x.AccountPrivateKeyBase64 = settingsOverride.AccountPrivateKeyBase64;
                        x.AsicePrivateKeyBase64 = settingsOverride.AsicePrivateKeyBase64;
                    }),
            useDefaultFiksIOSettings: provideDefaultSettings
        );

        // Act
        var fiksIOSettings = fixture.FiksIOSettings;

        // Assert
        Assert.NotNull(fiksIOSettings);
        Assert.Equal(settingsOverride, fiksIOSettings);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddFiksArkiv_OverridesFiksIOConfig_JsonPath(bool provideDefaultSettings)
    {
        // Arrange
        var settingsOverride1 = TestFixture.GetRandomFiksIOSettings();
        settingsOverride1.AccountPrivateKeyBase64 = null!;
        settingsOverride1.AsicePrivateKeyBase64 = null!;
        settingsOverride1.IntegrationPassword = null!;

        var settingsOverride2 = TestFixture.GetRandomFiksIOSettings();
        settingsOverride2.IntegrationId = Guid.Empty;
        settingsOverride2.AccountId = Guid.Empty;

        using var fixture = TestFixture.Create(
            services =>
            {
                services.AddFiksArkiv().WithFiksIOConfig("SuperCustomFiksIOSettings");
            },
            [("SuperCustomFiksIOSettings", settingsOverride1), ("SuperCustomFiksIOSettings", settingsOverride2)],
            useDefaultFiksIOSettings: provideDefaultSettings
        );

        // Act
        var fiksIOSettings = fixture.FiksIOSettings;

        // Assert
        Assert.NotNull(fiksIOSettings);
        Assert.Equal(settingsOverride1.IntegrationId, fiksIOSettings.IntegrationId);
        Assert.Equal(settingsOverride1.AccountId, fiksIOSettings.AccountId);
        Assert.Equal(settingsOverride2.IntegrationPassword, fiksIOSettings.IntegrationPassword);
        Assert.Equal(settingsOverride2.AccountPrivateKeyBase64, fiksIOSettings.AccountPrivateKeyBase64);
        Assert.Equal(settingsOverride2.AsicePrivateKeyBase64, fiksIOSettings.AsicePrivateKeyBase64);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddFiksArkiv_OverridesFiksArkivConfig_Delegate(bool provideDefaultSettings)
    {
        // Arrange
        var settingsOverride = TestFixture.GetRandomFiksArkivSettings();
        using var fixture = TestFixture.Create(
            services =>
                services
                    .AddFiksArkiv()
                    .WithFiksArkivConfig(x =>
                    {
                        x.AutoSend = settingsOverride.AutoSend;
                        x.ErrorHandling = settingsOverride.ErrorHandling;
                    }),
            useDefaultFiksArkivSettings: provideDefaultSettings
        );

        // Act
        var fiksArkivSettings = fixture.FiksArkivSettings;

        // Assert
        Assert.NotNull(fiksArkivSettings);
        Assert.Equal(settingsOverride, fiksArkivSettings);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddFiksArkiv_OverridesFiksArkivConfig_JsonPath(bool provideDefaultSettings)
    {
        // Arrange
        var errorHandlingOverride = TestFixture.GetRandomFiksArkivSettings();
        errorHandlingOverride.AutoSend = null;
        var autoSendOverride = TestFixture.GetRandomFiksArkivSettings();
        autoSendOverride.ErrorHandling = null;

        using var fixture = TestFixture.Create(
            services => services.AddFiksArkiv().WithFiksArkivConfig("SuperCustomFiksArkivSettings"),
            [
                ("SuperCustomFiksArkivSettings", errorHandlingOverride),
                ("SuperCustomFiksArkivSettings", autoSendOverride),
            ],
            useDefaultFiksArkivSettings: provideDefaultSettings
        );

        // Act
        var fiksArkivSettings = fixture.FiksArkivSettings;

        // Assert
        Assert.NotNull(fiksArkivSettings.AutoSend);
        Assert.NotNull(autoSendOverride.AutoSend);
        Assert.Equivalent(autoSendOverride.AutoSend.Attachments, fiksArkivSettings.AutoSend.Attachments);
        Assert.Equal(autoSendOverride.AutoSend.PrimaryDocument, fiksArkivSettings.AutoSend.PrimaryDocument);
        Assert.Equal(autoSendOverride.AutoSend.Recipient, fiksArkivSettings.AutoSend.Recipient);

        Assert.NotNull(fiksArkivSettings.ErrorHandling);
        Assert.NotNull(errorHandlingOverride.ErrorHandling);
        Assert.Equal(
            errorHandlingOverride.ErrorHandling.SendEmailNotifications,
            fiksArkivSettings.ErrorHandling.SendEmailNotifications
        );
        Assert.Equivalent(
            errorHandlingOverride.ErrorHandling.EmailNotificationRecipients,
            fiksArkivSettings.ErrorHandling.EmailNotificationRecipients
        );
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
