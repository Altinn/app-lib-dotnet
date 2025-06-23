using System.Collections.Frozen;
using System.Collections.Immutable;
using Altinn.App.Api.Infrastructure.Filters;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Altinn.App.Api.Tests.Controllers;

public class ScopeAuthorizerTests : ApiTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    public ScopeAuthorizerTests(WebApplicationFactory<Program> factory, ITestOutputHelper outputHelper)
        : base(factory, outputHelper)
    {
        OverrideServicesForAllTests = (services) => { };
    }

    [Fact]
    public async Task ActionDescriptor()
    {
        string org = "tdd";
        string app = "contributer-restriction";
        HttpClient client = GetRootedClient(org, app);

        using var response = await client.GetAsync($"/{org}/{app}/api/v1/applicationmetadata");
        response.EnsureSuccessStatusCode();

        var descriptors = this.Services.GetRequiredService<CustomActionDescriptorProvider>();
        Assert.NotNull(descriptors?.ActionsNotAuthorized);
        Assert.NotNull(descriptors?.ActionsToAuthorize);
        var snapshot = new
        {
            ActionsToAuthorize = descriptors.ActionsToAuthorize.Select(a => a.DisplayName).Order().ToArray(),
            ScopesToAuthorize = descriptors
                .ActionsToAuthorize.Select(a => new
                {
                    Endpoint = a.DisplayName ?? "N/A",
                    Scope = ((FrozenSet<string>)a.Properties[CustomActionDescriptorProvider.RequiredScopesKey]!)
                        .Order()
                        .ToArray(),
                })
                .ToImmutableSortedDictionary(a => a.Endpoint, a => a.Scope),
            IgnoredActions = descriptors.ActionsNotAuthorized.Select(a => a.DisplayName).Order().ToArray(),
        };

        await Verify(snapshot);
    }
}
