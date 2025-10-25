using Altinn.App.Clients.Fiks.FiksArkiv.Models;

namespace Altinn.App.Clients.Fiks.Extensions;

internal static class ListExtensions
{
    /// <summary>
    /// Ensures that all filenames in the list of attachments are unique by appending a unique identifier to duplicates.
    /// </summary>
    public static void EnsureUniqueFilenames(this IReadOnlyList<MessagePayloadWrapper> attachments)
    {
        var hasDuplicateFilenames = attachments
            .GroupBy(x => x.Payload.Filename.ToLowerInvariant())
            .Where(x => x.Count() > 1)
            .Select(x => x.ToList());

        foreach (var duplicates in hasDuplicateFilenames)
        {
            for (int i = 0; i < duplicates.Count; i++)
            {
                int uniqueId = i + 1;
                string filename = Path.GetFileNameWithoutExtension(duplicates[i].Payload.Filename);
                string extension = Path.GetExtension(duplicates[i].Payload.Filename);

                duplicates[i].Payload.Filename = $"{filename}({uniqueId}){extension}";
            }
        }
    }
}
