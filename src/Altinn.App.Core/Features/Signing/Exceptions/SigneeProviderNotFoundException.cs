namespace Altinn.App.Core.Features.Signing.Exceptions;

internal class SigneeProviderNotFoundException : Exception
{
    public SigneeProviderNotFoundException(string message)
        : base(message) { }
}
