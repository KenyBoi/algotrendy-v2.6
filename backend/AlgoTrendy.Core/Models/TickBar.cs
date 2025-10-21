using AlgoTrendy.Core.Enums;

namespace AlgoTrendy.Core.Models;

/// <summary>
/// Represents a volume-based bar constructed from tick data
/// Each bar contains exactly N ticks (e.g., 100, 500, 1000)
/// </summary>
public class TickBar
{
    /// <summary>
    /// Trading symbol (e.g., "BTCUSDT")
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Timestamp when the bar was completed (last tick timestamp)
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Opening price (first tick in the bar)
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
    /// Closing price (last tick in the bar)
    /// </summary>
    public required decimal Close { get; init; }

    /// <summary>
    /// Total volume in base currency
    /// </summary>
    public required decimal Volume { get; init; }

    /// <summary>
    /// Total volume in quote currency
    /// </summary>
    public required decimal QuoteVolume { get; init; }

    /// <summary>
    /// Number of ticks in this bar (e.g., 100, 500, 1000)
    /// </summary>
    public required int TickSize { get; init; }

    /// <summary>
    /// Number of buy-side ticks (market buys)
    /// </summary>
    public int BuyTicks { get; init; }

    /// <summary>
    /// Number of sell-side ticks (market sells)
    /// </summary>
    public int SellTicks { get; init; }

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
    /// Delta between buy and sell volume (positive = bullish)
    /// </summary>
    public decimal VolumeDelta => BuyVolume - SellVolume;

    /// <summary>
    /// Buy/Sell ratio (values > 1 indicate bullish pressure)
    /// </summary>
    public decimal BuySellRatio =>
        SellVolume > 0 ? BuyVolume / SellVolume : BuyVolume > 0 ? decimal.MaxValue : 1;

    /// <summary>
    /// Price change percentage
    /// </summary>
    public decimal ChangePercent =>
        Open != 0 ? ((Close - Open) / Open) * 100 : 0;

    /// <summary>
    /// Price range (High - Low)
    /// </summary>
    public decimal Range => High - Low;

    /// <summary>
    /// Checks if the bar is bullish (close > open)
    /// </summary>
    public bool IsBullish => Close > Open;

    /// <summary>
    /// Checks if the bar is bearish (close < open)
    /// </summary>
    public bool IsBearish => Close < Open;
}
