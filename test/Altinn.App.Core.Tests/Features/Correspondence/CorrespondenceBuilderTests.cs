using System.Collections.Generic;
using System.Text;
using Altinn.App.Core.Features.Correspondence.Builder;
using Altinn.App.Core.Features.Correspondence.Exceptions;
using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Models;
using FluentAssertions;
using Xunit;

namespace Altinn.App.Core.Tests.Features.Correspondence;

public class CorrespondenceBuilderTests
{
    const string ValidOrganisationNumber = "806945609";
    const string InvalidOrganisationNumber = "123456789";

    [Fact]
    public void Build_WithAllRequiredProperties_ShouldReturnCorrespondence()
    {
        // Arrange
        OrganisationNumber sender = OrganisationNumber.Parse(ValidOrganisationNumber);
        IReadOnlyList<OrganisationNumber> recipients = [OrganisationNumber.Parse(ValidOrganisationNumber)];
        string resourceId = "resource-id";
        string sendersReference = "sender-reference";
        DateTimeOffset dueDateTime = DateTimeOffset.UtcNow.AddDays(30);
        DateTimeOffset allowSystemDeleteAfter = DateTimeOffset.UtcNow.AddDays(30);
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
}
