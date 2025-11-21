using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.Api.Controllers;

/// <summary>
/// Controller for wallet credential verification endpoints.
/// Proxies requests to the external wallet verification service.
/// </summary>
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[Consumes(MediaTypeNames.Application.Json)]
[Route("{org}/{app}/api/wallet/verify")]
[AllowAnonymous]
public class WalletVerificationController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WalletVerificationController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WalletVerificationController"/> class.
    /// </summary>
    public WalletVerificationController(
        IHttpClientFactory httpClientFactory,
        ILogger<WalletVerificationController> logger
    )
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Initiates a wallet credential verification request.
    /// </summary>
    /// <param name="requestBody">The request body to forward to the external wallet API.</param>
    /// <returns>Verification transaction details including the authorization request URL.</returns>
    [HttpPost("start")]
    [ProducesResponseType(typeof(JsonElement), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> StartVerification([FromBody] JsonElement requestBody)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("WalletApiClient");

            var content = new StringContent(requestBody.GetRawText(), Encoding.UTF8, MediaTypeNames.Application.Json);

            _logger.LogInformation("Proxying wallet verification start request");
            var response = await httpClient.PostAsync("/v1/verify/start", content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Wallet verification start failed with status {StatusCode}", response.StatusCode);
                return StatusCode(
                    (int)response.StatusCode,
                    new ProblemDetails
                    {
                        Title = "Wallet verification start failed",
                        Detail = await response.Content.ReadAsStringAsync(),
                        Status = (int)response.StatusCode,
                    }
                );
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Wallet verification started successfully");

            return Content(responseJson, MediaTypeNames.Application.Json);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while starting wallet verification");
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new ProblemDetails
                {
                    Title = "Service unavailable",
                    Detail = ex.Message,
                    Status = StatusCodes.Status503ServiceUnavailable,
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while starting wallet verification");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Internal server error",
                    Detail = ex.Message,
                    Status = StatusCodes.Status500InternalServerError,
                }
            );
        }
    }

    /// <summary>
    /// Checks the status of a wallet verification request.
    /// </summary>
    /// <param name="verificationId">The verification transaction ID.</param>
    /// <returns>The current verification status (PENDING, AVAILABLE, or FAILED).</returns>
    [HttpGet("status/{verificationId}")]
    [ProducesResponseType(typeof(JsonElement), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStatus(string verificationId)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("WalletApiClient");

            _logger.LogInformation(
                "Checking wallet verification status for transaction {VerificationId}",
                verificationId
            );

            var response = await httpClient.GetAsync($"/v1/verify/status/{verificationId}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Wallet verification status check failed with status {StatusCode} for transaction {VerificationId}",
                    response.StatusCode,
                    verificationId
                );

                return StatusCode(
                    (int)response.StatusCode,
                    new ProblemDetails
                    {
                        Title = "Status check failed",
                        Detail = $"Failed to check verification status for transaction {verificationId}",
                        Status = (int)response.StatusCode,
                    }
                );
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            return Content(responseJson, MediaTypeNames.Application.Json);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Network error while checking wallet verification status for transaction {VerificationId}",
                verificationId
            );
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new ProblemDetails
                {
                    Title = "Service unavailable",
                    Detail = "Unable to reach wallet verification service",
                    Status = StatusCodes.Status503ServiceUnavailable,
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while checking wallet verification status for transaction {VerificationId}",
                verificationId
            );
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Internal server error",
                    Detail = "An unexpected error occurred while checking verification status",
                    Status = StatusCodes.Status500InternalServerError,
                }
            );
        }
    }

    /// <summary>
    /// Retrieves the verified credential data from a completed verification.
    /// </summary>
    /// <param name="verificationId">The verification transaction ID.</param>
    /// <returns>The verified credential claims including portrait and personal data.</returns>
    [HttpGet("result/{verificationId}")]
    [ProducesResponseType(typeof(JsonElement), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetResult(string verificationId)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("WalletApiClient");

            _logger.LogInformation(
                "Fetching wallet verification result for transaction {VerificationId}",
                verificationId
            );

            var response = await httpClient.GetAsync($"/v1/verify/result/{verificationId}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Wallet verification result fetch failed with status {StatusCode} for transaction {VerificationId}",
                    response.StatusCode,
                    verificationId
                );

                return StatusCode(
                    (int)response.StatusCode,
                    new ProblemDetails
                    {
                        Title = "Result fetch failed",
                        Detail = $"Failed to fetch verification result for transaction {verificationId}",
                        Status = (int)response.StatusCode,
                    }
                );
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            _logger.LogInformation(
                "Successfully fetched wallet verification result for transaction {VerificationId}",
                verificationId
            );

            return Content(responseJson, MediaTypeNames.Application.Json);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Network error while fetching wallet verification result for transaction {VerificationId}",
                verificationId
            );
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new ProblemDetails
                {
                    Title = "Service unavailable",
                    Detail = "Unable to reach wallet verification service",
                    Status = StatusCodes.Status503ServiceUnavailable,
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while fetching wallet verification result for transaction {VerificationId}",
                verificationId
            );
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Internal server error",
                    Detail = "An unexpected error occurred while fetching verification result",
                    Status = StatusCodes.Status500InternalServerError,
                }
            );
        }
    }
}
