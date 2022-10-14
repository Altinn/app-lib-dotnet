using System.Xml.Serialization;
using Altinn.App.Core.Interface;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Process.Elements.Base;
using Microsoft.IdentityModel.Tokens;

namespace Altinn.App.Core.Internal.Process;

/// <summary>
/// Implementation of <see cref="IProcessReader"/> that reads from a <see cref="Definitions"/>
/// </summary>
public class ProcessReader : IProcessReader
{
    private readonly Definitions _definitions;

    public ProcessReader(IProcess processService)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(Definitions));
        Definitions? definitions = (Definitions?)serializer.Deserialize(processService.GetProcessDefinition());
        if (definitions == null)
        {
            throw new InvalidOperationException("Failed to deserialize BPMN definitions. Definitions was null");
        }

        _definitions = definitions;
    }

    /// <inheritdoc />
    public List<StartEvent> GetStartEvents()
    {
        return _definitions.Process.StartEvents;
    }

    /// <inheritdoc />
    public List<string> GetStartEventIds()
    {
        return GetStartEvents().Select(s => s.Id).ToList();
    }

    /// <inheritdoc />
    public List<ProcessTask> GetProcessTasks()
    {
        return _definitions.Process.Tasks;
    }

    /// <inheritdoc />
    public List<string> GetProcessTaskIds()
    {
        return GetProcessTasks().Select(t => t.Id).ToList();
    }

    /// <inheritdoc />
    public List<ExclusiveGateway> GetExclusiveGateways()
    {
        return _definitions.Process.ExclusiveGateway;
    }

    /// <inheritdoc />
    public List<string> GetExclusiveGatewayIds()
    {
        return GetExclusiveGateways().Select(g => g.Id).ToList();
    }

    /// <inheritdoc />
    public List<EndEvent> GetEndEvents()
    {
        return _definitions.Process.EndEvents;
    }

    /// <inheritdoc />
    public List<string> GetEndEventIds()
    {
        return GetEndEvents().Select(e => e.Id).ToList();
    }

    /// <inheritdoc />
    public List<SequenceFlow> GetSequenceFlows()
    {
        return _definitions.Process.SequenceFlow;
    }

    /// <inheritdoc />
    public List<string> GetSequenceFlowIds()
    {
        return GetSequenceFlows().Select(s => s.Id).ToList();
    }

    /// <inheritdoc />
    public List<FlowElement> GetNextElements(string currentElementId, bool followGateways, bool useGatewayDefaults = false)
    {
        List<FlowElement> nextElements = new List<FlowElement>();
        List<FlowElement> allElements = GetAllFlowElements();
        foreach (SequenceFlow sequenceFlow in GetSequenceFlows().FindAll(s => s.SourceRef == currentElementId))
        {
            var nexts = allElements.FindAll(e => e.Incoming.Contains(sequenceFlow.Id));
            if (followGateways)
            {
                foreach (var next in nexts)
                {
                    if (next is ExclusiveGateway)
                    {
                        var gateway = (ExclusiveGateway)next;
                        if (useGatewayDefaults && !gateway.Default.IsNullOrEmpty())
                        {
                            var defaultElement = allElements.Find(e => e.Id == gateway.Default);
                            if (defaultElement != null)
                            {
                                nextElements.Add(defaultElement);
                            }
                        }
                        else
                        {
                            nextElements.AddRange(GetNextElements(next.Id, followGateways, useGatewayDefaults));
                        }
                    }
                    else
                    {
                        nextElements.Add(next);
                    }
                }
            }
            else
            {
                nextElements.AddRange(nexts);
            }
        }

        return nextElements;
    }

    /// <inheritdoc />
    public List<string> GetNextElementIds(string currentElement, bool followGateways, bool useGatewayDefaults = false)
    {
        return GetNextElements(currentElement, followGateways, useGatewayDefaults).Select(e => e.Id).ToList();
    }
    
    public List<SequenceFlow> GetSequenceFlowsBetween(string currentStepId, string nextElementId)
    {
        List<SequenceFlow> flowsToReachTarget = new List<SequenceFlow>();
        foreach (SequenceFlow sequenceFlow in _definitions.Process.SequenceFlow.FindAll(s => s.SourceRef == currentStepId))
        {
            if (sequenceFlow.TargetRef.Equals(nextElementId))
            {
                flowsToReachTarget.Add(sequenceFlow);
                return flowsToReachTarget;
            }

            if (_definitions.Process.ExclusiveGateway != null && _definitions.Process.ExclusiveGateway.FirstOrDefault(g => g.Id == sequenceFlow.TargetRef) != null)
            {
                List<SequenceFlow> subGatewayFlows = GetSequenceFlowsBetween(sequenceFlow.TargetRef, nextElementId);
                if (subGatewayFlows.Any())
                {
                    flowsToReachTarget.Add(sequenceFlow);
                    flowsToReachTarget.AddRange(subGatewayFlows);
                    return flowsToReachTarget;
                }
            }
        }

        return flowsToReachTarget;
    }

    public FlowElement? GetFlowElement(string elementId)
    {
        if (elementId == null)
        {
            throw new ArgumentNullException(nameof(elementId));
        }

        ProcessTask? task = _definitions.Process.Tasks.Find(t => t.Id == elementId);
        if (task != null)
        {
            return task;
        }

        EndEvent? endEvent = _definitions.Process.EndEvents.Find(e => e.Id == elementId);
        if (endEvent != null)
        {
            return endEvent;
        }

        StartEvent? startEvent = _definitions.Process.StartEvents.Find(e => e.Id == elementId);
        if (startEvent != null)
        {
            return startEvent;
        }

        return null;
    }

    public ElementInfo? GetElementInfo(string elementId)
    {
        var e = GetFlowElement(elementId);
        ElementInfo elementInfo = new ElementInfo()
        {
            Id = e.Id,
            Name = e.Name,
            ElementType = e.ElementType()
        };
        if (e is ProcessTask)
        {
            elementInfo.AltinnTaskType = ((ProcessTask)e).TaskType;
        }

        return elementInfo;
    }

    private List<FlowElement> GetAllFlowElements()
    {
        List<FlowElement> flowElements = new List<FlowElement>();
        flowElements.AddRange(GetStartEvents());
        flowElements.AddRange(GetProcessTasks());
        flowElements.AddRange(GetExclusiveGateways());
        flowElements.AddRange(GetEndEvents());
        return flowElements;
    }

    private List<string> GetAllFlowElementIds()
    {
        List<string> flowElements = new List<string>();
        flowElements.AddRange(GetStartEventIds());
        flowElements.AddRange(GetProcessTaskIds());
        flowElements.AddRange(GetExclusiveGatewayIds());
        flowElements.AddRange(GetEndEventIds());
        return flowElements;
    }
}
