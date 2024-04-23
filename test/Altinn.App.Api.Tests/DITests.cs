
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights;
using System.Diagnostics.Tracing;
using Altinn.App.Core.Features;

namespace Altinn.App.Api.Tests;

public class DITests
{
    private sealed record FakeWebHostEnvironment : IWebHostEnvironment, IHostingEnvironment
    {
        private string _env = "";

        public string WebRootPath { get => new DirectoryInfo("./").FullName; set => throw new NotImplementedException(); }
        public IFileProvider WebRootFileProvider { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string ApplicationName { get => "test"; set => throw new NotImplementedException(); }
        public IFileProvider ContentRootFileProvider { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string ContentRootPath { get => new DirectoryInfo("./").FullName; set => throw new NotImplementedException(); }
        public string EnvironmentName { get => _env; set => _env = value; }
    }

    private sealed class AppInsightsListener : EventListener
    {
        private readonly List<EventSource> _eventSources = [];
        public readonly List<EventWrittenEventArgs> Events = [];

        protected override void OnEventSourceCreated(EventSource eventSource)
        {

            if (eventSource.Name == "Microsoft-ApplicationInsights-AspNetCore")
            {
                _eventSources.Add(eventSource);
                EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All);
            }

            base.OnEventSourceCreated(eventSource);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.EventSource.Name != "Microsoft-ApplicationInsights-AspNetCore")
            {
                return;
            }

            Events.Add(eventData);
            base.OnEventWritten(eventData);
        }

        public override void Dispose()
        {
            foreach (var eventSource in _eventSources)
            {
                DisableEvents(eventSource);
            }
            base.Dispose();
        }
    }

    [Fact]
    public void AppInsights_Registers_Correctly()
    {
        using var listener = new AppInsightsListener();

        var services = new ServiceCollection();
        var env = new FakeWebHostEnvironment { EnvironmentName = "Development" };

        services.AddSingleton<IWebHostEnvironment>(env);
        services.AddSingleton<IHostingEnvironment>(env);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection([
                new KeyValuePair<string, string?>("ApplicationInsights:InstrumentationKey", "test")
            ]).Build();

        Extensions.ServiceCollectionExtensions.AddAltinnAppServices(services, config, env);

        using (var sp = services.BuildServiceProvider())
        {
            var telemetryConfig = sp.GetRequiredService<TelemetryConfiguration>();
            Assert.NotNull(telemetryConfig);

            var client = sp.GetRequiredService<TelemetryClient>();
            Assert.NotNull(client);
            client.Flush();
        }

        EventLevel[] errorLevels = [EventLevel.Error, EventLevel.Critical];
        Assert.Empty(listener.Events.Where(e => errorLevels.Contains(e.Level)));
    }

    [Fact]
    public void OpenTelemetry_Registers_Correctly()
    {
        var services = new ServiceCollection();
        var env = new FakeWebHostEnvironment { EnvironmentName = "Development" };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:UseOpenTelemetry"] = "true"
            })
            .Build();

        Extensions.ServiceCollectionExtensions.AddAltinnAppServices(services, config, env);

        using var sp = services.BuildServiceProvider();

        var telemetry = sp.GetService<Telemetry>();
        Assert.NotNull(telemetry);
    }

    [Fact]
    public void UseOpenTelemetry_WhenFalse_RegistersApplicationInsights()
    {
        var services = new ServiceCollection();
        var env = new FakeWebHostEnvironment { EnvironmentName = "Development" };

        services.AddSingleton<IWebHostEnvironment>(env);
        services.AddSingleton<IHostingEnvironment>(env);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:UseOpenTelemetry"] = "false",
                ["ApplicationInsights:InstrumentationKey"] = "test"
            })
            .Build();

        Extensions.ServiceCollectionExtensions.AddAltinnAppServices(services, config, env);

        using var sp = services.BuildServiceProvider();

        // OTEL specific services should NOT be registered
        var telemetry = sp.GetService<Telemetry>();
        Assert.Null(telemetry);

        // Application Insight services should be registered
        var telemetryConfig = sp.GetRequiredService<TelemetryConfiguration>();
        Assert.NotNull(telemetryConfig);

        var client = sp.GetRequiredService<TelemetryClient>();
        Assert.NotNull(client);
    }

    [Fact]
    public void UseOpenTelemetry_WhenNotParsable_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var env = new FakeWebHostEnvironment { EnvironmentName = "Development" };

        // Set a non-boolean value for the UseOpenTelemetry setting
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:UseOpenTelemetry"] = "not_a_boolean"
            })
            .Build();

        var exception = Assert.Throws<ArgumentException>(() =>
            Extensions.ServiceCollectionExtensions.AddAltinnAppServices(services, config, env)
        );

        Assert.Contains("UseOpenTelemetry must be boolean or not set", exception.Message);
    }
}