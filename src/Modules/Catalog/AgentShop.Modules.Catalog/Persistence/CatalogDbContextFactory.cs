using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AgentShop.Modules.Catalog.Persistence;

/// <summary>
/// Used only by `dotnet ef` so migrations can be created without starting the host.
/// The connection string is a placeholder; `migrations add` does not connect to the database.
/// </summary>
internal sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=agentshop;Username=postgres;Password=postgres",
                npgsql => npgsql.MigrationsHistoryTable(CatalogDbContext.MigrationsHistoryTable, CatalogDbContext.Schema))
            .Options;

        return new CatalogDbContext(options);
    }
}
