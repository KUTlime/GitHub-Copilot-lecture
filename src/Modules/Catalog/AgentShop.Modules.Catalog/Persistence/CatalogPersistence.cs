using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AgentShop.Modules.Catalog.Persistence;

internal static class CatalogPersistence
{
    public const string SkuUniqueConstraint = "ux_products_sku";

    public static bool IsUniqueViolation(this DbUpdateException exception, string constraintName) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres
        && string.Equals(postgres.ConstraintName, constraintName, StringComparison.Ordinal);
}
