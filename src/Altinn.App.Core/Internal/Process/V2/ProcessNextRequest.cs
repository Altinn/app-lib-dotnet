using System.Security.Claims;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process.V2;

public class ProcessNextRequest
{
    public Instance Instance { get; set; }
    public ClaimsPrincipal User { get; set; }
    public string? Action { get; set; }
}
