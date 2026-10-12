using AgentShop.Modules.Shipping.Domain;
using Microsoft.EntityFrameworkCore;

namespace AgentShop.Modules.Shipping.Persistence;

internal sealed class ShippingDbContext(DbContextOptions<ShippingDbContext> options) : DbContext(options)
{
    public const string Schema = "shipping";
    public const string MigrationsHistoryTable = "__EFMigrationsHistory";

    public DbSet<Shipment> Shipments => Set<Shipment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShippingDbContext).Assembly);
    }
}
