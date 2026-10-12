using AgentShop.Modules.Orders.Features.PlaceOrder;

namespace AgentShop.Modules.Orders.Tests.Features;

public sealed class PlaceOrderValidatorTests
{
    private readonly PlaceOrder.Validator _validator = new();

    [Fact]
    public void ValidRequest_HasNoErrors()
    {
        var request = new PlaceOrder.Request("jan@example.com", [new PlaceOrder.Request.Line(Guid.CreateVersion7(), 2)]);

        Assert.True(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void InvalidEmail_HasError()
    {
        var request = new PlaceOrder.Request("not-an-email", [new PlaceOrder.Request.Line(Guid.CreateVersion7(), 2)]);

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PlaceOrder.Request.CustomerEmail));
    }

    [Fact]
    public void NoLines_HasError()
    {
        var result = _validator.Validate(new PlaceOrder.Request("jan@example.com", []));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PlaceOrder.Request.Lines));
    }

    [Fact]
    public void NullLine_HasErrorForTheLine()
    {
        var request = new PlaceOrder.Request("jan@example.com", [null!]);

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, e => e.PropertyName == "Lines[0]");
    }

    [Fact]
    public void QuantityOutOfRange_HasErrorForTheLine()
    {
        var request = new PlaceOrder.Request("jan@example.com", [new PlaceOrder.Request.Line(Guid.CreateVersion7(), 0)]);

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, e => e.PropertyName == "Lines[0].Quantity");
    }
}
