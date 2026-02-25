using System.Globalization;
using Altinn.App.Core.Internal.Language;

namespace Altinn.App.Core.Features.Notifications.Texts;

internal static class NotificationTexts
{
    internal static string GetDefaultSubject(string? language)
    {
        return language switch
        {
            LanguageConst.En => "New form created in Altinn",
            LanguageConst.Nn => "Nytt skjema opprettet i Altinn",
            _ => "Nytt skjema opprettet i Altinn",
        };
    }

    internal static string GetDefaultBody(
        string? language,
        string? appid,
        string? serviceOwnerName,
        string? instanceOwnerName,
        string? orgNumber,
        string? socialSecurityNumber,
        DateOnly? dueDate
    )
    {
        List<string> parts = [];
        (string? appName, _) =
            appid?.Split('/') is string[] groups && groups.Length >= 2 ? (groups[1], groups[0]) : (null, null);

        parts.Add(
            serviceOwnerName is not null
                ? language switch
                {
                    LanguageConst.En => $"{serviceOwnerName} has created a new form",
                    LanguageConst.Nn => $"{serviceOwnerName} har opprettet eit nytt skjema",
                    _ => $"{serviceOwnerName} har opprettet et nytt skjema",
                }
                : language switch
                {
                    LanguageConst.En => "A new form has been created",
                    LanguageConst.Nn => "Eit nytt skjema har blitt opprettet",
                    _ => "Det har blitt opprettet et nytt skjema",
                }
        );

        if (appName is not null)
        {
            parts.Add(
                language switch
                {
                    LanguageConst.En => $"({appName})",
                    LanguageConst.Nn => $"({appName})",
                    _ => $"({appName})",
                }
            );
        }

        if (instanceOwnerName is not null)
        {
            parts.Add(
                language switch
                {
                    LanguageConst.En => $"for {instanceOwnerName}",
                    LanguageConst.Nn => $"for {instanceOwnerName}",
                    _ => $"for {instanceOwnerName}",
                }
            );
        }

        if (orgNumber is not null)
        {
            parts.Add(
                language switch
                {
                    LanguageConst.En => instanceOwnerName is not null
                        ? $"with organization number {orgNumber}"
                        : $"with organization number {orgNumber}",
                    LanguageConst.Nn => instanceOwnerName is not null
                        ? $"med organisasjonsnummer {orgNumber}"
                        : $"avgiver med organisasjonsnummer {orgNumber}",
                    _ => instanceOwnerName is not null
                        ? $"med organisasjonsnummer {orgNumber}"
                        : $"avgiver med organisasjonsnummer {orgNumber}",
                }
            );
        }

        // Org number should never be set if social security number is set, but the model allows it - so we have a fail safe to avoid corrupted notifications
        if (socialSecurityNumber is not null && orgNumber is null)
        {
            parts.Add(
                language switch
                {
                    LanguageConst.En => instanceOwnerName is not null
                        ? $"person with social security number {socialSecurityNumber}"
                        : $"person with social security number {socialSecurityNumber}",
                    LanguageConst.Nn => instanceOwnerName is not null
                        ? $"med fødselsnummer {socialSecurityNumber}"
                        : $"avgiver med fødselsnummer {socialSecurityNumber}",
                    _ => instanceOwnerName is not null
                        ? $"med fødselsnummer {socialSecurityNumber}"
                        : $"avgiver med fødselsnummer {socialSecurityNumber}",
                }
            );
        }

        if (dueDate is not null)
        {
            var formattedDate = dueDate.Value.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
            parts.Add(
                language switch
                {
                    LanguageConst.En => $"with due date {formattedDate}",
                    LanguageConst.Nn => $"med frist {formattedDate}",
                    _ => $"med frist {formattedDate}",
                }
            );
        }

        return string.Join(" ", parts);
    }
}
