using System.Diagnostics;
using static Altinn.App.Core.Features.Telemetry.ApplicationLanguage;
namespace Altinn.App.Core.Features;

public partial class Telemetry
{
    internal Activity? StartGetApplicationLanguageActivity()
    {
        var activity = ActivitySource.StartActivity(TraceNameGet);
        return activity;
    }
    internal static class ApplicationLanguage
    {
        private const string _prefix = "ApplicationLanguage";

        internal const string TraceNameGet = $"{_prefix}.Get";
    }
}