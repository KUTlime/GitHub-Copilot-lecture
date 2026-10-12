using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AgentShop.Modules.Shipping.Persistence;

internal static class ShippingPersistence
{
    public const string OrderIdUniqueConstraint = "ux_shipments_order_id";

    public static bool IsUniqueViolation(this DbUpdateException exception, string constraintName) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres
        && string.Equals(postgres.ConstraintName, constraintName, StringComparison.Ordinal);
}
