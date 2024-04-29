using System.Diagnostics;
using OpenTelemetry.Trace;

namespace Altinn.App.Core.Features;

internal static class TelemetryExtensions
{
    internal static void Errored(this Activity? activity, Exception? exception = null, string? error = null)
    {
        activity?.SetStatus(ActivityStatusCode.Error, error);
        activity?.RecordException(exception);
    }
}
