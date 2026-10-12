using AgentShop.SharedKernel;

namespace AgentShop.Modules.Shipping.Domain;

/// <summary>
/// Aggregate root of the Shipping module. Exactly one shipment exists per order (unique index on OrderId).
/// </summary>
internal sealed class Shipment
{
    private Shipment()
    {
    }

    public ShipmentId Id { get; private set; }

    /// <summary>Id of the order in the Orders module. A plain Guid: modules never share entity types.</summary>
    public Guid OrderId { get; private set; }

    public ShipmentStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? DispatchedAt { get; private set; }

    public static Shipment CreateFor(Guid orderId, DateTimeOffset createdAt) =>
        new()
        {
            Id = ShipmentId.New(),
            OrderId = orderId,
            Status = ShipmentStatus.Pending,
            CreatedAt = createdAt,
        };

    public Result Dispatch(DateTimeOffset dispatchedAt)
    {
        if (Status == ShipmentStatus.Dispatched)
        {
            return ShippingErrors.ShipmentAlreadyDispatched;
        }

        Status = ShipmentStatus.Dispatched;
        DispatchedAt = dispatchedAt;
        return Result.Success();
    }
}
