namespace Altinn.App.Core.Features.Correspondence.Models;

/// <summary>
/// Represents a reference to another item in the Altinn ecosystem
/// </summary>
public sealed record CorrespondenceExternalReference : CorrespondenceBase, ICorrespondenceItem
{
    /// <summary>
    /// The reference type
    /// </summary>
    public required CorrespondenceReferenceType ReferenceType { get; init; }

    /// <summary>
    /// The reference value
    /// </summary>
    public required string ReferenceValue { get; init; }

    // TODO: Should this be internal?
    /// <inheritdoc />
    public void Serialize(MultipartFormDataContent content, int index)
    {
        AddRequired(content, ReferenceType.ToString(), $"Correspondence.ExternalReferences[{index}].ReferenceType");
        AddRequired(content, ReferenceValue, $"Correspondence.ExternalReferences[{index}].ReferenceValue");
    }
}
