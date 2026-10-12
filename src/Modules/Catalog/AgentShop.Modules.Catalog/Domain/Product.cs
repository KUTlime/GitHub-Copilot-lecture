using AgentShop.SharedKernel;

namespace AgentShop.Modules.Catalog.Domain;

/// <summary>
/// Aggregate root of the Catalog module. All invariants live here, not in handlers.
/// </summary>
internal sealed class Product
{
    public const int SkuMaxLength = 32;
    public const int NameMaxLength = 200;
    public const string DefaultCurrency = "CZK";

    private Product()
    {
    }

    public ProductId Id { get; private set; }

    public string Sku { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public decimal Price { get; private set; }

    public string Currency { get; private set; } = DefaultCurrency;

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<Product> Create(string sku, string name, decimal price, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (price <= 0)
        {
            return CatalogErrors.PriceMustBePositive;
        }

        return new Product
        {
            Id = ProductId.New(),
            Sku = NormalizeSku(sku),
            Name = name.Trim(),
            Price = price,
            Currency = DefaultCurrency,
            IsActive = true,
            CreatedAt = createdAt,
        };
    }

    /// <summary>
    /// Deactivated products stay in the catalog but can no longer be ordered. Idempotent.
    /// </summary>
    public void Deactivate() => IsActive = false;

    public static string NormalizeSku(string sku) => sku.Trim().ToUpperInvariant();
}
