using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Altinn.App.Core.Extensions;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Action;
using Altinn.App.Core.Features.Auth;
using Altinn.App.Core.Features.Process;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.InstanceLocking;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Process.Elements.Base;
using Altinn.App.Core.Internal.Validation;
using Altinn.App.Core.Internal.WorkflowEngine;
using Altinn.App.Core.Internal.WorkflowEngine.Http;
using Altinn.App.Core.Internal.WorkflowEngine.Models;
using Altinn.App.Core.Internal.WorkflowEngine.Models.AppCommand;
using Altinn.App.Core.Internal.WorkflowEngine.Models.Engine;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Process;
using Altinn.App.Core.Models.UserAction;
using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Enums;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProcessNextRequest = Altinn.App.Core.Models.Process.ProcessNextRequest;
using WorkflowEnqueueRequest = Altinn.App.Core.Internal.WorkflowEngine.Models.Engine.WorkflowEnqueueRequest;

namespace Altinn.App.Core.Internal.Process;

/// <summary>
/// Default implementation of the <see cref="IProcessEngine"/>
/// </summary>
internal class ProcessEngine : IProcessEngine
{
    private const int MaxNextIterationsAllowed = 100;

    private readonly IProcessReader _processReader;
    private readonly IProcessNavigator _processNavigator;
    private readonly UserActionService _userActionService;
    private readonly Telemetry? _telemetry;
    private readonly IAuthenticationContext _authenticationContext;
    private readonly InstanceDataUnitOfWorkInitializer _instanceDataUnitOfWorkInitializer;
    private readonly AppImplementationFactory _appImplementationFactory;
    private readonly IProcessEngineAuthorizer _processEngineAuthorizer;
    private readonly ILogger<ProcessEngine> _logger;
    private readonly IValidationService _validationService;
    private readonly ProcessNextRequestFactory _processNextRequestFactory;
    private readonly InstanceStateService _instanceStateService;
    private readonly IWorkflowEngineClient _workflowEngineClient;
    private readonly IInstanceClient _instanceClient;
    private readonly AppIdentifier _appIdentifier;
    private readonly IInstanceLocker _instanceLocker;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessEngine"/> class.
    /// </summary>
    public ProcessEngine(
        IProcessReader processReader,
        IProcessNavigator processNavigator,
        UserActionService userActionService,
        IAuthenticationContext authenticationContext,
        IServiceProvider serviceProvider,
        IProcessEngineAuthorizer processEngineAuthorizer,
        IValidationService validationService,
        IWorkflowEngineClient workflowEngineClient,
        ILogger<ProcessEngine> logger,
        Telemetry? telemetry = null
    )
    {
        _processReader = processReader;
        _processNavigator = processNavigator;
        _userActionService = userActionService;
        _telemetry = telemetry;
        _authenticationContext = authenticationContext;
        _processEngineAuthorizer = processEngineAuthorizer;
        _validationService = validationService;
        _logger = logger;
        _workflowEngineClient = workflowEngineClient;
        _processNextRequestFactory = serviceProvider.GetRequiredService<ProcessNextRequestFactory>();
        _instanceStateService = serviceProvider.GetRequiredService<InstanceStateService>();
        _appImplementationFactory = serviceProvider.GetRequiredService<AppImplementationFactory>();
        _instanceDataUnitOfWorkInitializer = serviceProvider.GetRequiredService<InstanceDataUnitOfWorkInitializer>();
        _instanceClient = serviceProvider.GetRequiredService<IInstanceClient>();
        _appIdentifier = serviceProvider.GetRequiredService<AppIdentifier>();
        _instanceLocker = serviceProvider.GetRequiredService<IInstanceLocker>();
    }

    /// <inheritdoc/>
    public async Task<ProcessChangeResult> CreateInitialProcessState(ProcessStartRequest processStartRequest)
    {
        using var activity = _telemetry?.StartProcessStartActivity(processStartRequest.Instance);

        if (processStartRequest.Instance.Process != null)
        {
            var result = new ProcessChangeResult()
            {
                Success = false,
                ErrorMessage = "Process is already started. Use next.",
                ErrorType = ProcessErrorType.Conflict,
            };
            activity?.SetProcessChangeResult(result);
            return result;
        }

        string validStartElement;
        try
        {
            validStartElement = ProcessHelper.GetValidStartEventOrError(
                processStartRequest.StartEventId,
                _processReader.GetStartEventIds()
            );
        }
        catch (ProcessException e)
        {
            var result = new ProcessChangeResult()
            {
                Success = false,
                ErrorMessage = e.Message,
                ErrorType = ProcessErrorType.Conflict,
            };
            activity?.SetProcessChangeResult(result);
            return result;
        }

        // start process
        ProcessStateChange? startChange = await ProcessStart(processStartRequest.Instance, validStartElement);
        InstanceEvent? startEvent = startChange?.Events?[0].CopyValues();
        ProcessStateChange? nextChange = await MoveProcessStateToNextAndGenerateEvents(processStartRequest.Instance);
        InstanceEvent? goToNextEvent = nextChange?.Events?[0].CopyValues();
        List<InstanceEvent> events = [];
        if (startEvent is not null)
        {
            events.Add(startEvent);
        }

        if (goToNextEvent is not null)
        {
            events.Add(goToNextEvent);
        }

        ProcessStateChange processStateChange = new()
        {
            OldProcessState = startChange?.OldProcessState,
            NewProcessState = nextChange?.NewProcessState,
            Events = events,
        };

        _telemetry?.ProcessStarted();

        var changeResult = new ProcessChangeResult() { Success = true, ProcessStateChange = processStateChange };
        activity?.SetProcessChangeResult(changeResult);
        return changeResult;
    }

    /// <inheritdoc/>
    public async Task<Instance> SubmitInitialProcessState(
        Instance instance,
        ProcessStateChange processStateChange,
        string lockToken,
        Dictionary<string, string>? prefill = null,
        CancellationToken ct = default
    )
    {
        // Capture instance + form data state for transport to the workflow engine
        string? taskId = instance.Process?.CurrentTask?.ElementId;
        var unitOfWork = await _instanceDataUnitOfWorkInitializer.Init(
            instance,
            taskId,
            language: null,
            StorageAuthenticationMethod.ServiceOwner()
        );
        string state = await _instanceStateService.CaptureState(unitOfWork);

        await CreateAndEnqueueWorkflow(instance, processStateChange, lockToken, state, prefill: prefill, ct: ct);
        return await WaitForWorkflowsAndRefetchInstance(instance, ct);
    }

    /// <inheritdoc/>
    public async Task<ProcessChangeResult> Next(ProcessNextRequest request, CancellationToken ct = default)
    {
        using Activity? activity = _telemetry?.StartProcessNextActivity(request.Instance, request.Action);

        Instance instance = request.Instance;

        if (
            !TryGetCurrentTaskIdAndAltinnTaskType(
                instance,
                out CurrentTaskIdAndAltinnTaskType? currentTaskIdAndAltinnTaskType,
                out ProcessChangeResult? invalidProcessStateError
            )
        )
        {
            activity?.SetProcessChangeResult(invalidProcessStateError);
            return invalidProcessStateError;
        }

        (string currentTaskId, string altinnTaskType) = currentTaskIdAndAltinnTaskType;

        bool authorized = await _processEngineAuthorizer.AuthorizeProcessNext(instance, request.Action);

        if (!authorized)
        {
            var result = new ProcessChangeResult
            {
                Success = false,
                ErrorType = ProcessErrorType.Unauthorized,
                ErrorMessage =
                    $"User is not authorized to perform process next. Task ID: {LogSanitizer.Sanitize(currentTaskId)}. Task type: {LogSanitizer.Sanitize(altinnTaskType)}. Action: {LogSanitizer.Sanitize(request.Action ?? "none")}.",
            };
            activity?.SetProcessChangeResult(result);
            return result;
        }

        await _instanceLocker.LockAsync();

        _logger.LogDebug(
            "User successfully authorized to perform process next. Task ID: {CurrentTaskId}. Task type: {AltinnTaskType}. Action: {ProcessNextAction}.",
            LogSanitizer.Sanitize(currentTaskId),
            LogSanitizer.Sanitize(altinnTaskType),
            LogSanitizer.Sanitize(request.Action ?? "none")
        );

        string checkedAction = request.Action ?? ConvertTaskTypeToAction(altinnTaskType);
        bool isServiceTask = CheckIfServiceTask(altinnTaskType) is not null;
        string? processNextAction = request.Action;

        // If the action is 'reject', we should not run any service task and there is no need to check for a user action handler, since 'reject' doesn't have one.
        if (request.Action is not "reject")
        {
            if (request.Action is not null)
            {
                UserActionResult userActionResult = await HandleUserAction(instance, request, ct);

                if (userActionResult.ResultType is ResultType.Failure)
                {
                    var result = new ProcessChangeResult()
                    {
                        Success = false,
                        ErrorMessage = $"Action handler for action {LogSanitizer.Sanitize(request.Action)} failed!",
                        ErrorType = userActionResult.ErrorType,
                    };
                    activity?.SetProcessChangeResult(result);
                    return result;
                }
            }
        }

        // If the action is 'reject' the task is being abandoned, and we should skip validation, but only if reject has been allowed for the task in bpmn.
        if (checkedAction == "reject" && _processReader.IsActionAllowedForTask(currentTaskId, checkedAction))
        {
            _logger.LogInformation(
                "Skipping validation during process next because the action is 'reject' and the task is being abandoned."
            );
        }
        else if (isServiceTask)
        {
            _logger.LogInformation("Skipping validation during process next because the task is a service task.");
        }
        else
        {
            InstanceDataUnitOfWork dataAccessor = await _instanceDataUnitOfWorkInitializer.Init(
                instance,
                currentTaskId,
                request.Language
            );

            List<ValidationIssueWithSource> validationIssues = await _validationService.ValidateInstanceAtTask(
                dataAccessor,
                currentTaskId, // run full validation
                ignoredValidators: null,
                onlyIncrementalValidators: null,
                language: request.Language
            );

            int errorCount = validationIssues.Count(v => v.Severity == ValidationIssueSeverity.Error);

            if (errorCount > 0)
            {
                var result = new ProcessChangeResult
                {
                    Success = false,
                    ErrorType = ProcessErrorType.Conflict,
                    ErrorTitle = "Validation failed for task",
                    ErrorMessage = $"{errorCount} validation errors found for task {currentTaskId}",
                    ValidationIssues = validationIssues,
                };
                activity?.SetProcessChangeResult(result);
                return result;
            }
        }

        MoveToNextResult moveToNextResult;
        try
        {
            moveToNextResult = await HandleMoveToNext(instance, processNextAction, request.LockToken, ct);
        }
        catch (ServiceTaskFailedException ex)
        {
            // The process state was committed to Storage (post-commit), but the service task failed.
            // Return an error result so the caller knows the service task did not succeed.
            var failureResult = new ProcessChangeResult(mutatedInstance: instance)
            {
                Success = false,
                ErrorType = ProcessErrorType.Internal,
                ErrorTitle = "Service task failed!",
                ErrorMessage = ex.Message,
            };
            activity?.SetProcessChangeResult(failureResult);
            return failureResult;
        }

        var changeResult = new ProcessChangeResult(mutatedInstance: moveToNextResult.Instance)
        {
            Success = true,
            ProcessStateChange = moveToNextResult.ProcessStateChange,
        };

        activity?.SetProcessChangeResult(changeResult);
        return changeResult;
    }

    private async Task<UserActionResult> HandleUserAction(
        Instance instance,
        ProcessNextRequest request,
        CancellationToken ct
    )
    {
        Authenticated currentAuth = _authenticationContext.Current;
        IUserAction? actionHandler = _userActionService.GetActionHandler(request.Action);

        if (actionHandler is null)
            return UserActionResult.SuccessResult();

        InstanceDataUnitOfWork cachedDataMutator = await _instanceDataUnitOfWorkInitializer.Init(
            instance,
            taskId: null,
            request.Language
        );

        int? userId = currentAuth switch
        {
            Authenticated.User auth => auth.UserId,
            _ => null,
        };

        UserActionResult actionResult = await actionHandler.HandleAction(
            new UserActionContext(
                cachedDataMutator,
                userId,
                language: request.Language,
                authentication: currentAuth,
                onBehalfOf: request.ActionOnBehalfOf,
                cancellationToken: ct
            )
        );

        if (actionResult.ResultType == ResultType.Failure)
        {
            return actionResult;
        }

        if (cachedDataMutator.HasAbandonIssues)
        {
            throw new Exception(
                "Abandon issues found in data elements. Abandon issues should be handled by the action handler."
            );
        }

        DataElementChanges changes = cachedDataMutator.GetDataElementChanges(initializeAltinnRowId: false);
        await cachedDataMutator.UpdateInstanceData(changes);
        await cachedDataMutator.SaveChanges(changes);

        return actionResult;
    }

    /// <summary>
    /// Does not save process. Instance object is updated.
    /// </summary>
    private async Task<ProcessStateChange?> ProcessStart(Instance instance, string startEvent)
    {
        if (instance.Process != null)
        {
            return null;
        }

        DateTime now = DateTime.UtcNow;
        ProcessState startState = new()
        {
            Started = now,
            StartEvent = startEvent,
            CurrentTask = new ProcessElementInfo { Flow = 1, ElementId = startEvent },
        };

        instance.Process = startState;

        PlatformUser user = await ExtractPlatformUser();
        List<InstanceEvent> events =
        [
            CreateInstanceEvent(InstanceEventType.process_StartEvent.ToString(), instance, startState, user, now),
        ];

        // ! TODO: should probably improve nullability handling in the next major version
        return new ProcessStateChange
        {
            OldProcessState = null!,
            NewProcessState = startState,
            Events = events,
        };
    }

    /// <summary>
    /// Computes the next transition and updates instance.Process to reflect the new state.
    /// </summary>
    private async Task<ProcessStateChange?> MoveProcessStateToNextAndGenerateEvents(
        Instance instance,
        string? action = null
    )
    {
        if (instance.Process == null)
        {
            return null;
        }

        PlatformUser user = await ExtractPlatformUser();
        ProcessStateChange result = await ComputeNextTransition(instance, action, user);

        // Apply the mutation so callers see the updated process state on the instance
        instance.Process = result.NewProcessState;

        return result;
    }

    /// <summary>
    /// Core BPMN transition logic. Computes the ProcessStateChange for moving from the current task
    /// to the next element. Does NOT mutate instance.Process.
    /// Used by both the normal process-next flow and auto-advance.
    /// </summary>
    private async Task<ProcessStateChange> ComputeNextTransition(Instance instance, string? action, PlatformUser user)
    {
        ProcessState process = instance.Process ?? throw new ProcessException("Process is null");
        string currentTaskId =
            process.CurrentTask?.ElementId ?? throw new ProcessException("Current task element ID is null");

        ProcessElement? nextElement = await _processNavigator.GetNextTask(instance, currentTaskId, action);
        if (nextElement is null)
            throw new ProcessException("Next process element was unexpectedly null");

        DateTime now = DateTime.UtcNow;
        var events = new List<InstanceEvent>();

        ProcessState oldProcessState = new()
        {
            Started = process.Started,
            CurrentTask = process.CurrentTask,
            StartEvent = process.StartEvent,
        };

        // End current task event
        if (_processReader.IsProcessTask(currentTaskId))
        {
            string eventType = action is "reject"
                ? InstanceEventType.process_AbandonTask.ToString()
                : InstanceEventType.process_EndTask.ToString();
            events.Add(CreateInstanceEvent(eventType, instance, oldProcessState, user, now));
        }

        // Build new process state based on next element
        ProcessState newProcessState = new() { Started = process.Started, StartEvent = process.StartEvent };
        string nextElementId = nextElement.Id;

        if (_processReader.IsEndEvent(nextElementId))
        {
            using var activity = _telemetry?.StartProcessEndActivity(instance);

            newProcessState.CurrentTask = null;
            newProcessState.Ended = now;
            newProcessState.EndEvent = nextElementId;

            events.Add(
                CreateInstanceEvent(InstanceEventType.process_EndEvent.ToString(), instance, newProcessState, user, now)
            );
            // Submit event (to support Altinn2 SBL)
            events.Add(
                CreateInstanceEvent(InstanceEventType.Submited.ToString(), instance, newProcessState, user, now)
            );
        }
        else if (_processReader.IsProcessTask(nextElementId))
        {
            var task = nextElement as ProcessTask;
            newProcessState.CurrentTask = new ProcessElementInfo
            {
                Flow = (process.CurrentTask?.Flow ?? 0) + 1,
                ElementId = nextElementId,
                Name = nextElement.Name,
                Started = now,
                AltinnTaskType = task?.ExtensionElements?.TaskExtension?.TaskType,
                FlowType = action is "reject"
                    ? ProcessSequenceFlowType.AbandonCurrentMoveToNext.ToString()
                    : ProcessSequenceFlowType.CompleteCurrentMoveToNext.ToString(),
            };

            events.Add(
                CreateInstanceEvent(
                    InstanceEventType.process_StartTask.ToString(),
                    instance,
                    newProcessState,
                    user,
                    now
                )
            );
        }

        return new ProcessStateChange
        {
            OldProcessState = oldProcessState,
            NewProcessState = newProcessState,
            Events = events,
        };
    }

    private async Task<PlatformUser> ExtractPlatformUser()
    {
        var currentAuth = _authenticationContext.Current;
        return currentAuth switch
        {
            Authenticated.User auth => new PlatformUser
            {
                UserId = auth.UserId,
                AuthenticationLevel = auth.AuthenticationLevel,
                NationalIdentityNumber = (await auth.LoadDetails(validateSelectedParty: true)).Profile.Party.SSN,
            },
            Authenticated.Org => new PlatformUser { }, // TODO: what do we do here?
            Authenticated.ServiceOwner auth => new PlatformUser
            {
                OrgId = auth.Name,
                AuthenticationLevel = auth.AuthenticationLevel,
            },
            Authenticated.SystemUser auth => new PlatformUser
            {
                SystemUserId = auth.SystemUserId[0],
                SystemUserOwnerOrgNo = auth.SystemUserOrgNr.Get(Models.OrganisationNumberFormat.Local),
                SystemUserName = null, // TODO: will get this name later when a lookup API is implemented or the name is passed in token
                AuthenticationLevel = auth.AuthenticationLevel,
            },
            _ => throw new Exception($"Unknown authentication context: {currentAuth.GetType().Name}"),
        };
    }

    private async Task<MoveToNextResult> HandleMoveToNext(
        Instance instance,
        string? action,
        string lockToken,
        CancellationToken ct = default
    )
    {
        // Capture state BEFORE mutation — commands that run before SaveProcessStateToStorage
        // need to see the old process state, mirroring how they'd read it from Storage.
        string? currentTaskId = instance.Process?.CurrentTask?.ElementId;
        var unitOfWork = await _instanceDataUnitOfWorkInitializer.Init(
            instance,
            currentTaskId,
            language: null,
            StorageAuthenticationMethod.ServiceOwner()
        );
        string state = await _instanceStateService.CaptureState(unitOfWork);

        ProcessStateChange? processStateChange = await MoveProcessStateToNextAndGenerateEvents(instance, action);

        if (processStateChange is null)
        {
            return new MoveToNextResult(instance, null);
        }

        await CreateAndEnqueueWorkflow(instance, processStateChange, lockToken, state, ct: ct);

        Instance freshInstance = await WaitForWorkflowsAndRefetchInstance(instance, ct);

        return new MoveToNextResult(freshInstance, processStateChange);
    }

    /// <inheritdoc/>
    public async Task EnqueueProcessNext(
        Instance instance,
        Actor actor,
        string lockToken,
        Guid dependsOnWorkflowId,
        string state,
        string? action = null,
        CancellationToken ct = default
    )
    {
        PlatformUser user = CreatePlatformUser(actor);
        ProcessStateChange processStateChange = await ComputeNextTransition(instance, action, user);

        await CreateAndEnqueueWorkflow(
            instance,
            processStateChange,
            lockToken,
            state,
            actor: actor,
            dependsOn: [WorkflowRef.FromDatabaseId(dependsOnWorkflowId)],
            ct: ct
        );
    }

    /// <summary>
    /// Creates a workflow enqueue request from a process state change and sends it to the workflow engine.
    /// Returns the database ID of the enqueued workflow.
    /// </summary>
    private async Task<Guid> CreateAndEnqueueWorkflow(
        Instance instance,
        ProcessStateChange processStateChange,
        string lockToken,
        string? state = null,
        Actor? actor = null,
        IEnumerable<WorkflowRef>? dependsOn = null,
        Dictionary<string, string>? prefill = null,
        CancellationToken ct = default
    )
    {
        WorkflowEnqueueRequest enqueueRequest = await _processNextRequestFactory.Create(
            instance,
            processStateChange,
            lockToken,
            state,
            actor: actor,
            dependsOn: dependsOn,
            prefill: prefill
        );
        WorkflowEnqueueResponse.Accepted response = await _workflowEngineClient.EnqueueWorkflows(enqueueRequest, ct);
        return response.Workflows[0].DatabaseId;
    }

    private static InstanceEvent CreateInstanceEvent(
        string eventType,
        Instance instance,
        ProcessState processInfo,
        PlatformUser user,
        DateTime now
    )
    {
        return new InstanceEvent
        {
            InstanceId = instance.Id,
            InstanceOwnerPartyId = instance.InstanceOwner.PartyId,
            EventType = eventType,
            Created = now,
            User = user,
            ProcessInfo = processInfo,
        };
    }

    private static PlatformUser CreatePlatformUser(Actor actor)
    {
        if (int.TryParse(actor.UserIdOrOrgNumber, out int userId))
        {
            return new PlatformUser { UserId = userId };
        }
        return new PlatformUser { OrgId = actor.UserIdOrOrgNumber };
    }

    private sealed record MoveToNextResult(Instance Instance, ProcessStateChange? ProcessStateChange)
    {
        [MemberNotNullWhen(true, nameof(ProcessStateChange))]
        public bool IsEndEvent => ProcessStateChange?.NewProcessState?.Ended is not null;
    };

    internal static string ConvertTaskTypeToAction(string actionOrTaskType)
    {
        switch (actionOrTaskType)
        {
            case "data":
            case "feedback":
            case "pdf":
            case "eFormidling":
            case "fiksArkiv":
                return "write";
            case "confirmation":
                return "confirm";
            case "signing":
                return "sign";
            default:
                // Not any known task type, so assume it is an action type
                return actionOrTaskType;
        }
    }

    private static bool TryGetCurrentTaskIdAndAltinnTaskType(
        Instance instance,
        [NotNullWhen(true)] out CurrentTaskIdAndAltinnTaskType? state,
        [NotNullWhen(false)] out ProcessChangeResult? error
    )
    {
        state = null; // allowed because the method may return false
        error = null;

        ProcessState? process = instance.Process;

        if (process is null)
        {
            error = new ProcessChangeResult
            {
                Success = false,
                ErrorType = ProcessErrorType.Conflict,
                ErrorMessage = "The instance is missing process information.",
            };
            return false;
        }

        if (process.Ended is not null)
        {
            error = new ProcessChangeResult
            {
                Success = false,
                ErrorType = ProcessErrorType.Conflict,
                ErrorMessage = "Process is ended.",
            };
            return false;
        }

        if (process.CurrentTask?.ElementId is not string taskId)
        {
            error = new ProcessChangeResult
            {
                Success = false,
                ErrorType = ProcessErrorType.Conflict,
                ErrorMessage = "Process is not started. Use start!",
            };
            return false;
        }

        if (process.CurrentTask.AltinnTaskType is not string taskType)
        {
            error = new ProcessChangeResult
            {
                Success = false,
                ErrorType = ProcessErrorType.Conflict,
                ErrorMessage = "Instance does not have current altinn task type information!",
            };
            return false;
        }

        state = new CurrentTaskIdAndAltinnTaskType(taskId, taskType);
        return true;
    }

    /// <summary>
    /// Polls the workflow engine until all active workflows for the instance have completed,
    /// then fetches and returns the fresh instance from Storage.
    /// </summary>
    private async Task<Instance> WaitForWorkflowsAndRefetchInstance(Instance instance, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        const int timeoutMs = 100_000;
        const int initialDelayMs = 100;
        const int maxDelayMs = 2_000;
        int currentDelayMs = initialDelayMs;

        string ns = $"{_appIdentifier.Org}/{_appIdentifier.App}";
        Guid correlationId = new InstanceIdentifier(instance).InstanceGuid;

        while (!ct.IsCancellationRequested)
        {
            IReadOnlyList<WorkflowStatusResponse> activeWorkflows = await _workflowEngineClient.ListActiveWorkflows(
                ns,
                correlationId,
                cancellationToken: ct
            );

            if (activeWorkflows.Count == 0)
            {
                break;
            }

            // Check for terminal failure states
            foreach (var workflow in activeWorkflows)
            {
                switch (workflow.OverallStatus)
                {
                    case PersistentItemStatus.Canceled:
                        throw new InvalidOperationException($"Workflow '{workflow.OperationId}' was canceled.");
                    case PersistentItemStatus.Failed:
                        throw new InvalidOperationException($"Workflow '{workflow.OperationId}' failed.");
                    case PersistentItemStatus.DependencyFailed:
                        throw new InvalidOperationException(
                            $"Workflow '{workflow.OperationId}' failed due to a dependency failure."
                        );
                }
            }

            if (stopwatch.ElapsedMilliseconds > timeoutMs)
            {
                throw new TimeoutException("Timeout while waiting for workflows to complete.");
            }

            await Task.Delay(currentDelayMs, ct);
            currentDelayMs = Math.Min(currentDelayMs * 2, maxDelayMs);
        }

        ct.ThrowIfCancellationRequested();

        return await _instanceClient.GetInstance(instance, ct: ct);
    }

    private IServiceTask? CheckIfServiceTask(string? altinnTaskType)
    {
        if (altinnTaskType is null)
            return null;

        IEnumerable<IServiceTask> serviceTasks = _appImplementationFactory.GetAll<IServiceTask>();
        IServiceTask? serviceTask = serviceTasks.FirstOrDefault(x =>
            x.Type.Equals(altinnTaskType, StringComparison.OrdinalIgnoreCase)
        );

        return serviceTask;
    }

    private sealed record CurrentTaskIdAndAltinnTaskType(string CurrentTaskId, string AltinnTaskType);
}
