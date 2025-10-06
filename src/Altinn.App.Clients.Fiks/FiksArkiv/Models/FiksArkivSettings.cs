using System.Text.Json.Serialization;
using Altinn.App.Clients.Fiks.Exceptions;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Models.Layout;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Clients.Fiks.FiksArkiv.Models;

/// <summary>
/// Represents the Fiks Arkiv settings.
/// </summary>
public sealed record FiksArkivSettings
{
    /// <summary>
    /// Settings related to the receipt for a successful shipment.
    /// </summary>
    [JsonPropertyName("receipt")]
    public FiksArkivDataTypeSettings? Receipt { get; set; }

    /// <summary>
    /// Settings related to the recipient of the Fiks Arkiv message.
    /// </summary>
    [JsonPropertyName("recipient")]
    public FiksArkivRecipientSettings? Recipient { get; set; }

    /// <summary>
    /// Settings related to the Fiks Arkiv shipment metadata.
    /// </summary>
    public FiksArkivMetadataSettings? Metadata { get; set; }

    /// <summary>
    /// Settings related to the documents that will be sent to Fiks Arkiv.
    /// </summary>
    [JsonPropertyName("documents")]
    public FiksArkivDocumentSettings? Documents { get; set; }

    /// <summary>
    /// Settings related to auto-submission to Fiks Arkiv.
    /// </summary>
    [JsonPropertyName("autoSend")]
    public FiksArkivAutoSendSettings? AutoSend { get; set; }
}

/// <summary>
/// Represents various metadata settings for a Fiks Arkiv shipment, such as arkivmelding.xml properties.
/// </summary>
public class FiksArkivMetadataSettings
{
    /// <summary>
    /// The title to use for the generated saksmappe (case file) element in the arkivmelding.xml.
    /// If no title is provided, the value will default to the application title as defined in applicationmetadata.json.
    /// </summary>
    public FiksArkivBindableValue<string>? CaseFileTitle { get; init; }

    /// <summary>
    /// The title to use for the generated journalpost (journal entry) element in the arkivmelding.xml.
    /// If no title is provided, the value will default to the application title as defined in applicationmetadata.json.
    /// </summary>
    public FiksArkivBindableValue<string>? JournalEntryTitle { get; set; }

    /// <summary>
    /// Internal validation based on the requirements of <see cref="FiksArkivDefaultMessageHandler"/>
    /// </summary>
    internal void Validate(IReadOnlyList<DataType> dataTypes)
    {
        const string propertyName = $"{nameof(FiksArkivSettings.Metadata)}";

        CaseFileTitle?.Validate($"{propertyName}.{nameof(CaseFileTitle)}", dataTypes);
        JournalEntryTitle?.Validate($"{propertyName}.{nameof(JournalEntryTitle)}", dataTypes);
    }
}

/// <summary>
/// Represents the settings for Fiks Arkiv documents
/// </summary>
public sealed record FiksArkivDocumentSettings
{
    /// <summary>
    /// The settings for the primary document payload.
    /// This is usually the main data model for the form data, or the PDF representation of this data,
    /// which will eventually be sent as a `Hoveddokument` to Fiks Arkiv.
    /// </summary>
    [JsonPropertyName("primaryDocument")]
    public required FiksArkivDataTypeSettings PrimaryDocument { get; init; }

    /// <summary>
    /// Optional settings for attachments. These are additional documents that will be sent as `Vedlegg` to Fiks Arkiv.
    /// </summary>
    [JsonPropertyName("attachments")]
    public IReadOnlyList<FiksArkivDataTypeSettings>? Attachments { get; init; }

    /// <summary>
    /// Internal validation based on the requirements of <see cref="FiksArkivDefaultMessageHandler"/>
    /// </summary>
    internal void Validate(IReadOnlyList<DataType> dataTypes)
    {
        const string propertyName = nameof(FiksArkivSettings.Documents);

        PrimaryDocument.Validate($"{propertyName}.{nameof(PrimaryDocument)}", dataTypes);

        foreach (var attachment in Attachments ?? [])
        {
            attachment.Validate($"{propertyName}.{nameof(Attachments)}", dataTypes);
        }
    }
}

/// <summary>
/// Represents the settings for auto-sending a message.
/// </summary>
public sealed record FiksArkivAutoSendSettings
{
    /// <summary>
    /// The task ID to send the message after. This is applicable for use with the <see cref="FiksArkivDefaultAutoSendDecision"/> handler,
    /// and may or may not be used with other handlers.
    /// </summary>
    [JsonPropertyName("afterTaskId")]
    public string? AfterTaskId { get; init; }

    /// <summary>
    /// Settings related to error handling.
    /// </summary>
    [JsonPropertyName("errorHandling")]
    public FiksArkivErrorHandlingSettings? ErrorHandling { get; set; }

    /// <summary>
    /// Settings related to success handling.
    /// </summary>
    [JsonPropertyName("successHandling")]
    public FiksArkivSuccessHandlingSettings? SuccessHandling { get; set; }

    /// <summary>
    /// Internal validation based on the requirements of <see cref="FiksArkivDefaultAutoSendDecision"/>
    /// </summary>
    internal void Validate(IReadOnlyList<ProcessTask> configuredProcessTasks)
    {
        ErrorHandling?.Validate();

        const string propertyName =
            $"{nameof(FiksArkivSettings.AutoSend)}.{nameof(FiksArkivSettings.AutoSend.AfterTaskId)}";

        if (string.IsNullOrWhiteSpace(AfterTaskId))
            throw new FiksArkivConfigurationException(
                $"{propertyName} configuration is required for default handler {nameof(FiksArkivDefaultAutoSendDecision)}."
            );

        if (configuredProcessTasks.FirstOrDefault(x => x.Id == AfterTaskId) is null)
            throw new FiksArkivConfigurationException(
                $"{propertyName} mismatch with application process tasks: {AfterTaskId}"
            );
    }
}

/// <summary>
/// Represents the settings for success handling.
/// </summary>
public sealed record FiksArkivSuccessHandlingSettings
{
    /// <summary>
    /// Should we automatically progress to the next task after successfully sending the message?
    /// </summary>
    [JsonPropertyName("moveToNextTask")]
    public bool MoveToNextTask { get; init; }

    /// <summary>
    /// When progressing to the next task, which action should we send?
    /// </summary>
    [JsonPropertyName("action")]
    public string? Action { get; init; }

    /// <summary>
    /// Should we mark the instance as `completed` after successfully sending the message?
    /// </summary>
    [JsonPropertyName("markInstanceComplete")]
    public bool MarkInstanceComplete { get; init; }
}

/// <summary>
/// Represents the settings for error handling.
/// </summary>
public sealed record FiksArkivErrorHandlingSettings
{
    /// <summary>
    /// Should we automatically progress to the next task after failing to send the message?
    /// </summary>
    [JsonPropertyName("moveToNextTask")]
    public bool MoveToNextTask { get; init; }

    /// <summary>
    /// When progressing to the next task, which action should we send?
    /// </summary>
    [JsonPropertyName("action")]
    public string? Action { get; init; }

    /// <summary>
    /// Should we send email notifications?
    /// </summary>
    [JsonPropertyName("sendEmailNotifications")]
    public bool? SendEmailNotifications { get; init; }

    /// <summary>
    /// The email addresses to send error notifications to.
    /// </summary>
    [JsonPropertyName("emailNotificationRecipients")]
    public IEnumerable<string>? EmailNotificationRecipients { get; init; }

    internal void Validate()
    {
        const string propertyName =
            $"{nameof(FiksArkivSettings.AutoSend)}.{nameof(FiksArkivSettings.AutoSend.ErrorHandling)}";

        if (SendEmailNotifications is true && EmailNotificationRecipients?.Any() is not true)
            throw new FiksArkivConfigurationException(
                $"{propertyName}.{nameof(SendEmailNotifications)} is enabled, but no recipients have been configured."
            );

        if (EmailNotificationRecipients?.Any(string.IsNullOrWhiteSpace) is true)
            throw new FiksArkivConfigurationException(
                $"{propertyName}.{nameof(EmailNotificationRecipients)} contains empty entries."
            );
    }
}

/// <summary>
/// Represents the settings for a Fiks Arkiv recipient.
/// </summary>
public sealed record FiksArkivRecipientSettings
{
    /// <summary>
    /// The Fiks Arkiv recipient account. This is a <see cref="Guid"/> address to ship messages to.
    /// </summary>
    public required FiksArkivBindableValue<Guid?> FiksAccount { get; init; }

    /// <summary>
    /// An optional identifier for the recipient. This can be a municipality number or other relevant identifier.
    /// </summary>
    public FiksArkivBindableValue<string>? Identifier { get; init; }

    /// <summary>
    /// An optional organization number for the recipient.
    /// </summary>
    public FiksArkivBindableValue<string>? OrganizationNumber { get; init; }

    /// <summary>
    /// An optional name for the recipient.
    /// </summary>
    public FiksArkivBindableValue<string>? Name { get; init; }

    /// <summary>
    /// Internal validation based on the requirements of <see cref="FiksArkivDefaultMessageHandler"/>
    /// </summary>
    internal void Validate(IReadOnlyList<DataType> dataTypes)
    {
        const string propertyName = $"{nameof(FiksArkivSettings.Recipient)}";

        FiksAccount.Validate($"{propertyName}.{nameof(FiksAccount)}", dataTypes);
        Identifier?.Validate($"{propertyName}.{nameof(Identifier)}", dataTypes);
        OrganizationNumber?.Validate($"{propertyName}.{nameof(OrganizationNumber)}", dataTypes);
        Name?.Validate($"{propertyName}.{nameof(Name)}", dataTypes);
    }
}

/// <summary>
/// Represents the settings for a <see cref="FiksArkivRecipientSettings"/> property.
/// Allows setting the <see cref="Value"/> directly, or via a <see cref="DataModelBinding"/> which is evaluated right before shipment.
/// </summary>
public sealed record FiksArkivBindableValue<T>
{
    /// <summary>
    /// The value supplied directly.
    /// </summary>
    [JsonPropertyName("value")]
    public T? Value { get; init; }

    /// <summary>
    /// A data model binding to the property containing the desired value.
    /// </summary>
    [JsonPropertyName("dataModelBinding")]
    public FiksArkivDataModelBinding? DataModelBinding { get; init; }

    /// <summary>
    /// Internal validation based on the requirements of <see cref="FiksArkivDefaultMessageHandler"/>
    /// </summary>
    internal void Validate(string propertyName, IReadOnlyList<DataType> dataTypes)
    {
        if (Value is null && DataModelBinding is null)
            throw new FiksArkivConfigurationException(
                $"{propertyName}: Either `{nameof(Value)}` or `{nameof(DataModelBinding)}` must be configured for handler {nameof(FiksArkivDefaultMessageHandler)}."
            );

        if (Value is not null && DataModelBinding is not null)
            throw new FiksArkivConfigurationException(
                $"{propertyName}: Both `{nameof(Value)}` and `{nameof(DataModelBinding)}` cannot be set at the same time."
            );

        DataModelBinding?.Validate($"{propertyName}.{nameof(DataModelBinding)}", dataTypes);
    }
}

/// <summary>
/// Represents the settings for a Fiks Arkiv recipient data model binding.
/// </summary>
public sealed record FiksArkivDataModelBinding
{
    /// <summary>
    /// The data type of the binding (e.g. `Model`)
    /// </summary>
    [JsonPropertyName("dataType")]
    public required string DataType { get; init; }

    /// <summary>
    /// The field that contains the account ID. Dot notation is supported.
    /// </summary>
    [JsonPropertyName("field")]
    public required string Field { get; init; }

    /// <summary>
    /// Implicit conversion from <see cref="FiksArkivDataModelBinding"/> to <see cref="ModelBinding"/>.
    /// </summary>
    public static implicit operator ModelBinding(FiksArkivDataModelBinding modelBinding) =>
        new() { Field = modelBinding.Field, DataType = modelBinding.DataType };

    /// <summary>
    /// Internal validation based on the requirements of <see cref="FiksArkivDefaultMessageHandler"/>
    /// </summary>
    internal void Validate(string propertyName, IReadOnlyList<DataType> dataTypes)
    {
        if (string.IsNullOrWhiteSpace(DataType))
            throw new FiksArkivConfigurationException(
                $"{propertyName}.{nameof(DataType)} configuration is required for handler {nameof(FiksArkivDefaultMessageHandler)}"
            );

        if (dataTypes.Any(x => x.Id == DataType) is false)
            throw new FiksArkivConfigurationException(
                $"{propertyName}.{nameof(DataType)} mismatch with application data types: {DataType}"
            );

        if (string.IsNullOrWhiteSpace(Field))
            throw new FiksArkivConfigurationException(
                $"{propertyName}.{nameof(Field)} configuration is required for handler {nameof(FiksArkivDefaultMessageHandler)}."
            );
    }
}

/// <summary>
/// Represents the settings for a Fiks Arkiv <see cref="DataType"/> binding (document, attachment, receipt).
/// </summary>
public sealed record FiksArkivDataTypeSettings
{
    /// <summary>
    /// The data type as defined in applicationmetadata.json.
    /// </summary>
    [JsonPropertyName("dataType")]
    public required string DataType { get; init; }

    /// <summary>
    /// Optional filename for the binding. If not specified, the filename from <see cref="DataElement"/> will be used.
    /// If that also is missing, the filename will be derived from the data type.
    /// </summary>
    [JsonPropertyName("filename")]
    public string? Filename { get; init; }

    /// <summary>
    /// Internal validation based on the requirements of <see cref="FiksArkivDefaultMessageHandler"/>
    /// </summary>
    internal void Validate(string propertyName, IReadOnlyList<DataType> dataTypes)
    {
        if (string.IsNullOrWhiteSpace(DataType))
            throw new FiksArkivConfigurationException(
                $"{propertyName}.{nameof(DataType)} configuration is required for handler {nameof(FiksArkivDefaultMessageHandler)}."
            );
        if (dataTypes.Any(x => x.Id == DataType) is false)
            throw new FiksArkivConfigurationException(
                $"{propertyName}.{nameof(DataType)} mismatch with application data types: {DataType}"
            );
    }
}
