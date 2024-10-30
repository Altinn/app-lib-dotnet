using System.Text;
using Altinn.App.Core.Features.Correspondence.Builder;
using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Correspondence;

public class DevExTester
{
    public void Test()
    {
        var correspondence = CorrespondenceBuilder
            .Create()
            .WithResourceId("123")
            .WithSender(OrganisationNumber.Parse("123456789"))
            .WithSendersReference("123")
            .WithRecipients([OrganisationNumber.Parse("987654321")])
            .WithDueDateTime(DateTimeOffset.Now)
            .WithAllowSystemDeleteAfter(DateTimeOffset.Now.AddDays(30))
            .WithContent(
                CorrespondenceContentBuilder
                    .Create()
                    .WithTitle("Title")
                    .WithLanguage(LanguageCode<ISO_639_1>.Parse("en"))
                    .WithSummary("Summary")
                    .WithBody("Body")
                    // .WithAttachments(
                    //     [
                    //         new CorrespondenceAttachment
                    //         {
                    //             Filename = "file.txt",
                    //             Name = "File",
                    //             Sender = OrganisationNumber.Parse("123456789"),
                    //             SendersReference = "123",
                    //             DataType = "text/plain",
                    //             DataLocationType = CorrespondenceDataLocationType.ExistingCorrespondenceAttachment,
                    //             Data = new MemoryStream(Encoding.UTF8.GetBytes("Hello, World!"))
                    //         }
                    //     ]
                    // )
                    .WithAttachment(
                        CorrespondenceAttachmentBuilder
                            .Create()
                            .WithFilename("file.txt")
                            .WithName("File")
                            .WithSender(OrganisationNumber.Parse("123456789"))
                            .WithSendersReference("123")
                            .WithDataType("text/plain")
                            .WithData(new MemoryStream(Encoding.UTF8.GetBytes("Hello, World!")))
                    )
            )
            .WithNotification(
                CorrespondenceNotificationBuilder
                    .Create()
                    .WithNotificationTemplate(CorrespondenceNotificationTemplate.GenericAltinnMessage)
                    .WithEmailSubject("Email subject")
                    .WithEmailBody("Email body")
                    .WithSmsBody("SMS body")
            )
            .WithPropertyList(new Dictionary<string, string> { ["key"] = "value" })
            .WithRequestedPublishTime(DateTimeOffset.Now.AddDays(1))
            // .WithAttachment(
            //     CorrespondenceAttachmentBuilder
            //         .Create()
            //         .WithFilename("file.txt")
            //         .WithName("File")
            //         .WithSender(OrganisationNumber.Parse("123456789"))
            //         .WithSendersReference("123")
            //         .WithDataType("text/plain")
            //         .WithData(new MemoryStream(Encoding.UTF8.GetBytes("Hello, World!")))
            // )
            .Build();
    }
}
