using System.Net;
using System.Net.Http.Json;
using AgentShop.Api.IntegrationTests.Infrastructure;

namespace AgentShop.Api.IntegrationTests.Orders;

[Trait("Category", "Integration")]
public sealed class OrdersEndpointsTests(AgentShopApiFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task PlaceOrder_ValidRequest_CreatesOrderAndPendingShipment()
    {
        var keyboard = await Client.CreateProductAsync("KB-001", 1000m, CancellationToken);
        var mouse = await Client.CreateProductAsync("MS-001", 250.50m, CancellationToken);

        var response = await Client.PostAsJsonAsync(
            "/orders",
            new
            {
                customerEmail = "jan.novak@example.com",
                lines = new[] { new { productId = keyboard.Id, quantity = 2 }, new { productId = mouse.Id, quantity = 1 } },
            },
            CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var placed = (await response.Content.ReadFromJsonAsync<PlacedOrderDto>(CancellationToken))!;
        Assert.Equal(2250.50m, placed.Total);
        Assert.Equal($"/orders/{placed.OrderId}", response.Headers.Location?.ToString());

        var shipment = await Client.GetShipmentForOrderAsync(placed.OrderId, CancellationToken);
        Assert.Equal("Pending", shipment.Status);

        await JsonSnapshot.MatchAsync(await Client.GetStringAsync($"/orders/{placed.OrderId}", CancellationToken));
    }

    [Fact]
    public async Task PlaceOrder_UnknownProduct_ReturnsValidationProblemForTheLine()
    {
        var response = await Client.PostAsJsonAsync(
            "/orders",
            new { customerEmail = "jan.novak@example.com", lines = new[] { new { productId = Guid.CreateVersion7(), quantity = 1 } } },
            CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await JsonSnapshot.MatchAsync(await response.Content.ReadAsStringAsync(CancellationToken));
    }

    [Fact]
    public async Task PlaceOrder_InactiveProduct_ReturnsBadRequest()
    {
        var product = await Client.CreateProductAsync("KB-001", cancellationToken: CancellationToken);
        await Client.PostAsync($"/catalog/products/{product.Id}/deactivate", content: null, CancellationToken);

        var response = await Client.PostAsJsonAsync(
            "/orders",
            new { customerEmail = "jan.novak@example.com", lines = new[] { new { productId = product.Id, quantity = 1 } } },
            CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PlaceOrder_InvalidRequest_ReturnsValidationProblem()
    {
        var response = await Client.PostAsJsonAsync(
            "/orders",
            new { customerEmail = "not-an-email", lines = new[] { new { productId = Guid.Empty, quantity = 0 } } },
            CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await JsonSnapshot.MatchAsync(await response.Content.ReadAsStringAsync(CancellationToken));
    }

    [Fact]
    public async Task CancelOrder_PlacedOrder_ReturnsNoContentAndOrderIsCancelled()
    {
        var product = await Client.CreateProductAsync("KB-001", cancellationToken: CancellationToken);
        var placed = await Client.PlaceOrderAsync(product.Id, cancellationToken: CancellationToken);

        var response = await Client.PostAsync($"/orders/{placed.OrderId}/cancel", content: null, CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var order = await Client.GetFromJsonAsync<OrderDto>($"/orders/{placed.OrderId}", CancellationToken);
        Assert.Equal("Cancelled", order!.Status);
        Assert.Equal(AgentShopApiFactory.StartTime, order.CancelledAt);
    }

    [Fact]
    public async Task CancelOrder_ShippedOrder_ReturnsConflict()
    {
        var product = await Client.CreateProductAsync("KB-001", cancellationToken: CancellationToken);
        var placed = await Client.PlaceOrderAsync(product.Id, cancellationToken: CancellationToken);
        var shipment = await Client.GetShipmentForOrderAsync(placed.OrderId, CancellationToken);
        await Client.PostAsync($"/shipping/shipments/{shipment.Id}/dispatch", content: null, CancellationToken);

        var response = await Client.PostAsync($"/orders/{placed.OrderId}/cancel", content: null, CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await JsonSnapshot.MatchAsync(await response.Content.ReadAsStringAsync(CancellationToken));
    }

    [Fact]
    public async Task GetOrder_Unknown_ReturnsNotFound()
    {
        var response = await Client.GetAsync($"/orders/{Guid.CreateVersion7()}", CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PlaceOrder_NullLine_ReturnsValidationProblem()
    {
        using var content = new StringContent("""{"customerEmail":"jan.novak@example.com","lines":[null]}""", System.Text.Encoding.UTF8, "application/json");

        var response = await Client.PostAsync("/orders", content, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CancelAndDispatchConcurrently_OrderEndsInExactlyOneTerminalState()
    {
        var product = await Client.CreateProductAsync("KB-001", cancellationToken: CancellationToken);

        // The interleaving is non-deterministic; the invariant must hold for every one of them.
        for (var i = 0; i < 10; i++)
        {
            var placed = await Client.PlaceOrderAsync(product.Id, cancellationToken: CancellationToken);
            var shipment = await Client.GetShipmentForOrderAsync(placed.OrderId, CancellationToken);

            var responses = await Task.WhenAll(
                Client.PostAsync($"/orders/{placed.OrderId}/cancel", content: null, CancellationToken),
                Client.PostAsync($"/shipping/shipments/{shipment.Id}/dispatch", content: null, CancellationToken));

            Assert.All(responses, r => Assert.True(
                r.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.Conflict,
                $"Unexpected status {(int)r.StatusCode} for {r.RequestMessage?.RequestUri}"));

            var order = (await Client.GetFromJsonAsync<OrderDto>($"/orders/{placed.OrderId}", CancellationToken))!;
            Assert.False(order.CancelledAt is not null && order.ShippedAt is not null, "An order must not be both cancelled and shipped.");
            Assert.Equal(order.Status == "Cancelled", order.CancelledAt is not null);
            Assert.Equal(order.Status == "Shipped", order.ShippedAt is not null);
        }
    }
}
