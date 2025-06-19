using Altinn.App.Api.Models;
using Altinn.Platform.Storage.Interface.Models;
using Xunit.Abstractions;

namespace Altinn.App.Integration.Tests;

public class BasicAppTests(ITestOutputHelper output) : IAsyncLifetime
{
    private readonly ITestOutputHelper _output = output;
    private AppFixture? _fixture;
    public AppFixture Fixture
    {
        get
        {
            Assert.NotNull(_fixture);
            return _fixture;
        }
    }

    public async Task InitializeAsync()
    {
        _fixture = await AppFixture.Create(_output, TestApps.Basic);
    }

    public async Task DisposeAsync()
    {
        await Fixture.DisposeAsync();
    }

    [Fact]
    public async Task Instantiate()
    {
        var fixture = Fixture;

        var token = await fixture.Auth.GetUserToken(userId: 1337);

        using var response = await fixture.Instances.PostSimplified(
            token,
            new InstansiationInstance { InstanceOwner = new InstanceOwner { PartyId = "501337" } }
        );

        var instance = await response.Read<Instance>();
        Assert.NotNull(instance);

        await response.Verify(v =>
        {
            v = v.Replace(instance.InstanceOwner.PartyId, "<partyId>");
            v = v.Replace(instance.Id.Split('/')[1], "<instanceGuid>");
            return v;
        });
    }

    [Fact]
    public async Task Instantiate_With_Prefill()
    {
        var fixture = Fixture;

        var token = await fixture.Auth.GetUserToken(userId: 1337);

        using var response = await fixture.Instances.PostSimplified(
            token,
            new InstansiationInstance
            {
                InstanceOwner = new InstanceOwner { PartyId = "501337" },
                Prefill = new() { { "model.property1", "Testing" } },
            }
        );

        var instance = await response.Read<Instance>();
        Assert.NotNull(instance);

        await response.Verify(
            instance,
            v =>
            {
                v = v.Replace(instance.InstanceOwner.PartyId, "<partyId>");
                v = v.Replace(instance.Id.Split('/')[1], "<instanceGuid>");
                return v;
            }
        );
    }
}
