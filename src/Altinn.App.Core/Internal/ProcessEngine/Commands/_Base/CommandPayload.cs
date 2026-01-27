using System.Text.Json;
using System.Text.Json.Serialization;

namespace Altinn.App.Core.Internal.ProcessEngine.Commands;

/// <summary>
/// Base class for command request payloads.
/// Request payloads are sent from app → engine → app callback.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ExecuteServiceTaskPayload), typeDiscriminator: "executeServiceTask")]
[JsonDerivedType(typeof(UpdateProcessStatePayload), typeDiscriminator: "updateProcessState")]
internal abstract record CommandRequestPayload;

/// <summary>
/// Base class for command response payloads.
/// Response payloads are returned from app → engine after command execution.
/// </summary>
internal abstract record CommandResponsePayload;

/// <summary>
/// Source-generated JSON serialization context for command payloads.
/// Provides AOT-compatible, high-performance serialization.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CommandRequestPayload))]
[JsonSerializable(typeof(ExecuteServiceTaskPayload))]
[JsonSerializable(typeof(UpdateProcessStatePayload))]
internal partial class CommandPayloadJsonContext : JsonSerializerContext { }

/// <summary>
/// Helper methods for serializing/deserializing command payloads.
/// Uses source-generated serialization for better performance and AOT compatibility.
/// </summary>
internal static class CommandPayloadSerializer
{
    public static string? Serialize<T>(T? payload)
        where T : CommandRequestPayload
    {
        return payload is null ? null : JsonSerializer.Serialize(payload, CommandPayloadJsonContext.Default.Options);
    }

    public static T? Deserialize<T>(string? json)
        where T : CommandRequestPayload
    {
        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<T>(json, CommandPayloadJsonContext.Default.Options);
    }

    public static string? SerializeResponse<T>(T? payload)
        where T : CommandResponsePayload
    {
        return payload is null ? null : JsonSerializer.Serialize(payload, CommandPayloadJsonContext.Default.Options);
    }
}
