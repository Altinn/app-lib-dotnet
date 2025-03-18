using Altinn.App.Clients.Fiks.FiksArkiv;
using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Clients.Fiks.FiksIO;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.App.Core.Extensions;
using Altinn.App.Core.Features.Maskinporten.Exceptions;
using Altinn.App.Core.Internal.AltinnCdn;
using Altinn.App.Core.Internal.Process.ServiceTasks;
using KS.Fiks.IO.Send.Client.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Altinn.App.Clients.Fiks.Extensions;

public static class ServiceCollectionExtensions
{
    public static IFiksIOSetupBuilder AddFiksIOClient(this IServiceCollection services)
    {
        if (services.IsConfigured<FiksIOSettings>() is false)
            services.ConfigureFiksIOClient("FiksIOSettings");

        services.AddTransient<IFiksIOClient, FiksIOClient>();
        services.AddDefaultFiksIOResiliencePipeline();

        return new FiksIOSetupBuilder(services);
    }

    public static IFiksArkivSetupBuilder AddFiksArkiv(this IServiceCollection services)
    {
        if (services.IsConfigured<FiksArkivSettings>() is false)
        {
            services.ConfigureFiksArkiv("FiksArkivSettings");
        }

        services.AddFiksIOClient();
        services.AddTransient<IAltinnCdnClient, AltinnCdnClient>();
        services.AddTransient<IFiksArkivMessageHandler, FiksArkivDefaultMessageHandler>();
        services.AddTransient<IFiksArkivServiceTask, FiksArkivServiceTask>();
        services.AddTransient<IServiceTask>(x => x.GetRequiredService<IFiksArkivServiceTask>());
        services.AddHostedService<FiksArkivConfigValidationService>();
        services.AddHostedService<FiksArkivEventService>();

        return new FiksArkivSetupBuilder(services);
    }

    public static IServiceCollection ConfigureFiksIOClient(
        this IServiceCollection services,
        Action<FiksIOSettings> configureOptions
    )
    {
        services.AddOptions<FiksIOSettings>().Configure(configureOptions).ValidateDataAnnotations();
        return services;
    }

    public static IServiceCollection ConfigureFiksIOClient(this IServiceCollection services, string configSectionPath)
    {
        services.AddOptions<FiksIOSettings>().BindConfiguration(configSectionPath).ValidateDataAnnotations();
        return services;
    }

    public static IServiceCollection ConfigureFiksArkiv(
        this IServiceCollection services,
        Action<FiksArkivSettings> configureOptions
    )
    {
        services.AddOptions<FiksArkivSettings>().Configure(configureOptions);
        return services;
    }

    public static IServiceCollection ConfigureFiksArkiv(this IServiceCollection services, string configSectionPath)
    {
        services.AddOptions<FiksArkivSettings>().BindConfiguration(configSectionPath);
        return services;
    }

    internal static IServiceCollection AddDefaultFiksIOResiliencePipeline(this IServiceCollection services)
    {
        services.AddResiliencePipeline<string, FiksIOMessageResponse>(
            FiksIOConstants.ResiliencePipelineId,
            (builder, context) =>
            {
                var logger = context.ServiceProvider.GetRequiredService<ILogger<FiksIOClient>>();

                builder
                    .AddRetry(
                        new RetryStrategyOptions<FiksIOMessageResponse>
                        {
                            MaxRetryAttempts = 3,
                            Delay = TimeSpan.FromSeconds(1),
                            BackoffType = DelayBackoffType.Exponential,
                            ShouldHandle = new PredicateBuilder<FiksIOMessageResponse>().Handle<Exception>(ex =>
                            {
                                var shouldHandle = ErrorShouldBeHandled(ex);
                                if (shouldHandle is false)
                                    logger.LogInformation(
                                        ex,
                                        "Error is unrecoverable and will not be retried: {Exception}",
                                        ex.Message
                                    );

                                return shouldHandle;
                            }),
                            OnRetry = args =>
                            {
                                args.Context.Properties.TryGetValue(
                                    new ResiliencePropertyKey<FiksIOMessageRequest>(
                                        FiksIOConstants.MessageRequestPropertyKey
                                    ),
                                    out var messageRequest
                                );
                                logger.LogWarning(
                                    args.Outcome.Exception,
                                    "Failed to send FiksIO message {MessageType}:{ClientMessageId}. Retrying in {RetryDelay}",
                                    messageRequest?.MessageType,
                                    messageRequest?.SendersReference,
                                    args.RetryDelay
                                );
                                return ValueTask.CompletedTask;
                            },
                        }
                    )
                    .AddTimeout(TimeSpan.FromSeconds(5));
            }
        );

        return services;
    }

    private static bool ErrorShouldBeHandled(Exception ex)
    {
        if (ex is FiksIOSendUnauthorizedException or MaskinportenException)
            return false;

        if (
            ex is FiksIOSendUnexpectedResponseException unexpectedResponse
            && unexpectedResponse.Message.Contains("status code notfound", StringComparison.OrdinalIgnoreCase)
        )
            return false;

        return true;
    }
}
