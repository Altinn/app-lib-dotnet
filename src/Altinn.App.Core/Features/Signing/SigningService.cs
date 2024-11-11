using Altinn.App.Core.Features.Signing.Exceptions;
using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Mocks;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Internal.Registers;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Signing;

internal sealed class SigningService(
    ISigneeProvider signeeProvider, /*ISignClient signClient, IInstanceClient instanceClient, IAppMetadata appMetadata,*/
    IPersonClient personClient,
    IOrganizationClient organisationClient,
    IAltinnPartyClient altinnPartyClient,
    ISigningDelegationService signingDelegationService,
    ISigningNotificationService signingNotificationService,
    Telemetry? telemetry
) : ISigningService
{
    public async Task<List<SigneeContext>> InitializeSignees(string taskId, CancellationToken ct)
    {
        using var activity = telemetry?.StartAssignSigneesActivity();

        SigneesResult signeesResult = await signeeProvider.GetSigneesAsync();

        List<SigneeContext> personSigneeContexts = await GetPersonSigneeContexts(taskId, signeesResult, ct);
        List<SigneeContext> organisationSigneeContexts = await GetOrganisationSigneeContexts(taskId, signeesResult, ct);
        List<SigneeContext> signeeContexts = [.. personSigneeContexts, .. organisationSigneeContexts];

        // TODO: StorageClient.SetSignState(signeeContexts); ?
        return signeeContexts;
    }

    public async Task<List<SigneeContext>> ProcessSignees(
        string taskId,
        Instance instance,
        List<SigneeContext> signeeContexts,
        CancellationToken ct
    )
    {
        using var activity = telemetry?.StartAssignSigneesActivity();

        await signingDelegationService.DelegateSigneeRights(taskId, instance, signeeContexts, ct);

        //TODO: If something fails inside DelegateSigneeRights, abort and don't send notifications. Set error state in SigneeState.

        await signingNotificationService.NotifySignatureTask(signeeContexts, ct);

        // TODO: StorageClient.SetSignState(state); ?
        return signeeContexts;
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
            Person? person = await personClient.GetPerson(personSignee.SocialSecurityNumber, personSignee.LastName, ct);

            if (person is null)
            {
                //TODO: persist state and throw
                throw new SignaturePartyNotValidException(
                    $"Signature party with social security number {personSignee.SocialSecurityNumber} was not found in the registry."
                );
            }

            Party? party = await altinnPartyClient.LookupParty(
                new PartyLookup { Ssn = personSignee.SocialSecurityNumber }
            );

            Guid partyUuid =
                party.PartyUuid
                ?? throw new SignaturePartyNotValidException(
                    $"No partyUuid found for signature party with social security number {personSignee.SocialSecurityNumber}." // TODO: ikke gi ut ssn her?!
                );

            Sms? smsNotification = personSignee.Notifications?.OnSignatureTaskReceived?.Sms;
            if (smsNotification is not null && smsNotification.MobileNumber is null)
            {
                smsNotification.MobileNumber = person.MobileNumber;
            }

            personSigneeContexts.Add(new SigneeContext(taskId, partyUuid, personSignee, new SigneeState()));
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

            Guid partyUuid =
                party.PartyUuid
                ?? throw new SignaturePartyNotValidException(
                    $"No partyId found for signature party with organisation number {organisationSignee.OrganisationNumber}."
                );

            //TODO: Is this the correct place to set email to registry fallback? Maybe move it to notification service?
            Email? emailNotification = organisationSignee.Notifications?.OnSignatureTaskReceived?.Email;
            if (emailNotification is not null && emailNotification.EmailAddress is null)
            {
                emailNotification.EmailAddress = organisation.EMailAddress;
            }

            Sms? smsNotification = organisationSignee.Notifications?.OnSignatureTaskReceived?.Sms;
            if (smsNotification is not null && smsNotification.MobileNumber is null)
            {
                smsNotification.MobileNumber = organisation.MobileNumber;
            }

            organisationSigneeContexts.Add(new SigneeContext(taskId, partyUuid, organisationSignee, new SigneeState()));
        }

        return organisationSigneeContexts;
    }

    public List<SigneeContext> ReadSignees()
    {
        using var activity = telemetry?.StartReadSigneesActivity();
        // TODO: Get signees from state

        // TODO: Get signees from policy

        throw new NotImplementedException();
    }

    //TODO: There is already logic for the sign action in the SigningUserAction class. Maybe move most of it here?
    internal async Task Sign(SigneeContext signee)
    {
        using var activity = telemetry?.StartSignActivity();
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
                VisibleFrom = DateTimeOffset.Now
            };
            var request = new InitializeCorrespondenceRequestMock
            {
                Correspondence = correspondence,
                Recipients =
                [ /*SigneeState.Id*/
                ],
                ExistingAttachments = [] // TODO: all relevant documents
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
}
