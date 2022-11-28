﻿#nullable enable
using System.Text;
using System.Text.Json;

using Altinn.App.Core.Internal.Pdf;

using Altinn.App.Core.Interface;
using Altinn.App.Core.Internal.Pdf;
using Altinn.App.Core.Models;

using Microsoft.Extensions.Options;

using System.Text;
using System.Text.Json;

namespace Altinn.App.Core.Infrastructure.Clients.Pdf;

/// <summary>
/// Implementation of the <see cref="IPdfGeneratorClient"/> interface using a HttpClient to send
/// requests to the PDF Generator service.
/// </summary>
public class PdfGeneratorClient : IPdfGeneratorClient
{
    private readonly HttpClient _httpClient;
    private readonly PdfGeneratorSettings _pdfGeneratorSettings;
    private readonly IUserTokenProvider _userTokenProvider;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfGeneratorClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HttpClient to use in communication with the PDF generator service.</param>
    /// <param name="pdfGeneratorSettings">
    /// All generic settings needed for communication with the PDF generator service.
    /// </param>
    /// <param name="userTokenProvider">A service able to identify the JWT for currently authenticated user.</param>
    public PdfGeneratorClient(
        HttpClient httpClient,
        IOptions<PdfGeneratorSettings> pdfGeneratorSettings,
        IUserTokenProvider userTokenProvider)
    {
        _httpClient = httpClient;
        _userTokenProvider = userTokenProvider;
        _pdfGeneratorSettings = pdfGeneratorSettings.Value;
    }

    /// <inheritdoc/>
    public async Task<Stream> GeneratePdf(Uri uri, CancellationToken ct)
    {
        bool hasWaitForSelector = !string.IsNullOrWhiteSpace(_pdfGeneratorSettings.WaitForSelector);
        PdfGeneratorRequest generatorRequest = new()
        {
            Url = uri.AbsoluteUri,
            WaitFor = hasWaitForSelector ? _pdfGeneratorSettings.WaitForSelector : _pdfGeneratorSettings.WaitForTime
        };

        generatorRequest.Cookies.Add(new PdfGeneratorCookieOptions
        {
            Value = _userTokenProvider.GetUserToken(),
            Domain = uri.Host
        });

        string requestContent = JsonSerializer.Serialize(generatorRequest, _jsonSerializerOptions);

        var httpResponseMessage = await _httpClient.PostAsync(
            _pdfGeneratorSettings.ServiceEndpointUri,
            new StringContent(requestContent, Encoding.UTF8, "application/json"),
            ct);

        return await httpResponseMessage.Content.ReadAsStreamAsync(ct);
    }
}