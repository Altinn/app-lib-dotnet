namespace Altinn.App.Clients.Fiks.FiksIO;

/// <summary>
/// Constants related to the configuration and operation of the Fiks IO client.
/// </summary>
public static class FiksIOConstants
{
    /// <summary>
    /// The ID for the resilience pipeline (retry strategy).
    /// </summary>
    public const string ResiliencePipelineId = "FiksIOResiliencePipeline";

    /// <summary>
    /// The ID for a resilience contextual property used to enrich the logs during retries.
    /// </summary>
    internal const string MessageRequestPropertyKey = "FiksIOMessageRequest";

    internal static class ErrorStubs
    {
        public const string InvalidRequest = "ugyldigforespoersel";
        public const string ServerError = "serverfeil";
        public const string NotFound = "ikkefunnet";
    }

    internal static bool IsErrorType(string messageType) =>
        messageType.Contains(ErrorStubs.InvalidRequest, StringComparison.OrdinalIgnoreCase)
        || messageType.Contains(ErrorStubs.ServerError, StringComparison.OrdinalIgnoreCase)
        || messageType.Contains(ErrorStubs.NotFound, StringComparison.OrdinalIgnoreCase);
}
