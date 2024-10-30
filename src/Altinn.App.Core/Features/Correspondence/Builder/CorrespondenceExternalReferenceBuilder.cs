using Altinn.App.Core.Features.Correspondence.Models;

namespace Altinn.App.Core.Features.Correspondence.Builder;

/// <summary>
/// Builder factory for creating <see cref="CorrespondenceExternalReference"/> objects
/// </summary>
public class CorrespondenceExternalReferenceBuilder
    : CorrespondenceBuilderBase,
        ICorrespondenceExternalReferenceBuilderType,
        ICorrespondenceExternalReferenceBuilderValue,
        ICorrespondenceExternalReferenceBuilderBuild
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
        _referenceType = referenceType;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceExternalReferenceBuilderBuild BuildWithReferenceValue(string referenceValue)
    {
        _referenceValue = referenceValue;
        return this;
    }

    /// <inheritdoc/>
    public CorrespondenceExternalReference Build()
    {
        NotNullOrEmpty(_referenceType, "Reference type is required");
        NotNullOrEmpty(_referenceValue, "Reference value is required");

        return new CorrespondenceExternalReference
        {
            ReferenceType = _referenceType.Value,
            ReferenceValue = _referenceValue
        };
    }
}
