using Altinn.App.Generated.Model;
using Altinn.App.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Altinn.App.SourceGenerator.Tests;

public class AppModelGeneratorTest
{
    [Fact]
    public void GetModelType_returns_correct_type()
    {
        ILogger<AppModel> logger = NullLogger<AppModel>.Instance;
        AppModel appModel = new AppModel(logger);
        Type t = appModel.GetModelType("Altinn.App.Models.Skjema");
        Skjema s = new Skjema();
        s.Should().BeOfType(t);
    }

    [Fact]
    public void Create_returns_object_of_correct_type()
    {
        ILogger<AppModel> logger = NullLogger<AppModel>.Instance;
        AppModel appModel = new AppModel(logger);
        object o = appModel.Create("Altinn.App.Models.Skjema");
        o.Should().BeOfType(typeof(Skjema));
    }
}