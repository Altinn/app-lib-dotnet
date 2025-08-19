using System;
using Altinn.App.Core.Features;
using Altinn.App.Core.Helpers.DataModel;
using Altinn.App.SourceGenerator.Integration.Tests.Models;

namespace Altinn.App.SourceGenerator.Integration.Tests.UnitTest;

public class TestGeneratedGetter
{
    private readonly Skjema _skjema = new Skjema()
    {
        Skjemanummer = "1243",
        Skjemaversjon = "x4",
        Skjemainnhold =
        [
            new SkjemaInnhold()
            {
                Navn = "navn",
                Alder = 42,
                Deltar = true,
            },
            new SkjemaInnhold()
            {
                Navn = "navn2",
                Alder = 43,
                Deltar = false,
                Adresse = new() { Gate = "gate", Postnummer = 1234 },
                TidligereAdresse =
                [
                    new() { Gate = "gate1", Postnummer = 1235 },
                    new() { Gate = "gate2", Postnummer = 1236 },
                ],
            },
        ],
    };

    [Theory]
    [InlineData("skjemanummer", "1243")]
    [InlineData("skjemaversjon", "x4")]
    [InlineData("skjemainnhold[0].altinnRowId", "00000000-0000-0000-0000-000000000000")]
    [InlineData("skjemainnhold[0].navn", "navn")]
    [InlineData("skjemainnhold[0].alder", 42)]
    [InlineData("skjemainnhold[0].deltar", true)]
    [InlineData("skjemainnhold[1].altinnRowId", "00000000-0000-0000-0000-000000000000")]
    [InlineData("skjemainnhold[1].navn", "navn2")]
    [InlineData("skjemainnhold[1].alder", 43)]
    [InlineData("skjemainnhold[1].deltar", false)]
    [InlineData("skjemainnhold[1].tidligere-adresse[1].postnummer", 1236)]
    public void TestGetRaw(string path, object expected)
    {
        IFormDataWrapper dataWrapper = new Altinn_App_SourceGenerator_Integration_Tests_Models_SkjemaFormDataWrapper(
            _skjema
        );
        var actual = dataWrapper.Get(path);
        if (actual is Guid guid && expected is string stringGuid)
        {
            Assert.Equal(Guid.Parse(stringGuid), guid);
        }
        else
        {
            Assert.Equal(expected, actual);
        }

        var reflector = new ReflectionFormDataWrapper(_skjema);
        var reflectorActual = reflector.Get(path);
        if (reflectorActual is Guid rGuid && expected is string rstringGuid)
        {
            Assert.Equal(Guid.Parse(rstringGuid), rGuid);
        }
        else
        {
            Assert.Equal(expected, reflectorActual);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("skjemanummer.not-exists")]
    [InlineData("skjemanummer[4].not-exists")]
    [InlineData("not-exists")]
    [InlineData("skjemainnhold[2]")]
    [InlineData("skjemainnhold[4]")]
    [InlineData("skjemainnhold[2].navn")]
    [InlineData("skjemainnhold[0].not-exists")]
    [InlineData("skjemainnhold[0].navn.not-exists")]
    [InlineData("skjemainnhold[0].adresse.gate")]
    public void TestGetRawErrorReturnNull(string? path)
    {
        // These might all throw exceptions when we have better validation of data model bindings at startup
        IFormDataWrapper dataWrapper = new Altinn_App_SourceGenerator_Integration_Tests_Models_SkjemaFormDataWrapper(
            _skjema
        );
        Assert.Null(dataWrapper.Get(path));

        var reflector = new ReflectionFormDataWrapper(_skjema);
        Assert.Null(reflector.Get(path));
    }

    // [Fact]
    // public void TestPathHelper()
    // {
    //     var path = "skjemainnhold.navn";
    //     var segment = Altinn_App_SourceGenerator_Integration_Tests_models_SkjemaFormDataWrapper.GetNextSegment(
    //         path,
    //         0,
    //         out int nextOffset
    //     );
    //     Assert.Equal("skjemainnhold", segment);
    //     Assert.Equal(14, nextOffset);
    //     segment = Altinn_App_SourceGenerator_Integration_Tests_models_SkjemaFormDataWrapper.GetNextSegment(
    //         path,
    //         nextOffset,
    //         out nextOffset
    //     );
    //     Assert.Equal("navn", segment);
    //     Assert.Equal(-1, nextOffset);
    //     Assert.Throws<ArgumentOutOfRangeException>(
    //         () =>
    //             Altinn_App_SourceGenerator_Integration_Tests_models_SkjemaFormDataWrapper.GetNextSegment(path, nextOffset, out nextOffset)
    //     );
    // }
    //
    // [Fact]
    // public void TestAltinn_App_SourceGenerator_Integration_Tests_models_SkjemaFormDataWrapperRecursive()
    // {
    //     var path = "a.b[3].c";
    //     var segment = Altinn_App_SourceGenerator_Integration_Tests_models_SkjemaFormDataWrapper.GetNextSegment(
    //         path,
    //         0,
    //         out int nextOffset
    //     );
    //     Assert.Equal("a", segment);
    //     Assert.Equal(2, nextOffset);
    //     segment = Altinn_App_SourceGenerator_Integration_Tests_models_SkjemaFormDataWrapper.GetNextSegment(
    //         path,
    //         nextOffset,
    //         out nextOffset
    //     );
    //     Assert.Equal("b", segment);
    //     Assert.Equal(4, nextOffset);
    //     var index = Altinn_App_SourceGenerator_Integration_Tests_models_SkjemaFormDataWrapper.GetIndex(path, nextOffset, out nextOffset);
    //     Assert.Equal(3, index);
    //     segment = Altinn_App_SourceGenerator_Integration_Tests_models_SkjemaFormDataWrapper.GetNextSegment(
    //         path,
    //         nextOffset,
    //         out nextOffset
    //     );
    //     Assert.Equal("c", segment);
    //     Assert.Equal(-1, nextOffset);
    //
    //     Assert.Throws<ArgumentOutOfRangeException>(
    //         () =>
    //             Altinn_App_SourceGenerator_Integration_Tests_models_SkjemaFormDataWrapper.GetNextSegment(path, nextOffset, out nextOffset)
    //     );
    // }
}
