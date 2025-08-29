using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Clients.Fiks.Extensions;

internal static class DataElementExtensions
{
    private static readonly Dictionary<string, string> _mimeTypeToExtensionMapping = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["application/xml"] = ".xml",
        ["text/xml"] = ".xml",
        ["application/pdf"] = ".pdf",
        ["application/json"] = ".json",
    };

    public static string? GetExtensionForMimeType(this DataElement dataElement)
    {
        var mimeType = dataElement.ContentType;
        return mimeType is null ? null : _mimeTypeToExtensionMapping.GetValueOrDefault(mimeType);
    }
}
