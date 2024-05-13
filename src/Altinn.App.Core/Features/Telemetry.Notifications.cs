using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using NetEscapades.EnumGenerators;
using static Altinn.App.Core.Features.Telemetry.Notifications;
using Tag = System.Collections.Generic.KeyValuePair<string, object?>;

namespace Altinn.App.Core.Features;

partial class Telemetry
{
    private void InitNotifications(InitContext context)
    {
        InitMetricCounter(
            context,
            OrderMetricName,
            init: static m =>
            {
                foreach (var type in OrderTypeExtensions.GetValues())
                {
                    foreach (var result in OrderResultExtensions.GetValues())
                    {
                        m.Add(
                            0,
                            new Tag(InternalLabels.TypeLabel, type.ToStringFast()),
                            new Tag(InternalLabels.Result, result.ToStringFast())
                        );
                    }
                }
            }
        );
    }

    internal Activity? StartNotificationOrderActivity(OrderType type)
    {
        var activity = ActivitySource.StartActivity("Notifications.Order");
        activity?.SetTag(InternalLabels.TypeLabel, type.ToStringFast());
        return activity;
    }

    internal void RecordNotificationOrder(OrderType type, OrderResult result) =>
        _counters[OrderMetricName]
            .Add(
                1,
                new Tag(InternalLabels.TypeLabel, type.ToStringFast()),
                new Tag(InternalLabels.Result, result.ToStringFast())
            );

    internal static class Notifications
    {
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
