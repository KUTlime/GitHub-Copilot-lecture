using AgentShop.SharedKernel;

namespace AgentShop.Modules.Orders.Domain;

/// <summary>
/// Aggregate root of the Orders module. State transitions: Placed -> Cancelled, Placed -> Shipped.
/// </summary>
internal sealed class Order
{
    public const int CustomerEmailMaxLength = 320;

    private readonly List<OrderLine> _lines = [];

    private Order()
    {
    }

    public OrderId Id { get; private set; }

    public string CustomerEmail { get; private set; } = string.Empty;

    public string Currency { get; private set; } = string.Empty;

    public OrderStatus Status { get; private set; }

    public DateTimeOffset PlacedAt { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public DateTimeOffset? ShippedAt { get; private set; }

    public IReadOnlyList<OrderLine> Lines => _lines;

    public decimal Total => _lines.Sum(line => line.LineTotal);

    public static Result<Order> Place(string customerEmail, string currency, IReadOnlyList<OrderLine> lines, DateTimeOffset placedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        ArgumentNullException.ThrowIfNull(lines);

        if (lines.Count == 0)
        {
            return OrderErrors.OrderMustHaveLines;
        }

        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].Quantity is < OrderLine.MinQuantity or > OrderLine.MaxQuantity)
            {
                return OrderErrors.QuantityOutOfRange(i);
            }
        }

        var order = new Order
        {
            Id = OrderId.New(),
            CustomerEmail = customerEmail.Trim(),
            Currency = currency,
            Status = OrderStatus.Placed,
            PlacedAt = placedAt,
        };
        order._lines.AddRange(lines);

        return order;
    }

    public Result Cancel(DateTimeOffset cancelledAt)
    {
        if (Status != OrderStatus.Placed)
        {
            return OrderErrors.OnlyPlacedOrderCanBeCancelled;
        }

        Status = OrderStatus.Cancelled;
        CancelledAt = cancelledAt;
        return Result.Success();
    }

    /// <summary>
    /// Idempotent: marking an already shipped order again is a no-op.
    /// </summary>
    public Result MarkShipped(DateTimeOffset shippedAt)
    {
        switch (Status)
        {
            case OrderStatus.Shipped:
                return Result.Success();
            case OrderStatus.Cancelled:
                return OrderErrors.CancelledOrderCannotBeShipped;
            case OrderStatus.Placed:
            default:
                Status = OrderStatus.Shipped;
                ShippedAt = shippedAt;
                return Result.Success();
        }
    }
}
