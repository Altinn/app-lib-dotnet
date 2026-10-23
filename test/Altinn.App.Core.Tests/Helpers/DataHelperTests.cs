using Altinn.App.Core.Helpers;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Tests.Helpers;

public class DataHelperTests
{
    private sealed class Soknad
    {
        public string? NavnElev { get; set; }

        public string? NavnElevManuelt { get; set; }
    }

    private sealed class Model
    {
        public Soknad Soknad { get; set; } = new();
    }

    private static Model Data =>
        new()
        {
            Soknad = new Soknad { NavnElev = "Kari", NavnElevManuelt = "Ola" },
        };

    private static DataField Field(string id, string path, string dataTypeId) =>
        new()
        {
            Id = id,
            Path = path,
            DataTypeId = dataTypeId,
        };

    [Fact]
    public void GetUpdatedDataValues_Returns_A_Value_Per_Field()
    {
        List<DataField> fields =
        [
            Field("Navn", "Soknad.NavnElev", "datamodel"),
            Field("NavnManuelt", "Soknad.NavnElevManuelt", "datamodel"),
        ];

        var updated = DataHelper.GetUpdatedDataValues(fields, [], "datamodel", Data);

        Assert.Equal(new Dictionary<string, string?> { ["Navn"] = "Kari", ["NavnManuelt"] = "Ola" }, updated);
    }

    [Fact]
    public void GetUpdatedDataValues_Reports_Only_Values_That_Changed()
    {
        List<DataField> fields = [Field("Navn", "Soknad.NavnElev", "datamodel")];
        Dictionary<string, string?> current = new() { ["Navn"] = "Kari" };

        var updated = DataHelper.GetUpdatedDataValues(fields, current, "datamodel", Data);

        Assert.Empty(updated);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetUpdatedDataValues_Does_Not_Report_An_Empty_Value_For_An_Absent_Key(string? emptyValue)
    {
        List<DataField> fields =
        [
            Field("Navn", "Soknad.NavnElev", "datamodel"),
            Field("NavnManuelt", "Soknad.NavnElevManuelt", "datamodel"),
        ];
        Model data = new()
        {
            Soknad = new Soknad { NavnElev = "Kari", NavnElevManuelt = emptyValue },
        };

        var updated = DataHelper.GetUpdatedDataValues(fields, [], "datamodel", data);

        Assert.Equal(new Dictionary<string, string?> { ["Navn"] = "Kari" }, updated);
    }

    [Fact]
    public void GetUpdatedDataValues_Does_Not_Report_An_Empty_Value_When_The_Instance_Has_No_Values()
    {
        List<DataField> fields =
        [
            Field("Navn", "Soknad.NavnElev", "datamodel"),
            Field("NavnManuelt", "Soknad.NavnElevManuelt", "datamodel"),
        ];
        Model data = new()
        {
            Soknad = new Soknad { NavnElev = "Kari", NavnElevManuelt = null },
        };

        var updated = DataHelper.GetUpdatedDataValues(fields, null!, "datamodel", data);

        Assert.Equal(new Dictionary<string, string?> { ["Navn"] = "Kari" }, updated);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetUpdatedDataValues_Reports_A_Present_Value_That_Became_Empty(string? emptyValue)
    {
        List<DataField> fields = [Field("NavnManuelt", "Soknad.NavnElevManuelt", "datamodel")];
        Dictionary<string, string?> current = new() { ["NavnManuelt"] = "Ola" };
        Model data = new()
        {
            Soknad = new Soknad { NavnElev = "Kari", NavnElevManuelt = emptyValue },
        };

        var updated = DataHelper.GetUpdatedDataValues(fields, current, "datamodel", data);

        Assert.Equal(new Dictionary<string, string?> { ["NavnManuelt"] = emptyValue }, updated);
    }
}
