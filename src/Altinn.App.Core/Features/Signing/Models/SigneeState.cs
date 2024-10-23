namespace Altinn.App.Core.Features.Signing.Models;

internal sealed class SigneeState
{
    /// <summary>Indicates whether signee has been delegated rights to sign.</summary>
    internal bool IsDelegated { get; set; }

    /// <summary>Indicates whether signee has been notified to sign.</summary>
    internal bool IsNotified { get; set; }

    /// <summary>Indicates whether the signee has signed.</summary>
    internal bool HasSigned { get; set; }

    /// <summary>Indicates whether the receipt for the signature has been send to the signee.</summary>
    internal bool IsReceiptSent { get; set; }
}
