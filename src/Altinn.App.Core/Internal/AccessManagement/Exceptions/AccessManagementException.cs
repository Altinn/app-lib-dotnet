namespace Altinn.App.Core.Internal.AccessManagement.Exceptions;

/// <summary>
/// Generic Access Management related exception.
/// </summary>
internal abstract class AccessManagementException : Exception
{
    /// <inheritdoc/>
    protected AccessManagementException() { }

    /// <inheritdoc/>
    protected AccessManagementException(string? message)
        : base(message) { }

    /// <inheritdoc/>
    protected AccessManagementException(string? message, Exception? innerException)
        : base(message, innerException) { }
}
