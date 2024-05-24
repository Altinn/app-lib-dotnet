using Microsoft.CodeAnalysis;

namespace Altinn.App.Analyzers.Tests;

// Based on: https://github.com/VerifyTests/Verify.SourceGenerators/blob/18eb1cd67ebf76803f164751ecb27397a4c11951/src/Verify.SourceGenerators/Converters/DiagnosticConverter.cs
internal sealed class DiagnosticJsonConverter : WriteOnlyJsonConverter<Diagnostic>
{
    public override void Write(VerifyJsonWriter writer, Diagnostic value)
    {
        writer.WriteStartObject();
        writer.WriteMember(value, value.Id, "Id");
        var descriptor = value.Descriptor;
        writer.WriteMember(value, descriptor.Title.ToString(), "Title");
        writer.WriteMember(value, value.Severity.ToString(), "Severity");
        writer.WriteMember(value, value.WarningLevel, "WarningLevel");
        writer.WriteMember(value, value.Location.ToString(), "Location");
        var description = descriptor.Description.ToString();
        if (!string.IsNullOrWhiteSpace(description))
        {
            writer.WriteMember(value, description, "Description");
        }

        var help = descriptor.HelpLinkUri;
        if (!string.IsNullOrWhiteSpace(help))
        {
            writer.WriteMember(value, help, "HelpLink");
        }

        writer.WriteMember(value, descriptor.MessageFormat.ToString(), "MessageFormat");
        writer.WriteMember(value, value.GetMessage(), "Message");
        writer.WriteMember(value, descriptor.Category, "Category");
        writer.WriteMember(value, descriptor.CustomTags, "CustomTags");
        writer.WriteEndObject();
    }
}
