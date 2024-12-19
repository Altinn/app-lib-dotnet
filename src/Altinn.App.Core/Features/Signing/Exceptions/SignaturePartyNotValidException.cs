namespace Altinn.App.Core.Features.Signing.Exceptions;

/// <summary>
/// Represents the exception that is thrown when the signature party is not valid.
/// </summary>
internal class SignaturePartyNotValidException : Exception
{
    public SignaturePartyNotValidException(string message)
        : base(message) { }
}
