using System.Net.Http.Headers;
using System.Text;
using Altinn.App.Core.Exceptions;

namespace Altinn.App.Core.Helpers;

/// <summary>
/// Exception that represents a failed HTTP call to the Altinn Platform,
/// containing an immutable snapshot of the HTTP response.
/// <para>
/// Unlike <see cref="PlatformHttpException"/>, this class does not hold on to a
/// <see cref="HttpResponseMessage"/> instance. Instead, it copies relevant
/// metadata and the response body into strings, making it safe to throw,
/// log, and persist without leaking disposable resources.
/// </para>
/// </summary>
public sealed class PlatformHttpResponseSnapshotException : AltinnException
{
    private const int MaxContentCharacters = 16 * 1024;

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
    /// Gets the response body content as a string.
    /// </summary>
    public string Content { get; }

    /// <summary>
    /// Gets a value indicating whether the content was truncated due to the configured maximum length.
    /// </summary>
    public bool ContentTruncated { get; }

    /// <summary>
    /// Creates a new <see cref="PlatformHttpResponseSnapshotException"/> by snapshotting
    /// the provided <see cref="HttpResponseMessage"/> into immutable string values,
    /// and then disposes the response.
    /// </summary>
    /// <param name="response">The HTTP response to snapshot and dispose.</param>
    /// <param name="cancellationToken">A cancellation token to cancel reading the content.</param>
    public static async Task<PlatformHttpResponseSnapshotException> CreateAndDisposeHttpResponse(
        HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        try
        {
            string content = await response.Content.ReadAsStringAsync(cancellationToken);

            bool truncated = content.Length > MaxContentCharacters;
            if (truncated)
            {
                content = content[..MaxContentCharacters];
            }

            string headers = FlattenHeaders(response.Headers, response.Content?.Headers, response.TrailingHeaders);
            string message = BuildMessage((int)response.StatusCode, response.ReasonPhrase, content, truncated);

            return new PlatformHttpResponseSnapshotException(
                statusCode: (int)response.StatusCode,
                reasonPhrase: response.ReasonPhrase,
                httpVersion: response.Version?.ToString() ?? string.Empty,
                headers: headers,
                content: content,
                contentTruncated: truncated,
                message: message);
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

    private PlatformHttpResponseSnapshotException(
        int statusCode,
        string? reasonPhrase,
        string httpVersion,
        string headers,
        string content,
        bool contentTruncated,
        string message)
        : base(message)
    {
        StatusCode = statusCode;
        ReasonPhrase = reasonPhrase;
        HttpVersion = httpVersion;
        Headers = headers;
        Content = content;
        ContentTruncated = contentTruncated;
    }

    private static string BuildMessage(int statusCode, string? reason, string content, bool truncated)
    {
        StringBuilder sb = new StringBuilder().Append(statusCode).Append(' ').Append(reason ?? string.Empty);
        if (string.IsNullOrEmpty(content))
        {
            return sb.ToString();
        }

        sb.Append(" - ").Append(content);
        if (truncated) sb.Append("… [truncated]");

        return sb.ToString();
    }

    private static string FlattenHeaders(
        HttpResponseHeaders? responseHeaders,
        HttpContentHeaders? contentHeaders,
        HttpResponseHeaders? trailingHeaders)
    {
        var sb = new StringBuilder();

        Append("Headers", responseHeaders);
        Append("Content-Headers", contentHeaders);
        Append("Trailing-Headers", trailingHeaders);

        return sb.ToString();

        void Append(string prefix, IEnumerable<KeyValuePair<string, IEnumerable<string>>>? headers)
        {
            if (headers is null) return;
            foreach ((string key, IEnumerable<string> values) in headers)
            {
                sb.Append(prefix).Append(": ").Append(key).Append(": ")
                    .AppendLine(string.Join(", ", values));
            }
        }
    }
}
