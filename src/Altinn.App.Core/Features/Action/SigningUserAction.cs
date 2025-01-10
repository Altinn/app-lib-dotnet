using System.Globalization;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features.Correspondence;
using Altinn.App.Core.Features.Correspondence.Builder;
using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Internal.AltinnCdn;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Language;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Profile;
using Altinn.App.Core.Internal.Sign;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Process;
using Altinn.App.Core.Models.UserAction;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private readonly AppSettings _settings;

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
    /// <param name="settings">The application settings</param>
    public SigningUserAction(
        IProcessReader processReader,
        ILogger<SigningUserAction> logger,
        IProfileClient profileClient,
        ISignClient signClient,
        ICorrespondenceClient correspondenceClient,
        IDataClient dataClient,
        IAppMetadata appMetadata,
        IAppResources appResources,
        IOptions<AppSettings> settings
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
        _settings = settings.Value;
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
        List<string> dataTypeIds =
            currentTask.ExtensionElements?.TaskExtension?.SignatureConfiguration?.DataTypesToSign ?? [];
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

        try
        {
            var result = await SendCorrespondence(
                signatureContext.InstanceIdentifier,
                signatureContext.Signee,
                dataElementSignatures,
                appMetadata,
                context
            );

            if (result is not null)
            {
                _logger.LogInformation(
                    "Correspondence successfully sent to {Recipients}",
                    string.Join(", ", result.Correspondences.Select(x => x.Recipient))
                );
            }
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
        UserActionContext context
    )
    {
        string? resource = _settings.SigningCorrespondenceResource;
        if (string.IsNullOrEmpty(resource))
        {
            _logger.LogInformation("No correspondence resource configured, skipping correspondence send");
            return null;
        }

        string? recipient = signee.PersonNumber;
        if (string.IsNullOrEmpty(recipient))
        {
            throw new InvalidOperationException(
                $"Signee's national identity number is missing, unable to send correspondence"
            );
        }

        using var altinnCdnClient = new AltinnCdnClient();
        AltinnCdnOrgs altinnCdnOrgs = await altinnCdnClient.GetOrgs();
        string? sender = altinnCdnOrgs.Orgs?.GetValueOrDefault(appMetadata.Org)?.Orgnr;

        if (string.IsNullOrEmpty(sender))
        {
            throw new InvalidOperationException(
                $"Error looking up sender's organisation number from Altinn CDN, using key `{appMetadata.Org}`"
            );
        }

        CorrespondenceContent content = await GetCorrespondenceContent(context);
        var builder = CorrespondenceRequestBuilder
            .Create()
            .WithResourceId(resource)
            .WithSender(sender)
            .WithSendersReference(instanceIdentifier.ToString())
            .WithRecipient(recipient)
            .WithAllowSystemDeleteAfter(DateTime.Now.AddYears(1))
            .WithContent(content);

        IEnumerable<DataElement> attachments = context.Instance.Data.Where(x =>
            IsSignedDataElement(x) || IsGeneratedPdf(x)
        );

        foreach (DataElement attachment in attachments)
        {
            string filename = GetDataElementFilename(attachment);

            builder.WithAttachment(
                CorrespondenceAttachmentBuilder
                    .Create()
                    .WithFilename(filename)
                    .WithName(filename)
                    .WithSendersReference(attachment.Id)
                    .WithDataType(attachment.ContentType)
                    .WithData(
                        await _dataClient.GetDataBytes(
                            appMetadata.AppIdentifier.Org,
                            appMetadata.AppIdentifier.App,
                            instanceIdentifier.InstanceOwnerPartyId,
                            instanceIdentifier.InstanceGuid,
                            Guid.Parse(attachment.Id)
                        )
                    )
            );
        }

        return await _correspondenceClient.Send(
            new SendCorrespondencePayload(builder.Build(), CorrespondenceAuthorisation.Maskinporten)
        );

        bool IsSignedDataElement(DataElement dataElement) =>
            dataElementSignatures.Any(x => x.DataElementId == dataElement.Id);

        bool IsGeneratedPdf(DataElement dataElement) =>
            dataElement is { ContentType: "application/pdf", DataType: "ref-data-as-pdf" };
    }

    private async Task<CorrespondenceContent> GetCorrespondenceContent(UserActionContext context)
    {
        string? title = null;
        string? summary = null;
        string? body = null;
        var language = LanguageCode<Iso6391>.Parse(LanguageConst.Nb);

        // TODO: Write better defaults
        var defaults = new
        {
            Title = "Meldingstittel",
            Summary = "Meldingsoppsummering",
            Body = "Full meldingstekst",
        };

        try
        {
            if (context.Language is not null && context.Language != language)
            {
                language = LanguageCode<Iso6391>.Parse(context.Language);
            }

            var appIdentifier = new AppIdentifier(context.Instance);
            TextResource textResource =
                await _appResources.GetTexts(appIdentifier.Org, appIdentifier.App, language)
                ?? throw new InvalidOperationException($"No text resource found for language {language}");

            title = textResource
                .Resources.FirstOrDefault(textResourceElement =>
                    textResourceElement.Id.Equals("signing-receipt-title", StringComparison.Ordinal)
                )
                ?.Value;
            summary = textResource
                .Resources.FirstOrDefault(textResourceElement =>
                    textResourceElement.Id.Equals("signing-receipt-summary", StringComparison.Ordinal)
                )
                ?.Value;
            body = textResource
                .Resources.FirstOrDefault(textResourceElement =>
                    textResourceElement.Id.Equals("signing-receipt-body", StringComparison.Ordinal)
                )
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

        return new CorrespondenceContent
        {
            Language = language,
            Title = title ?? defaults.Title,
            Summary = summary ?? defaults.Summary,
            Body = body ?? defaults.Body,
        };
    }

    // TODO: Get some critical eyes on this
    /// <summary>
    /// Note: This method contains only an extremely small list of known mime types.
    /// The aim here is not to be exhaustive, just to cover some common cases.
    /// </summary>
    public static string GetDataElementFilename(DataElement dataElement)
    {
        if (!string.IsNullOrWhiteSpace(dataElement.Filename))
        {
            return dataElement.Filename;
        }

        string mimeType = dataElement.ContentType.ToLower(CultureInfo.InvariantCulture);
        var mapping = new Dictionary<string, string>
        {
            ["application/xml"] = ".xml",
            ["text/xml"] = ".xml",
            ["application/pdf"] = ".pdf",
            ["application/json"] = ".json",
        };

        string? extension = mapping.GetValueOrDefault(mimeType);
        string filename = dataElement.DataType;
        if (filename == "model" && extension == ".xml")
        {
            filename = "skjemadata";
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
