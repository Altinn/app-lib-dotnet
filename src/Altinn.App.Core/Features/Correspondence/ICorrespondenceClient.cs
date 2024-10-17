using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Constants;
using Altinn.App.Core.Features.Maskinporten;
using Altinn.App.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Features.Correspondence;

internal static class CorrespondenceClientDependencyInjection
{
    public static IServiceCollection AddCorrespondenceClient(this IServiceCollection services)
    {
        services.AddHttpClient<ICorrespondenceClient, CorrespondenceClient>();
        return services;
    }
}

/// <summary>
///
/// </summary>
public interface ICorrespondenceClient
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task Send(CorrespondenceMessage message, CancellationToken cancellationToken);
}

internal sealed class CorrespondenceClient : ICorrespondenceClient
{
    private readonly ILogger<CorrespondenceClient> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMaskinportenClient _maskinportenClient;
    private readonly PlatformSettings _platformSettings;
    private readonly Telemetry _telemetry;

    public CorrespondenceClient(
        ILogger<CorrespondenceClient> logger,
        IHttpClientFactory httpClientFactory,
        [FromKeyedServices(MaskinportenClient.VariantInternal)] IMaskinportenClient maskinportenClient,
        IOptions<PlatformSettings> platformSettings,
        Telemetry telemetry
    )
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _maskinportenClient = maskinportenClient;
        _platformSettings = platformSettings.Value;
        _telemetry = telemetry;
    }

    public async Task Send(CorrespondenceMessage message, CancellationToken cancellationToken)
    {
        using var activity = _telemetry.StartSendCorrespondenceActivity();
        HttpResponseMessage? response = null;
        string? responseBody = null;
        ProblemDetails? problemDetails = null;
        try
        {
            using var client = _httpClientFactory.CreateClient();

            using var content = new MultipartFormDataContent();

            message.Serialize(content);
            foreach (var attachment in message.Content.Attachments ?? [])
            {
                content.Add(new StreamContent(attachment.Data), "attachments", attachment.FileName ?? "");
            }

            var uri = _platformSettings.ApiCorrespondenceEndpoint.TrimEnd('/') + "/correspondence/upload";

            using var request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Content = content;
            var maskinportenToken = await _maskinportenClient.GetAccessToken(
                ["altinn:correspondence.write"],
                cancellationToken
            );
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", maskinportenToken.AccessToken);
            request.Headers.TryAddWithoutValidation(
                General.SubscriptionKeyHeaderName,
                _platformSettings.SubscriptionKey
            );

            response = await client.SendAsync(request, cancellationToken);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed sending Correspondence message - status={StatusCode} problem.type={}",
                response?.StatusCode,
                problemDetails?.Type
            );
            activity?.Errored(ex);
        }
        finally
        {
            response?.Dispose();
        }
    }
}

/// <summary>
/// Domain model for a Correspondence message
/// </summary>
/// <param name="ResourceId"></param>
/// <param name="Sender"></param>
/// <param name="SendersReference"></param>
/// <param name="Content"></param>
/// <param name="Recipients"></param>
/// <param name="MessageSender"></param>
public sealed record CorrespondenceMessage(
    string ResourceId,
    // Sender org number
    OrganisationNumber Sender,
    string SendersReference,
    MessageContent Content,
    DateTimeOffset? RequestedPublishTime,
    DateTimeOffset? AllowSystemDeleteAfter,
    DateTimeOffset? DueDateTime,
    IReadOnlyList<string> Recipients,
    // User friendly name of the sender
    string? MessageSender = null,
    IReadOnlyList<CorrespondenceExternalReference>? ExternalReferences = null,
    IReadOnlyDictionary<string, string>? PropertyList = null,
    IReadOnlyList<CorrespondenceReplyOptions>? ReplyOptions = null,
    CorrespondeNotification? Notification = null, // TODO: is this not optional?
    bool? IgnoreReservation = null,
    IReadOnlyList<Guid>? ExistingAttachments = null
)
{
    internal void Serialize(MultipartFormDataContent multipartContent)
    {
        var sender = Sender.Get(OrganisationNumberFormat.International);

        multipartContent.Add(new StringContent(ResourceId), "Correspondence.ResourceId");
        multipartContent.Add(new StringContent(sender), "Correspondence.Sender");
        multipartContent.Add(new StringContent(SendersReference), "Correspondence.SendersReference");
        if (!string.IsNullOrWhiteSpace(MessageSender))
            multipartContent.Add(new StringContent(MessageSender), "Correspondence.MessageSender");

        Content.Serialize(multipartContent);

        for (int i = 0; i < Recipients.Count; i++)
        {
            multipartContent.Add(new StringContent(Recipients[i]), $"Correspondence.Recipients[{i}]");
        }

        if (RequestedPublishTime is not null)
            multipartContent.Add(
                new StringContent(RequestedPublishTime.Value.ToString("O")),
                "Correspondence.RequestedPublishTime"
            );
        if (AllowSystemDeleteAfter is not null)
            multipartContent.Add(
                new StringContent(AllowSystemDeleteAfter.Value.ToString("O")),
                "Correspondence.AllowSystemDeleteAfter"
            );
        if (DueDateTime is not null)
            multipartContent.Add(new StringContent(DueDateTime.Value.ToString("O")), "Correspondence.DueDateTime");

        if (ExternalReferences?.Count > 0)
        {
            for (int i = 0; i < ExternalReferences?.Count; i++)
            {
                ExternalReferences[i].Serialize(multipartContent, i);
            }
        }

        if (PropertyList?.Count > 0)
        {
            foreach (var (key, value) in PropertyList)
            {
                multipartContent.Add(new StringContent(value), $"Correspondence.PropertyList.{key}");
            }
        }

        if (ReplyOptions?.Count > 0)
        {
            for (int i = 0; i < ReplyOptions.Count; i++)
            {
                ReplyOptions[i].Serialize(multipartContent, i);
            }
        }

        if (Notification is not null)
            Notification.Serialize(multipartContent);

        if (IgnoreReservation is not null)
            multipartContent.Add(
                new StringContent(IgnoreReservation.Value.ToString()),
                "Correspondence.IgnoreReservation"
            );

        if (ExistingAttachments?.Count > 0)
        {
            for (int i = 0; i < ExistingAttachments.Count; i++)
            {
                multipartContent.Add(
                    new StringContent(ExistingAttachments[i].ToString()),
                    $"Correspondence.ExistingAttachments[{i}]"
                );
            }
        }
    }
}

public sealed record CorrespondeNotification(
    string NotificationTemplate,
    string? EmailSubject,
    string? EmailBody,
    string? SmsBody,
    bool? SendReminder,
    string? ReminderEmailSubject,
    string? ReminderEmailBody,
    string? ReminderSmsBody,
    string? NotificationChannel,
    string? ReminderNotificationChannel,
    string? SendersReference,
    DateTimeOffset? RequestedSendTime
)
{
    internal void Serialize(MultipartFormDataContent content)
    {
        content.Add(new StringContent(NotificationTemplate), "Correspondence.Notification.NotificationTemplate");
        TryAddField(content, "Correspondence.Notification.EmailSubject", EmailSubject);
        TryAddField(content, "Correspondence.Notification.EmailBody", EmailBody);
        TryAddField(content, "Correspondence.Notification.SmsBody", SmsBody);
        TryAddField(content, "Correspondence.Notification.SendReminder", SendReminder?.ToString());
        TryAddField(content, "Correspondence.Notification.ReminderEmailSubject", ReminderEmailSubject);
        TryAddField(content, "Correspondence.Notification.ReminderEmailBody", ReminderEmailBody);
        TryAddField(content, "Correspondence.Notification.ReminderSmsBody", ReminderSmsBody);
        TryAddField(content, "Correspondence.Notification.NotificationChannel", NotificationChannel);
        TryAddField(content, "Correspondence.Notification.ReminderNotificationChannel", ReminderNotificationChannel);
        TryAddField(content, "Correspondence.Notification.SendersReference", SendersReference);
        TryAddField(content, "Correspondence.Notification.RequestedSendTime", RequestedSendTime?.ToString("O"));

        static void TryAddField(MultipartFormDataContent content, string name, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                content.Add(new StringContent(value), name);
        }
    }
}

/// <summary>
///
/// </summary>
/// <param name="ReferenceType"></param>
/// <param name="ReferenceValue"></param>
public sealed record CorrespondenceExternalReference(string ReferenceType, string ReferenceValue)
{
    internal void Serialize(MultipartFormDataContent content, int i)
    {
        content.Add(new StringContent(ReferenceType), $"Correspondence.ExternalReferences[{i}].ReferenceType");
        content.Add(new StringContent(ReferenceValue), $"Correspondence.ExternalReferences[{i}].ReferenceValue");
    }
}

/// <summary>
///
/// </summary>
/// <param name="LinkUrl"></param>
/// <param name="LinkText"></param>
public sealed record CorrespondenceReplyOptions(string LinkUrl, string LinkText)
{
    internal void Serialize(MultipartFormDataContent content, int i)
    {
        content.Add(new StringContent(LinkUrl), $"Correspondence.ReplyOptions[{i}].LinkUrl");
        content.Add(new StringContent(LinkText), $"Correspondence.ReplyOptions[{i}].LinkText");
    }
}

/// <summary>
/// Domain model for a Correspondence message content
/// </summary>
/// <param name="Title"></param>
/// <param name="Language"></param>
/// <param name="Summary"></param>
/// <param name="Body"></param>
/// <param name="Attachments"></param>
public sealed record MessageContent(
    string Title,
    string Language,
    string Summary,
    string Body,
    IReadOnlyList<CorrespondenceAttachment>? Attachments
)
{
    internal void Serialize(MultipartFormDataContent content)
    {
        content.Add(new StringContent(Language), "Correspondence.Content.Language");
        content.Add(new StringContent(Title), "Correspondence.Content.MessageTitle");
        content.Add(new StringContent(Summary), "Correspondence.Content.MessageSummary");
        content.Add(new StringContent(Body), "Correspondence.Content.MessageBody");

        if (Attachments is not null)
        {
            for (int i = 0; i < Attachments.Count; i++)
            {
                var attachment = Attachments[i];
                attachment.Serialize(content, i);
            }
        }
    }
}

/// <summary>
/// Domain model for attachment
/// </summary>
/// <param name="FileName"></param>
/// <param name="Name"></param>
/// <param name="RestrictionName"></param>
/// <param name="IsEncrypted"></param>
/// <param name="Sender"></param>
/// <param name="SenderseReference"></param>
/// <param name="DataType"></param>
/// <param name="DataLocationType"></param>
/// <param name="Data"></param>
public sealed record CorrespondenceAttachment(
    string? FileName,
    string Name,
    string RestrictionName,
    bool? IsEncrypted,
    string Sender,
    string SenderseReference,
    string DataType,
    string DataLocationType,
    Stream Data
)
{
    internal void Serialize(MultipartFormDataContent content, int i)
    {
        const string typePrefix = "Correspondence.Content.Attachments";

        var prefix = $"{typePrefix}[{i}]";
        if (!string.IsNullOrWhiteSpace(FileName))
            content.Add(new StringContent(FileName), $"{prefix}.FileName");
        content.Add(new StringContent(Name), $"{prefix}.Name");
        content.Add(new StringContent(RestrictionName), $"{prefix}.RestrictionName");
        if (IsEncrypted != null)
            content.Add(new StringContent(IsEncrypted.Value.ToString()), $"{prefix}.IsEncrypted");
        content.Add(new StringContent(Sender), $"{prefix}.Sender");
        content.Add(new StringContent(SenderseReference), $"{prefix}.SenderseReference");
        content.Add(new StringContent(DataType), $"{prefix}.DataType");
        content.Add(new StringContent(DataLocationType), $"{prefix}.DataLocationType");
    }
}

/// <summary>
///
/// </summary>
/// <param name="Title"></param>
/// <param name="Language"></param>
/// <param name="Summary"></param>
/// <param name="Body"></param>
// public sealed record MessageContentBuilder(string Title, string Language, string Summary, string Body)
// {
//     public IReadOnlyList<CorrespondenceAttachment>? Attachments { get; init; }

//     public MessageContentBuilder WithAttachments(params IReadOnlyList<CorrespondenceAttachment> attachments) =>
//         this with
//         {
//             Attachments = [.. Attachments, .. attachments]
//         };

//     public MessageContent Build() => new(Title, Language, Summary, Body, Attachments);
// }

// /// <summary>
// ///
// /// </summary>
// /// <param name="ResourceId"></param>
// /// <param name="Sender"></param>
// /// <param name="SendersReference"></param>
// /// <param name="Content"></param>
// /// <param name="Recipients"></param>
// public sealed record CorrespondenceMessageBuilder(
//     string ResourceId,
//     // Sender org number
//     OrganisationNumber Sender,
//     string SendersReference,
//     MessageContentBuilder Content,
//     IReadOnlyList<string> Recipients
// )
// {
//     public DateTimeOffset? RequestedPublishTime { get; init; }
//     public DateTimeOffset? AllowSystemDeleteAfter { get; init; }
//     public DateTimeOffset? DueDateTime { get; init; }
//     public IReadOnlyList<string>? ExternalReferences { get; init; }
//     public string? MessageSender { get; init; }

//     public CorrespondenceMessageBuilder WithRequestedPublishTime(DateTimeOffset requestedPublishTime) =>
//         this with
//         {
//             RequestedPublishTime = requestedPublishTime
//         };

//     public CorrespondenceMessageBuilder WithContentAttachments(
//         params IReadOnlyList<CorrespondenceAttachment> attachments
//     ) => this with { Content = Content.WithAttachments(attachments) };

//     public CorrespondenceMessage Build() =>
//         new(
//             ResourceId,
//             Sender,
//             SendersReference,
//             Content.Build(),
//             Recipients,
//             RequestedPublishTime,
//             AllowSystemDeleteAfter,
//             DueDateTime,
//             ExternalReferences,
//             MessageSender
//         );
// }
