namespace Altinn.App.Core.Features.Correspondence.Models;

internal interface ICorrespondence
{
    /// <summary>
    /// Serialize the correspondence to a <see cref="MultipartFormDataContent"/> instance
    /// </summary>
    /// <param name="content"></param>
    void Serialize(MultipartFormDataContent content);
}

internal interface ICorrespondenceItem
{
    /// <summary>
    /// Serialize each correspondence item to a <see cref="MultipartFormDataContent"/> instance
    /// </summary>
    /// <param name="content"></param>
    /// <param name="index"></param>
    void Serialize(MultipartFormDataContent content, int index);
}
