using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Mocks;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Internal.Registers;
using Altinn.App.Core.Internal.Sign;
using Altinn.Platform.Register.Models;

namespace Altinn.App.Core.Features.Signing;

internal sealed class SigningService(
    ISigneeConfiguration signeeConfiguration, /*ISignClient signClient, IInstanceClient instanceClient, IAppMetadata appMetadata,*/
    Telemetry telemetry,
    IPersonClient personClient,
    IOrganizationClient organisationClient,
    IAltinnPartyClient altinnPartyClient,
    ISmsNotificationClient? smsNotificationClient = null,
    IEmailNotificationClient? emailNotificationClient = null
)
{
    internal async void AssignSignees(CancellationToken ct)
    {
        using var activity = telemetry.StartAssignSigneesActivity();
        List<SigneeState> state = /*StorageClient.GetSignState ??*/
        [];

        SigneeConfigurationResult signeeConfigurationResult = await signeeConfiguration.GetSigneeConfiguration();
        var personSigneeContainer = await GetPersonSigneeStates(state, signeeConfigurationResult, ct);
        var organisationSigneeContainer = await GetOrganisationSigneeStates(state, signeeConfigurationResult, ct);

        List<(SigneeState, SigneeConfig)>? signeeContainer = [.. personSigneeContainer, .. organisationSigneeContainer];

        await DelegateSigneeRights(signeeContainer, ct);
        await NotifySignees(signeeContainer, ct);

        // TODO: StorageClient.SetSignState(state);
        throw new NotImplementedException();
    }

    private async Task<List<(SigneeState, SigneeConfig)>> GetPersonSigneeStates(
        List<SigneeState> state,
        SigneeConfigurationResult signeeConfigurationResult,
        CancellationToken cancellationToken
    )
    {
        List<(SigneeState, SigneeConfig)> personSigneeContainer = []; //TODO rename
        foreach (PersonSigneeConfig personSigneeConfig in signeeConfigurationResult.PersonSigneeConfigs)
        {
            Person? person = await personClient.GetPerson(
                personSigneeConfig.SocialSecurityNumber,
                personSigneeConfig.LastName,
                cancellationToken
            );

            if (person is null)
            {
                //TODO: persist state and throw
            }

            Party? party = await altinnPartyClient.LookupParty(
                new PartyLookup { Ssn = personSigneeConfig.SocialSecurityNumber }
            );
            //TODO: handle null

            SigneeState signeeState =
                state.FirstOrDefault(s => s.PartyId == s.PartyId)
                ?? new SigneeState(
                    partyId: party.PartyId,
                    displayName: party.Name,
                    mobilePhone: personSigneeConfig.Notification.MobileNumber ?? person.MobileNumber,
                    email: personSigneeConfig.Notification.EmailAddress,
                    taskId: "" //TODO: get current task
                );
            personSigneeContainer.Add((signeeState, personSigneeConfig));
        }
        return personSigneeContainer;
    }

    private async Task<List<(SigneeState, SigneeConfig)>> GetOrganisationSigneeStates(
        List<SigneeState> state,
        SigneeConfigurationResult signeeConfigurationResult,
        CancellationToken cancellationToken
    )
    {
        List<(SigneeState, SigneeConfig)> organisationSigneeContainer = []; //TODO rename
        foreach (
            OrganisationSigneeConfig organisationSigneeConfig in signeeConfigurationResult.OrgansiationSigneeConfigs
        )
        {
            Organization? organisation = await organisationClient.GetOrganization(
                organisationSigneeConfig.OrganisationNumber
            );

            if (organisation is null)
            {
                //TODO: persist state and throw
            }

            Party? party = await altinnPartyClient.LookupParty(
                new PartyLookup { OrgNo = organisationSigneeConfig.OrganisationNumber }
            );
            //TODO: handle null

            SigneeState signeeState =
                state.FirstOrDefault(s => s.PartyId == s.PartyId)
                ?? new SigneeState(
                    partyId: party.PartyId,
                    displayName: party.Name,
                    mobilePhone: organisationSigneeConfig.Notification.MobileNumber ?? organisation.MobileNumber,
                    email: organisationSigneeConfig.Notification.EmailAddress ?? organisation.EMailAddress,
                    taskId: "" //TODO: get current task
                );
            organisationSigneeContainer.Add((signeeState, organisationSigneeConfig));
        }
        return organisationSigneeContainer;
    }

    private async Task DelegateSigneeRights(List<(SigneeState, SigneeConfig)> signeeContainer, CancellationToken ct)
    {
        foreach ((SigneeState signeeState, SigneeConfig signeeConfig) in signeeContainer)
            try
            {
                if (signeeState.IsDelegated is false)
                {
                    //TODO: delegateSignAction
                    signeeState.IsDelegated = true;
                }
            }
            catch
            {
                // TODO: log + telemetry?
            }
    }

    private async Task NotifySignees(List<(SigneeState, SigneeConfig)> signeeContainer, CancellationToken ct)
    {
        foreach ((SigneeState signeeState, SigneeConfig signeeConfig) in signeeContainer)
            try
            {
                if (signeeState.IsNotified is false)
                {
                    if (signeeConfig.Notification.ShouldSendSms)
                    {
                        await TrySendSms(smsNotificationClient, signeeConfig.Notification.MobileNumber, ct);
                        signeeState.IsNotified = true;
                    }
                }
            }
            catch
            {
                // TODO: log + telemetry?
            }
    }

    internal List<Signee> ReadSignees()
    {
        using var activity = telemetry.StartReadSigneesActivity();
        // TODO: Get signees from state

        // TODO: Get signees from policy

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

    private static async Task TrySendSms(
        ISmsNotificationClient? smsNotificationClient,
        string smsNumber,
        CancellationToken cancellationToken
    )
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
        //TODO: implement fully
        // if (smsNotificationClient is null && emailNotificationClient is null)
        // {
        //     throw new InvalidOperationException("Unable to send Notification. Neither Sms nor Email notification service available.");
        // }

        // if (smsNotificationClient is not null)
        // {
        //     var notification = new SmsNotification()
        //     {
        //         Body = "",
        //         Recipients = [new SmsRecipient("", "", "")],
        //         SenderNumber = "",
        //         SendersReference = ""
        //     };
        //     await smsNotificationClient.Order(notification, cancellationToken);
        // }

        // if (emailNotificationClient is not null)
        // {
        //     var notification = new EmailNotification
        //     {
        //         Body = "",
        //         Recipients = [new EmailRecipient("")],
        //         Subject = "",
        //         SendersReference = ""
        //     };
        //     await emailNotificationClient.Order(notification, cancellationToken);
        // }
    }

    private static async Task TrySendEmail(
        IEmailNotificationClient? emailNotificationClient,
        string email,
        CancellationToken cancellationToken
    )
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
        //TODO: implement fully
        // if (smsNotificationClient is null && emailNotificationClient is null)
        // {
        //     throw new InvalidOperationException("Unable to send Notification. Neither Sms nor Email notification service available.");
        // }

        // if (smsNotificationClient is not null)
        // {
        //     var notification = new SmsNotification()
        //     {
        //         Body = "",
        //         Recipients = [new SmsRecipient("", "", "")],
        //         SenderNumber = "",
        //         SendersReference = ""
        //     };
        //     await smsNotificationClient.Order(notification, cancellationToken);
        // }

        // if (emailNotificationClient is not null)
        // {
        //     var notification = new EmailNotification
        //     {
        //         Body = "",
        //         Recipients = [new EmailRecipient("")],
        //         Subject = "",
        //         SendersReference = ""
        //     };
        //     await emailNotificationClient.Order(notification, cancellationToken);
        // }
    }
}
