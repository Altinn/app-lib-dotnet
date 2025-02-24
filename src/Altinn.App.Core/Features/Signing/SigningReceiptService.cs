using System.Globalization;
using Altinn.App.Core.Constants;
using Altinn.App.Core.Exceptions;
using Altinn.App.Core.Features.Correspondence;
using Altinn.App.Core.Features.Correspondence.Builder;
using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Internal.AltinnCdn;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Language;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Sign;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.UserAction;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Signee = Altinn.App.Core.Internal.Sign.Signee;

namespace Altinn.App.Core.Features.Signing;

internal sealed class SigningReceiptService(
    ICorrespondenceClient correspondenceClient,
    IDataClient dataClient,
    IHostEnvironment hostEnvironment,
    IAppResources appResources,
    IAppMetadata appMetadata,
    ILogger<SigningReceiptService> logger
) : ISigningReceiptService
{
    private readonly ICorrespondenceClient _correspondenceClient = correspondenceClient;
    private readonly IDataClient _dataClient = dataClient;
    private readonly IHostEnvironment _hostEnvironment = hostEnvironment;
    private readonly IAppResources _appResources = appResources;
    private readonly IAppMetadata _appMetadata = appMetadata;
    private readonly ILogger<SigningReceiptService> _logger = logger;

    public async Task<SendCorrespondenceResponse?> SendSignatureReceipt(
        InstanceIdentifier instanceIdentifier,
        Signee signee,
        IEnumerable<DataElementSignature> dataElementSignatures,
        UserActionContext context,
        List<AltinnEnvironmentConfig>? correspondenceResources
    )
    {
        ApplicationMetadata applicationMetadata = await _appMetadata.GetApplicationMetadata();
        var (resource, senderOrgNumber, senderDetails, recipient) = await GetCorrespondenceHeaders(
            signee.PersonNumber,
            applicationMetadata,
            correspondenceResources,
            context.AltinnCdnClient
        );

        CorrespondenceContent content = await GetContent(context, applicationMetadata, senderDetails);
        IEnumerable<CorrespondenceAttachment> attachments = await GetCorrespondenceAttachments(
            instanceIdentifier,
            dataElementSignatures,
            applicationMetadata,
            context,
            _dataClient
        );

        return await _correspondenceClient.Send(
            new SendCorrespondencePayload(
                CorrespondenceRequestBuilder
                    .Create()
                    .WithResourceId(resource)
                    .WithSender(senderOrgNumber)
                    .WithSendersReference(instanceIdentifier.ToString())
                    .WithRecipient(recipient)
                    .WithAllowSystemDeleteAfter(DateTime.Now.AddYears(1))
                    .WithContent(content)
                    .WithAttachments(attachments)
                    .Build(),
                CorrespondenceAuthorisation.Maskinporten
            )
        );
    }

    internal async Task<(
        string resource,
        string senderOrgNumber,
        AltinnCdnOrgDetails senderDetails,
        string recipient
    )> GetCorrespondenceHeaders(
        string? recipientNin,
        ApplicationMetadata appMetadata,
        List<AltinnEnvironmentConfig>? correspondenceResources,
        IAltinnCdnClient? altinnCdnClient = null
    )
    {
        HostingEnvironment env = AltinnEnvironments.GetHostingEnvironment(_hostEnvironment);
        var resource = AltinnTaskExtension.GetConfigForEnvironment(env, correspondenceResources)?.Value;
        if (string.IsNullOrEmpty(resource))
        {
            throw new ConfigurationException(
                $"No correspondence resource configured for environment {env}, skipping correspondence send"
            );
        }

        string? recipient = recipientNin;
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

    internal async Task<CorrespondenceContent> GetContent(
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

            textResource ??=
                await _appResources.GetTexts(appIdentifier.Org, appIdentifier.App, context.Language ?? defaultLanguage)
                ?? throw new InvalidOperationException(
                    $"No text resource found for specified language ({context.Language}) nor the default language ({defaultLanguage})"
                );

            title = textResource.GetText("signing.receipt_title"); // TODO: Document these text keys
            summary = textResource.GetText("signing.receipt_summary"); // TODO: Document these text keys
            body = textResource.GetText("signing.receipt_body"); // TODO: Document these text keys
            appName = textResource.GetFirstMatchingText("appName", "ServiceName");
        }
        catch (Exception e)
        {
            _logger.LogWarning(
                e,
                "Unable to fetch custom message correspondence message content, falling back to default values: {Exception}",
                e.Message
            );
        }

        if (string.IsNullOrWhiteSpace(appName))
        {
            appName = defaultAppName;
        }

        var defaults = new // TODO: Add language support, and use profile
        {
            Title = $"{appName}: Signeringen er bekreftet",
            Summary = $"Du har signert for {appName}.",
            Body = $"Dokumentene du har signert er vedlagt. Disse kan lastes ned om ønskelig. <br /><br />Hvis du lurer på noe, kan du kontakte {appOwner}.",
        };

        CorrespondenceContent content = new()
        {
            Language = LanguageCode<Iso6391>.Parse(textResource?.Language ?? defaultLanguage),
            Title = title ?? defaults.Title,
            Summary = summary ?? defaults.Summary,
            Body = body ?? defaults.Body,
        };
        return content;
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
}
