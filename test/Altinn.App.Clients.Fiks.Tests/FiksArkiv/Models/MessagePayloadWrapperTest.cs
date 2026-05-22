using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using KS.Fiks.Arkiv.Models.V1.Kodelister;

namespace Altinn.App.Clients.Fiks.Tests.FiksArkiv.Models;

public class MessagePayloadWrapperTest
{
    private static readonly Kode _dummyCode = new(".", string.Empty);

    [Theory]
    [InlineData("PDF/A")]
    [InlineData("XML")]
    [InlineData("anything-goes")]
    public void GetFileFormat_UsesOverride_WhenFormatCodeIsProvided(string formatCode)
    {
        var wrapper = new MessagePayloadWrapper(
            new FiksIOMessagePayload("document.pdf", Stream.Null),
            _dummyCode,
            FileFormatCode: formatCode
        );

        var result = wrapper.GetFileFormat();

        Assert.Equal(formatCode, result.KodeProperty);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetFileFormat_FallsBackToFileExtension_WhenFormatCodeIsMissing(string? formatCode)
    {
        var wrapper = new MessagePayloadWrapper(
            new FiksIOMessagePayload("report.pdf", Stream.Null),
            _dummyCode,
            FileFormatCode: formatCode
        );

        var result = wrapper.GetFileFormat();

        Assert.Equal("PDF", result.KodeProperty);
    }

    [Fact]
    public void GetFileFormat_FallsBackToUppercasedFilename_WhenNoExtensionAndNoOverride()
    {
        var wrapper = new MessagePayloadWrapper(
            new FiksIOMessagePayload("extensionless", Stream.Null),
            _dummyCode,
            FileFormatCode: null
        );

        var result = wrapper.GetFileFormat();

        Assert.Equal("EXTENSIONLESS", result.KodeProperty);
    }
}
