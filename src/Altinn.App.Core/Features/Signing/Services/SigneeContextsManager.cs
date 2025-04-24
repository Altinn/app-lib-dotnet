using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.App.Core.Features.Signing.Exceptions;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Registers;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using static Altinn.App.Core.Features.Signing.Models.Signee;
using Signee = Altinn.App.Core.Features.Signing.Models.Signee;

namespace Altinn.App.Core.Features.Signing.Services;

internal sealed class SigneeContextsManager : ISigneeContextsManager
{
    private readonly IAltinnPartyClient _altinnPartyClient;
    private readonly AppImplementationFactory _appImplementationFactory;
    private readonly ILogger<SigneeContextsManager> _logger;
    private readonly Telemetry? _telemetry;

    public SigneeContextsManager(
        IAltinnPartyClient altinnPartyClient,
        AppImplementationFactory appImplementationFactory,
        ILogger<SigneeContextsManager> logger,
        Telemetry? telemetry = null
    )
    {
        _altinnPartyClient = altinnPartyClient;
        _appImplementationFactory = appImplementationFactory;
        _logger = logger;
        _telemetry = telemetry;
    }

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(
        new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.Preserve,
            MaxDepth = 16,
        }
    );

    // <inheritdoc />
    public async Task<List<SigneeContext>> GenerateSigneeContexts(
        IInstanceDataMutator instanceDataMutator,
        AltinnSignatureConfiguration signatureConfiguration,
        CancellationToken ct
    )
    {
        using Activity? activity = _telemetry?.StartAssignSigneesActivity();

        Instance instance = instanceDataMutator.Instance;
        string taskId = instance.Process.CurrentTask.ElementId;

        SigneeProviderResult? signeesResult = await GetSigneesFromProvider(instance, signatureConfiguration);
        if (signeesResult is null)
        {
            throw new SigningException("No signees returned from the signees provider");
        }

        List<SigneeContext> signeeContexts = [];
        foreach (ProvidedSignee signeeParty in signeesResult.Signees)
        {
            SigneeContext signeeContext = await GenerateSigneeContext(taskId, signeeParty, ct);
            signeeContexts.Add(signeeContext);
        }

        _logger.LogInformation(
            "Assigning {SigneeContextsCount} signees to task {TaskId}.",
            signeeContexts.Count,
            taskId
        );
        _logger.LogDebug(
            "Signee context state: {SigneeContexts}",
            JsonSerializer.Serialize(signeeContexts, _jsonSerializerOptions)
        );

        return signeeContexts;
    }

    // <inheritdoc />
    public async Task<List<SigneeContext>> GetSigneeContexts(
        IInstanceDataAccessor instanceDataAccessor,
        AltinnSignatureConfiguration signatureConfiguration
    )
    {
        using Activity? activity = _telemetry?.StartReadSigneesActivity();
        // If no SigneeStatesDataTypeId is set, delegated signing is not enabled and there is nothing to download.
        List<SigneeContext> signeeContexts = !string.IsNullOrEmpty(signatureConfiguration.SigneeStatesDataTypeId)
            ? await DownloadSigneeContexts(instanceDataAccessor, signatureConfiguration)
            : [];

        return signeeContexts;
    }

    /// <summary>
    /// Get signees from the signee provider implemented in the App.
    /// </summary>
    private async Task<SigneeProviderResult> GetSigneesFromProvider(
        Instance instance,
        AltinnSignatureConfiguration signatureConfiguration
    )
    {
        string taskId = instance.Process.CurrentTask.ElementId;

        List<ISigneeProvider> matchingSigneeProviders = _appImplementationFactory
            .GetAll<ISigneeProvider>()
            .Where(x => x.ShouldRunForTask(taskId))
            .ToList();

        if (matchingSigneeProviders.Count == 0)
        {
            throw new SigneeProviderNotFoundException(
                $"No signee provider found for task {taskId}. Please add an implementation of the {nameof(ISigneeProvider)} interface with that ID or correct the ID."
            );
        }

        if (matchingSigneeProviders.Count > 1)
        {
            throw new SigneeProviderNotFoundException(
                $"Found more than one signee provider for task {taskId}: [{string.Join(",", matchingSigneeProviders)}]. Please ensure that maximum one signee provider is configured to run for each signing task. Multiple tasks can use the same provider."
            );
        }

        ISigneeProvider signeeProvider =
            matchingSigneeProviders.FirstOrDefault()
            ?? throw new SigneeProviderNotFoundException(
                "Unexpected error: Didn't find a matching signee provider when one was expected. Signee provider matching code is not working as expected."
            );

        SigneeProviderResult signeesResult = await signeeProvider.GetSigneesAsync(instance);

        return signeesResult;
    }

    private async Task<SigneeContext> GenerateSigneeContext(
        string taskId,
        ProvidedSignee providedSignee,
        CancellationToken ct
    )
    {
        Signee signee = await From(providedSignee, _altinnPartyClient.LookupParty);
        Party party = signee.GetParty();

        Notifications? notifications = providedSignee.Notifications;

        Email? emailNotification = notifications?.OnSignatureAccessRightsDelegated?.Email;
        if (emailNotification is not null && string.IsNullOrEmpty(emailNotification.EmailAddress))
        {
            emailNotification.EmailAddress = party.Organization?.EMailAddress;
        }

        Sms? smsNotification = notifications?.OnSignatureAccessRightsDelegated?.Sms;
        if (smsNotification is not null && string.IsNullOrEmpty(smsNotification.MobileNumber))
        {
            smsNotification.MobileNumber = party.Organization?.MobileNumber ?? party.Person?.MobileNumber;
        }

        return new SigneeContext
        {
            TaskId = taskId,
            SigneeState = new SigneeContextState(),
            Notifications = notifications,
            Signee = signee,
        };
    }

    private async Task<List<SigneeContext>> DownloadSigneeContexts(
        IInstanceDataAccessor instanceDataAccessor,
        AltinnSignatureConfiguration signatureConfiguration
    )
    {
        string signeeStatesDataTypeId =
            signatureConfiguration.SigneeStatesDataTypeId
            ?? throw new ApplicationConfigException(
                "SigneeStatesDataTypeId is not set in the signature configuration."
            );

        IEnumerable<DataElement> dataElements = instanceDataAccessor.GetDataElementsForType(signeeStatesDataTypeId);

        DataElement? signeeStateDataElement = dataElements.SingleOrDefault();

        if (signeeStateDataElement is null)
        {
            _logger.LogInformation("Didn't find any signee states for task.");
            return [];
        }

        ReadOnlyMemory<byte> data = await instanceDataAccessor.GetBinaryData(signeeStateDataElement);
        string signeeStateSerialized = Encoding.UTF8.GetString(data.ToArray());

        List<SigneeContext> signeeContexts =
            JsonSerializer.Deserialize<List<SigneeContext>>(signeeStateSerialized, _jsonSerializerOptions) ?? [];

        return signeeContexts;
    }
}
