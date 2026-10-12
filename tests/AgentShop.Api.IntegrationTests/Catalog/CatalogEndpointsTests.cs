using System.Net;
using System.Net.Http.Json;
using AgentShop.Api.IntegrationTests.Infrastructure;

namespace AgentShop.Api.IntegrationTests.Catalog;

[Trait("Category", "Integration")]
public sealed class CatalogEndpointsTests(AgentShopApiFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task CreateProduct_ValidRequest_ReturnsCreatedProduct()
    {
        var response = await Client.PostAsJsonAsync("/catalog/products", new { sku = "kb-001", name = "Keyboard", price = 1299.90m }, CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Matches("^/catalog/products/[0-9a-f-]{36}$", response.Headers.Location?.ToString());
        await JsonSnapshot.MatchAsync(await response.Content.ReadAsStringAsync(CancellationToken));
    }

    [Fact]
    public async Task CreateProduct_InvalidRequest_ReturnsValidationProblemAndPersistsNothing()
    {
        var response = await Client.PostAsJsonAsync("/catalog/products", new { sku = "bad sku", name = "", price = 0m }, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var list = await Client.GetFromJsonAsync<ListDto>("/catalog/products", CancellationToken);
        Assert.Equal(0, list!.TotalCount);
        await JsonSnapshot.MatchAsync(await response.Content.ReadAsStringAsync(CancellationToken));
    }

    [Fact]
    public async Task CreateProduct_SameSkuConcurrently_OneSucceedsAndOneConflicts()
    {
        var requests = Enumerable.Range(0, 2).Select(i =>
            Client.PostAsJsonAsync("/catalog/products", new { sku = "DUP-1", name = $"Product {i}", price = 10m }, CancellationToken));

        var statuses = (await Task.WhenAll(requests)).Select(r => r.StatusCode).Order().ToList();

        Assert.Equal([HttpStatusCode.Created, HttpStatusCode.Conflict], statuses);
    }

    [Fact]
    public async Task CreateProduct_SkuDiffersOnlyInCase_ReturnsConflict()
    {
        await Client.CreateProductAsync("KB-001", cancellationToken: CancellationToken);

        var response = await Client.PostAsJsonAsync("/catalog/products", new { sku = "kb-001", name = "Other", price = 1m }, CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await JsonSnapshot.MatchAsync(await response.Content.ReadAsStringAsync(CancellationToken));
    }

    [Fact]
    public async Task GetProduct_Unknown_ReturnsNotFound()
    {
        var response = await Client.GetAsync($"/catalog/products/{Guid.CreateVersion7()}", CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListProducts_ReturnsPageOrderedBySku()
    {
        foreach (var sku in new[] { "C-3", "A-1", "B-2" })
        {
            await Client.CreateProductAsync(sku, cancellationToken: CancellationToken);
        }

        var response = await Client.GetAsync("/catalog/products?page=1&pageSize=2", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await JsonSnapshot.MatchAsync(await response.Content.ReadAsStringAsync(CancellationToken));
    }

    [Fact]
    public async Task ListProducts_PageSizeTooLarge_ReturnsBadRequest()
    {
        var response = await Client.GetAsync("/catalog/products?pageSize=1000", CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ListProducts_PageThatWouldOverflowOffset_ReturnsBadRequest()
    {
        var response = await Client.GetAsync("/catalog/products?page=21474838&pageSize=100", CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateProduct_Existing_ReturnsNoContentAndProductIsInactive()
    {
        var product = await Client.CreateProductAsync("KB-001", cancellationToken: CancellationToken);

        var response = await Client.PostAsync($"/catalog/products/{product.Id}/deactivate", content: null, CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var reloaded = await Client.GetFromJsonAsync<ProductDto>($"/catalog/products/{product.Id}", CancellationToken);
        Assert.False(reloaded!.IsActive);
    }

    [Fact]
    public async Task DeactivateProduct_Unknown_ReturnsNotFound()
    {
        var response = await Client.PostAsync($"/catalog/products/{Guid.CreateVersion7()}/deactivate", content: null, CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record ListDto(int TotalCount);
}
