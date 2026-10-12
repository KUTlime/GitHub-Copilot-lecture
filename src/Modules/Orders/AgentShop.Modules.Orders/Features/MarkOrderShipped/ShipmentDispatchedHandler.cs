using AgentShop.Modules.Orders.Domain;
using AgentShop.Modules.Orders.Persistence;
using AgentShop.Modules.Shipping.Contracts;
using AgentShop.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgentShop.Modules.Orders.Features.MarkOrderShipped;

/// <summary>
/// Reacts to Shipping dispatching a shipment by moving the order to Shipped. Idempotent.
/// On a concurrency conflict (e.g. the order is being cancelled) the order is reloaded and the rule re-evaluated.
/// </summary>
internal sealed partial class ShipmentDispatchedHandler(OrdersDbContext db, ILogger<ShipmentDispatchedHandler> logger)
    : IIntegrationEventHandler<ShipmentDispatchedIntegrationEvent>
{
    private const int MaxAttempts = 3;

    public async Task HandleAsync(ShipmentDispatchedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var orderId = new OrderId(integrationEvent.OrderId);
        for (var attempt = 1; ; attempt++)
        {
            var order = await db.Orders.SingleOrDefaultAsync(o => o.Id == orderId, cancellationToken);
            if (order is null)
            {
                LogUnknownOrder(logger, integrationEvent.ShipmentId, integrationEvent.OrderId);
                return;
            }

            var shipped = order.MarkShipped(integrationEvent.DispatchedAt);
            if (shipped.IsFailure)
            {
                LogCannotMarkShipped(logger, integrationEvent.OrderId, shipped.Error.Code);
                return;
            }

            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxAttempts)
            {
                db.ChangeTracker.Clear();
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Shipment {ShipmentId} was dispatched for unknown order {OrderId}")]
    private static partial void LogUnknownOrder(ILogger logger, Guid shipmentId, Guid orderId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Order {OrderId} could not be marked as shipped: {ErrorCode}")]
    private static partial void LogCannotMarkShipped(ILogger logger, Guid orderId, string errorCode);
}
