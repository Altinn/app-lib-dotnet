using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.App.Core.Exceptions;
using Altinn.App.Core.Features.Auth;
using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Features.Signing.Exceptions;
using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Internal.AltinnCdn;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Registers;
using Altinn.App.Core.Internal.Sign;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.UserAction;
using Altinn.Platform.Profile.Models;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using static Altinn.App.Core.Features.Signing.Models.Signee;
using JsonException = Newtonsoft.Json.JsonException;
using Signee = Altinn.App.Core.Internal.Sign.Signee;

namespace Altinn.App.Core.Features.Signing;

internal sealed class SigningService(
    IAltinnPartyClient altinnPartyClient,
    ISigningDelegationService signingDelegationService,
    IEnumerable<ISigneeProvider> signeeProviders,
    IAppMetadata appMetadata,
    ISignClient signClient,
    ISigningReceiptService signingReceiptService,
    ISigningCallToActionService signingCallToActionService,
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
    private readonly ISignClient _signClient = signClient;
    private readonly ISigningReceiptService _signingReceiptService = signingReceiptService;
    private readonly ISigningCallToActionService _signingCallToActionService = signingCallToActionService;
    private const string ApplicationJsonContentType = "application/json";

    public async Task<List<SigneeContext>> GenerateSigneeContexts(
        IInstanceDataMutator instanceMutator,
        AltinnSignatureConfiguration signatureConfiguration,
        CancellationToken ct
    )
    {
        using Activity? activity = telemetry?.StartAssignSigneesActivity();

        Instance instance = instanceMutator.Instance;
        string taskId = instance.Process.CurrentTask.ElementId;

        SigneesResult? signeesResult = await GetSigneesFromProvider(instance, signatureConfiguration);
        if (signeesResult is null)
        {
            return [];
        }

        List<SigneeContext> signeeContexts = [];
        foreach (SigneeParty signeeParty in signeesResult.Signees)
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

    public async Task<List<SigneeContext>> InitialiseSignees(
        string taskId,
        IInstanceDataMutator instanceMutator,
        List<SigneeContext> signeeContexts,
        AltinnSignatureConfiguration signatureConfiguration,
        CancellationToken ct
    )
    {
        using Activity? activity = telemetry?.StartAssignSigneesActivity();

        string instanceIdCombo = instanceMutator.Instance.Id;
        InstanceOwner instanceOwner = instanceMutator.Instance.InstanceOwner;

        Party? instanceOwnerParty = null;
        try
        {
            if (instanceOwner.OrganisationNumber == "ttd")
            {
                // TestDepartementet is often used in test environments, it does not have a organisation number, so we use Digitaliseringsdirektoratet's orgnr instead.
                instanceOwner.OrganisationNumber = "991825827";
            }
            instanceOwnerParty = await altinnPartyClient.LookupParty(
                !string.IsNullOrEmpty(instanceOwner.OrganisationNumber)
                    ? new PartyLookup { OrgNo = instanceOwner.OrganisationNumber }
                    : new PartyLookup { Ssn = instanceOwner.PersonNumber }
            );
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to look up party for instance owner.");
        }

        Guid? instanceOwnerPartyUuid = instanceOwnerParty?.PartyUuid;

        AppIdentifier appIdentifier = new(instanceMutator.Instance.AppId);
        (signeeContexts, bool delegateSuccess) = await signingDelegationService.DelegateSigneeRights(
            taskId,
            instanceIdCombo,
            instanceOwnerPartyUuid,
            appIdentifier,
            signeeContexts,
            ct,
            telemetry
        );

        ApplicationMetadata applicationMetadata = await _appMetadata.GetApplicationMetadata();

        using var altinnCdnClient = new AltinnCdnClient();

        AltinnCdnOrgs altinnCdnOrgs = await altinnCdnClient.GetOrgs(ct);

        AltinnCdnOrgDetails? serviceOwnerDetails = altinnCdnOrgs.Orgs?.GetValueOrDefault(applicationMetadata.Org);

        Party serviceOwnerParty = await altinnPartyClient.LookupParty(
            new PartyLookup { OrgNo = serviceOwnerDetails?.Orgnr }
        );

        if (delegateSuccess)
        {
            foreach (SigneeContext signeeContext in signeeContexts)
            {
                if (signeeContext.SigneeState.IsMessagedForCallToSign)
                {
                    continue;
                }

                try
                {
                    Party signingParty = signeeContext.Signee.GetParty();

                    await _signingCallToActionService.SendSignCallToAction(
                        signeeContext.Notifications?.OnSignatureAccessRightsDelegated,
                        appIdentifier,
                        new InstanceIdentifier(instanceMutator.Instance),
                        signingParty,
                        serviceOwnerParty,
                        signatureConfiguration.CorrespondenceResources
                    );
                    signeeContext.SigneeState.IsMessagedForCallToSign = true;
                }
                catch (ConfigurationException e)
                {
                    _logger.LogError(e, "Correspondence configuration error: {Exception}", e.Message);
                    signeeContext.SigneeState.IsMessagedForCallToSign = false;
                    signeeContext.SigneeState.CallToSignFailedReason = $"Correspondence configuration error.";
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Correspondence send failed: {Exception}", e.Message);
                    signeeContext.SigneeState.IsMessagedForCallToSign = false;
                    signeeContext.SigneeState.CallToSignFailedReason = $"Correspondence configuration error.";
                }
            }
        }

        // Saves the signee context state to Storage
        // ! TODO: Remove nullable
        instanceMutator.AddBinaryDataElement(
            dataTypeId: signatureConfiguration.SigneeStatesDataTypeId!,
            contentType: ApplicationJsonContentType,
            filename: null,
            bytes: JsonSerializer.SerializeToUtf8Bytes(signeeContexts, _jsonSerializerOptions)
        );

        await Task.CompletedTask;

        return signeeContexts;
    }

    public async Task<List<SigneeContext>> GetSigneeContexts(
        IInstanceDataAccessor instanceDataAccessor,
        AltinnSignatureConfiguration signatureConfiguration
    )
    {
        using Activity? activity = telemetry?.StartReadSigneesActivity();

        // If no SigneeStatesDataTypeId is set, delegated signing is not enabled and there is nothing to download.
        List<SigneeContext> signeeContexts = signatureConfiguration.SigneeStatesDataTypeId is not null
            ? await DownloadSigneeContexts(instanceDataAccessor, signatureConfiguration)
            : [];

        var taskId = instanceDataAccessor.Instance.Process.CurrentTask.ElementId;

        List<SignDocument> signDocuments = await DownloadSignDocuments(instanceDataAccessor, signatureConfiguration);

        await SynchronizeSigneeContextsWithSignDocuments(taskId, signeeContexts, signDocuments);

        return signeeContexts;
    }

    public async Task Sign(UserActionContext userActionContext, ProcessTask currentTask)
    {
        using Activity? activity = telemetry?.StartSignActivity();

        ApplicationMetadata appMetadata = await _appMetadata.GetApplicationMetadata();
        AltinnSignatureConfiguration? signatureConfiguration = currentTask
            .ExtensionElements
            ?.TaskExtension
            ?.SignatureConfiguration;

        List<string> dataTypeIds = signatureConfiguration?.DataTypesToSign ?? [];

        List<DataType>? dataTypesToSign = appMetadata
            .DataTypes?.Where(d => dataTypeIds.Contains(d.Id, StringComparer.OrdinalIgnoreCase))
            .ToList();
        List<DataElementSignature> dataElementSignatures = GetDataElementSignatures(
            userActionContext.Instance.Data,
            dataTypesToSign
        );

        string signatureDataType =
            GetDataTypeForSignature(currentTask, userActionContext.Instance.Data, dataTypesToSign)
            ?? throw new ApplicationConfigException(
                "Missing configuration for signing. Check that the task has a signature configuration and that the data types to sign are defined."
            );

        SignatureContext signatureContext = new(
            new InstanceIdentifier(userActionContext.Instance),
            currentTask.Id,
            signatureDataType,
            await GetSignee(userActionContext),
            dataElementSignatures
        );

        await _signClient.SignDataElements(signatureContext);

        try
        {
            SendCorrespondenceResponse? result = await _signingReceiptService.SendSignatureReceipt(
                signatureContext.InstanceIdentifier,
                signatureContext.Signee,
                dataElementSignatures,
                userActionContext,
                signatureConfiguration?.CorrespondenceResources
            );

            if (result is not null)
            {
                _logger.LogInformation(
                    "Correspondence successfully sent to {Recipients}",
                    string.Join(", ", result.Correspondences.Select(x => x.Recipient))
                );
            }
        }
        catch (ConfigurationException e)
        {
            // TODO: What do we do here? Probably nothing.
            _logger.LogWarning(e, "Correspondence configuration error: {Exception}", e.Message);
        }
        catch (Exception e)
        {
            // TODO: What do we do here? This failure is pretty silent... but throwing would cause havoc
            _logger.LogError(e, "Correspondence send failed: {Exception}", e.Message);
        }
    }

    public void RemoveSigneeState(IInstanceDataMutator instanceMutator, string signeeStatesDataTypeId)
    {
        using Activity? activity = telemetry?.StartRemoveSigneeStateActivity();

        IEnumerable<DataElement> signeeStateDataElements = instanceMutator.GetDataElementsForType(
            signeeStatesDataTypeId
        );

        DataElement? signeeStateDataElement = signeeStateDataElements.SingleOrDefault();
        if (signeeStateDataElement is not null)
        {
            instanceMutator.RemoveDataElement(signeeStateDataElement);
        }
    }

    public void RemoveAllSignatures(IInstanceDataMutator instanceMutator, string signatureDataType)
    {
        using Activity? activity = telemetry?.StartRemoveAllSignaturesActivity();

        IEnumerable<DataElement> signatures = instanceMutator.GetDataElementsForType(signatureDataType);

        foreach (DataElement signature in signatures)
        {
            instanceMutator.RemoveDataElement(signature);
        }
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
        if (signeeProviderId is null)
            return null;

        ISigneeProvider signeeProvider =
            signeeProviders.FirstOrDefault(sp => sp.Id == signeeProviderId)
            ?? throw new SigneeProviderNotFoundException(
                $"No signee provider found for task {instance.Process.CurrentTask.ElementId} with signeeProviderId {signeeProviderId}. Please add an implementation of the {nameof(ISigneeProvider)} interface with that ID or correct the ID."
            );

        SigneesResult signeesResult = await signeeProvider.GetSigneesAsync(instance);
        return signeesResult;
    }

    private static List<DataElementSignature> GetDataElementSignatures(
        IEnumerable<DataElement> dataElements,
        IEnumerable<DataType>? dataTypesToSign
    )
    {
        List<DataElementSignature> connectedDataElements = [];
        if (dataTypesToSign.IsNullOrEmpty())
        {
            return connectedDataElements;
        }

        foreach (DataType dataType in dataTypesToSign)
        {
            connectedDataElements.AddRange(
                dataElements
                    .Where(d => d.DataType.Equals(dataType.Id, StringComparison.OrdinalIgnoreCase))
                    .Select(d => new DataElementSignature(d.Id))
            );
        }

        return connectedDataElements;
    }

    private static string? GetDataTypeForSignature(
        ProcessTask currentTask,
        IEnumerable<DataElement> dataElements,
        IEnumerable<DataType>? dataTypesToSign
    )
    {
        string? signatureDataType = currentTask
            .ExtensionElements
            ?.TaskExtension
            ?.SignatureConfiguration
            ?.SignatureDataType;

        if (dataTypesToSign.IsNullOrEmpty())
        {
            return null;
        }

        var dataElementMatchExists = dataElements.Any(de =>
            dataTypesToSign.Any(dt => string.Equals(dt.Id, de.DataType, StringComparison.OrdinalIgnoreCase))
        );
        var allDataTypesAreOptional = dataTypesToSign.All(d => d.MinCount == 0);
        return dataElementMatchExists || allDataTypesAreOptional ? signatureDataType : null;
    }

    private static async Task<Signee> GetSignee(UserActionContext context)
    {
        switch (context.Authentication)
        {
            case Authenticated.User user:
            {
                UserProfile userProfile = await user.LookupProfile();
                Party orgProfile = await user.LookupSelectedParty();

                return new Signee
                {
                    UserId = userProfile.UserId.ToString(CultureInfo.InvariantCulture),
                    PersonNumber = userProfile.Party.SSN,
                    OrganisationNumber = orgProfile.OrgNumber,
                };
            }
            case Authenticated.SelfIdentifiedUser selfIdentifiedUser:
                return new Signee { UserId = selfIdentifiedUser.UserId.ToString(CultureInfo.InvariantCulture) };
            case Authenticated.SystemUser systemUser:
                return new Signee
                {
                    SystemUserId = systemUser.SystemUserId[0],
                    OrganisationNumber = systemUser.SystemUserOrgNr.Get(OrganisationNumberFormat.Local),
                };
            default:
                throw new SigningException("Could not get signee");
        }
    }

    private async Task<SigneeContext> GenerateSigneeContext(
        string taskId,
        SigneeParty signeeParty,
        CancellationToken ct
    )
    {
        Models.Signee signee = await From(
            signeeParty.SocialSecurityNumber,
            signeeParty.OnBehalfOfOrganisation?.OrganisationNumber,
            null,
            altinnPartyClient.LookupParty
        );

        Party party = signee.GetParty();

        Models.Notifications? notifications = signeeParty.Notifications;

        Email? emailNotification = notifications?.OnSignatureAccessRightsDelegated?.Email;
        if (emailNotification is not null && emailNotification.EmailAddress is null)
        {
            emailNotification.EmailAddress = party.Organization?.EMailAddress;
        }

        Sms? smsNotification = notifications?.OnSignatureAccessRightsDelegated?.Sms;
        if (smsNotification is not null && smsNotification.MobileNumber is null)
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

    private static async Task<List<SigneeContext>> DownloadSigneeContexts(
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

        DataElement signeeStateDataElement =
            dataElements.SingleOrDefault()
            ?? throw new SigningException(
                $"Failed to find the data element containing signee contexts using dataTypeId {signatureConfiguration.SigneeStatesDataTypeId}."
            );

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

        List<DataElement> signatureDataElements = instanceDataAccessor
            .Instance.Data.Where(x => x.DataType == signatureDataTypeId)
            .ToList();

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

        if (signeeInfo.PersonNumber is not null)
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
                IsMessagedForCallToSign = true,
                IsReceiptSent = false,
            },
            SignDocument = signDocument,
        };
    }

    private static bool IsPersonOnBehalfOfOrgSignDocument(SignDocument signDocument)
    {
        return signDocument.SigneeInfo.PersonNumber is not null
            && signDocument.SigneeInfo.OrganisationNumber is not null;
    }

    private static bool IsPersonSignDocument(SignDocument signDocument)
    {
        return signDocument.SigneeInfo.PersonNumber is not null && signDocument.SigneeInfo.OrganisationNumber is null;
    }

    private static bool IsOrgSignDocument(SignDocument signDocument)
    {
        return signDocument.SigneeInfo.OrganisationNumber is not null;
    }

    private static bool IsSystemSignDocument(SignDocument signDocument)
    {
        return signDocument.SigneeInfo.OrganisationNumber is not null && signDocument.SigneeInfo.SystemUserId.HasValue;
    }
}
