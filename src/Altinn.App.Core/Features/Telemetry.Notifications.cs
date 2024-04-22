using System.Diagnostics;
using NetEscapades.EnumGenerators;
using Tag = System.Collections.Generic.KeyValuePair<string, object?>;
using static Altinn.App.Core.Features.Telemetry.Notifications;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Metrics;

namespace Altinn.App.Core.Features;

public partial class Telemetry
{
    internal Activity? StartNotificationOrderActivity(OrderType type)
    {
        var activity = ActivitySource.StartActivity(OrderTraceName);
        activity?.SetTag(TypeLabel, type.ToStringFast());
        return activity;
    }

    private Counter<long> GetNotificationOrdersMetric()
    {
        return GetCounter(OrderMetricName, static (name, self) => self.Meter.CreateCounter<long>(
            name,
            unit: null,
            description: null
        ));
    }

    internal void NotificationOrderRequest(OrderType type, OrderResult result)
    {
        var counter = GetNotificationOrdersMetric();
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