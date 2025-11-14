using Altinn.App.ProcessEngine.Models;
using Xunit;

namespace Altinn.App.ProcessEngine.Tests.Models;

public class InstanceInformationTest
{
    [Fact]
    public void SupportsCaseInsensitiveEquality()
    {
        var a = new InstanceInformation("org", "app", 123, Guid.NewGuid());
        var b = new InstanceInformation("ORG", "App", 123, a.InstanceGuid);

        Assert.Equal(a, b);
    }
}
