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

    /// <summary>
    /// Create instance of ProcessReader where process stream is fetched from <see cref="IProcess"/>
    /// </summary>
    /// <param name="processService">Implementation of IProcess used to get stream of BPMN process</param>
    /// <exception cref="InvalidOperationException">If BPMN file could not be deserialized</exception>
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
    public List<FlowElement> GetNextElements(string currentElementId, bool followGateways, bool useGatewayDefaults = true)
    {
        EnsureArgumentNotNull(currentElementId, nameof(currentElementId));
        List<FlowElement> nextElements = new List<FlowElement>();
        List<FlowElement> allElements = GetAllFlowElements();
        if (!allElements.Exists(e => e.Id == currentElementId))
        {
            throw new ProcessException($"Unable to find a element using element id {currentElementId}.");
        }

        foreach (SequenceFlow sequenceFlow in GetSequenceFlows().FindAll(s => s.SourceRef == currentElementId))
        {
            var nexts = allElements.FindAll(e => sequenceFlow.TargetRef == e.Id);
            if (followGateways)
            {
                foreach (var next in nexts)
                {
                    if (next is ExclusiveGateway)
                    {
                        var gateway = (ExclusiveGateway)next;
                        var nextId = next.Id;
                        if (useGatewayDefaults && !gateway.Default.IsNullOrEmpty())
                        {
                            nextElements.Add(GetDefaultElementForGateway(gateway, allElements));
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
    public List<string> GetNextElementIds(string currentElement, bool followGateways, bool useGatewayDefaults = true)
    {
        return GetNextElements(currentElement, followGateways, useGatewayDefaults).Select(e => e.Id).ToList();
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public FlowElement? GetFlowElement(string elementId)
    {
        EnsureArgumentNotNull(elementId, nameof(elementId));

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

    /// <inheritdoc />
    public ElementInfo? GetElementInfo(string elementId)
    {
        var e = GetFlowElement(elementId);
        if (e == null)
        {
            return null;
        }

        ElementInfo elementInfo = new ElementInfo()
        {
            Id = e.Id,
            Name = e.Name,
            ElementType = e.ElementType()
        };
        if (e is ProcessTask task)
        {
            elementInfo.AltinnTaskType = task.TaskType;
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

    private FlowElement GetDefaultElementForGateway(ExclusiveGateway exclusiveGateway, List<FlowElement> allElements)
    {
        var defaultSequenceFlow = GetSequenceFlows().Find(s => s.Id == exclusiveGateway.Default)?.TargetRef ?? "";
        var defaultElement = allElements.Find(e => e.Id == defaultSequenceFlow);
        if (defaultElement != null)
        {
            return defaultElement;
        }

        throw new ProcessException($"Unable to find process task with id: '{defaultSequenceFlow}' in process definition.");
    }

    private void EnsureArgumentNotNull(object argument, string paramName)
    {
        if (argument == null)
            throw new ArgumentNullException(paramName);
    }
}
