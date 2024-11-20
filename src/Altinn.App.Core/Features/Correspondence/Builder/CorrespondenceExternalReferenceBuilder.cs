using Altinn.App.Core.Features.Correspondence.Models;

namespace Altinn.App.Core.Features.Correspondence.Builder;

/// <summary>
/// Builder factory for creating <see cref="CorrespondenceExternalReference"/> objects
/// </summary>
public class CorrespondenceExternalReferenceBuilder : ICorrespondenceExternalReferenceBuilder
{
    private CorrespondenceReferenceType? _referenceType;
    private string? _referenceValue;

    private CorrespondenceExternalReferenceBuilder() { }

    /// <summary>
    /// Creates a new <see cref="CorrespondenceExternalReferenceBuilder"/> instance
    /// </summary>
    public static ICorrespondenceExternalReferenceBuilderType Create() => new CorrespondenceExternalReferenceBuilder();

    /// <inheritdoc/>
    public ICorrespondenceExternalReferenceBuilderValue WithReferenceType(CorrespondenceReferenceType referenceType)
    {
        BuilderUtils.NotNullOrEmpty(referenceType, "Reference type cannot be empty");
        _referenceType = referenceType;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceExternalReferenceBuilder WithReferenceValue(string referenceValue)
    {
        BuilderUtils.NotNullOrEmpty(referenceValue, "Reference value cannot be empty");
        _referenceValue = referenceValue;
        return this;
    }

    /// <inheritdoc/>
    public CorrespondenceExternalReference Build()
    {
        BuilderUtils.NotNullOrEmpty(_referenceType);
        BuilderUtils.NotNullOrEmpty(_referenceValue);

        return new CorrespondenceExternalReference
        {
            ReferenceType = _referenceType.Value,
            ReferenceValue = _referenceValue
        };
    }
}
