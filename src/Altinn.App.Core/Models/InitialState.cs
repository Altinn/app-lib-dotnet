using Altinn.App.Core.Configuration;
using Altinn.Platform.Profile.Models;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.Core.Models;

using System.Text.Json;

public class InitialState
{
    public InitialState(ApplicationMetadata metadata)
    {
        ApplicationMetadata = metadata;
    }

    public ApplicationMetadata ApplicationMetadata { get; set; }

    public JsonResult FrontEndSettings { get; set; }

    public UserProfile User { get; set; }
}
