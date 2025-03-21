using Altinn.App.Clients.Fiks.Constants;
using Altinn.App.Clients.Fiks.Exceptions;
using Altinn.App.Clients.Fiks.FiksIO.Models;
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
using Polly.Registry;
using RabbitMQ.Client.Events;
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
    private EventHandler<FiksIOReceivedMessage>? _messageReceivedHandler;

    public IFiksIOAccountSettings AccountSettings => _fiksIOSettings.CurrentValue;

    public FiksIOClient(
        IOptionsMonitor<FiksIOSettings> fiksIOSettings,
        IWebHostEnvironment env,
        IAppMetadata appMetadata,
        IMaskinportenClient maskinportenClient,
        ResiliencePipelineProvider<string> resiliencePipelineProvider,
        ILoggerFactory loggerFactory,
        KS.Fiks.IO.Client.IFiksIOClient? fiksIoClientOverride = null
    )
    {
        _fiksIOSettings = fiksIOSettings;
        _appMetadata = appMetadata;
        _env = env;
        _loggerFactory = loggerFactory;
        _maskinportenClient = maskinportenClient;
        _logger = loggerFactory.CreateLogger<FiksIOClient>();
        _fiksIoClientOverride = fiksIoClientOverride;
        _resiliencePipeline = resiliencePipelineProvider.GetPipeline<FiksIOMessageResponse>(
            FiksIOConstants.ResiliencePipelineId
        );

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

            return await _resiliencePipeline.ExecuteAsync(
                async context =>
                {
                    if (_fiksIoClient is null || _fiksIoClient.IsOpen() is false)
                        _fiksIoClient = await InitialiseFiksIOClient();

                    numAttempts += 1;

                    // TODO: Pass context.CancellationToken onwards
                    FiksIOMessageResponse result = new(await _fiksIoClient.Send(externalRequest, externalAttachments));
                    _logger.LogInformation("FiksIO message sent successfully: {MessageDetails}", result);

                    return result;
                },
                context
            );
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
            throw;
        }
    }

    public async Task OnMessageReceived(EventHandler<FiksIOReceivedMessage> listener)
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
            SubscribeToEvents();
        }
    }

    public bool IsHealthy()
    {
        return _fiksIoClient?.IsOpen() ?? false;
    }

    public async Task Reconnect()
    {
        _fiksIoClient?.Dispose();
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

        if (_fiksIoClientOverride is not null)
        {
            _fiksIoClient = _fiksIoClientOverride;
        }
        else
        {
            _fiksIoClient?.Dispose();
            _fiksIoClient = await KS.Fiks.IO.Client.FiksIOClient.CreateAsync(
                configuration: fiksConfiguration,
                maskinportenClient: new FiksIOMaskinportenClient(_maskinportenClient),
                loggerFactory: _loggerFactory
            );
        }

        if (_messageReceivedHandler is not null)
            SubscribeToEvents();

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

    private void SubscribeToEvents()
    {
        _fiksIoClient?.NewSubscription(MessageReceivedHandler, SubscriptionCancelledHandler);
    }

    private void MessageReceivedHandler(object? sender, MottattMeldingArgs eventArgs)
    {
        _messageReceivedHandler?.Invoke(sender, new FiksIOReceivedMessage(eventArgs));
    }

    private void SubscriptionCancelledHandler(object? sender, ConsumerEventArgs eventArgs)
    {
        InitialiseFiksIOClient_NeverThrowsWrapper();
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

    public void Dispose()
    {
        _fiksIoClient?.Dispose();
    }

    private readonly record struct EnvironmentConfiguration(
        ApiConfiguration FiksApiConfiguration,
        string FiksAmqpHost,
        string FiksAmqpAppName
    );
}
