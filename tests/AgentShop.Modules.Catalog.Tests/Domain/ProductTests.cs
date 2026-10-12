using AgentShop.Modules.Catalog.Domain;
using AgentShop.SharedKernel;
using Microsoft.Extensions.Time.Testing;

namespace AgentShop.Modules.Catalog.Tests.Domain;

public sealed class ProductTests
{
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Create_WithValidData_ReturnsActiveProductWithNormalizedSku()
    {
        var result = Product.Create("  kb-001 ", " Keyboard ", 1299.90m, _clock.GetUtcNow());

        Assert.True(result.IsSuccess);
        var product = result.Value;
        Assert.Equal("KB-001", product.Sku);
        Assert.Equal("Keyboard", product.Name);
        Assert.Equal(1299.90m, product.Price);
        Assert.Equal(Product.DefaultCurrency, product.Currency);
        Assert.True(product.IsActive);
        Assert.Equal(_clock.GetUtcNow(), product.CreatedAt);
        Assert.NotEqual(Guid.Empty, product.Id.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositivePrice_ReturnsValidationError(decimal price)
    {
        var result = Product.Create("KB-001", "Keyboard", price, _clock.GetUtcNow());

        Assert.True(result.IsFailure);
        var error = Assert.IsType<ValidationError>(result.Error);
        Assert.True(error.Errors.ContainsKey("Price"));
    }

    [Fact]
    public void Deactivate_IsIdempotent()
    {
        var product = Product.Create("KB-001", "Keyboard", 10m, _clock.GetUtcNow()).Value;

        product.Deactivate();
        product.Deactivate();

        Assert.False(product.IsActive);
    }
}
