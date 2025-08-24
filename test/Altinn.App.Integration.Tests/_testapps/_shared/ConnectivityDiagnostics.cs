using System;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

#nullable enable

namespace TestApp.Shared;

/// <summary>
/// Provides connectivity diagnostic endpoints to verify container-to-container communication.
/// This is useful for testing network connectivity and diagnosing host.docker.internal issues.
/// </summary>
public static class ConnectivityDiagnostics
{
    /// <summary>
    /// Maps connectivity diagnostic endpoints.
    /// </summary>
    public static WebApplication UseConnectivityDiagnostics(this WebApplication app)
    {
        app.MapGet(
            "/{org}/{app}/diagnostics/connectivity/pdf",
            async ([FromServices] IHttpClientFactory httpClientFactory, [FromServices] IConfiguration configuration) =>
            {
                try
                {
                    using var httpClient = httpClientFactory.CreateClient();
                    httpClient.Timeout = TimeSpan.FromSeconds(5);

                    // Get PDF service URL from configuration (as set in AppFixture.cs)
                    var pdfServiceUrl =
                        configuration["PlatformSettings:ApiPdf2Endpoint"]
                        ?? throw new Exception("PlatformSettings.ApiPdf2Endpoint not configured");

                    var activeEndpoint = pdfServiceUrl.Replace("/pdf", "/config");

                    var response = await httpClient.GetAsync(activeEndpoint);
                    var content = await response.Content.ReadAsStringAsync();

                    return Results.Json(
                        new ConnectivityResult
                        {
                            Success = response.IsSuccessStatusCode,
                            StatusCode = (int)response.StatusCode,
                            Url = activeEndpoint,
                            ResponseContent = content,
                            Message = response.IsSuccessStatusCode
                                ? "PDF service connectivity verified"
                                : $"PDF service connectivity failed: {response.ReasonPhrase}",
                        }
                    );
                }
                catch (Exception ex)
                {
                    return Results.Json(
                        new ConnectivityResult
                        {
                            Success = false,
                            StatusCode = 0,
                            Url = "unknown",
                            ResponseContent = null,
                            Message = $"PDF service connectivity error: {ex.Message}",
                            Exception = ex.ToString(),
                        }
                    );
                }
            }
        );

        return app;
    }
}

public class ConnectivityResult
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? ResponseContent { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
}
