using AgentShop.Modules.Orders.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgentShop.Modules.Orders.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id)
            .HasConversion(id => id.Value, value => new OrderId(value))
            .ValueGeneratedNever();

        builder.Property(o => o.CustomerEmail).HasMaxLength(Order.CustomerEmailMaxLength).IsRequired();
        builder.Property(o => o.Currency).HasMaxLength(3).IsRequired();
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        builder.Ignore(o => o.Total);

        // Optimistic concurrency via PostgreSQL's xmin system column: concurrent state transitions (cancel vs. ship)
        // fail with DbUpdateConcurrencyException instead of silently overwriting each other.
        builder.Property<uint>("Version").IsRowVersion();

        builder.OwnsMany(o => o.Lines, lines =>
        {
            lines.ToTable("order_lines");
            lines.WithOwner().HasForeignKey("OrderId");
            lines.Property<int>("Id");
            lines.HasKey("Id");
            lines.Property(l => l.ProductName).HasMaxLength(200).IsRequired();
            lines.Property(l => l.UnitPrice).HasPrecision(18, 2);
            lines.Ignore(l => l.LineTotal);
        });
        builder.Navigation(o => o.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
