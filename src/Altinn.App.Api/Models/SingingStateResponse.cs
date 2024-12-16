namespace Altinn.App.Api.Models;

/// <summary>
/// Contains the result of a get signees request.
/// </summary>
public class SingingStateResponse
{
    /// <summary>
    /// The signees for the current task.
    /// </summary>
    public required List<SigneeState> SigneeStates { get; set; }
}

/// <summary>
/// Contains information about a signee and the current signing status.
/// </summary>
public class SigneeState
{
    /// <summary>
    /// The name of the signee.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The organisation of the signee.
    /// </summary>
    public string? Organisation { get; set; }

    /// <summary>
    /// Whether the signee has signed or not.
    /// </summary>
    public required bool HasSigned { get; set; }

    /// <summary>
    /// Whether delegation of signing rights has been successful.
    /// </summary>
    public bool DelegationSuccessful { get; set; }

    /// <summary>
    /// Whether the signee has been notified to sign via email or sms.
    /// </summary>
    public bool NotificationSuccessful { get; set; }

    //TODO: Add necessary properties
}
