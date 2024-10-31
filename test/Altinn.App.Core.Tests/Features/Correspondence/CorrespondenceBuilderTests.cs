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
        var builder = CorrespondenceBuilder
            .Create()
            .WithResourceId("123")
            .WithSender(sender)
            .WithSendersReference("123")
            .WithRecipient(recipient)
            .WithDueDateTime(DateTimeOffset.Now.AddDays(30))
            .WithAllowSystemDeleteAfter(DateTimeOffset.Now.AddDays(60))
            .WithContent(
                CorrespondenceContentBuilder
                    .Create()
                    .WithTitle("Title")
                    .WithLanguage(LanguageCode<ISO_639_1>.Parse("en"))
                    .WithSummary("Summary")
                    .WithBody("Body")
                    .WithAttachment(
                        CorrespondenceAttachmentBuilder
                            .Create()
                            .WithFilename("file.txt")
                            .WithName("File")
                            .WithSender(sender)
                            .WithSendersReference("123")
                            .WithDataType("text/plain")
                            .WithData(new MemoryStream(Encoding.UTF8.GetBytes("Hello, World!")))
                            .WithDataLocationType(CorrespondenceDataLocationType.ExistingCorrespondenceAttachment)
                            .WithIsEncrypted(false)
                            .WithRestrictionName("restriction-name")
                    )
                    .WithAttachment(
                        new CorrespondenceAttachment
                        {
                            Filename = "file.txt",
                            Name = "File",
                            Sender = sender,
                            SendersReference = "123",
                            DataType = "text/plain",
                            Data = new MemoryStream(Encoding.UTF8.GetBytes("Hello, World!")),
                            DataLocationType = CorrespondenceDataLocationType.ExistingCorrespondenceAttachment,
                            IsEncrypted = false,
                            RestrictionName = "restriction-name"
                        }
                    )
                    .WithAttachments(
                        [
                            new CorrespondenceAttachment
                            {
                                Filename = "file.txt",
                                Name = "File",
                                Sender = sender,
                                SendersReference = "123",
                                DataType = "text/plain",
                                Data = new MemoryStream(Encoding.UTF8.GetBytes("Hello, World!")),
                                DataLocationType = CorrespondenceDataLocationType.ExistingCorrespondenceAttachment,
                                IsEncrypted = false,
                                RestrictionName = "restriction-name"
                            }
                        ]
                    )
            )
            .WithNotification(
                CorrespondenceNotificationBuilder
                    .Create()
                    .WithNotificationTemplate(CorrespondenceNotificationTemplate.GenericAltinnMessage)
                    .WithEmailSubject("Email subject")
                    .WithEmailBody("Email body")
                    .WithSmsBody("SMS body")
                    .WithReminderEmailSubject("Reminder email subject")
                    .WithReminderEmailBody("Reminder email body")
                    .WithReminderSmsBody("Reminder SMS body")
                    .WithRequestedSendTime(DateTimeOffset.Now.AddDays(1))
                    .WithSendersReference("123")
                    .WithSendReminder(true)
            )
            .WithNotification(
                new CorrespondenceNotification
                {
                    NotificationTemplate = CorrespondenceNotificationTemplate.GenericAltinnMessage,
                    EmailSubject = "Email subject",
                    EmailBody = "Email body",
                    SmsBody = "SMS body",
                    ReminderEmailSubject = "Reminder email subject",
                    ReminderEmailBody = "Reminder email body",
                    ReminderSmsBody = "Reminder SMS body",
                    RequestedSendTime = DateTimeOffset.Now.AddDays(1),
                    SendersReference = "123",
                    SendReminder = true
                }
            )
            .WithIgnoreReservation(true)
            .WithRequestedPublishTime(DateTimeOffset.Now.AddDays(1))
            .WithPropertyList(new Dictionary<string, string> { ["key"] = "value" })
            .WithAttachment(
                CorrespondenceAttachmentBuilder
                    .Create()
                    .WithFilename("file.txt")
                    .WithName("File")
                    .WithSender(sender)
                    .WithSendersReference("123")
                    .WithDataType("text/plain")
                    .WithData(new MemoryStream(Encoding.UTF8.GetBytes("Hello, World!")))
                    .WithDataLocationType(CorrespondenceDataLocationType.ExistingCorrespondenceAttachment)
                    .WithIsEncrypted(false)
                    .WithRestrictionName("restriction-name")
            )
            .WithAttachment(
                new CorrespondenceAttachment
                {
                    Filename = "file.txt",
                    Name = "File",
                    Sender = sender,
                    SendersReference = "123",
                    DataType = "text/plain",
                    Data = new MemoryStream(Encoding.UTF8.GetBytes("Hello, World!")),
                    DataLocationType = CorrespondenceDataLocationType.ExistingCorrespondenceAttachment,
                    IsEncrypted = false,
                    RestrictionName = "restriction-name"
                }
            )
            .WithAttachments(
                [
                    new CorrespondenceAttachment
                    {
                        Filename = "file.txt",
                        Name = "File",
                        Sender = sender,
                        SendersReference = "123",
                        DataType = "text/plain",
                        Data = new MemoryStream(Encoding.UTF8.GetBytes("Hello, World!")),
                        DataLocationType = CorrespondenceDataLocationType.ExistingCorrespondenceAttachment,
                        IsEncrypted = false,
                        RestrictionName = "restriction-name"
                    },
                    new CorrespondenceAttachment
                    {
                        Filename = "file.txt",
                        Name = "File",
                        Sender = sender,
                        SendersReference = "123",
                        DataType = "text/plain",
                        Data = new MemoryStream(Encoding.UTF8.GetBytes("Hello, World!")),
                        DataLocationType = CorrespondenceDataLocationType.ExistingCorrespondenceAttachment,
                        IsEncrypted = false,
                        RestrictionName = "restriction-name"
                    }
                ]
            )
            .WithExistingAttachment(Guid.NewGuid())
            .WithExistingAttachments([Guid.NewGuid()])
            .WithExternalReference(
                CorrespondenceExternalReferenceBuilder
                    .Create()
                    .WithReferenceType(CorrespondenceReferenceType.Generic)
                    .WithReferenceValue("value")
            )
            .WithExternalReference(
                new CorrespondenceExternalReference
                {
                    ReferenceType = CorrespondenceReferenceType.Generic,
                    ReferenceValue = "value"
                }
            )
            .WithExternalReferences(
                [
                    new CorrespondenceExternalReference
                    {
                        ReferenceType = CorrespondenceReferenceType.Generic,
                        ReferenceValue = "value"
                    }
                ]
            )
            .WithReplyOption(CorrespondenceReplyOptionBuilder.Create().WithLinkUrl("url").WithLinkText("text"))
            .WithReplyOption(CorrespondenceReplyOptionBuilder.Create().WithLinkUrl("url").WithLinkText("text").Build())
            .WithReplyOptions([new CorrespondenceReplyOption { LinkUrl = "url" }]);

        // Act
        var correspondence = builder.Build();

        // Assert
        correspondence.Content.Attachments.Should().HaveCount(7);
    }
}
