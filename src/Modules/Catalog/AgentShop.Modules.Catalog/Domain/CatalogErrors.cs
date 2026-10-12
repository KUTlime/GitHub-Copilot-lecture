using AgentShop.SharedKernel;

namespace AgentShop.Modules.Catalog.Domain;

internal static class CatalogErrors
{
    public static readonly Error ProductNotFound =
        Error.NotFound("Catalog.ProductNotFound", "The product was not found.");

    public static readonly Error SkuAlreadyExists =
        Error.Conflict("Catalog.SkuAlreadyExists", "A product with the same SKU already exists.");

    public static readonly ValidationError PriceMustBePositive =
        ValidationError.For("Price", "Price must be greater than zero.");
}
