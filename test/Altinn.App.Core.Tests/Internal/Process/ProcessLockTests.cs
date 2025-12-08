using System.Net;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Infrastructure.Clients.Storage;
using Altinn.App.Core.Internal.Process.ProcessLock;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WireMock.Matchers.Request;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Altinn.App.Core.Tests.Internal.Process;

public sealed class ProcessLockTests
{
    private sealed record Fixture(WireMockServer Server, ServiceProvider ServiceProvider) : IDisposable
    {
        public readonly Guid InstanceGuid = Guid.NewGuid();
        public readonly int InstanceOwnerPartyId = 12345;
        private const string RuntimeCookieName = "test-cookie";
        private const string BearerToken = "test-token";

        public readonly string ServerUrl = Server.Url ?? throw new Exception("Missing server URL");

        public static Fixture Create(Action<IServiceCollection>? registerCustomServices = null)
        {
            var server = WireMockServer.Start();

            var services = new ServiceCollection();

            services.Configure<PlatformSettings>(settings =>
            {
                var testUrl = server.Url ?? throw new Exception("Missing server URL");
                settings.ApiStorageEndpoint = testUrl + new Uri(settings.ApiStorageEndpoint).PathAndQuery;
            });

            services.Configure<AppSettings>(settings => settings.RuntimeCookieName = RuntimeCookieName);

            services.AddHttpClient<ProcessLockClient>();

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers.Cookie = $"{RuntimeCookieName}={BearerToken}";

            var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
            services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);

            services.AddTransient<ProcessLocker, ProcessLocker>();

            registerCustomServices?.Invoke(services);

            var serviceProvider = services.BuildServiceProvider();

            return new Fixture(server, serviceProvider);
        }

        public void Dispose()
        {
            Server.Stop();
            Server.Dispose();
            ServiceProvider.Dispose();
        }

        public ProcessLocker GetProcessLocker()
        {
            return ServiceProvider.GetRequiredService<ProcessLocker>();
        }

        public Instance CreateInstance()
        {
            return new Instance { Id = $"{InstanceOwnerPartyId}/{InstanceGuid}" };
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

        var testRequestBuilder = Request.Create().WithPath($"/test").UsingGet();

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

        fixture.Server.Given(testRequestBuilder).RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK));

        var httpClient = fixture.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient();

        var processLocker = fixture.GetProcessLocker();
        var instance = fixture.CreateInstance();

        await using (var _ = await processLocker.AcquireAsync(instance))
        {
            using var response = await httpClient.GetAsync($"{fixture.ServerUrl}/test");
            response.EnsureSuccessStatusCode();
        }

        var requests = fixture.Server.LogEntries;
        Assert.Equal(3, requests.Count);

        var acquireMatchResult = new RequestMatchResult();
        acquireLockRequestBuilder.GetMatchingScore(requests[0].RequestMessage, acquireMatchResult);
        Assert.True(acquireMatchResult.IsPerfectMatch);

        var testMatchResult = new RequestMatchResult();
        testRequestBuilder.GetMatchingScore(requests[1].RequestMessage, testMatchResult);
        Assert.True(testMatchResult.IsPerfectMatch);

        var releaseMatchResult = new RequestMatchResult();
        releaseLockRequestBuilder.GetMatchingScore(requests[2].RequestMessage, releaseMatchResult);
        Assert.True(releaseMatchResult.IsPerfectMatch);
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

        var processLocker = fixture.GetProcessLocker();
        var instance = fixture.CreateInstance();

        await Assert.ThrowsAsync<Exception>(async () =>
        {
            await using var _ = await processLocker.AcquireAsync(instance);
            throw new Exception();
        });

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

        var processLocker = fixture.GetProcessLocker();
        var instance = fixture.CreateInstance();

        await using (var _ = await processLocker.AcquireAsync(instance)) { }

        var acquireRequests = fixture.Server.FindLogEntries(fixture.GetAcquireLockRequestBuilder());
        Assert.Single(acquireRequests);
        var requestBody = acquireRequests[0].RequestMessage.Body;

        await Verify(new { RequestBody = requestBody });
    }

    [Fact]
    public async Task LockReleaseFailure_DoesNotThrow()
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

        var processLocker = fixture.GetProcessLocker();
        var instance = fixture.CreateInstance();

        await using (var _ = await processLocker.AcquireAsync(instance)) { }

        var releaseRequests = fixture.Server.FindLogEntries(fixture.GetReleaseLockRequestBuilder(lockId));
        Assert.Single(releaseRequests);
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task StorageApiError_ThrowsCorrectPlatformHttpException(HttpStatusCode storageStatusCode)
    {
        using var fixture = Fixture.Create();

        fixture
            .Server.Given(fixture.GetAcquireLockRequestBuilder())
            .RespondWith(Response.Create().WithStatusCode(storageStatusCode));

        var processLocker = fixture.GetProcessLocker();
        var instance = fixture.CreateInstance();

        var exception = await Assert.ThrowsAsync<PlatformHttpResponseSnapshotException>(async () =>
        {
            await using var _ = await processLocker.AcquireAsync(instance);
        });

        Assert.Single(fixture.Server.LogEntries);

        await Verify(new { Exception = exception })
            .UseParameters(storageStatusCode)
            .IgnoreMember<PlatformHttpResponseSnapshotException>(x => x.Headers);
    }

    [Fact]
    public async Task NullResponseBody_ThrowsPlatformHttpException()
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

        var processLocker = fixture.GetProcessLocker();
        var instance = fixture.CreateInstance();

        var exception = await Assert.ThrowsAsync<PlatformHttpResponseSnapshotException>(async () =>
        {
            await using var _ = await processLocker.AcquireAsync(instance);
        });

        Assert.Single(fixture.Server.LogEntries);

        await Verify(new { Exception = exception }).IgnoreMember<PlatformHttpResponseSnapshotException>(x => x.Headers);
    }

    [Fact]
    public async Task EmptyJsonResponseBody_ThrowsPlatformHttpException()
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

        var processLocker = fixture.GetProcessLocker();
        var instance = fixture.CreateInstance();

        var exception = await Assert.ThrowsAsync<PlatformHttpResponseSnapshotException>(async () =>
        {
            await using var _ = await processLocker.AcquireAsync(instance);
        });

        Assert.Single(fixture.Server.LogEntries);

        await Verify(new { Exception = exception }).IgnoreMember<PlatformHttpResponseSnapshotException>(x => x.Headers);
    }

    [Fact]
    public async Task InvalidInstanceId_ThrowsArgumentException()
    {
        using var fixture = Fixture.Create();

        var processLocker = fixture.GetProcessLocker();
        var instance = new Instance { Id = "invalid-format" };

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await using var _ = await processLocker.AcquireAsync(instance);
        });

        Assert.Empty(fixture.Server.LogEntries);
        await Verify(new { Exception = exception }).IgnoreMember<PlatformHttpResponseSnapshotException>(x => x.Headers);
    }
}
