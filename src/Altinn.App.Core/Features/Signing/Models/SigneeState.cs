namespace Altinn.App.Core.Features.Signing.Models;

internal sealed class SigneeState
{
    /// <summary>
    /// Create a new instance of the <see cref="SigneeState"/> class
    /// </summary>
    /// <param name="id">The identifier of the signee.</param>
    /// <param name="displayName">The display name of the signee.</param>
    internal SigneeState(string id, string displayName)
    {
        Id = id;
        DisplayName = displayName;
    }

    /// <summary>The identifier of the signee.</summary>
    internal string Id { get; }

    /// <summary>The display name of the signee.</summary>
    internal string DisplayName { get; }

    /// <summary>Indicates whether signee has been delegated rights to sign.</summary>
    internal bool IsDelegated { get; set; }

    /// <summary>Indicates whether signee has been notified to sign.</summary>
    internal bool IsNotified { get; set; }

    /// <summary>Indicates whether the signee has signed.</summary>
    internal bool HasSigned { get; set; }

    /// <summary>Indicates whether the signature has been notified to the process task owner.</summary>
    internal bool IsTaskOwnerNotified { get; set; }
}
