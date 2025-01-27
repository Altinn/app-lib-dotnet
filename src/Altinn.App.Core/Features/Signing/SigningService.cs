using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Exceptions;
using Altinn.App.Core.Features.Signing.Exceptions;
using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Profile;
using Altinn.App.Core.Internal.Registers;
using Altinn.App.Core.Internal.Sign;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Result;
using Altinn.App.Core.Models.UserAction;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using JsonException = Newtonsoft.Json.JsonException;
using Signee = Altinn.App.Core.Internal.Sign.Signee;

namespace Altinn.App.Core.Features.Signing;

internal sealed class SigningService(
    IAltinnPartyClient altinnPartyClient,
    ISigningDelegationService signingDelegationService,
    ISigningNotificationService signingNotificationService,
    IEnumerable<ISigneeProvider> signeeProviders,
    IAppMetadata appMetadata,
    IHttpContextAccessor httpContextAccessor,
    ISignClient signClient,
    ISigningCorrespondenceService signingCorrespondenceService,
    IProfileClient profileClient,
    IAltinnPartyClient altinnPartyClientService,
    IOptions<GeneralSettings> settings,
    IDataClient dataClient,
    ILogger<SigningService> logger,
    Telemetry? telemetry = null
) : ISigningService
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly ILogger<SigningService> _logger = logger;
    private readonly IAppMetadata _appMetadata = appMetadata;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ISignClient _signClient = signClient;
    private readonly UserHelper _userHelper = new(profileClient, altinnPartyClientService, settings);
    private readonly ISigningCorrespondenceService _signingCorrespondenceService = signingCorrespondenceService;
    private readonly IDataClient _dataClient = dataClient;
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
            var signeeContext = await GenerateSigneeContext(taskId, signeeParty, ct);
            signeeContexts.Add(signeeContext);
        }

        _logger.LogInformation("Assigning signees to task {TaskId}: {SigneeContexts}", taskId, signeeContexts.Count);

        return signeeContexts;
    }

    public async Task<List<SigneeContext>> InitialiseSignees(
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

        List<SignDocument> signDocuments = await DownloadSignDocuments(instanceDataAccessor, signatureConfiguration);

        await SynchronizeSigneeContextsWithSignDocuments(instanceDataAccessor, signeeContexts, signDocuments);

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
            await GetSignee(
                _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is not available.")
            ),
            dataElementSignatures
        );

        await _signClient.SignDataElements(signatureContext);

        // Update matching signeeContext with new signee information
        await UpdateSigneeContext(signatureConfiguration, signatureContext, userActionContext.DataMutator);

        try
        {
            var result = await _signingCorrespondenceService.SendCorrespondence(
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

    private async Task UpdateSigneeContext(
        AltinnSignatureConfiguration? signatureConfiguration,
        SignatureContext signatureContext,
        IInstanceDataMutator dataMutator
    )
    {
        if (signatureConfiguration?.SigneeStatesDataTypeId is not null)
        {
            List<SigneeContext> signeeContexts = await GetSigneeContexts(dataMutator, signatureConfiguration);

            foreach (var signeeContext in signeeContexts)
            {
                // Update the signee context with information about person signee if
                // the original signee is an organisation and missing person information.
                if (
                    signatureContext.Signee.OrganisationNumber is not null
                    && signatureContext.Signee.OrganisationNumber
                        == signeeContext.OnBehalfOfOrganisation?.OrganisationNumber
                    && signeeContext.SocialSecurityNumber is null
                )
                {
                    Party personParty = await altinnPartyClient.LookupParty(
                        new PartyLookup { Ssn = signatureContext.Signee.PersonNumber }
                    );

                    signeeContext.FullName = personParty.Person.Name;
                    signeeContext.SocialSecurityNumber = personParty.SSN;
                }
            }

            IEnumerable<DataElement> dataElements = dataMutator.GetDataElementsForType(
                signatureConfiguration.SigneeStatesDataTypeId
            );

            DataElement signeeStateDataElement =
                dataElements.SingleOrDefault()
                ?? throw new ApplicationException(
                    $"Failed to find the data element containing signee contexts using dataTypeId {signatureConfiguration.SigneeStatesDataTypeId}."
                );

            try
            {
                await _dataClient.UpdateBinaryData(
                    new InstanceIdentifier(dataMutator.Instance),
                    signeeStateDataElement.ContentType,
                    signeeStateDataElement.Filename,
                    new DataElementIdentifier(signeeStateDataElement.Id).Guid,
                    new MemoryAsStream(JsonSerializer.SerializeToUtf8Bytes(signeeContexts, _jsonSerializerOptions))
                );
            }
            catch (PlatformHttpException ex)
            {
                // TODO: Successful signing, but failed to update signee context data element. Should this throw or smth?
                _logger.LogError(ex, "Failed to update signee context data element.");
            }
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

        foreach (var dataType in dataTypesToSign)
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
        var signatureDataType = currentTask.ExtensionElements?.TaskExtension?.SignatureConfiguration?.SignatureDataType;
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

    private async Task<Signee> GetSignee(HttpContext context)
    {
        UserContext? userProfile =
            await _userHelper.GetUserContext(context)
            ?? throw new Exception("Could not get user profile while getting signee");

        return new Signee
        {
            UserId = userProfile.UserId.ToString(CultureInfo.InvariantCulture),
            PersonNumber = userProfile.SocialSecurityNumber,
            OrganisationNumber = userProfile.Party.OrgNumber,
        };
    }

    private async Task<SigneeContext> GenerateSigneeContext(
        string taskId,
        SigneeParty signeeParty,
        CancellationToken ct
    )
    {
        var orgNumber = signeeParty.OnBehalfOfOrganisation?.OrganisationNumber;
        Party party = await altinnPartyClient.LookupParty(
            new PartyLookup { Ssn = orgNumber is null ? signeeParty.SocialSecurityNumber : null, OrgNo = orgNumber }
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
            FullName = signeeParty.FullName,
            SocialSecurityNumber = signeeParty.SocialSecurityNumber,
            OnBehalfOfOrganisation = signeeParty.OnBehalfOfOrganisation is null
                ? null
                : new SigneeContextOrganisation
                {
                    Name = signeeParty.OnBehalfOfOrganisation.Name,
                    OrganisationNumber = signeeParty.OnBehalfOfOrganisation.OrganisationNumber,
                },
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
                x.OnBehalfOfOrganisation?.OrganisationNumber == signDocument.SigneeInfo.OrganisationNumber
                && x.SocialSecurityNumber == signDocument.SigneeInfo.PersonNumber
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

    /// <summary>
    /// Catch exceptions from a task and return them as a ServiceResult record with the result.
    /// </summary>
    private static async Task<ServiceResult<T, Exception>> CatchError<T>(Task<T> task)
    {
        try
        {
            var result = await task;
            return result;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
