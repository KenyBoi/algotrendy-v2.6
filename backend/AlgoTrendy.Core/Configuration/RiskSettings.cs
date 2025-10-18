namespace AlgoTrendy.Core.Configuration;

/// <summary>
/// Risk management settings for trading operations
/// </summary>
public class RiskSettings
{
    /// <summary>
    /// Maximum position size as percentage of account balance (0-100)
    /// </summary>
    public decimal MaxPositionSizePercent { get; set; } = 10m;

    /// <summary>
    /// Default stop loss percentage (0-100)
    /// </summary>
    public decimal DefaultStopLossPercent { get; set; } = 5m;

    /// <summary>
    /// Default take profit percentage (0-100)
    /// </summary>
    public decimal DefaultTakeProfitPercent { get; set; } = 10m;

    /// <summary>
    /// Maximum number of concurrent open positions
    /// </summary>
    public int MaxConcurrentPositions { get; set; } = 5;

    /// <summary>
    /// Maximum total exposure as percentage of account balance (0-100)
    /// </summary>
    public decimal MaxTotalExposurePercent { get; set; } = 50m;

    /// <summary>
    /// Minimum order size in quote currency (e.g., USDT)
    /// </summary>
    public decimal MinOrderSize { get; set; } = 10m;

    /// <summary>
    /// Maximum order size in quote currency (e.g., USDT)
    /// </summary>
    public decimal? MaxOrderSize { get; set; }

    /// <summary>
    /// Enable risk validation before placing orders
    /// </summary>
    public bool EnableRiskValidation { get; set; } = true;
}
