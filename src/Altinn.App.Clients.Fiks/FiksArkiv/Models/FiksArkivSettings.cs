using System.Text.Json.Serialization;
using Altinn.App.Clients.Fiks.Exceptions;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Layout;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Clients.Fiks.FiksArkiv.Models;

/// <summary>
/// Represents the Fiks Arkiv settings.
/// </summary>
public sealed record FiksArkivSettings
{
    /// <summary>
    /// Settings related to error handling.
    /// </summary>
    [JsonPropertyName("errorHandling")]
    public FiksArkivErrorHandlingSettings? ErrorHandling { get; set; }

    /// <summary>
    /// Settings related to auto-submission to Fiks Arkiv.
    /// </summary>
    [JsonPropertyName("autoSend")]
    public FiksArkivAutoSendSettings? AutoSend { get; set; }

    internal void Validate(IReadOnlyList<DataType> dataTypes, IReadOnlyList<ProcessTask> processTasks)
    {
        ErrorHandling?.Validate();
        AutoSend?.Validate(dataTypes, processTasks);
    }
}

/// <summary>
/// Represents the settings for error handling.
/// </summary>
public sealed record FiksArkivErrorHandlingSettings
{
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
        const string propertyName = nameof(FiksArkivSettings.ErrorHandling);

        if (SendEmailNotifications is true && EmailNotificationRecipients?.Any() is false)
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
/// Represents the settings for auto-sending a message.
/// </summary>
public sealed record FiksArkivAutoSendSettings
{
    /// <summary>
    /// The task ID to send the message after.
    /// </summary>
    [JsonPropertyName("afterTaskId")]
    public required string AfterTaskId { get; init; }

    /// <summary>
    /// Should we automatically progress to the next task after successfully sending the message?
    /// </summary>
    [JsonPropertyName("autoProgressToNextTask")]
    public bool AutoProgressToNextTask { get; init; }

    /// <summary>
    /// The data type of the receipt object.
    /// </summary>
    public required string ReceiptDataType { get; init; }

    /// <summary>
    /// The recipient of the message.
    /// </summary>
    [JsonPropertyName("recipient")]
    public required FiksArkivRecipientSettings Recipient { get; init; }

    /// <summary>
    /// The settings for the primary document payload.
    /// This is usually the main data model for the form data, or the PDF representation of this data,
    /// which will eventually be sent as a `Hoveddokument` to Fiks Arkiv.
    /// </summary>
    [JsonPropertyName("primaryDocument")]
    public required FiksArkivPayloadSettings PrimaryDocument { get; init; }

    /// <summary>
    /// Optional settings for attachments. These are additional documents that will be sent as `Vedlegg` to Fiks Arkiv.
    /// </summary>
    [JsonPropertyName("attachments")]
    public IReadOnlyList<FiksArkivPayloadSettings>? Attachments { get; init; }

    internal void Validate(IReadOnlyList<DataType> dataTypes, IReadOnlyList<ProcessTask> processTasks)
    {
        const string propertyName = nameof(FiksArkivSettings.AutoSend);

        if (string.IsNullOrWhiteSpace(AfterTaskId))
            throw new FiksArkivConfigurationException(
                $"{propertyName}.{nameof(AfterTaskId)} configuration is required for auto-send."
            );

        if (processTasks.FirstOrDefault(x => x.Id == AfterTaskId) is null)
            throw new FiksArkivConfigurationException(
                $"{propertyName}.{nameof(AfterTaskId)} mismatch with application process tasks: {AfterTaskId}"
            );

        if (string.IsNullOrWhiteSpace(ReceiptDataType))
            throw new FiksArkivConfigurationException(
                $"{propertyName}.{nameof(ReceiptDataType)} configuration is required for auto-send."
            );

        if (dataTypes.Any(x => x.Id == ReceiptDataType) is false)
            throw new FiksArkivConfigurationException(
                $"{propertyName}.{nameof(ReceiptDataType)} mismatch with application data types: {ReceiptDataType}"
            );

        Recipient.Validate($"{propertyName}.{nameof(Recipient)}", dataTypes);
        PrimaryDocument.Validate($"{propertyName}.{nameof(PrimaryDocument)}", dataTypes);

        foreach (var attachment in Attachments ?? [])
        {
            attachment.Validate($"{propertyName}.{nameof(Attachments)}", dataTypes);
        }
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
    public required FiksArkivRecipientValue<Guid?> FiksAccount { get; init; }

    /// <summary>
    /// An optional identifier for the recipient. This can be a municipality number or other relevant identifier.
    /// </summary>
    public FiksArkivRecipientValue<string>? Identifier { get; init; }

    /// <summary>
    /// An optional organization number for the recipient.
    /// </summary>
    public FiksArkivRecipientValue<string>? OrganizationNumber { get; init; }

    /// <summary>
    /// An optional name for the recipient.
    /// </summary>
    public FiksArkivRecipientValue<string>? Name { get; init; }

    internal void Validate(string propertyName, IReadOnlyList<DataType> dataTypes)
    {
        FiksAccount.Validate($"{propertyName}.{nameof(FiksAccount)}", dataTypes);
        Identifier?.Validate($"{propertyName}.{nameof(Identifier)}", dataTypes);
        OrganizationNumber?.Validate($"{propertyName}.{nameof(OrganizationNumber)}", dataTypes);
        Name?.Validate($"{propertyName}.{nameof(Name)}", dataTypes);
    }
}

/// <summary>
/// Represents the settings for a <see cref="FiksArkivRecipientSettings"/> property.
/// Allows setting the <see cref="Id"/> directly, or via a <see cref="DataModelBinding"/> which is evaluated right before shipment.
/// </summary>
public sealed record FiksArkivRecipientValue<T>
{
    /// <summary>
    /// The account ID of the recipient.
    /// </summary>
    [JsonPropertyName("id")]
    public T? Id { get; init; }

    /// <summary>
    /// A data model binding for the account ID of the recipient.
    /// </summary>
    [JsonPropertyName("dataModelBinding")]
    public FiksArkivDataModelBinding? DataModelBinding { get; init; }

    internal void Validate(string propertyName, IReadOnlyList<DataType> dataTypes)
    {
        if (Id is null && DataModelBinding is null)
            throw new FiksArkivConfigurationException(
                $"{propertyName}: Either `{nameof(Id)}` or `{nameof(DataModelBinding)}` must be configured."
            );

        if (Id is not null && DataModelBinding is not null)
            throw new FiksArkivConfigurationException(
                $"{propertyName}: Both `{nameof(Id)}` and `{nameof(DataModelBinding)}` cannot be set at the same time."
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

    internal void Validate(string propertyName, IReadOnlyList<DataType> dataTypes)
    {
        if (string.IsNullOrWhiteSpace(DataType))
            throw new FiksArkivConfigurationException(
                $"{propertyName}.{nameof(DataType)} configuration is required for auto-send."
            );
        if (dataTypes.Any(x => x.Id == DataType) is false)
            throw new FiksArkivConfigurationException(
                $"{propertyName}.{nameof(DataType)} mismatch with application data types: {DataType}"
            );

        if (string.IsNullOrWhiteSpace(Field))
            throw new FiksArkivConfigurationException(
                $"{propertyName}.{nameof(Field)} configuration is required for auto-send."
            );
    }
}

/// <summary>
/// Represents the settings for a Fiks Arkiv document or attachment payload.
/// </summary>
public sealed record FiksArkivPayloadSettings
{
    /// <summary>
    /// The data type of the payload.
    /// </summary>
    [JsonPropertyName("dataType")]
    public required string DataType { get; init; }

    /// <summary>
    /// Optional filename for the payload. If not specified, the filename from <see cref="DataElement"/> will be used.
    /// If that also is missing, the filename will be derived from the data type.
    /// </summary>
    [JsonPropertyName("filename")]
    public string? Filename { get; init; }

    internal void Validate(string propertyName, IReadOnlyList<DataType> dataTypes)
    {
        if (string.IsNullOrWhiteSpace(DataType))
            throw new FiksArkivConfigurationException(
                $"{propertyName}.{nameof(DataType)} configuration is required for auto-send."
            );
        if (dataTypes.Any(x => x.Id == DataType) is false)
            throw new FiksArkivConfigurationException(
                $"{propertyName}.{nameof(DataType)} mismatch with application data types: {DataType}"
            );
    }
}
