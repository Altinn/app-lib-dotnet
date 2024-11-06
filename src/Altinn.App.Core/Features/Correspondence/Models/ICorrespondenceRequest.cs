namespace Altinn.App.Core.Features.Correspondence.Models;

internal interface ICorrespondenceRequest : ICorrespondenceSerializer
{
    /// <summary>
    /// Serialize the correspondence to a newly created <see cref="MultipartFormDataContent"/> instance
    /// </summary>
    MultipartFormDataContent Serialize();
}

internal interface ICorrespondenceSerializer
{
    /// <summary>
    /// Serialize the correspondence to an existing <see cref="MultipartFormDataContent"/> instance
    /// </summary>
    /// <param name="content"></param>
    void Serialize(MultipartFormDataContent content);
}

internal interface ICorrespondenceItemSerializer
{
    /// <summary>
    /// Serialize each correspondence item to a <see cref="MultipartFormDataContent"/> instance
    /// </summary>
    /// <param name="content"></param>
    /// <param name="index"></param>
    void Serialize(MultipartFormDataContent content, int index);
}
