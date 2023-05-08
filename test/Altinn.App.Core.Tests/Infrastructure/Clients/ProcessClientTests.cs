using Altinn.App.Core.Configuration;
using Altinn.App.Core.Infrastructure.Clients.Storage;
using Altinn.App.Core.Interface;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Altinn.App.Core.Tests.Infrastructure.Clients;

public class ProcessClientTests: IDisposable
{
    private readonly Mock<IInstanceEvent> _instanceEventClientMock = new Mock<IInstanceEvent>();
    private readonly Mock<HttpClient> _httpClientMock = new Mock<HttpClient>();
    
    [Fact]
    public async Task DispatchProcessEventsToStorage_does_not_save_events_if_list_is_null()
    {
        IProcess processClient = GetProcessClient();
        await processClient.DispatchProcessEventsToStorage(
            new Instance()
            {
                Org = "ttd",
                AppId = "1337/aaaa-bbbbb-dddd-eeee"
            },
            null);
    }
    
    [Fact]
    public async Task DispatchProcessEventsToStorage_sends_events_to_instanceEventClient()
    {
        IProcess processClient = GetProcessClient();
        var instanceEvent = new InstanceEvent();
        await processClient.DispatchProcessEventsToStorage(
            new Instance()
            {
                Org = "ttd",
                AppId = "ttd/demo-app"
            },
            new List<InstanceEvent>()
            {
                instanceEvent
            });
        _instanceEventClientMock.Verify(i => i.SaveInstanceEvent(instanceEvent, "ttd", "demo-app"));
    }

    private ProcessClient GetProcessClient()
    {
        var platformSettings = Options.Create(new PlatformSettings());
        var appSettings = Options.Create(new AppSettings());
        var logger = new NullLogger<ProcessClient>();
        var httpContextAccessor = new Mock<IHttpContextAccessor>();

        return new ProcessClient(platformSettings, appSettings, _instanceEventClientMock.Object, logger, httpContextAccessor.Object, _httpClientMock.Object);
    }

    public void Dispose()
    {
        _instanceEventClientMock.VerifyNoOtherCalls();
        _httpClientMock.VerifyNoOtherCalls();
    }
}
