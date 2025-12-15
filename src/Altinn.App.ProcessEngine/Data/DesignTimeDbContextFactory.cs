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

        // Dummy connection string - only used for generating migrations
        // Real connection string is provided at runtime by the consuming application
        optionsBuilder.UseNpgsql("Host=localhost;Database=design_time_placeholder;Username=postgres;Password=postgres");

        return new ProcessEngineDbContext(optionsBuilder.Options);
    }
}
