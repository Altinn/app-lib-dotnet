namespace Altinn.App.Core.Internal.Notifications.Email;

/// <summary>
/// Class representing an exception throw when an email notification could not be sent.
/// </summary>
public sealed class EmailNotificationException : Exception
{
    internal EmailNotificationException(string? message, HttpResponseMessage? response, string? content, Exception? innerException)
    : base($"{message}: StatuCode={response?.StatusCode}\nReason={response?.ReasonPhrase}\nBody={content}\n", innerException)
    {
    }
}

