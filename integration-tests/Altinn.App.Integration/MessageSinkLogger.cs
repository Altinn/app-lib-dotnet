using Microsoft.Extensions.Logging;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Altinn.App.Integration;

internal abstract class Logger : ILogger
{
    protected static string? GetMessage<TState>(
        TState state,
        Exception? exception,
        Func<TState, Exception?, string?> formatter
    )
    {
        return exception == null
            ? formatter(state, null)
            : $"{formatter(state, exception)}{Environment.NewLine}{exception}";
    }

    protected abstract void Log<TState>(
        TState state,
        Exception? exception,
        Func<TState, Exception?, string?> formatter
    );

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string?> formatter
    )
    {
        Log(state, exception, formatter);
    }

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull => new NullScope();
}

internal sealed class MessageSinkLogger(IMessageSink? messageSink) : Logger
{
    private readonly IMessageSink? _messageSink = messageSink;

    protected override void Log<TState>(TState state, Exception? exception, Func<TState, Exception?, string?> formatter)
    {
        if (_messageSink == null)
        {
            return;
        }

        var message = GetMessage(state, exception, formatter);
        _messageSink.OnMessage(new DiagnosticMessage($"[testcontainers.org] {message}"));
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is MessageSinkLogger other)
        {
            return Equals(_messageSink, other._messageSink);
        }

        return false;
    }

    /// <returns>
    /// The hash code of the underlying message sink, because <see cref="DotNet.Testcontainers.Clients.DockerApiClient.LogContainerRuntimeInfoAsync" />
    /// logs the runtime information once per Docker Engine API client and logger.
    /// </returns>
    public override int GetHashCode() => _messageSink?.GetHashCode() ?? 0;
}

internal sealed class NullScope : IDisposable
{
    public void Dispose() { }
}
