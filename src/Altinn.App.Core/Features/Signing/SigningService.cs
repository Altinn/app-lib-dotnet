using System.Diagnostics;
using System.Text.Json;
using Altinn.App.Core.Features.Signing.Exceptions;
using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Mocks;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Registers;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Features.Signing;

internal sealed class SigningService(
    /*ISignClient signClient, IInstanceClient instanceClient, IAppMetadata appMetadata,*/
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
        Instance instance,
        AltinnSignatureConfiguration signatureConfiguration,
        CancellationToken ct
    )
    {
        using Activity? activity = telemetry?.StartAssignSigneesActivity();
        string taskId = instance.Process.CurrentTask.ElementId;

        SigneesResult? signeesResult = await GetSignees(instance, signatureConfiguration);
        if (signeesResult is null)
        {
            return [];
        }

        List<SigneeContext> personSigneeContexts = await GetPersonSigneeContexts(taskId, signeesResult, ct);
        List<SigneeContext> organisationSigneeContexts = await GetOrganisationSigneeContexts(taskId, signeesResult, ct);
        List<SigneeContext> signeeContexts = [.. personSigneeContexts, .. organisationSigneeContexts];

        // TODO: StorageClient.SetSignState(signeeContexts); ?
        return signeeContexts;
    }

    public async Task<List<SigneeContext>> ProcessSignees(
        IInstanceDataMutator instanceMutator,
        List<SigneeContext> signeeContexts,
        AltinnSignatureConfiguration signatureConfiguration,
        CancellationToken ct
    )
    {
        using Activity? activity = telemetry?.StartAssignSigneesActivity();
        string taskId = instanceMutator.Instance.Process.CurrentTask.ElementId;

        await signingDelegationService.DelegateSigneeRights(taskId, instanceMutator.Instance, signeeContexts, ct);

        //TODO: If something fails inside DelegateSigneeRights, abort and don't send notifications. Set error state in SigneeState.

        await signingNotificationService.NotifySignatureTask(signeeContexts, ct);

        instanceMutator.AddBinaryDataElement(
            dataTypeId: signatureConfiguration.SignatureDataTypeId,
            contentType: ApplicationJsonContentType,
            filename: null,
            bytes: JsonSerializer.SerializeToUtf8Bytes(signeeContexts)
        );
        return signeeContexts;
    }

    public Task<List<SigneeContext>> GetSigneeContexts()
    {
        using Activity? activity = telemetry?.StartReadSigneesActivity();
        // TODO: Get signees from state

        // TODO: Get signees from policy??

        return Task.FromResult(
            new List<SigneeContext>
            {
                new(
                    "taskId",
                    50000123,
                    new PersonSignee
                    {
                        DisplayName = "Klara Ku",
                        LastName = "Ku",
                        SocialSecurityNumber = "12345678911",
                    },
                    new SigneeState
                    {
                        IsAccessDelegated = true,
                        DelegationFailedReason = null,
                        SignatureRequestSmsSent = false,
                        SignatureRequestSmsNotSentReason = null,
                        SignatureRequestEmailSent = false,
                        SignatureRequestEmailNotSentReason = null,
                        IsReceiptSent = false,
                    }
                ),
                new(
                    "taskId",
                    50000124,
                    new OrganisationSignee { DisplayName = "Skog og Fjell", OrganisationNumber = "043871668" },
                    new SigneeState
                    {
                        IsAccessDelegated = false,
                        DelegationFailedReason = null,
                        SignatureRequestSmsSent = false,
                        SignatureRequestSmsNotSentReason = null,
                        SignatureRequestEmailSent = false,
                        SignatureRequestEmailNotSentReason = null,
                        IsReceiptSent = false,
                    }
                ),
                new(
                    "taskId",
                    50000125,
                    new PersonSignee
                    {
                        DisplayName = "Pengelens Partner",
                        SocialSecurityNumber = "01899699552",
                        LastName = "Partner",
                    },
                    new SigneeState
                    {
                        IsAccessDelegated = true,
                        DelegationFailedReason = null,
                        SignatureRequestSmsSent = false,
                        SignatureRequestSmsNotSentReason = null,
                        SignatureRequestEmailSent = false,
                        SignatureRequestEmailNotSentReason = null,
                        IsReceiptSent = false,
                    }
                ),
                new(
                    "taskId",
                    50000126,
                    new PersonSignee
                    {
                        DisplayName = "Gjentakende Forelder",
                        SocialSecurityNumber = "17858296439",
                        LastName = "Forelder",
                    },
                    new SigneeState
                    {
                        IsAccessDelegated = false,
                        DelegationFailedReason = null,
                        SignatureRequestSmsSent = false,
                        SignatureRequestSmsNotSentReason = null,
                        SignatureRequestEmailSent = false,
                        SignatureRequestEmailNotSentReason = null,
                        IsReceiptSent = false,
                    }
                ),
            }
        );
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
            _logger.LogInformation(
                "Looking up person with SSN {SocialSecurityNumber} and last name {LastName}.",
                personSignee.SocialSecurityNumber,
                personSignee.LastName.Split(" ").Last()
            );
            Person? person = await personClient.GetPerson(
                personSignee.SocialSecurityNumber,
                personSignee.LastName.Split(" ").Last(),
                ct
            );

            if (person is null)
            {
                //TODO: persist state and throw

                throw new SignaturePartyNotValidException(
                    $"The given SSN and last name did not match any person in the registry."
                );
            }

            Party? party = await altinnPartyClient.LookupParty(
                new PartyLookup { Ssn = personSignee.SocialSecurityNumber }
            );

            Sms? smsNotification = personSignee.Notifications?.OnSignatureAccessRightsDelegated?.Sms;
            if (smsNotification is not null && smsNotification.MobileNumber is null)
            {
                smsNotification.MobileNumber = person.MobileNumber;
            }

            personSigneeContexts.Add(new SigneeContext(taskId, party.PartyId, personSignee, new SigneeState()));
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
            Organization? organisation = await organisationClient.GetOrganization(
                organisationSignee.OrganisationNumber
            );

            if (organisation is null)
            {
                //TODO: persist state and throw
                throw new SignaturePartyNotValidException(
                    $"Signature party with organisation number {organisationSignee.OrganisationNumber} was not found in the registry."
                );
            }

            Party? party = await altinnPartyClient.LookupParty(
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

            organisationSigneeContexts.Add(new SigneeContext(taskId, party.PartyId, organisationSignee, new SigneeState()));
        }

        return organisationSigneeContexts;
    }
}
