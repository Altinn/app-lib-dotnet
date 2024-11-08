using Altinn.App.Core.Features.Correspondence.Models;

namespace Altinn.App.Core.Features.Correspondence.Builder;

/// <summary>
/// Indicates that the <see cref="CorrespondenceExternalReferenceBuilder"/> instance is on the <see cref="CorrespondenceExternalReference.ReferenceType"/> step
/// </summary>
public interface ICorrespondenceExternalReferenceBuilderType
{
    /// <summary>
    /// Sets the reference type of the correspondence external reference
    /// </summary>
    /// <param name="referenceType">The reference type</param>
    ICorrespondenceExternalReferenceBuilderValue WithReferenceType(CorrespondenceReferenceType referenceType);
}

/// <summary>
/// Indicates that the <see cref="CorrespondenceExternalReferenceBuilder"/> instance is on the <see cref="CorrespondenceExternalReference.ReferenceValue"/> step
/// </summary>
public interface ICorrespondenceExternalReferenceBuilderValue
{
    /// <summary>
    /// Sets the reference value of the correspondence external reference
    /// </summary>
    /// <param name="referenceValue">The reference value</param>
    ICorrespondenceExternalReferenceBuilder WithReferenceValue(string referenceValue);
}

/// <summary>
/// Indicates that the <see cref="CorrespondenceExternalReferenceBuilder"/> instance has completed all required steps and can proceed to <see cref="CorrespondenceExternalReferenceBuilder.Build"/>
/// </summary>
public interface ICorrespondenceExternalReferenceBuilder
    : ICorrespondenceExternalReferenceBuilderType,
        ICorrespondenceExternalReferenceBuilderValue
{
    /// <summary>
    /// Builds the <see cref="CorrespondenceExternalReference"/> instance
    /// </summary>
    /// <returns></returns>
    CorrespondenceExternalReference Build();
}
