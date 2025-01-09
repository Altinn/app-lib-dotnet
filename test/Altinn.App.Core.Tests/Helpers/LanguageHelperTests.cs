using System.Security.Claims;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.Language;
using Altinn.App.Core.Internal.Profile;
using Altinn.Platform.Profile.Models;
using AltinnCore.Authentication.Constants;
using Moq;

namespace Altinn.App.Core.Tests.Helpers;

public class LanguageHelperTests
{
    [Fact]
    public async Task GetUserLanguage_WithUser_ReturnsLanguageFromPreference()
    {
        // Arrange
        int userId = 123;
        Claim claim = new(AltinnCoreClaimTypes.UserId, userId.ToString());
        ClaimsIdentity claimsIdentity = new([claim], "TestAuthType");
        ClaimsPrincipal user = new(claimsIdentity);
        Mock<IProfileClient> profileClient = new();
        profileClient
            .Setup(p => p.GetUserProfile(userId))
            .ReturnsAsync(
                new UserProfile
                {
                    ProfileSettingPreference = new ProfileSettingPreference { Language = LanguageConst.En },
                    UserId = userId,
                }
            );
        LanguageHelper languageHelper = new(profileClient.Object);

        // Act
        string language = await languageHelper.GetUserLanguage(user);

        // Assert
        Assert.Equal(LanguageConst.En, language);
    }

    [Fact]
    public async Task GetUserLanguage_WithUserAndNoLanguagePreference_ReturnsNb()
    {
        // Arrange
        int userId = 123;
        Claim claim = new(AltinnCoreClaimTypes.UserId, userId.ToString());
        ClaimsIdentity claimsIdentity = new([claim], "TestAuthType");
        ClaimsPrincipal user = new(claimsIdentity);
        Mock<IProfileClient> profileClient = new();
        profileClient
            .Setup(p => p.GetUserProfile(userId))
            .ReturnsAsync(
                new UserProfile
                {
                    // ProfileSetingPreference is null
                    UserId = userId,
                }
            );
        LanguageHelper languageHelper = new(profileClient.Object);

        // Act
        string language = await languageHelper.GetUserLanguage(user);

        // Assert
        Assert.Equal(LanguageConst.Nb, language);
    }

    [Fact]
    public async Task GetUserLanguage_WithUserIsNull_ReturnsNb()
    {
        // Arrange
        ClaimsPrincipal? user = null;
        Mock<IProfileClient> profileClient = new();
        LanguageHelper languageHelper = new(profileClient.Object);

        // Act
        string language = await languageHelper.GetUserLanguage(user);

        // Assert
        Assert.Equal(LanguageConst.Nb, language);
    }

    [Fact]
    public async Task GetUserLanguage_WithUserId_ReturnsLanguageFromPreference()
    {
        // Arrange
        int userId = 123;
        Mock<IProfileClient> profileClient = new();
        profileClient
            .Setup(p => p.GetUserProfile(userId))
            .ReturnsAsync(
                new UserProfile
                {
                    ProfileSettingPreference = new ProfileSettingPreference { Language = LanguageConst.Nn },
                    UserId = userId,
                }
            );
        LanguageHelper languageHelper = new(profileClient.Object);

        // Act
        string language = await languageHelper.GetUserLanguage(userId);

        // Assert
        Assert.Equal(LanguageConst.Nn, language);
    }

    [Fact]
    public async Task GetUserLanguage_WithUserIdAndNoLanguagePreference_ReturnsNb()
    {
        // Arrange
        int userId = 123;
        Mock<IProfileClient> profileClient = new();
        profileClient
            .Setup(p => p.GetUserProfile(userId))
            .ReturnsAsync(
                new UserProfile
                {
                    // ProfileSetingPreference is null
                    UserId = userId,
                }
            );
        LanguageHelper languageHelper = new(profileClient.Object);

        // Act
        string language = await languageHelper.GetUserLanguage(userId);

        // Assert
        Assert.Equal(LanguageConst.Nb, language);
    }

    [Fact]
    public async Task GetUserLanguage_WithUserIdIsNull_ReturnsNb()
    {
        // Arrange
        int? userId = null;
        Mock<IProfileClient> profileClient = new();
        LanguageHelper languageHelper = new(profileClient.Object);

        // Act
        string language = await languageHelper.GetUserLanguage(userId);

        // Assert
        Assert.Equal(LanguageConst.Nb, language);
    }
}
