using AlgoTrendy.Core.Enums;
using System.ComponentModel.DataAnnotations;

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
    [StringLength(100, ErrorMessage = "ClientOrderId must be 100 characters or less")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "ClientOrderId can only contain alphanumeric characters, hyphens, and underscores")]
    public string? ClientOrderId { get; init; }

    /// <summary>
    /// Trading symbol (e.g., "BTCUSDT")
    /// </summary>
    [Required(ErrorMessage = "Symbol is required")]
    [StringLength(20, MinimumLength = 3, ErrorMessage = "Symbol must be between 3 and 20 characters")]
    [RegularExpression(@"^[A-Z0-9-_/]+$", ErrorMessage = "Symbol must contain only uppercase letters, numbers, hyphens, underscores, or forward slashes")]
    public required string Symbol { get; init; }

    /// <summary>
    /// Exchange where order should be placed
    /// </summary>
    [Required(ErrorMessage = "Exchange is required")]
    [StringLength(50, ErrorMessage = "Exchange name must be 50 characters or less")]
    [RegularExpression(@"^[a-zA-Z]+$", ErrorMessage = "Exchange must contain only letters")]
    public required string Exchange { get; init; }

    /// <summary>
    /// Order side (Buy or Sell)
    /// </summary>
    [Required(ErrorMessage = "Order side is required")]
    public required OrderSide Side { get; init; }

    /// <summary>
    /// Order type (Market, Limit, StopLoss, etc.)
    /// </summary>
    [Required(ErrorMessage = "Order type is required")]
    public required OrderType Type { get; init; }

    /// <summary>
    /// Order quantity in base currency
    /// </summary>
    [Required(ErrorMessage = "Quantity is required")]
    [Range(0.00000001, 1000000, ErrorMessage = "Quantity must be between 0.00000001 and 1,000,000")]
    public required decimal Quantity { get; init; }

    /// <summary>
    /// Limit price (required for Limit orders)
    /// </summary>
    [Range(0.00000001, 10000000, ErrorMessage = "Price must be between 0.00000001 and 10,000,000")]
    public decimal? Price { get; init; }

    /// <summary>
    /// Stop price (required for StopLoss/StopLimit orders)
    /// </summary>
    [Range(0.00000001, 10000000, ErrorMessage = "StopPrice must be between 0.00000001 and 10,000,000")]
    public decimal? StopPrice { get; init; }

    /// <summary>
    /// Strategy ID that generated this order
    /// </summary>
    [StringLength(100, ErrorMessage = "StrategyId must be 100 characters or less")]
    public string? StrategyId { get; init; }

    /// <summary>
    /// Additional metadata in JSON format
    /// </summary>
    [StringLength(5000, ErrorMessage = "Metadata must be 5000 characters or less")]
    public string? Metadata { get; init; }
}
