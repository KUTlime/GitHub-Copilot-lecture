namespace AgentShop.Modules.Catalog.Contracts;

/// <summary>
/// Synchronous, in-process API of the Catalog module for other modules.
/// </summary>
public interface ICatalogModule
{
    /// <summary>
    /// Returns snapshots of the requested products. Unknown ids are omitted from the result.
    /// </summary>
    Task<IReadOnlyList<ProductSnapshot>> GetProductsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken);
}

public sealed record ProductSnapshot(Guid ProductId, string Name, decimal UnitPrice, string Currency, bool IsActive);
