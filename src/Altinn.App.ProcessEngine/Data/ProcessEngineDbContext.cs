using Altinn.App.ProcessEngine.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Altinn.App.ProcessEngine.Data;

internal sealed class ProcessEngineDbContext : DbContext
{
    public ProcessEngineDbContext(DbContextOptions<ProcessEngineDbContext> options)
        : base(options) { }

    public DbSet<ProcessEngineJobEntity> Jobs { get; set; }
    public DbSet<ProcessEngineTaskEntity> Tasks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Job entity
        modelBuilder.Entity<ProcessEngineJobEntity>(entity =>
        {
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new
            {
                e.InstanceOrg,
                e.InstanceApp,
                e.InstanceGuid,
            });
            entity.HasIndex(e => e.Key).IsUnique();
        });

        // Configure Task entity
        modelBuilder.Entity<ProcessEngineTaskEntity>(entity =>
        {
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.BackoffUntil);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ProcessingOrder);

            // Configure relationship to Jobs
            entity
                .HasOne(t => t.Job)
                .WithMany(j => j.Tasks)
                .HasForeignKey(t => t.JobId)
                .HasPrincipalKey(j => j.Id)
                .OnDelete(DeleteBehavior.Cascade);

            // Add unique index on Identifier for both entities
            entity.HasIndex(e => e.Key).IsUnique();
        });
    }
}
