using Altinn.App.Core.Features.Signing.Interfaces;

namespace Altinn.App.Core.Features.Signing.Models;

internal sealed class SigneeContext
{
    internal SigneeContext(string taskId, Guid partyId, SigneeParty signeeParty, SigneeState signeeState)
    {
        SigneeState = signeeState;
        SigneeParty = signeeParty;
        PartyId = partyId;
        TaskId = taskId;
    }

    /// <summary>The identifier of the signee.</summary>
    internal Guid PartyId { get; }

    /// <summary>The task associated with the signee state.</summary>
    internal string TaskId { get; set; }

    internal SigneeState SigneeState { get; set; }
    internal SigneeParty SigneeParty { get; set; }
}
