using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Altinn.App.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
    /// <returns>Verification transaction details including the authorization request URL.</returns>
    [HttpPost("start")]
    [ProducesResponseType(typeof(VerificationStartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VerificationStartResponse>> StartVerification()
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("WalletApiClient");

            var requestBody = new
            {
                credential_issuer = "https://utsteder.test.eidas2sandkasse.net",
                credential_configuration_id = "org.iso.18013.5.1.mDL_mso_mdoc",
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                MediaTypeNames.Application.Json
            );

            _logger.LogInformation("Starting wallet verification request");
            var response = await httpClient.PostAsync("/v1/verify/start", content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Wallet verification start failed with status {StatusCode}", response.StatusCode);
                return StatusCode(
                    (int)response.StatusCode,
                    new ProblemDetails
                    {
                        Title = "Wallet verification start failed",
                        Detail = "Failed to initiate wallet verification with external service",
                        Status = (int)response.StatusCode,
                    }
                );
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseData = JsonSerializer.Deserialize<VerificationStartResponse>(responseJson);

            if (responseData == null)
            {
                _logger.LogError("Failed to deserialize wallet verification start response");
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new ProblemDetails
                    {
                        Title = "Deserialization error",
                        Detail = "Failed to parse response from wallet verification service",
                        Status = StatusCodes.Status500InternalServerError,
                    }
                );
            }

            _logger.LogInformation(
                "Wallet verification started successfully with transaction ID {TransactionId}",
                responseData.VerifierTransactionId
            );

            return Ok(responseData);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while starting wallet verification");
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
            _logger.LogError(ex, "Unexpected error while starting wallet verification");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Internal server error",
                    Detail = "An unexpected error occurred while starting wallet verification",
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
    [ProducesResponseType(typeof(VerificationStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VerificationStatusResponse>> GetStatus(string verificationId)
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
            var responseData = JsonSerializer.Deserialize<VerificationStatusResponse>(responseJson);

            if (responseData == null)
            {
                _logger.LogError(
                    "Failed to deserialize wallet verification status response for transaction {VerificationId}",
                    verificationId
                );
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new ProblemDetails
                    {
                        Title = "Deserialization error",
                        Detail = "Failed to parse status response from wallet verification service",
                        Status = StatusCodes.Status500InternalServerError,
                    }
                );
            }

            return Ok(responseData);
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
    [ProducesResponseType(typeof(VerificationResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VerificationResultResponse>> GetResult(string verificationId)
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
            var responseData = JsonSerializer.Deserialize<VerificationResultResponse>(responseJson);

            if (responseData == null)
            {
                _logger.LogError(
                    "Failed to deserialize wallet verification result response for transaction {VerificationId}",
                    verificationId
                );
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new ProblemDetails
                    {
                        Title = "Deserialization error",
                        Detail = "Failed to parse result response from wallet verification service",
                        Status = StatusCodes.Status500InternalServerError,
                    }
                );
            }

            _logger.LogInformation(
                "Successfully fetched wallet verification result for transaction {VerificationId}",
                verificationId
            );

            return Ok(responseData);
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
