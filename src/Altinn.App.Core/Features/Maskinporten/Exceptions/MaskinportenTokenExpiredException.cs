namespace Altinn.App.Core.Features.Maskinporten.Exceptions;

/// <summary>
/// An exception that indicates the access token has expired when it was in fact expected to be valid
/// </summary>
public class MaskinportenTokenExpiredException : MaskinportenException
{
    public MaskinportenTokenExpiredException() { }

    public MaskinportenTokenExpiredException(string? message)
        : base(message) { }

    public MaskinportenTokenExpiredException(string? message, Exception? innerException)
        : base(message, innerException) { }
}
