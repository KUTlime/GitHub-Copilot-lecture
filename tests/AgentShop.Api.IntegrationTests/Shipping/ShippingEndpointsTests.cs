using System.Net;
using System.Net.Http.Json;
using AgentShop.Api.IntegrationTests.Infrastructure;
using AgentShop.Modules.Orders.Contracts;
using AgentShop.SharedKernel;
using Microsoft.Extensions.DependencyInjection;

namespace AgentShop.Api.IntegrationTests.Shipping;

[Trait("Category", "Integration")]
public sealed class ShippingEndpointsTests(AgentShopApiFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task DispatchShipment_PendingShipment_MarksOrderAsShipped()
    {
        var product = await Client.CreateProductAsync("KB-001", cancellationToken: CancellationToken);
        var placed = await Client.PlaceOrderAsync(product.Id, cancellationToken: CancellationToken);
        var shipment = await Client.GetShipmentForOrderAsync(placed.OrderId, CancellationToken);
        Factory.Clock.Advance(TimeSpan.FromHours(3));

        var response = await Client.PostAsync($"/shipping/shipments/{shipment.Id}/dispatch", content: null, CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var order = await Client.GetFromJsonAsync<OrderDto>($"/orders/{placed.OrderId}", CancellationToken);
        Assert.Equal("Shipped", order!.Status);
        Assert.Equal(AgentShopApiFactory.StartTime.AddHours(3), order.ShippedAt);
        await JsonSnapshot.MatchAsync(await Client.GetStringAsync($"/shipping/shipments?orderId={placed.OrderId}", CancellationToken));
    }

    [Fact]
    public async Task DispatchShipment_Twice_ReturnsConflict()
    {
        var product = await Client.CreateProductAsync("KB-001", cancellationToken: CancellationToken);
        var placed = await Client.PlaceOrderAsync(product.Id, cancellationToken: CancellationToken);
        var shipment = await Client.GetShipmentForOrderAsync(placed.OrderId, CancellationToken);
        await Client.PostAsync($"/shipping/shipments/{shipment.Id}/dispatch", content: null, CancellationToken);

        var response = await Client.PostAsync($"/shipping/shipments/{shipment.Id}/dispatch", content: null, CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetShipmentByOrder_UnknownOrder_ReturnsNotFound()
    {
        var response = await Client.GetAsync($"/shipping/shipments?orderId={Guid.CreateVersion7()}", CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetShipmentByOrder_EmptyOrderId_ReturnsBadRequest()
    {
        var response = await Client.GetAsync($"/shipping/shipments?orderId={Guid.Empty}", CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OrderPlacedEvent_DeliveredTwice_CreatesSingleShipment()
    {
        var product = await Client.CreateProductAsync("KB-001", cancellationToken: CancellationToken);
        var placed = await Client.PlaceOrderAsync(product.Id, cancellationToken: CancellationToken);
        var duplicate = new OrderPlacedIntegrationEvent(
            placed.OrderId,
            "jan.novak@example.com",
            [new OrderPlacedIntegrationEvent.Line(product.Id, 1)],
            placed.Total,
            placed.Currency,
            AgentShopApiFactory.StartTime);

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();
            await publisher.PublishAsync(duplicate, CancellationToken);
        }

        // GetShipmentByOrder uses SingleOrDefault: a duplicate shipment would surface as a 500.
        var response = await Client.GetAsync($"/shipping/shipments?orderId={placed.OrderId}", CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
