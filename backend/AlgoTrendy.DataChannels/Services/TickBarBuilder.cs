using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Logging;

namespace AlgoTrendy.DataChannels.Services;

/// <summary>
/// Builds volume-based tick bars from individual tick data
/// Each bar contains exactly N ticks (e.g., 100, 500, 1000, 2000)
/// </summary>
public class TickBarBuilder
{
    private readonly int _tickSize;
    private readonly string _symbol;
    private readonly string _source;
    private readonly ILogger<TickBarBuilder>? _logger;

    private decimal _open;
    private decimal _high;
    private decimal _low;
    private decimal _close;
    private decimal _volume;
    private decimal _quoteVolume;
    private int _buyTicks;
    private int _sellTicks;
    private decimal _buyVolume;
    private decimal _sellVolume;
    private int _currentTickCount;
    private DateTime _firstTickTimestamp;
    private DateTime _lastTickTimestamp;

    /// <summary>
    /// Number of ticks required to complete each bar
    /// </summary>
    public int TickSize => _tickSize;

    /// <summary>
    /// Trading symbol this builder is aggregating
    /// </summary>
    public string Symbol => _symbol;

    /// <summary>
    /// Current number of ticks in the incomplete bar
    /// </summary>
    public int CurrentTickCount => _currentTickCount;

    /// <summary>
    /// Indicates if the builder has started collecting ticks
    /// </summary>
    public bool HasStarted => _currentTickCount > 0;

    public TickBarBuilder(
        string symbol,
        int tickSize,
        string source,
        ILogger<TickBarBuilder>? logger = null)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol cannot be empty", nameof(symbol));

        if (tickSize <= 0)
            throw new ArgumentException("Tick size must be greater than 0", nameof(tickSize));

        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source cannot be empty", nameof(source));

        _symbol = symbol;
        _tickSize = tickSize;
        _source = source;
        _logger = logger;

        Reset();
    }

    /// <summary>
    /// Adds a tick to the current bar
    /// Returns completed TickBar if tick size threshold is reached, null otherwise
    /// </summary>
    public TickBar? AddTick(TickData tick)
    {
        if (tick.Symbol != _symbol)
        {
            _logger?.LogWarning(
                "Tick symbol {TickSymbol} does not match builder symbol {BuilderSymbol}",
                tick.Symbol, _symbol);
            return null;
        }

        // First tick in the bar
        if (_currentTickCount == 0)
        {
            _open = tick.Price;
            _high = tick.Price;
            _low = tick.Price;
            _firstTickTimestamp = tick.Timestamp;
        }
        else
        {
            // Update high/low
            if (tick.Price > _high) _high = tick.Price;
            if (tick.Price < _low) _low = tick.Price;
        }

        // Always update close and timestamp
        _close = tick.Price;
        _lastTickTimestamp = tick.Timestamp;

        // Accumulate volume
        _volume += tick.Quantity;
        _quoteVolume += tick.QuoteVolume;

        // Track buy/sell pressure
        if (tick.IsMarketBuy)
        {
            _buyTicks++;
            _buyVolume += tick.Quantity;
        }
        else
        {
            _sellTicks++;
            _sellVolume += tick.Quantity;
        }

        _currentTickCount++;

        // Check if bar is complete
        if (_currentTickCount >= _tickSize)
        {
            var completedBar = BuildBar();
            Reset();
            return completedBar;
        }

        return null;
    }

    /// <summary>
    /// Forces completion of the current bar even if tick size hasn't been reached
    /// Useful for end-of-day processing or when switching symbols
    /// Returns null if no ticks have been collected
    /// </summary>
    public TickBar? ForceComplete()
    {
        if (_currentTickCount == 0)
            return null;

        var completedBar = BuildBar();
        Reset();
        return completedBar;
    }

    /// <summary>
    /// Builds a TickBar from the current accumulated data
    /// </summary>
    private TickBar BuildBar()
    {
        return new TickBar
        {
            Symbol = _symbol,
            Timestamp = _lastTickTimestamp,
            Open = _open,
            High = _high,
            Low = _low,
            Close = _close,
            Volume = _volume,
            QuoteVolume = _quoteVolume,
            TickSize = _tickSize,
            BuyTicks = _buyTicks,
            SellTicks = _sellTicks,
            BuyVolume = _buyVolume,
            SellVolume = _sellVolume,
            Source = _source
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
        _buyTicks = 0;
        _sellTicks = 0;
        _buyVolume = 0;
        _sellVolume = 0;
        _currentTickCount = 0;
        _firstTickTimestamp = default;
        _lastTickTimestamp = default;
    }

    /// <summary>
    /// Gets the current progress of the bar (0.0 to 1.0)
    /// </summary>
    public double GetProgress()
    {
        return _currentTickCount / (double)_tickSize;
    }

    /// <summary>
    /// Gets statistics about the current incomplete bar
    /// </summary>
    public (int tickCount, decimal volume, decimal deltaVolume) GetCurrentStats()
    {
        return (_currentTickCount, _volume, _buyVolume - _sellVolume);
    }
}
