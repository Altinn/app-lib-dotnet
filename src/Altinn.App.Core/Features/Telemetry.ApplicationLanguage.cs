using System.Diagnostics;

namespace Altinn.App.Core.Features;

public partial class Telemetry
{
    internal Activity? StartGetApplicationLanguageActivity() => ActivitySource.StartActivity("ApplicationLanguage.Get");
}
