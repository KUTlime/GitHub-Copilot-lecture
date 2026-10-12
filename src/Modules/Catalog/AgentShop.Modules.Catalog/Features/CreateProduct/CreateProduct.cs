using AgentShop.Modules.Catalog.Domain;
using AgentShop.Modules.Catalog.Persistence;
using AgentShop.SharedKernel;
using AgentShop.SharedKernel.AspNetCore;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace AgentShop.Modules.Catalog.Features.CreateProduct;

internal static class CreateProduct
{
    public sealed record Request(string Sku, string Name, decimal Price);

    public sealed record Response(Guid Id, string Sku, string Name, decimal Price, string Currency, bool IsActive);

    internal sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Sku).NotEmpty().MaximumLength(Product.SkuMaxLength).Matches("^[A-Za-z0-9-]+$");
            RuleFor(x => x.Name).NotEmpty().MaximumLength(Product.NameMaxLength);
            RuleFor(x => x.Price).GreaterThan(0).PrecisionScale(18, 2, ignoreTrailingZeros: true);
        }
    }

    internal static async Task<Result<Response>> HandleAsync(
        Request request,
        CatalogDbContext db,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        var created = Product.Create(request.Sku, request.Name, request.Price, clock.GetUtcNow());
        if (created.IsFailure)
        {
            return created.Error;
        }

        var product = created.Value;
        db.Products.Add(product);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation(CatalogPersistence.SkuUniqueConstraint))
        {
            return CatalogErrors.SkuAlreadyExists;
        }

        return new Response(product.Id.Value, product.Sku, product.Name, product.Price, product.Currency, product.IsActive);
    }

    internal sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app) =>
            app.MapPost("/", async (Request request, CatalogDbContext db, TimeProvider clock, CancellationToken cancellationToken) =>
                    (await HandleAsync(request, db, clock, cancellationToken))
                        .ToCreatedResult(product => $"/catalog/products/{product.Id}"))
               .AddEndpointFilter<ValidationFilter<Request>>()
               .WithName(nameof(CreateProduct))
               .Produces<Response>(StatusCodes.Status201Created)
               .ProducesValidationProblem()
               .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
