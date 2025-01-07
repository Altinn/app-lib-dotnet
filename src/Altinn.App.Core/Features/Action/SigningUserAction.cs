using System.Globalization;
using Altinn.App.Core.Features.Correspondence;
using Altinn.App.Core.Features.Correspondence.Builder;
using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Internal.AltinnCdn;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Profile;
using Altinn.App.Core.Internal.Sign;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Process;
using Altinn.App.Core.Models.UserAction;
using Altinn.Platform.Storage.Interface.Models;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="SigningUserAction"/> class
    /// </summary>
    /// <param name="processReader">The process reader</param>
    /// <param name="logger">The logger</param>
    /// <param name="profileClient">The profile client</param>
    /// <param name="signClient">The sign client</param>
    /// <param name="correspondenceClient">The correspondence client</param>
    /// <param name="appMetadata">The application metadata</param>
    public SigningUserAction(
        IProcessReader processReader,
        ILogger<SigningUserAction> logger,
        IProfileClient profileClient,
        ISignClient signClient,
        ICorrespondenceClient correspondenceClient,
        IAppMetadata appMetadata
    )
    {
        _logger = logger;
        _profileClient = profileClient;
        _signClient = signClient;
        _processReader = processReader;
        _correspondenceClient = correspondenceClient;
        _appMetadata = appMetadata;
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
            await SendCorrespondence(signatureContext.InstanceIdentifier, signatureContext.Signee, appMetadata);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Correspondence send failed: {Exception}", e.Message);
        }

        return UserActionResult.SuccessResult();
    }

    private async Task<SendCorrespondenceResponse?> SendCorrespondence(
        InstanceIdentifier instanceIdentifier,
        Signee signee,
        ApplicationMetadata appMetadata
    )
    {
        NationalIdentityNumber recipient = NationalIdentityNumber.Parse(signee.PersonNumber ?? string.Empty);

        using var altinnCdnClient = new AltinnCdnClient();
        AltinnCdnOrgs altinnCdnOrgs = await altinnCdnClient.GetOrgs();
        string? sender = altinnCdnOrgs.Orgs?.GetValueOrDefault(appMetadata.Org)?.Orgnr;

        if (string.IsNullOrEmpty(sender))
        {
            // TODO: Fix all `Exception` types in this class
            throw new Exception(
                $"Error looking up sender's organisation number from Altinn CDN, using key {appMetadata.Org}"
            );
        }

        var builder = CorrespondenceRequestBuilder
            .Create()
            .WithResourceId("apps-correspondence-integrasjon2") // TODO: Configurable resource
            .WithSender(sender) // TODO: Needs fallback for TTD
            .WithSendersReference(instanceIdentifier.ToString())
            .WithRecipient(recipient)
            .WithAllowSystemDeleteAfter(DateTime.Now.AddYears(1))
            .WithContent(
                CorrespondenceContentBuilder
                    .Create()
                    .WithLanguage(LanguageCode<Iso6391>.Parse("en"))
                    .WithTitle("Hello from .NET (with builder)")
                    .WithSummary("This is a summary of the message. This was sent to an organisation number.")
                    .WithBody(
                        "This is the full message in all its glory.\n\nHere's some markdown: **bold** *italic* `code`"
                    )
            )
            .WithAttachment(
                CorrespondenceAttachmentBuilder
                    .Create()
                    .WithFilename("attachment.txt")
                    .WithName("The attachment 📎")
                    .WithSendersReference("12345-attachmentref")
                    .WithDataType("application/pdf")
                    .WithData("This is the attachment content"u8.ToArray())
            );

        return await _correspondenceClient.Send(
            new SendCorrespondencePayload(builder.Build(), CorrespondenceAuthorisation.Maskinporten)
        );
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
