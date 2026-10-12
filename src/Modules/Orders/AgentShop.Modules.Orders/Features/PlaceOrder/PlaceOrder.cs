using AgentShop.Modules.Catalog.Contracts;
using AgentShop.Modules.Orders.Contracts;
using AgentShop.Modules.Orders.Domain;
using AgentShop.Modules.Orders.Persistence;
using AgentShop.SharedKernel;
using AgentShop.SharedKernel.AspNetCore;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AgentShop.Modules.Orders.Features.PlaceOrder;

/// <summary>
/// GOLDEN REFERENCE SLICE. New slices copy this shape:
/// Request/Response records, Validator, static HandleAsync returning Result, nested Endpoint.
/// </summary>
internal static class PlaceOrder
{
    public sealed record Request(string CustomerEmail, IReadOnlyList<Request.Line> Lines)
    {
        public sealed record Line(Guid ProductId, int Quantity);
    }

    public sealed record Response(Guid OrderId, decimal Total, string Currency);

    internal sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.CustomerEmail).NotEmpty().EmailAddress().MaximumLength(Order.CustomerEmailMaxLength);
            RuleFor(x => x.Lines).NotEmpty();
            // ChildRules skips null elements, so reject them explicitly (JSON [null] would otherwise reach the handler).
            RuleForEach(x => x.Lines).NotNull().ChildRules(line =>
            {
                line.RuleFor(x => x.ProductId).NotEmpty();
                line.RuleFor(x => x.Quantity).InclusiveBetween(OrderLine.MinQuantity, OrderLine.MaxQuantity);
            });
        }
    }

    internal static async Task<Result<Response>> HandleAsync(
        Request request,
        OrdersDbContext db,
        ICatalogModule catalog,
        IIntegrationEventPublisher publisher,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        // 1) Load data owned by other modules through their Contracts only.
        var productIds = request.Lines.Select(line => line.ProductId).ToList();
        var products = (await catalog.GetProductsAsync(productIds, cancellationToken))
            .ToDictionary(product => product.ProductId);

        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var lines = new List<OrderLine>(request.Lines.Count);
        for (var i = 0; i < request.Lines.Count; i++)
        {
            var requested = request.Lines[i];
            if (!products.TryGetValue(requested.ProductId, out var product))
            {
                errors[$"Lines[{i}].ProductId"] = ["Product does not exist."];
            }
            else if (!product.IsActive)
            {
                errors[$"Lines[{i}].ProductId"] = ["Product is not active and cannot be ordered."];
            }
            else
            {
                lines.Add(new OrderLine(product.ProductId, product.Name, product.UnitPrice, requested.Quantity));
            }
        }

        if (errors.Count > 0)
        {
            return new ValidationError(errors);
        }

        // 2) Business rules live in the aggregate.
        var currency = products.Values.First().Currency;
        var placed = Order.Place(request.CustomerEmail, currency, lines, clock.GetUtcNow());
        if (placed.IsFailure)
        {
            return placed.Error;
        }

        // 3) Persist.
        var order = placed.Value;
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        // 4) Publish AFTER the commit so other modules never see an order that does not exist.
        //    The data is committed now, so a client disconnect must not cancel the follow-up work.
        await publisher.PublishAsync(
            new OrderPlacedIntegrationEvent(
                order.Id.Value,
                order.CustomerEmail,
                [.. order.Lines.Select(line => new OrderPlacedIntegrationEvent.Line(line.ProductId, line.Quantity))],
                order.Total,
                order.Currency,
                order.PlacedAt),
            CancellationToken.None);

        return new Response(order.Id.Value, order.Total, order.Currency);
    }

    internal sealed class Endpoint : IEndpoint
    {
        // The route is RELATIVE: OrdersModule maps this endpoint under the "/orders" group.
        public void MapEndpoint(IEndpointRouteBuilder app) =>
            app.MapPost("/", async (
                    Request request,
                    OrdersDbContext db,
                    ICatalogModule catalog,
                    IIntegrationEventPublisher publisher,
                    TimeProvider clock,
                    CancellationToken cancellationToken) =>
                    (await HandleAsync(request, db, catalog, publisher, clock, cancellationToken))
                        .ToCreatedResult(order => $"/orders/{order.OrderId}"))
               .AddEndpointFilter<ValidationFilter<Request>>()
               .WithName(nameof(PlaceOrder))
               .Produces<Response>(StatusCodes.Status201Created)
               .ProducesValidationProblem();
    }
}
