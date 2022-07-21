using Altinn.Common.EFormidlingClient.Models.SBD;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.EFormidling.Interface;

public interface IEFormidlingReceivers
{
    public Task<List<Receiver>> GetEFormidlingReceivers(Instance instance);
}