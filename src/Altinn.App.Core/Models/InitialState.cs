using Altinn.App.Core.Configuration;
using Altinn.Platform.Profile.Models;
using Altinn.Platform.Register.Models;

namespace Altinn.App.Core.Models;

public class InitialState
{
    public InitialState(ApplicationMetadata metadata)
    {
        ApplicationMetadata = metadata;
    }

    public ApplicationMetadata ApplicationMetadata { get; set; }

    public FrontEndSettings FrontEndSettings { get; set; }

    public UserProfile User { get; set; }

    public List<Party> ValidParties { get; set; }
}
