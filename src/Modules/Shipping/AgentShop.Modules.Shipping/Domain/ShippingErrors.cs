using AgentShop.SharedKernel;

namespace AgentShop.Modules.Shipping.Domain;

internal static class ShippingErrors
{
    public static readonly Error ShipmentNotFound =
        Error.NotFound("Shipping.ShipmentNotFound", "The shipment was not found.");

    public static readonly Error ShipmentAlreadyDispatched =
        Error.Conflict("Shipping.ShipmentAlreadyDispatched", "The shipment has already been dispatched.");

    public static readonly Error ConcurrentModification =
        Error.Conflict("Shipping.ConcurrentModification", "The shipment was modified by another request. Reload it and try again.");
}
