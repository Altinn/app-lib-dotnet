namespace Altinn.App.Core.Features.Correspondence.Exceptions;

/// <summary>
/// An exception that indicates an error was returned from the correspondence server
/// </summary>
public class CorrespondenceRequestException : CorrespondenceException
{
    /// <inheritdoc/>
    public CorrespondenceRequestException() { }

    /// <inheritdoc/>
    public CorrespondenceRequestException(string? message)
        : base(message) { }

    /// <inheritdoc/>
    public CorrespondenceRequestException(string? message, Exception? innerException)
        : base(message, innerException) { }
}
