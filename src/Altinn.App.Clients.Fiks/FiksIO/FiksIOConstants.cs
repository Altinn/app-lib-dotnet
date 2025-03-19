namespace Altinn.App.Clients.Fiks.FiksIO;

public static class FiksIOConstants
{
    public const string ResiliencePipelineId = "FiksIOResiliencePipeline";
    public const string MessageRequestPropertyKey = "FiksIOMessageRequest";

    public static class ErrorStubs
    {
        public const string InvalidRequest = "ugyldigforespoersel";
        public const string ServerError = "serverfeil";
        public const string NotFound = "ikkefunnet";
    }

    public static bool IsErrorType(string messageType) =>
        messageType.Contains(ErrorStubs.InvalidRequest, StringComparison.OrdinalIgnoreCase)
        || messageType.Contains(ErrorStubs.ServerError, StringComparison.OrdinalIgnoreCase)
        || messageType.Contains(ErrorStubs.NotFound, StringComparison.OrdinalIgnoreCase);
}
