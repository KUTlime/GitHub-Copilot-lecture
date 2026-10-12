using AgentShop.Modules.Orders.Contracts;
using AgentShop.Modules.Shipping.Domain;
using AgentShop.Modules.Shipping.Persistence;
using AgentShop.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgentShop.Modules.Shipping.Features.CreateShipment;

/// <summary>
/// Creates a pending shipment for every placed order. Idempotent: a second delivery of the same
/// event hits the unique index on OrderId and is treated as "already processed".
/// </summary>
internal sealed partial class OrderPlacedHandler(ShippingDbContext db, TimeProvider clock, ILogger<OrderPlacedHandler> logger)
    : IIntegrationEventHandler<OrderPlacedIntegrationEvent>
{
    public async Task HandleAsync(OrderPlacedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        db.Shipments.Add(Shipment.CreateFor(integrationEvent.OrderId, clock.GetUtcNow()));

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation(ShippingPersistence.OrderIdUniqueConstraint))
        {
            db.ChangeTracker.Clear();
            LogShipmentAlreadyExists(logger, integrationEvent.OrderId);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Shipment for order {OrderId} already exists; event ignored")]
    private static partial void LogShipmentAlreadyExists(ILogger logger, Guid orderId);
}
