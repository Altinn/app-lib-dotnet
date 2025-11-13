namespace Altinn.App.ProcessEngine.Models;

/// <summary>
///
/// </summary>
public abstract record ProcessEngineCommand
{
    public abstract string Identifier { get; }
    public TimeSpan? MaxExecutionTime { get; init; }

    private ProcessEngineCommand(TimeSpan? maxExecutionTime = null)
    {
        MaxExecutionTime = maxExecutionTime;
    }

    public sealed record AppCommand(string CommandKey, string Metadata, TimeSpan? MaxExecutionTime = null)
        : ProcessEngineCommand(MaxExecutionTime)
    {
        public override string Identifier => CommandKey;
    };

    internal sealed record Throw : ProcessEngineCommand
    {
        public override string Identifier => "throw";
    }

    internal sealed record Noop : ProcessEngineCommand
    {
        public override string Identifier => "noop";
    }

    internal sealed record Delay(TimeSpan Duration) : ProcessEngineCommand
    {
        public override string Identifier => "delay";
    }

    internal sealed record Callback(string Uri, object? Payload = null) : ProcessEngineCommand
    {
        public override string Identifier => "callback";
    }

    public sealed override string ToString() => Identifier;
}
