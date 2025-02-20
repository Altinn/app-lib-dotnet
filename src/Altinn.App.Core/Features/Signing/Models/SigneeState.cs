using System.Text.Json.Serialization;

namespace Altinn.App.Core.Features.Signing.Models;

/// <summary>
/// The signee state
/// </summary>
public sealed class SigneeState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SigneeState"/> class.
    /// </summary>
    public SigneeState() { }

    /// <summary>
    /// Indicates whether signee has been delegated rights to sign.
    /// </summary>
    [JsonPropertyName("isAccessDelegated")]
    public bool IsAccessDelegated { get; set; }

    /// <summary>
    /// The reason why the delegation failed.
    /// </summary>
    [JsonPropertyName("delegationFailedReason")]
    public string? DelegationFailedReason { get; set; }

    /// <summary>
    /// Indicates whether the signee has been messaged about the signature call to action.
    /// </summary>
    [JsonPropertyName("isCalledToSign")]
    public bool IsMessagedForCallToSign { get; set; }

    /// <summary>
    /// The reason why the message failed.
    /// </summary>
    [JsonPropertyName("callToSignFailedReason")]
    public string? CallToSignFailedReason { get; set; }

    /// <summary>Indicates whether the receipt for the signature has been send to the signee.</summary>
    [JsonPropertyName("isReceiptSent")]
    public bool IsReceiptSent { get; set; }
}
