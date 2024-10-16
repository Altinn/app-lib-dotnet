using System.Globalization;
using System.Net.Http.Headers;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Constants;
using Altinn.App.Core.Features.Maskinporten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core;

internal static class CorrespondenceClientDependencyInjection
{
    public static IServiceCollection AddCorrespondenceClient(this IServiceCollection services)
    {
        services.AddHttpClient<ICorrespondenceClient, CorrespondenceClient>();
        return services;
    }
}

public interface ICorrespondenceClient
{
    Task Send(Message message, CancellationToken cancellationToken);
}

internal sealed class CorrespondenceClient : ICorrespondenceClient
{
    private readonly HttpClient _httpClient;
    private readonly IMaskinportenClient _maskinportenClient;
    private readonly PlatformSettings _platformSettings;

    public CorrespondenceClient(
        HttpClient httpClient,
        IMaskinportenClient maskinportenClient,
        IOptions<PlatformSettings> platformSettings
    )
    {
        _httpClient = httpClient;
        _maskinportenClient = maskinportenClient;
        _platformSettings = platformSettings.Value;
    }

    public async Task Send(Message message, CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent();

        var sender = message.Sender.Get(OrganisationNumberFormat.International);

        content.Add(new StringContent(message.ResourceId), "Correspondence.ResourceId");
        content.Add(new StringContent(sender), "Correspondence.Sender");
        content.Add(new StringContent(message.SendersReference), "Correspondence.SendersReference");
        if (!string.IsNullOrWhiteSpace(message.MessageSender))
            content.Add(new StringContent(message.MessageSender), "Correspondence.MessageSender");

        var uri = _platformSettings.ApiCorrespondenceEndpoint.TrimEnd('/') + "/correspondence/upload";

        using var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Content = content;
        var maskinportenToken = await _maskinportenClient.GetAccessToken(
            // TODO: make sure we retrieve this token using the "internal maskinporten client"
            ["altinn/correspondence.write"],
            cancellationToken
        );
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", maskinportenToken.AccessToken);
        request.Headers.TryAddWithoutValidation(General.SubscriptionKeyHeaderName, _platformSettings.SubscriptionKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
    }
}

public sealed record Message(
    string ResourceId,
    // Sender org number
    OrganisationNumber Sender,
    string SendersReference,
    MessageContent Content,
    // User friendly name of the sender
    string? MessageSender = null
);

public enum OrganisationNumberFormat
{
    /// <summary>
    /// Represents only the locally recognised organisation number, e.g. "991825827".
    /// </summary>
    Local,

    /// <summary>
    /// Represents only the locally recognised organisation number, e.g. "0192:991825827".
    /// </summary>
    International,
}

public readonly struct OrganisationNumber : IEquatable<OrganisationNumber>
{
    private readonly string _local;
    private readonly string _international;

    public string Get(OrganisationNumberFormat format) =>
        format switch
        {
            OrganisationNumberFormat.Local => _local,
            OrganisationNumberFormat.International => _international,
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };

    private OrganisationNumber(string local, string international)
    {
        _local = local;
        _international = international;
    }

    public static OrganisationNumber Parse(string value)
    {
        if (!TryParse(value, out var organisationNumber))
        {
            throw new FormatException("Invalid organisation number format.");
        }
        return organisationNumber;
    }

    public static bool TryParse(string value, out OrganisationNumber organisationNumber)
    {
        organisationNumber = default;

        // Either local="991825827" or international="0192:991825827"
        if (value.Length != 9 && value.Length != 14)
            return false;

        string local;
        string international;
        if (value.Length == 9)
        {
            local = value;
            international = "0192:" + value;
        }
        else
        {
            if (!value.StartsWith("0192:", StringComparison.Ordinal))
                return false;
            local = value.Substring(5);
            international = value;
        }

        ReadOnlySpan<int> weights = [3, 2, 7, 6, 5, 4, 3, 2];

        int currentDigit;
        int sum = 0;
        for (int i = 0; i < local.Length - 1; i++)
        {
            if (!int.TryParse(local.AsSpan(i, 1), CultureInfo.InvariantCulture, out currentDigit))
                return false;
            sum += currentDigit * weights[i];
        }

        int ctrlDigit = 11 - (sum % 11);
        if (ctrlDigit == 11)
        {
            ctrlDigit = 0;
        }

        if (!int.TryParse(local.AsSpan(local.Length - 1, 1), CultureInfo.InvariantCulture, out var lastDigit))
            return false;

        if (lastDigit != ctrlDigit)
            return false;

        organisationNumber = new OrganisationNumber(local, international);
        return true;
    }

    public bool Equals(OrganisationNumber other) => _local == other._local;

    public override bool Equals(object? obj) => obj is OrganisationNumber other && Equals(other);

    public override int GetHashCode() => _local.GetHashCode();

    public override string ToString() => _local;

    public static bool operator ==(OrganisationNumber left, OrganisationNumber right) => left.Equals(right);

    public static bool operator !=(OrganisationNumber left, OrganisationNumber right) => !left.Equals(right);
}

public sealed record MessageContent(string Title, string Language, string Summary, string Body);
