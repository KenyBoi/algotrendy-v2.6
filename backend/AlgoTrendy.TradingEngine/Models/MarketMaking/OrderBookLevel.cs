namespace AlgoTrendy.TradingEngine.Models.MarketMaking;

/// <summary>
/// Represents a single price level in the order book (Level 2 data)
/// </summary>
public class OrderBookLevel
{
    /// <summary>
    /// Price at this level
    /// </summary>
    public required decimal Price { get; init; }

    /// <summary>
    /// Total quantity available at this price level
    /// </summary>
    public required decimal Quantity { get; init; }

    /// <summary>
    /// Number of orders at this price level (if available from exchange)
    /// </summary>
    public int? OrderCount { get; init; }

    /// <summary>
    /// Dollar value at this level (Price * Quantity)
    /// </summary>
    public decimal Value => Price * Quantity;

    /// <summary>
    /// Creates a copy of this order book level
    /// </summary>
    public OrderBookLevel Clone()
    {
        return new OrderBookLevel
        {
            Price = Price,
            Quantity = Quantity,
            OrderCount = OrderCount
        };
    }

    public override string ToString()
    {
        return $"{Price:F8} @ {Quantity:F4} ({Value:C2})";
    }
}
