using Altinn.App.Core.Features;
using Altinn.App.Core.Helpers.DataModel;

namespace Altinn.App.SourceGenerator.Tests;

public class TestAddIndexToPath
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Run(bool reflection)
    {
        var data = new Skjema();

        IFormDataWrapper wrapper = reflection
            ? new ReflectionFormDataWrapper(data)
            : new Altinn_App_SourceGenerator_Tests_SkjemaFormDataWrapper(data);
        Assert.Equal("skjemainnhold[214].navn", wrapper.AddIndexToPath("skjemainnhold.navn", [214, 33]));
        Assert.Equal("skjemainnhold[214].alder", wrapper.AddIndexToPath("skjemainnhold.alder", [214]));
        Assert.Null(wrapper.AddIndexToPath("skjemainnhold.finnes-ikke", [214, 1]));
        Assert.Equal(
            "skjemainnhold[2147483647].adresse.gate",
            wrapper.AddIndexToPath("skjemainnhold.adresse.gate", [int.MaxValue])
        );
        Assert.Equal(
            "skjemainnhold[0].tidligere-adresse[4].gate",
            wrapper.AddIndexToPath("skjemainnhold.tidligere-adresse.gate", [0, 4])
        );
    }
}
