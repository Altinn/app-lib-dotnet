namespace Altinn.App.ProcessEngine.Models;

public abstract record ProcessEngineTaskInstruction
{
    private ProcessEngineTaskInstruction() { }

    public sealed record MoveProcessForward(string From, string To, string? Action = null)
        : ProcessEngineTaskInstruction;

    public sealed record ExecuteServiceTask(string Identifier) : ProcessEngineTaskInstruction;

    public sealed record ExecuteInterfaceHooks(object Something) : ProcessEngineTaskInstruction;

    public sealed record SendCorrespondence(object Something) : ProcessEngineTaskInstruction;

    public sealed record SendEformidling(object Something) : ProcessEngineTaskInstruction;

    public sealed record SendFiksArkiv(object Something) : ProcessEngineTaskInstruction;

    public sealed record PublishAltinnEvent(object Something) : ProcessEngineTaskInstruction;
}
