using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Altinn.App.ProcessEngine.Models;

namespace Altinn.App.ProcessEngine.Data.Entities;

[Table("process_engine_tasks")]
internal sealed class ProcessEngineTaskEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [MaxLength(500)]
    public required string Key { get; set; }

    public ProcessEngineItemStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    public int ProcessingOrder { get; set; }

    public DateTimeOffset? StartTime { get; set; }

    public DateTimeOffset? BackoffUntil { get; set; }

    public int RequeueCount { get; set; }

    // Actor information - flattened
    [MaxLength(50)]
    public required string ActorUserIdOrOrgNumber { get; set; }

    [MaxLength(10)]
    public string? ActorLanguage { get; set; }

    // Foreign key
    public long JobId { get; set; }

    [MaxLength(500)]
    public required string JobIdentifier { get; set; }

    // JSON columns
    [Column(TypeName = "jsonb")]
    public string CommandJson { get; set; } = "{}";

    [Column(TypeName = "jsonb")]
    public string? RetryStrategyJson { get; set; }

    // Navigation properties
    public ProcessEngineJobEntity Job { get; set; } = null!;

    public static ProcessEngineTaskEntity FromDomainModel(ProcessEngineTask task, long? jobId = null)
    {
        return new ProcessEngineTaskEntity
        {
            Key = task.Key,
            Status = task.Status,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            ProcessingOrder = task.ProcessingOrder,
            StartTime = task.StartTime,
            BackoffUntil = task.BackoffUntil,
            RequeueCount = task.RequeueCount,
            ActorUserIdOrOrgNumber = task.Actor.UserIdOrOrgNumber,
            ActorLanguage = task.Actor.Language,
            JobId = jobId ?? 0, // Will be set properly during job creation
            JobIdentifier = task.JobIdentifier,
            CommandJson = JsonSerializer.Serialize(task.Command),
            RetryStrategyJson = task.RetryStrategy != null ? JsonSerializer.Serialize(task.RetryStrategy) : null,
        };
    }

    public ProcessEngineTask ToDomainModel()
    {
        var command = JsonSerializer.Deserialize<ProcessEngineCommand>(CommandJson)!;
        var retryStrategy =
            RetryStrategyJson != null
                ? JsonSerializer.Deserialize<ProcessEngineRetryStrategy>(RetryStrategyJson)
                : null;

        return new ProcessEngineTask
        {
            Key = Key,
            Status = Status,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            ProcessingOrder = ProcessingOrder,
            StartTime = StartTime,
            BackoffUntil = BackoffUntil,
            RequeueCount = RequeueCount,
            Actor = new ProcessEngineActor { UserIdOrOrgNumber = ActorUserIdOrOrgNumber, Language = ActorLanguage },
            JobIdentifier = JobIdentifier,
            Command = command,
            RetryStrategy = retryStrategy,
        };
    }
}
