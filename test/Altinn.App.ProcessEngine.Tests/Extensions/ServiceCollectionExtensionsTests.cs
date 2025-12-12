using System.Text;
using System.Text.Json;
using Altinn.App.ProcessEngine.Constants;
using Altinn.App.ProcessEngine.Extensions;
using Altinn.App.ProcessEngine.Models;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Altinn.App.ProcessEngine.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public async Task AddProcessEngine_AddsRequiredServices()
    {
        await using var fixture = TestFixture.Create();
        Assert.NotNull(fixture.ProcessEngine);
        Assert.NotNull(fixture.ProcessEngineTaskHandler);
        Assert.NotNull(fixture.ProcessEngineHost);
        Assert.NotNull(fixture.ProcessEngineSettings);

        Assert.Equal(new ProcessEngineSettings(), fixture.ProcessEngineSettings);
        Assert.Equal(Defaults.ApiKey, fixture.ProcessEngineSettings.ApiKey);
    }

    [Fact]
    public async Task ConfigureProcessEngine_Delegate_OverridesDefault()
    {
        await using var fixture = TestFixture.Create(builder =>
            builder.Services.ConfigureProcessEngine(options =>
            {
                options.ApiKey = "override";
                options.QueueCapacity = 5;
                options.DefaultTaskExecutionTimeout = TimeSpan.FromDays(365);
                options.DefaultTaskRetryStrategy = ProcessEngineRetryStrategy.None();
            })
        );

        Assert.Equal("override", fixture.ProcessEngineSettings.ApiKey);
        Assert.Equal(5, fixture.ProcessEngineSettings.QueueCapacity);
        Assert.Equal(TimeSpan.FromDays(365), fixture.ProcessEngineSettings.DefaultTaskExecutionTimeout);
        Assert.Equal(ProcessEngineRetryStrategy.None(), fixture.ProcessEngineSettings.DefaultTaskRetryStrategy);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ConfigureProcessEngine_SectionPath_OverridesDefault(bool addBeforeEngine)
    {
        var settings = new ProcessEngineSettings
        {
            ApiKey = "override",
            QueueCapacity = 5,
            DefaultTaskExecutionTimeout = TimeSpan.FromDays(365),
            DefaultTaskRetryStrategy = ProcessEngineRetryStrategy.None(),
        };
        await using var fixture = TestFixture.Create(
            builder =>
            {
                if (addBeforeEngine)
                {
                    builder.Services.ConfigureProcessEngine("CustomConfigPath");
                    builder.Configuration.AddJsonStream(GetJsonStream("CustomConfigPath", settings));
                    builder.Services.AddProcessEngine();
                }
                else
                {
                    builder.Services.AddProcessEngine();
                    builder.Services.ConfigureProcessEngine("CustomConfigPath");
                    builder.Configuration.AddJsonStream(GetJsonStream("CustomConfigPath", settings));
                }
            },
            autoAddProcessEngine: false
        );

        Assert.Equal(settings, fixture.ProcessEngineSettings);
    }

    private static Stream GetJsonStream(string key, object data)
    {
        var dict = new Dictionary<string, object> { { key, data } };
        var json = JsonSerializer.Serialize(dict);
        return new MemoryStream(Encoding.UTF8.GetBytes(json));
    }
}
