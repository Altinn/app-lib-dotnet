using System.Diagnostics;

namespace Altinn.App.Core.Features;

partial class Telemetry
{
    internal Activity? StartAnalyseActivity()
    {
        var activity = ActivitySource.StartActivity("FileAnalysis.Analyse");
        return activity;
    }
}
