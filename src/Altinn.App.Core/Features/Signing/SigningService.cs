using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Altinn.App.Core.Features.Signing.Exceptions;
using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Mocks;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Helpers.Serialization;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Registers;
using Altinn.App.Core.Models;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Features.Signing;

internal sealed class SigningService(
    IPersonClient personClient,
    IOrganizationClient organisationClient,
    IAltinnPartyClient altinnPartyClient,
    ISigningDelegationService signingDelegationService,
    // ISigningNotificationService signingNotificationService,
    IEnumerable<ISigneeProvider> signeeProviders,
    IDataClient dataClient,
    IInstanceClient instanceClient,
    ModelSerializationService modelSerialization,
    IAppMetadata appMetadata,
    ILogger<SigningService> logger,
    Telemetry? telemetry = null
) : ISigningService
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IDataClient _dataClient = dataClient;
    private readonly IInstanceClient _instanceClient = instanceClient;
    private readonly ModelSerializationService _modelSerialization = modelSerialization;
    private readonly IAppMetadata _appMetadata = appMetadata;
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

        var instance = instanceMutator.Instance;
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
        IInstanceDataMutator instanceMutator,
        List<SigneeContext> signeeContexts,
        AltinnSignatureConfiguration signatureConfiguration,
        CancellationToken ct
    )
    {
        using Activity? activity = telemetry?.StartAssignSigneesActivity();
        string taskId = instanceMutator.Instance.Process.CurrentTask.ElementId;

        (signeeContexts, var delegateSuccess) = await signingDelegationService.DelegateSigneeRights(
            taskId,
            instanceMutator,
            signeeContexts,
            ct,
            telemetry
        );

        if (delegateSuccess)
        {
            // await signingNotificationService.NotifySignatureTask(signeeContexts, ct);
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
        Instance instance,
        AltinnSignatureConfiguration signatureConfiguration
    )
    {
        using Activity? activity = telemetry?.StartReadSigneesActivity();
        // TODO: Get signees from state
        ApplicationMetadata appMetadata = await _appMetadata.GetApplicationMetadata();

        var cachedDataMutator = new InstanceDataUnitOfWork(
            instance,
            _dataClient,
            _instanceClient,
            appMetadata,
            _modelSerialization
        );

        // ! TODO: Remove nullable
        IEnumerable<DataElement> dataElements = cachedDataMutator.GetDataElementsForType(
            signatureConfiguration.SigneeStatesDataTypeId!
        );

        DataElement signeeStateDataElement = dataElements.Single();
        ReadOnlyMemory<byte> data = await cachedDataMutator.GetBinaryData(signeeStateDataElement);
        string asString = Encoding.UTF8.GetString(data.ToArray());

        var result = JsonSerializer.Deserialize<SigneeContext[]>(asString, _jsonSerializerOptions) ?? [];

        return [.. result];

        // TODO: Get signees from policy??
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
            Person? person =
                await personClient.GetPerson(
                    personSignee.SocialSecurityNumber,
                    personSignee.LastName.Split(" ").Last(),
                    ct
                )
                ?? throw new SignaturePartyNotValidException(
                    $"The given SSN and last name did not match any person in the registry."
                );
            Party? party = await altinnPartyClient.LookupParty(
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
                    PartyId = party.PartyId,
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

            organisationSigneeContexts.Add(
                new SigneeContext
                {
                    TaskId = taskId,
                    PartyId = party.PartyId,
                    OrganisationSignee = organisationSignee,
                    SigneeState = new SigneeState(),
                }
            );
        }
        return organisationSigneeContexts;
    }
}
