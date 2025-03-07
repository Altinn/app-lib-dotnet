using Altinn.App.Core.Exceptions;

namespace Altinn.App.Clients.Fiks.Exceptions;

public class FiksIOException : AltinnException
{
    /// <inheritdoc/>
    protected FiksIOException() { }

    /// <inheritdoc/>
    protected FiksIOException(string? message)
        : base(message) { }

    /// <inheritdoc/>
    protected FiksIOException(string? message, Exception? innerException)
        : base(message, innerException) { }
}
