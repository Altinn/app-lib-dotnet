using System.Security.Claims;
using System.Text;

using Altinn.App.Core.Configuration;
using Altinn.App.Core.Extensions;
using Altinn.App.Core.Helpers.Extensions;
using Altinn.App.Core.Interface;
using Altinn.App.Core.Models;
using Altinn.Platform.Profile.Models;
using Altinn.Platform.Storage.Interface.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Internal.Pdf;

/// <summary>
/// Service for handling the creation and storage of receipt Pdf.
/// </summary>
public class PdfService : IPdfService
{
    private readonly IAppResources _resourceService;
    private readonly IData _dataClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IProfile _profileClient;

    private readonly IPdfGeneratorClient _pdfGeneratorClient;
    private readonly PdfGeneratorSettings _pdfGeneratorSettings;
    private readonly GeneralSettings _generalSettings;

    private const string PdfElementType = "ref-data-as-pdf";
    private const string PdfContentType = "application/pdf";

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfService"/> class.
    /// </summary>
    /// <param name="appResources">The service giving access to local resources.</param>
    /// <param name="dataClient">The data client.</param>
    /// <param name="httpContextAccessor">The httpContextAccessor</param>
    /// <param name="profileClient">The profile client</param>
    /// <param name="pdfGeneratorClient">PDF generator client for the experimental PDF generator service</param>
    /// <param name="pdfGeneratorSettings">PDF generator related settings.</param>
    /// <param name="generalSettings">The app general settings.</param>
    public PdfService(
        IAppResources appResources,
        IData dataClient,
        IHttpContextAccessor httpContextAccessor,
        IProfile profileClient,
        IPdfGeneratorClient pdfGeneratorClient,
        IOptions<PdfGeneratorSettings> pdfGeneratorSettings,
        IOptions<GeneralSettings> generalSettings)
    {
        _resourceService = appResources;
        _dataClient = dataClient;
        _httpContextAccessor = httpContextAccessor;
        _profileClient = profileClient;
        _pdfGeneratorClient = pdfGeneratorClient;
        _pdfGeneratorSettings = pdfGeneratorSettings.Value;
        _generalSettings = generalSettings.Value;
    }


    /// <inheritdoc/>
    public async Task GenerateAndStorePdf(Instance instance, CancellationToken ct)
    {
        StringBuilder address = new StringBuilder(_pdfGeneratorSettings.AppPdfPageUriTemplate.ToLower());

        address.Replace("{org}", instance.Org);
        address.Replace("{hostname}", _generalSettings.HostName);
        address.Replace("{appid}", instance.AppId);
        address.Replace("{instanceid}", instance.Id);

        var pdfContent = await _pdfGeneratorClient.GeneratePdf(new Uri(address.ToString()), ct);

        var appIdentifier = new AppIdentifier(instance.AppId);
        var language = await GetLanguage();
        TextResource? textResource = await GetTextResource(appIdentifier.App, appIdentifier.Org, language);
        string fileName = GetFileName(instance, textResource);

        await _dataClient.InsertBinaryData(
            instance.Id,
            PdfElementType,
            PdfContentType,
            fileName,
            pdfContent);
    }

    private async Task<string> GetLanguage()
    {
        string language = "nb";
        ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;

        int? userId = user.GetUserIdAsInt();

        if (userId != null)
        {
            UserProfile userProfile = await _profileClient.GetUserProfile((int)userId);

            if (!string.IsNullOrEmpty(userProfile.ProfileSettingPreference?.Language))
            {
                language = userProfile.ProfileSettingPreference.Language;
            }
        }

        return language;
    }

    private async Task<TextResource> GetTextResource(string app, string org, string language)
    {
        TextResource? textResource = await _resourceService.GetTexts(org, app, language);

        if (textResource == null && language != "nb")
        {
            // fallback to norwegian if texts does not exist
            textResource = await _resourceService.GetTexts(org, app, "nb");
        }

        return textResource;
    }

    private static string GetFileName(Instance instance, TextResource textResource)
    {
        string? fileName = null;
        string app = instance.AppId.Split("/")[1];

        fileName = $"{app}.pdf";

        if (textResource == null)
        {
            return GetValidFileName(fileName);
        }

        TextResourceElement? titleText =
            textResource.Resources.Find(textResourceElement => textResourceElement.Id.Equals("appName")) ??
            textResource.Resources.Find(textResourceElement => textResourceElement.Id.Equals("ServiceName"));

        if (titleText != null && !string.IsNullOrEmpty(titleText.Value))
        {
            fileName = titleText.Value + ".pdf";
        }
        
        return GetValidFileName(fileName);
    }

    private static string GetValidFileName(string fileName)
    {
        fileName = Uri.EscapeDataString(fileName.AsFileName(false));
        return fileName;
    }
}
