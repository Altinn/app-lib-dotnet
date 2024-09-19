using System.Reflection;
using Altinn.App.Core.EFormidling.Implementation;
using Altinn.App.Core.Features;
using FluentAssertions;

namespace Altinn.App.Core.TEsts.Features;

public class AppImplementationFactoryTests
{
    [Fact]
    public void Public_Interfaces_Meant_For_Apps_Arent_Constructor_Injected() { }
}
