using AgentShop.Modules.Catalog.Persistence;
using AgentShop.SharedKernel;
using AgentShop.SharedKernel.AspNetCore;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace AgentShop.Modules.Catalog.Features.ListProducts;

internal static class ListProducts
{
    public const int MaxPageSize = 100;

    // (Page - 1) * PageSize must fit into an int OFFSET.
    public const int MaxPage = int.MaxValue / MaxPageSize;

    public sealed record Request(int Page = 1, int PageSize = 20);

    public sealed record Response(IReadOnlyList<Response.Item> Items, int Page, int PageSize, int TotalCount)
    {
        public sealed record Item(Guid Id, string Sku, string Name, decimal Price, string Currency, bool IsActive);
    }

    internal sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Page).InclusiveBetween(1, MaxPage);
            RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize);
        }
    }

    internal static async Task<Result<Response>> HandleAsync(Request request, CatalogDbContext db, CancellationToken cancellationToken)
    {
        var query = db.Products.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(p => p.Sku)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new Response.Item(p.Id.Value, p.Sku, p.Name, p.Price, p.Currency, p.IsActive))
            .ToListAsync(cancellationToken);

        return new Response(items, request.Page, request.PageSize, totalCount);
    }

    internal sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app) =>
            app.MapGet("/", async ([AsParameters] Request request, CatalogDbContext db, CancellationToken cancellationToken) =>
                    (await HandleAsync(request, db, cancellationToken)).ToHttpResult())
               .AddEndpointFilter<ValidationFilter<Request>>()
               .WithName(nameof(ListProducts))
               .Produces<Response>()
               .ProducesValidationProblem();
    }
}
