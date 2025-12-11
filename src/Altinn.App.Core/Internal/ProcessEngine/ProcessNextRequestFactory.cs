using System.Globalization;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Auth;
using Altinn.App.Core.Features.Process;
using Altinn.App.Core.Internal.ProcessEngine.Commands;
using Altinn.App.Core.Models.Process;
using Altinn.App.ProcessEngine.Models;
using Altinn.Platform.Storage.Interface.Enums;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.ProcessEngine;

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
    public async Task<Altinn.App.ProcessEngine.Models.ProcessNextRequest> Create(
        Instance instance,
        ProcessStateChange processStateChange
    )
    {
        if (processStateChange.Events == null || processStateChange.Events.Count == 0)
        {
            return new Altinn.App.ProcessEngine.Models.ProcessNextRequest
            {
                CurrentElementId = processStateChange.OldProcessState?.CurrentTask?.ElementId ?? string.Empty,
                DesiredElementId = processStateChange.NewProcessState?.CurrentTask?.ElementId ?? string.Empty,
                InstanceInformation = ExtractInstanceInformation(instance),
                Actor = await ExtractActor(),
                Tasks = [],
            };
        }

        var sequence = new ProcessCommandSequence(processStateChange);

        foreach (InstanceEvent instanceEvent in processStateChange.Events)
        {
            if (!Enum.TryParse(instanceEvent.EventType, true, out InstanceEventType eventType))
                continue;

            string? altinnTaskType = instanceEvent.ProcessInfo?.CurrentTask?.AltinnTaskType;

            ProcessEventCommandGroup? commandGroup = GetCommandGroupForEvent(eventType);
            if (commandGroup != null)
            {
                sequence.AddEventCommandGroup(commandGroup);

                // Add service task execution if this is a StartTask event for a service task
                if (eventType == InstanceEventType.process_StartTask && IsServiceTask(altinnTaskType))
                {
                    sequence.AddCommand(CreateCommand(ExecuteServiceTask.Key));
                }
            }
        }

        return new Altinn.App.ProcessEngine.Models.ProcessNextRequest
        {
            CurrentElementId = processStateChange.OldProcessState?.CurrentTask?.ElementId ?? string.Empty,
            DesiredElementId = processStateChange.NewProcessState?.CurrentTask?.ElementId ?? string.Empty,
            InstanceInformation = ExtractInstanceInformation(instance),
            Actor = await ExtractActor(),
            Tasks = sequence.ToList(),
        };
    }

    private static ProcessEventCommandGroup? GetCommandGroupForEvent(InstanceEventType eventType)
    {
        return eventType switch
        {
            InstanceEventType.process_StartTask => ProcessEventCommandGroup.ForTaskStart(),
            InstanceEventType.process_EndTask => ProcessEventCommandGroup.ForTaskEnd(),
            InstanceEventType.process_AbandonTask => ProcessEventCommandGroup.ForTaskAbandon(),
            InstanceEventType.process_EndEvent => ProcessEventCommandGroup.ForProcessEnd(),
            _ => null,
        };
    }

    private bool IsServiceTask(string? altinnTaskType)
    {
        if (altinnTaskType is null)
            return false;

        IEnumerable<IServiceTask> serviceTasks = _appImplementationFactory.GetAll<IServiceTask>();
        return serviceTasks.Any(x => x.Type.Equals(altinnTaskType, StringComparison.OrdinalIgnoreCase));
    }

    private static InstanceInformation ExtractInstanceInformation(Instance instance)
    {
        string[] appIdParts = instance.AppId.Split("/");
        string[] instanceIdParts = instance.Id.Split("/");

        return new InstanceInformation
        {
            Org = instance.Org,
            App = appIdParts[1],
            InstanceOwnerPartyId = int.Parse(instance.InstanceOwner.PartyId, CultureInfo.InvariantCulture),
            InstanceGuid = Guid.Parse(instanceIdParts[1]),
        };
    }

    private async Task<ProcessEngineActor> ExtractActor()
    {
        Authenticated currentAuth = _authenticationContext.Current;
        string userIdOrOrgNumber = currentAuth switch
        {
            Authenticated.User user => user.UserId.ToString(CultureInfo.InvariantCulture),
            Authenticated.Org org => org.OrgNo,
            Authenticated.ServiceOwner serviceOwner => serviceOwner.OrgNo,
            _ => throw new InvalidOperationException($"Unknown authentication type: {currentAuth.GetType().Name}"),
        };

        string? language = await currentAuth.GetLanguage();

        return new ProcessEngineActor { UserIdOrOrgNumber = userIdOrOrgNumber, Language = language };
    }

    private static ProcessEngineCommandRequest CreateCommand(string commandKey, string? metadata = null)
    {
        return new ProcessEngineCommandRequest
        {
            Command = new ProcessEngineCommand.AppCommand(CommandKey: commandKey, Metadata: metadata),
        };
    }
}
