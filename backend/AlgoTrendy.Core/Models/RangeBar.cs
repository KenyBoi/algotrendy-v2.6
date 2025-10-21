using AlgoTrendy.Core.Enums;

namespace AlgoTrendy.Core.Models;

/// <summary>
/// Represents a range bar where each bar has a fixed high-low range
/// Bars are completed when the price range reaches the specified threshold
/// </summary>
public class RangeBar
{
    /// <summary>
    /// Trading symbol (e.g., "BTCUSDT")
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Timestamp when the bar was completed
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Opening price of the bar
    /// </summary>
    public required decimal Open { get; init; }

    /// <summary>
    /// Highest price during the bar
    /// </summary>
    public required decimal High { get; init; }

    /// <summary>
    /// Lowest price during the bar
    /// </summary>
    public required decimal Low { get; init; }

    /// <summary>
    /// Closing price of the bar
    /// </summary>
    public required decimal Close { get; init; }

    /// <summary>
    /// Fixed range threshold that triggers bar completion (High - Low)
    /// </summary>
    public required decimal RangeThreshold { get; init; }

    /// <summary>
    /// Actual range achieved (should equal or slightly exceed RangeThreshold)
    /// </summary>
    public decimal ActualRange => High - Low;

    /// <summary>
    /// Total volume in base currency
    /// </summary>
    public decimal Volume { get; init; }

    /// <summary>
    /// Total volume in quote currency
    /// </summary>
    public decimal QuoteVolume { get; init; }

    /// <summary>
    /// Number of ticks that formed this bar
    /// </summary>
    public int TickCount { get; init; }

    /// <summary>
    /// Volume from market buys
    /// </summary>
    public decimal BuyVolume { get; init; }

    /// <summary>
    /// Volume from market sells
    /// </summary>
    public decimal SellVolume { get; init; }

    /// <summary>
    /// Data source (binance, okx, bybit, etc.)
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// Asset type classification
    /// </summary>
    public AssetType AssetType { get; set; } = AssetType.Cryptocurrency;

    /// <summary>
    /// Duration it took to complete this bar
    /// </summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>
    /// Checks if the bar is bullish (close > open)
    /// </summary>
    public bool IsBullish => Close > Open;

    /// <summary>
    /// Checks if the bar is bearish (close < open)
    /// </summary>
    public bool IsBearish => Close < Open;

    /// <summary>
    /// Price change percentage
    /// </summary>
    public decimal ChangePercent =>
        Open != 0 ? ((Close - Open) / Open) * 100 : 0;

    /// <summary>
    /// Delta between buy and sell volume (positive = bullish)
    /// </summary>
    public decimal VolumeDelta => BuyVolume - SellVolume;

    /// <summary>
    /// Buy/Sell ratio (values > 1 indicate bullish pressure)
    /// </summary>
    public decimal BuySellRatio =>
        SellVolume > 0 ? BuyVolume / SellVolume : BuyVolume > 0 ? decimal.MaxValue : 1;
}
