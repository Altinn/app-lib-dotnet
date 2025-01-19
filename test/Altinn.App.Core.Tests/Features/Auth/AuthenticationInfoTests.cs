using Altinn.App.Api.Tests.Utils;
using Altinn.App.Core.Features.Auth;
using Altinn.App.Core.Internal.Language;
using Altinn.Platform.Profile.Models;
using FluentAssertions;

namespace Altinn.App.Core.Tests.Features.Auth;

public class AuthenticationInfoTests
{
    [Fact]
    public async Task Test_User_Get_Language_From_Profile()
    {
        var user = TestAuthentication.GetUserAuthenticationInfo(
            profileSettingPreference: new ProfileSettingPreference { Language = LanguageConst.En }
        );

        var lang = await user.GetLanguage();

        lang.Should().Be(LanguageConst.En);
    }

    [Fact]
    public async Task Test_User_Get_Default_Language()
    {
        var user = TestAuthentication.GetUserAuthenticationInfo();

        var lang = await user.GetLanguage();

        lang.Should().Be(LanguageConst.Nb);
    }

    [Fact]
    public async Task Test_Unauth_Get_Default_Language()
    {
        var user = new Authenticated.Unauthenticated("");

        var lang = await user.GetLanguage();

        lang.Should().Be(LanguageConst.Nb);
    }
}
