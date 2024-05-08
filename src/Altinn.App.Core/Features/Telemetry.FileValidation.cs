using System.Diagnostics;

namespace Altinn.App.Core.Features;

partial class Telemetry
{
    internal Activity? StartFileValidateActivity()
    {
        var activity = ActivitySource.StartActivity("FileValidatorService.Validate");
        return activity;
    }
}
