using Altinn.App.ProcessEngine.Models;
using Xunit;

namespace Altinn.App.ProcessEngine.Tests.Models;

public class InstanceInformationTest
{
    [Fact]
    public void SupportsCaseInsensitiveEquality()
    {
        var a = new InstanceInformation
        {
            Org = "org",
            App = "app",
            InstanceOwnerPartyId = 123,
            InstanceGuid = Guid.NewGuid(),
        };
        var b = new InstanceInformation
        {
            Org = "ORG",
            App = "App",
            InstanceOwnerPartyId = 123,
            InstanceGuid = a.InstanceGuid,
        };

        Assert.Equal(a, b);
    }
}
