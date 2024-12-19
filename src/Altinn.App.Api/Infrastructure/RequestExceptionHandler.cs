using Altinn.App.Core.Models.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.Api.Infrastructure;

internal class RequestExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private ILogger<RequestExceptionHandler> _logger;

    public RequestExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<RequestExceptionHandler> logger
    )
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        _logger.LogError(exception, "Handling exception in IExceptionHandler");
        if (exception is AltinnAppException altinn3AppException)
        {
            await HandleAltinn3AppExceptionAsync(httpContext, altinn3AppException);
            return true;
        }
        return false;
    }

    private async ValueTask HandleAltinn3AppExceptionAsync(
        HttpContext httpContext,
        AltinnAppException altinnAppException
    )
    {
        _logger.LogError(altinnAppException, "An Altinn3AppException occurred");
        await _problemDetailsService.WriteAsync(
            new ProblemDetailsContext()
            {
                HttpContext = httpContext,
                ProblemDetails = new ProblemDetails()
                {
                    Type = altinnAppException.Type,
                    Title = altinnAppException.Title,
                    Detail = altinnAppException.Description,
                    Status = altinnAppException.StatusCode,
                },
            }
        );
    }
}
