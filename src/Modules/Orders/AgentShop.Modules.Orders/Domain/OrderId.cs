namespace AgentShop.Modules.Orders.Domain;

internal readonly record struct OrderId(Guid Value)
{
    public static OrderId New() => new(Guid.CreateVersion7());
}
