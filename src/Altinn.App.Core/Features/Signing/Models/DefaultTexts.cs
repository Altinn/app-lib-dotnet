namespace Altinn.App.Core.Features.Signing.Models;

internal sealed record DefaultTexts
{
    internal required string Title { get; init; }
    internal required string Summary { get; init; }
    internal required string Body { get; init; }
}
