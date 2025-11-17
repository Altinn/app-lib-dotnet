using System.Net;
using System.Net.Http.Json;
using Altinn.App.Api.Infrastructure.Middleware;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Infrastructure.Clients.Storage;
using Altinn.App.Core.Internal.Process.ProcessLock;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WireMock.Matchers.Request;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Altinn.App.Api.Tests.Middleware;

public sealed class ProcessLockMiddlewareTests
{
    private sealed record Fixture(IHost Host, WireMockServer Server) : IDisposable
    {
        public readonly Guid InstanceGuid = Guid.NewGuid();
        public readonly int InstanceOwnerPartyId = 12345;
        private const string RuntimeCookieName = "test-cookie";
        private const string BearerToken = "test-token";

        public static Fixture Create(Action<IServiceCollection>? registerCustomAppServices = null)
        {
            var server = WireMockServer.Start();

            var host = new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder
                        .UseTestServer()
                        .ConfigureServices(services =>
                        {
                            services.AddRouting();

                            services.Configure<PlatformSettings>(settings =>
                            {
                                var testUrl = server.Url ?? throw new Exception("Missing server URL");
                                settings.ApiStorageEndpoint =
                                    testUrl + new Uri(settings.ApiStorageEndpoint).PathAndQuery;
                            });

                            services.Configure<AppSettings>(settings => settings.RuntimeCookieName = RuntimeCookieName);

                            services.AddHttpClient<ProcessLockClient>();

                            services.AddHttpContextAccessor();

                            registerCustomAppServices?.Invoke(services);
                        })
                        .Configure(app =>
                        {
                            app.UseRouting();
                            app.UseMiddleware<ProcessLockMiddleware>();
                            app.UseEndpoints(endpoints =>
                            {
                                // Endpoint with lock
                                endpoints
                                    .MapGet(
                                        "/instances/{instanceOwnerPartyId:int}/{instanceGuid:guid}/test",
                                        () => "Success"
                                    )
                                    .WithMetadata(new EnableProcessLockAttribute());

                                // Endpoint without lock
                                endpoints.MapGet("/without-lock", () => "No lock required");

                                // Endpoint with lock that throws exception
                                endpoints
                                    .MapGet(
                                        "/instances/{instanceOwnerPartyId:int}/{instanceGuid:guid}/test-exception",
                                        _ => throw new InvalidOperationException("Test exception")
                                    )
                                    .WithMetadata(new EnableProcessLockAttribute());

                                // Endpoint with lock but no route parameters
                                endpoints
                                    .MapGet("/invalid-route", () => Results.Ok())
                                    .WithMetadata(new EnableProcessLockAttribute());
                            });
                        });
                })
                .Start();

            return new Fixture(host, server);
        }

        public void Dispose()
        {
            Server.Stop();
            Server.Dispose();
            Host.Dispose();
        }

        public HttpClient GetTestClient()
        {
            var httpClient = Host.GetTestClient();
            httpClient.DefaultRequestHeaders.Add("cookie", $"{RuntimeCookieName}={BearerToken}");

            return httpClient;
        }

        public IRequestBuilder GetAcquireLockRequestBuilder()
        {
            return Request
                .Create()
                .WithPath($"/storage/api/v1/instances/{InstanceOwnerPartyId}/{InstanceGuid}/process/lock")
                .UsingPost()
                .WithHeader("Authorization", $"Bearer {BearerToken}");
        }

        public IRequestBuilder GetReleaseLockRequestBuilder(Guid lockId)
        {
            return Request
                .Create()
                .WithPath($"/storage/api/v1/instances/{InstanceOwnerPartyId}/{InstanceGuid}/process/lock/{lockId}")
                .UsingPatch()
                .WithHeader("Authorization", $"Bearer {BearerToken}");
        }
    }

    [Fact]
    public async Task HappyPath()
    {
        using var fixture = Fixture.Create();

        var lockId = Guid.NewGuid();

        var acquireLockRequestBuilder = fixture.GetAcquireLockRequestBuilder();
        var releaseLockRequestBuilder = fixture.GetReleaseLockRequestBuilder(lockId);

        fixture
            .Server.Given(acquireLockRequestBuilder)
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithBodyAsJson(new ProcessLockResponse { LockId = lockId })
            );

        fixture
            .Server.Given(releaseLockRequestBuilder)
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK));

        var response = await fixture
            .GetTestClient()
            .GetAsync($"/instances/{fixture.InstanceOwnerPartyId}/{fixture.InstanceGuid}/test");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Success", content);

        var requests = fixture.Server.LogEntries;
        Assert.Equal(2, requests.Count);

        var acquireMatchResult = new RequestMatchResult();
        acquireLockRequestBuilder.GetMatchingScore(requests[0].RequestMessage, acquireMatchResult);
        Assert.True(acquireMatchResult.IsPerfectMatch);

        var releaseMatchResult = new RequestMatchResult();
        releaseLockRequestBuilder.GetMatchingScore(requests[1].RequestMessage, releaseMatchResult);
        Assert.True(releaseMatchResult.IsPerfectMatch);
    }

    [Fact]
    public async Task EndpointWithoutAttribute_SkipsMiddleware()
    {
        using var fixture = Fixture.Create();

        var response = await fixture.GetTestClient().GetAsync("/without-lock");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("No lock required", content);

        Assert.Empty(fixture.Server.LogEntries);
    }

    [Fact]
    public async Task LockReleasedOnException()
    {
        using var fixture = Fixture.Create();

        var lockId = Guid.NewGuid();

        fixture
            .Server.Given(fixture.GetAcquireLockRequestBuilder())
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithBodyAsJson(new ProcessLockResponse { LockId = lockId })
            );

        fixture
            .Server.Given(fixture.GetReleaseLockRequestBuilder(lockId))
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await fixture
                .GetTestClient()
                .GetAsync($"/instances/{fixture.InstanceOwnerPartyId}/{fixture.InstanceGuid}/test-exception")
        );

        var releaseRequests = fixture.Server.FindLogEntries(fixture.GetReleaseLockRequestBuilder(lockId));
        Assert.Single(releaseRequests);
    }

    [Fact]
    public async Task CustomExpirationConfiguration_UsedInStorageApiCall()
    {
        var lockId = Guid.NewGuid();
        const int customExpirationSeconds = 120;

        using var fixture = Fixture.Create(services =>
            services.Configure<ProcessLockOptions>(options =>
                options.Expiration = TimeSpan.FromSeconds(customExpirationSeconds)
            )
        );

        fixture
            .Server.Given(fixture.GetAcquireLockRequestBuilder())
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithBodyAsJson(new ProcessLockResponse { LockId = lockId })
            );

        fixture
            .Server.Given(fixture.GetReleaseLockRequestBuilder(lockId))
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK));

        var response = await fixture
            .GetTestClient()
            .GetAsync($"/instances/{fixture.InstanceOwnerPartyId}/{fixture.InstanceGuid}/test");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var acquireRequests = fixture.Server.FindLogEntries(fixture.GetAcquireLockRequestBuilder());
        Assert.Single(acquireRequests);
        var requestBody = acquireRequests[0].RequestMessage.Body;
        Assert.Contains($"\"expiration\":{customExpirationSeconds}", requestBody);
    }

    [Fact]
    public async Task MissingRouteParameters_ThrowsException()
    {
        using var fixture = Fixture.Create();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await fixture.GetTestClient().GetAsync("/invalid-route")
        );

        Assert.Contains("Unable to extract instance identifiers.", exception.Message);

        Assert.Empty(fixture.Server.LogEntries);
    }

    [Fact]
    public async Task LockReleaseFailure_DoesNotAffectResponse()
    {
        using var fixture = Fixture.Create();

        var lockId = Guid.NewGuid();

        fixture
            .Server.Given(fixture.GetAcquireLockRequestBuilder())
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithBodyAsJson(new ProcessLockResponse { LockId = lockId })
            );

        fixture
            .Server.Given(fixture.GetReleaseLockRequestBuilder(lockId))
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.InternalServerError));

        var response = await fixture
            .GetTestClient()
            .GetAsync($"/instances/{fixture.InstanceOwnerPartyId}/{fixture.InstanceGuid}/test");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Success", content);

        var releaseRequests = fixture.Server.FindLogEntries(fixture.GetReleaseLockRequestBuilder(lockId));
        Assert.Single(releaseRequests);
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task StorageApiError_ReturnsCorrectStatusCode(HttpStatusCode storageStatusCode)
    {
        using var fixture = Fixture.Create();

        fixture
            .Server.Given(fixture.GetAcquireLockRequestBuilder())
            .RespondWith(Response.Create().WithStatusCode(storageStatusCode));

        var response = await fixture
            .GetTestClient()
            .GetAsync($"/instances/{fixture.InstanceOwnerPartyId}/{fixture.InstanceGuid}/test");

        Assert.Equal(storageStatusCode, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(problemDetails);
        Assert.Equal("Failed to acquire lock.", problemDetails.Title);
        Assert.Equal((int)storageStatusCode, problemDetails.Status);

        Assert.Single(fixture.Server.LogEntries);
    }

    [Fact]
    public async Task NullResponseBody_ReturnsProblemDetails()
    {
        using var fixture = Fixture.Create();

        fixture
            .Server.Given(fixture.GetAcquireLockRequestBuilder())
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody("null")
            );

        var response = await fixture
            .GetTestClient()
            .GetAsync($"/instances/{fixture.InstanceOwnerPartyId}/{fixture.InstanceGuid}/test");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(problemDetails);
        Assert.Equal("Failed to acquire lock.", problemDetails.Title);
        Assert.Equal("The response from the lock acquisition endpoint was not expected.", problemDetails.Detail);
        Assert.Equal((int)HttpStatusCode.InternalServerError, problemDetails.Status);

        Assert.Single(fixture.Server.LogEntries);
    }

    [Fact]
    public async Task EmptyJsonResponseBody_ReturnsProblemDetails()
    {
        using var fixture = Fixture.Create();

        fixture
            .Server.Given(fixture.GetAcquireLockRequestBuilder())
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody("{}")
            );

        var response = await fixture
            .GetTestClient()
            .GetAsync($"/instances/{fixture.InstanceOwnerPartyId}/{fixture.InstanceGuid}/test");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(problemDetails);
        Assert.Equal("Failed to acquire lock.", problemDetails.Title);
        Assert.Equal("The response from the lock acquisition endpoint was not expected.", problemDetails.Detail);
        Assert.Equal((int)HttpStatusCode.InternalServerError, problemDetails.Status);

        Assert.Single(fixture.Server.LogEntries);
    }
}
