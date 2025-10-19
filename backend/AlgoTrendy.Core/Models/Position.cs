using AlgoTrendy.Core.Enums;

namespace AlgoTrendy.Core.Models;

/// <summary>
/// Represents an active trading position
/// </summary>
public class Position
{
    /// <summary>
    /// Unique position identifier
    /// </summary>
    public required string PositionId { get; init; }

    /// <summary>
    /// Trading symbol (e.g., "BTCUSDT")
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Exchange where position is held
    /// </summary>
    public required string Exchange { get; init; }

    /// <summary>
    /// Position side (Buy = Long, Sell = Short)
    /// </summary>
    public required OrderSide Side { get; init; }

    /// <summary>
    /// Position size in base currency
    /// </summary>
    public required decimal Quantity { get; init; }

    /// <summary>
    /// Average entry price
    /// </summary>
    public required decimal EntryPrice { get; init; }

    /// <summary>
    /// Current market price
    /// </summary>
    public decimal CurrentPrice { get; set; }

    /// <summary>
    /// Stop loss price (optional)
    /// </summary>
    public decimal? StopLoss { get; set; }

    /// <summary>
    /// Take profit price (optional)
    /// </summary>
    public decimal? TakeProfit { get; set; }

    /// <summary>
    /// Strategy ID that opened this position
    /// </summary>
    public string? StrategyId { get; init; }

    /// <summary>
    /// Order ID that opened this position
    /// </summary>
    public string? OpenOrderId { get; init; }

    /// <summary>
    /// Timestamp when position was opened (UTC)
    /// </summary>
    public required DateTime OpenedAt { get; init; }

    /// <summary>
    /// Timestamp when position was last updated (UTC)
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Calculates unrealized PnL in dollars
    /// </summary>
    public decimal UnrealizedPnL
    {
        get
        {
            if (Side == OrderSide.Buy)
            {
                return (CurrentPrice - EntryPrice) * Quantity;
            }
            else
            {
                return (EntryPrice - CurrentPrice) * Quantity;
            }
        }
    }

    /// <summary>
    /// Calculates unrealized PnL percentage
    /// </summary>
    public decimal UnrealizedPnLPercent
    {
        get
        {
            if (EntryPrice == 0) return 0;

            if (Side == OrderSide.Buy)
            {
                return ((CurrentPrice - EntryPrice) / EntryPrice) * 100;
            }
            else
            {
                return ((EntryPrice - CurrentPrice) / EntryPrice) * 100;
            }
        }
    }

    /// <summary>
    /// Total position value at entry
    /// </summary>
    public decimal EntryValue => EntryPrice * Quantity;

    /// <summary>
    /// Current total position value
    /// </summary>
    public decimal CurrentValue => CurrentPrice * Quantity;

    /// <summary>
    /// Checks if stop loss is hit
    /// </summary>
    public bool IsStopLossHit
    {
        get
        {
            if (!StopLoss.HasValue) return false;

            return Side == OrderSide.Buy
                ? CurrentPrice <= StopLoss.Value
                : CurrentPrice >= StopLoss.Value;
        }
    }

    /// <summary>
    /// Checks if take profit is hit
    /// </summary>
    public bool IsTakeProfitHit
    {
        get
        {
            if (!TakeProfit.HasValue) return false;

            return Side == OrderSide.Buy
                ? CurrentPrice >= TakeProfit.Value
                : CurrentPrice <= TakeProfit.Value;
        }
    }

    /// <summary>
    /// Leverage multiplier (1.0 for spot, >1 for margin) - optional
    /// </summary>
    public decimal Leverage { get; set; } = 1.0m;

    /// <summary>
    /// Margin type (Cross or Isolated) - optional
    /// </summary>
    public Enums.MarginType? MarginType { get; set; }

    /// <summary>
    /// Collateral amount (for margin positions)
    /// </summary>
    public decimal? CollateralAmount { get; set; }

    /// <summary>
    /// Borrowed amount (for margin positions)
    /// </summary>
    public decimal? BorrowedAmount { get; set; }

    /// <summary>
    /// Interest rate on borrowed funds (daily percentage)
    /// </summary>
    public decimal? InterestRate { get; set; }

    /// <summary>
    /// Liquidation price (price at which position will be automatically closed)
    /// </summary>
    public decimal? LiquidationPrice { get; set; }

    /// <summary>
    /// Margin health ratio (0.0 to 1.0 scale)
    /// </summary>
    public decimal? MarginHealthRatio { get; set; }

    /// <summary>
    /// Whether this position is using margin/leverage
    /// </summary>
    public bool IsMarginPosition => Leverage > 1.0m || MarginType.HasValue;

    /// <summary>
    /// Calculates the effective position size with leverage
    /// </summary>
    public decimal EffectivePositionSize => CurrentValue * Leverage;

    /// <summary>
    /// Calculates used margin amount
    /// </summary>
    public decimal UsedMargin => CurrentValue / Leverage;

    /// <summary>
    /// Checks if position is in liquidation risk zone
    /// </summary>
    public bool IsInLiquidationRisk => MarginHealthRatio.HasValue && MarginHealthRatio.Value < 0.05m;
}
