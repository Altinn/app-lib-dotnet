using Altinn.App.Core.EFormidling.Interface;
using Altinn.App.Services.Interface;
using Altinn.Common.EFormidlingClient.Models.SBD;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Rest.Azure;

namespace Altinn.App.Core.EFormidling.Implementation;

public class DefaultEFormidlingReceivers: IEFormidlingReceivers
{
    private readonly Application _appMetadata;
    
    public DefaultEFormidlingReceivers(IAppResources resources)
    {
        _appMetadata = resources.GetApplication();
    }
    
    public async Task<List<Receiver>> GetEFormidlingReceivers(Instance instance)
    {
        await Task.CompletedTask;
        Identifier identifier = new Identifier
        {
            // 0192 prefix for all Norwegian organisations.
            Value = $"0192:{_appMetadata.EFormidling.Receiver.Trim()}",
            Authority = "iso6523-actorid-upis"
        };

        Receiver receiver = new Receiver { Identifier = identifier };

        return new List<Receiver> { receiver };
    }
}