using Altinn.App.Core.Exceptions;

namespace Altinn.App.Clients.Fiks.Exceptions;

/// <summary>
/// An error occurred and it was related to Fiks Arkiv.
/// </summary>
public class FiksArkivException : AltinnException
{
    /// <inheritdoc/>
    internal FiksArkivException() { }

    /// <inheritdoc/>
    internal FiksArkivException(string? message)
        : base(message) { }

    /// <inheritdoc/>
    internal FiksArkivException(string? message, Exception? innerException)
        : base(message, innerException) { }
}
