using AgentShop.Modules.Shipping.Contracts;
using AgentShop.Modules.Shipping.Domain;
using AgentShop.Modules.Shipping.Persistence;
using AgentShop.SharedKernel;
using AgentShop.SharedKernel.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace AgentShop.Modules.Shipping.Features.DispatchShipment;

internal static class DispatchShipment
{
    internal static async Task<Result> HandleAsync(
        Guid id,
        ShippingDbContext db,
        IIntegrationEventPublisher publisher,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        var shipmentId = new ShipmentId(id);
        var shipment = await db.Shipments.SingleOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);
        if (shipment is null)
        {
            return ShippingErrors.ShipmentNotFound;
        }

        var dispatched = shipment.Dispatch(clock.GetUtcNow());
        if (dispatched.IsFailure)
        {
            return dispatched;
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // A concurrent dispatch won; publishing a second event would be wrong.
            return ShippingErrors.ConcurrentModification;
        }

        // The data is committed now, so a client disconnect must not cancel the follow-up work.
        await publisher.PublishAsync(
            new ShipmentDispatchedIntegrationEvent(shipment.Id.Value, shipment.OrderId, shipment.DispatchedAt!.Value),
            CancellationToken.None);

        return Result.Success();
    }

    internal sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app) =>
            app.MapPost("/{id:guid}/dispatch", async (
                    Guid id,
                    ShippingDbContext db,
                    IIntegrationEventPublisher publisher,
                    TimeProvider clock,
                    CancellationToken cancellationToken) =>
                    (await HandleAsync(id, db, publisher, clock, cancellationToken)).ToHttpResult())
               .WithName(nameof(DispatchShipment))
               .Produces(StatusCodes.Status204NoContent)
               .ProducesProblem(StatusCodes.Status404NotFound)
               .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
