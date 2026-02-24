using System.Globalization;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Auth;
using Altinn.App.Core.Features.Process;
using Altinn.App.Core.Internal.WorkflowEngine.Commands;
using Altinn.App.Core.Internal.WorkflowEngine.Models;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Process;
using Altinn.Platform.Storage.Interface.Enums;
using Altinn.Platform.Storage.Interface.Models;
using ProcessNextRequest = Altinn.App.Core.Internal.WorkflowEngine.Models.ProcessNextRequest;

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
    public async Task<WorkflowEngine.Models.ProcessNextRequest> Create(
        ProcessStateChange processStateChange,
        string lockToken,
        string state,
        Dictionary<string, string>? prefill = null
    )
    {
        var taskEndSteps = new List<StepRequest>();
        var taskStartSteps = new List<StepRequest>();
        var postCommitSteps = new List<StepRequest>();

        bool isInitialTaskStart = processStateChange.OldProcessState?.CurrentTask is null;

        foreach (InstanceEvent instanceEvent in processStateChange.Events ?? [])
        {
            if (!Enum.TryParse(instanceEvent.EventType, true, out InstanceEventType instanceEventType))
                continue;

            string? altinnTaskType = instanceEvent.ProcessInfo?.CurrentTask?.AltinnTaskType;

            WorkflowCommandSet? workflowCommands = GetWorkflowStepsForInstanceEvent(
                instanceEventType,
                altinnTaskType,
                isInitialTaskStart,
                prefill
            );
            if (workflowCommands != null)
            {
                // Task-end/abandon commands go in the first group (they need OLD CurrentTask).
                // Task-start and process-end commands go in the second group (they need NEW CurrentTask).
                // AdvanceProcessState is inserted between the two groups to transition in-memory state.
                if (instanceEventType is InstanceEventType.process_EndTask or InstanceEventType.process_AbandonTask)
                {
                    taskEndSteps.AddRange(workflowCommands.Commands);
                }
                else
                {
                    taskStartSteps.AddRange(workflowCommands.Commands);
                }

                postCommitSteps.AddRange(workflowCommands.PostProcessNextCommittedCommands);
            }
        }

        var commands = new List<StepRequest>();
        commands.AddRange(taskEndSteps);
        if (taskEndSteps.Count > 0)
        {
            commands.Add(CreateAdvanceProcessStateCommand(processStateChange));
        }
        commands.AddRange(taskStartSteps);
        commands.Add(CreateUpdateProcessStateCommand(processStateChange));
        commands.AddRange(postCommitSteps);

        return new ProcessNextRequest
        {
            CurrentElementId = processStateChange.OldProcessState?.CurrentTask?.ElementId ?? string.Empty,
            DesiredElementId = processStateChange.NewProcessState?.CurrentTask?.ElementId ?? string.Empty,
            Actor = await ExtractActor(),
            Steps = commands,
            LockToken = lockToken,
            State = state,
        };
    }

    private WorkflowCommandSet? GetWorkflowStepsForInstanceEvent(
        InstanceEventType eventType,
        string? altinnTaskType,
        bool isInitialTaskStart,
        Dictionary<string, string>? prefill
    )
    {
        return eventType switch
        {
            InstanceEventType.process_StartEvent => null, // No commands for process start event itself
            InstanceEventType.process_StartTask => WorkflowCommandSet.GetTaskStartSteps(
                GetServiceTaskType(altinnTaskType),
                isInitialTaskStart,
                isInitialTaskStart ? prefill : null // Only pass prefill for initial task start
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

    private async Task<Actor> ExtractActor()
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

        return new Actor { UserIdOrOrgNumber = userIdOrOrgNumber, Language = language };
    }

    private static StepRequest CreateAdvanceProcessStateCommand(ProcessStateChange processStateChange)
    {
        var payload = new UpdateProcessStatePayload(processStateChange);
        string? serializedPayload = CommandPayloadSerializer.Serialize(payload);
        return new StepRequest
        {
            Command = new Command.AppCommand(CommandKey: AdvanceProcessState.Key, Payload: serializedPayload),
        };
    }

    private static StepRequest CreateUpdateProcessStateCommand(ProcessStateChange processStateChange)
    {
        var payload = new UpdateProcessStatePayload(processStateChange);
        string? serializedPayload = CommandPayloadSerializer.Serialize(payload);
        return new StepRequest
        {
            Command = new Command.AppCommand(CommandKey: UpdateProcessStateInStorage.Key, Payload: serializedPayload),
        };
    }
}
