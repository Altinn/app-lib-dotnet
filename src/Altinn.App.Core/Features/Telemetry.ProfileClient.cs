using System.Diagnostics;

namespace Altinn.App.Core.Features;

partial class Telemetry
{
    internal Activity? StartGetUserProfileActivity(int? userId)
    {
        var activity = ActivitySource.StartActivity("ProfileClient.GetUserProfile");
        activity?.SetTag(ProfileClientLabels.UserId, userId);
        return activity;
    }

    internal class ProfileClientLabels
    {
        internal const string UserId = "ProfileClient.UserId";
    }
}
