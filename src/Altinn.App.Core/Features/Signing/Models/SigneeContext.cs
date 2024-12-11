namespace Altinn.App.Core.Features.Signing.Models;

internal sealed class SigneeContext
{
    internal SigneeContext(string taskId, int partyId, SigneeParty signeeParty, SigneeState signeeState)
    {
        SigneeState = signeeState;
        SigneeParty = signeeParty;
        PartyId = partyId;
        TaskId = taskId;
    }

    /// <summary>The identifier of the signee.</summary>
    internal int PartyId { get; }

    /// <summary>The task associated with the signee state.</summary>
    internal string TaskId { get; set; }

    internal SigneeState SigneeState { get; set; }
    internal SigneeParty SigneeParty { get; set; }
}
