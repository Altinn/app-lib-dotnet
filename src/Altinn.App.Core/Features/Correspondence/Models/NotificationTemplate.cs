namespace Altinn.App.Core.Features.Correspondence.Models;

/// <summary>
/// The message template to use for notifications
/// </summary>
public enum NotificationTemplate
{
    /// <summary>
    /// Fully customizable template (e.g. no template)
    /// </summary>
    CustomMessage,

    /// <summary>
    /// Standard Altinn notification template ("You have received a message in Altinn...")
    /// </summary>
    GenericAltinnMessage,
}
