using Altinn.App.Core.Interface;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Implementation;

public class NullEFormidlingMetadata: IEFormidlingMetadata
{
    public Task<(string MetadataFilename, Stream Metadata)> GenerateEFormidlingMetadata(Instance instance)
    {
        throw new NotImplementedException(
            "No method available for generating arkivmelding for eFormidling shipment supplied. Please implement IEFormidlingMetadata interface");
    }
}