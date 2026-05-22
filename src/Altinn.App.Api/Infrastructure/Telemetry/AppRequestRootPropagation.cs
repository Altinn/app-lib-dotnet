using System.ComponentModel;
using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace Altinn.App.Api.Infrastructure.Telemetry;

/// <summary>
/// Installs and enables request trace propagation rules for Altinn apps.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class AppRequestRootPropagation
{
    private const string PdfHeaderName = "X-Altinn-IsPdf";
    private static readonly object _lock = new();
    private static volatile bool _rootAppRequests;

    /// <summary>
    /// Installs the propagation wrappers. This should run before <c>WebApplication.CreateBuilder</c>.
    /// </summary>
    public static void Install()
    {
        lock (_lock)
        {
            _ = Sdk.SuppressInstrumentation; // Triggers Sdk static initialization before reading the default propagator.

            if (Propagators.DefaultTextMapPropagator is not RootingTextMapPropagator)
            {
                Sdk.SetDefaultTextMapPropagator(new RootingTextMapPropagator(Propagators.DefaultTextMapPropagator));
            }

            if (DistributedContextPropagator.Current is not RootingDistributedContextPropagator)
            {
                DistributedContextPropagator.Current = new RootingDistributedContextPropagator(
                    DistributedContextPropagator.Current
                );
            }
        }
    }

    /// <summary>
    /// Enables root request traces for apps that have opted into OpenTelemetry.
    /// </summary>
    public static void Enable()
    {
        Install();
        _rootAppRequests = true;
    }

    private static bool ShouldStartRootTrace<T>(T carrier, Func<T, string, IEnumerable<string>?> getter)
    {
        if (!_rootAppRequests)
            return false;

        if (carrier is not HttpRequest request)
            return false;

        return !IsPdfGeneratorRequest(request.Headers);
    }

    private static bool ShouldStartRootTrace(
        object? carrier,
        DistributedContextPropagator.PropagatorGetterCallback? getter
    )
    {
        if (!_rootAppRequests || carrier is null)
            return false;

        if (!IsAspNetCoreRequestCarrier(carrier))
            return false;

        return !IsPdfGeneratorRequest(carrier, getter);
    }

    /// <summary>
    /// PDF generation works by using a headless browser to render the frontend of an app instance.
    /// To make debugging PDF generation failures easier, requests originating from the PDF generator
    /// stay in the root trace as children. The frontend sets this header when making app backend
    /// requests in PDF mode.
    /// </summary>
    private static bool IsPdfGeneratorRequest(IHeaderDictionary headers) => headers.ContainsKey(PdfHeaderName);

    private static bool IsAspNetCoreRequestCarrier(object carrier) =>
        carrier is HttpRequest
        || carrier is IHeaderDictionary
        || carrier.GetType().FullName?.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal) is true;

    private static bool IsPdfGeneratorRequest(
        object carrier,
        DistributedContextPropagator.PropagatorGetterCallback? getter
    )
    {
        if (carrier is HttpRequest request)
            return IsPdfGeneratorRequest(request.Headers);

        if (carrier is IHeaderDictionary headers)
            return IsPdfGeneratorRequest(headers);

        if (getter is null)
            return false;

        getter(carrier, PdfHeaderName, out var fieldValue, out var fieldValues);
        if (!string.IsNullOrEmpty(fieldValue))
            return true;

        return fieldValues?.Any(value => !string.IsNullOrEmpty(value)) is true;
    }

    internal sealed class RootingTextMapPropagator(TextMapPropagator inner) : TextMapPropagator
    {
        public override ISet<string>? Fields => inner.Fields;

        public override PropagationContext Extract<T>(
            PropagationContext context,
            T carrier,
            Func<T, string, IEnumerable<string>?> getter
        )
        {
            if (ShouldStartRootTrace(carrier, getter))
                return default;

            return inner.Extract(context, carrier, getter);
        }

        public override void Inject<T>(PropagationContext context, T carrier, Action<T, string, string> setter) =>
            inner.Inject(context, carrier, setter);
    }

    internal sealed class RootingDistributedContextPropagator(DistributedContextPropagator inner)
        : DistributedContextPropagator
    {
        public override IReadOnlyCollection<string> Fields => inner.Fields;

        public override IEnumerable<KeyValuePair<string, string?>>? ExtractBaggage(
            object? carrier,
            PropagatorGetterCallback? getter
        )
        {
            if (ShouldStartRootTrace(carrier, getter))
                return null;

            return inner.ExtractBaggage(carrier, getter);
        }

        public override void ExtractTraceIdAndState(
            object? carrier,
            PropagatorGetterCallback? getter,
            out string? traceId,
            out string? traceState
        )
        {
            if (ShouldStartRootTrace(carrier, getter))
            {
                traceId = null;
                traceState = null;
                return;
            }

            inner.ExtractTraceIdAndState(carrier, getter, out traceId, out traceState);
        }

        public override void Inject(Activity? activity, object? carrier, PropagatorSetterCallback? setter) =>
            inner.Inject(activity, carrier, setter);
    }
}
