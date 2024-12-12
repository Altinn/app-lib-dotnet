using Altinn.App.Core.Internal.Auth;

namespace Altinn.App.Api.Infrastructure.Middleware;

internal sealed class AuthenticationContextMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationContextMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public AuthenticationContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Invokes the middleware to process the HTTP context.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public Task InvokeAsync(HttpContext context)
    {
        var authenticationContext = (AuthenticationContext)
            context.RequestServices.GetRequiredService<IAuthenticationContext>();
        authenticationContext.ResolveCurrent(context);
        return _next(context);
    }
}

/// <summary>
/// Extension methods for adding the <see cref="TelemetryEnrichingMiddleware"/> to the application pipeline.
/// </summary>
public static class AuthenticationContextMiddlewareExtensions
{
    /// <summary>
    /// Adds the <see cref="AuthenticationContextMiddleware"/> to the application's request pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    public static IApplicationBuilder UseAuthenticationContext(this IApplicationBuilder app)
    {
        return app.UseMiddleware<AuthenticationContextMiddleware>();
    }
}
