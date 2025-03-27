using System.Diagnostics;
using Altinn.App.Clients.Fiks.Constants;
using Altinn.App.Clients.Fiks.Exceptions;
using Altinn.App.Clients.Fiks.Extensions;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Maskinporten;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Models;
using KS.Fiks.IO.Client.Configuration;
using KS.Fiks.IO.Client.Models;
using KS.Fiks.IO.Crypto.Configuration;
using KS.Fiks.IO.Send.Client.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client.Events;
using ExternalFiksIOClient = KS.Fiks.IO.Client.FiksIOClient;
using FiksResult = Altinn.App.Core.Features.Telemetry.Fiks.FiksResult;
using IExternalFiksIOClient = KS.Fiks.IO.Client.IFiksIOClient;

namespace Altinn.App.Clients.Fiks.FiksIO;

internal sealed class FiksIOClient : IFiksIOClient
{
    private readonly IOptionsMonitor<FiksIOSettings> _fiksIOSettings;
    private readonly IAppMetadata _appMetadata;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FiksIOClient> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IMaskinportenClient _maskinportenClient;
    private readonly ResiliencePipeline<FiksIOMessageResponse> _resiliencePipeline;
    private readonly IExternalFiksIOClient? _fiksIoClientOverride;
    private IExternalFiksIOClient? _fiksIoClient;
    private event Func<FiksIOReceivedMessage, Task>? _messageReceivedHandler;
    private readonly Telemetry? _telemetry;

    public IFiksIOAccountSettings AccountSettings => _fiksIOSettings.CurrentValue;

    public FiksIOClient(
        IServiceProvider serviceProvider,
        IOptionsMonitor<FiksIOSettings> fiksIOSettings,
        IWebHostEnvironment env,
        IAppMetadata appMetadata,
        IMaskinportenClient maskinportenClient,
        ILoggerFactory loggerFactory,
        IExternalFiksIOClient? fiksIoClientOverride = null,
        Telemetry? telemetry = null
    )
    {
        _fiksIOSettings = fiksIOSettings;
        _appMetadata = appMetadata;
        _env = env;
        _loggerFactory = loggerFactory;
        _maskinportenClient = maskinportenClient;
        _logger = loggerFactory.CreateLogger<FiksIOClient>();
        _fiksIoClientOverride = fiksIoClientOverride;
        _resiliencePipeline = serviceProvider.ResolveResiliencePipeline();
        _telemetry = telemetry;

        if (fiksIOSettings.CurrentValue is null)
            throw new FiksIOConfigurationException("Fiks IO has not been configured");

        // Subscribe to settings changes
        fiksIOSettings.OnChange(InitialiseFiksIOClient_NeverThrowsWrapper);
    }

    public async Task<FiksIOMessageResponse> SendMessage(
        FiksIOMessageRequest request,
        CancellationToken cancellationToken = default
    )
    {
        using Activity? activity = _telemetry?.StartSendFiksActivity();
        _logger.LogInformation(
            "Sending Fiks IO message {MessageType}:{ClientMessageId}",
            request.MessageType,
            request.SendersReference
        );
        var externalRequest = request.ToMeldingRequest(AccountSettings.AccountId);
        var externalAttachments = request.ToPayload();
        var numAttempts = 0;

        try
        {
            ResilienceContext context = ResilienceContextPool.Shared.Get(cancellationToken);
            context.Properties.Set(
                new ResiliencePropertyKey<FiksIOMessageRequest>(FiksIOConstants.MessageRequestPropertyKey),
                request
            );

            FiksIOMessageResponse result = await _resiliencePipeline.ExecuteAsync(
                async context =>
                {
                    if (_fiksIoClient is null || await _fiksIoClient.IsOpenAsync() is false)
                        _fiksIoClient = await InitialiseFiksIOClient();

                    numAttempts += 1;

                    var externalResult = await _fiksIoClient.Send(
                        externalRequest,
                        externalAttachments,
                        cancellationToken
                    );
                    var result = new FiksIOMessageResponse(externalResult);
                    _logger.LogInformation("FiksIO message sent successfully: {MessageDetails}", result);

                    return result;
                },
                context
            );

            activity?.AddTag(Telemetry.Labels.FiksMessageId, result.MessageId);
            _telemetry?.RecordFiksMessageSent(FiksResult.Success);

            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "Failed to send message {MessageType}:{ClientMessageId} after {NumRetries} attempts: {Exception}",
                request.MessageType,
                request.SendersReference,
                numAttempts,
                e.Message
            );
            _telemetry?.RecordFiksMessageSent(FiksResult.Error);
            activity?.Errored(e);
            throw;
        }
    }

    public async Task OnMessageReceived(Func<FiksIOReceivedMessage, Task> listener)
    {
        bool alreadySubscribed = _messageReceivedHandler is not null;
        _messageReceivedHandler = listener; // Always update the handler

        if (alreadySubscribed)
            return;

        if (_fiksIoClient is null)
        {
            await InitialiseFiksIOClient();
        }
        else
        {
            await SubscribeToEvents();
        }
    }

    public async Task<bool> IsHealthy()
    {
        if (_fiksIoClient is null)
            return false;

        return await _fiksIoClient.IsOpenAsync();
    }

    public async Task Reconnect()
    {
        await InitialiseFiksIOClient();
    }

    private async Task<IExternalFiksIOClient> InitialiseFiksIOClient()
    {
        var fiksIOSettings = _fiksIOSettings.CurrentValue;
        var appMeta = await _appMetadata.GetApplicationMetadata();
        var environmentConfig = GetConfiguration(appMeta.AppIdentifier);

        var fiksConfiguration = new FiksIOConfiguration(
            amqpConfiguration: new AmqpConfiguration(
                environmentConfig.FiksAmqpHost,
                applicationName: environmentConfig.FiksAmqpAppName,
                prefetchCount: 0
            ),
            apiConfiguration: environmentConfig.FiksApiConfiguration,
            asiceSigningConfiguration: new AsiceSigningConfiguration(fiksIOSettings.GenerateAsiceCertificate()),
            integrasjonConfiguration: new IntegrasjonConfiguration(
                fiksIOSettings.IntegrationId,
                fiksIOSettings.IntegrationPassword
            ),
            kontoConfiguration: new KontoConfiguration(fiksIOSettings.AccountId, fiksIOSettings.AccountPrivateKey)
        );

        if (_fiksIoClient is not null)
            await _fiksIoClient.DisposeAsync();

        _fiksIoClient =
            _fiksIoClientOverride
            ?? await ExternalFiksIOClient.CreateAsync(
                configuration: fiksConfiguration,
                maskinportenClient: new FiksIOMaskinportenClient(_maskinportenClient),
                loggerFactory: _loggerFactory
            );

        if (_messageReceivedHandler is not null)
            await SubscribeToEvents();

        return _fiksIoClient;
    }

    private async void InitialiseFiksIOClient_NeverThrowsWrapper(object? x = null)
    {
        try
        {
            await InitialiseFiksIOClient();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to initialise Fiks IO client: {ErrorMessage}", e.Message);
        }
    }

    private async Task SubscribeToEvents()
    {
        if (_fiksIoClient is null)
            return;

        await _fiksIoClient.NewSubscriptionAsync(MessageReceivedHandler, SubscriptionCancelledHandler);
    }

    private async Task MessageReceivedHandler(MottattMeldingArgs eventArgs)
    {
        if (_messageReceivedHandler is null)
            return;

        await _messageReceivedHandler.Invoke(new FiksIOReceivedMessage(eventArgs));
    }

    private Task SubscriptionCancelledHandler(ConsumerEventArgs eventArgs)
    {
        InitialiseFiksIOClient_NeverThrowsWrapper();
        return Task.CompletedTask;
    }

    private EnvironmentConfiguration GetConfiguration(AppIdentifier appIdentifier)
    {
        var ampqAppName = $"altinn-app-{appIdentifier.Org}-{appIdentifier.App}";

        return _env.IsProduction()
            ? new EnvironmentConfiguration(
                ApiConfiguration.CreateProdConfiguration(),
                AmqpConfiguration.ProdHost,
                ampqAppName
            )
            : new EnvironmentConfiguration(
                ApiConfiguration.CreateTestConfiguration(),
                AmqpConfiguration.TestHost,
                ampqAppName
            );
    }

    private readonly record struct EnvironmentConfiguration(
        ApiConfiguration FiksApiConfiguration,
        string FiksAmqpHost,
        string FiksAmqpAppName
    );

    public async ValueTask DisposeAsync()
    {
        if (_fiksIoClientOverride is not null)
        {
            await _fiksIoClientOverride.DisposeAsync();
        }

        if (_fiksIoClient is not null)
        {
            await _fiksIoClient.DisposeAsync();
        }
    }
}
