using System.Security.Claims;
using Altinn.App.Core.Extensions;
using Altinn.App.Core.Internal.Language;
using Altinn.App.Core.Internal.Profile;
using Altinn.Platform.Profile.Models;

namespace Altinn.App.Core.Helpers;

internal class LanguageHelper
{
    private readonly IProfileClient _profileClient;

    public LanguageHelper(IProfileClient profileClient)
    {
        _profileClient = profileClient;
    }

    internal async Task<string> GetUserLanguage(ClaimsPrincipal? user)
    {
        if (user is null)
        {
            return LanguageConst.Nb;
        }

        return await GetUserLanguage(user.GetUserIdAsInt());
    }

    internal async Task<string> GetUserLanguage(int? userId)
    {
        string language = LanguageConst.Nb;

        if (userId is null)
        {
            return language;
        }

        UserProfile userProfile =
            await _profileClient.GetUserProfile(userId.Value)
            ?? throw new InvalidOperationException("Could not get user profile while getting language");

        return userProfile?.ProfileSettingPreference?.Language ?? language;
    }
}
