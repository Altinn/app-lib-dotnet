using System.Security.Claims;
using Altinn.App.Api.Tests.Mocks;
using Altinn.App.Api.Tests.Utils;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.Profile;
using Altinn.App.Core.Internal.Registers;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Altinn.App.Api.Tests.Helpers;

public class UserHelperTest
{
    private sealed record Fixture(WebApplication App) : IAsyncDisposable
    {
        public readonly IOptions<GeneralSettings> GeneralSettings = Options.Create(new GeneralSettings());
        public IProfileClient ProfileClientMock => App.Services.GetRequiredService<IProfileClient>();
        public IAltinnPartyClient AltinnPartyClientMock => App.Services.GetRequiredService<IAltinnPartyClient>();

        public static Fixture Create(ClaimsPrincipal userPrincipal, string? partyCookieValue = null)
        {
            var app = TestUtils.AppBuilder.Build(overrideAltinnAppServices: services =>
            {
                var httpContextMock = new Mock<HttpContext>();
                httpContextMock.Setup(x => x.Request.Cookies["AltinnPartyId"]).Returns(partyCookieValue);
                httpContextMock.Setup(httpContext => httpContext.User).Returns(userPrincipal);
                var httpContextAccessor = new Mock<IHttpContextAccessor>();
                httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

                services.AddSingleton(httpContextAccessor.Object);
                services.AddTransient<IProfileClient, ProfileClientMock>();
                services.AddTransient<IAltinnPartyClient, AltinnPartyClientMock>();
            });
            return new Fixture(app);
        }

        public async ValueTask DisposeAsync() => await App.DisposeAsync();
    }

    [Fact]
    public async Task GetUserContext_PerformsCorrectLogic()
    {
        // Arrange
        const int userId = 1337;
        const int partyId = 501337;
        const int authLevel = 3;

        var userPrincipal = PrincipalUtil.GetUserPrincipal(userId, partyId, authLevel);
        await using var fixture = Fixture.Create(userPrincipal);
        var userHelper = new UserHelper(
            profileClient: fixture.ProfileClientMock,
            altinnPartyClientService: fixture.AltinnPartyClientMock,
            settings: fixture.GeneralSettings
        );
        var httpContextAccessor = fixture.App.Services.GetRequiredService<IHttpContextAccessor>();
        var httpContext = httpContextAccessor.HttpContext;
        var userProfile = await fixture.ProfileClientMock.GetUserProfile(userId);
        var party = await fixture.AltinnPartyClientMock.GetParty(partyId);

        // Act
        var result = await userHelper.GetUserContext(httpContext!);

        // Assert
        result
            .Should()
            .BeEquivalentTo(
                new Altinn.App.Core.Models.UserContext
                {
                    SocialSecurityNumber = "01039012345",
                    UserName = $"User{userId}",
                    UserId = userId,
                    PartyId = partyId,
                    AuthenticationLevel = authLevel,
                    User = userPrincipal,
                    UserParty = userProfile!.Party,
                    Party = party,
                }
            );
    }
}
