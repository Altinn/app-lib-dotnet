using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.Process.Elements.Base;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process;

/// <summary>
/// Defines method needed for filtering process flows based on application configuration
/// </summary>
public interface IFlowHydration
{
    /// <summary>
    /// Checks next elements of current for gateways and apply custom gateway decisions based on <see cref="IProcessExclusiveGateway"/> implementations 
    /// </summary>
    /// <param name="instance">Instance data</param>
    /// <param name="currentElement">Current process element id</param>
    /// <returns>Filtered list of next elements</returns>
    public Task<List<FlowElement>> NextFollowAndFilterGateways(Instance instance, string? currentElement);

    /// <summary>
    /// Takes a list of flows checks for gateways and apply custom gateway decisions based on <see cref="IProcessExclusiveGateway"/> implementations 
    /// </summary>
    /// <param name="instance">Instance data</param>
    /// <param name="originNextElements">Original list of next elements</param>
    /// <returns>Filtered list of next elements</returns>
    public Task<List<FlowElement>> NextFollowAndFilterGateways(Instance instance, List<FlowElement?> originNextElements);
}
