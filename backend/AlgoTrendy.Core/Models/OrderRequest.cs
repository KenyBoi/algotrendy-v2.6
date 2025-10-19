using AlgoTrendy.Core.Enums;

namespace AlgoTrendy.Core.Models;

/// <summary>
/// Represents a request to place an order
/// </summary>
public class OrderRequest
{
    /// <summary>
    /// Client-generated idempotency key (optional - auto-generated if not provided)
    /// Used to prevent duplicate orders on network retries
    /// </summary>
    public string? ClientOrderId { get; init; }

    /// <summary>
    /// Trading symbol (e.g., "BTCUSDT")
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Exchange where order should be placed
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
    /// Order quantity in base currency
    /// </summary>
    public required decimal Quantity { get; init; }

    /// <summary>
    /// Limit price (required for Limit orders)
    /// </summary>
    public decimal? Price { get; init; }

    /// <summary>
    /// Stop price (required for StopLoss/StopLimit orders)
    /// </summary>
    public decimal? StopPrice { get; init; }

    /// <summary>
    /// Strategy ID that generated this order
    /// </summary>
    public string? StrategyId { get; init; }

    /// <summary>
    /// Additional metadata in JSON format
    /// </summary>
    public string? Metadata { get; init; }
}
