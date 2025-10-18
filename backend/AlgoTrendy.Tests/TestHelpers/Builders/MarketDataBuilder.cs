using AlgoTrendy.Core.Models;

namespace AlgoTrendy.Tests.TestHelpers.Builders;

/// <summary>
/// Builder for creating test MarketData instances with fluent API
/// </summary>
public class MarketDataBuilder
{
    private string _symbol = "BTCUSDT";
    private DateTime _timestamp = DateTime.UtcNow;
    private decimal _open = 50000m;
    private decimal _high = 51000m;
    private decimal _low = 49000m;
    private decimal _close = 50500m;
    private decimal _volume = 100m;
    private decimal? _quoteVolume = 5000000m;
    private long? _tradesCount = 1000;
    private string _source = "binance";
    private string? _metadata;

    public MarketDataBuilder WithSymbol(string symbol)
    {
        _symbol = symbol;
        return this;
    }

    public MarketDataBuilder WithTimestamp(DateTime timestamp)
    {
        _timestamp = timestamp;
        return this;
    }

    public MarketDataBuilder WithOpen(decimal open)
    {
        _open = open;
        return this;
    }

    public MarketDataBuilder WithHigh(decimal high)
    {
        _high = high;
        return this;
    }

    public MarketDataBuilder WithLow(decimal low)
    {
        _low = low;
        return this;
    }

    public MarketDataBuilder WithClose(decimal close)
    {
        _close = close;
        return this;
    }

    public MarketDataBuilder WithVolume(decimal volume)
    {
        _volume = volume;
        return this;
    }

    public MarketDataBuilder WithQuoteVolume(decimal? quoteVolume)
    {
        _quoteVolume = quoteVolume;
        return this;
    }

    public MarketDataBuilder WithTradesCount(long? tradesCount)
    {
        _tradesCount = tradesCount;
        return this;
    }

    public MarketDataBuilder WithSource(string source)
    {
        _source = source;
        return this;
    }

    public MarketDataBuilder WithMetadata(string? metadata)
    {
        _metadata = metadata;
        return this;
    }

    /// <summary>
    /// Creates a bullish candle (close > open)
    /// </summary>
    public MarketDataBuilder Bullish()
    {
        _open = 50000m;
        _high = 51000m;
        _low = 49500m;
        _close = 50800m;
        return this;
    }

    /// <summary>
    /// Creates a bearish candle (close < open)
    /// </summary>
    public MarketDataBuilder Bearish()
    {
        _open = 50000m;
        _high = 50500m;
        _low = 49000m;
        _close = 49200m;
        return this;
    }

    /// <summary>
    /// Creates a neutral/doji candle (close = open)
    /// </summary>
    public MarketDataBuilder Neutral()
    {
        _open = 50000m;
        _high = 50500m;
        _low = 49500m;
        _close = 50000m;
        return this;
    }

    public MarketData Build()
    {
        return new MarketData
        {
            Symbol = _symbol,
            Timestamp = _timestamp,
            Open = _open,
            High = _high,
            Low = _low,
            Close = _close,
            Volume = _volume,
            QuoteVolume = _quoteVolume,
            TradesCount = _tradesCount,
            Source = _source,
            Metadata = _metadata
        };
    }
}
