namespace AlgoTrendy.Core.Models;

/// <summary>
/// Cryptocurrency candlestick (OHLCV) data from Finnhub
/// </summary>
public class CryptoCandle
{
    /// <summary>
    /// Trading symbol (e.g., "BINANCE:BTCUSDT")
    /// </summary>
    public required string Symbol { get; set; }

    /// <summary>
    /// Timestamp (Unix seconds)
    /// </summary>
    public long Timestamp { get; set; }

    /// <summary>
    /// Open price
    /// </summary>
    public decimal Open { get; set; }

    /// <summary>
    /// High price
    /// </summary>
    public decimal High { get; set; }

    /// <summary>
    /// Low price
    /// </summary>
    public decimal Low { get; set; }

    /// <summary>
    /// Close price
    /// </summary>
    public decimal Close { get; set; }

    /// <summary>
    /// Trading volume
    /// </summary>
    public decimal Volume { get; set; }

    /// <summary>
    /// Candle resolution (e.g., "1", "5", "15", "60", "D")
    /// </summary>
    public string Resolution { get; set; } = "1";

    /// <summary>
    /// DateTime representation of Timestamp (UTC)
    /// </summary>
    public DateTime TimestampUtc => DateTimeOffset.FromUnixTimeSeconds(Timestamp).UtcDateTime;
}

/// <summary>
/// Cryptocurrency symbol information
/// </summary>
public class CryptoSymbol
{
    /// <summary>
    /// Symbol description (e.g., "Binance BTCUSDT")
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Display symbol (e.g., "BINANCE:BTCUSDT")
    /// </summary>
    public required string DisplaySymbol { get; set; }

    /// <summary>
    /// Trading symbol (e.g., "BINANCE:BTCUSDT")
    /// </summary>
    public required string Symbol { get; set; }
}

/// <summary>
/// Cryptocurrency exchange information
/// </summary>
public class CryptoExchange
{
    /// <summary>
    /// Exchange code (e.g., "binance", "coinbase", "kraken")
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// Exchange name (e.g., "Binance")
    /// </summary>
    public required string Name { get; set; }
}
