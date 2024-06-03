namespace Altinn.App.Core.Features.Maskinporten.Exceptions;

/// <summary>
/// Generic Maskinporten related exception. Something went wrong, and it was related to Maskinporten.
/// </summary>
public class MaskinportenException : Exception
{
    public MaskinportenException() { }

    public MaskinportenException(string? message)
        : base(message) { }

    public MaskinportenException(string? message, Exception? innerException)
        : base(message, innerException) { }
}
