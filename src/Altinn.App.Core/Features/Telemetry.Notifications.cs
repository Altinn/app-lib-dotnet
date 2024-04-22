using System.Diagnostics;
using NetEscapades.EnumGenerators;
using Tag = System.Collections.Generic.KeyValuePair<string, object?>;
using static Altinn.App.Core.Features.Telemetry.Notifications;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Metrics;

namespace Altinn.App.Core.Features;

public partial class Telemetry
{
    private void InitNotifications()
    {
        _counters.Add(OrderMetricName, Meter.CreateCounter<long>(
            OrderMetricName,
            unit: null,
            description: null
        ));
    }

    internal Activity? StartNotificationOrderActivity(OrderType type)
    {
        var activity = ActivitySource.StartActivity(OrderTraceName);
        activity?.SetTag(TypeLabel, type.ToStringFast());
        return activity;
    }

    internal void RecordNotificationOrder(OrderType type, OrderResult result)
    {
        var counter = _counters[OrderMetricName];
        counter.Add(1, new Tag(TypeLabel, type.ToStringFast()), new Tag(ResultLabel, result.ToStringFast()));
    }

    internal static class Notifications
    {
        internal static readonly string TypeLabel = "type";
        internal static readonly string ResultLabel = "result";

        internal static readonly string OrderTraceName = "Notifications.Order";

        internal static readonly string OrderMetricName = Metrics.CreateLibName("notification_orders");

        [EnumExtensions]
        internal enum OrderResult
        {
            [Display(Name = "success")]
            Success,
            [Display(Name = "error")]
            Error
        }

        [EnumExtensions]
        internal enum OrderType
        {
            [Display(Name = "sms")]
            Sms,
            [Display(Name = "email")]
            Email,
        }
    }
}