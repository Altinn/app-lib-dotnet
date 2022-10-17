using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Process.Elements.Base;

namespace Altinn.App.Core.Internal.Process;

/// <summary>
/// Interface for classes that reads the applications process
/// </summary>
public interface IProcessReader
{

    /// <summary>
    /// Get all defined StartEvents in the process
    /// </summary>
    /// <returns></returns>
    public List<StartEvent> GetStartEvents();
    
    /// <summary>
    /// Get ids of all defined StartEvents in the process
    /// </summary>
    /// <returns></returns>
    public List<string> GetStartEventIds();
    
    /// <summary>
    /// Get all defined Tasks in the process
    /// </summary>
    /// <returns></returns>
    public List<ProcessTask> GetProcessTasks();
    
    /// <summary>
    /// Get ids of all defined Tasks in the process
    /// </summary>
    /// <returns></returns>
    public List<string> GetProcessTaskIds();

    /// <summary>
    /// Get all ExclusiveGateways defined in the process
    /// </summary>
    /// <returns></returns>
    public List<ExclusiveGateway> GetExclusiveGateways();
    
    /// <summary>
    /// Get ids of all defined ExclusiveGateways in the process
    /// </summary>
    /// <returns></returns>
    public List<string> GetExclusiveGatewayIds();

    /// <summary>
    /// Get all EndEvents defined in the process
    /// </summary>
    /// <returns></returns>
    public List<EndEvent> GetEndEvents();
    
    /// <summary>
    /// Get ids of all EndEvents defined in the process
    /// </summary>
    /// <returns></returns>
    public List<string> GetEndEventIds();

    /// <summary>
    /// Get all SequenceFlows defined in the process
    /// </summary>
    /// <returns></returns>
    public List<SequenceFlow> GetSequenceFlows();

    
    /// <summary>
    /// Get ids of all SequenceFlows defined in the process
    /// </summary>
    /// <returns></returns>
    public List<string> GetSequenceFlowIds();

    /// <summary>
    /// Find all possible next elements from current element
    /// </summary>
    /// <param name="currentElement">Current FlowElement</param>
    /// <param name="followGateways">Follow gateways and return downstream element instead</param>
    /// <param name="useGatewayDefaults">If gateway has default follow it</param>
    /// <returns></returns>
    public List<FlowElement> GetNextElements(string currentElement, bool followGateways, bool useGatewayDefaults = true);

    /// <summary>
    /// Find ids of all possible next elements from current element
    /// </summary>
    /// <param name="currentElement">Current FlowElement Id</param>
    /// <param name="followGateways">Follow gateways and return downstream ids instead</param>
    /// <param name="useGatewayDefaults">If gateway has default follow it</param>
    /// <returns></returns>
    public List<string> GetNextElementIds(string currentElement, bool followGateways, bool useGatewayDefaults = true);

    /// <summary>
    /// Returns a list of sequence flow to be followed between current step and next element
    /// </summary>
    public List<SequenceFlow> GetSequenceFlowsBetween(string currentStepId, string nextElementId);

    /// <summary>
    /// Returns StartEvent, Task or EndEvent with given Id, null if element not found
    /// </summary>
    /// <param name="elementId">Id of element to look for</param>
    /// <returns><see cref="FlowElement"/> or null</returns>
    public FlowElement? GetFlowElement(string elementId);

    /// <summary>
    /// Retuns ElementInfo for StartEvent, Task or EndEvent with given Id, null if element not found
    /// </summary>
    /// <param name="elementId">Id of element to look for</param>
    /// <returns><see cref="ElementInfo"/> or null</returns>
    public ElementInfo? GetElementInfo(string elementId);

}
