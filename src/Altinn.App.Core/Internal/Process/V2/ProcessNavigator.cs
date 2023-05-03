using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Process.Elements.Base;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process.V2;

/// <summary>
/// Default implementation of <see cref="IProcessNavigator"/>
/// </summary>
public class ProcessNavigator : IProcessNavigator
{
    private readonly IProcessReader _processReader;
    private readonly ExclusiveGatewayFactory _gatewayFactory;

    /// <summary>
    /// Initialize a new instance of <see cref="ProcessNavigator"/>
    /// </summary>
    /// <param name="processReader">The process reader</param>
    /// <param name="gatewayFactory">Service to fetch wanted gateway filter implementation</param>
    public ProcessNavigator(IProcessReader processReader, ExclusiveGatewayFactory gatewayFactory)
    {
        _processReader = processReader;
        _gatewayFactory = gatewayFactory;
    }


    public async Task<ProcessElement?> GetNextTask(Instance instance, string currentElement, string? action)
    {
        List<ProcessElement> directFlowTargets = _processReader.GetNextElements(currentElement);
        foreach (var directFlowTarget in directFlowTargets)
        {
            if (!IsGateway(directFlowTarget))
            {
                return directFlowTarget;
            }

            var gateway = (ExclusiveGateway)directFlowTarget;
            IProcessExclusiveGateway? gatewayFilter = _gatewayFactory.GetProcessExclusiveGateway(directFlowTarget.Id);
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

            if (filteredList.Count == 1)
            {
                return _processReader.GetFlowElement(filteredList[0].TargetRef);
            }

            var defaultSequenceFlow = filteredList.Find(s => s.Id == gateway.Default);
            if (defaultSequenceFlow != null)
            {
                return _processReader.GetFlowElement(defaultSequenceFlow.TargetRef);
            }
        }

        throw new Exception("No able to find next element");
    }


    private static bool IsGateway(ProcessElement processElement)
    {
        return processElement is ExclusiveGateway;
    }
}
