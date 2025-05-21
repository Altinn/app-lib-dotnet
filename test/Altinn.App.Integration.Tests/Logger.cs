using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Altinn.App.Integration.Tests;

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

internal sealed class TestOutputLogger(
    ITestOutputHelper? output,
    long fixtureInstance,
    string name,
    bool forTestContainers
) : Logger
{
    private readonly ITestOutputHelper? _output = output;
    private readonly long _fixtureInstance = fixtureInstance;
    private readonly string _name = name;
    private readonly bool _forTestContainers = forTestContainers;

    protected override void Log<TState>(TState state, Exception? exception, Func<TState, Exception?, string?> formatter)
    {
        if (_output == null)
        {
            return;
        }

        var message = GetMessage(state, exception, formatter);
        if (_forTestContainers)
            _output.WriteLine($"[{_fixtureInstance:00}, {_name}, testcontainers] {message}");
        else
            _output.WriteLine($"[{_fixtureInstance:00}, {_name}] {message}");
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is TestOutputLogger other)
        {
            return Equals(_output, other._output);
        }

        return false;
    }

    /// <returns>
    /// The hash code of the underlying message sink, because <see cref="DotNet.Testcontainers.Clients.DockerApiClient.LogContainerRuntimeInfoAsync" />
    /// logs the runtime information once per Docker Engine API client and logger.
    /// </returns>
    public override int GetHashCode() => _output?.GetHashCode() ?? 0;
}

internal sealed class NullScope : IDisposable
{
    public void Dispose() { }
}
