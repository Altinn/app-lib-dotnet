using System.Diagnostics;

namespace Altinn.App.Core.Features;

partial class Telemetry
{
    internal Activity? StartFileValidateActivity()
    {
        return ActivitySource.StartActivity("FileValidatorService.Validate");
    }
}
