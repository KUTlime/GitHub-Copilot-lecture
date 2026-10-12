using AgentShop.Modules.Catalog.Domain;
using AgentShop.Modules.Catalog.Persistence;
using AgentShop.SharedKernel;
using AgentShop.SharedKernel.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace AgentShop.Modules.Catalog.Features.DeactivateProduct;

internal static class DeactivateProduct
{
    internal static async Task<Result> HandleAsync(Guid id, CatalogDbContext db, CancellationToken cancellationToken)
    {
        var productId = new ProductId(id);
        var product = await db.Products.SingleOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (product is null)
        {
            return CatalogErrors.ProductNotFound;
        }

        product.Deactivate();
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    internal sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app) =>
            app.MapPost("/{id:guid}/deactivate", async (Guid id, CatalogDbContext db, CancellationToken cancellationToken) =>
                    (await HandleAsync(id, db, cancellationToken)).ToHttpResult())
               .WithName(nameof(DeactivateProduct))
               .Produces(StatusCodes.Status204NoContent)
               .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
