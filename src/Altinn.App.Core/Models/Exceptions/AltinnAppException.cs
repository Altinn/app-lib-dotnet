using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.Core.Models.Exceptions;

/// <summary>
/// Custom exception for fatal errors in Altinn that should result in a
/// with a <see cref="ProblemDetails"/> response with status 500 or 400
/// </summary>
/// <remarks>
/// <see cref="ProblemDetails.Status" /> is set to 500 Internal Server Error by default
/// and can be changed to 403 Forbidden or 400 Bad Request by setting <see cref="IsForbidden"/> or <see cref="IsBadRequest"/>
/// on initialization
/// </remarks>
/// <example>
/// <code>
/// throw new AltinnAppException
/// {
///     Title = "Bad Request",
///     Description = "Set the ProblemDetails.Status to 400 Bad Request",
///     IsBadRequest = true,
/// };
/// </code>
/// </example>
internal class AltinnAppException : Exception
{
    public AltinnAppException([CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0)
        : base($"An error occurred in {filePath} at line {lineNumber}") { }

    /// <summary>
    /// The type to use in <see cref="ProblemDetails.Type"/> of the response
    ///
    /// Defaults to the class name of the exception
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// The title to use in <see cref="ProblemDetails.Title"/> of the response
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Human-readable description of the error to be shown to the user
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// The status code to be used in the response
    /// </summary>
    /// <remarks>
    /// Use custom setters for specific allowed status codes like 403 Forbidden or 400 Bad Request
    ///
    /// This exception should only be used for errors that is visible
    /// </remarks>
    public int StatusCode { get; private init; } = StatusCodes.Status500InternalServerError;

    /// <summary>
    /// Set the status code to 403 Forbidden
    /// </summary>
    public bool IsForbidden
    {
        // ReSharper disable once ValueParameterNotUsed
        init { StatusCode = StatusCodes.Status403Forbidden; }
    }

    /// <summary>
    /// Set the status code to 400 Bad Request
    /// </summary>
    public bool IsBadRequest
    {
        // ReSharper disable once ValueParameterNotUsed
        init { StatusCode = StatusCodes.Status400BadRequest; }
    }
}
