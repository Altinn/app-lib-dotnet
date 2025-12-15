using System.Text.Json;
using Altinn.App.ProcessEngine.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.App.ProcessEngine.Data;

internal sealed class ProcessEngineDbContext : DbContext
{
    public ProcessEngineDbContext(DbContextOptions<ProcessEngineDbContext> options)
        : base(options) { }

    public DbSet<ProcessEngineJob> Jobs { get; set; }
    public DbSet<ProcessEngineTask> Tasks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Job entity
        modelBuilder.Entity<ProcessEngineJob>(entity =>
        {
            entity.ToTable("process_engine_jobs");

            entity.Property(e => e.Identifier).HasMaxLength(500);

            // Configure complex properties as JSON
            entity.OwnsOne(
                e => e.Actor,
                actor =>
                {
                    actor
                        .Property(a => a.UserIdOrOrgNumber)
                        .HasColumnName("actor_user_id_or_org_number")
                        .HasMaxLength(50);
                    actor.Property(a => a.Language).HasColumnName("actor_language").HasMaxLength(10);
                }
            );

            entity.OwnsOne(
                e => e.InstanceInformation,
                instance =>
                {
                    instance.Property(i => i.Org).HasColumnName("instance_org").HasMaxLength(100);
                    instance.Property(i => i.App).HasColumnName("instance_app").HasMaxLength(100);
                    instance.Property(i => i.InstanceOwnerPartyId).HasColumnName("instance_owner_party_id");
                    instance.Property(i => i.InstanceGuid).HasColumnName("instance_guid");
                }
            );

            // Indexes - cannot directly index owned entity properties, so we skip complex indexes for now
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Configure Task entity
        modelBuilder.Entity<ProcessEngineTask>(entity =>
        {
            entity.ToTable("process_engine_tasks");

            entity.Property(e => e.Identifier).HasMaxLength(500);

            // Configure complex properties
            entity.OwnsOne(
                e => e.Actor,
                actor =>
                {
                    actor
                        .Property(a => a.UserIdOrOrgNumber)
                        .HasColumnName("actor_user_id_or_org_number")
                        .HasMaxLength(50);
                    actor.Property(a => a.Language).HasColumnName("actor_language").HasMaxLength(10);
                }
            );

            // Store Command as JSON
            entity
                .Property(e => e.Command)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<ProcessEngineCommand>(v, (JsonSerializerOptions?)null)!
                )
                .HasColumnName("command_data");

            // Store RetryStrategy as JSON
            entity
                .Property(e => e.RetryStrategy)
                .HasConversion(
                    v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v =>
                        v == null
                            ? null
                            : JsonSerializer.Deserialize<ProcessEngineRetryStrategy>(v, (JsonSerializerOptions?)null)
                );

            // Indexes
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.BackoffUntil);
            entity.HasIndex(e => e.CreatedAt);

            // Configure relationship to Jobs
            entity
                .HasOne<ProcessEngineJob>()
                .WithMany(j => j.Tasks)
                .HasForeignKey("JobIdentifier")
                .HasPrincipalKey(j => j.Identifier);
        });
    }
}
