using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Altinn.App.ProcessEngine.Models;

namespace Altinn.App.ProcessEngine.Data.Entities;

[Table("process_engine_jobs")]
internal sealed class ProcessEngineJobEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [MaxLength(500)]
    public required string Key { get; set; }

    public ProcessEngineItemStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    // Actor information - flattened
    [MaxLength(50)]
    public required string ActorUserIdOrOrgNumber { get; set; }

    [MaxLength(10)]
    public string? ActorLanguage { get; set; }

    // Instance information - flattened
    [MaxLength(100)]
    public required string InstanceOrg { get; set; }

    [MaxLength(100)]
    public required string InstanceApp { get; set; }

    public int InstanceOwnerPartyId { get; set; }

    public Guid InstanceGuid { get; set; }

    // Navigation property
    public ICollection<ProcessEngineTaskEntity> Tasks { get; set; } = [];

    public static ProcessEngineJobEntity FromDomainModel(ProcessEngineJob job)
    {
        return new ProcessEngineJobEntity
        {
            Key = job.Key,
            Status = job.Status,
            CreatedAt = job.CreatedAt,
            UpdatedAt = job.UpdatedAt,
            ActorUserIdOrOrgNumber = job.Actor.UserIdOrOrgNumber,
            ActorLanguage = job.Actor.Language,
            InstanceOrg = job.InstanceInformation.Org,
            InstanceApp = job.InstanceInformation.App,
            InstanceOwnerPartyId = job.InstanceInformation.InstanceOwnerPartyId,
            InstanceGuid = job.InstanceInformation.InstanceGuid,
            Tasks = job.Tasks.Select(ProcessEngineTaskEntity.FromDomainModel).ToList(),
        };
    }

    public ProcessEngineJob ToDomainModel()
    {
        return new ProcessEngineJob
        {
            Key = Key,
            Status = Status,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            Actor = new ProcessEngineActor { UserIdOrOrgNumber = ActorUserIdOrOrgNumber, Language = ActorLanguage },
            InstanceInformation = new InstanceInformation
            {
                Org = InstanceOrg,
                App = InstanceApp,
                InstanceOwnerPartyId = InstanceOwnerPartyId,
                InstanceGuid = InstanceGuid,
            },
            Tasks = Tasks.Select(t => t.ToDomainModel()).ToList(),
        };
    }
}
