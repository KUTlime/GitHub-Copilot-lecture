using System.Net;
using System.Net.Http.Json;

namespace AgentShop.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Test-side view of the HTTP contract. These DTOs are deliberately independent of the production types,
/// so a breaking change in the API breaks these tests.
/// </summary>
public sealed record ProductDto(Guid Id, string Sku, string Name, decimal Price, string Currency, bool IsActive);

public sealed record PlacedOrderDto(Guid OrderId, decimal Total, string Currency);

public sealed record OrderDto(Guid Id, string Status, decimal Total, DateTimeOffset? CancelledAt, DateTimeOffset? ShippedAt);

public sealed record ShipmentDto(Guid Id, Guid OrderId, string Status);

public static class ApiClientExtensions
{
    public static async Task<ProductDto> CreateProductAsync(this HttpClient client, string sku, decimal price = 100m, CancellationToken cancellationToken = default)
    {
        var response = await client.PostAsJsonAsync("/catalog/products", new { sku, name = $"Product {sku}", price }, cancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProductDto>(cancellationToken))!;
    }

    public static async Task<PlacedOrderDto> PlaceOrderAsync(this HttpClient client, Guid productId, int quantity = 1, CancellationToken cancellationToken = default)
    {
        var response = await client.PostAsJsonAsync(
            "/orders",
            new { customerEmail = "jan.novak@example.com", lines = new[] { new { productId, quantity } } },
            cancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PlacedOrderDto>(cancellationToken))!;
    }

    public static async Task<ShipmentDto> GetShipmentForOrderAsync(this HttpClient client, Guid orderId, CancellationToken cancellationToken = default)
    {
        var shipment = await client.GetFromJsonAsync<ShipmentDto>($"/shipping/shipments?orderId={orderId}", cancellationToken);
        return shipment!;
    }
}
