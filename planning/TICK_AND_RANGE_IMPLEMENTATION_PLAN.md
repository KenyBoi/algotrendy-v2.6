# Tick Data & Range Charts Implementation Plan

**Date**: October 21, 2025
**Purpose**: Add tick data collection + range-based charts (Renko & Range bars) to AlgoTrendy
**Status**: Ready for implementation

---

## Executive Summary

We're implementing **dual chart infrastructure**:

1. **Tick Data System**: Collect, store, and generate tick-based bars (500, 1000, 2000 ticks)
2. **Range Chart System**: Build Renko, Range bars, and other price-action charts

**Why Both?**
- ✅ **Tick charts**: React fastest to market moves, volatility-adjusted
- ✅ **Range charts**: Filter noise, find institutional levels, pure price action
- ✅ **Combined**: MEM learns when to use each based on market conditions

**Timeline**: 4-6 weeks to full production deployment

---

## Table of Contents

1. [Tick Data Architecture](#tick-data-architecture)
2. [Range Chart System](#range-chart-system)
3. [Storage Strategy](#storage-strategy)
4. [Implementation Roadmap](#implementation-roadmap)
5. [Code Implementation](#code-implementation)
6. [MEM Integration](#mem-integration)
7. [Testing & Validation](#testing--validation)

---

## Tick Data Architecture

### What is Tick Data?

**Tick** = Individual trade execution
```
Each tick contains:
- Price: $65,432.10
- Size: 0.05 BTC
- Timestamp: 2025-10-21 14:32:15.234
- Side: Buy or Sell
```

**Tick Bar** = Aggregated N ticks into one bar
```
500-tick bar = OHLCV data from 500 consecutive trades
1000-tick bar = OHLCV data from 1000 consecutive trades
```

### Data Flow

```
Exchange WebSocket → Tick Collector → Tick Bar Generator → Strategy Engine
                           ↓
                      QuestDB Storage
                           ↓
                    Historical Analysis
```

### Collection Infrastructure

```csharp
// File: AlgoTrendy.DataChannels/TickData/TickDataCollector.cs

namespace AlgoTrendy.DataChannels.TickData;

/// <summary>
/// Collects individual ticks from exchange WebSocket feeds
/// </summary>
public class TickDataCollector
{
    private readonly IQuestDBWriter _questDB;
    private readonly ILogger<TickDataCollector> _logger;
    private readonly Dictionary<string, TickBarBuilder> _tickBarBuilders;

    public TickDataCollector(
        IQuestDBWriter questDB,
        ILogger<TickDataCollector> logger)
    {
        _questDB = questDB;
        _logger = logger;
        _tickBarBuilders = new Dictionary<string, TickBarBuilder>();
    }

    /// <summary>
    /// Called for EVERY trade execution from exchange
    /// </summary>
    public async Task OnTradeAsync(string symbol, Trade trade)
    {
        // Store raw tick
        await _questDB.WriteTick(new TickData
        {
            Symbol = symbol,
            Price = trade.Price,
            Size = trade.Quantity,
            Timestamp = trade.Timestamp,
            Side = trade.IsBuyerMaker ? TradeSide.Sell : TradeSide.Buy,
            TradeId = trade.Id
        });

        // Build tick bars for all configured sizes
        var builders = GetOrCreateBuilders(symbol);

        foreach (var builder in builders.Values)
        {
            var completedBar = builder.AddTick(trade.Price, trade.Quantity);

            if (completedBar != null)
            {
                // New tick bar completed!
                await OnTickBarCompleted(symbol, builder.TickSize, completedBar);
            }
        }
    }

    private async Task OnTickBarCompleted(string symbol, int tickSize, TickBar bar)
    {
        // Store tick bar
        await _questDB.WriteTickBar(symbol, tickSize, bar);

        // Notify strategy engine
        await _eventBus.PublishAsync(new TickBarCompletedEvent
        {
            Symbol = symbol,
            TickSize = tickSize,
            Bar = bar,
            Timestamp = bar.CloseTime
        });

        _logger.LogDebug(
            "Tick bar completed: {Symbol} {TickSize}-tick O:{Open} H:{High} L:{Low} C:{Close} V:{Volume}",
            symbol, tickSize, bar.Open, bar.High, bar.Low, bar.Close, bar.Volume);
    }

    private Dictionary<int, TickBarBuilder> GetOrCreateBuilders(string symbol)
    {
        if (!_tickBarBuilders.ContainsKey(symbol))
        {
            // Create builders for standard tick sizes
            _tickBarBuilders[symbol] = new Dictionary<int, TickBarBuilder>
            {
                { 100, new TickBarBuilder(100) },
                { 500, new TickBarBuilder(500) },
                { 1000, new TickBarBuilder(1000) },
                { 2000, new TickBarBuilder(2000) }
            };
        }

        return _tickBarBuilders[symbol];
    }
}
```

### Tick Bar Builder

```csharp
// File: AlgoTrendy.DataChannels/TickData/TickBarBuilder.cs

public class TickBarBuilder
{
    private readonly int _tickSize;
    private int _currentTickCount;
    private decimal _open, _high, _low, _close;
    private decimal _volume;
    private DateTime _openTime;

    public TickBarBuilder(int tickSize)
    {
        _tickSize = tickSize;
        Reset();
    }

    public int TickSize => _tickSize;

    /// <summary>
    /// Add a tick to the current bar
    /// Returns completed bar if tick count reached, null otherwise
    /// </summary>
    public TickBar? AddTick(decimal price, decimal size)
    {
        if (_currentTickCount == 0)
        {
            // First tick of new bar
            _open = price;
            _high = price;
            _low = price;
            _openTime = DateTime.UtcNow;
        }
        else
        {
            // Update OHLC
            _high = Math.Max(_high, price);
            _low = Math.Min(_low, price);
        }

        _close = price;
        _volume += size;
        _currentTickCount++;

        // Check if bar is complete
        if (_currentTickCount >= _tickSize)
        {
            var completedBar = new TickBar
            {
                Open = _open,
                High = _high,
                Low = _low,
                Close = _close,
                Volume = _volume,
                TickCount = _currentTickCount,
                OpenTime = _openTime,
                CloseTime = DateTime.UtcNow
            };

            Reset();
            return completedBar;
        }

        return null;
    }

    private void Reset()
    {
        _currentTickCount = 0;
        _open = _high = _low = _close = 0;
        _volume = 0;
    }
}

/// <summary>
/// Tick bar data structure
/// </summary>
public class TickBar
{
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    public int TickCount { get; set; }
    public DateTime OpenTime { get; set; }
    public DateTime CloseTime { get; set; }

    // Duration varies based on market activity
    public TimeSpan Duration => CloseTime - OpenTime;
}
```

### WebSocket Integration

```csharp
// File: AlgoTrendy.DataChannels/Binance/BinanceTickFeed.cs

public class BinanceTickFeed : ITickDataFeed
{
    private readonly BinanceSocketClient _socketClient;
    private readonly TickDataCollector _tickCollector;
    private readonly List<UpdateSubscription> _subscriptions;

    public async Task SubscribeToTicks(string symbol)
    {
        // Subscribe to Binance trade stream
        var subscription = await _socketClient.SpotApi.ExchangeData
            .SubscribeToTradeUpdatesAsync(symbol, async trade =>
            {
                // Every trade = 1 tick
                await _tickCollector.OnTradeAsync(symbol, new Trade
                {
                    Id = trade.Id.ToString(),
                    Price = trade.Price,
                    Quantity = trade.Quantity,
                    Timestamp = trade.TradeTime,
                    IsBuyerMaker = trade.BuyerIsMaker
                });
            });

        _subscriptions.Add(subscription.Data);

        _logger.LogInformation("Subscribed to tick data for {Symbol}", symbol);
    }

    public async Task UnsubscribeAll()
    {
        foreach (var sub in _subscriptions)
        {
            await sub.CloseAsync();
        }
        _subscriptions.Clear();
    }
}
```

---

## Range Chart System

### Chart Types

We'll implement **THREE** range-based chart types:

1. **Renko Charts**: Fixed price boxes (bricks)
2. **Range Bars**: Fixed high-low range
3. **Point & Figure (P&F)**: Column-based price movement

### 1. Renko Charts

**Concept**: New brick only forms when price moves by brick size

```
Brick Size = $500

Price moves from $65,000 to $65,500 → New bullish brick
Price moves from $65,500 to $65,000 → New bearish brick
Price at $65,250 → No new brick (insufficient movement)
```

**Implementation**:

```csharp
// File: AlgoTrendy.DataChannels/RangeCharts/RenkoChartBuilder.cs

public class RenkoChartBuilder
{
    private readonly decimal _brickSize;
    private decimal _currentBrickOpen;
    private decimal _currentBrickHigh;
    private decimal _currentBrickLow;
    private BrickDirection _currentDirection;
    private List<RenkoBrick> _bricks;

    public RenkoChartBuilder(decimal brickSize, decimal initialPrice)
    {
        _brickSize = brickSize;
        _currentBrickOpen = RoundToNearestBrick(initialPrice);
        _currentBrickHigh = _currentBrickOpen;
        _currentBrickLow = _currentBrickOpen;
        _bricks = new List<RenkoBrick>();
    }

    /// <summary>
    /// Create Renko builder with ATR-based brick size
    /// </summary>
    public static RenkoChartBuilder CreateATR(IEnumerable<OHLCV> historicalData, int atrPeriod = 13)
    {
        var atr = CalculateATR(historicalData, atrPeriod);
        var lastPrice = historicalData.Last().Close;

        return new RenkoChartBuilder(atr, lastPrice);
    }

    /// <summary>
    /// Process new price tick/candle
    /// Returns list of newly formed bricks (can be multiple!)
    /// </summary>
    public List<RenkoBrick> OnPrice(decimal price, DateTime timestamp)
    {
        var newBricks = new List<RenkoBrick>();

        // Update current brick extremes
        _currentBrickHigh = Math.Max(_currentBrickHigh, price);
        _currentBrickLow = Math.Min(_currentBrickLow, price);

        // Check for bullish bricks
        while (price >= _currentBrickOpen + _brickSize)
        {
            var brick = new RenkoBrick
            {
                Open = _currentBrickOpen,
                Close = _currentBrickOpen + _brickSize,
                High = _currentBrickOpen + _brickSize,
                Low = _currentBrickOpen,
                Direction = BrickDirection.Bullish,
                Timestamp = timestamp,
                BrickSize = _brickSize
            };

            newBricks.Add(brick);
            _bricks.Add(brick);

            _currentBrickOpen += _brickSize;
            _currentDirection = BrickDirection.Bullish;

            // Reset extremes for next brick
            _currentBrickHigh = _currentBrickOpen;
            _currentBrickLow = _currentBrickOpen;
        }

        // Check for bearish bricks
        while (price <= _currentBrickOpen - _brickSize)
        {
            var brick = new RenkoBrick
            {
                Open = _currentBrickOpen,
                Close = _currentBrickOpen - _brickSize,
                High = _currentBrickOpen,
                Low = _currentBrickOpen - _brickSize,
                Direction = BrickDirection.Bearish,
                Timestamp = timestamp,
                BrickSize = _brickSize
            };

            newBricks.Add(brick);
            _bricks.Add(brick);

            _currentBrickOpen -= _brickSize;
            _currentDirection = BrickDirection.Bearish;

            // Reset extremes
            _currentBrickHigh = _currentBrickOpen;
            _currentBrickLow = _currentBrickOpen;
        }

        return newBricks;
    }

    private decimal RoundToNearestBrick(decimal price)
    {
        return Math.Round(price / _brickSize) * _brickSize;
    }

    private static decimal CalculateATR(IEnumerable<OHLCV> data, int period)
    {
        var trueRanges = new List<decimal>();
        OHLCV? previous = null;

        foreach (var candle in data)
        {
            if (previous != null)
            {
                var tr = Math.Max(
                    candle.High - candle.Low,
                    Math.Max(
                        Math.Abs(candle.High - previous.Close),
                        Math.Abs(candle.Low - previous.Close)
                    )
                );
                trueRanges.Add(tr);
            }
            previous = candle;
        }

        return trueRanges.TakeLast(period).Average();
    }
}

public class RenkoBrick
{
    public decimal Open { get; set; }
    public decimal Close { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public BrickDirection Direction { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal BrickSize { get; set; }

    public bool IsBullish => Direction == BrickDirection.Bullish;
    public bool IsBearish => Direction == BrickDirection.Bearish;
}

public enum BrickDirection
{
    Bullish,
    Bearish
}
```

### 2. Range Bars

**Concept**: New bar forms when High - Low = fixed range

```
Range = $300

Price moves: $65,000 → $65,200 → $65,100 → $65,300
High-Low = $65,300 - $65,000 = $300 → Complete bar!
```

**Implementation**:

```csharp
// File: AlgoTrendy.DataChannels/RangeCharts/RangeBarBuilder.cs

public class RangeBarBuilder
{
    private readonly decimal _rangeSize;
    private decimal _open, _high, _low, _close;
    private decimal _volume;
    private DateTime _openTime;
    private bool _barInProgress;

    public RangeBarBuilder(decimal rangeSize)
    {
        _rangeSize = rangeSize;
        Reset();
    }

    /// <summary>
    /// Create range builder with ATR-based range
    /// </summary>
    public static RangeBarBuilder CreateATR(IEnumerable<OHLCV> data, int period = 13, decimal multiplier = 1.0m)
    {
        var atr = CalculateATR(data, period);
        return new RangeBarBuilder(atr * multiplier);
    }

    /// <summary>
    /// Process new tick/price update
    /// Returns completed bar if range reached, null otherwise
    /// </summary>
    public RangeBar? OnPrice(decimal price, decimal size, DateTime timestamp)
    {
        if (!_barInProgress)
        {
            // Start new bar
            _open = price;
            _high = price;
            _low = price;
            _openTime = timestamp;
            _barInProgress = true;
        }

        // Update OHLC
        _high = Math.Max(_high, price);
        _low = Math.Min(_low, price);
        _close = price;
        _volume += size;

        // Check if range met
        if (_high - _low >= _rangeSize)
        {
            var completedBar = new RangeBar
            {
                Open = _open,
                High = _high,
                Low = _low,
                Close = _close,
                Volume = _volume,
                Range = _high - _low,
                OpenTime = _openTime,
                CloseTime = timestamp
            };

            Reset();
            return completedBar;
        }

        return null;
    }

    private void Reset()
    {
        _barInProgress = false;
        _volume = 0;
    }
}

public class RangeBar
{
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    public decimal Range { get; set; }
    public DateTime OpenTime { get; set; }
    public DateTime CloseTime { get; set; }

    public TimeSpan Duration => CloseTime - OpenTime;
    public bool IsBullish => Close > Open;
}
```

### 3. Unified Range Chart Manager

```csharp
// File: AlgoTrendy.DataChannels/RangeCharts/RangeChartManager.cs

public class RangeChartManager
{
    private readonly IQuestDBWriter _questDB;
    private readonly IEventBus _eventBus;
    private readonly Dictionary<string, Dictionary<string, object>> _chartBuilders;

    public async Task RegisterRenkoChart(
        string symbol,
        string chartId,
        RenkoBrickSizeType sizeType,
        decimal? fixedSize = null,
        int? atrPeriod = null)
    {
        RenkoChartBuilder builder;

        switch (sizeType)
        {
            case RenkoBrickSizeType.Fixed:
                builder = new RenkoChartBuilder(fixedSize.Value, GetCurrentPrice(symbol));
                break;

            case RenkoBrickSizeType.ATR:
                var historicalData = await GetHistoricalData(symbol, 200);
                builder = RenkoChartBuilder.CreateATR(historicalData, atrPeriod ?? 13);
                break;

            case RenkoBrickSizeType.Percentage:
                var currentPrice = GetCurrentPrice(symbol);
                var brickSize = currentPrice * (fixedSize.Value / 100);
                builder = new RenkoChartBuilder(brickSize, currentPrice);
                break;

            default:
                throw new ArgumentException("Invalid brick size type");
        }

        RegisterBuilder(symbol, chartId, builder);
    }

    public async Task RegisterRangeChart(
        string symbol,
        string chartId,
        decimal rangeSize)
    {
        var builder = new RangeBarBuilder(rangeSize);
        RegisterBuilder(symbol, chartId, builder);
    }

    /// <summary>
    /// Feed price updates to all registered charts
    /// </summary>
    public async Task OnPriceUpdate(string symbol, decimal price, decimal size, DateTime timestamp)
    {
        if (!_chartBuilders.ContainsKey(symbol))
            return;

        foreach (var kvp in _chartBuilders[symbol])
        {
            var chartId = kvp.Key;
            var builder = kvp.Value;

            if (builder is RenkoChartBuilder renkoBuilder)
            {
                var newBricks = renkoBuilder.OnPrice(price, timestamp);

                foreach (var brick in newBricks)
                {
                    await _questDB.WriteRenkoBrick(symbol, chartId, brick);
                    await _eventBus.PublishAsync(new RenkoBrickCompletedEvent
                    {
                        Symbol = symbol,
                        ChartId = chartId,
                        Brick = brick
                    });
                }
            }
            else if (builder is RangeBarBuilder rangeBuilder)
            {
                var completedBar = rangeBuilder.OnPrice(price, size, timestamp);

                if (completedBar != null)
                {
                    await _questDB.WriteRangeBar(symbol, chartId, completedBar);
                    await _eventBus.PublishAsync(new RangeBarCompletedEvent
                    {
                        Symbol = symbol,
                        ChartId = chartId,
                        Bar = completedBar
                    });
                }
            }
        }
    }

    private void RegisterBuilder(string symbol, string chartId, object builder)
    {
        if (!_chartBuilders.ContainsKey(symbol))
            _chartBuilders[symbol] = new Dictionary<string, object>();

        _chartBuilders[symbol][chartId] = builder;
    }
}

public enum RenkoBrickSizeType
{
    Fixed,       // $500 bricks
    ATR,         // ATR(13) bricks
    Percentage   // 0.5% bricks
}
```

---

## Storage Strategy

### QuestDB Schema

```sql
-- Raw tick data
CREATE TABLE ticks (
    symbol SYMBOL,
    price DOUBLE,
    size DOUBLE,
    side SYMBOL,
    trade_id SYMBOL,
    timestamp TIMESTAMP
) TIMESTAMP(timestamp) PARTITION BY DAY;

-- Tick bars (multiple tick sizes per symbol)
CREATE TABLE tick_bars (
    symbol SYMBOL,
    tick_size INT,
    open DOUBLE,
    high DOUBLE,
    low DOUBLE,
    close DOUBLE,
    volume DOUBLE,
    tick_count INT,
    open_time TIMESTAMP,
    close_time TIMESTAMP
) TIMESTAMP(close_time) PARTITION BY DAY;

-- Renko bricks
CREATE TABLE renko_bricks (
    symbol SYMBOL,
    chart_id SYMBOL,
    open DOUBLE,
    close DOUBLE,
    high DOUBLE,
    low DOUBLE,
    direction SYMBOL,
    brick_size DOUBLE,
    timestamp TIMESTAMP
) TIMESTAMP(timestamp) PARTITION BY DAY;

-- Range bars
CREATE TABLE range_bars (
    symbol SYMBOL,
    chart_id SYMBOL,
    open DOUBLE,
    high DOUBLE,
    low DOUBLE,
    close DOUBLE,
    volume DOUBLE,
    range DOUBLE,
    open_time TIMESTAMP,
    close_time TIMESTAMP
) TIMESTAMP(close_time) PARTITION BY DAY;
```

### Storage Costs

**Tick Data** (most expensive):
```
BTC/USDT generates ~20,000 ticks/hour
= 480,000 ticks/day
= 14.4M ticks/month

Per tick: ~50 bytes
Monthly storage: ~720 MB/month per symbol

10 symbols = 7.2 GB/month
```

**Tick Bars** (efficient):
```
1000-tick bars for BTC = 480 bars/day
Per bar: ~80 bytes
Monthly: ~1.2 MB/month per symbol per tick size

Very affordable!
```

**Renko/Range** (most efficient):
```
~50-200 bricks/bars per day
Monthly: < 500 KB/month per symbol

Negligible!
```

### Data Retention Policy

```csharp
// File: AlgoTrendy.DataChannels/DataRetentionPolicy.cs

public class DataRetentionPolicy
{
    public static readonly Dictionary<DataType, TimeSpan> RetentionPeriods = new()
    {
        // Raw ticks: Keep 7 days (for recent analysis)
        { DataType.RawTicks, TimeSpan.FromDays(7) },

        // Tick bars: Keep 90 days (for backtesting)
        { DataType.TickBars, TimeSpan.FromDays(90) },

        // Renko/Range: Keep 1 year (lightweight)
        { DataType.RenkoBricks, TimeSpan.FromDays(365) },
        { DataType.RangeBars, TimeSpan.FromDays(365) },

        // Minute candles: Keep 2 years
        { DataType.MinuteCandles, TimeSpan.FromDays(730) }
    };

    public async Task CleanupOldData()
    {
        foreach (var kvp in RetentionPeriods)
        {
            var cutoffDate = DateTime.UtcNow - kvp.Value;

            await _questDB.ExecuteAsync($@"
                DELETE FROM {GetTableName(kvp.Key)}
                WHERE timestamp < '{cutoffDate:yyyy-MM-dd}'
            ");
        }
    }
}
```

---

## Implementation Roadmap

### Week 1: Tick Data Foundation

**Days 1-2**: Infrastructure
- [ ] Create TickDataCollector class
- [ ] Create TickBarBuilder class
- [ ] Set up QuestDB schemas
- [ ] Add configuration for tick sizes

**Days 3-4**: WebSocket Integration
- [ ] Implement BinanceTickFeed
- [ ] Implement BybitTickFeed
- [ ] Test tick collection (log to file first)
- [ ] Verify tick counts match exchange

**Day 5**: Storage & Testing
- [ ] Implement QuestDB tick storage
- [ ] Test write performance (can handle 1000 ticks/sec?)
- [ ] Create tick bar retrieval queries
- [ ] Unit tests for TickBarBuilder

**Deliverable**: Live tick data flowing into QuestDB

---

### Week 2: Tick Bar Generation

**Days 1-2**: Multi-Size Tick Bars
- [ ] Generate 100, 500, 1000, 2000-tick bars
- [ ] Store all tick sizes in QuestDB
- [ ] Event bus notifications for completed bars
- [ ] Performance optimization (concurrent builders)

**Days 3-4**: API Integration
- [ ] Add tick bar endpoints to API
  - `GET /api/market/tick-bars/{symbol}?tickSize=1000`
  - `GET /api/market/tick-data/{symbol}`
- [ ] WebSocket subscriptions for live tick bars
- [ ] Dashboard visualization (TradingView integration)

**Day 5**: Backtesting Support
- [ ] Historical tick bar retrieval
- [ ] Resample minute data to ticks (if needed)
- [ ] Export tick bars to CSV
- [ ] Documentation

**Deliverable**: Tick bars accessible via API and working in strategies

---

### Week 3: Renko Charts

**Days 1-2**: Renko Core
- [ ] Implement RenkoChartBuilder (fixed size)
- [ ] Implement ATR-based brick sizing
- [ ] Implement percentage-based brick sizing
- [ ] Unit tests for all brick calculations

**Days 3-4**: Renko Storage & API
- [ ] QuestDB renko_bricks table
- [ ] Store bricks as they form
- [ ] API endpoints:
  - `GET /api/market/renko/{symbol}?brickSize=500`
  - `GET /api/market/renko/{symbol}?method=atr&period=13`
- [ ] Historical brick retrieval

**Day 5**: Visualization & Testing
- [ ] TradingView Renko chart display
- [ ] Test on historical data
- [ ] Verify "secret bank levels" detection
- [ ] Documentation + examples

**Deliverable**: Renko charts working and visible in UI

---

### Week 4: Range Bars & Integration

**Days 1-2**: Range Bars
- [ ] Implement RangeBarBuilder
- [ ] ATR-based range sizing
- [ ] QuestDB storage
- [ ] API endpoints

**Days 3-4**: Unified Chart Manager
- [ ] RangeChartManager class
- [ ] Register multiple chart types per symbol
- [ ] Feed price updates to all charts
- [ ] Configuration system (appsettings.json)

**Day 5**: Strategy Integration
- [ ] Update IStrategy interface for tick/range data
- [ ] Create example tick-based strategy
- [ ] Create example Renko strategy
- [ ] Performance testing

**Deliverable**: Full tick + range chart infrastructure operational

---

### Week 5: MEM Integration

**Days 1-2**: MEM Tick Enhancement
- [ ] MEM learns optimal tick size per symbol
- [ ] MEM detects when to switch to tick charts
- [ ] Volatility-based chart switching
- [ ] Tick-based signal enhancement

**Days 3-4**: MEM Range Enhancement
- [ ] MEM learns optimal Renko brick sizes
- [ ] Institutional level detection from Renko
- [ ] Range bar pattern recognition
- [ ] Multi-chart strategy blending

**Day 5**: MEM Learning & Optimization
- [ ] Train MEM on historical tick data
- [ ] Backtest tick vs minute vs Renko
- [ ] Performance comparison
- [ ] Documentation

**Deliverable**: MEM intelligently using tick + range data

---

### Week 6: Production Deployment

**Days 1-2**: Performance Optimization
- [ ] Benchmark tick ingestion (target: 10K ticks/sec)
- [ ] Optimize QuestDB queries
- [ ] Add caching for frequently accessed data
- [ ] Memory profiling

**Days 3-4**: Monitoring & Alerts
- [ ] Tick data health checks
- [ ] Alert if tick feed drops
- [ ] Chart builder performance metrics
- [ ] Dashboard for tick/range status

**Day 5**: Production Launch
- [ ] Deploy to production
- [ ] Monitor for 24 hours
- [ ] Verify data quality
- [ ] Team training

**Deliverable**: Tick + Range charts in production

---

## Code Implementation Examples

### Strategy Using Tick Data

```csharp
// File: AlgoTrendy.TradingEngine/Strategies/TickMomentumStrategy.cs

public class TickMomentumStrategy : IStrategy
{
    private readonly int _tickSize = 1000;

    public async Task<Signal?> OnTickBarAsync(string symbol, TickBar tickBar)
    {
        // Get last 10 tick bars
        var recentBars = await _dataProvider.GetRecentTickBars(symbol, _tickSize, 10);

        // Calculate momentum
        var momentum = (tickBar.Close - recentBars.First().Close) / recentBars.First().Close;

        // Volume surge?
        var avgVolume = recentBars.Average(b => b.Volume);
        var volumeSurge = tickBar.Volume / avgVolume;

        if (momentum > 0.002m && volumeSurge > 1.5m)
        {
            return new Signal
            {
                Action = SignalAction.Buy,
                Symbol = symbol,
                Confidence = Math.Min(0.95m, momentum * 100),
                Reasoning = $"Tick momentum surge: {momentum:P2}, Volume {volumeSurge:F1}x avg"
            };
        }

        return null;
    }
}
```

### Strategy Using Renko Charts

```csharp
// File: AlgoTrendy.TradingEngine/Strategies/RenkoTrendStrategy.cs

public class RenkoTrendStrategy : IStrategy
{
    public async Task<Signal?> OnRenkoBrickAsync(string symbol, RenkoBrick brick)
    {
        // Get last 10 bricks
        var recentBricks = await _dataProvider.GetRecentRenkoBricks(symbol, "atr13", 10);

        // Count consecutive bullish/bearish bricks
        var consecutiveBullish = CountConsecutive(recentBricks, BrickDirection.Bullish);
        var consecutiveBearish = CountConsecutive(recentBricks, BrickDirection.Bearish);

        // Strong trend = 5+ consecutive bricks
        if (consecutiveBullish >= 5 && brick.IsBullish)
        {
            return new Signal
            {
                Action = SignalAction.Buy,
                Symbol = symbol,
                Confidence = Math.Min(0.95m, consecutiveBullish / 10m),
                Reasoning = $"Renko uptrend: {consecutiveBullish} consecutive bullish bricks"
            };
        }
        else if (consecutiveBearish >= 5 && brick.IsBearish)
        {
            return new Signal
            {
                Action = SignalAction.Sell,
                Symbol = symbol,
                Confidence = Math.Min(0.95m, consecutiveBearish / 10m),
                Reasoning = $"Renko downtrend: {consecutiveBearish} consecutive bearish bricks"
            };
        }

        return null;
    }

    private int CountConsecutive(List<RenkoBrick> bricks, BrickDirection direction)
    {
        int count = 0;
        foreach (var brick in bricks.Reverse<RenkoBrick>())
        {
            if (brick.Direction == direction)
                count++;
            else
                break;
        }
        return count;
    }
}
```

### MEM Chart Switching

```python
# File: MEM/chart_selector.py

class MEMChartSelector:
    """
    MEM intelligently selects chart type based on market conditions
    """

    async def select_optimal_chart(self, symbol, market_data):
        # Calculate market metrics
        volatility = self.calculate_volatility(market_data)
        trend_strength = self.calculate_trend_strength(market_data)
        volume_profile = self.calculate_volume_profile(market_data)

        # MEM classifies regime
        regime = self.mem_classify_regime({
            "volatility": volatility,
            "trend_strength": trend_strength,
            "volume": volume_profile
        })

        # MEM learned chart preferences
        preferences = {
            "high_volatility_trending": {
                "chart_type": "tick",
                "tick_size": 1000,
                "reason": "Tick charts capture rapid moves in volatile trends"
            },
            "high_volatility_ranging": {
                "chart_type": "range",
                "range_size": "atr13",
                "reason": "Range bars filter noise in choppy volatile markets"
            },
            "low_volatility_trending": {
                "chart_type": "renko",
                "brick_size": "atr13",
                "reason": "Renko shows clean trend in calm markets"
            },
            "low_volatility_ranging": {
                "chart_type": "minute",
                "timeframe": "5m",
                "reason": "Standard candles work fine in calm, ranging markets"
            }
        }

        selected = preferences.get(regime, {
            "chart_type": "minute",
            "timeframe": "5m",
            "reason": "Default to minute charts"
        })

        # Log decision
        self.mem_agent.store(f"chart_selection_{datetime.now()}", {
            "symbol": symbol,
            "regime": regime,
            "volatility": volatility,
            "selection": selected
        })

        return selected
```

---

## Configuration

```json
// appsettings.Production.json

{
  "TickData": {
    "Enabled": true,
    "Symbols": ["BTCUSDT", "ETHUSDT", "BNBUSDT"],
    "TickSizes": [100, 500, 1000, 2000],
    "Storage": {
      "QuestDB": {
        "Host": "localhost:9000",
        "RetentionDays": 7
      }
    }
  },

  "RangeCharts": {
    "Enabled": true,

    "Renko": {
      "BTCUSDT": [
        {
          "ChartId": "renko_atr13",
          "SizeType": "ATR",
          "ATRPeriod": 13
        },
        {
          "ChartId": "renko_500",
          "SizeType": "Fixed",
          "BrickSize": 500
        },
        {
          "ChartId": "renko_1pct",
          "SizeType": "Percentage",
          "Percentage": 1.0
        }
      ]
    },

    "RangeBars": {
      "BTCUSDT": [
        {
          "ChartId": "range_atr13",
          "SizeType": "ATR",
          "ATRPeriod": 13,
          "Multiplier": 1.0
        }
      ]
    }
  },

  "MEM": {
    "ChartSwitching": {
      "Enabled": true,
      "MonitorInterval": "1m",
      "VolatilityThreshold": 2.0
    }
  }
}
```

---

## Testing & Validation

### Unit Tests

```csharp
// File: AlgoTrendy.Tests/TickDataTests.cs

[Fact]
public void TickBarBuilder_CompletesBarAtTickSize()
{
    var builder = new TickBarBuilder(tickSize: 100);
    TickBar? completedBar = null;

    // Add 99 ticks - should NOT complete
    for (int i = 0; i < 99; i++)
    {
        completedBar = builder.AddTick(price: 65000 + i, size: 0.01m);
        Assert.Null(completedBar);
    }

    // 100th tick - should complete
    completedBar = builder.AddTick(price: 65100, size: 0.01m);
    Assert.NotNull(completedBar);
    Assert.Equal(100, completedBar.TickCount);
}

[Fact]
public void RenkoBuilder_CreatesBullishBrick()
{
    var builder = new RenkoChartBuilder(brickSize: 500, initialPrice: 65000);

    // Price moves up by 500
    var bricks = builder.OnPrice(price: 65500, DateTime.UtcNow);

    Assert.Single(bricks);
    Assert.Equal(BrickDirection.Bullish, bricks[0].Direction);
    Assert.Equal(65000, bricks[0].Open);
    Assert.Equal(65500, bricks[0].Close);
}

[Fact]
public void RenkoBuilder_CreatesMultipleBricks()
{
    var builder = new RenkoChartBuilder(brickSize: 500, initialPrice: 65000);

    // Price jumps 1500 - should create 3 bricks
    var bricks = builder.OnPrice(price: 66500, DateTime.UtcNow);

    Assert.Equal(3, bricks.Count);
    Assert.All(bricks, b => Assert.Equal(BrickDirection.Bullish, b.Direction));
}
```

### Integration Tests

```csharp
// File: AlgoTrendy.Tests/Integration/TickDataIntegrationTests.cs

[Fact]
public async Task TickCollector_StoresTicksInQuestDB()
{
    // Arrange
    var collector = new TickDataCollector(_questDB, _logger);

    // Act
    await collector.OnTradeAsync("BTCUSDT", new Trade
    {
        Price = 65000,
        Quantity = 0.5m,
        Timestamp = DateTime.UtcNow
    });

    // Assert
    var ticks = await _questDB.QueryAsync("SELECT * FROM ticks WHERE symbol = 'BTCUSDT'");
    Assert.NotEmpty(ticks);
}
```

### Performance Tests

```csharp
[Fact]
public async Task TickCollector_Handles10000TicksPerSecond()
{
    var collector = new TickDataCollector(_questDB, _logger);
    var stopwatch = Stopwatch.StartNew();

    // Simulate 10,000 ticks
    for (int i = 0; i < 10000; i++)
    {
        await collector.OnTradeAsync("BTCUSDT", new Trade
        {
            Price = 65000 + (i % 100),
            Quantity = 0.01m,
            Timestamp = DateTime.UtcNow
        });
    }

    stopwatch.Stop();

    // Should complete in < 1 second
    Assert.True(stopwatch.ElapsedMilliseconds < 1000,
        $"Took {stopwatch.ElapsedMilliseconds}ms to process 10K ticks");
}
```

---

## Success Metrics

### Performance Targets

| Metric | Target | Critical Threshold |
|--------|--------|-------------------|
| **Tick Ingestion** | 10,000 ticks/sec | 5,000 ticks/sec |
| **Tick Bar Latency** | < 10ms | < 50ms |
| **Renko Brick Latency** | < 5ms | < 20ms |
| **Storage Write Speed** | 10K writes/sec | 5K writes/sec |
| **Query Response** | < 100ms | < 500ms |
| **Memory Usage** | < 2GB | < 4GB |

### Data Quality Metrics

- ✅ Tick count matches exchange (99.9%+ accuracy)
- ✅ No missed ticks during normal operation
- ✅ Tick bars match manually calculated OHLCV
- ✅ Renko bricks mathematically correct
- ✅ No data gaps > 1 second

### Strategy Performance

- ✅ Tick strategies show improved entry timing
- ✅ Renko strategies filter noise effectively
- ✅ MEM learns optimal chart switching
- ✅ Overall win rate improves > 5%

---

## Next Steps

1. **Review & Approve** this implementation plan
2. **Allocate Resources** (1-2 developers for 6 weeks)
3. **Week 1**: Start tick data collection infrastructure
4. **Week 2**: Generate tick bars
5. **Week 3**: Build Renko charts
6. **Week 4**: Add Range bars
7. **Week 5**: MEM integration
8. **Week 6**: Production deployment

---

**Status**: Ready to begin implementation
**Priority**: High (critical for day trading strategies)
**Risk**: Low (isolated from existing systems)
**ROI**: High (better data = better strategies = more profit)

---

**Let's build it!** 🚀
