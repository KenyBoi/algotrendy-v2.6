using AlgoTrendy.Core.Enums;

namespace AlgoTrendy.Core.Models;

/// <summary>
/// Represents an executed trade (fill)
/// </summary>
public class Trade
{
    /// <summary>
    /// Unique identifier for the trade
    /// </summary>
    public required string TradeId { get; init; }

    /// <summary>
    /// Order ID that this trade is associated with
    /// </summary>
    public required string OrderId { get; init; }

    /// <summary>
    /// Exchange-provided trade ID
    /// </summary>
    public string? ExchangeTradeId { get; init; }

    /// <summary>
    /// Trading symbol
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Exchange where trade was executed
    /// </summary>
    public required string Exchange { get; init; }

    /// <summary>
    /// Trade side (Buy or Sell)
    /// </summary>
    public required OrderSide Side { get; init; }

    /// <summary>
    /// Execution price
    /// </summary>
    public required decimal Price { get; init; }

    /// <summary>
    /// Quantity executed (base currency)
    /// </summary>
    public required decimal Quantity { get; init; }

    /// <summary>
    /// Quote currency amount (Price * Quantity)
    /// </summary>
    public required decimal QuoteQuantity { get; init; }

    /// <summary>
    /// Fee paid for this trade
    /// </summary>
    public required decimal Fee { get; init; }

    /// <summary>
    /// Currency in which fee was paid
    /// </summary>
    public required string FeeCurrency { get; init; }

    /// <summary>
    /// Strategy ID that generated the order
    /// </summary>
    public string? StrategyId { get; init; }

    /// <summary>
    /// Timestamp when trade was executed (UTC)
    /// </summary>
    public required DateTime ExecutedAt { get; init; }

    /// <summary>
    /// Additional metadata in JSON format
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Calculates the total cost including fees for buy orders
    /// or total proceeds minus fees for sell orders
    /// </summary>
    public decimal NetAmount =>
        Side == OrderSide.Buy
            ? QuoteQuantity + Fee
            : QuoteQuantity - Fee;
}
