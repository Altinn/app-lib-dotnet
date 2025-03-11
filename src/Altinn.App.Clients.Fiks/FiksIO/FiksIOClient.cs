using System.Diagnostics.CodeAnalysis;
using Altinn.App.Core.Features.Maskinporten.Models;
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
using RabbitMQ.Client.Events;
using IMaskinportenClient = Altinn.App.Core.Features.Maskinporten.IMaskinportenClient;

namespace Altinn.App.Clients.Fiks.FiksIO;

internal sealed class FiksIOClient : IFiksIOClient
{
    private readonly IOptionsMonitor<FiksIOSettings> _fiksIOSettings;
    private readonly IAppMetadata _appMetadata;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FiksIOClient> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private KS.Fiks.IO.Client.FiksIOClient? _fiksIoClient;
    private EventHandler<FiksIOReceivedMessageArgs>? _messageReceivedHandler;
    private readonly IMaskinportenClient _maskinportenClient;

    public IFiksIOAccountSettings AccountSettings => _fiksIOSettings.CurrentValue;
    public RetryStrategy RetryStrategy { get; set; } = RetryStrategy.Default;

    public FiksIOClient(
        IOptionsMonitor<FiksIOSettings> fiksIOSettings,
        IWebHostEnvironment env,
        IAppMetadata appMetadata,
        IMaskinportenClient maskinportenClient,
        ILoggerFactory loggerFactory
    )
    {
        _fiksIOSettings = fiksIOSettings;
        _appMetadata = appMetadata;
        _env = env;
        _loggerFactory = loggerFactory;
        _maskinportenClient = maskinportenClient;
        _logger = loggerFactory.CreateLogger<FiksIOClient>();

        if (fiksIOSettings.CurrentValue is null)
        {
            throw new Exception("FiksIO has not been configured");
        }

        // Subscribe to settings changes
        fiksIOSettings.OnChange(InitialiseFiksIOClient_NeverThrowsWrapper);
    }

    public async Task<FiksIOMessageResponse> SendMessage(
        FiksIOMessageRequest request,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation(
            "Sending FiksIO message {MessageType}:{ClientMessageId}",
            request.MessageType,
            request.SendersReference
        );
        var externalRequest = request.ToMeldingRequest(AccountSettings.AccountId);
        var externalAttachments = request.ToPayload();

        _logger.LogDebug("Message details: {Message}", externalRequest);

        try
        {
            return await RetryStrategy.Execute(
                async () =>
                {
                    if (_fiksIoClient is null || _fiksIoClient.IsOpen() is false)
                        await InitialiseFiksIOClient();

                    return new FiksIOMessageResponse(await _fiksIoClient.Send(externalRequest, externalAttachments));
                },
                (error, delay) =>
                {
                    _logger.LogWarning(
                        error,
                        "Failed to send FiksIO message {MessageType}:{ClientMessageId}. Retrying in {RetryDelay}",
                        request.MessageType,
                        request.SendersReference,
                        delay
                    );
                }
            );
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "Failed to send message {MessageType}:{ClientMessageId} after {NumRetries}",
                request.MessageType,
                request.SendersReference,
                RetryStrategy.Intervals.Count
            );
            throw;
        }
    }

    public async Task OnMessageReceived(EventHandler<FiksIOReceivedMessageArgs> listener)
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

    [MemberNotNull(nameof(_fiksIoClient))]
    private async Task InitialiseFiksIOClient()
    {
        // var maskinportenSettings = _maskinportenSettings.CurrentValue;
        var fiksIOSettings = _fiksIOSettings.CurrentValue;

        // var maskinportenJwk = maskinportenSettings.GetJsonWebKey();
        // var maskinportenClientId = maskinportenSettings.ClientId;
        // var (maskinportenPublicKey, maskinportenPrivateKey) = maskinportenJwk.ConvertJwkToRsa();

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
        // maskinportenConfiguration: new MaskinportenClientConfiguration(
        //     audience: environmentConfig.MaskinportenAuthority,
        //     tokenEndpoint: environmentConfig.MaskinportenTokenEndpoint,
        //     issuer: maskinportenClientId,
        //     numberOfSecondsLeftBeforeExpire: 10,
        //     publicKey: maskinportenPublicKey,
        //     privateKey: maskinportenPrivateKey,
        //     keyIdentifier: maskinportenJwk.Kid
        // )
        );

        _fiksIoClient?.Dispose();
        _fiksIoClient = await KS.Fiks.IO.Client.FiksIOClient.CreateAsync(
            configuration: fiksConfiguration,
            maskinportenClient: new FiksIOMaskinportenClient(_maskinportenClient),
            loggerFactory: _loggerFactory
        );

        if (_messageReceivedHandler is not null)
            SubscribeToEvents();
    }

    private async void InitialiseFiksIOClient_NeverThrowsWrapper(object? x = null)
    {
        try
        {
            await InitialiseFiksIOClient();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to initialise FiksIO client: {ErrorMessage}", e.Message);
        }
    }

    private void SubscribeToEvents()
    {
        _fiksIoClient?.NewSubscription(MessageReceivedHandler, SubscriptionCancelledHandler);
    }

    private void MessageReceivedHandler(object? sender, MottattMeldingArgs e)
    {
        _messageReceivedHandler?.Invoke(sender, new FiksIOReceivedMessageArgs(e));
    }

    private void SubscriptionCancelledHandler(object? sender, ConsumerEventArgs e)
    {
        InitialiseFiksIOClient_NeverThrowsWrapper();
    }

    private EnvironmentConfiguration GetConfiguration(AppIdentifier appIdentifier)
    {
        var ampqAppName = $"altinn-app-{appIdentifier.Org}-{appIdentifier.App}";

        return _env.IsProduction()
            ? new EnvironmentConfiguration(
                ApiConfiguration.CreateProdConfiguration(),
                FiksIOConfiguration.maskinportenProdAudience,
                AmqpConfiguration.ProdHost,
                ampqAppName
            )
            : new EnvironmentConfiguration(
                ApiConfiguration.CreateTestConfiguration(),
                FiksIOConfiguration.maskinportenTestAudience,
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
        string MaskinportenAuthority,
        string FiksAmqpHost,
        string FiksAmqpAppName
    )
    {
        // public string MaskinportenTokenEndpoint => $"{MaskinportenAuthority.TrimEnd('/')}/token";
    }
}
