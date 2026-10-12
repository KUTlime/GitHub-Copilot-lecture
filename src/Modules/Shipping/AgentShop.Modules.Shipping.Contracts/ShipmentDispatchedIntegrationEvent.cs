using AgentShop.SharedKernel;

namespace AgentShop.Modules.Shipping.Contracts;

/// <summary>
/// Published by Shipping when a shipment for an order has been dispatched.
/// </summary>
public sealed record ShipmentDispatchedIntegrationEvent(Guid ShipmentId, Guid OrderId, DateTimeOffset DispatchedAt) : IIntegrationEvent;
