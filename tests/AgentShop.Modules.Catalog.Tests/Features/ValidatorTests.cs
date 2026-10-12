using AgentShop.Modules.Catalog.Features.CreateProduct;
using AgentShop.Modules.Catalog.Features.ListProducts;

namespace AgentShop.Modules.Catalog.Tests.Features;

public sealed class ValidatorTests
{
    [Fact]
    public void CreateProduct_ValidRequest_HasNoErrors()
    {
        var result = new CreateProduct.Validator().Validate(new CreateProduct.Request("KB-001", "Keyboard", 1299.90m));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "Keyboard", 10)]
    [InlineData("KB 001", "Keyboard", 10)]
    [InlineData("KB-001", "", 10)]
    [InlineData("KB-001", "Keyboard", 0)]
    [InlineData("KB-001", "Keyboard", 10.123)]
    public void CreateProduct_InvalidRequest_HasErrors(string sku, string name, decimal price)
    {
        var result = new CreateProduct.Validator().Validate(new CreateProduct.Request(sku, name, price));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, ListProducts.MaxPageSize + 1)]
    [InlineData(ListProducts.MaxPage + 1, ListProducts.MaxPageSize)]
    public void ListProducts_InvalidPaging_HasErrors(int page, int pageSize)
    {
        var result = new ListProducts.Validator().Validate(new ListProducts.Request(page, pageSize));

        Assert.False(result.IsValid);
    }
}
