using Altinn.App.Core.Models;
using FluentAssertions;

namespace Altinn.App.Core.Tests.Models;

public class ApplicationMetdataTests
{
    [Fact]
    public void ConstructorSetIdAndAppIdentifier()
    {
        ApplicationMetadata metadata = new ApplicationMetadata("ttd/test");
        metadata.Id.Should().BeEquivalentTo("ttd/test");
        AppIdentifier expected = new AppIdentifier("ttd/test");
        metadata.AppIdentifier.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void UpdatingIdUpdatesAppIdentifier()
    {
        ApplicationMetadata metadata = new ApplicationMetadata("ttd/test");
        metadata.Id.Should().BeEquivalentTo("ttd/test");
        AppIdentifier expected = new AppIdentifier("ttd/test");
        metadata.AppIdentifier.Should().BeEquivalentTo(expected);
        metadata.Id = "ttd/updated";
        metadata.Id.Should().BeEquivalentTo("ttd/updated");
        AppIdentifier expectedUpdate = new AppIdentifier("ttd/updated");
        metadata.AppIdentifier.Should().BeEquivalentTo(expectedUpdate);
    }

    [Fact]
    public void UpdatingIdFailsIfInvalidApplicationIdFormat()
    {
        ApplicationMetadata metadata = new ApplicationMetadata("ttd/test");
        metadata.Id.Should().BeEquivalentTo("ttd/test");
        AppIdentifier expected = new AppIdentifier("ttd/test");
        metadata.AppIdentifier.Should().BeEquivalentTo(expected);
        Assert.Throws<ArgumentOutOfRangeException>(() => metadata.Id = "invalid");
        metadata.AppIdentifier.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("8.6.0", "8.6.0.0")]
    [InlineData("v8.6.0", "8.6.0.0")]
    [InlineData("v8.6.0.0", "8.6.0.0")]
    [InlineData("V8.6.0.0", "8.6.0.0")]
    [InlineData("8.6.0.0", "8.6.0.0")]
    [InlineData("8.5.0-rc.9.signing-notification.2ae9d133", "8.5.0.9")]
    [InlineData("8.5.0-rc.9.signing-notification.2ae9d133+312rwelqdfj019235rds", "8.5.0.9")]
    [InlineData("4.14.0-preview.1", "4.14.0.1")]
    [InlineData("v8.5.0-rc.9.signing-notification.2ae9d133", "8.5.0.9")]
    [InlineData("v8.5.0-rc.9.signing-notification.2ae9d133+312rwelqdfj019235rds", "8.5.0.9")]
    [InlineData("v4.14.0-preview.1", "4.14.0.1")]
    [InlineData("V8.5.0-rc.9.signing-notification.2ae9d133", "8.5.0.9")]
    [InlineData("V8.5.0-rc.9.signing-notification.2ae9d133+312rwelqdfj019235rds", "8.5.0.9")]
    [InlineData("V4.14.0-preview.1", "4.14.0.1")]
    [InlineData("v8.6.0.1", "8.6.0.1")]
    [InlineData("V8.6.0.1", "8.6.0.1")]
    [InlineData("8.6.0.1", "8.6.0.1")]
    [InlineData("V10.10.10-rc.10.signing-notification.2ae9d133+312rwelqdfj019235rds", "10.10.10.10")]
    [InlineData("V10.10.10-preview.10", "10.10.10.10")]
    [InlineData("V10.10.10", "10.10.10.0")]
    [InlineData("V10.10.10.10", "10.10.10.10")]
    [InlineData("V01.01.01-rc.01.signing-notification.2ae9d133+312rwelqdfj019235rds", "01.01.01.01")]
    [InlineData("V01.01.01-preview.01", "01.01.01.01")]
    [InlineData("V01.01.01", "01.01.01.0")]
    [InlineData("V01.01.01.01", "01.01.01.01")]
    [InlineData("V01.01.01.", null)]
    [InlineData("V01.01.", null)]
    [InlineData("V01.01...", null)]
    [InlineData("V01.01.01.01.01", null)]
    [InlineData("V01.01.01-preview.0a1", null)]
    [InlineData("8.6.0.a1", null)]
    [InlineData("V01.0a1.01-preview.0a1", null)]
    [InlineData("V01.01..01.01", null)]
    [InlineData("V01.a01.01.01", null)]
    [InlineData("Va01.a01.01.01", null)]
    [InlineData("V", null)]
    [InlineData("1", null)]
    [InlineData("V1", null)]
    [InlineData("V12", null)]
    [InlineData(" ", null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public void AltinnNuGetVersion(string? input, string? expected)
    {
        if (expected is not null)
        {
            var result = ApplicationMetadata.GetStandardizedVersion(input!);
            Assert.Equal(expected, result);
        }
        else
        {
            Assert.ThrowsAny<ArgumentException>(() => ApplicationMetadata.GetStandardizedVersion(input!));
        }
    }
}
