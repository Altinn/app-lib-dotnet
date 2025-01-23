using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Altinn.App.Core.Features.Signing.Exceptions;
using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Mocks;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Registers;
using Altinn.App.Core.Models;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using JsonException = Newtonsoft.Json.JsonException;

namespace Altinn.App.Core.Features.Signing;

internal sealed class SigningService(
    IAltinnPartyClient altinnPartyClient,
    ISigningDelegationService signingDelegationService,
    ISigningNotificationService signingNotificationService,
    IEnumerable<ISigneeProvider> signeeProviders,
    ILogger<SigningService> logger,
    Telemetry? telemetry = null
) : ISigningService
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly ILogger<SigningService> _logger = logger;
    private const string ApplicationJsonContentType = "application/json";

    public async Task<SigneesResult?> GetSigneesFromProvider(
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

    public async Task<List<SigneeContext>> CreateSigneeContexts(
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
            var signeeContext = await CreateSigneeContext(taskId, signeeParty, ct);
            signeeContexts.Add(signeeContext);
        }

        _logger.LogInformation("Assigning signees to task {TaskId}: {SigneeContexts}", taskId, signeeContexts.Count);

        return signeeContexts;
    }

    public async Task<List<SigneeContext>> DelegateAccessAndNotifySignees(
        string taskId,
        Party delegatorParty,
        IInstanceDataMutator instanceMutator,
        List<SigneeContext> signeeContexts,
        AltinnSignatureConfiguration signatureConfiguration,
        CancellationToken ct
    )
    {
        using Activity? activity = telemetry?.StartAssignSigneesActivity();

        string instanceId = instanceMutator.Instance.Id;

        AppIdentifier appIdentifier = new(instanceMutator.Instance.AppId);
        (signeeContexts, var delegateSuccess) = await signingDelegationService.DelegateSigneeRights(
            taskId,
            instanceId,
            delegatorParty ?? throw new InvalidOperationException("Delegator party is null"),
            appIdentifier,
            signeeContexts,
            ct,
            telemetry
        );

        if (delegateSuccess)
        {
            await signingNotificationService.NotifySignatureTask(signeeContexts, ct);
        }

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

        List<SignDocument> signDocuments = await DownloadSignDocuments(instanceDataAccessor, signatureConfiguration);

        await SynchronizeSigneeContextsWithSignDocuments(instanceDataAccessor, signeeContexts, signDocuments);

        return signeeContexts;
    }

    //TODO: There is already logic for the sign action in the SigningUserAction class. Maybe move most of it here?
    public async Task Sign(SigneeContext signee)
    {
        using Activity? activity = telemetry?.StartSignActivity();
        // var state = StorageClient.GetSignState(...);
        try
        {
            // SigneeState signeeState = state.FirstOrDefault(s => s.Id == signee.UserId)
            // if(signeeState.hasSigned is false)
            // {
            //      await signClient.SignDataElements();
            //      signeeState.hasSigned = true;
            // }
            // if(signeeState.IsReceiptSent is false)
            // {
            var correspondanceClient = new CorrespondanceClientMock();
            var correspondence = new BaseCorrespondenceExt
            {
                ResourceId = "",
                Sender = "",
                SendersReference = "",
                VisibleFrom = DateTimeOffset.Now,
            };
            var request = new InitializeCorrespondenceRequestMock
            {
                Correspondence = correspondence,
                Recipients =
                [ /*SigneeState.Id*/
                ],
                ExistingAttachments = [], // TODO: all relevant documents
            };
            //      correspondanceClient.SendMessage(...);
            //      signeeState.IsReceiptSent = true;
            // }
            await Task.CompletedTask;
        }
        catch
        {
            // TODO: log + telemetry?
        }
        finally
        {
            // StorageClient.SetSignState(state);
        }

        throw new NotImplementedException();
    }

    private async Task<SigneeContext> CreateSigneeContext(string taskId, SigneeParty signeeParty, CancellationToken ct)
    {
        Party party = await altinnPartyClient.LookupParty(
            new PartyLookup
            {
                Ssn = signeeParty.SocialSecurityNumber,
                OrgNo = signeeParty.OnBehalfOfOrganisation?.OrganisationNumber,
            }
        );

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
            Party = party,
            SigneeState = new SigneeState(),
            Notifications = notifications,
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
            ?? throw new ApplicationException(
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
    private async Task SynchronizeSigneeContextsWithSignDocuments(
        IInstanceDataAccessor instanceDataAccessor,
        List<SigneeContext> signeeContexts,
        List<SignDocument> signDocuments
    )
    {
        foreach (SignDocument signDocument in signDocuments)
        {
            SigneeContext? matchingSigneeContext = signeeContexts.FirstOrDefault(x =>
                x.Party?.SSN == signDocument.SigneeInfo.PersonNumber
                || x.Party?.OrgNumber == signDocument.SigneeInfo.OrganisationNumber
            );

            if (matchingSigneeContext is not null)
            {
                // If the signee has been delegated access to sign there will be a matching SigneeContext. Setting the sign document property on this context.
                matchingSigneeContext.SignDocument = signDocument;
            }
            else
            {
                // If the signee has signed using access granted through the policy.xml file, there is no persisted signee context. We create a signee context on the fly.
                SigneeContext signeeContext = await CreateSigneeContextFromSignDocument(
                    instanceDataAccessor,
                    signDocument
                );

                signeeContexts.Add(signeeContext);
            }
        }
    }

    private async Task<SigneeContext> CreateSigneeContextFromSignDocument(
        IInstanceDataAccessor instanceDataAccessor,
        SignDocument signDocument
    )
    {
        Party party = await altinnPartyClient.LookupParty(
            new PartyLookup
            {
                Ssn = signDocument.SigneeInfo.OrganisationNumber is null ? signDocument.SigneeInfo.PersonNumber : null,
                OrgNo = signDocument.SigneeInfo.OrganisationNumber,
            }
        );

        return new SigneeContext
        {
            TaskId = instanceDataAccessor.Instance.Process.CurrentTask.ElementId,
            Party = party,
            SigneeState = new SigneeState()
            {
                IsAccessDelegated = true,
                SignatureRequestEmailSent = true,
                SignatureRequestSmsSent = true,
                IsReceiptSent = false,
            },
            SignDocument = signDocument,
        };
    }
}
