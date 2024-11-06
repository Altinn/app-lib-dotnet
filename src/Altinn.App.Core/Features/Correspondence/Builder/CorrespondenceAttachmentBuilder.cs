using Altinn.App.Core.Features.Correspondence.Models;

namespace Altinn.App.Core.Features.Correspondence.Builder;

/// <summary>
/// Builder factory for creating <see cref="CorrespondenceAttachment"/> objects
/// </summary>
public class CorrespondenceAttachmentBuilder
    : CorrespondenceBuilderBase,
        ICorrespondenceAttachmentBuilderNeedsFilename,
        ICorrespondenceAttachmentBuilderNeedsName,
        ICorrespondenceAttachmentBuilderNeedsSendersReference,
        ICorrespondenceAttachmentBuilderNeedsDataType,
        ICorrespondenceAttachmentBuilderNeedsData,
        ICorrespondenceAttachmentBuilderCanBuild
{
    private string? _filename;
    private string? _name;
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
    public static ICorrespondenceAttachmentBuilderNeedsFilename Create() => new CorrespondenceAttachmentBuilder();

    /// <inheritdoc/>
    public ICorrespondenceAttachmentBuilderNeedsName WithFilename(string filename)
    {
        _filename = filename;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceAttachmentBuilderNeedsSendersReference WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceAttachmentBuilderNeedsDataType WithSendersReference(string sendersReference)
    {
        _sendersReference = sendersReference;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceAttachmentBuilderNeedsData WithDataType(string dataType)
    {
        _dataType = dataType;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceAttachmentBuilderCanBuild WithData(Stream data)
    {
        _data = data;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceAttachmentBuilderCanBuild WithRestrictionName(string restrictionName)
    {
        _restrictionName = restrictionName;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceAttachmentBuilderCanBuild WithIsEncrypted(bool isEncrypted)
    {
        _isEncrypted = isEncrypted;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceAttachmentBuilderCanBuild WithDataLocationType(
        CorrespondenceDataLocationType dataLocationType
    )
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
            RestrictionName = _restrictionName,
            IsEncrypted = _isEncrypted,
            DataLocationType = _dataLocationType
        };
    }
}
