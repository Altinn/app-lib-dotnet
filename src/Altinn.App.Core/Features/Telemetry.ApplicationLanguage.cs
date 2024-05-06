using System.Diagnostics;

namespace Altinn.App.Core.Features;

public partial class Telemetry
{
    internal Activity? StartGetApplicationLanguageActivity()
    {
        var activity = ActivitySource.StartActivity("ApplicationLanguage.Get");
        return activity;
    }
}
