using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Process;

public class DefaultExclusiveGateway : IProcessExclusiveGateway
{
    public string GatewayId { get; internal set; }
    public async Task<List<string>> FilterAsync(List<string> outgoingFlows, Instance instance)
    {
        throw new NotImplementedException();
    }
    
    /// <summary>
    /// Internal method for cloning the default implementation and setting the id
    /// as the implementation will use the id when finding the configured option files.
    /// </summary>
    /// <param name="cloneToOptionId">The actual option id to use.</param>
    /// <returns></returns>
    internal IProcessExclusiveGateway CloneDefaultTo(string cloneToOptionId)
    {
        var clone = new DefaultExclusiveGateway()
        {
            GatewayId = cloneToOptionId
        };
        return clone;
    }
}
