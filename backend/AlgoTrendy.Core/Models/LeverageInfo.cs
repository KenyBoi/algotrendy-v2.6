using AlgoTrendy.Core.Enums;

namespace AlgoTrendy.Core.Models;

/// <summary>
/// Represents leverage and margin information for a position or account
/// </summary>
public class LeverageInfo
{
    /// <summary>
    /// Current leverage multiplier (e.g., 3.0 for 3x leverage)
    /// </summary>
    public required decimal CurrentLeverage { get; init; }

    /// <summary>
    /// Maximum allowed leverage for the symbol
    /// </summary>
    public required decimal MaxLeverage { get; init; }

    /// <summary>
    /// Margin type (Cross or Isolated)
    /// </summary>
    public required MarginType MarginType { get; init; }

    /// <summary>
    /// Total collateral amount
    /// </summary>
    public required decimal CollateralAmount { get; init; }

    /// <summary>
    /// Total borrowed amount
    /// </summary>
    public required decimal BorrowedAmount { get; init; }

    /// <summary>
    /// Interest rate on borrowed funds (daily percentage)
    /// </summary>
    public decimal InterestRate { get; init; }

    /// <summary>
    /// Liquidation price (price at which position will be liquidated)
    /// </summary>
    public decimal? LiquidationPrice { get; init; }

    /// <summary>
    /// Margin health ratio (0.0 to 1.0, where 1.0 is good and &lt;0.05 is liquidation risk)
    /// </summary>
    public decimal MarginHealthRatio { get; init; }

    /// <summary>
    /// Whether account is at risk of liquidation
    /// </summary>
    public bool IsLiquidationRisk => MarginHealthRatio < 0.05m;

    /// <summary>
    /// Margin maintenance required ratio
    /// </summary>
    public decimal MaintenanceMarginRatio { get; init; } = 0.05m;

    /// <summary>
    /// Initial margin required ratio
    /// </summary>
    public decimal InitialMarginRatio { get; init; } = 0.1m;

    /// <summary>
    /// Timestamp when info was retrieved (UTC)
    /// </summary>
    public DateTime RetrievedAt { get; init; } = DateTime.UtcNow;
}
