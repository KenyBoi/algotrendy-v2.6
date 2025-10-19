namespace AlgoTrendy.Core.Models;

/// <summary>
/// Margin and leverage configuration limits for a broker
/// </summary>
public class MarginConfiguration
{
    /// <summary>
    /// Broker name (e.g., "Binance", "OKX")
    /// </summary>
    public required string BrokerName { get; init; }

    /// <summary>
    /// Maximum leverage allowed (e.g., 20.0 for 20x)
    /// </summary>
    public decimal MaxLeverageAllowed { get; init; } = 10.0m;

    /// <summary>
    /// Default leverage if not specified (e.g., 3.0 for 3x)
    /// </summary>
    public decimal DefaultLeverage { get; init; } = 1.0m;

    /// <summary>
    /// Maintenance margin ratio (percent of position value required as margin)
    /// </summary>
    public decimal MaintenanceMarginRatio { get; init; } = 0.05m;  // 5%

    /// <summary>
    /// Initial margin ratio (percent of position value required to open)
    /// </summary>
    public decimal InitialMarginRatio { get; init; } = 0.10m;  // 10%

    /// <summary>
    /// Liquidation threshold (when position will be forcefully closed)
    /// </summary>
    public decimal LiquidationThreshold { get; init; } = 0.05m;  // 5%

    /// <summary>
    /// Supported margin types for this broker
    /// </summary>
    public bool SupportsIsolatedMargin { get; init; } = true;

    /// <summary>
    /// Whether cross margin is supported
    /// </summary>
    public bool SupportsCrossMargin { get; init; } = true;

    /// <summary>
    /// Daily interest rate for borrowed funds (percentage)
    /// </summary>
    public decimal DailyInterestRate { get; init; } = 0.001m;  // 0.1% daily

    /// <summary>
    /// Minimum position size to use leverage
    /// </summary>
    public decimal MinPositionSize { get; init; } = 10m;  // Minimum $10 position

    /// <summary>
    /// Maximum total leverage exposure allowed
    /// </summary>
    public decimal MaxTotalExposure { get; init; } = 10000000m;  // $10M max exposure

    /// <summary>
    /// Whether the broker is in maintenance mode (no leverage operations allowed)
    /// </summary>
    public bool IsMaintenanceMode { get; init; } = false;

    /// <summary>
    /// Configuration last updated (UTC)
    /// </summary>
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Validates if a leverage level is allowed
    /// </summary>
    public bool IsLeverageAllowed(decimal leverage) =>
        leverage >= 1.0m && leverage <= MaxLeverageAllowed && !IsMaintenanceMode;

    /// <summary>
    /// Calculates margin required for a position
    /// </summary>
    public decimal CalculateRequiredMargin(decimal positionValue, decimal leverage) =>
        (positionValue / leverage) * InitialMarginRatio;

    /// <summary>
    /// Calculates liquidation price for a position
    /// </summary>
    public decimal CalculateLiquidationPrice(decimal entryPrice, decimal leverage, bool isLongPosition) =>
        isLongPosition
            ? entryPrice * (1 - (MaintenanceMarginRatio / leverage))
            : entryPrice * (1 + (MaintenanceMarginRatio / leverage));
}
