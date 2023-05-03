using System.Security.Claims;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process.V2
{
    public class ProcessStartRequest
    {
        public Instance Instance { get; set; }
        public ClaimsPrincipal User { get; set; }
        public Dictionary<string, string>? Prefill { get; set; }
        public string? StartEventId { get; set; }
        public bool Dryrun { get; set; }
    }
}
