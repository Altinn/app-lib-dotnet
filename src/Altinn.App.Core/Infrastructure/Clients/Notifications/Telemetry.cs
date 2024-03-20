using System.Diagnostics;
using Microsoft.ApplicationInsights;
using Prometheus;

namespace Altinn.App.Core.Infrastructure.Clients.Notifications;

internal static class Telemetry 
{
    internal static readonly Counter OrderCount = Metrics
        .CreateCounter("altinn_app_notification_order_request_count", "Number of notification order requests.", labelNames: ["type", "result"]);

    internal static class Types 
    {
        internal static readonly string Sms = "sms";
        internal static readonly string Email = "email";
    }

    internal static class Result 
    {
        internal static readonly string Success = "success";
        internal static readonly string Error = "error";
    }

    internal struct Dependency : IDisposable
    {
        private readonly TelemetryClient? _telemetryClient;
        private readonly long _startTimestamp;
        private bool _errored;

        public void Errored() => _errored = true;

        public Dependency(TelemetryClient? telemetryClient)
        {
            _telemetryClient = telemetryClient;
            _startTimestamp = Stopwatch.GetTimestamp();
        }

        public readonly void Dispose()
        {
            var stopTimestamp = Stopwatch.GetTimestamp();
            var elapsed = Stopwatch.GetElapsedTime(_startTimestamp, stopTimestamp);

            _telemetryClient?.TrackDependency(
                "Altinn.Notifications",
                "OrderNotification",
                null,
                new DateTime(_startTimestamp),
                elapsed,
                !_errored
            );
        }
    }
}
