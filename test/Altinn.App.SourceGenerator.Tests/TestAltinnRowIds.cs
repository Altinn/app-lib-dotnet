using Altinn.App.Core.Features;
using Altinn.App.Core.Helpers.DataModel;

namespace Altinn.App.SourceGenerator.Tests;

public class TestAltinnRowIds
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
                TidligereAdresse =
                [
                    new Adresse()
                    {
                        Gate = "Gata",
                        Postnummer = 1245,
                        Poststed = "Sted",
                    },
                    new Adresse()
                    {
                        Gate = "Gata",
                        Postnummer = null,
                        Poststed = "Sted",
                    },
                ],
            },
            new SkjemaInnhold()
            {
                Navn = "navn2",
                Alder = 43,
                Deltar = false,
            },
        ],
    };

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TestAddAndRemoveAltinnRowId(bool reflection)
    {
        IFormDataWrapper dataWrapper = reflection
            ? new ReflectionFormDataWrapper(_skjema)
            : new Altinn_App_SourceGenerator_Tests_SkjemaFormDataWrapper(_skjema);

        Assert.Equal(Guid.Empty, _skjema.Skjemainnhold?[0]?.AltinnRowId);
        Assert.Equal(Guid.Empty, _skjema.Skjemainnhold?[1]?.AltinnRowId);
        Assert.Equal(Guid.Empty, _skjema.Skjemainnhold?[0]?.TidligereAdresse?[0].AltinnRowId);
        dataWrapper.InitializeAltinnRowIds();
        Assert.NotEqual(Guid.Empty, _skjema.Skjemainnhold?[0]?.AltinnRowId);
        Assert.NotEqual(Guid.Empty, _skjema.Skjemainnhold?[1]?.AltinnRowId);
        Assert.NotEqual(Guid.Empty, _skjema.Skjemainnhold?[0]?.TidligereAdresse?[0].AltinnRowId);
        dataWrapper.RemoveAltinnRowIds();
        Assert.Equal(Guid.Empty, _skjema.Skjemainnhold?[0]?.AltinnRowId);
        Assert.Equal(Guid.Empty, _skjema.Skjemainnhold?[1]?.AltinnRowId);
        Assert.Equal(Guid.Empty, _skjema.Skjemainnhold?[0]?.TidligereAdresse?[0].AltinnRowId);
        Assert.Equal("navn", _skjema.Skjemainnhold?[0]?.Navn);
        Assert.Equal("navn2", _skjema.Skjemainnhold?[1]?.Navn);
    }
}
