using System.Diagnostics;
using NetEscapades.EnumGenerators;
using Tag = System.Collections.Generic.KeyValuePair<string, object?>;
using static Altinn.App.Core.Features.Telemetry.Notifications;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Metrics;

namespace Altinn.App.Core.Features;

public partial class Telemetry
{
    internal Activity? StartNotificationActivity(OrderType type)
    {
        var activity = ActivitySource.StartActivity(OrderTraceName);
        activity?.SetTag(TypeLabel, type.ToStringFast());
        return activity;
    }

    internal Counter<long> GetNotificationOrdersMetric(OrderType type, OrderResult result)
    {
        var closure = (type, result);
        return GetCounter(OrderMetricKey(type, result), static (name, self, context) => self.Meter.CreateCounter<long>(
            name,
            unit: null,
            description: null,
            tags: [new Tag(TypeLabel, context.type.ToStringFast()), new Tag(ResultLabel, context.result.ToStringFast())]
        ), closure);
    }

    internal void NotificationOrderAdded(OrderType type, OrderResult result)
    {
        var counter = GetNotificationOrdersMetric(type, result);
        counter.Add(1);
    }

    internal static class Notifications
    {
        internal static readonly string TypeLabel = "type";
        internal static readonly string ResultLabel = "result";

        internal static readonly string OrderTraceName = "Notifications.Order";

        private static readonly string _orderMetricName = Metrics.CreateName("notification_orders");

        internal static string OrderMetricKey(OrderType type, OrderResult result) => $"{_orderMetricName}-{type.ToStringFast()}-{result.ToStringFast()}";

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