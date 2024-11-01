using System.Text;
using Altinn.App.Core.Features.Correspondence.Builder;
using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Models;
using Altinn.App.Core.Tests.Models;
using FluentAssertions;

namespace Altinn.App.Core.Tests.Features.Correspondence;

public class CorrespondenceBuilderTests
{
    static OrganisationNumber GetOrganisationNumber(int index)
    {
        var i = index % OrganisationNumberTests.ValidOrganisationNumbers.Length;
        return OrganisationNumber.Parse(OrganisationNumberTests.ValidOrganisationNumbers[i]);
    }

    static string StreamReadHelper(Stream stream)
    {
        stream.Position = 0;
        return new StreamReader(stream).ReadToEnd();
    }

    [Fact]
    public void Build_WithOnlyRequiredProperties_ShouldReturnValidCorrespondence()
    {
        // Arrange
        OrganisationNumber sender = GetOrganisationNumber(1);
        IReadOnlyList<OrganisationNumber> recipients = [GetOrganisationNumber(1), GetOrganisationNumber(2)];
        string resourceId = "resource-id";
        string sendersReference = "sender-reference";
        DateTimeOffset dueDateTime = DateTimeOffset.UtcNow.AddDays(30);
        DateTimeOffset allowSystemDeleteAfter = DateTimeOffset.UtcNow.AddDays(60);
        string contentTitle = "content-title";
        LanguageCode<ISO_639_1> contentLanguage = LanguageCode<ISO_639_1>.Parse("no");
        string contentSummary = "content-summary";
        string contentBody = "content-body";

        var builder = CorrespondenceBuilder
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

    // TODO: Finish this
    [Fact]
    public void Build_WithAllProperties_ShouldReturnValidCorrespondence()
    {
        // Arrange
        var sender = GetOrganisationNumber(1);
        var recipient = GetOrganisationNumber(2);
        var data = new
        {
            sender,
            recipient,
            resourceId = "resource-id",
            sendersReference = "senders-ref",
            due = DateTimeOffset.Now.AddDays(30),
            deleteAfter = DateTimeOffset.Now.AddDays(60),
            ignoreReservation = true,
            requestedPublishTime = DateTimeOffset.Now.AddSeconds(45),
            propertyList = new Dictionary<string, string> { ["prop1"] = "value1", ["prop2"] = "value2" },
            content = new
            {
                title = "content-title",
                language = LanguageCode<ISO_639_1>.Parse("en"),
                summary = "content-summary",
                body = "content-body"
            },
            notifications = new[]
            {
                new
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
                    sendReminder = true
                },
                new
                {
                    template = CorrespondenceNotificationTemplate.CustomMessage,
                    emailSubject = "email-subject-2",
                    emailBody = "email-body-2",
                    smsBody = "sms-body-2",
                    reminderEmailSubject = "reminder-email-subject-2",
                    reminderEmailBody = "reminder-email-body-2",
                    reminderSmsBody = "reminder-sms-body-2",
                    requestedSendTime = DateTimeOffset.Now.AddDays(2),
                    sendersReference = "notification-senders-ref-2",
                    sendReminder = false
                }
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
                },
                new
                {
                    sender,
                    filename = "file-3.txt",
                    name = "File 3",
                    sendersReference = "1234-3",
                    dataType = "text/plain",
                    data = "attachment-data-3",
                    dataLocationType = CorrespondenceDataLocationType.ExisitingExternalStorage,
                    isEncrypted = false,
                    restrictionName = "restriction-name-3"
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
                new { url = "reply-url-2", text = "reply-text-2" },
                new { url = "reply-url-3", text = "reply-text-3" }
            }
        };

        var builder = CorrespondenceBuilder
            .Create()
            .WithResourceId(data.resourceId)
            .WithSender(data.sender)
            .WithSendersReference(data.sendersReference)
            .WithRecipient(data.recipient)
            .WithDueDateTime(data.due)
            .WithAllowSystemDeleteAfter(data.deleteAfter)
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
                    .WithNotificationTemplate(data.notifications[0].template)
                    .WithEmailSubject(data.notifications[0].emailSubject)
                    .WithEmailBody(data.notifications[0].emailBody)
                    .WithSmsBody(data.notifications[0].smsBody)
                    .WithReminderEmailSubject(data.notifications[0].reminderEmailSubject)
                    .WithReminderEmailBody(data.notifications[0].reminderEmailBody)
                    .WithReminderSmsBody(data.notifications[0].reminderSmsBody)
                    .WithRequestedSendTime(data.notifications[0].requestedSendTime)
                    .WithSendersReference(data.notifications[0].sendersReference)
                    .WithSendReminder(data.notifications[0].sendReminder)
            )
            .WithNotification(
                new CorrespondenceNotification
                {
                    NotificationTemplate = data.notifications[1].template,
                    EmailSubject = data.notifications[1].emailSubject,
                    EmailBody = data.notifications[1].emailBody,
                    SmsBody = data.notifications[1].smsBody,
                    ReminderEmailSubject = data.notifications[1].reminderEmailSubject,
                    ReminderEmailBody = data.notifications[1].reminderEmailBody,
                    ReminderSmsBody = data.notifications[1].reminderSmsBody,
                    RequestedSendTime = data.notifications[1].requestedSendTime,
                    SendersReference = data.notifications[1].sendersReference,
                    SendReminder = data.notifications[1].sendReminder
                }
            )
            .WithIgnoreReservation(data.ignoreReservation)
            .WithRequestedPublishTime(data.requestedPublishTime)
            .WithPropertyList(data.propertyList)
            .WithAttachment(
                CorrespondenceAttachmentBuilder
                    .Create()
                    .WithFilename(data.attachments[0].filename)
                    .WithName(data.attachments[0].name)
                    .WithSender(data.attachments[0].sender)
                    .WithSendersReference(data.attachments[0].sendersReference)
                    .WithDataType(data.attachments[0].dataType)
                    .WithData(new MemoryStream(Encoding.UTF8.GetBytes(data.attachments[0].data)))
                    .WithDataLocationType(data.attachments[0].dataLocationType)
                    .WithIsEncrypted(data.attachments[0].isEncrypted)
                    .WithRestrictionName(data.attachments[0].restrictionName)
            )
            .WithAttachment(
                new CorrespondenceAttachment
                {
                    Filename = data.attachments[1].filename,
                    Name = data.attachments[1].name,
                    Sender = data.attachments[1].sender,
                    SendersReference = data.attachments[1].sendersReference,
                    DataType = data.attachments[1].dataType,
                    Data = new MemoryStream(Encoding.UTF8.GetBytes(data.attachments[1].data)),
                    DataLocationType = data.attachments[1].dataLocationType,
                    IsEncrypted = data.attachments[1].isEncrypted,
                    RestrictionName = data.attachments[1].restrictionName
                }
            )
            .WithAttachments(
                [
                    new CorrespondenceAttachment
                    {
                        Filename = data.attachments[2].filename,
                        Name = data.attachments[2].name,
                        Sender = data.attachments[2].sender,
                        SendersReference = data.attachments[2].sendersReference,
                        DataType = data.attachments[2].dataType,
                        Data = new MemoryStream(Encoding.UTF8.GetBytes(data.attachments[2].data)),
                        DataLocationType = data.attachments[2].dataLocationType,
                        IsEncrypted = data.attachments[2].isEncrypted,
                        RestrictionName = data.attachments[2].restrictionName
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
            .WithExternalReference(
                new CorrespondenceExternalReference
                {
                    ReferenceType = data.externalReferences[1].type,
                    ReferenceValue = data.externalReferences[1].value
                }
            )
            .WithExternalReferences(
                data.externalReferences.Skip(2)
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
            .WithReplyOption(
                CorrespondenceReplyOptionBuilder
                    .Create()
                    .WithLinkUrl(data.replyOptions[1].url)
                    .WithLinkText(data.replyOptions[1].text)
                    .Build()
            )
            .WithReplyOptions(
                [
                    new CorrespondenceReplyOption
                    {
                        LinkUrl = data.replyOptions[2].url,
                        LinkText = data.replyOptions[2].text
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

        correspondence.Content.Attachments.Should().HaveCount(3);
        for (int i = 0; i < data.attachments.Length; i++)
        {
            correspondence.Content.Attachments[i].Filename.Should().Be(data.attachments[i].filename);
            correspondence.Content.Attachments[i].Name.Should().Be(data.attachments[i].name);
            correspondence.Content.Attachments[i].RestrictionName.Should().Be(data.attachments[i].restrictionName);
            correspondence.Content.Attachments[i].IsEncrypted.Should().Be(data.attachments[i].isEncrypted);
            correspondence.Content.Attachments[i].Sender.Should().Be(data.attachments[i].sender);
            correspondence.Content.Attachments[i].SendersReference.Should().Be(data.attachments[i].sendersReference);
            correspondence.Content.Attachments[i].DataType.Should().Be(data.attachments[i].dataType);
            correspondence.Content.Attachments[i].DataLocationType.Should().Be(data.attachments[i].dataLocationType);
            StreamReadHelper(correspondence.Content.Attachments[i].Data).Should().Be(data.attachments[i].data);
        }

        // TODO: All the other ones
    }
}
