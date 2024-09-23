using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.App.Api.Controllers.Conventions;
using Altinn.App.Api.Extensions;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Auth;
using Altinn.App.Core.Models;
using Altinn.Platform.Register.Enums;
using Altinn.Platform.Storage.Interface.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit.Abstractions;

namespace Altinn.App.Api.Tests.Controllers.Conventions;

public class EnumSerializationTests : ApiTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    private const string Org = "tdd";
    private const string App = "contributer-restriction";
    private const int PartyId = 500600;

    private readonly Mock<IAuthorizationClient> _authorizationClientMock;
    private readonly Mock<IAppMetadata> _appMetadataMock;

    public EnumSerializationTests(WebApplicationFactory<Program> factory, ITestOutputHelper outputHelper)
        : base(factory, outputHelper)
    {
        // Mock auth client to return the enum we want to test
        _authorizationClientMock = new Mock<IAuthorizationClient>();
        _authorizationClientMock
            .Setup(a => a.GetPartyList(It.IsAny<int>()))
            .ReturnsAsync([new() { PartyTypeName = PartyType.Person }]);

        _appMetadataMock = new Mock<IAppMetadata>();
        _appMetadataMock
            .Setup(s => s.GetApplicationMetadata())
            .ReturnsAsync(
                new ApplicationMetadata(id: "ttd/test") { PartyTypesAllowed = new PartyTypesAllowed { Person = true } }
            );

        OverrideServicesForAllTests = (services) =>
        {
            services
                .AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            services.AddSingleton<IConfigureOptions<MvcOptions>>(sp =>
            {
                var options = sp.GetRequiredService<IOptionsMonitor<JsonOptions>>();
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                return new ConfigureMvcJsonOptions("EnumAsNumber", options, loggerFactory);
            });

            services.AddScoped(_ => _authorizationClientMock.Object);
            services.AddScoped(_ => _appMetadataMock.Object);
        };
    }

    [Fact]
    public async Task ValidateInstantiation_SerializesPartyTypesAllowedAsNumber()
    {
        // Arrange
        using var client = GetRootedClient(Org, App, 1337, PartyId);

        // Act
        var response = await client.PostAsync(
            $"{Org}/{App}/api/v1/parties/validateInstantiation?partyId={PartyId}",
            null
        );
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var partyTypeEnumJson = JsonDocument
            .Parse(content)
            .RootElement.GetProperty("validParties")
            .EnumerateArray()
            .First()
            .GetProperty("partyTypeName");

        // Assert
        partyTypeEnumJson.Should().NotBeNull();
        partyTypeEnumJson.TryGetInt32(out var partyTypeJsonValue);
        partyTypeJsonValue.Should().Be(1, "PartyTypesAllowed should be serialized as its numeric value");
    }
}
