using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AgentShop.Modules.Orders.Persistence;

/// <summary>
/// Used only by `dotnet ef` so migrations can be created without starting the host.
/// The connection string is a placeholder; `migrations add` does not connect to the database.
/// </summary>
internal sealed class OrdersDbContextFactory : IDesignTimeDbContextFactory<OrdersDbContext>
{
    public OrdersDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=agentshop;Username=postgres;Password=postgres",
                npgsql => npgsql.MigrationsHistoryTable(OrdersDbContext.MigrationsHistoryTable, OrdersDbContext.Schema))
            .Options;

        return new OrdersDbContext(options);
    }
}
