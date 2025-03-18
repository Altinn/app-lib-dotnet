namespace Altinn.App.Clients.Fiks.Exceptions;

public class FiksIOConfigurationException : FiksIOException
{
    /// <inheritdoc/>
    internal FiksIOConfigurationException() { }

    /// <inheritdoc/>
    internal FiksIOConfigurationException(string? message)
        : base(message) { }

    /// <inheritdoc/>
    internal FiksIOConfigurationException(string? message, Exception? innerException)
        : base(message, innerException) { }
}
