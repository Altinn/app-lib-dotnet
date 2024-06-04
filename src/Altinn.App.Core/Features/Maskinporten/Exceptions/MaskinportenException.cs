namespace Altinn.App.Core.Features.Maskinporten.Exceptions;

/// <summary>
/// Generic Maskinporten related exception. Something went wrong, and it was related to Maskinporten.
/// </summary>
public class MaskinportenException : Exception
{
    /// <inheritdoc/>
    public MaskinportenException() { }

    /// <inheritdoc/>
    public MaskinportenException(string? message)
        : base(message) { }

    /// <inheritdoc/>
    public MaskinportenException(string? message, Exception? innerException)
        : base(message, innerException) { }
}
