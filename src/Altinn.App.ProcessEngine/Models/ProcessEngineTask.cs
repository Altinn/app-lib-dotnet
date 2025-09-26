namespace Altinn.App.ProcessEngine.Models;

internal sealed record ProcessEngineTask
{
    public ProcessEngineItemStatus Status { get; set; }
    public required string Identifier { get; init; }
    public required int ProcessingOrder { get; init; }
    public required ProcessEngineTaskInstruction Instruction { get; init; }
    public DateTimeOffset? StartTime { get; init; }
    public int RequeueCount { get; set; }

    public static ProcessEngineTask FromRequest(ProcessEngineTaskRequest request, int index) =>
        new()
        {
            Identifier = request.Identifier,
            StartTime = request.StartTime,
            ProcessingOrder = index,
            Instruction = request.Instruction,
        };
};
