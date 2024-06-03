namespace Altinn.App.Core.Features.Maskinporten.Exceptions;

/// <summary>
/// An exception that indicates a missing or invalid `maskinporten-settings.json` file
/// </summary>
public class MaskinportenConfigurationException : MaskinportenException
{
    public MaskinportenConfigurationException() { }

    public MaskinportenConfigurationException(string? message)
        : base(message) { }

    public MaskinportenConfigurationException(string? message, Exception? innerException)
        : base(message, innerException) { }
}
