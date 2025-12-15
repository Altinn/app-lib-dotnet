using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Altinn.App.ProcessEngine.Data;

/// <summary>
/// Design-time factory for EF Core tooling (migrations, etc.)
/// Only used during development - not at runtime
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ProcessEngineDbContext>
{
    public ProcessEngineDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProcessEngineDbContext>();

        // Connection string for development - matches docker-compose.yaml
        // Real connection string is provided at runtime by the consuming application
        optionsBuilder.UseNpgsql("Host=localhost;Database=altinn_processengine;Username=postgres;Password=postgres123");

        return new ProcessEngineDbContext(optionsBuilder.Options);
    }
}
