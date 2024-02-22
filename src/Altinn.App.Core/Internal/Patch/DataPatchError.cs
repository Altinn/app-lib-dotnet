using Altinn.App.Core.Models.Result;

namespace Altinn.App.Core.Internal.Patch;

public class DataPatchError: IResultError
{
    public string? Title { get; set; }
    public string? Detail { get; set; }
    public DataPatchErrorStatus? Status { get; set; }
    public IDictionary<string, object?>? Extensions { get; set; }
    public string Reason()
    {
        return $"Data patch failed to apply. Do to {Title} with details: {Detail}";
    }
}

public enum DataPatchErrorStatus
{
    PatchTestFailed,
    DeserializationFailed,
}