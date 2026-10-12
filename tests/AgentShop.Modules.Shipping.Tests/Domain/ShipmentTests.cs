using AgentShop.Modules.Shipping.Domain;
using Microsoft.Extensions.Time.Testing;

namespace AgentShop.Modules.Shipping.Tests.Domain;

public sealed class ShipmentTests
{
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void CreateFor_Order_IsPending()
    {
        var orderId = Guid.CreateVersion7();

        var shipment = Shipment.CreateFor(orderId, _clock.GetUtcNow());

        Assert.Equal(orderId, shipment.OrderId);
        Assert.Equal(ShipmentStatus.Pending, shipment.Status);
        Assert.Null(shipment.DispatchedAt);
    }

    [Fact]
    public void Dispatch_PendingShipment_IsDispatched()
    {
        var shipment = Shipment.CreateFor(Guid.CreateVersion7(), _clock.GetUtcNow());
        _clock.Advance(TimeSpan.FromHours(2));

        var result = shipment.Dispatch(_clock.GetUtcNow());

        Assert.True(result.IsSuccess);
        Assert.Equal(ShipmentStatus.Dispatched, shipment.Status);
        Assert.Equal(_clock.GetUtcNow(), shipment.DispatchedAt);
    }

    [Fact]
    public void Dispatch_AlreadyDispatched_ReturnsConflict()
    {
        var shipment = Shipment.CreateFor(Guid.CreateVersion7(), _clock.GetUtcNow());
        shipment.Dispatch(_clock.GetUtcNow());

        var result = shipment.Dispatch(_clock.GetUtcNow());

        Assert.Equal(ShippingErrors.ShipmentAlreadyDispatched, result.Error);
    }
}
