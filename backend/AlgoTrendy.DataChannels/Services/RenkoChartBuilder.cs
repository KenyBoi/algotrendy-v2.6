using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Logging;

namespace AlgoTrendy.DataChannels.Services;

/// <summary>
/// Builds Renko chart bricks from price data (OHLCV or ticks)
/// Supports fixed brick size, ATR-based sizing, and percentage-based sizing
/// </summary>
public class RenkoChartBuilder
{
    private readonly string _symbol;
    private readonly decimal _brickSize;
    private readonly string _source;
    private readonly string _sizingMethod;
    private readonly ILogger<RenkoChartBuilder>? _logger;

    private decimal? _currentBrickOpen;
    private decimal _highSinceLastBrick;
    private decimal _lowSinceLastBrick;
    private decimal _accumulatedVolume;
    private decimal _accumulatedQuoteVolume;
    private int _sourceDataPoints;
    private DateTime _lastTimestamp;
    private bool _lastBrickWasUp;

    /// <summary>
    /// Trading symbol this builder is processing
    /// </summary>
    public string Symbol => _symbol;

    /// <summary>
    /// Size of each brick in price units
    /// </summary>
    public decimal BrickSize => _brickSize;

    /// <summary>
    /// Brick sizing method (Fixed, ATR, Percentage)
    /// </summary>
    public string SizingMethod => _sizingMethod;

    public RenkoChartBuilder(
        string symbol,
        decimal brickSize,
        string source,
        string sizingMethod = "Fixed",
        ILogger<RenkoChartBuilder>? logger = null)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol cannot be empty", nameof(symbol));

        if (brickSize <= 0)
            throw new ArgumentException("Brick size must be greater than 0", nameof(brickSize));

        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source cannot be empty", nameof(source));

        _symbol = symbol;
        _brickSize = brickSize;
        _source = source;
        _sizingMethod = sizingMethod;
        _logger = logger;

        Reset();
    }

    /// <summary>
    /// Creates a Renko builder with ATR-based brick sizing
    /// </summary>
    public static RenkoChartBuilder CreateATR(
        string symbol,
        IEnumerable<MarketData> recentData,
        string source,
        int atrPeriod = 13,
        ILogger<RenkoChartBuilder>? logger = null)
    {
        var atr = CalculateATR(recentData, atrPeriod);

        if (atr <= 0)
            throw new InvalidOperationException(
                $"Invalid ATR value {atr} calculated for {symbol}. Need more historical data.");

        logger?.LogInformation(
            "Created ATR-based Renko builder for {Symbol} with brick size {BrickSize:F2} (ATR{Period})",
            symbol, atr, atrPeriod);

        return new RenkoChartBuilder(symbol, atr, source, $"ATR({atrPeriod})", logger);
    }

    /// <summary>
    /// Creates a Renko builder with percentage-based brick sizing
    /// </summary>
    public static RenkoChartBuilder CreatePercentage(
        string symbol,
        decimal currentPrice,
        decimal percentageSize,
        string source,
        ILogger<RenkoChartBuilder>? logger = null)
    {
        if (percentageSize <= 0 || percentageSize > 100)
            throw new ArgumentException("Percentage must be between 0 and 100", nameof(percentageSize));

        var brickSize = currentPrice * (percentageSize / 100);

        logger?.LogInformation(
            "Created percentage-based Renko builder for {Symbol} with {Percentage}% brick size = {BrickSize:F2}",
            symbol, percentageSize, brickSize);

        return new RenkoChartBuilder(symbol, brickSize, source, $"Percentage({percentageSize}%)", logger);
    }

    /// <summary>
    /// Processes a price update (from OHLCV candle or tick)
    /// Returns list of completed bricks (can be multiple if price moved significantly)
    /// </summary>
    public List<RenkoBrick> ProcessPrice(decimal price, decimal volume, decimal quoteVolume, DateTime timestamp)
    {
        var completedBricks = new List<RenkoBrick>();

        // Initialize if this is the first price
        if (_currentBrickOpen == null)
        {
            _currentBrickOpen = RoundToNearestBrick(price);
            _highSinceLastBrick = price;
            _lowSinceLastBrick = price;
            _lastTimestamp = timestamp;
            return completedBricks;
        }

        // Update high/low tracking
        if (price > _highSinceLastBrick) _highSinceLastBrick = price;
        if (price < _lowSinceLastBrick) _lowSinceLastBrick = price;

        // Accumulate volume
        _accumulatedVolume += volume;
        _accumulatedQuoteVolume += quoteVolume;
        _sourceDataPoints++;
        _lastTimestamp = timestamp;

        // Check for brick formation
        var currentOpen = _currentBrickOpen.Value;

        // Check for upward bricks
        while (_highSinceLastBrick >= currentOpen + _brickSize)
        {
            var brick = CreateBrick(
                open: currentOpen,
                close: currentOpen + _brickSize,
                isUp: true,
                isReversal: !_lastBrickWasUp && _sourceDataPoints > 1);

            completedBricks.Add(brick);

            currentOpen += _brickSize;
            _currentBrickOpen = currentOpen;
            _lastBrickWasUp = true;
            ResetAccumulators();
        }

        // Check for downward bricks
        while (_lowSinceLastBrick <= currentOpen - _brickSize)
        {
            var brick = CreateBrick(
                open: currentOpen,
                close: currentOpen - _brickSize,
                isUp: false,
                isReversal: _lastBrickWasUp && _sourceDataPoints > 1);

            completedBricks.Add(brick);

            currentOpen -= _brickSize;
            _currentBrickOpen = currentOpen;
            _lastBrickWasUp = false;
            ResetAccumulators();
        }

        return completedBricks;
    }

    /// <summary>
    /// Processes OHLCV candle data
    /// </summary>
    public List<RenkoBrick> ProcessCandle(MarketData candle)
    {
        var allBricks = new List<RenkoBrick>();

        // Process in sequence: Open -> High -> Low -> Close
        // This simulates realistic price movement within the candle
        allBricks.AddRange(ProcessPrice(candle.Open, 0, 0, candle.Timestamp));
        allBricks.AddRange(ProcessPrice(candle.High, candle.Volume / 3, candle.QuoteVolume ?? 0 / 3, candle.Timestamp));
        allBricks.AddRange(ProcessPrice(candle.Low, candle.Volume / 3, candle.QuoteVolume ?? 0 / 3, candle.Timestamp));
        allBricks.AddRange(ProcessPrice(candle.Close, candle.Volume / 3, candle.QuoteVolume ?? 0 / 3, candle.Timestamp));

        return allBricks;
    }

    /// <summary>
    /// Calculates ATR (Average True Range) from historical data
    /// </summary>
    public static decimal CalculateATR(IEnumerable<MarketData> data, int period = 13)
    {
        var dataList = data.ToList();

        if (dataList.Count < period + 1)
            throw new ArgumentException($"Need at least {period + 1} candles to calculate ATR({period})");

        var trueRanges = new List<decimal>();

        for (int i = 1; i < dataList.Count; i++)
        {
            var current = dataList[i];
            var previous = dataList[i - 1];

            var tr = Math.Max(
                current.High - current.Low,
                Math.Max(
                    Math.Abs(current.High - previous.Close),
                    Math.Abs(current.Low - previous.Close)
                )
            );

            trueRanges.Add(tr);
        }

        // Take the last 'period' TRs and calculate average
        var atr = trueRanges.TakeLast(period).Average();
        return atr;
    }

    /// <summary>
    /// Creates a completed Renko brick
    /// </summary>
    private RenkoBrick CreateBrick(decimal open, decimal close, bool isUp, bool isReversal)
    {
        return new RenkoBrick
        {
            Symbol = _symbol,
            Timestamp = _lastTimestamp,
            Open = open,
            Close = close,
            High = isUp ? close : open,
            Low = isUp ? open : close,
            BrickSize = _brickSize,
            IsUpBrick = isUp,
            Volume = _accumulatedVolume,
            QuoteVolume = _accumulatedQuoteVolume,
            SourceDataPoints = _sourceDataPoints,
            Source = _source,
            IsReversal = isReversal,
            SizingMethod = _sizingMethod
        };
    }

    /// <summary>
    /// Rounds a price to the nearest brick boundary
    /// </summary>
    private decimal RoundToNearestBrick(decimal price)
    {
        return Math.Round(price / _brickSize) * _brickSize;
    }

    /// <summary>
    /// Resets volume accumulators after brick formation
    /// </summary>
    private void ResetAccumulators()
    {
        _accumulatedVolume = 0;
        _accumulatedQuoteVolume = 0;
        _sourceDataPoints = 0;
    }

    /// <summary>
    /// Resets the builder completely
    /// </summary>
    private void Reset()
    {
        _currentBrickOpen = null;
        _highSinceLastBrick = 0;
        _lowSinceLastBrick = 0;
        _accumulatedVolume = 0;
        _accumulatedQuoteVolume = 0;
        _sourceDataPoints = 0;
        _lastTimestamp = default;
        _lastBrickWasUp = true;
    }

    /// <summary>
    /// Gets the current state of the builder
    /// </summary>
    public (decimal? currentOpen, decimal high, decimal low, int dataPoints) GetCurrentState()
    {
        return (_currentBrickOpen, _highSinceLastBrick, _lowSinceLastBrick, _sourceDataPoints);
    }
}
