using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Correspondence.Builder;

/// <summary>
/// Builder factory for creating <see cref="CorrespondenceAttachment"/> objects
/// </summary>
public class CorrespondenceAttachmentBuilder
    : CorrespondenceBuilderBase,
        ICorrespondenceAttachmentBuilderFilename,
        ICorrespondenceAttachmentBuilderName,
        ICorrespondenceAttachmentBuilderSender,
        ICorrespondenceAttachmentBuilderSendersReference,
        ICorrespondenceAttachmentBuilderDataType,
        ICorrespondenceAttachmentBuilderData,
        ICorrespondenceAttachmentBuilderBuild
{
    private string? _filename;
    private string? _name;
    private OrganisationNumber? _sender;
    private string? _sendersReference;
    private string? _dataType;
    private Stream? _data;
    private string? _restrictionName;
    private bool? _isEncrypted;
    private CorrespondenceDataLocationType _dataLocationType =
        CorrespondenceDataLocationType.ExistingCorrespondenceAttachment;

    private CorrespondenceAttachmentBuilder() { }

    /// <summary>
    /// Creates a new <see cref="CorrespondenceAttachmentBuilder"/> instance
    /// </summary>
    public static ICorrespondenceAttachmentBuilderFilename Create() => new CorrespondenceAttachmentBuilder();

    /// <inheritdoc/>
    public ICorrespondenceAttachmentBuilderName WithFilename(string filename)
    {
        _filename = filename;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceAttachmentBuilderSender WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceAttachmentBuilderSendersReference WithSender(OrganisationNumber sender)
    {
        _sender = sender;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceAttachmentBuilderDataType WithSendersReference(string sendersReference)
    {
        _sendersReference = sendersReference;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceAttachmentBuilderData WithDataType(string dataType)
    {
        _dataType = dataType;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceAttachmentBuilderBuild WithData(Stream data)
    {
        _data = data;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceAttachmentBuilderBuild WithRestrictionName(string restrictionName)
    {
        _restrictionName = restrictionName;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceAttachmentBuilderBuild WithIsEncrypted(bool isEncrypted)
    {
        _isEncrypted = isEncrypted;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceAttachmentBuilderBuild WithDataLocationType(CorrespondenceDataLocationType dataLocationType)
    {
        _dataLocationType = dataLocationType;
        return this;
    }

    /// <inheritdoc/>
    public CorrespondenceAttachment Build()
    {
        NotNullOrEmpty(_filename, "Filename is required");
        NotNullOrEmpty(_name, "Name is required");
        NotNullOrEmpty(_sender, "Sender is required");
        NotNullOrEmpty(_sendersReference, "Senders reference is required");
        NotNullOrEmpty(_dataType, "Data type is required");
        NotNullOrEmpty(_data, "Data is required");

        return new CorrespondenceAttachment
        {
            Filename = _filename,
            Name = _name,
            Sender = _sender.Value,
            SendersReference = _sendersReference,
            DataType = _dataType,
            Data = _data,
            RestrictionName = _restrictionName,
            IsEncrypted = _isEncrypted,
            DataLocationType = _dataLocationType
        };
    }
}
