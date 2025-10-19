namespace AlgoTrendy.Core.Models;

/// <summary>
/// Represents OHLCV market data for a trading symbol
/// </summary>
public class MarketData
{
    /// <summary>
    /// Trading symbol (e.g., "BTCUSDT")
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Timestamp of the candle (bucket) in UTC
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Opening price
    /// </summary>
    public required decimal Open { get; init; }

    /// <summary>
    /// Highest price during the period
    /// </summary>
    public required decimal High { get; init; }

    /// <summary>
    /// Lowest price during the period
    /// </summary>
    public required decimal Low { get; init; }

    /// <summary>
    /// Closing price
    /// </summary>
    public required decimal Close { get; init; }

    /// <summary>
    /// Volume traded in base currency
    /// </summary>
    public required decimal Volume { get; init; }

    /// <summary>
    /// Volume traded in quote currency
    /// </summary>
    public decimal? QuoteVolume { get; init; }

    /// <summary>
    /// Number of trades during the period
    /// </summary>
    public long? TradesCount { get; init; }

    /// <summary>
    /// Data source (binance, okx, coinbase, kraken, bybit)
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// Additional metadata in JSON format
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Calculates the price change percentage
    /// </summary>
    public decimal ChangePercent =>
        Open != 0 ? ((Close - Open) / Open) * 100 : 0;

    /// <summary>
    /// Calculates the price range (High - Low)
    /// </summary>
    public decimal Range => High - Low;

    /// <summary>
    /// Checks if the candle is bullish (close &gt; open)
    /// </summary>
    public bool IsBullish => Close > Open;

    /// <summary>
    /// Checks if the candle is bearish (close &lt; open)
    /// </summary>
    public bool IsBearish => Close < Open;
}
