namespace Altinn.App.Api.Infrastructure.Middleware;

internal sealed class ProcessLockOptions
{
    public TimeSpan Expiration { get; set; } = TimeSpan.FromMinutes(5);
}
