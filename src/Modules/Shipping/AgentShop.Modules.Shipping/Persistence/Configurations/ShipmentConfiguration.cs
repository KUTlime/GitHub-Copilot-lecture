using AgentShop.Modules.Shipping.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgentShop.Modules.Shipping.Persistence.Configurations;

internal sealed class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("shipments");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasConversion(id => id.Value, value => new ShipmentId(value))
            .ValueGeneratedNever();

        // One shipment per order. The database enforces it, which makes the OrderPlaced handler idempotent.
        builder.HasIndex(s => s.OrderId)
            .IsUnique()
            .HasDatabaseName(ShippingPersistence.OrderIdUniqueConstraint);

        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        // Optimistic concurrency via PostgreSQL's xmin system column (prevents a double dispatch).
        builder.Property<uint>("Version").IsRowVersion();
    }
}
