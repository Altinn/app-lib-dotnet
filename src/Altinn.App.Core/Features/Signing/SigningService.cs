using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Mocks;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Internal.Registers;
using Altinn.App.Core.Internal.Sign;
using Altinn.Platform.Register.Models;

namespace Altinn.App.Core.Features.Signing;

internal sealed class SigningService(
    ISigneeProvider signeeProvider, /*ISignClient signClient, IInstanceClient instanceClient, IAppMetadata appMetadata,*/
    Telemetry telemetry,
    IPersonClient personClient,
    IOrganizationClient organisationClient,
    IAltinnPartyClient altinnPartyClient,
    ISigningDelegationService signingDelegationService,
    ISigningNotificationService signingNotificationService
)
{
    internal async void AssignSignees(CancellationToken ct)
    {
        using var activity = telemetry.StartAssignSigneesActivity();
        List<SigneeState> state = /*StorageClient.GetSignState ??*/
        [];

        SigneesResult signeeResult = await signeeProvider.GetSigneesAsync();

        List<SigneeContext> personSigneeContexts = await GetPersonSigneeContexts(state, signeeResult, ct);
        List<SigneeContext> organisationSigneeContexts = await GetOrganisationSigneeContexts(state, signeeResult, ct);

        List<SigneeContext> signeeContexts = [.. personSigneeContexts, .. organisationSigneeContexts];

        await ProcessSignees(signeeContexts, ct);

        // TODO: StorageClient.SetSignState(state);
        throw new NotImplementedException();
    }

    internal async Task ProcessSignees(List<SigneeContext> signeeContexts, CancellationToken ct)
    {
        using var activity = telemetry.StartAssignSigneesActivity();

        await signingDelegationService.DelegateSigneeRights(signeeContexts, ct);
        await signingNotificationService.NotifySignees(signeeContexts, ct);

        // TODO: StorageClient.SetSignState(state);
        throw new NotImplementedException();
    }

    private async Task<List<SigneeContext>> GetPersonSigneeContexts(
        List<SigneeState> state,
        SigneesResult signeeResult,
        CancellationToken cancellationToken
    )
    {
        List<SigneeContext> personSigneeContainer = []; //TODO rename
        foreach (PersonSignee personSignee in signeeResult.PersonSignees)
        {
            Person? person = await personClient.GetPerson(
                personSignee.SocialSecurityNumber,
                personSignee.LastName,
                cancellationToken
            );

            if (person is null)
            {
                //TODO: persist state and throw
            }

            Party? party = await altinnPartyClient.LookupParty(
                new PartyLookup { Ssn = personSignee.SocialSecurityNumber }
            );
            //TODO: handle null

            SigneeState signeeState =
                state.FirstOrDefault(s => s.PartyId == s.PartyId)
                ?? new SigneeState(
                    partyId: party.PartyId,
                    displayName: party.Name,
                    mobilePhone: personSignee.Notification.MobileNumber ?? person.MobileNumber,
                    email: personSignee.Notification.EmailAddress,
                    taskId: "" //TODO: get current task
                );
            personSigneeContainer.Add(new SigneeContext(signeeState, personSignee));
        }
        return personSigneeContainer;
    }

    private async Task<List<SigneeContext>> GetOrganisationSigneeContexts(
        List<SigneeState> state,
        SigneesResult signeeResult,
        CancellationToken ct
    )
    {
        List<SigneeContext> organisationSigneeContainer = []; //TODO rename
        foreach (OrganisationSignee organisationSignee in signeeResult.OrganisationSignees)
        {
            Organization? organisation = await organisationClient.GetOrganization(
                organisationSignee.OrganisationNumber
            );

            if (organisation is null)
            {
                //TODO: persist state and throw
            }

            Party? party = await altinnPartyClient.LookupParty(
                new PartyLookup { OrgNo = organisationSignee.OrganisationNumber }
            );
            //TODO: handle null

            SigneeState signeeState =
                state.FirstOrDefault(s => s.PartyId == s.PartyId)
                ?? new SigneeState(
                    partyId: party.PartyId,
                    displayName: party.Name,
                    mobilePhone: organisationSignee.Notification.MobileNumber ?? organisation.MobileNumber,
                    email: organisationSignee.Notification.EmailAddress ?? organisation.EMailAddress,
                    taskId: "" //TODO: get current task
                );
            organisationSigneeContainer.Add(new SigneeContext(signeeState, organisationSignee));
        }
        return organisationSigneeContainer;
    }

    internal List<Signee> ReadSignees()
    {
        using var activity = telemetry.StartReadSigneesActivity();
        // TODO: Get signees from state

        // TODO: Get signees from policy

        // TODO: Get signees from interface

        throw new NotImplementedException();
    }

    internal async Task Sign(Signee signee)
    {
        using var activity = telemetry.StartSignActivity();
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
