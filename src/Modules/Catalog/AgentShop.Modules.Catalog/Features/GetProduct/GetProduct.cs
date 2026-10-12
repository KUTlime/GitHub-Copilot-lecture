using AgentShop.Modules.Catalog.Domain;
using AgentShop.Modules.Catalog.Persistence;
using AgentShop.SharedKernel;
using AgentShop.SharedKernel.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace AgentShop.Modules.Catalog.Features.GetProduct;

internal static class GetProduct
{
    public sealed record Response(Guid Id, string Sku, string Name, decimal Price, string Currency, bool IsActive);

    internal static async Task<Result<Response>> HandleAsync(Guid id, CatalogDbContext db, CancellationToken cancellationToken)
    {
        var productId = new ProductId(id);

        var response = await db.Products
            .AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => new Response(p.Id.Value, p.Sku, p.Name, p.Price, p.Currency, p.IsActive))
            .SingleOrDefaultAsync(cancellationToken);

        return response is null ? CatalogErrors.ProductNotFound : response;
    }

    internal sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app) =>
            app.MapGet("/{id:guid}", async (Guid id, CatalogDbContext db, CancellationToken cancellationToken) =>
                    (await HandleAsync(id, db, cancellationToken)).ToHttpResult())
               .WithName(nameof(GetProduct))
               .Produces<Response>()
               .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
