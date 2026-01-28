using Altinn.App.Core.Models.Process;

internal sealed class ProcessNextCommands
{
    public List<string> Generate(ProcessStateChange processStateChange)
    {
        return [];
    }

    private List<string> GenerateTaskStartCommands(string taskId)
    {
        return [];
    }

    private List<string> GenerateTaskEndCommands(string taskId)
    {
        return [];
    }

    private List<string> GenerateProcessEndCommands(string taskId)
    {
        return [];
    }
}
