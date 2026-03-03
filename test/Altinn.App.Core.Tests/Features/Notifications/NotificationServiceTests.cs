using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Notifications;
using Altinn.App.Core.Internal.AltinnCdn;
using Altinn.App.Core.Internal.Language;
using Altinn.App.Core.Internal.Profile;
using Altinn.App.Core.Internal.Registers;
using Altinn.Platform.Profile.Models;
using Altinn.Platform.Storage.Interface.Models;
using Moq;

namespace Altinn.App.Core.Tests.Features.Notifications;

public class NotificationServiceTests
{
    private readonly Mock<INotificationOrderClient> _orderClientMock = new();
    private readonly Mock<IProfileClient> _profileClientMock = new();
    private readonly Mock<IAltinnCdnClient> _cdnClientMock = new();
    private readonly Mock<IAltinnPartyClient> _partyClientMock = new();

    private NotificationService CreateSut() =>
        new(_orderClientMock.Object, _profileClientMock.Object, _cdnClientMock.Object, _partyClientMock.Object);

    #region SSN

    [Fact]
    public async Task DetermineLanguage_PersonOwner_UsesProfileLanguage()
    {
        const string ssn = "01010112345";
        var instanceOwner = new InstanceOwner { PersonNumber = ssn };

        _profileClientMock
            .Setup(p => p.GetUserProfile(ssn))
            .ReturnsAsync(
                new UserProfile
                {
                    ProfileSettingPreference = new ProfileSettingPreference { Language = LanguageConst.En },
                }
            );

        var result = await CreateSut()
            .DetermineLanguage(instanceOwner, requestedOrgLanguage: null, CancellationToken.None);

        Assert.Equal(LanguageConst.En, result);
        _profileClientMock.Verify(p => p.GetUserProfile(ssn), Times.Once);
    }

    [Fact]
    public async Task DetermineLanguage_PersonOwner_ProfileLanguageIsNull_FallsBackToNb()
    {
        const string ssn = "01010112345";
        var instanceOwner = new InstanceOwner { PersonNumber = ssn };

        _profileClientMock
            .Setup(p => p.GetUserProfile(ssn))
            .ReturnsAsync(
                new UserProfile { ProfileSettingPreference = new ProfileSettingPreference { Language = null } }
            );

        var result = await CreateSut()
            .DetermineLanguage(instanceOwner, requestedOrgLanguage: null, CancellationToken.None);

        Assert.Equal(LanguageConst.Nb, result);
    }

    [Fact]
    public async Task DetermineLanguage_PersonOwner_ProfileIsNull_FallsBackToNb()
    {
        const string ssn = "01010112345";
        var instanceOwner = new InstanceOwner { PersonNumber = ssn };

        UserProfile? profile = null;
        _profileClientMock.Setup(p => p.GetUserProfile(ssn)).ReturnsAsync(profile);

        var result = await CreateSut()
            .DetermineLanguage(instanceOwner, requestedOrgLanguage: null, CancellationToken.None);

        Assert.Equal(LanguageConst.Nb, result);
    }

    #endregion

    #region Org number

    [Theory]
    [InlineData(LanguageConst.En, LanguageConst.En)]
    [InlineData(LanguageConst.Nb, LanguageConst.Nb)]
    [InlineData(LanguageConst.Nn, LanguageConst.Nn)]
    [InlineData(null, LanguageConst.Nb)]
    public async Task DetermineLanguage_OrgOwner_UsesRequestedLanguage(
        string? requestedLanguage,
        string expectedLanguage
    )
    {
        var instanceOwner = new InstanceOwner { OrganisationNumber = "123456789" };

        var result = await CreateSut()
            .DetermineLanguage(instanceOwner, requestedOrgLanguage: requestedLanguage, CancellationToken.None);

        Assert.Equal(expectedLanguage, result);
        _profileClientMock.Verify(p => p.GetUserProfile(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region ExternalIdentifier

    [Fact]
    public async Task DetermineLanguage_ExternalIdentifierOwner_PartyUuidMissing_FallsBackToEnglish()
    {
        var instanceOwner = new InstanceOwner { ExternalIdentifier = "ext-user-42" };
        Guid? guid = null;
        _partyClientMock.Setup(p => p.GetPartyUuidByUrn("ext-user-42")).ReturnsAsync(guid);
        var result = await CreateSut()
            .DetermineLanguage(instanceOwner, requestedOrgLanguage: LanguageConst.Nb, CancellationToken.None);
        Assert.Equal(LanguageConst.En, result);
        _partyClientMock.Verify(p => p.GetPartyUuidByUrn("ext-user-42"), Times.Once);
        _profileClientMock.Verify(p => p.GetUserProfile(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DetermineLanguage_ExternalIdentifierOwner_UsesProfileLanguage()
    {
        Guid partyGuid = Guid.NewGuid();
        InstanceOwner instanceOwner = new() { ExternalIdentifier = "ext-user-42" };

        _partyClientMock.Setup(p => p.GetPartyUuidByUrn("ext-user-42")).ReturnsAsync(partyGuid);

        _profileClientMock
            .Setup(p => p.GetUserProfile(partyGuid))
            .ReturnsAsync(
                new UserProfile
                {
                    ProfileSettingPreference = new ProfileSettingPreference { Language = LanguageConst.Nb },
                }
            );

        var result = await CreateSut()
            .DetermineLanguage(instanceOwner, requestedOrgLanguage: null, CancellationToken.None);

        Assert.Equal(LanguageConst.Nb, result);
        _partyClientMock.Verify(p => p.GetPartyUuidByUrn("ext-user-42"), Times.Once);
        _profileClientMock.Verify(p => p.GetUserProfile(partyGuid), Times.Once);
    }

    [Fact]
    public async Task DetermineLanguage_ExternalIdentifierOwner_ProfileLanguageIsNull_FallsBackToEnglish()
    {
        var partyGuid = Guid.NewGuid();
        var instanceOwner = new InstanceOwner { ExternalIdentifier = "ext-user-42" };

        _partyClientMock.Setup(p => p.GetPartyUuidByUrn("ext-user-42")).ReturnsAsync(partyGuid);

        _profileClientMock
            .Setup(p => p.GetUserProfile(partyGuid))
            .ReturnsAsync(
                new UserProfile { ProfileSettingPreference = new ProfileSettingPreference { Language = null } }
            );

        var result = await CreateSut()
            .DetermineLanguage(instanceOwner, requestedOrgLanguage: null, CancellationToken.None);

        Assert.Equal(LanguageConst.En, result);
    }

    [Fact]
    public async Task DetermineLanguage_ExternalIdentifierOwner_ProfileIsNull_FallsBackToEnglish()
    {
        var partyGuid = Guid.NewGuid();
        var instanceOwner = new InstanceOwner { ExternalIdentifier = "ext-user-42" };

        _partyClientMock.Setup(p => p.GetPartyUuidByUrn("ext-user-42")).ReturnsAsync(partyGuid);

        UserProfile? profile = null;
        _profileClientMock.Setup(p => p.GetUserProfile(partyGuid)).ReturnsAsync(profile);

        var result = await CreateSut()
            .DetermineLanguage(instanceOwner, requestedOrgLanguage: null, CancellationToken.None);

        Assert.Equal(LanguageConst.En, result);
    }

    #endregion

    #region Guard

    [Fact]
    public async Task DetermineLanguage_NoIdentifierSet_ThrowsInvalidOperationException()
    {
        var instanceOwner = new InstanceOwner();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut().DetermineLanguage(instanceOwner, requestedOrgLanguage: null, CancellationToken.None)
        );
    }

    #endregion
}
