using System.Diagnostics;
using System.Globalization;
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
    IPersonClient personClient,
    IOrganizationClient organisationClient,
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

    public async Task<SigneesResult?> GetSignees(Instance instance, AltinnSignatureConfiguration signatureConfiguration)
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

    public async Task<List<SigneeContext>> InitializeSignees(
        IInstanceDataMutator instanceMutator,
        AltinnSignatureConfiguration signatureConfiguration,
        CancellationToken ct
    )
    {
        using Activity? activity = telemetry?.StartAssignSigneesActivity();

        Instance instance = instanceMutator.Instance;
        string taskId = instance.Process.CurrentTask.ElementId;

        SigneesResult? signeesResult = await GetSignees(instance, signatureConfiguration);
        if (signeesResult is null)
        {
            return [];
        }

        List<SigneeContext> personSigneeContexts = await GetPersonSigneeContexts(taskId, signeesResult, ct);
        List<SigneeContext> organisationSigneeContexts = await GetOrganisationSigneeContexts(taskId, signeesResult, ct);
        List<SigneeContext> signeeContexts = [.. personSigneeContexts, .. organisationSigneeContexts];

        _logger.LogInformation("Assigning signees to task {TaskId}: {SigneeContexts}", taskId, signeeContexts.Count);

        return signeeContexts;
    }

    public async Task<List<SigneeContext>> ProcessSignees(
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
        IInstanceDataMutator instanceMutator,
        AltinnSignatureConfiguration signatureConfiguration
    )
    {
        using Activity? activity = telemetry?.StartReadSigneesActivity();

        List<SigneeContext> signeeContexts = await DownloadSigneeContexts(instanceMutator, signatureConfiguration);
        List<SignDocument> signDocuments = await DownloadSignDocuments(instanceMutator, signatureConfiguration);

        await SynchronizeSigneeContextsWithSignDocuments(instanceMutator, signeeContexts, signDocuments);

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

    private async Task<List<SigneeContext>> GetPersonSigneeContexts(
        string taskId,
        SigneesResult signeeResult,
        CancellationToken ct
    )
    {
        List<SigneeContext> personSigneeContexts = [];
        foreach (PersonSignee personSignee in signeeResult.PersonSignees)
        {
            var lastName = personSignee.FullName.Split(" ").Last().ToLower(CultureInfo.InvariantCulture);
            Person? person =
                await personClient.GetPerson(personSignee.SocialSecurityNumber, lastName, ct)
                ?? throw new SignaturePartyNotValidException(
                    $"The given SSN and last name did not match any person in the registry."
                );
            Party party = await altinnPartyClient.LookupParty(
                new PartyLookup { Ssn = personSignee.SocialSecurityNumber }
            );

            Sms? smsNotification = personSignee.Notifications?.OnSignatureAccessRightsDelegated?.Sms;
            if (smsNotification is not null && smsNotification.MobileNumber is null)
            {
                smsNotification.MobileNumber = person.MobileNumber;
            }

            personSigneeContexts.Add(
                new SigneeContext
                {
                    TaskId = taskId,
                    Party = party,
                    PersonSignee = personSignee,
                    SigneeState = new SigneeState(),
                }
            );
        }

        return personSigneeContexts;
    }

    private async Task<List<SigneeContext>> GetOrganisationSigneeContexts(
        string taskId,
        SigneesResult signeeResult,
        CancellationToken? ct = null
    )
    {
        List<SigneeContext> organisationSigneeContexts = []; //TODO rename
        foreach (OrganisationSignee organisationSignee in signeeResult.OrganisationSignees)
        {
            Organization? organisation =
                await organisationClient.GetOrganization(organisationSignee.OrganisationNumber)
                ?? throw new SignaturePartyNotValidException(
                    $"Signature party with organisation number {organisationSignee.OrganisationNumber} was not found in the registry."
                );
            Party party = await altinnPartyClient.LookupParty(
                new PartyLookup { OrgNo = organisationSignee.OrganisationNumber }
            );

            //TODO: Is this the correct place to set email to registry fallback? Maybe move it to notification service?
            Email? emailNotification = organisationSignee.Notifications?.OnSignatureAccessRightsDelegated?.Email;
            if (emailNotification is not null && emailNotification.EmailAddress is null)
            {
                emailNotification.EmailAddress = organisation.EMailAddress;
            }

            Sms? smsNotification = organisationSignee.Notifications?.OnSignatureAccessRightsDelegated?.Sms;
            if (smsNotification is not null && smsNotification.MobileNumber is null)
            {
                smsNotification.MobileNumber = organisation.MobileNumber;
            }

            organisationSigneeContexts.Add(
                new SigneeContext
                {
                    TaskId = taskId,
                    Party = party,
                    OrganisationSignee = organisationSignee,
                    SigneeState = new SigneeState(),
                }
            );
        }

        return organisationSigneeContexts;
    }

    private static async Task<List<SigneeContext>> DownloadSigneeContexts(
        IInstanceDataMutator instanceMutator,
        AltinnSignatureConfiguration signatureConfiguration
    )
    {
        string signeeStatesDataTypeId =
            signatureConfiguration.SigneeStatesDataTypeId
            ?? throw new ApplicationConfigException(
                "SigneeStatesDataTypeId is not set in the signature configuration."
            );

        IEnumerable<DataElement> dataElements = instanceMutator.GetDataElementsForType(signeeStatesDataTypeId);

        DataElement signeeStateDataElement =
            dataElements.SingleOrDefault()
            ?? throw new ApplicationException(
                $"Failed to find the data element containing signee contexts using dataTypeId {signatureConfiguration.SigneeStatesDataTypeId}."
            );

        ReadOnlyMemory<byte> data = await instanceMutator.GetBinaryData(signeeStateDataElement);
        string signeeStateSerialized = Encoding.UTF8.GetString(data.ToArray());

        List<SigneeContext> signeeContexts =
            JsonSerializer.Deserialize<List<SigneeContext>>(signeeStateSerialized, _jsonSerializerOptions) ?? [];

        return signeeContexts;
    }

    private async Task<List<SignDocument>> DownloadSignDocuments(
        IInstanceDataMutator instanceMutator,
        AltinnSignatureConfiguration signatureConfiguration
    )
    {
        string signatureDataTypeId =
            signatureConfiguration.SignatureDataType
            ?? throw new ApplicationConfigException("SignatureDataType is not set in the signature configuration.");

        List<DataElement> signatureDataElements = instanceMutator
            .Instance.Data.Where(x => x.DataType == signatureDataTypeId)
            .ToList();

        try
        {
            SignDocument[] signDocuments = await Task.WhenAll(
                signatureDataElements.Select(async signatureDataElement =>
                {
                    try
                    {
                        ReadOnlyMemory<byte> data = await instanceMutator.GetBinaryData(signatureDataElement);
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
                })
            );

            return [.. signDocuments];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download signature documents.");
            throw;
        }
    }

    /// <summary>
    /// This method exists to ensure we have a SigneeContext for both signees that have been delegated access to sign and signees that have signed using access granted through the policy.xml file.
    /// </summary>
    private async Task SynchronizeSigneeContextsWithSignDocuments(
        IInstanceDataMutator instanceMutator,
        List<SigneeContext> signeeContexts,
        List<SignDocument> signDocuments
    )
    {
        foreach (SignDocument signDocument in signDocuments)
        {
            SigneeContext? matchingSigneeContext = signeeContexts.FirstOrDefault(x =>
                x.PersonSignee?.SocialSecurityNumber == signDocument.SigneeInfo.PersonNumber
                || x.OrganisationSignee?.OrganisationNumber == signDocument.SigneeInfo.OrganisationNumber
            );

            if (matchingSigneeContext is not null)
            {
                // If the signee has been delegated access to sign there will be a matching SigneeContext. Setting the sign document property on this context.
                matchingSigneeContext.SignDocument = signDocument;
            }
            else
            {
                // If the signee has signed using access granted through the policy.xml file, there is no persisted signee context. We create a signee context on the fly.
                Party party = await altinnPartyClient.LookupParty(
                    new PartyLookup
                    {
                        Ssn = signDocument.SigneeInfo.PersonNumber,
                        OrgNo = signDocument.SigneeInfo.OrganisationNumber,
                    }
                );

                PersonSignee? personSignee = party.Person is not null
                    ? new PersonSignee
                    {
                        SocialSecurityNumber = party.Person.SSN,
                        DisplayName = party.Person.Name,
                        FullName = party.Person.Name,
                        OnBehalfOfOrganisation = party.Organization?.Name,
                    }
                    : null;

                OrganisationSignee? organisationSignee = party.Organization is not null
                    ? new OrganisationSignee
                    {
                        OrganisationNumber = party.Organization.OrgNumber,
                        DisplayName = party.Organization.Name,
                    }
                    : null;

                signeeContexts.Add(
                    new SigneeContext
                    {
                        TaskId = instanceMutator.Instance.Process.CurrentTask.ElementId,
                        Party = party,
                        PersonSignee = personSignee,
                        OrganisationSignee = organisationSignee,
                        SigneeState = new SigneeState()
                        {
                            IsAccessDelegated = true,
                            SignatureRequestEmailSent = true,
                            SignatureRequestSmsSent = true,
                            IsReceiptSent = false,
                        },
                    }
                );
            }
        }
    }
}
