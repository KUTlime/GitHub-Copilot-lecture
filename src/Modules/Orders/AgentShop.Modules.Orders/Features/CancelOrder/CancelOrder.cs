using AgentShop.Modules.Orders.Domain;
using AgentShop.Modules.Orders.Persistence;
using AgentShop.SharedKernel;
using AgentShop.SharedKernel.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace AgentShop.Modules.Orders.Features.CancelOrder;

internal static class CancelOrder
{
    internal static async Task<Result> HandleAsync(Guid id, OrdersDbContext db, TimeProvider clock, CancellationToken cancellationToken)
    {
        var orderId = new OrderId(id);
        var order = await db.Orders.SingleOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        if (order is null)
        {
            return OrderErrors.OrderNotFound;
        }

        var cancelled = order.Cancel(clock.GetUtcNow());
        if (cancelled.IsFailure)
        {
            return cancelled;
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another request (e.g. the shipment being dispatched) changed the order in the meantime.
            return OrderErrors.ConcurrentModification;
        }

        return Result.Success();
    }

    internal sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app) =>
            app.MapPost("/{id:guid}/cancel", async (Guid id, OrdersDbContext db, TimeProvider clock, CancellationToken cancellationToken) =>
                    (await HandleAsync(id, db, clock, cancellationToken)).ToHttpResult())
               .WithName(nameof(CancelOrder))
               .Produces(StatusCodes.Status204NoContent)
               .ProducesProblem(StatusCodes.Status404NotFound)
               .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
