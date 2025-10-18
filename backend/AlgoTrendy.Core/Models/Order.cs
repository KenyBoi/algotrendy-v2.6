using AlgoTrendy.Core.Enums;

namespace AlgoTrendy.Core.Models;

/// <summary>
/// Represents a trading order in the system
/// </summary>
public class Order
{
    /// <summary>
    /// Unique identifier for the order (UUID)
    /// </summary>
    public required string OrderId { get; init; }

    /// <summary>
    /// Exchange-provided order ID (after submission)
    /// </summary>
    public string? ExchangeOrderId { get; set; }

    /// <summary>
    /// Trading symbol (e.g., "BTCUSDT", "ETHUSDT")
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Exchange where order is placed (binance, okx, coinbase, kraken, bybit)
    /// </summary>
    public required string Exchange { get; init; }

    /// <summary>
    /// Order side (Buy or Sell)
    /// </summary>
    public required OrderSide Side { get; init; }

    /// <summary>
    /// Order type (Market, Limit, StopLoss, etc.)
    /// </summary>
    public required OrderType Type { get; init; }

    /// <summary>
    /// Current status of the order
    /// </summary>
    public required OrderStatus Status { get; set; }

    /// <summary>
    /// Order quantity (base currency)
    /// </summary>
    public required decimal Quantity { get; init; }

    /// <summary>
    /// Quantity that has been filled so far
    /// </summary>
    public decimal FilledQuantity { get; set; }

    /// <summary>
    /// Limit price (null for market orders)
    /// </summary>
    public decimal? Price { get; init; }

    /// <summary>
    /// Stop price for stop orders (null for non-stop orders)
    /// </summary>
    public decimal? StopPrice { get; init; }

    /// <summary>
    /// Average fill price (calculated from executions)
    /// </summary>
    public decimal? AverageFillPrice { get; set; }

    /// <summary>
    /// Strategy ID that generated this order
    /// </summary>
    public string? StrategyId { get; init; }

    /// <summary>
    /// Timestamp when order was created (UTC)
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// Timestamp when order was last updated (UTC)
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Timestamp when order was submitted to exchange (UTC)
    /// </summary>
    public DateTime? SubmittedAt { get; set; }

    /// <summary>
    /// Timestamp when order was filled/cancelled/expired (UTC)
    /// </summary>
    public DateTime? ClosedAt { get; set; }

    /// <summary>
    /// Additional metadata in JSON format
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Calculates the remaining unfilled quantity
    /// </summary>
    public decimal RemainingQuantity => Quantity - FilledQuantity;

    /// <summary>
    /// Checks if order is in a terminal state (cannot be modified)
    /// </summary>
    public bool IsTerminal => Status is OrderStatus.Filled
        or OrderStatus.Cancelled
        or OrderStatus.Rejected
        or OrderStatus.Expired;

    /// <summary>
    /// Checks if order is active on the exchange
    /// </summary>
    public bool IsActive => Status is OrderStatus.Open or OrderStatus.PartiallyFilled;
}
