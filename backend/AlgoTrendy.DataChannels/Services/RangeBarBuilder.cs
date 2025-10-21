using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Logging;

namespace AlgoTrendy.DataChannels.Services;

/// <summary>
/// Builds range bars where each bar has a fixed high-low range
/// Bars complete when the price range reaches the specified threshold
/// </summary>
public class RangeBarBuilder
{
    private readonly string _symbol;
    private readonly decimal _rangeThreshold;
    private readonly string _source;
    private readonly ILogger<RangeBarBuilder>? _logger;

    private decimal _open;
    private decimal _high;
    private decimal _low;
    private decimal _close;
    private decimal _volume;
    private decimal _quoteVolume;
    private int _tickCount;
    private decimal _buyVolume;
    private decimal _sellVolume;
    private DateTime _firstTickTimestamp;
    private DateTime _lastTickTimestamp;
    private bool _hasStarted;

    /// <summary>
    /// Trading symbol this builder is processing
    /// </summary>
    public string Symbol => _symbol;

    /// <summary>
    /// Fixed range threshold that triggers bar completion
    /// </summary>
    public decimal RangeThreshold => _rangeThreshold;

    /// <summary>
    /// Current range of the incomplete bar
    /// </summary>
    public decimal CurrentRange => _hasStarted ? _high - _low : 0;

    /// <summary>
    /// Indicates if the builder has started collecting data
    /// </summary>
    public bool HasStarted => _hasStarted;

    public RangeBarBuilder(
        string symbol,
        decimal rangeThreshold,
        string source,
        ILogger<RangeBarBuilder>? logger = null)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol cannot be empty", nameof(symbol));

        if (rangeThreshold <= 0)
            throw new ArgumentException("Range threshold must be greater than 0", nameof(rangeThreshold));

        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source cannot be empty", nameof(source));

        _symbol = symbol;
        _rangeThreshold = rangeThreshold;
        _source = source;
        _logger = logger;

        Reset();
    }

    /// <summary>
    /// Creates a range bar builder with ATR-based range threshold
    /// </summary>
    public static RangeBarBuilder CreateATR(
        string symbol,
        IEnumerable<MarketData> recentData,
        string source,
        int atrPeriod = 13,
        decimal atrMultiplier = 1.0m,
        ILogger<RangeBarBuilder>? logger = null)
    {
        var atr = RenkoChartBuilder.CalculateATR(recentData, atrPeriod);
        var rangeThreshold = atr * atrMultiplier;

        if (rangeThreshold <= 0)
            throw new InvalidOperationException(
                $"Invalid range threshold {rangeThreshold} calculated for {symbol}");

        logger?.LogInformation(
            "Created ATR-based range bar builder for {Symbol} with threshold {Threshold:F2} (ATR{Period} × {Multiplier})",
            symbol, rangeThreshold, atrPeriod, atrMultiplier);

        return new RangeBarBuilder(symbol, rangeThreshold, source, logger);
    }

    /// <summary>
    /// Adds a tick to the current range bar
    /// Returns completed RangeBar if range threshold is reached, null otherwise
    /// </summary>
    public RangeBar? AddTick(TickData tick)
    {
        if (tick.Symbol != _symbol)
        {
            _logger?.LogWarning(
                "Tick symbol {TickSymbol} does not match builder symbol {BuilderSymbol}",
                tick.Symbol, _symbol);
            return null;
        }

        return ProcessPrice(
            tick.Price,
            tick.Quantity,
            tick.QuoteVolume,
            tick.Timestamp,
            tick.IsMarketBuy);
    }

    /// <summary>
    /// Processes a price update (from tick or candle)
    /// Returns completed RangeBar if threshold is reached, null otherwise
    /// </summary>
    public RangeBar? ProcessPrice(
        decimal price,
        decimal volume,
        decimal quoteVolume,
        DateTime timestamp,
        bool? isBuy = null)
    {
        // First price - initialize the bar
        if (!_hasStarted)
        {
            _open = price;
            _high = price;
            _low = price;
            _close = price;
            _firstTickTimestamp = timestamp;
            _hasStarted = true;
        }
        else
        {
            // Update high/low
            if (price > _high) _high = price;
            if (price < _low) _low = price;
        }

        // Always update close and timestamp
        _close = price;
        _lastTickTimestamp = timestamp;

        // Accumulate volume
        _volume += volume;
        _quoteVolume += quoteVolume;
        _tickCount++;

        // Track buy/sell volume if side is known
        if (isBuy.HasValue)
        {
            if (isBuy.Value)
                _buyVolume += volume;
            else
                _sellVolume += volume;
        }

        // Check if range threshold is reached
        var currentRange = _high - _low;
        if (currentRange >= _rangeThreshold)
        {
            var completedBar = BuildBar();
            Reset();
            return completedBar;
        }

        return null;
    }

    /// <summary>
    /// Processes OHLCV candle data
    /// May return a completed bar if range threshold is reached
    /// </summary>
    public RangeBar? ProcessCandle(MarketData candle)
    {
        // Process the candle prices in sequence
        // Note: This is a simplified approach - in reality, we don't know the exact order
        RangeBar? result = null;

        result ??= ProcessPrice(candle.Open, 0, 0, candle.Timestamp);
        result ??= ProcessPrice(candle.High, candle.Volume / 3, (candle.QuoteVolume ?? 0) / 3, candle.Timestamp);
        result ??= ProcessPrice(candle.Low, candle.Volume / 3, (candle.QuoteVolume ?? 0) / 3, candle.Timestamp);
        result ??= ProcessPrice(candle.Close, candle.Volume / 3, (candle.QuoteVolume ?? 0) / 3, candle.Timestamp);

        return result;
    }

    /// <summary>
    /// Forces completion of the current bar even if range threshold hasn't been reached
    /// Useful for end-of-day processing or when switching symbols
    /// Returns null if no data has been collected
    /// </summary>
    public RangeBar? ForceComplete()
    {
        if (!_hasStarted)
            return null;

        var completedBar = BuildBar();
        Reset();
        return completedBar;
    }

    /// <summary>
    /// Builds a RangeBar from the current accumulated data
    /// </summary>
    private RangeBar BuildBar()
    {
        var duration = _lastTickTimestamp - _firstTickTimestamp;

        return new RangeBar
        {
            Symbol = _symbol,
            Timestamp = _lastTickTimestamp,
            Open = _open,
            High = _high,
            Low = _low,
            Close = _close,
            RangeThreshold = _rangeThreshold,
            Volume = _volume,
            QuoteVolume = _quoteVolume,
            TickCount = _tickCount,
            BuyVolume = _buyVolume,
            SellVolume = _sellVolume,
            Source = _source,
            Duration = duration
        };
    }

    /// <summary>
    /// Resets the builder to start a new bar
    /// </summary>
    private void Reset()
    {
        _open = 0;
        _high = 0;
        _low = 0;
        _close = 0;
        _volume = 0;
        _quoteVolume = 0;
        _tickCount = 0;
        _buyVolume = 0;
        _sellVolume = 0;
        _firstTickTimestamp = default;
        _lastTickTimestamp = default;
        _hasStarted = false;
    }

    /// <summary>
    /// Gets the current progress toward completing a bar (0.0 to 1.0)
    /// </summary>
    public double GetProgress()
    {
        if (!_hasStarted || _rangeThreshold == 0)
            return 0;

        var progress = (double)(CurrentRange / _rangeThreshold);
        return Math.Min(progress, 1.0);
    }

    /// <summary>
    /// Gets statistics about the current incomplete bar
    /// </summary>
    public (decimal range, int tickCount, decimal volume, decimal deltaVolume) GetCurrentStats()
    {
        return (CurrentRange, _tickCount, _volume, _buyVolume - _sellVolume);
    }
}
