using System;
using System.Collections.Generic;
using System.Linq;
using Altinn.App.Core.Helpers.DataModel;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Models.Layout;
using Altinn.App.SourceGenerator.Integration.Tests.Models;
using Altinn.Platform.Storage.Interface.Models;
using Xunit;

namespace Altinn.App.SourceGenerator.Integration.Tests.UnitTest;

public class TestGetResolvedKeys()
{
    private readonly DataElement _dataElement = new()
    {
        Id = "00000000-0000-0000-0000-000000000000",
        DataType = "model",
    };
    private readonly DataType _dataType = new() { Id = "model" };

    /// <summary>
    /// Create a shared instance of Skjema for testing.
    /// </summary>
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
                    new()
                    {
                        Gate = "gate2",
                        Postnummer = 1236,
                        Tags = ["tag1", "tag2"],
                    },
                ],
            },
        ],
        EierAdresse = null,
    };

    [Theory]
    [InlineData("skjemanummer", new[] { "skjemanummer" })]
    [InlineData("skjemainnhold.navn", new[] { "skjemainnhold[0].navn", "skjemainnhold[1].navn" })]
    [InlineData("skjemainnhold.alder", new[] { "skjemainnhold[0].alder", "skjemainnhold[1].alder" })]
    [InlineData("skjemainnhold[0].alder", new[] { "skjemainnhold[0].alder" })]
    [InlineData("skjemainnhold[1].alder", new[] { "skjemainnhold[1].alder" })]
    [InlineData("skjemainnhold.deltar", new[] { "skjemainnhold[0].deltar", "skjemainnhold[1].deltar" })]
    [InlineData("skjemainnhold.adresse", new[] { "skjemainnhold[0].adresse", "skjemainnhold[1].adresse" })]
    [InlineData(
        "skjemainnhold.adresse.gate",
        new[] { "skjemainnhold[0].adresse.gate", "skjemainnhold[1].adresse.gate" }
    )]
    [InlineData(
        "skjemainnhold.adresse.postnummer",
        new[] { "skjemainnhold[0].adresse.postnummer", "skjemainnhold[1].adresse.postnummer" }
    )]
    [InlineData(
        "skjemainnhold.tidligere-adresse.gate",
        new[] { "skjemainnhold[1].tidligere-adresse[0].gate", "skjemainnhold[1].tidligere-adresse[1].gate" }
    )]
    [InlineData("skjemainnhold[0].tidligere-adresse.gate", new string[] { })]
    [InlineData(
        "skjemainnhold.tidligere-adresse.postnummer",
        new[] { "skjemainnhold[1].tidligere-adresse[0].postnummer", "skjemainnhold[1].tidligere-adresse[1].postnummer" }
    )]
    [InlineData("eierAdresse", new[] { "eierAdresse" })]
    [InlineData("eierAdresse.gate", new[] { "eierAdresse.gate" })]
    [InlineData("doesnotexist", new string[] { })]
    [InlineData("skjemainnhold.doesnotexist", new string[] { })]
    // These cases ends in enumerables does not make sense to resolve keys for, and should throw exceptions.
    // Currently added as test here to verify current behaviour, but should be removed when the implementation is updated to throw exceptions for these cases.
    [InlineData("skjemainnhold", new string[] { "skjemainnhold[0]", "skjemainnhold[1]" })]
    [InlineData(
        "skjemainnhold.tidligere-adresse",
        new string[]
        {
            "skjemainnhold[0].tidligere-adresse",
            "skjemainnhold[1].tidligere-adresse[0]",
            "skjemainnhold[1].tidligere-adresse[1]",
        }
    )]
    [InlineData(
        "skjemainnhold.tidligere-adresse.tags",
        new string[]
        {
            "skjemainnhold[1].tidligere-adresse[0].tags",
            "skjemainnhold[1].tidligere-adresse[1].tags[0]",
            "skjemainnhold[1].tidligere-adresse[1].tags[1]",
        }
    )]
    [InlineData(
        "skjemainnhold.tidligere-adresse[1]",
        new string[] { "skjemainnhold[0].tidligere-adresse", "skjemainnhold[1].tidligere-adresse[1]" }
    )]
    [InlineData("skjemainnhold[0]", new string[] { "skjemainnhold[0]" })]
    public void TestResolvedKeys(string field, string[] expectedKeys)
    {
        // Test old reflection based implementation
        var modelWrapper = new DataModelWrapper(_skjema);
        var resolvedKeysReflection = modelWrapper.GetResolvedKeys(field);
        Assert.Equal(expectedKeys, resolvedKeysReflection);

        // Test formDataWrapper
        var dataWrapper = FormDataWrapperFactory.Create(_skjema, _dataType, _dataElement);
        var resolvedKeys = dataWrapper
            .GetResolvedKeys(new DataReference() { Field = field, DataElementIdentifier = _dataElement })
            .Select(k => k.Field)
            .ToArray();
        Assert.Equal(expectedKeys, resolvedKeys);
    }

    // [Theory]
    // TODO: move test cases here when implementation is updated to throw exceptions for cases that resolves to enumerables, as these cases does not make sense to resolve keys for.
    private void TestResolvedKeys_ErrorConditions(string field)
    {
        // Test old reflection based implementation
        var modelWrapper = new DataModelWrapper(_skjema);
        var exception = Assert.Throws<Exception>(() => modelWrapper.GetResolvedKeys(field).ToArray());
        Assert.Contains("ResolveKeys", exception.Message);

        var dataWrapper = FormDataWrapperFactory.Create(_skjema, _dataType, _dataElement);
        var dataException = Assert.Throws<ArgumentException>(() =>
            dataWrapper
                .GetResolvedKeys(new DataReference() { Field = field, DataElementIdentifier = _dataElement })
                .ToArray()
        );
        Assert.Contains("ResolveKeys", dataException.Message);
    }
}
