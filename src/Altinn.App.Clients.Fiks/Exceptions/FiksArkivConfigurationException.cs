namespace Altinn.App.Clients.Fiks.Exceptions;

public class FiksArkivConfigurationException : FiksArkivException
{
    /// <inheritdoc/>
    internal FiksArkivConfigurationException() { }

    /// <inheritdoc/>
    internal FiksArkivConfigurationException(string? message)
        : base(message) { }

    /// <inheritdoc/>
    internal FiksArkivConfigurationException(string? message, Exception? innerException)
        : base(message, innerException) { }
}
