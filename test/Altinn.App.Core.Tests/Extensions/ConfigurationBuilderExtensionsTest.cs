using Altinn.App.Core.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration.Json;

namespace Altinn.App.Core.Tests.Extensions;

public class ConfigurationBuilderExtensionsTest
{
    [Fact]
    public void AddAppSettingsSecretFile_IsSafeToCallMultipleTimes()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        /*
            Must override root path to something valid,
            otherwise `AddAppSettingsSecretFile` will exit gracefully with noop.
         */
        ConfigurationBuilderExtensions.AppSettingsSecretsRoot = AppContext.BaseDirectory;

        // Act
        builder.Configuration.AddAppSettingsSecretFile();
        builder.Configuration.AddAppSettingsSecretFile();

        // Assert
        Assert.Single(
            builder.Configuration.Sources.OfType<JsonConfigurationSource>(),
            x => x.Path == ConfigurationBuilderExtensions.AppSettingsSecretsFile
        );
    }
}
