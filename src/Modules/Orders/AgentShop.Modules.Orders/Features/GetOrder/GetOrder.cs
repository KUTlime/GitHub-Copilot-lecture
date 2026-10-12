using AgentShop.Modules.Orders.Domain;
using AgentShop.Modules.Orders.Persistence;
using AgentShop.SharedKernel;
using AgentShop.SharedKernel.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace AgentShop.Modules.Orders.Features.GetOrder;

internal static class GetOrder
{
    public sealed record Response(
        Guid Id,
        string CustomerEmail,
        string Status,
        decimal Total,
        string Currency,
        DateTimeOffset PlacedAt,
        DateTimeOffset? CancelledAt,
        DateTimeOffset? ShippedAt,
        IReadOnlyList<Response.Line> Lines)
    {
        public sealed record Line(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal LineTotal);
    }

    internal static async Task<Result<Response>> HandleAsync(Guid id, OrdersDbContext db, CancellationToken cancellationToken)
    {
        var orderId = new OrderId(id);
        var order = await db.Orders.AsNoTracking().SingleOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        if (order is null)
        {
            return OrderErrors.OrderNotFound;
        }

        return new Response(
            order.Id.Value,
            order.CustomerEmail,
            order.Status.ToString(),
            order.Total,
            order.Currency,
            order.PlacedAt,
            order.CancelledAt,
            order.ShippedAt,
            [.. order.Lines.Select(l => new Response.Line(l.ProductId, l.ProductName, l.UnitPrice, l.Quantity, l.LineTotal))]);
    }

    internal sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app) =>
            app.MapGet("/{id:guid}", async (Guid id, OrdersDbContext db, CancellationToken cancellationToken) =>
                    (await HandleAsync(id, db, cancellationToken)).ToHttpResult())
               .WithName(nameof(GetOrder))
               .Produces<Response>()
               .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
