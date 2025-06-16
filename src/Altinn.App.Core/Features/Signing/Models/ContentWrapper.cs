using Altinn.App.Core.Features.Correspondence.Models;

namespace Altinn.App.Core.Features.Signing.Models;

internal record ContentWrapper
{
    internal required CorrespondenceContent CorrespondenceContent { get; init; }
    internal string? SmsBody { get; init; }
    internal string? EmailBody { get; init; }
    internal string? EmailSubject { get; init; }
    internal string? ReminderSmsBody { get; init; }
    internal string? ReminderEmailBody { get; init; }
    internal string? ReminderEmailSubject { get; init; }
}
