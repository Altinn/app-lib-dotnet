using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Texts;

namespace Altinn.App.Core.Features.Notifications;

internal sealed class NotificationTextHelper
{
    private readonly ITranslationService _translationService;

    public NotificationTextHelper(ITranslationService translationService)
    {
        _translationService = translationService;
    }

    public async Task<(string Subject, string Body)> GetEmailText(
        string language,
        EmailConfig emailConfig
    )
    {
        string subject = await GetTextResourceOrDefault(
            language,
            BackendTextResource.EmailDefaultSubject,
            emailConfig.SubjectTextResource
        );
        string body = await GetTextResourceOrDefault(
            language,
            BackendTextResource.EmailDefaultBody,
            emailConfig.BodyTextResource
        );
        return (subject, body);
    }

    public async Task<string> GetSmsBody(
        string language,
        SmsConfig smsConfig
    )
    {
        return await GetTextResourceOrDefault(
            language,
            BackendTextResource.SmsDefaultBody,
            smsConfig.BodyTextResource
        );
    }

    private async Task<string> GetTextResourceOrDefault(
        string language,
        string defaultTextResourceId,
        string? textResourceId = null
    )
    {
        string? translatedText =
            await _translationService.TranslateTextKey(language, textResourceId ?? defaultTextResourceId)
            ?? throw new InvalidOperationException(
                $"Default text resource '{defaultTextResourceId}' could not be found for language '{language}'"
            );
        return translatedText;
    }
}
