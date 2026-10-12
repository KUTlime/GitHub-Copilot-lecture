using AgentShop.SharedKernel;

namespace AgentShop.Modules.Orders.Domain;

internal static class OrderErrors
{
    public static readonly Error OrderNotFound =
        Error.NotFound("Orders.OrderNotFound", "The order was not found.");

    public static readonly Error OnlyPlacedOrderCanBeCancelled =
        Error.Conflict("Orders.OnlyPlacedOrderCanBeCancelled", "Only an order in the Placed state can be cancelled.");

    public static readonly Error CancelledOrderCannotBeShipped =
        Error.Conflict("Orders.CancelledOrderCannotBeShipped", "A cancelled order cannot be marked as shipped.");

    public static readonly Error ConcurrentModification =
        Error.Conflict("Orders.ConcurrentModification", "The order was modified by another request. Reload it and try again.");

    public static readonly ValidationError OrderMustHaveLines =
        ValidationError.For("Lines", "An order must contain at least one line.");

    public static ValidationError QuantityOutOfRange(int lineIndex) =>
        ValidationError.For($"Lines[{lineIndex}].Quantity", $"Quantity must be between {OrderLine.MinQuantity} and {OrderLine.MaxQuantity}.");
}
