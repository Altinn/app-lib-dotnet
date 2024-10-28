namespace Altinn.App.Core.Features.Signing.Models;

internal sealed class SigneeState
{
    /// <summary>Indicates whether signee has been delegated rights to sign.</summary>
    internal bool IsAccessDelegated { get; set; }

    /// <summary>Indicates whether signee has been notified to sign via sms.</summary>
    internal bool SignatureRequestSmsSent { get; set; }

    /// <summary>
    /// The reason why the sms was not sent.
    /// </summary>
    internal string? SignatureRequestSmsNotSentReason { get; set; }

    /// <summary>
    /// Indicated whether signee has been notified to sign via email.
    /// </summary>
    internal bool SignatureRequestEmailSent { get; set; }

    /// <summary>
    /// The reason why the email was not sent.
    /// </summary>
    internal string? SignatureRequestEmailNotSentReason { get; set; }

    // internal bool HasSigned { get; set; } //TODO: Probably don't want to store this here, but rather check for signature documents for this signee and make sure hash is correct?

    /// <summary>Indicates whether the receipt for the signature has been send to the signee.</summary>
    internal bool IsReceiptSent { get; set; }
}
