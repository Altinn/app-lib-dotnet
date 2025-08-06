using Altinn.App.Core.Features.Correspondence.Models;

namespace Altinn.App.Core.Features.Correspondence.Builder;

/// <summary>
/// Builder factory for creating <see cref="CorrespondenceAttachment"/> objects.
/// </summary>
public class CorrespondenceAttachmentBuilder : ICorrespondenceAttachmentBuilder
{
    private string? _filename;
    private string? _sendersReference;
    private ReadOnlyMemory<byte>? _data;
    private Stream? _streamedData;
    private bool? _isEncrypted;
    private CorrespondenceDataLocationType _dataLocationType =
        CorrespondenceDataLocationType.ExistingCorrespondenceAttachment;

    private CorrespondenceAttachmentBuilder() { }

    /// <summary>
    /// Creates a new <see cref="CorrespondenceAttachmentBuilder"/> instance.
    /// </summary>
    public static ICorrespondenceAttachmentBuilderFilename Create() => new CorrespondenceAttachmentBuilder();

    /// <inheritdoc/>
    public ICorrespondenceAttachmentBuilderSendersReference WithFilename(string filename)
    {
        BuilderUtils.NotNullOrEmpty(filename, "Filename cannot be empty");
        _filename = filename;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceAttachmentBuilderData WithSendersReference(string sendersReference)
    {
        BuilderUtils.NotNullOrEmpty(sendersReference, "Senders reference cannot be empty");
        _sendersReference = sendersReference;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceAttachmentBuilder WithData(ReadOnlyMemory<byte> data)
    {
        _data = data;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceAttachmentBuilder WithData(Stream data)
    {
        _streamedData = data;
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
        BuilderUtils.NotNullOrEmpty(_filename);
        BuilderUtils.NotNullOrEmpty(_sendersReference);
        BuilderUtils.RequireExactlyOneOf(_data, _streamedData);

        if (_streamedData is not null)
        {
            BuilderUtils.NotNullOrEmpty(_streamedData);
            return new CorrespondenceStreamedAttachment
            {
                Filename = _filename,
                SendersReference = _sendersReference,
                Data = _streamedData,
                IsEncrypted = _isEncrypted,
                DataLocationType = _dataLocationType,
            };
        }
        else
        {
            BuilderUtils.NotNullOrEmpty(_data);
            return new CorrespondenceAttachmentInMemory
            {
                Filename = _filename,
                SendersReference = _sendersReference,
                Data = _data!.Value,
                IsEncrypted = _isEncrypted,
                DataLocationType = _dataLocationType,
            };
        }
    }
}
