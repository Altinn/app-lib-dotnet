using Altinn.App.Core.Internal.Language;

namespace Altinn.App.Core.Features.Signing.Constants;

internal sealed class SigningNotificationConst
{
    private const string DefaultSmsBody =
        "Du har mottatt en ny oppgave i Altinn. Åpne din Altinn-innboks for å fullføre oppgaven.";
    private const string DefaultEmailBody =
        "Du har mottatt en ny oppgave i Altinn. Åpne din Altinn-innboks for å fullføre oppgaven.";
    private const string DefaultSmsBodyEn =
        "You have received a new task in Altinn. Open your altinn.no inbox to complete the task.";
    private const string DefaultEmailBodyEn =
        "You have received a new task in Altinn. Open your altinn.no inbox to complete the task.";
    private const string DefaultSmsBodyNn =
        "Du har motteke ei ny oppgåve i Altinn. Opne din Altinn-innboks for å fullføre oppgåva.";
    private const string DefaultEmailBodyNn =
        "Du har motteke ei ny oppgåve i Altinn. Opne din Altinn-innboks for å fullføre oppgåva.";

    internal static string GetDefaultSmsBody(string language)
    {
        return language switch
        {
            LanguageConst.En => DefaultSmsBodyEn,
            LanguageConst.Nn => DefaultSmsBodyNn,
            _ => DefaultSmsBody,
        };
    }

    internal static string GetDefaultEmailBody(string language)
    {
        return language switch
        {
            LanguageConst.En => DefaultEmailBodyEn,
            LanguageConst.Nn => DefaultEmailBodyNn,
            _ => DefaultEmailBody,
        };
    }
}
