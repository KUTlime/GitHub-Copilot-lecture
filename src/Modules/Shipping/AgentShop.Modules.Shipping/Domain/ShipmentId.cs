namespace AgentShop.Modules.Shipping.Domain;

internal readonly record struct ShipmentId(Guid Value)
{
    public static ShipmentId New() => new(Guid.CreateVersion7());
}
