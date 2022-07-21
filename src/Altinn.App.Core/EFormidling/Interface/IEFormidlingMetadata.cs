using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.EFormidling.Interface;

public interface IEFormidlingMetadata
{
    public Task<(string MetadataFilename, Stream Metadata)> GenerateEFormidlingMetadata(Instance instance);
}