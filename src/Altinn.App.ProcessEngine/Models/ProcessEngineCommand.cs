using System.Text.Json.Serialization;

namespace Altinn.App.ProcessEngine.Models;

/// <summary>
/// Describes a command to be executed by the process engine.
/// </summary>
[JsonPolymorphic(
    TypeDiscriminatorPropertyName = "type",
    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor
)]
[JsonDerivedType(typeof(AppCommand), "app")]
public abstract record ProcessEngineCommand
{
    /// <summary>
    /// The unique identifier of the command.
    /// </summary>
    public abstract string Identifier { get; }

    /// <summary>
    /// The maximum allowed execution time for the command.
    /// If the command does not complete within this time, it will be considered failed.
    /// </summary>
    public TimeSpan? MaxExecutionTime { get; init; }

    private ProcessEngineCommand(TimeSpan? maxExecutionTime = null)
    {
        MaxExecutionTime = maxExecutionTime;
    }

    /// <summary>
    /// A command that gets handled by the calling application (via webhook).
    /// </summary>
    /// <param name="CommandKey">The command key. A unique identifier that is understood by the app's webhook receiver</param>
    /// <param name="Metadata">Optional metadata to send back with the command. If specified this becomes a POST request. Otherwise, GET.</param>
    /// <param name="MaxExecutionTime">The maximum allowed execution time for the command.</param>
    public sealed record AppCommand(string CommandKey, string? Metadata = null, TimeSpan? MaxExecutionTime = null)
        : ProcessEngineCommand(MaxExecutionTime)
    {
        /// <inheritdoc/>
        public override string Identifier => CommandKey;
    };

    /// <summary>
    /// Debug: A command that throws an exception when executed.
    /// </summary>
    internal sealed record Throw : ProcessEngineCommand
    {
        /// <inheritdoc/>
        public override string Identifier => "throw";
    }

    /// <summary>
    /// Debug: A command that performs no operation, simply returns a completed task.
    /// </summary>
    internal sealed record Noop : ProcessEngineCommand
    {
        /// <inheritdoc/>
        public override string Identifier => "noop";
    }

    /// <summary>
    /// Debug: A command that performs a timeout/delay when executed.
    /// </summary>
    /// <param name="Duration">The timeout duration.</param>
    internal sealed record Timeout(TimeSpan Duration) : ProcessEngineCommand
    {
        /// <inheritdoc/>
        public override string Identifier => "timeout";
    }

    /// <summary>
    /// A command that performs a webhook callback to the specified URI with an optional payload.
    /// </summary>
    /// <remarks>Currently only used for debugging, but otherwise a potentially useful command type in general.</remarks>
    /// <param name="Uri">The uri to call.</param>
    /// <param name="Payload">An optional payload string. If provided, a POST request will be issued. Otherwise, GET.</param>
    /// <param name="ContentType">The value to send along with the request in the Content-Type header.</param>
    internal sealed record Webhook(string Uri, string? Payload = null, string? ContentType = null)
        : ProcessEngineCommand
    {
        public override string Identifier => "webhook";
    }

    /// <summary>
    /// Debug: A command that executes a delegate function.
    /// </summary>
    /// <param name="Action">The delegate method</param>
    internal sealed record Delegate(Func<ProcessEngineJob, ProcessEngineTask, CancellationToken, Task> Action)
        : ProcessEngineCommand
    {
        public override string Identifier => "delegate";
    }

    /// <inheritdoc/>
    public sealed override string ToString() => Identifier;
}
