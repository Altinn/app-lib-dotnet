using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.WorkflowEngine.Models;

/// <summary>
/// Internal DTO representing the transported instance state.
/// The workflow engine never inspects this — it's serialized into an opaque JsonElement.
/// </summary>
internal sealed record InstanceState
{
    [JsonPropertyName("instance")]
    public required Instance Instance { get; init; }

    /// <summary>
    /// Form data keyed by DataElement.Id (guid string), serialized as JSON.
    /// Only includes form data elements (those with AppLogic.ClassRef), not binary attachments.
    /// </summary>
    [JsonPropertyName("formData")]
    public required Dictionary<string, JsonElement> FormData { get; init; }
}
