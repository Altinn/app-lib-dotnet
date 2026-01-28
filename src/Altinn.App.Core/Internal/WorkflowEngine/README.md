# Process Engine Integration

Integration layer between Altinn.App.Core and the async Process Engine service.

## Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│ App                                                              │
│                                                                  │
│  ProcessEngineClient.ProcessNext(request)                        │
│                        │                                         │
│                        ▼                                         │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │ Process Engine Service                                     │  │
│  │ • Executes commands sequentially                           │  │
│  │ • Handles retries and error recovery                       │  │
│  │ • Calls back to app for each command                       │  │
│  └────────────────────────────────────────────────────────────┘  │
│                        │                                         │
│                        ▼                                         │
│  ProcessEngineCallbackController.ExecuteCommand(key, payload)    │
│                        │                                         │
│                        ▼                                         │
│  Resolve IProcessEngineCommand by commandKey                     │
│                        │                                         │
│                        ▼                                         │
│  command.Execute(context, typedPayload)                          │
│  • ProcessEngineCommandBase deserializes payload                 │
│  • Command executes with strongly-typed payload                  │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

## How to Add a New Command

**ALL commands MUST be idempotent** - they may be retried on failure.

### Command Without Payload

```csharp
internal sealed class MyCommand : IProcessEngineCommand
{
    public static string Key => "MyCommand";
    public string GetKey() => Key;

    public async Task<ProcessEngineCommandResult> Execute(ProcessEngineCommandContext context)
    {
        Instance instance = context.InstanceDataMutator.Instance;
        // ... do work ...
        return new SuccessfulProcessEngineCommandResult();
    }
}
```

### Command With Typed Payload

```csharp
internal sealed record MyCommandPayload(string Data) : CommandRequestPayload;

internal sealed class MyCommand : ProcessEngineCommandBase<MyCommandPayload>
{
    public static string Key => "MyCommand";
    public override string GetKey() => Key;

    public override async Task<ProcessEngineCommandResult> Execute(
        ProcessEngineCommandContext context,
        MyCommandPayload payload)
    {
        // Use payload.Data directly
        return new SuccessfulProcessEngineCommandResult();
    }
}
```

Then:

1. **Register payload in `CommandPayloadJsonContext`** (`Commands/_Base/CommandPayload.cs`) - Add
   `[JsonSerializable(typeof(MyCommandPayload))]` for AOT-compatible source generation.
2. Register command in DI (`Extensions/ServiceCollectionExtensions.cs`)
3. Add to command sequence (`ProcessEventCommands.cs`)

## Command Sequences

Commands execute in sequences defined in `ProcessEventCommands.cs`:

- **Task Start**: UnlockTaskData → ProcessTaskStart → ... → UpdateProcessState → MovedToAltinnEvent
- **Task End**: ProcessTaskEnd → ... → LockTaskData → UpdateProcessState
- **Process End**: OnProcessEndingHook → UpdateProcessState → CompletedAltinnEvent

`UpdateProcessState` commits the process state to storage - commands after it are "post-commit" side effects.
