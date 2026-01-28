using System.Globalization;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Auth;
using Altinn.App.Core.Features.Process;
using Altinn.App.Core.Internal.WorkflowEngine.Commands;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Process;
using Altinn.App.ProcessEngine.Models;
using Altinn.Platform.Storage.Interface.Enums;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.WorkflowEngine;

/// <summary>
/// Factory for creating ProcessNextRequest objects from process state changes.
/// Maps instance events to command sequences and assembles the complete request.
/// </summary>
internal sealed class ProcessNextRequestFactory
{
    private readonly AppImplementationFactory _appImplementationFactory;
    private readonly IAuthenticationContext _authenticationContext;

    public ProcessNextRequestFactory(
        AppImplementationFactory appImplementationFactory,
        IAuthenticationContext authenticationContext
    )
    {
        _appImplementationFactory = appImplementationFactory;
        _authenticationContext = authenticationContext;
    }

    /// <summary>
    /// Creates a ProcessNextRequest from the instance and process state change.
    /// Maps each instance event to its corresponding command sequence.
    /// </summary>
    public async Task<Altinn.App.ProcessEngine.Models.ProcessNextRequest> Create(ProcessStateChange processStateChange)
    {
        var commands = new List<ProcessEngineCommandRequest>();

        bool isInitialTaskStart = processStateChange.OldProcessState?.CurrentTask is null;

        foreach (InstanceEvent instanceEvent in processStateChange.Events ?? [])
        {
            if (!Enum.TryParse(instanceEvent.EventType, true, out InstanceEventType instanceEventType))
                continue;

            string? altinnTaskType = instanceEvent.ProcessInfo?.CurrentTask?.AltinnTaskType;

            WorkflowCommandSet? workflowCommands = GetWorkflowStepsForInstanceEvent(
                instanceEventType,
                altinnTaskType,
                isInitialTaskStart
            );
            if (workflowCommands != null)
            {
                commands.AddRange(workflowCommands.Commands);
                commands.Add(CreateUpdateProcessStateCommand(processStateChange));
                commands.AddRange(workflowCommands.PostProcessNextCommittedCommands);
            }
        }

        return new Altinn.App.ProcessEngine.Models.ProcessNextRequest
        {
            CurrentElementId = processStateChange.OldProcessState?.CurrentTask?.ElementId ?? string.Empty,
            DesiredElementId = processStateChange.NewProcessState?.CurrentTask?.ElementId ?? string.Empty,
            Actor = await ExtractActor(),
            Tasks = commands,
        };
    }

    private WorkflowCommandSet? GetWorkflowStepsForInstanceEvent(
        InstanceEventType eventType,
        string? altinnTaskType,
        bool isInitialTaskStart
    )
    {
        return eventType switch
        {
            InstanceEventType.process_StartEvent => null, // No commands for process start event itself
            InstanceEventType.process_StartTask => WorkflowCommandSet.GetTaskStartSteps(
                GetServiceTaskType(altinnTaskType),
                isInitialTaskStart
            ),
            InstanceEventType.process_EndTask => WorkflowCommandSet.GetTaskEndSteps(),
            InstanceEventType.process_AbandonTask => WorkflowCommandSet.GetTaskAbandonSteps(),
            InstanceEventType.process_EndEvent => WorkflowCommandSet.GetProcessEndSteps(),
            _ => null,
        };
    }

    private string? GetServiceTaskType(string? altinnTaskType)
    {
        if (altinnTaskType is null)
            return null;

        IEnumerable<IServiceTask> serviceTasks = _appImplementationFactory.GetAll<IServiceTask>();
        bool isServiceTask = serviceTasks.Any(x => x.Type.Equals(altinnTaskType, StringComparison.OrdinalIgnoreCase));
        return isServiceTask ? altinnTaskType : null;
    }

    private async Task<ProcessEngineActor> ExtractActor()
    {
        Authenticated currentAuth = _authenticationContext.Current;
        string userIdOrOrgNumber = currentAuth switch
        {
            Authenticated.User user => user.UserId.ToString(CultureInfo.InvariantCulture),
            Authenticated.Org org => org.OrgNo,
            Authenticated.ServiceOwner serviceOwner => serviceOwner.OrgNo,
            Authenticated.SystemUser systemUser => systemUser.SystemUserOrgNr.Get(OrganisationNumberFormat.Local),
            _ => throw new InvalidOperationException($"Unknown authentication type: {currentAuth.GetType().Name}"),
        };

        string? language = await currentAuth.GetLanguage();

        return new ProcessEngineActor { UserIdOrOrgNumber = userIdOrOrgNumber, Language = language };
    }

    private static ProcessEngineCommandRequest CreateUpdateProcessStateCommand(ProcessStateChange processStateChange)
    {
        var payload = new UpdateProcessStatePayload(processStateChange);
        string? serializedPayload = CommandPayloadSerializer.Serialize(payload);
        return new ProcessEngineCommandRequest
        {
            Command = new ProcessEngineCommand.AppCommand(
                CommandKey: UpdateProcessStateInStorage.Key,
                Payload: serializedPayload
            ),
        };
    }
}
