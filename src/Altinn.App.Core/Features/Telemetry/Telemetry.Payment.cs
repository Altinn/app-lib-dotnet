using System.Diagnostics;
using Altinn.App.Core.Features.Payment;

namespace Altinn.App.Core.Features;

partial class Telemetry
{
    internal Activity? StartCalculateOrderDetailsActivity(IOrderDetailsCalculator calculator) =>
        ActivitySource
            .StartActivity($"{Payment.Prefix}.CalculateOrderDetails")
            ?.AddTag("calculatorType", calculator.GetType().FullName);

    internal static class Payment
    {
        internal const string Prefix = "Payment";
    }
}
