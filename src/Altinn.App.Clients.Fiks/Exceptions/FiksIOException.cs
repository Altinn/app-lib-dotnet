using Altinn.App.Core.Exceptions;

namespace Altinn.App.Clients.Fiks.Exceptions;

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
