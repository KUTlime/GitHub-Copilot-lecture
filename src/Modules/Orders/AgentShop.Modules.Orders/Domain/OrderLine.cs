namespace AgentShop.Modules.Orders.Domain;

/// <summary>
/// A line of an order. Product name and unit price are snapshots taken from Catalog when the order was placed.
/// </summary>
internal sealed class OrderLine
{
    public const int MinQuantity = 1;
    public const int MaxQuantity = 100;

    private OrderLine()
    {
    }

    public OrderLine(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);

        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public Guid ProductId { get; private set; }

    public string ProductName { get; private set; } = string.Empty;

    public decimal UnitPrice { get; private set; }

    public int Quantity { get; private set; }

    public decimal LineTotal => UnitPrice * Quantity;
}
