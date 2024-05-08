using System.Diagnostics;

namespace Altinn.App.Core.Features;

partial class Telemetry
{
    internal Activity? StartGetOrganizationActivity(string? orgNr)
    {
        var activity = ActivitySource.StartActivity("RegisterERClient.GetOrganization");
        activity?.SetTag(ErClientLables.OrganisationNumber, orgNr);
        return activity;
    }

    internal class ErClientLables
    {
        internal const string OrganisationNumber = "RegisterERClient.OrganisationNumber";
    }
}
