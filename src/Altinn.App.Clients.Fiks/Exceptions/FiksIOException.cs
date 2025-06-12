using Altinn.App.Core.Exceptions;

namespace Altinn.App.Clients.Fiks.Exceptions;

/// <summary>
/// An error occurred and it was related to Fiks IO.
/// </summary>
public class FiksIOException : AltinnException
{
    /// <inheritdoc/>
    internal FiksIOException() { }

    /// <inheritdoc/>
    internal FiksIOException(string? message)
        : base(message) { }

    /// <inheritdoc/>
    internal FiksIOException(string? message, Exception? innerException)
        : base(message, innerException) { }
}
