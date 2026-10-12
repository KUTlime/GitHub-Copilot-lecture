using AgentShop.Modules.Orders.Domain;
using Microsoft.EntityFrameworkCore;

namespace AgentShop.Modules.Orders.Persistence;

internal sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public const string Schema = "orders";
    public const string MigrationsHistoryTable = "__EFMigrationsHistory";

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);
    }
}
