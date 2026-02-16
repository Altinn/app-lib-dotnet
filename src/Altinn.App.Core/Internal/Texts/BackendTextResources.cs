using Altinn.App.Core.Internal.Language;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Texts;

internal record BackendTextResource
{
    internal const string ValidationErrorsRequired = "backend.validation_errors.required";
    internal const string PdfDefaultFileName = "backend.pdf_default_file_name";
    internal const string PdfPreviewText = "backend.pdf_preview_text";
    internal const string SmsDefaultBody = "backend.sms_default_body";
    internal const string EmailDefaultSubject = "backend.email_default_subject";
    internal const string EmailDefaultBody = "backend.email_default_body";
}

internal static class BackendTextResources
{

    internal static TextResourceElement? GetBackendFallbackResource(
        string resource,
        string language
    )
    {
        return resource switch
        {
            BackendTextResource.ValidationErrorsRequired => new TextResourceElement()
            {
                Id = resource,
                Value = language switch
                {
                    LanguageConst.Nb => "Feltet er påkrevd",
                    LanguageConst.Nn => "Feltet er påkravd",
                    _ => "Field is required",
                },
            },
            BackendTextResource.PdfDefaultFileName => new TextResourceElement()
            {
                Id = resource,
                Value = "{0}.pdf",
                Variables =
                [
                    new TextResourceVariable()
                    {
                        Key = "appName",
                        DataSource = "text",
                        DefaultValue = "Altinn PDF",
                    },
                ],
            },
            BackendTextResource.PdfPreviewText => new TextResourceElement()
            {
                Id = resource,
                Value = language switch
                {
                    LanguageConst.En => "The document is a preview",
                    LanguageConst.Nn => "Dokumentet er ein førehandsvisning",
                    _ => "Dokumentet er en forhåndsvisning",
                },
            },
            BackendTextResource.SmsDefaultBody =>
                new TextResourceElement()
                {
                    Id = resource,
                    Value = language switch
                    {
                        LanguageConst.En => "You have received a message in your Altinn inbox. Log in to view the message.",
                        LanguageConst.Nn => "Du har motteke ei melding i innboksen din i Altinn. Logg inn for å sjå meldinga.",
                        _ => "Du har mottatt en melding i innboksen din i Altinn. Logg inn for å se meldingen.",
                    },
                },
            BackendTextResource.EmailDefaultSubject =>
                new TextResourceElement()
                {
                    Id = resource,
                    Value = language switch
                    {
                        LanguageConst.En => "You have received a message in your Altinn inbox",
                        LanguageConst.Nn => "Du har motteke ei melding i innboksen din i Altinn",
                        _ => "Du har mottatt en melding i innboksen din i Altinn",
                    },
                },
             BackendTextResource.EmailDefaultBody =>
                new TextResourceElement()
                {
                    Id = resource,
                    Value = language switch
                    {
                        LanguageConst.En => "You have received a message in your Altinn inbox. Log in to view the message.",
                        LanguageConst.Nn => "Du har motteke ei melding i innboksen din i Altinn. Logg inn for å sjå meldinga.",
                        _ => "Du har mottatt en melding i innboksen din i Altinn. Logg inn for å se meldingen.",
                    },
                },
            _ => null,
        };
    }
}
