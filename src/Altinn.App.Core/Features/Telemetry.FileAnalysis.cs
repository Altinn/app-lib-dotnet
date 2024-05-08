using System.Diagnostics;
using Altinn.Platform.Storage.Interface.Models;
using static Altinn.App.Core.Features.Telemetry.Data;

namespace Altinn.App.Core.Features;

partial class Telemetry
{
    internal Activity? StartAnalyseActivity()
    {
        var activity = ActivitySource.StartActivity("FileAnalysis.Analyse");
        return activity;
    }
}
