using AgentShop.SharedKernel;

namespace AgentShop.Modules.Orders.Contracts;

/// <summary>
/// Published by Orders after a new order has been committed.
/// </summary>
public sealed record OrderPlacedIntegrationEvent(
    Guid OrderId,
    string CustomerEmail,
    IReadOnlyList<OrderPlacedIntegrationEvent.Line> Lines,
    decimal Total,
    string Currency,
    DateTimeOffset PlacedAt) : IIntegrationEvent
{
    public sealed record Line(Guid ProductId, int Quantity);
}
