using System.Text;
using Altinn.App.Core.Features.Correspondence.Builder;
using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Models;
using FluentAssertions;

namespace Altinn.App.Core.Tests.Features.Correspondence.Builder;

public class CorrespondenceBuilderTests
{
    private static string StreamReadHelper(Stream stream)
    {
        stream.Position = 0;
        return new StreamReader(stream).ReadToEnd();
    }

    [Fact]
    public void Build_WithOnlyRequiredProperties_ShouldReturnValidCorrespondence()
    {
        // Arrange
        OrganisationNumber sender = TestHelpers.GetOrganisationNumber(1);
        IReadOnlyList<OrganisationNumber> recipients =
        [
            TestHelpers.GetOrganisationNumber(1),
            TestHelpers.GetOrganisationNumber(2)
        ];
        string resourceId = "resource-id";
        string sendersReference = "sender-reference";
        DateTimeOffset dueDateTime = DateTimeOffset.UtcNow.AddDays(30);
        DateTimeOffset allowSystemDeleteAfter = DateTimeOffset.UtcNow.AddDays(60);
        string contentTitle = "content-title";
        LanguageCode<Iso6391> contentLanguage = LanguageCode<Iso6391>.Parse("no");
        string contentSummary = "content-summary";
        string contentBody = "content-body";

        var builder = CorrespondenceRequestBuilder
            .Create()
            .WithResourceId(resourceId)
            .WithSender(sender)
            .WithSendersReference(sendersReference)
            .WithRecipients(recipients)
            .WithDueDateTime(dueDateTime)
            .WithAllowSystemDeleteAfter(allowSystemDeleteAfter)
            .WithContent(
                CorrespondenceContentBuilder
                    .Create()
                    .WithTitle(contentTitle)
                    .WithLanguage(contentLanguage)
                    .WithSummary(contentSummary)
                    .WithBody(contentBody)
            );

        // Act
        var correspondence = builder.Build();

        // Assert
        correspondence.Should().NotBeNull();
        correspondence.ResourceId.Should().Be("resource-id");
        correspondence.Sender.Should().Be(sender);
        correspondence.SendersReference.Should().Be("sender-reference");
        correspondence.AllowSystemDeleteAfter.Should().BeExactly(allowSystemDeleteAfter);
        correspondence.DueDateTime.Should().BeExactly(dueDateTime);
        correspondence.Recipients.Should().BeEquivalentTo(recipients);
        correspondence.Content.Title.Should().Be(contentTitle);
        correspondence.Content.Language.Should().Be(contentLanguage);
        correspondence.Content.Summary.Should().Be(contentSummary);
        correspondence.Content.Body.Should().Be(contentBody);
    }

    [Fact]
    public void Build_WithAllProperties_ShouldReturnValidCorrespondence()
    {
        // Arrange
        var sender = TestHelpers.GetOrganisationNumber(1);
        var recipient = TestHelpers.GetOrganisationNumber(2);
        var data = new
        {
            sender,
            recipient,
            messageSender = "message-sender",
            resourceId = "resource-id",
            sendersReference = "senders-ref",
            dueDateTime = DateTimeOffset.Now.AddDays(30),
            allowDeleteAfter = DateTimeOffset.Now.AddDays(60),
            ignoreReservation = true,
            requestedPublishTime = DateTimeOffset.Now.AddSeconds(45),
            propertyList = new Dictionary<string, string> { ["prop1"] = "value1", ["prop2"] = "value2" },
            content = new
            {
                title = "content-title",
                language = LanguageCode<Iso6391>.Parse("en"),
                summary = "content-summary",
                body = "content-body"
            },
            notification = new
            {
                template = CorrespondenceNotificationTemplate.GenericAltinnMessage,
                emailSubject = "email-subject-1",
                emailBody = "email-body-1",
                smsBody = "sms-body-1",
                reminderEmailSubject = "reminder-email-subject-1",
                reminderEmailBody = "reminder-email-body-1",
                reminderSmsBody = "reminder-sms-body-1",
                requestedSendTime = DateTimeOffset.Now.AddDays(1),
                sendersReference = "notification-senders-ref-1",
                sendReminder = true,
                notificationChannel = CorrespondenceNotificationChannel.EmailPreferred,
                reminderNotificationChannel = CorrespondenceNotificationChannel.SmsPreferred
            },
            attachments = new[]
            {
                new
                {
                    sender,
                    filename = "file-1.txt",
                    name = "File 1",
                    sendersReference = "1234-1",
                    dataType = "text/plain",
                    data = "attachment-data-1",
                    dataLocationType = CorrespondenceDataLocationType.ExistingCorrespondenceAttachment,
                    isEncrypted = false,
                    restrictionName = "restriction-name-1"
                },
                new
                {
                    sender,
                    filename = "file-2.txt",
                    name = "File 2",
                    sendersReference = "1234-2",
                    dataType = "text/plain",
                    data = "attachment-data-2",
                    dataLocationType = CorrespondenceDataLocationType.NewCorrespondenceAttachment,
                    isEncrypted = true,
                    restrictionName = "restriction-name-2"
                }
            },
            existingAttachments = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },
            externalReferences = new[]
            {
                new { type = CorrespondenceReferenceType.Generic, value = "ref-1" },
                new { type = CorrespondenceReferenceType.AltinnAppInstance, value = "ref-2" },
                new { type = CorrespondenceReferenceType.DialogportenDialogId, value = "ref-3" },
                new { type = CorrespondenceReferenceType.DialogportenProcessId, value = "ref-4" },
                new { type = CorrespondenceReferenceType.AltinnBrokerFileTransfer, value = "ref-5" }
            },
            replyOptions = new[]
            {
                new { url = "reply-url-1", text = "reply-text-1" },
                new { url = "reply-url-2", text = "reply-text-2" }
            }
        };

        var builder = CorrespondenceRequestBuilder
            .Create()
            .WithResourceId(data.resourceId)
            .WithSender(data.sender)
            .WithSendersReference(data.sendersReference)
            .WithRecipient(data.recipient)
            .WithDueDateTime(data.dueDateTime)
            .WithAllowSystemDeleteAfter(data.allowDeleteAfter)
            .WithContent(
                CorrespondenceContentBuilder
                    .Create()
                    .WithTitle(data.content.title)
                    .WithLanguage(data.content.language)
                    .WithSummary(data.content.summary)
                    .WithBody(data.content.body)
            )
            .WithNotification(
                CorrespondenceNotificationBuilder
                    .Create()
                    .WithNotificationTemplate(data.notification.template)
                    .WithEmailSubject(data.notification.emailSubject)
                    .WithEmailBody(data.notification.emailBody)
                    .WithSmsBody(data.notification.smsBody)
                    .WithReminderEmailSubject(data.notification.reminderEmailSubject)
                    .WithReminderEmailBody(data.notification.reminderEmailBody)
                    .WithReminderSmsBody(data.notification.reminderSmsBody)
                    .WithRequestedSendTime(data.notification.requestedSendTime)
                    .WithSendersReference(data.notification.sendersReference)
                    .WithSendReminder(data.notification.sendReminder)
                    .WithNotificationChannel(data.notification.notificationChannel)
                    .WithReminderNotificationChannel(data.notification.reminderNotificationChannel)
            )
            .WithMessageSender(data.messageSender)
            .WithIgnoreReservation(data.ignoreReservation)
            .WithRequestedPublishTime(data.requestedPublishTime)
            .WithPropertyList(data.propertyList)
            .WithAttachment(
                CorrespondenceAttachmentBuilder
                    .Create()
                    .WithFilename(data.attachments[0].filename)
                    .WithName(data.attachments[0].name)
                    .WithSendersReference(data.attachments[0].sendersReference)
                    .WithDataType(data.attachments[0].dataType)
                    .WithData(new MemoryStream(Encoding.UTF8.GetBytes(data.attachments[0].data)))
                    .WithDataLocationType(data.attachments[0].dataLocationType)
                    .WithIsEncrypted(data.attachments[0].isEncrypted)
                    .WithRestrictionName(data.attachments[0].restrictionName)
            )
            .WithAttachments(
                [
                    new CorrespondenceAttachment
                    {
                        Filename = data.attachments[1].filename,
                        Name = data.attachments[1].name,
                        SendersReference = data.attachments[1].sendersReference,
                        DataType = data.attachments[1].dataType,
                        Data = new MemoryStream(Encoding.UTF8.GetBytes(data.attachments[1].data)),
                        DataLocationType = data.attachments[1].dataLocationType,
                        IsEncrypted = data.attachments[1].isEncrypted,
                        RestrictionName = data.attachments[1].restrictionName
                    }
                ]
            )
            .WithExistingAttachment(data.existingAttachments[0])
            .WithExistingAttachments(data.existingAttachments.Skip(1).ToList())
            .WithExternalReference(
                CorrespondenceExternalReferenceBuilder
                    .Create()
                    .WithReferenceType(data.externalReferences[0].type)
                    .WithReferenceValue(data.externalReferences[0].value)
            )
            .WithExternalReferences(
                data.externalReferences.Skip(1)
                    .Select(x => new CorrespondenceExternalReference
                    {
                        ReferenceType = x.type,
                        ReferenceValue = x.value
                    })
                    .ToList()
            )
            .WithReplyOption(
                CorrespondenceReplyOptionBuilder
                    .Create()
                    .WithLinkUrl(data.replyOptions[0].url)
                    .WithLinkText(data.replyOptions[0].text)
            )
            .WithReplyOptions(
                [
                    new CorrespondenceReplyOption
                    {
                        LinkUrl = data.replyOptions[1].url,
                        LinkText = data.replyOptions[1].text
                    }
                ]
            );

        // Act
        var correspondence = builder.Build();

        // Assert
        Assert.NotNull(correspondence);
        Assert.NotNull(correspondence.Content);
        Assert.NotNull(correspondence.Content.Attachments);
        Assert.NotNull(correspondence.Notification);
        Assert.NotNull(correspondence.ExternalReferences);
        Assert.NotNull(correspondence.ReplyOptions);

        correspondence.ResourceId.Should().Be(data.resourceId);
        correspondence.Sender.Should().Be(data.sender);
        correspondence.SendersReference.Should().Be(data.sendersReference);
        correspondence.Recipients.Should().BeEquivalentTo([data.recipient]);
        correspondence.DueDateTime.Should().Be(data.dueDateTime);
        correspondence.AllowSystemDeleteAfter.Should().Be(data.allowDeleteAfter);
        correspondence.IgnoreReservation.Should().Be(data.ignoreReservation);
        correspondence.RequestedPublishTime.Should().Be(data.requestedPublishTime);
        correspondence.PropertyList.Should().BeEquivalentTo(data.propertyList);
        correspondence.MessageSender.Should().Be(data.messageSender);

        correspondence.Content.Title.Should().Be(data.content.title);
        correspondence.Content.Language.Should().Be(data.content.language);
        correspondence.Content.Summary.Should().Be(data.content.summary);
        correspondence.Content.Body.Should().Be(data.content.body);
        correspondence.Content.Attachments.Should().HaveCount(data.attachments.Length);
        for (int i = 0; i < data.attachments.Length; i++)
        {
            correspondence.Content.Attachments[i].Filename.Should().Be(data.attachments[i].filename);
            correspondence.Content.Attachments[i].Name.Should().Be(data.attachments[i].name);
            correspondence.Content.Attachments[i].RestrictionName.Should().Be(data.attachments[i].restrictionName);
            correspondence.Content.Attachments[i].IsEncrypted.Should().Be(data.attachments[i].isEncrypted);
            correspondence.Content.Attachments[i].SendersReference.Should().Be(data.attachments[i].sendersReference);
            correspondence.Content.Attachments[i].DataType.Should().Be(data.attachments[i].dataType);
            correspondence.Content.Attachments[i].DataLocationType.Should().Be(data.attachments[i].dataLocationType);
            StreamReadHelper(correspondence.Content.Attachments[i].Data).Should().Be(data.attachments[i].data);
        }

        correspondence.Notification.NotificationTemplate.Should().Be(data.notification.template);
        correspondence.Notification.EmailSubject.Should().Be(data.notification.emailSubject);
        correspondence.Notification.EmailBody.Should().Be(data.notification.emailBody);
        correspondence.Notification.SmsBody.Should().Be(data.notification.smsBody);
        correspondence.Notification.ReminderEmailSubject.Should().Be(data.notification.reminderEmailSubject);
        correspondence.Notification.ReminderEmailBody.Should().Be(data.notification.reminderEmailBody);
        correspondence.Notification.ReminderSmsBody.Should().Be(data.notification.reminderSmsBody);
        correspondence.Notification.RequestedSendTime.Should().Be(data.notification.requestedSendTime);
        correspondence.Notification.SendersReference.Should().Be(data.notification.sendersReference);
        correspondence.Notification.SendReminder.Should().Be(data.notification.sendReminder);
        correspondence.Notification.NotificationChannel.Should().Be(data.notification.notificationChannel);
        correspondence
            .Notification.ReminderNotificationChannel.Should()
            .Be(data.notification.reminderNotificationChannel);

        correspondence.ExistingAttachments.Should().BeEquivalentTo(data.existingAttachments);

        correspondence.ExternalReferences.Should().HaveCount(data.externalReferences.Length);
        for (int i = 0; i < data.externalReferences.Length; i++)
        {
            correspondence.ExternalReferences[i].ReferenceType.Should().Be(data.externalReferences[i].type);
            correspondence.ExternalReferences[i].ReferenceValue.Should().Be(data.externalReferences[i].value);
        }

        correspondence.ReplyOptions.Should().HaveCount(data.replyOptions.Length);
        for (int i = 0; i < data.replyOptions.Length; i++)
        {
            correspondence.ReplyOptions[i].LinkUrl.Should().Be(data.replyOptions[i].url);
            correspondence.ReplyOptions[i].LinkText.Should().Be(data.replyOptions[i].text);
        }
    }

    // TODO: This isn't exhaustive, but it's so boring to write up all permutations... do it anyway, some day.
    [Fact]
    public void Builder_UpdatesAndOverwritesValuesCorrectly()
    {
        // Arrange
        var builder = CorrespondenceRequestBuilder
            .Create()
            .WithResourceId("resourceId-1")
            .WithSender(TestHelpers.GetOrganisationNumber(1))
            .WithSendersReference("sender-reference-1")
            .WithRecipient(TestHelpers.GetOrganisationNumber(1))
            .WithDueDateTime(DateTimeOffset.UtcNow.AddDays(1))
            .WithAllowSystemDeleteAfter(DateTimeOffset.UtcNow.AddDays(1))
            .WithContent(
                CorrespondenceContentBuilder
                    .Create()
                    .WithTitle("content-title-1")
                    .WithLanguage(LanguageCode<Iso6391>.Parse("no"))
                    .WithSummary("content-summary-1")
                    .WithBody("content-body-1")
            )
            .WithNotification(
                CorrespondenceNotificationBuilder
                    .Create()
                    .WithNotificationTemplate(CorrespondenceNotificationTemplate.GenericAltinnMessage)
                    .WithEmailBody("email-body-1")
            );

        builder.WithResourceId("resourceId-2");
        builder.WithSender(TestHelpers.GetOrganisationNumber(2));
        builder.WithSendersReference("sender-reference-2");
        builder.WithRecipient(TestHelpers.GetOrganisationNumber(2));
        builder.WithRecipients([TestHelpers.GetOrganisationNumber(3), TestHelpers.GetOrganisationNumber(4)]);
        builder.WithDueDateTime(DateTimeOffset.UtcNow.AddDays(2));
        builder.WithAllowSystemDeleteAfter(DateTimeOffset.UtcNow.AddDays(2));
        builder.WithContent(
            CorrespondenceContentBuilder
                .Create()
                .WithTitle("content-title-2")
                .WithLanguage(LanguageCode<Iso6391>.Parse("en"))
                .WithSummary("content-summary-2")
                .WithBody("content-body-2")
        );
        builder.WithNotification(
            CorrespondenceNotificationBuilder
                .Create()
                .WithNotificationTemplate(CorrespondenceNotificationTemplate.CustomMessage)
                .WithEmailBody("email-body-2")
        );

        // Act
        var correspondence = builder.Build();

        // Assert
        Assert.NotNull(correspondence);
        Assert.NotNull(correspondence.Notification);

        correspondence.ResourceId.Should().Be("resourceId-2");
        correspondence.Sender.Should().Be(TestHelpers.GetOrganisationNumber(2));
        correspondence.SendersReference.Should().Be("sender-reference-2");
        correspondence.AllowSystemDeleteAfter.Should().BeSameDateAs(DateTimeOffset.UtcNow.AddDays(2));
        correspondence.DueDateTime.Should().BeSameDateAs(DateTimeOffset.UtcNow.AddDays(2));

        correspondence.Recipients.Should().HaveCount(4);
        correspondence
            .Recipients.Should()
            .BeEquivalentTo(
                [
                    TestHelpers.GetOrganisationNumber(1),
                    TestHelpers.GetOrganisationNumber(2),
                    TestHelpers.GetOrganisationNumber(3),
                    TestHelpers.GetOrganisationNumber(4)
                ]
            );

        correspondence.Content.Title.Should().Be("content-title-2");
        correspondence.Content.Language.Should().Be(LanguageCode<Iso6391>.Parse("en"));
        correspondence.Content.Summary.Should().Be("content-summary-2");
        correspondence.Content.Body.Should().Be("content-body-2");

        correspondence.Notification.NotificationTemplate.Should().Be(CorrespondenceNotificationTemplate.CustomMessage);
        correspondence.Notification.EmailBody.Should().Be("email-body-2");
    }
}
