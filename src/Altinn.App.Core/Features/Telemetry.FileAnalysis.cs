using System.Diagnostics;

namespace Altinn.App.Core.Features;

partial class Telemetry
{
    internal Activity? StartAnalyseActivity()
    {
        return ActivitySource.StartActivity("FileAnalysis.Analyse");
    }
}
