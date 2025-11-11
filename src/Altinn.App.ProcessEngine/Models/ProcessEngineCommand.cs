namespace Altinn.App.ProcessEngine.Models;

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

    public sealed record Throw : ProcessEngineCommand
    {
        public override string Identifier => Guid.NewGuid().ToString();
    }

    public sealed record Noop : ProcessEngineCommand
    {
        public override string Identifier => Guid.NewGuid().ToString();
    }
}
