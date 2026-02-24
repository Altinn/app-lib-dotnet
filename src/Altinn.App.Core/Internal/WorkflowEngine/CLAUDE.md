# Workflow Engine Integration Layer

App-lib integration with the async Workflow Engine service. The engine runs as a separate service; this code handles **sending requests** and **receiving callbacks**.

## Architecture

The Workflow Engine service (external, .NET, PostgreSQL-backed) orchestrates process transitions. This integration layer:

1. **Outbound**: `ProcessNextRequestFactory` builds a `ProcessNextRequest` (command sequence + actor + lock token) and `WorkflowEngineClient` POSTs it to the engine's `/next` endpoint
2. **Inbound**: The engine calls back to `WorkflowEngineCallbackController` for each command, one at a time, sequentially
3. **Per-callback lifecycle**: Controller fetches Instance from Storage, initializes `InstanceDataUnitOfWork`, resolves the `IWorkflowEngineCommand` by key, executes it, and commits data changes on success

```
App ProcessNext API
  → ProcessNextRequestFactory.Create()     (builds command list from ProcessStateChange)
  → WorkflowEngineClient.ProcessNext()     (HTTP POST to engine)

Engine (external service)
  → Executes steps sequentially
  → For each AppCommand: POST to /workflow-engine-callbacks/{commandKey}

WorkflowEngineCallbackController.ExecuteCommand()
  → Resolve IWorkflowEngineCommand by key
  → Init InstanceDataUnitOfWork
  → command.Execute(context)
  → Save data changes on success, return error on failure
```

## Key Design Constraints

- **ALL commands MUST be idempotent** - the engine retries failed commands with configurable backoff
- **Commands run in separate HTTP requests** - each callback is independent, no shared in-memory state between commands
- **Pre-commit vs post-commit**: `WorkflowCommandSet` separates `Commands` (run before `UpdateProcessState`) from `PostProcessNextCommittedCommands` (run after state is persisted)
- **Authentication**: Callbacks use `[AllowAnonymous]` currently (TODO: X-Api-Key scheme). Data operations use `StorageAuthenticationMethod.ServiceOwner()`

## File Structure

```
WorkflowEngine/
├── CLAUDE.md
├── README.md
├── Commands/
│   ├── _Base/
│   │   ├── IWorkflowEngineCommand.cs        - Command interface (plain + generic with payload)
│   │   ├── WorkflowEngineCommandBase<T>.cs  - Base class for typed-payload commands
│   │   ├── ProcessEngineCommandContext.cs   - Context struct (AppId, InstanceId, Mutator, Payload, CT)
│   │   ├── ProcessEngineCommandResult.cs    - Success/Failed result types
│   │   ├── CommandPayload.cs                - Polymorphic JSON payload base + serializer + source gen context
│   │   └── ProcessTaskResolver.cs           - Resolves IProcessTask/IServiceTask by AltinnTaskType
│   ├── ProcessNext/
│   │   ├── TaskStart/
│   │   │   ├── UnlockTaskData.cs            - Unlock data elements for new task
│   │   │   ├── WorkflowTaskStartLegacyHook  - Runs legacy IProcessTaskStart (obsolete API)
│   │   │   ├── OnTaskStartingHook.cs        - Runs IOnTaskStartingHandler (new API, max 1 per task)
│   │   │   ├── CommonTaskInitialization.cs   - Auto-create data elements, prefill, remove task-generated data
│   │   │   └── ProcessTaskStart.cs          - Calls IProcessTask.Start()
│   │   ├── TaskEnd/
│   │   │   ├── ProcessTaskEnd.cs            - Calls IProcessTask.End()
│   │   │   ├── CommonTaskFinalization.cs    - Remove hidden data, shadow fields, AltinnRowIds
│   │   │   ├── EndTaskLegacyHook.cs         - Runs legacy IProcessTaskEnd (obsolete API)
│   │   │   ├── OnTaskEndingHook.cs          - Runs IOnTaskEndingHandler (new API, max 1 per task)
│   │   │   └── LockTaskData.cs              - Lock data elements after task completes
│   │   ├── TaskAbandon/
│   │   │   ├── ProcessTaskAbandon.cs
│   │   │   ├── OnTaskEndingHook.cs          - (reused from TaskEnd namespace - runs IOnTaskAbandonHandler)
│   │   │   └── AbandonTaskLegacyHook.cs
│   │   └── ProcessEnd/
│   │       ├── OnWorkflowEndingHook.cs      - Runs IOnProcessEndingHandler (pre-commit)
│   │       ├── ProcessEndLegacyHook.cs      - Runs legacy IProcessEnd (post-commit)
│   │       ├── DeleteDataElements.cs        - Auto-delete data types (not in command sequences yet)
│   │       └── DeleteInstance.cs            - Hard-delete instance (not in command sequences yet)
│   ├── AltinnEvents/
│   │   ├── MovedToAltinnEvent.cs            - Fires movedTo.{taskId} event (post-commit)
│   │   ├── CompletedAltinnEvent.cs          - Fires process.completed event (post-commit)
│   │   └── InstanceCreatedAltinnEvent.cs    - Fires instance.created event (post-commit, first task only)
│   ├── ExecuteServiceTask.cs                - Runs IServiceTask.Execute() (post-commit)
│   └── UpdateProcessStateInStorage.cs       - Commits ProcessStateChange to Storage (the commit boundary)
├── DependencyInjection/
│   ├── ServiceCollectionExtensions.cs       - Registers all commands + client + helpers
│   └── WorkflowEngineCommandValidator.cs    - Startup check: all keys in WorkflowCommandSet are registered
├── Http/
│   ├── IWorkflowEngineClient.cs             - ProcessNext() and GetActiveJobStatus()
│   └── WorkflowEngineClient.cs              - HTTP impl with X-Api-Key auth
├── Models/
│   ├── ProcessNextRequest.cs                - Request to engine (elements, actor, lock, steps)
│   ├── StepRequest.cs                       - Single step (command + optional startTime + retryStrategy)
│   ├── Command.cs                           - Polymorphic: AppCommand | Webhook | Debug (Noop/Throw/Timeout)
│   ├── AppCallbackPayload.cs                - Payload engine sends back per callback
│   ├── Actor.cs                             - User/org identity for the request
│   ├── RetryStrategy.cs                     - Backoff config (Exponential/Linear/Constant)
│   ├── BackoffType.cs                       - Enum
│   ├── PersistentItemStatus.cs              - Enum (Enqueued/Processing/Requeued/Completed/Failed/Canceled)
│   ├── WorkflowStatusResponse.cs            - Response from engine status endpoint
│   └── CallbackErrorResponse.cs             - Error response from callback controller
├── ProcessNextRequestFactory.cs             - Maps ProcessStateChange → ProcessNextRequest
└── WorkflowCommandSet.cs                    - Defines command sequences per event type
```

## Command Sequences

Defined in `WorkflowCommandSet.cs`. `UpdateProcessState` is always inserted between main commands and post-commit commands by `ProcessNextRequestFactory`.

### Task Start
```
UnlockTaskData → ProcessTaskStart(legacy) → OnTaskStartingHook → CommonTaskInitialization → ProcessTaskStart
  ── UpdateProcessState ──
MovedToAltinnEvent → [ExecuteServiceTask if service task] → [InstanceCreatedAltinnEvent if first task]
```

### Task End
```
ProcessTaskEnd → CommonTaskFinalization → EndTaskLegacyHook → OnTaskEndingHook → LockTaskData
  ── UpdateProcessState ──
(no post-commit commands)
```

### Task Abandon
```
ProcessTaskAbandon → OnTaskAbandonHook → AbandonTaskLegacyHook
  ── UpdateProcessState ──
(no post-commit commands)
```

### Process End
```
OnWorkflowEndingHook
  ── UpdateProcessState ──
ProcessEndLegacyHook → CompletedAltinnEvent
```

## How to Add a New Command

1. **Create the command class** in the appropriate `Commands/` subfolder:
   - Without payload: implement `IWorkflowEngineCommand` directly
   - With typed payload: extend `WorkflowEngineCommandBase<TPayload>` and create a `record TPayload : CommandRequestPayload`

2. **If using a payload**: register it in `CommandPayload.cs`:
   - Add `[JsonDerivedType(typeof(MyPayload), typeDiscriminator: "myPayload")]` to `CommandRequestPayload`
   - Add `[JsonSerializable(typeof(MyPayload))]` to `CommandPayloadJsonContext`

3. **Register in DI**: add `services.AddTransient<IWorkflowEngineCommand, MyCommand>()` in `ServiceCollectionExtensions.cs`

4. **Add to sequence**: add to the appropriate method in `WorkflowCommandSet.cs` (use `AddCommand` for pre-commit, `AddPostProcessNextCommittedCommand` for post-commit)

5. **Startup validation**: `WorkflowEngineCommandValidator` will fail at startup if a key in `WorkflowCommandSet` isn't registered in DI

## Command Conventions

- Every command has `public static string Key => "..."` and `public string GetKey() => Key`
- Commands return `SuccessfulProcessEngineCommandResult` or `FailedProcessEngineCommandResult` (never throw from Execute)
- Commands get instance data through `context.InstanceDataMutator` (an `InstanceDataUnitOfWork`)
- The callback controller saves data changes after successful execution - commands don't need to persist data themselves (except `UpdateProcessStateInStorage` which writes to the process/events API)
- Hook commands (OnTaskStarting/Ending, OnProcessEnding) enforce max 1 handler per task

## Interaction with Workflow Engine Service

The engine service (separate repo at `altinn-studio/src/Runtime/workflow-engine`):
- .NET service backed by PostgreSQL
- Receives `ProcessNextRequest`, stores it, executes steps sequentially
- Calls back to the app via HTTP POST for each `AppCommand`
- Retries failed steps with configurable backoff (default: exponential, 1s base, 5min max delay, 24h max duration)
- One active workflow per instance at a time
- Steps execute in order; previous step must complete before next begins
- Lock token is passed through for idempotency/caching scoping

## Known TODOs / In-Progress

- Authentication on callback controller: currently `[AllowAnonymous]`, should use X-Api-Key scheme
- `DeleteDataElements` and `DeleteInstance` exist but aren't wired into command sequences
- `Actor.UserIdOrOrgNumber` could use a more specific type
- `AppCallbackPayload.LockToken` naming inconsistency with engine (LockKey vs LockToken)
- "Go to next task after service task" - automatic progression not yet reimplemented
