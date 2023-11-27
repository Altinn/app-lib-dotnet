using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Options;
using Altinn.App.Core.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Altinn.App.PlatformServices.Tests.Options;

public class AppOptionsFactoryTests
{
    private readonly ServiceCollection _serviceCollection;

    public AppOptionsFactoryTests()
    {
        _serviceCollection = new ServiceCollection();
        _serviceCollection.AddTransient<IInstanceAppOptionsProvider, VehiclesInstanceAppOptionsProvider>();
        _serviceCollection.AddTransient<IAppOptionsProvider, CountryAppOptionsProvider>();
        _serviceCollection.AddTransient<AppOptionsFactory>();
        _serviceCollection.Configure<AppSettings>((o) =>
        {
            o.AppBasePath = "TODO";
            o.OptionsFolder = "TODO";
        });
    }

    [Fact]
    public async Task GetOptionsProvider_CustomProviderRegistrered_ShouldReturnProvider()
    {
        await using var serviceProvider = _serviceCollection.BuildServiceProvider();
        var factory = serviceProvider.GetService<AppOptionsFactory>();

        IAppOptionsProvider optionsProvider = factory.GetOptionsProvider("country");

        optionsProvider.Should().NotBeNull();
        optionsProvider!.Id.Should().Be("country");
        var list = await optionsProvider.GetAppOptionsAsync("nb", new Dictionary<string, string>());
        list.Should().NotBeNull();
        list.Options.Should().HaveCount(2);
        list.Options.Should().Contain(x => x.Label == "Norge");
    }

    [Fact]
    public void GetOptionsProvider_UnknownOptionlist_ShouldReturnNull()
    {
        using var serviceProvider = _serviceCollection.BuildServiceProvider();
        var factory = serviceProvider.GetService<AppOptionsFactory>();

        var action = () => factory.GetOptionsProvider("unknown");

        action.Should().Throw<DirectoryNotFoundException>($"{nameof(AppOptionsFactoryTests)} does not provide a valid config for the options folder");
    }

    [Fact]
    public void GetOptionsProvider_CustomOptionsProvider_ShouldReturnCustomType()
    {
        using var serviceProvider = _serviceCollection.BuildServiceProvider();
        var factory = serviceProvider.GetService<AppOptionsFactory>();

        IAppOptionsProvider optionsProvider = factory.GetOptionsProvider("country");

        optionsProvider.Should().BeOfType<CountryAppOptionsProvider>();
        optionsProvider!.Id.Should().Be("country");
    }

    [Fact]
    public void GetOptionsProvider_CustomOptionsProviderWithUpperCase_ShouldReturnCustomType()
    {
        using var serviceProvider = _serviceCollection.BuildServiceProvider();
        var factory = serviceProvider.GetService<AppOptionsFactory>();

        IAppOptionsProvider optionsProvider = factory.GetOptionsProvider("Country");

        optionsProvider.Should().BeOfType<CountryAppOptionsProvider>();
        optionsProvider!.Id.Should().Be("country");
    }

    [Fact]
    public async Task GetParameters_CustomOptionsProviderWithUpperCase_ShouldReturnCustomType()
    {
        using var serviceProvider = _serviceCollection.BuildServiceProvider();
        var factory = serviceProvider.GetService<AppOptionsFactory>();

        IAppOptionsProvider optionsProvider = factory.GetOptionsProvider("Country");

        optionsProvider.Should().NotBeNull();
        AppOptions options = await optionsProvider!.GetAppOptionsAsync("nb", new Dictionary<string, string>() { { "key", "value" } });
        options.Parameters.First(x => x.Key == "key").Value.Should().Be("value");
    }

    [Fact]
    public void GetInstanceOptionsProvider_CustomOptionsProvider_ShouldReturnCustomType()
    {
        using var serviceProvider = _serviceCollection.BuildServiceProvider();
        var factory = serviceProvider.GetService<AppOptionsFactory>();

        IInstanceAppOptionsProvider optionsProvider = factory.GetInstanceOptionsProvider("vehicles");

        optionsProvider.Should().BeOfType<VehiclesInstanceAppOptionsProvider>();
        optionsProvider!.Id.Should().Be("vehicles");
    }

    internal class CountryAppOptionsProvider : IAppOptionsProvider
    {
        public string Id { get; set; } = "country";

        public Task<AppOptions> GetAppOptionsAsync(string language, Dictionary<string, string> keyValuePairs)
        {
            var options = new AppOptions
            {
                Options = new List<AppOption>
                {
                    new AppOption
                    {
                        Label = "Norge",
                        Value = "47"
                    },
                    new AppOption
                    {
                        Label = "Sverige",
                        Value = "46"
                    }
                },

                Parameters = keyValuePairs
            };

            return Task.FromResult(options);
        }
    }

    public class VehiclesInstanceAppOptionsProvider : IInstanceAppOptionsProvider
    {
        public string Id => "vehicles";

        public Task<AppOptions> GetInstanceAppOptionsAsync(InstanceIdentifier instanceIdentifier, string language, Dictionary<string, string> keyValuePairs)
        {
            var options = new AppOptions
            {
                Options = new List<AppOption>
                {
                    new AppOption
                    {
                        Label = "Skoda Octavia 1.6",
                        Value = "DN49525"
                    },
                    new AppOption
                    {
                        Label = "e-Golf",
                        Value = "EK38470"
                    },
                    new AppOption
                    {
                        Label = "Tilhenger",
                        Value = "JT5817"
                    }
                }
            };

            return Task.FromResult(options);
        }
    }
}
