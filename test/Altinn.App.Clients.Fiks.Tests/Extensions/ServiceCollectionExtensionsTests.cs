using Altinn.App.Clients.Fiks.Constants;
using Altinn.App.Clients.Fiks.Extensions;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Microsoft.Extensions.Logging;
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
        var fixture = TestFixture.Create(services => services.AddFiksIOClient());

        // Act
        var fiksIOClient = fixture.FiksIOClient;
        var fiksIOSettings = fixture.FiksIOSettings;
        var resiliencePipeline = fixture.FiksIOResiliencePipeline;
        var resiliencePipelineDescriptor = resiliencePipeline.GetPipelineDescriptor();

        // Assert
        Assert.NotNull(fiksIOClient);
        Assert.NotNull(fiksIOSettings);
        Assert.NotNull(resiliencePipeline);
        Assert.Equal(TestFixture.GetDefaultFiksIOSettings(), fiksIOSettings);

        Assert.Equal(2, resiliencePipelineDescriptor.Strategies.Count);
        var retryOptions = Assert.IsType<RetryStrategyOptions<FiksIOMessageResponse>>(
            resiliencePipelineDescriptor.Strategies[0].Options
        );
        var timeoutOptions = Assert.IsType<TimeoutStrategyOptions>(resiliencePipelineDescriptor.Strategies[1].Options);
        Assert.Equal(3, retryOptions.MaxRetryAttempts);
        Assert.Equal(TimeSpan.FromSeconds(1), retryOptions.Delay);
        Assert.Equal(DelayBackoffType.Exponential, retryOptions.BackoffType);
        Assert.Equal(TimeSpan.FromSeconds(2), timeoutOptions.Timeout);
    }

    // TODO: Overriding resilience pipelines are not working.
    [Fact]
    public void AddFiksIOClient_AddsRequiredServicesWithOverrides()
    {
        // Arrange
        var pipelineOverride = (
            ResiliencePipelineBuilder<FiksIOMessageResponse> builder,
            AddResiliencePipelineContext<string> context
        ) =>
        {
            builder.AddRetry(new RetryStrategyOptions<FiksIOMessageResponse> { MaxRetryAttempts = 1 });
        };

        var settingsOverride = new FiksIOSettings
        {
            AccountId = Guid.NewGuid(),
            IntegrationId = Guid.NewGuid(),
            IntegrationPassword = "override-integration-password",
            AccountPrivateKeyBase64 = "override-account-pk-base64",
            AsicePrivateKeyBase64 = "override-asice-pk-base64",
        };

        var fixture = TestFixture.Create(services =>
        {
            services
                .AddFiksIOClient()
                .WithResiliencePipeline(pipelineOverride) // This is not working
                .WithConfig(x =>
                {
                    x.AccountId = settingsOverride.AccountId;
                    x.IntegrationId = settingsOverride.IntegrationId;
                    x.IntegrationPassword = settingsOverride.IntegrationPassword;
                    x.AccountPrivateKeyBase64 = settingsOverride.AccountPrivateKeyBase64;
                    x.AsicePrivateKeyBase64 = settingsOverride.AsicePrivateKeyBase64;
                });
        });

        // Act
        var fiksIOClient = fixture.FiksIOClient;
        var fiksIOSettings = fixture.FiksIOSettings;
        var resiliencePipeline = fixture.FiksIOResiliencePipeline;
        var resiliencePipelineDescriptor = resiliencePipeline.GetPipelineDescriptor();

        // Assert
        Assert.NotNull(fiksIOClient);
        Assert.NotNull(fiksIOSettings);
        Assert.NotNull(resiliencePipeline);
        Assert.Equal(settingsOverride, fiksIOSettings);

        Assert.Single(resiliencePipelineDescriptor.Strategies);
        var retryOptions = Assert.IsType<RetryStrategyOptions<FiksIOMessageResponse>>(
            resiliencePipelineDescriptor.Strategies[0].Options
        );
        Assert.Equal(1, retryOptions.MaxRetryAttempts);
    }
}
