using AgentShop.Modules.Orders.Domain;
using AgentShop.SharedKernel;
using Microsoft.Extensions.Time.Testing;

namespace AgentShop.Modules.Orders.Tests.Domain;

public sealed class OrderTests
{
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Place_WithValidLines_CalculatesTotalAndIsPlaced()
    {
        var order = PlaceOrder(
            new OrderLine(Guid.CreateVersion7(), "Keyboard", 1000m, 2),
            new OrderLine(Guid.CreateVersion7(), "Mouse", 250.50m, 1));

        Assert.Equal(OrderStatus.Placed, order.Status);
        Assert.Equal(2250.50m, order.Total);
        Assert.Equal(_clock.GetUtcNow(), order.PlacedAt);
        Assert.Equal(2, order.Lines.Count);
    }

    [Fact]
    public void Place_WithoutLines_ReturnsValidationError()
    {
        var result = Order.Place("jan@example.com", "CZK", [], _clock.GetUtcNow());

        var error = Assert.IsType<ValidationError>(result.Error);
        Assert.True(error.Errors.ContainsKey("Lines"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(OrderLine.MaxQuantity + 1)]
    public void Place_WithQuantityOutOfRange_ReturnsValidationErrorForTheLine(int quantity)
    {
        var lines = new[]
        {
            new OrderLine(Guid.CreateVersion7(), "Keyboard", 1000m, 1),
            new OrderLine(Guid.CreateVersion7(), "Mouse", 250m, quantity),
        };

        var result = Order.Place("jan@example.com", "CZK", lines, _clock.GetUtcNow());

        var error = Assert.IsType<ValidationError>(result.Error);
        Assert.True(error.Errors.ContainsKey("Lines[1].Quantity"));
    }

    [Fact]
    public void Cancel_PlacedOrder_IsCancelled()
    {
        var order = PlaceOrder(new OrderLine(Guid.CreateVersion7(), "Keyboard", 1000m, 1));

        var result = order.Cancel(_clock.GetUtcNow());

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(_clock.GetUtcNow(), order.CancelledAt);
    }

    [Fact]
    public void Cancel_ShippedOrder_ReturnsConflict()
    {
        var order = PlaceOrder(new OrderLine(Guid.CreateVersion7(), "Keyboard", 1000m, 1));
        order.MarkShipped(_clock.GetUtcNow());

        var result = order.Cancel(_clock.GetUtcNow());

        Assert.Equal(OrderErrors.OnlyPlacedOrderCanBeCancelled, result.Error);
        Assert.Equal(OrderStatus.Shipped, order.Status);
    }

    [Fact]
    public void MarkShipped_IsIdempotent()
    {
        var order = PlaceOrder(new OrderLine(Guid.CreateVersion7(), "Keyboard", 1000m, 1));
        var firstShippedAt = _clock.GetUtcNow();
        order.MarkShipped(firstShippedAt);
        _clock.Advance(TimeSpan.FromHours(1));

        var result = order.MarkShipped(_clock.GetUtcNow());

        Assert.True(result.IsSuccess);
        Assert.Equal(firstShippedAt, order.ShippedAt);
    }

    [Fact]
    public void MarkShipped_CancelledOrder_ReturnsConflict()
    {
        var order = PlaceOrder(new OrderLine(Guid.CreateVersion7(), "Keyboard", 1000m, 1));
        order.Cancel(_clock.GetUtcNow());

        var result = order.MarkShipped(_clock.GetUtcNow());

        Assert.Equal(OrderErrors.CancelledOrderCannotBeShipped, result.Error);
    }

    private Order PlaceOrder(params OrderLine[] lines) =>
        Order.Place("jan@example.com", "CZK", lines, _clock.GetUtcNow()).Value;
}
