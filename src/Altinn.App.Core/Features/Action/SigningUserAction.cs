using System.Globalization;
using Altinn.App.Core.Constants;
using Altinn.App.Core.Exceptions;
using Altinn.App.Core.Features.Correspondence;
using Altinn.App.Core.Features.Correspondence.Builder;
using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Internal.AltinnCdn;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Language;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Profile;
using Altinn.App.Core.Internal.Sign;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Process;
using Altinn.App.Core.Models.UserAction;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Signee = Altinn.App.Core.Internal.Sign.Signee;

namespace Altinn.App.Core.Features.Action;

/// <summary>
/// Class handling tasks that should happen when action signing is performed.
/// </summary>
public class SigningUserAction : IUserAction
{
    private readonly IProcessReader _processReader;
    private readonly IAppMetadata _appMetadata;
    private readonly ILogger<SigningUserAction> _logger;
    private readonly IProfileClient _profileClient;
    private readonly ISignClient _signClient;
    private readonly ICorrespondenceClient _correspondenceClient;
    private readonly IDataClient _dataClient;
    private readonly IAppResources _appResources;
    private readonly IHostEnvironment _hostEnvironment;

    /// <summary>
    /// Initializes a new instance of the <see cref="SigningUserAction"/> class
    /// </summary>
    /// <param name="processReader">The process reader</param>
    /// <param name="logger">The logger</param>
    /// <param name="profileClient">The profile client</param>
    /// <param name="signClient">The sign client</param>
    /// <param name="correspondenceClient">The correspondence client</param>
    /// <param name="dataClient">The data client</param>
    /// <param name="appMetadata">The application metadata</param>
    /// <param name="appResources">The application resources</param>
    /// <param name="hostEnvironment">The hosting environment details</param>
    public SigningUserAction(
        IProcessReader processReader,
        ILogger<SigningUserAction> logger,
        IProfileClient profileClient,
        ISignClient signClient,
        ICorrespondenceClient correspondenceClient,
        IDataClient dataClient,
        IAppMetadata appMetadata,
        IAppResources appResources,
        IHostEnvironment hostEnvironment
    )
    {
        _logger = logger;
        _profileClient = profileClient;
        _signClient = signClient;
        _processReader = processReader;
        _correspondenceClient = correspondenceClient;
        _dataClient = dataClient;
        _appMetadata = appMetadata;
        _appResources = appResources;
        _hostEnvironment = hostEnvironment;
    }

    /// <inheritdoc />
    public string Id => "sign";

    /// <inheritdoc />
    /// <exception cref="Helpers.PlatformHttpException"></exception>
    /// <exception cref="ApplicationConfigException"></exception>
    public async Task<UserActionResult> HandleAction(UserActionContext context)
    {
        if (context.UserId is null)
        {
            return UserActionResult.FailureResult(
                error: new ActionError { Code = "NoUserId", Message = "User id is missing in token" },
                errorType: ProcessErrorType.Unauthorized
            );
        }

        ProcessTask? currentTask =
            _processReader.GetFlowElement(context.Instance.Process.CurrentTask.ElementId) as ProcessTask;

        if (currentTask is null)
        {
            return UserActionResult.FailureResult(
                new ActionError { Code = "NoProcessTask", Message = "Current task is not a process task." }
            );
        }

        _logger.LogInformation(
            "Signing action handler invoked for instance {Id}. In task: {CurrentTaskId}",
            context.Instance.Id,
            currentTask.Id
        );

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
            context.Instance.Data,
            dataTypesToSign
        );
        string signatureDataType =
            GetDataTypeForSignature(currentTask, context.Instance.Data, dataTypesToSign)
            ?? throw new ApplicationConfigException(
                "Missing configuration for signing. Check that the task has a signature configuration and that the data types to sign are defined."
            );

        SignatureContext signatureContext = new(
            new InstanceIdentifier(context.Instance),
            currentTask.Id,
            signatureDataType,
            await GetSignee(context.UserId.Value),
            dataElementSignatures
        );

        await _signClient.SignDataElements(signatureContext);

        // TODO: Metrics

        try
        {
            var result = await SendCorrespondence(
                signatureContext.InstanceIdentifier,
                signatureContext.Signee,
                dataElementSignatures,
                appMetadata,
                context,
                signatureConfiguration
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

        return UserActionResult.SuccessResult();
    }

    private async Task<SendCorrespondenceResponse?> SendCorrespondence(
        InstanceIdentifier instanceIdentifier,
        Signee signee,
        IEnumerable<DataElementSignature> dataElementSignatures,
        ApplicationMetadata appMetadata,
        UserActionContext context,
        AltinnSignatureConfiguration? signatureConfiguration
    )
    {
        var headers = await GetCorrespondenceHeaders(
            signee,
            appMetadata,
            signatureConfiguration,
            _hostEnvironment,
            context.AltinnCdnClient
        );
        CorrespondenceContent content = await GetCorrespondenceContent(context, appMetadata, headers.senderDetails);
        IEnumerable<CorrespondenceAttachment> attachments = await GetCorrespondenceAttachments(
            instanceIdentifier,
            dataElementSignatures,
            appMetadata,
            context,
            _dataClient
        );

        return await _correspondenceClient.Send(
            new SendCorrespondencePayload(
                CorrespondenceRequestBuilder
                    .Create()
                    .WithResourceId(headers.resource)
                    .WithSender(headers.senderOrgNumber)
                    .WithSendersReference(instanceIdentifier.ToString())
                    .WithRecipient(headers.recipient)
                    .WithAllowSystemDeleteAfter(DateTime.Now.AddYears(1))
                    .WithContent(content)
                    .WithAttachments(attachments)
                    .Build(),
                CorrespondenceAuthorisation.Maskinporten
            )
        );
    }

    internal static async Task<(
        string resource,
        string senderOrgNumber,
        AltinnCdnOrgDetails senderDetails,
        string recipient
    )> GetCorrespondenceHeaders(
        Signee signee,
        ApplicationMetadata appMetadata,
        AltinnSignatureConfiguration? signatureConfiguration,
        IHostEnvironment hostEnvironment,
        IAltinnCdnClient? altinnCdnClient = null
    )
    {
        HostingEnvironment env = AltinnEnvironments.GetHostingEnvironment(hostEnvironment);
        var resource = AltinnTaskExtension
            .GetConfigForEnvironment(env, signatureConfiguration?.CorrespondenceResources)
            ?.Value;
        if (string.IsNullOrEmpty(resource))
        {
            throw new ConfigurationException(
                $"No correspondence resource configured for environment {env}, skipping correspondence send"
            );
        }

        string? recipient = signee.PersonNumber;
        if (string.IsNullOrEmpty(recipient))
        {
            throw new InvalidOperationException(
                "Signee's national identity number is missing, unable to send correspondence"
            );
        }

        bool disposeClient = altinnCdnClient is null;
        altinnCdnClient ??= new AltinnCdnClient();
        try
        {
            AltinnCdnOrgs altinnCdnOrgs = await altinnCdnClient.GetOrgs();
            AltinnCdnOrgDetails? senderDetails = altinnCdnOrgs.Orgs?.GetValueOrDefault(appMetadata.Org);
            string? senderOrgNumber = senderDetails?.Orgnr;

            if (senderDetails is null || string.IsNullOrEmpty(senderOrgNumber))
            {
                throw new InvalidOperationException(
                    $"Error looking up sender's organisation number from Altinn CDN, using key `{appMetadata.Org}`"
                );
            }

            return (resource, senderOrgNumber, senderDetails, recipient);
        }
        finally
        {
            if (disposeClient)
            {
                altinnCdnClient.Dispose();
            }
        }
    }

    internal async Task<CorrespondenceContent> GetCorrespondenceContent(
        UserActionContext context,
        ApplicationMetadata appMetadata,
        AltinnCdnOrgDetails senderDetails
    )
    {
        TextResource? textResource = null;
        string? title = null;
        string? summary = null;
        string? body = null;
        string? appName = null;

        string appOwner = senderDetails.Name?.Nb ?? senderDetails.Name?.Nn ?? senderDetails.Name?.En ?? appMetadata.Org;
        string defaultLanguage = LanguageConst.Nb;
        string defaultAppName =
            appMetadata.Title?.GetValueOrDefault(defaultLanguage)
            ?? appMetadata.Title?.FirstOrDefault().Value
            ?? appMetadata.Id;

        try
        {
            AppIdentifier appIdentifier = new(context.Instance);

            if (context.Language is not null && context.Language != defaultLanguage)
            {
                textResource = await _appResources.GetTexts(appIdentifier.Org, appIdentifier.App, context.Language);
            }

            textResource ??=
                await _appResources.GetTexts(appIdentifier.Org, appIdentifier.App, defaultLanguage)
                ?? throw new InvalidOperationException(
                    $"No text resource found for specified language ({context.Language}) nor the default language ({defaultLanguage})"
                );

            title = textResource
                .Resources.FirstOrDefault(x => x.Id.Equals("signing.receipt_title", StringComparison.Ordinal))
                ?.Value;
            summary = textResource
                .Resources.FirstOrDefault(x => x.Id.Equals("signing.receipt_summary", StringComparison.Ordinal))
                ?.Value;
            body = textResource
                .Resources.FirstOrDefault(x => x.Id.Equals("signing.receipt_body", StringComparison.Ordinal))
                ?.Value;

            appName =
                textResource.Resources.FirstOrDefault(x => x.Id.Equals("appName", StringComparison.Ordinal))?.Value
                ?? textResource
                    .Resources.FirstOrDefault(x => x.Id.Equals("ServiceName", StringComparison.Ordinal))
                    ?.Value;
        }
        catch (Exception e)
        {
            _logger.LogInformation(
                e,
                "Unable to fetch custom message correspondence message content, falling back to default values: {Exception}",
                e.Message
            );
        }

        if (string.IsNullOrWhiteSpace(appName))
        {
            appName = defaultAppName;
        }

        var defaults = new
        {
            Title = $"{appName}: Signeringen er bekreftet",
            Summary = $"Du har signert for {appName}.",
            Body = $"Dokumentene du har signert er vedlagt. Disse kan lastes ned om ønskelig.<br /><br />Hvis du lurer på noe, kan du kontakte {appOwner}.",
        };

        return new CorrespondenceContent
        {
            Language = LanguageCode<Iso6391>.Parse(textResource?.Language ?? defaultLanguage),
            Title = title ?? defaults.Title,
            Summary = summary ?? defaults.Summary,
            Body = body ?? defaults.Body,
        };
    }

    internal static async Task<IEnumerable<CorrespondenceAttachment>> GetCorrespondenceAttachments(
        InstanceIdentifier instanceIdentifier,
        IEnumerable<DataElementSignature> dataElementSignatures,
        ApplicationMetadata appMetadata,
        UserActionContext context,
        IDataClient dataClient
    )
    {
        List<CorrespondenceAttachment> attachments = [];
        IEnumerable<DataElement> signedElements = context.Instance.Data.Where(IsSignedDataElement);

        foreach (DataElement element in signedElements)
        {
            string filename = GetDataElementFilename(element, appMetadata);

            attachments.Add(
                CorrespondenceAttachmentBuilder
                    .Create()
                    .WithFilename(filename)
                    .WithName(filename)
                    .WithSendersReference(element.Id)
                    .WithDataType(element.ContentType ?? "application/octet-stream")
                    .WithData(
                        await dataClient.GetDataBytes(
                            appMetadata.AppIdentifier.Org,
                            appMetadata.AppIdentifier.App,
                            instanceIdentifier.InstanceOwnerPartyId,
                            instanceIdentifier.InstanceGuid,
                            Guid.Parse(element.Id)
                        )
                    )
                    .Build()
            );
        }

        return attachments;

        bool IsSignedDataElement(DataElement dataElement) =>
            dataElementSignatures.Any(x => x.DataElementId == dataElement.Id);
    }

    /// <summary>
    /// Note: This method contains only an extremely small list of known mime types.
    /// The aim here is not to be exhaustive, just to cover some common cases.
    /// </summary>
    internal static string GetDataElementFilename(DataElement dataElement, ApplicationMetadata appMetadata)
    {
        if (!string.IsNullOrWhiteSpace(dataElement.Filename))
        {
            return dataElement.Filename;
        }

        DataType? dataType = appMetadata.DataTypes.Find(x => x.Id == dataElement.DataType);

        var mimeType = dataElement.ContentType?.ToLower(CultureInfo.InvariantCulture) ?? string.Empty;
        var formDataExtensions = new[] { ".xml", ".json" };
        var mapping = new Dictionary<string, string>
        {
            ["application/xml"] = ".xml",
            ["text/xml"] = ".xml",
            ["application/pdf"] = ".pdf",
            ["application/json"] = ".json",
        };

        string? extension = mapping.GetValueOrDefault(mimeType);
        string filename = dataElement.DataType;
        if (dataType?.AppLogic is not null && formDataExtensions.Contains(extension))
        {
            filename = $"skjemadata_{filename}";
        }

        return $"{filename}{extension}";
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

    internal static List<DataElementSignature> GetDataElementSignatures(
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

    private async Task<Signee> GetSignee(int userId)
    {
        var userProfile =
            await _profileClient.GetUserProfile(userId)
            ?? throw new Exception("Could not get user profile while getting signee");

        return new Signee
        {
            UserId = userProfile.UserId.ToString(CultureInfo.InvariantCulture),
            PersonNumber = userProfile.Party.SSN,
            OrganisationNumber = userProfile.Party.OrgNumber,
        };
    }
}
