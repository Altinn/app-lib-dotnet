using System.Diagnostics;

namespace Altinn.App.Core.Features;

partial class Telemetry
{
    internal Activity? StartGetUserProfileActivity(int? userId)
    {
        var activity = ActivitySource.StartActivity("ProfileClient.GetUserProfile");
        activity?.SetTag(InternalLabels.ProfileClientUserId, userId);
        return activity;
    }
}
