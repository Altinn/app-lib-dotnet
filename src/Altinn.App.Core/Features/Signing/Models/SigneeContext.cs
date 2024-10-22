using Altinn.App.Core.Features.Signing.Interfaces;

namespace Altinn.App.Core.Features.Signing.Models;

internal sealed class SigneeContext
{
    internal SigneeContext(SigneeState signeeState, SigneeParty signeeParty)
    {
        SigneeState = signeeState;
        SigneeParty = signeeParty;
    }

    internal SigneeState SigneeState { get; set; }
    internal SigneeParty SigneeParty { get; set; }
}
