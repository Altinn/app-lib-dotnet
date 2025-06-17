using Altinn.App.Api.Models;
using Altinn.Platform.Storage.Interface.Models;
using Xunit.Abstractions;

namespace Altinn.App.Integration.Tests;

public sealed class BasicAppFixture : IAsyncLifetime
{
    private AppFixture? _appFixture;
    private ITestOutputHelper? _output;

    public AppFixture Fixture => _appFixture ?? throw new InvalidOperationException("Fixture not initialized");

    public void SetTestOutput(ITestOutputHelper output) => _output = output;

    public async Task EnsureInitialized()
    {
        if (_appFixture is not null)
            return;

        Assert.NotNull(_output);
        _appFixture = await AppFixture.Create(_output, TestApps.Basic);
    }

    public async Task DisposeAsync()
    {
        if (_appFixture is null)
            return;

        await _appFixture.DisposeAsync();
    }

    public Task InitializeAsync() => Task.CompletedTask;
}

public class BasicAppTests(ITestOutputHelper output, BasicAppFixture fixture)
    : IClassFixture<BasicAppFixture>,
        IAsyncLifetime
{
    private readonly ITestOutputHelper _output = output;
    private readonly BasicAppFixture _fixture = fixture;

    public async Task InitializeAsync()
    {
        _fixture.SetTestOutput(_output);
        await _fixture.EnsureInitialized();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Instantiate()
    {
        var fixture = _fixture.Fixture;

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
}
