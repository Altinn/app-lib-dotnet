using System.Net.Http.Headers;
using System.Text;

namespace Altinn.App.Core.Helpers;

/// <summary>
/// Exception that represents a failed HTTP call to the Altinn Platform,
/// containing an immutable snapshot of the HTTP response, while remaining
/// backward compatible with <see cref="PlatformHttpException"/>.
/// <para>
/// This class derives from <see cref="PlatformHttpException"/> so existing
/// catch blocks continue to work. It passes a sanitized, non-streaming
/// <see cref="HttpResponseMessage"/> to the base class to avoid keeping any
/// live network resources, and it exposes string-based snapshot properties
/// for safe logging and persistence.
/// </para>
/// </summary>
internal sealed class PlatformHttpResponseSnapshotException : PlatformHttpException
{
    /// <summary>
    /// The maximum number of characters captured from the response content.
    /// </summary>
    private const int MaxCapturedContentLength = 16 * 1024; // 16 KB

    /// <summary>
    /// Gets the numeric HTTP status code.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Gets the reason phrase sent by the server, if any.
    /// </summary>
    public string? ReasonPhrase { get; }

    /// <summary>
    /// Gets the HTTP version used by the response (e.g. "1.1", "2.0").
    /// </summary>
    public string HttpVersion { get; }

    /// <summary>
    /// Gets a flattened string representation of all response, content, and trailing headers.
    /// </summary>
    public string Headers { get; }

    /// <summary>
    /// Gets the response body content as a string (possibly truncated).
    /// </summary>
    public string Content { get; }

    /// <summary>
    /// Gets a value indicating whether the content was truncated due to the configured maximum length.
    /// </summary>
    public bool ContentTruncated { get; }

    /// <summary>
    /// Creates a new <see cref="PlatformHttpResponseSnapshotException"/> by snapshotting
    /// the provided <see cref="HttpResponseMessage"/> into immutable string values,
    /// constructing a sanitized clone for the base class, and then disposing the original response.
    /// </summary>
    /// <param name="response">The HTTP response to snapshot and dispose.</param>
    /// <param name="cancellationToken">A cancellation token to cancel reading the content.</param>
    /// <returns>The constructed <see cref="PlatformHttpResponseSnapshotException"/>.</returns>
    public static async Task<PlatformHttpResponseSnapshotException> CreateAndDisposeHttpResponse(
        HttpResponseMessage response,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(response);

        try
        {
            // Snapshot content first (handle null)
            string content = response.Content is null
                ? string.Empty
                : await response.Content.ReadAsStringAsync(cancellationToken);

            bool truncated = content.Length > MaxCapturedContentLength;
            if (truncated)
            {
                content = content[..MaxCapturedContentLength];
            }

            string headers = FlattenHeaders(response.Headers, response.Content?.Headers, response.TrailingHeaders);
            string message = BuildMessage((int)response.StatusCode, response.ReasonPhrase, content, truncated);

            // Build a sanitized, non-streaming HttpResponseMessage for the base class
            var safeResponse = new HttpResponseMessage(response.StatusCode)
            {
                ReasonPhrase = response.ReasonPhrase,
                Version = response.Version,
            };

            // Copy normal headers
            foreach (KeyValuePair<string, IEnumerable<string>> h in response.Headers)
            {
                safeResponse.Headers.TryAddWithoutValidation(h.Key, h.Value);
            }

            // Attach a diagnostic snapshot body for legacy consumers (text only, truncated)
            string mediaType = response.Content?.Headers?.ContentType?.MediaType ?? "text/plain";
            var safeContent = new StringContent(content, Encoding.UTF8, mediaType);
            safeResponse.Content = safeContent;

            // Important: do not copy content headers blindly (avoid Content-Length/Encoding mismatch).
            // StringContent already sets Content-Type (with charset) appropriately.

            // Copy trailing headers if present (HTTP/2+)
            foreach (KeyValuePair<string, IEnumerable<string>> h in response.TrailingHeaders)
            {
                safeResponse.TrailingHeaders.TryAddWithoutValidation(h.Key, h.Value);
            }

            return new PlatformHttpResponseSnapshotException(
                safeResponse,
                statusCode: (int)response.StatusCode,
                reasonPhrase: response.ReasonPhrase,
                httpVersion: response.Version?.ToString() ?? string.Empty,
                headers: headers,
                content: content,
                contentTruncated: truncated,
                message: message
            );
        }
        finally
        {
            try
            {
                response.Dispose();
            }
            catch
            {
                /* ignore dispose failures */
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlatformHttpResponseSnapshotException"/> class.
    /// </summary>
    /// <param name="safeResponse">A sanitized, non-streaming <see cref="HttpResponseMessage"/> suitable for legacy consumers.</param>
    /// <param name="statusCode">The numeric HTTP status code.</param>
    /// <param name="reasonPhrase">The reason phrase sent by the server, if any.</param>
    /// <param name="httpVersion">The HTTP version used by the response.</param>
    /// <param name="headers">A flattened string representation of response, content, and trailing headers.</param>
    /// <param name="content">The response body content as a string (possibly truncated).</param>
    /// <param name="contentTruncated">Whether the content was truncated.</param>
    /// <param name="message">The exception message.</param>
    private PlatformHttpResponseSnapshotException(
        HttpResponseMessage safeResponse,
        int statusCode,
        string? reasonPhrase,
        string httpVersion,
        string headers,
        string content,
        bool contentTruncated,
        string message
    )
        : base(safeResponse, message)
    {
        StatusCode = statusCode;
        ReasonPhrase = reasonPhrase;
        HttpVersion = string.IsNullOrEmpty(httpVersion) ? string.Empty : httpVersion;
        Headers = headers;
        Content = content;
        ContentTruncated = contentTruncated;
    }

    private static string BuildMessage(int statusCode, string? reason, string content, bool truncated)
    {
        StringBuilder sb = new StringBuilder().Append(statusCode).Append(' ').Append(reason ?? string.Empty);
        if (string.IsNullOrEmpty(content))
            return sb.ToString();

        sb.Append(" - ").Append(content);
        if (truncated)
            sb.Append("… [truncated]");
        return sb.ToString();
    }

    private static string FlattenHeaders(
        HttpResponseHeaders? responseHeaders,
        HttpContentHeaders? contentHeaders,
        HttpResponseHeaders? trailingHeaders
    )
    {
        var sb = new StringBuilder();

        Append("Headers", responseHeaders);
        Append("Content-Headers", contentHeaders);
        Append("Trailing-Headers", trailingHeaders);

        return sb.ToString();

        void Append(string prefix, IEnumerable<KeyValuePair<string, IEnumerable<string>>>? headers)
        {
            if (headers is null)
                return;
            foreach ((string key, IEnumerable<string> values) in headers)
            {
                sb.Append(prefix).Append(": ").Append(key).Append(": ").AppendLine(string.Join(", ", values));
            }
        }
    }
}
