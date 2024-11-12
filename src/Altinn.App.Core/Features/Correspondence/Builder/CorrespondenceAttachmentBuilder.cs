using Altinn.App.Core.Features.Correspondence.Models;

namespace Altinn.App.Core.Features.Correspondence.Builder;

/// <summary>
/// Builder factory for creating <see cref="CorrespondenceAttachment"/> objects
/// </summary>
public class CorrespondenceAttachmentBuilder : CorrespondenceBuilderBase, ICorrespondenceAttachmentBuilder
{
    private string? _filename;
    private string? _name;
    private string? _sendersReference;
    private string? _dataType;
    private Stream? _data;
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
    public ICorrespondenceAttachmentBuilderSendersReference WithName(string name)
    {
        _name = name;
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
    public ICorrespondenceAttachmentBuilder WithData(Stream data)
    {
        _data = data;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceAttachmentBuilder WithIsEncrypted(bool isEncrypted)
    {
        _isEncrypted = isEncrypted;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceAttachmentBuilder WithDataLocationType(CorrespondenceDataLocationType dataLocationType)
    {
        _dataLocationType = dataLocationType;
        return this;
    }

    /// <inheritdoc/>
    public CorrespondenceAttachment Build()
    {
        NotNullOrEmpty(_filename, "Filename is required");
        NotNullOrEmpty(_name, "Name is required");
        NotNullOrEmpty(_sendersReference, "Senders reference is required");
        NotNullOrEmpty(_dataType, "Data type is required");
        NotNullOrEmpty(_data, "Data is required");

        return new CorrespondenceAttachment
        {
            Filename = _filename,
            Name = _name,
            SendersReference = _sendersReference,
            DataType = _dataType,
            Data = _data,
            IsEncrypted = _isEncrypted,
            DataLocationType = _dataLocationType
        };
    }
}
