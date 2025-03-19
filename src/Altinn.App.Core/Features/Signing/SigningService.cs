using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.App.Core.Exceptions;
using Altinn.App.Core.Features.Signing.Exceptions;
using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Internal.AltinnCdn;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Auth;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Registers;
using Altinn.App.Core.Models;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;
using Authorization.Platform.Authorization.Models;
using Microsoft.Extensions.Logging;
using static Altinn.App.Core.Features.Signing.Models.Signee;
using JsonException = Newtonsoft.Json.JsonException;
using OrganisationSignee = Altinn.App.Core.Features.Signing.Models.Signee.OrganisationSignee;
using PersonSignee = Altinn.App.Core.Features.Signing.Models.Signee.PersonSignee;

namespace Altinn.App.Core.Features.Signing;

internal sealed class SigningService(
    IAltinnPartyClient altinnPartyClient,
    ISigningDelegationService signingDelegationService,
    AppImplementationFactory appImplementationFactory,
    IAppMetadata appMetadata,
    ISigningCallToActionService signingCallToActionService,
    IAuthorizationClient authorizationClient,
    ILogger<SigningService> logger,
    Telemetry? telemetry = null
) : ISigningService
{
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
    private readonly ILogger<SigningService> _logger = logger;
    private readonly IAppMetadata _appMetadata = appMetadata;
    private readonly ISigningCallToActionService _signingCallToActionService = signingCallToActionService;
    private readonly AppImplementationFactory _appImplementationFactory = appImplementationFactory;
    private const string ApplicationJsonContentType = "application/json";

    // <inheritdoc />
    public async Task<List<SigneeContext>> GenerateSigneeContexts(
        IInstanceDataMutator instanceDataMutator,
        AltinnSignatureConfiguration signatureConfiguration,
        CancellationToken ct
    )
    {
        using Activity? activity = telemetry?.StartAssignSigneesActivity();

        Instance instance = instanceDataMutator.Instance;
        string taskId = instance.Process.CurrentTask.ElementId;

        SigneesResult? signeesResult = await GetSigneesFromProvider(instance, signatureConfiguration);
        if (signeesResult is null)
        {
            return [];
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
    public async Task<List<SigneeContext>> InitialiseSignees(
        string taskId,
        IInstanceDataMutator instanceDataMutator,
        List<SigneeContext> signeeContexts,
        AltinnSignatureConfiguration signatureConfiguration,
        CancellationToken ct
    )
    {
        using Activity? activity = telemetry?.StartAssignSigneesActivity();

        string signeeStateDataTypeId =
            signatureConfiguration.SigneeStatesDataTypeId
            ?? throw new ApplicationConfigException(
                "SigneeStatesDataTypeId is not set in the signature configuration."
            );

        //TODO: Can be removed when AddBinaryDataElement supports setting generatedFromTask, because then it will be automatically deleted in ProcessTaskInitializer.
        RemoveSigneeState(instanceDataMutator, signeeStateDataTypeId);

        string instanceIdCombo = instanceDataMutator.Instance.Id;
        InstanceOwner instanceOwner = instanceDataMutator.Instance.InstanceOwner;
        Party? instanceOwnerParty = await GetInstanceOwnerParty(instanceOwner);
        Guid? instanceOwnerPartyUuid = instanceOwnerParty?.PartyUuid;
        AppIdentifier appIdentifier = new(instanceDataMutator.Instance.AppId);

        (signeeContexts, bool delegateSuccess) = await signingDelegationService.DelegateSigneeRights(
            taskId,
            instanceIdCombo,
            instanceOwnerPartyUuid,
            appIdentifier,
            signeeContexts,
            ct,
            telemetry
        );

        Party serviceOwnerParty = new();
        bool getServiceOwnerSuccess = false;

        if (delegateSuccess)
        {
            (serviceOwnerParty, getServiceOwnerSuccess) = await GetServiceOwnerParty(ct);
        }

        if (getServiceOwnerSuccess)
        {
            foreach (SigneeContext signeeContext in signeeContexts)
            {
                if (signeeContext.SigneeState.HasBeenMessagedForCallToSign)
                {
                    continue;
                }

                try
                {
                    Party signingParty = signeeContext.Signee.GetParty();

                    await _signingCallToActionService.SendSignCallToAction(
                        signeeContext.Notifications?.OnSignatureAccessRightsDelegated,
                        appIdentifier,
                        new InstanceIdentifier(instanceDataMutator.Instance),
                        signingParty,
                        serviceOwnerParty,
                        signatureConfiguration.CorrespondenceResources
                    );
                    signeeContext.SigneeState.HasBeenMessagedForCallToSign = true;
                }
                catch (ConfigurationException e)
                {
                    _logger.LogError(e, "Correspondence configuration error: {Exception}", e.Message);
                    signeeContext.SigneeState.HasBeenMessagedForCallToSign = false;
                    signeeContext.SigneeState.CallToSignFailedReason = $"Correspondence configuration error.";
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Correspondence send failed: {Exception}", e.Message);
                    signeeContext.SigneeState.HasBeenMessagedForCallToSign = false;
                    signeeContext.SigneeState.CallToSignFailedReason = $"Correspondence configuration error.";
                }
            }
        }

        instanceDataMutator.AddBinaryDataElement(
            dataTypeId: signeeStateDataTypeId,
            contentType: ApplicationJsonContentType,
            filename: null,
            bytes: JsonSerializer.SerializeToUtf8Bytes(signeeContexts, _jsonSerializerOptions)
        );

        return signeeContexts;
    }

    // <inheritdoc />
    public async Task<List<SigneeContext>> GetSigneeContexts(
        IInstanceDataAccessor instanceDataAccessor,
        AltinnSignatureConfiguration signatureConfiguration
    )
    {
        using Activity? activity = telemetry?.StartReadSigneesActivity();
        List<SigneeContext> signeeContexts = await TryDownLoadSigneeContexts(
            instanceDataAccessor,
            signatureConfiguration
        );

        var taskId = instanceDataAccessor.Instance.Process.CurrentTask.ElementId;

        List<SignDocument> signDocuments = await DownloadSignDocuments(instanceDataAccessor, signatureConfiguration);

        await SynchronizeSigneeContextsWithSignDocuments(taskId, signeeContexts, signDocuments);

        return signeeContexts;
    }

    // <inheritdoc />
    public async Task<List<OrganisationSignee>> GetAuthorizedOrganisations(
        IInstanceDataAccessor instanceDataAccessor,
        AltinnSignatureConfiguration signatureConfiguration,
        int userId
    )
    {
        List<SigneeContext> signeeContexts = await TryDownLoadSigneeContexts(
            instanceDataAccessor,
            signatureConfiguration
        );

        List<OrganisationSignee> authorizedOrganisations = [];
        List<OrganisationSignee> orgSignees = [.. signeeContexts.Select(x => x.Signee).OfType<OrganisationSignee>()];

        foreach (OrganisationSignee organisationSignee in orgSignees)
        {
            List<Role> roles = await authorizationClient.GetRoles(userId, organisationSignee.OrgParty.PartyId);
            if (roles.Count != 0)
            {
                authorizedOrganisations.Add(organisationSignee);
            }
        }
        return authorizedOrganisations;
    }

    // <inheritdoc />
    public async Task AbortRuntimeDelegatedSigning(
        string taskId,
        IInstanceDataMutator instanceDataMutator,
        AltinnSignatureConfiguration signatureConfiguration,
        CancellationToken ct
    )
    {
        if (signatureConfiguration.SigneeStatesDataTypeId is not null)
        {
            RemoveSigneeState(instanceDataMutator, signatureConfiguration.SigneeStatesDataTypeId);
        }

        if (signatureConfiguration.SignatureDataType is not null)
        {
            RemoveAllSignatures(instanceDataMutator, signatureConfiguration.SignatureDataType);
        }

        List<SigneeContext> signeeContexts = await GetSigneeContexts(instanceDataMutator, signatureConfiguration);
        List<SigneeContext> signeeContextsWithDelegation = signeeContexts
            .Where(x => x.SigneeState.IsAccessDelegated)
            .ToList();

        if (signeeContextsWithDelegation.IsNullOrEmpty())
        {
            _logger.LogInformation("Didn't find any signee contexts with delegated access rights. Nothing to revoke.");
            return;
        }

        string instanceIdCombo = instanceDataMutator.Instance.Id;
        InstanceOwner instanceOwner = instanceDataMutator.Instance.InstanceOwner;
        Party instanceOwnerParty =
            await GetInstanceOwnerParty(instanceOwner)
            ?? throw new SigningException("Failed to lookup instance owner party.");

        Guid instanceOwnerPartyUuid =
            instanceOwnerParty.PartyUuid ?? throw new SigningException("PartyUuid was missing on instance owner party");

        AppIdentifier appIdentifier = new(instanceDataMutator.Instance.AppId);

        await signingDelegationService.RevokeSigneeRights(
            taskId,
            instanceIdCombo,
            instanceOwnerPartyUuid,
            appIdentifier,
            signeeContextsWithDelegation,
            ct
        );
    }

    private async Task<List<SigneeContext>> TryDownLoadSigneeContexts(
        IInstanceDataAccessor instanceDataAccessor,
        AltinnSignatureConfiguration signatureConfiguration
    )
    {
        // If no SigneeStatesDataTypeId is set, delegated signing is not enabled and there is nothing to download.
        return !string.IsNullOrEmpty(signatureConfiguration.SigneeStatesDataTypeId)
            ? await DownloadSigneeContexts(instanceDataAccessor, signatureConfiguration)
            : [];
    }

    /// <summary>
    /// Get signees from the signee provider implemented in the App.
    /// </summary>
    /// <exception cref="SigneeProviderNotFoundException"></exception>
    private async Task<SigneesResult?> GetSigneesFromProvider(
        Instance instance,
        AltinnSignatureConfiguration signatureConfiguration
    )
    {
        string? signeeProviderId = signatureConfiguration.SigneeProviderId;
        if (string.IsNullOrEmpty(signeeProviderId))
            return null;

        var signeeProviders = _appImplementationFactory.GetAll<ISigneeProvider>();
        ISigneeProvider signeeProvider =
            signeeProviders.FirstOrDefault(sp => sp.Id == signeeProviderId)
            ?? throw new SigneeProviderNotFoundException(
                $"No signee provider found for task {instance.Process.CurrentTask.ElementId} with signeeProviderId {signeeProviderId}. Please add an implementation of the {nameof(ISigneeProvider)} interface with that ID or correct the ID."
            );

        SigneesResult signeesResult = await signeeProvider.GetSigneesAsync(instance);
        return signeesResult;
    }

    private async Task<SigneeContext> GenerateSigneeContext(
        string taskId,
        ProvidedSignee signeeParty,
        CancellationToken ct
    )
    {
        Models.Signee signee = await From(signeeParty, altinnPartyClient.LookupParty);
        Party party = signee.GetParty();

        Models.Notifications? notifications = signeeParty.Notifications;

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
            SigneeState = new SigneeState(),
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

    private async Task<List<SignDocument>> DownloadSignDocuments(
        IInstanceDataAccessor instanceDataAccessor,
        AltinnSignatureConfiguration signatureConfiguration
    )
    {
        string signatureDataTypeId =
            signatureConfiguration.SignatureDataType
            ?? throw new ApplicationConfigException("SignatureDataType is not set in the signature configuration.");

        IEnumerable<DataElement> signatureDataElements = instanceDataAccessor.GetDataElementsForType(
            signatureDataTypeId
        );

        try
        {
            SignDocument[] signDocuments = await Task.WhenAll(
                signatureDataElements.Select(signatureDataElement =>
                    DownloadSignDocumentAsync(instanceDataAccessor, signatureDataElement)
                )
            );

            return [.. signDocuments];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download signature documents.");
            throw;
        }
    }

    private async Task<SignDocument> DownloadSignDocumentAsync(
        IInstanceDataAccessor instanceDataAccessor,
        DataElement signatureDataElement
    )
    {
        try
        {
            ReadOnlyMemory<byte> data = await instanceDataAccessor.GetBinaryData(signatureDataElement);
            string signDocumentSerialized = Encoding.UTF8.GetString(data.ToArray());

            return JsonSerializer.Deserialize<SignDocument>(signDocumentSerialized, _jsonSerializerOptions)
                ?? throw new JsonException("Could not deserialize signature document.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to download signature document for DataElement with ID {DataElementId}.",
                signatureDataElement.Id
            );
            throw;
        }
    }

    /// <summary>
    /// This method exists to ensure we have a SigneeContext for both signees that have been delegated access to sign and signees that have signed using access granted through the policy.xml file.
    /// </summary>
    internal async Task SynchronizeSigneeContextsWithSignDocuments(
        string taskId,
        List<SigneeContext> signeeContexts,
        List<SignDocument> signDocuments
    )
    {
        _logger.LogDebug(
            "Synchronizing signee contexts {SigneeContexts} with sign documents {SignDocuments} for task {TaskId}.",
            JsonSerializer.Serialize(signeeContexts, _jsonSerializerOptions),
            JsonSerializer.Serialize(signDocuments, _jsonSerializerOptions),
            taskId
        );

        List<SignDocument> unmatchedSignDocuments = signDocuments;

        // OrganisationSignee is most general, so it should be sorted to the end of the list
        signeeContexts.Sort(
            (a, b) =>
                a.Signee is OrganisationSignee ? 1
                : b.Signee is OrganisationSignee ? -1
                : 0
        );

        foreach (SigneeContext signeeContext in signeeContexts)
        {
            SignDocument? matchedSignDocument = signDocuments.FirstOrDefault(signDocument =>
            {
                return signeeContext.Signee switch
                {
                    PersonSignee personSignee => IsPersonSignDocument(signDocument)
                        && personSignee.SocialSecurityNumber == signDocument.SigneeInfo.PersonNumber,
                    PersonOnBehalfOfOrgSignee personOnBehalfOfOrgSignee => IsPersonOnBehalfOfOrgSignDocument(
                        signDocument
                    )
                        && personOnBehalfOfOrgSignee.OnBehalfOfOrg.OrgNumber
                            == signDocument.SigneeInfo.OrganisationNumber
                        && personOnBehalfOfOrgSignee.SocialSecurityNumber == signDocument.SigneeInfo.PersonNumber,
                    SystemSignee systemSignee => IsSystemSignDocument(signDocument)
                        && systemSignee.OnBehalfOfOrg.OrgNumber == signDocument.SigneeInfo.OrganisationNumber
                        && systemSignee.SystemId.Equals(signDocument.SigneeInfo.SystemUserId),
                    OrganisationSignee orgSignee => IsOrgSignDocument(signDocument)
                        && orgSignee.OrgNumber == signDocument.SigneeInfo.OrganisationNumber,

                    _ => throw new InvalidOperationException("Signee is not of a supported type."),
                };
            });

            if (matchedSignDocument is not null)
            {
                if (signeeContext.Signee is OrganisationSignee orgSignee)
                {
                    await ConvertOrgSignee(matchedSignDocument, signeeContext, orgSignee);
                }

                signeeContext.SignDocument = matchedSignDocument;
                unmatchedSignDocuments.Remove(matchedSignDocument);
            }
        }

        // Create new contexts for documents that aren't matched with existing signee contexts
        foreach (SignDocument signDocument in unmatchedSignDocuments)
        {
            SigneeContext newSigneeContext = await CreateSigneeContextFromSignDocument(taskId, signDocument);
            signeeContexts.Add(newSigneeContext);
        }
    }

    private async Task ConvertOrgSignee(
        SignDocument? signDocument,
        SigneeContext orgSigneeContext,
        OrganisationSignee orgSignee
    )
    {
        if (signDocument is null)
        {
            return;
        }

        var signeeInfo = signDocument.SigneeInfo;

        if (!string.IsNullOrEmpty(signeeInfo.PersonNumber))
        {
            orgSigneeContext.Signee = await orgSignee.ToPersonOnBehalfOfOrgSignee(
                signeeInfo.PersonNumber,
                altinnPartyClient.LookupParty
            );
        }
        else if (signeeInfo.SystemUserId.HasValue)
        {
            orgSigneeContext.Signee = orgSignee.ToSystemSignee(signeeInfo.SystemUserId.Value);
        }
        else
        {
            throw new InvalidOperationException("Signee is neither a person nor a system user");
        }
    }

    private async Task<SigneeContext> CreateSigneeContextFromSignDocument(string taskId, SignDocument signDocument)
    {
        _logger.LogDebug(
            "Creating signee context for sign document {SignDocument} for task {TaskId}.",
            JsonSerializer.Serialize(signDocument, _jsonSerializerOptions),
            taskId
        );

        return new SigneeContext
        {
            TaskId = taskId,
            Signee = await From(
                signDocument.SigneeInfo.PersonNumber,
                signDocument.SigneeInfo.OrganisationNumber,
                signDocument.SigneeInfo.SystemUserId,
                altinnPartyClient.LookupParty
            ),
            SigneeState = new SigneeState()
            {
                IsAccessDelegated = true,
                HasBeenMessagedForCallToSign = true,
                IsReceiptSent = false,
            },
            SignDocument = signDocument,
        };
    }

    private async Task<Party?> GetInstanceOwnerParty(InstanceOwner instanceOwner)
    {
        try
        {
            if (instanceOwner.OrganisationNumber == "ttd")
            {
                // Testdepartementet is often used in test environments, it does not have an organisation number, so we use Digitaliseringsdirektoratet's orgnr instead.
                instanceOwner.OrganisationNumber = "991825827";
            }

            return await altinnPartyClient.LookupParty(
                !string.IsNullOrEmpty(instanceOwner.OrganisationNumber)
                    ? new PartyLookup { OrgNo = instanceOwner.OrganisationNumber }
                    : new PartyLookup { Ssn = instanceOwner.PersonNumber }
            );
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to look up party for instance owner.");
        }

        return null;
    }

    private async Task<(Party serviceOwnerParty, bool success)> GetServiceOwnerParty(CancellationToken ct)
    {
        Party serviceOwnerParty;
        try
        {
            ApplicationMetadata applicationMetadata = await _appMetadata.GetApplicationMetadata();

            using var altinnCdnClient = new AltinnCdnClient();

            AltinnCdnOrgs altinnCdnOrgs = await altinnCdnClient.GetOrgs(ct);

            AltinnCdnOrgDetails? serviceOwnerDetails = altinnCdnOrgs.Orgs?.GetValueOrDefault(applicationMetadata.Org);

            if (serviceOwnerDetails?.Orgnr == "ttd")
            {
                // TestDepartementet is often used in test environments, it does not have an organisation number, so we use Digitaliseringsdirektoratet's orgnr instead.
                serviceOwnerDetails.Orgnr = "991825827";
            }

            serviceOwnerParty = await altinnPartyClient.LookupParty(
                new PartyLookup { OrgNo = serviceOwnerDetails?.Orgnr }
            );
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to look up party for service owner.");
            return (new Party(), false);
        }

        return (serviceOwnerParty, true);
    }

    private void RemoveSigneeState(IInstanceDataMutator instanceDataMutator, string signeeStatesDataTypeId)
    {
        using Activity? activity = telemetry?.StartRemoveSigneeStateActivity();

        IEnumerable<DataElement> signeeStateDataElements = instanceDataMutator.GetDataElementsForType(
            signeeStatesDataTypeId
        );

        DataElement? signeeStateDataElement = signeeStateDataElements.SingleOrDefault();
        if (signeeStateDataElement is not null)
        {
            instanceDataMutator.RemoveDataElement(signeeStateDataElement);
        }
    }

    private void RemoveAllSignatures(IInstanceDataMutator instanceDataMutator, string signatureDataType)
    {
        using Activity? activity = telemetry?.StartRemoveAllSignaturesActivity();

        IEnumerable<DataElement> signatures = instanceDataMutator.GetDataElementsForType(signatureDataType);

        foreach (DataElement signature in signatures)
        {
            instanceDataMutator.RemoveDataElement(signature);
        }
    }

    private static bool IsPersonOnBehalfOfOrgSignDocument(SignDocument signDocument)
    {
        return !string.IsNullOrEmpty(signDocument.SigneeInfo.PersonNumber)
            && !string.IsNullOrEmpty(signDocument.SigneeInfo.OrganisationNumber);
    }

    private static bool IsPersonSignDocument(SignDocument signDocument)
    {
        return !string.IsNullOrEmpty(signDocument.SigneeInfo.PersonNumber)
            && string.IsNullOrEmpty(signDocument.SigneeInfo.OrganisationNumber);
    }

    private static bool IsOrgSignDocument(SignDocument signDocument)
    {
        return !string.IsNullOrEmpty(signDocument.SigneeInfo.OrganisationNumber);
    }

    private static bool IsSystemSignDocument(SignDocument signDocument)
    {
        return !string.IsNullOrEmpty(signDocument.SigneeInfo.OrganisationNumber)
            && signDocument.SigneeInfo.SystemUserId.HasValue;
    }
}
