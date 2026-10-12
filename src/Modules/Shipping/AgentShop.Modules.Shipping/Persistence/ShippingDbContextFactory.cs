using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AgentShop.Modules.Shipping.Persistence;

/// <summary>
/// Used only by `dotnet ef` so migrations can be created without starting the host.
/// The connection string is a placeholder; `migrations add` does not connect to the database.
/// </summary>
internal sealed class ShippingDbContextFactory : IDesignTimeDbContextFactory<ShippingDbContext>
{
    public ShippingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ShippingDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=agentshop;Username=postgres;Password=postgres",
                npgsql => npgsql.MigrationsHistoryTable(ShippingDbContext.MigrationsHistoryTable, ShippingDbContext.Schema))
            .Options;

        return new ShippingDbContext(options);
    }
}
