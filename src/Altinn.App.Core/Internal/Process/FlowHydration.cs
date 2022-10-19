using Altinn.App.Core.Features.Process;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Process.Elements.Base;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process;

public class FlowHydration: IFlowHydration
{
    private readonly IProcessReader _processReader;
    private readonly ExclusiveGatewayFactory _gatewayFactory;

    public FlowHydration(IProcessReader processReader, ExclusiveGatewayFactory gatewayFactory)
    {
        _processReader = processReader;
        _gatewayFactory = gatewayFactory;
    }
    
    /// <inheritdoc />
    public async Task<List<FlowElement>> NextFollowAndFilterGateways(Instance instance, string? currentElement)
    {
        List<FlowElement> filteredNext = new List<FlowElement>();
        var directFlowTargets = _processReader.GetNextElements(currentElement, false, false);
        return await NextFollowAndFilterGateways(instance, directFlowTargets);
    }

    /// <inheritdoc />
    public async Task<List<FlowElement>> NextFollowAndFilterGateways(Instance instance, List<FlowElement?> originNextElements)
    {
        List<FlowElement> filteredNext = new List<FlowElement>();
        foreach (var directFlowTarget in originNextElements)
        {
            if (directFlowTarget == null)
            {
                continue;
            }
            if (!IsGateway(directFlowTarget))
            {
                filteredNext.Add(directFlowTarget);
                continue;
            }

            var gateway = (ExclusiveGateway)directFlowTarget;
            var gatewayFilter = _gatewayFactory.GetProcessExclusiveGateway(directFlowTarget.Id);
            List<SequenceFlow> outgoingFlows = _processReader.GetOutgoingSequenceFlows(directFlowTarget);
            List<SequenceFlow> filteredList;
            if (gatewayFilter == null)
            {
                filteredList = outgoingFlows;
            }
            else
            {
                filteredList = await gatewayFilter.FilterAsync(outgoingFlows, instance);
            }

            var defaultSequenceFlow = filteredList.Find(s => s.Id == gateway.Default);
            if (defaultSequenceFlow != null)
            {
                var defaultTarget = _processReader.GetFlowElement(defaultSequenceFlow.TargetRef);
                filteredNext.AddRange(await NextFollowAndFilterGateways(instance, new List<FlowElement?> { defaultTarget }));
            }
            else
            {
                var filteredTargets= filteredList.Select(e => _processReader.GetFlowElement(e.TargetRef)).ToList();
                filteredNext.AddRange(await NextFollowAndFilterGateways(instance, filteredTargets));
            }
        }

        return filteredNext;
    }

    private bool IsGateway(FlowElement flowElement)
    {
        return flowElement is ExclusiveGateway;
    }
}
