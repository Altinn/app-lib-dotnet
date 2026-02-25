# Workflow Engine Integration Layer

App-lib integration with the async Workflow Engine service. The engine runs as a separate service; this code handles **sending requests** and **receiving callbacks**.

## Architecture

The Workflow Engine service (external, .NET, PostgreSQL-backed) orchestrates process transitions. This integration layer:

1. **Outbound**: `ProcessNextRequestFactory` builds a `ProcessNextRequest` (command sequence + actor + lock token + state blob) and `WorkflowEngineClient` POSTs it to the engine's `/next` endpoint
2. **Inbound**: The engine calls back to `WorkflowEngineCallbackController` for each command, one at a time, sequentially
3. **Per-callback lifecycle**: Controller restores `InstanceDataUnitOfWork` from the opaque state blob, resolves the `IWorkflowEngineCommand` by key, executes it, commits data changes on success, captures updated state, and returns it to the engine

```
App ProcessNext API
  → Capture instance + form data into opaque state blob (InstanceStateService)
  → ProcessNextRequestFactory.Create()     (builds command list from ProcessStateChange)
  → WorkflowEngineClient.ProcessNext()     (HTTP POST to engine with state blob)

Engine (external service)
  → Executes steps sequentially
  → For each AppCommand: POST to /workflow-engine-callbacks/{commandKey} (echoes state blob)

WorkflowEngineCallbackController.ExecuteCommand()
  → Restore InstanceDataUnitOfWork from state blob (InstanceStateService)
  → Resolve IWorkflowEngineCommand by key
  → command.Execute(context)
  → Save data changes on success, capture updated state blob, return to engine
  → (Engine uses returned state blob for next callback)
```

## Key Design Constraints

- **ALL commands MUST be idempotent** - the engine retries failed commands with configurable backoff
- **Commands run in separate HTTP requests** - each callback is independent; state is passed between commands via an opaque JSON blob (see State Passthrough below)
- **Three command phases**: task-end commands → `MutateProcessState` (in-memory state transition) → task-start commands → `UpdateProcessState` (persist to Storage) → post-commit commands
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
│   │       ├── DeleteDataElements.cs        - Auto-delete data types with AutoDeleteOnProcessEnd (post-commit)
│   │       └── DeleteInstance.cs            - Hard-delete instance if ApplicationMetadata.AutoDeleteOnProcessEnd (post-commit)
│   ├── AltinnEvents/
│   │   ├── MovedToAltinnEvent.cs            - Fires movedTo.{taskId} event (post-commit)
│   │   ├── CompletedAltinnEvent.cs          - Fires process.completed event (post-commit)
│   │   └── InstanceCreatedAltinnEvent.cs    - Fires instance.created event (post-commit, first task only)
│   ├── ExecuteServiceTask.cs                - Runs IServiceTask.Execute() (post-commit)
│   ├── MutateProcessState.cs                - Mutates in-memory process state between task-end and task-start
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
│   ├── CallbackErrorResponse.cs             - Error response from callback controller
│   ├── AppCallbackResponse.cs               - Success response with updated state blob
│   └── InstanceState.cs                     - Internal DTO for transported instance + form data state
├── InstanceStateService.cs                  - Captures/restores InstanceDataUnitOfWork to/from opaque state blob
├── ProcessNextRequestFactory.cs             - Maps ProcessStateChange → ProcessNextRequest
└── WorkflowCommandSet.cs                    - Defines command sequences per event type
```

## Command Sequences

Defined in `WorkflowCommandSet.cs`. `ProcessNextRequestFactory` assembles the full sequence:
1. Task-end/abandon commands (from `process_EndTask`/`process_AbandonTask` events)
2. `MutateProcessState` (inserted by factory if there are task-end/abandon commands)
3. Task-start and process-end commands (from `process_StartTask`/`process_EndEvent` events)
4. `UpdateProcessState` (always inserted by factory)
5. Post-commit commands

### Task-to-Task Transition (e.g., Task_1 → Task_2)
```
── instance.Process.CurrentTask = Task_1 (OLD) ──
ProcessTaskEnd → CommonTaskFinalization → EndTaskLegacyHook → OnTaskEndingHook → LockTaskData
  ── MutateProcessState (in-memory: CurrentTask → Task_2) ──
── instance.Process.CurrentTask = Task_2 (NEW) ──
UnlockTaskData → ProcessTaskStart(legacy) → OnTaskStartingHook → CommonTaskInitialization → ProcessTaskStart
  ── UpdateProcessState (persist to Storage) ──
MovedToAltinnEvent → [ExecuteServiceTask if service task]
```

### Task-to-End Transition (e.g., Task_1 → EndEvent)
```
── instance.Process.CurrentTask = Task_1 (OLD) ──
ProcessTaskEnd → CommonTaskFinalization → EndTaskLegacyHook → OnTaskEndingHook → LockTaskData
  ── MutateProcessState (in-memory: CurrentTask → null, EndEvent set) ──
OnWorkflowEndingHook
  ── UpdateProcessState (persist to Storage) ──
ProcessEndLegacyHook → DeleteDataElementsIfConfigured → DeleteInstanceIfConfigured → CompletedAltinnEvent
```

### Initial Task Start (process just created)
```
── instance.Process.CurrentTask = Task_1 (already set by CreateInitialProcessState) ──
UnlockTaskData → ProcessTaskStart(legacy) → OnTaskStartingHook → CommonTaskInitialization → ProcessTaskStart
  ── UpdateProcessState (persist to Storage) ──
MovedToAltinnEvent → [ExecuteServiceTask if service task] → [InstanceCreatedAltinnEvent if first task]
```

### Task Abandon (reject → end)
```
── instance.Process.CurrentTask = Task_1 (OLD) ──
ProcessTaskAbandon → OnTaskAbandonHook → AbandonTaskLegacyHook
  ── MutateProcessState (in-memory: CurrentTask → null or next task) ──
[OnWorkflowEndingHook if ending] / [task-start commands if moving to next task]
  ── UpdateProcessState (persist to Storage) ──
[post-commit commands]
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

## State Passthrough

Each callback needs an `InstanceDataUnitOfWork` (instance + form data). Rather than fetching from Storage on every callback (which would see stale process state), the app captures instance + form data into an opaque `JsonElement` blob that the engine stores and echoes back with each callback.

**Capture point**: `ProcessEngine.HandleMoveToNext` captures state BEFORE `MoveProcessStateToNextAndGenerateEvents` mutates `instance.Process`. This means the blob carries the OLD process state (CurrentTask = the task being left). `MutateProcessState` transitions the in-memory state to the new task between the two command groups.

**Flow**:
1. `InstanceStateService.CaptureState` → serializes instance + form data into `InstanceState` → `JsonElement`
2. State blob is included in `ProcessNextRequest.State`
3. Engine echoes it back in `AppCallbackPayload.State` for each callback
4. `InstanceStateService.RestoreState` → deserializes, creates `InstanceDataUnitOfWork` with preloaded form data
5. After command execution, updated state is captured and returned in `AppCallbackResponse.State`
6. Engine uses the returned state for the next callback — state evolves command by command

**Why commands read from `instance.Process.CurrentTask`**: In the old ProcessEngine, task-end/start handlers received `taskId` as an explicit parameter (extracted from the event's `ProcessInfo`). The new commands read directly from `instance.Process.CurrentTask` instead — simpler, single source of truth. `MutateProcessState` ensures each command group sees the correct CurrentTask.

## Authorization and Data Saves (current plan)

All data saves during callbacks use `StorageAuthenticationMethod.ServiceOwner()`. This is a change from the old ProcessEngine where data operations used the end user's token.

**Current design**: The app (ServiceOwner) performs all data writes during process transitions. The user authorized the action (e.g., "confirm", "reject") at the ProcessNext API entry point. After that, the app executes the transition as ServiceOwner. This means:
- **policy.xml must grant ServiceOwner write rights on all tasks** — this is a prerequisite for the workflow engine
- Storage's authorization service checks the ServiceOwner identity (from the token), not the original user
- The task in Storage's XACML resource comes from `instance.Process.CurrentTask` as persisted in Storage's DB

**Implication for task-start data saves**: Between `MutateProcessState` and `UpdateProcessState`, task-start commands create/modify data while Storage still has the OLD task as current. This works because ServiceOwner has write access on all tasks. If Storage ever starts forwarding the real userId to the authorization service (e.g., via a header), we would need to persist the process state between the two command groups instead. The factory already separates `taskEndSteps` and `taskStartSteps`, so moving the `UpdateProcessState` insert point would be straightforward.

## Known TODOs / In-Progress

- Authentication on callback controller: currently `[AllowAnonymous]`, should use X-Api-Key scheme
- `DeleteDataElementsIfConfigured` and `DeleteInstanceIfConfigured` are wired into process end sequence (post-commit)
- `Actor.UserIdOrOrgNumber` could use a more specific type
- `AppCallbackPayload.LockToken` naming inconsistency with engine (LockKey vs LockToken)
- "Go to next task after service task" - automatic progression not yet reimplemented
