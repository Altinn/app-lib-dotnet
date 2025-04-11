namespace Altinn.App.Clients.Fiks.Constants;

/// <summary>
/// Constants related to the configuration and operation of the Fiks IO client.
/// </summary>
public static class FiksIOConstants
{
    /// <summary>
    /// The ID for the user-configurable resilience pipeline (retry strategy).
    /// </summary>
    public const string UserDefinedResiliencePipelineId = "FiksIOResiliencePipeline";

    internal const string DefaultResiliencePipelineId = "DefaultFiksIOResiliencePipeline";
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
