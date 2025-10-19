namespace AlgoTrendy.Core.Models;

/// <summary>
/// Summarizes debt and margin status across the account
/// </summary>
public class DebtSummary
{
    /// <summary>
    /// Total amount borrowed (in base currency, e.g., USDT)
    /// </summary>
    public decimal TotalBorrowedAmount { get; init; }

    /// <summary>
    /// Total collateral locked (in base currency)
    /// </summary>
    public decimal TotalCollateralAmount { get; init; }

    /// <summary>
    /// Total interest accrued on borrowed funds
    /// </summary>
    public decimal TotalInterestAccrued { get; init; }

    /// <summary>
    /// Number of active leveraged positions
    /// </summary>
    public int ActiveLeveragedPositions { get; init; }

    /// <summary>
    /// Account margin health ratio (0.0 to 1.0)
    /// </summary>
    public decimal MarginHealthRatio { get; init; }

    /// <summary>
    /// Whether account is at risk of liquidation
    /// </summary>
    public bool IsAtLiquidationRisk => MarginHealthRatio < 0.05m;

    /// <summary>
    /// Total leverage exposure (sum of all position sizes * leverage)
    /// </summary>
    public decimal TotalLeverageExposure { get; init; }

    /// <summary>
    /// Average leverage across all positions
    /// </summary>
    public decimal AverageLeverage { get; init; }

    /// <summary>
    /// Maximum leverage currently in use
    /// </summary>
    public decimal MaxLeverageInUse { get; init; }

    /// <summary>
    /// Total PnL from all leveraged positions
    /// </summary>
    public decimal TotalPnL { get; init; }

    /// <summary>
    /// Daily interest cost at current rates
    /// </summary>
    public decimal DailyInterestCost { get; init; }

    /// <summary>
    /// Timestamp when summary was generated (UTC)
    /// </summary>
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Available collateral for new positions
    /// </summary>
    public decimal AvailableCollateral => TotalCollateralAmount - TotalBorrowedAmount;

    /// <summary>
    /// Debt-to-collateral ratio
    /// </summary>
    public decimal DebtToCollateralRatio => TotalCollateralAmount > 0
        ? TotalBorrowedAmount / TotalCollateralAmount
        : 0;
}
