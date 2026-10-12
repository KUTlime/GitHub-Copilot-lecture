using AgentShop.Modules.Catalog.Contracts;
using AgentShop.Modules.Catalog.Domain;
using AgentShop.Modules.Catalog.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgentShop.Modules.Catalog.PublicApi;

/// <summary>
/// Implementation of the module's public contract (<see cref="ICatalogModule"/>) used by other modules.
/// </summary>
internal sealed class CatalogModuleApi(CatalogDbContext db) : ICatalogModule
{
    public async Task<IReadOnlyList<ProductSnapshot>> GetProductsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(productIds);

        var ids = productIds.Distinct().Select(id => new ProductId(id)).ToList();

        return await db.Products
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new ProductSnapshot(p.Id.Value, p.Name, p.Price, p.Currency, p.IsActive))
            .ToListAsync(cancellationToken);
    }
}
