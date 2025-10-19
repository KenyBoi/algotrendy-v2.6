# Phase 7B: Backtesting Engine Implementation Plan

**Date:** October 19, 2025
**Status:** 🚀 STARTED
**Estimated Duration:** 30-40 hours
**Priority:** 🔴 CRITICAL

---

## 📋 Overview

Port the complete backtesting system from v2.5 Python to v2.6 C# .NET 8, enabling strategy validation before live trading.

---

## 🎯 Success Criteria

- ✅ Port all 8 technical indicators from Python to C#
- ✅ Implement custom backtesting engine
- ✅ Calculate 14 performance metrics
- ✅ Create 6 REST API endpoints
- ✅ Write 50+ unit tests
- ✅ Integrate with existing QuestDB for historical data
- ✅ Match or exceed v2.5 functionality

---

## 📁 Project Structure

```
backend/
├── AlgoTrendy.Backtesting/          # NEW PROJECT
│   ├── Models/                      # Data models
│   │   ├── BacktestConfig.cs
│   │   ├── BacktestResults.cs
│   │   ├── BacktestMetrics.cs
│   │   ├── TradeResult.cs
│   │   ├── EquityPoint.cs
│   │   └── Enums.cs
│   │
│   ├── Indicators/                  # Technical indicators
│   │   ├── IIndicator.cs
│   │   ├── SMA.cs
│   │   ├── EMA.cs
│   │   ├── RSI.cs
│   │   ├── MACD.cs
│   │   ├── BollingerBands.cs
│   │   ├── ATR.cs
│   │   ├── Stochastic.cs
│   │   └── IndicatorCalculator.cs
│   │
│   ├── Engines/                     # Backtesting engines
│   │   ├── IBacktestEngine.cs
│   │   ├── CustomEngine.cs
│   │   └── EngineFactory.cs
│   │
│   ├── Metrics/                     # Performance metrics
│   │   └── PerformanceCalculator.cs
│   │
│   └── Services/                    # Business logic
│       ├── IBacktestService.cs
│       └── BacktestService.cs
│
├── AlgoTrendy.API/
│   └── Controllers/
│       └── BacktestController.cs    # NEW CONTROLLER
│
└── AlgoTrendy.Tests/
    └── Unit/
        └── Backtesting/              # NEW TEST DIRECTORY
            ├── IndicatorTests.cs
            ├── EngineTests.cs
            ├── MetricsTests.cs
            └── BacktestServiceTests.cs
```

---

## 🔧 Implementation Tasks

### Task 1: Create Project Structure (1 hour)

**Steps:**
1. Create `AlgoTrendy.Backtesting` class library project
2. Add project references
3. Install NuGet packages
4. Set up directory structure

**Commands:**
```bash
cd /root/AlgoTrendy_v2.6/backend
dotnet new classlib -n AlgoTrendy.Backtesting -f net8.0
cd AlgoTrendy.Backtesting

# Add references
dotnet add reference ../AlgoTrendy.Core/AlgoTrendy.Core.csproj

# Install packages
dotnet add package MathNet.Numerics
dotnet add package System.Linq.Async
```

**Deliverables:**
- Project file created
- Directory structure established
- Dependencies installed

---

### Task 2: Port Data Models (2 hours)

**Python → C# Mappings:**

| Python (Pydantic) | C# (.NET 8) |
|-------------------|-------------|
| `BaseModel` | `class` with properties |
| `Field()` | `[Required]`, `[Range()]` attributes |
| `Enum` | `enum` |
| `validator` | `IValidatableObject` |
| `Optional[T]` | `T?` (nullable) |
| `List[T]` | `List<T>` |
| `Dict[str, Any]` | `Dictionary<string, object>` |

**Files to Create:**
1. `Models/Enums.cs` - All enumerations
2. `Models/BacktestConfig.cs` - Configuration model
3. `Models/TradeResult.cs` - Trade result model
4. `Models/EquityPoint.cs` - Equity curve point
5. `Models/BacktestMetrics.cs` - Performance metrics
6. `Models/BacktestResults.cs` - Complete results

**Example Conversion:**

Python:
```python
class BacktestConfig(BaseModel):
    symbol: str = Field(description="Trading symbol")
    initial_capital: float = Field(default=10000.0, gt=0)
    start_date: str = Field(description="Start date (YYYY-MM-DD)")
```

C#:
```csharp
public class BacktestConfig : IValidatableObject
{
    [Required]
    public string Symbol { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal InitialCapital { get; set; } = 10000m;

    [Required]
    public string StartDate { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext context) { ... }
}
```

---

### Task 3: Port Technical Indicators (8 hours)

**Indicators to Port (8 total):**

#### 1. SMA - Simple Moving Average (30 min)
```csharp
public static class SMA
{
    public static List<decimal?> Calculate(List<decimal> data, int period)
    {
        var result = new List<decimal?>();

        for (int i = 0; i < data.Count; i++)
        {
            if (i < period - 1)
            {
                result.Add(null);
            }
            else
            {
                var avg = data.Skip(i - period + 1).Take(period).Average();
                result.Add(avg);
            }
        }

        return result;
    }
}
```

#### 2. EMA - Exponential Moving Average (1 hour)
Uses exponential weighting factor:
`EMA = (Close - EMA_prev) × multiplier + EMA_prev`
where `multiplier = 2 / (period + 1)`

#### 3. RSI - Relative Strength Index (1 hour)
```
RSI = 100 - (100 / (1 + RS))
RS = Average Gain / Average Loss
```

#### 4. MACD (1 hour)
```
MACD Line = EMA(12) - EMA(26)
Signal Line = EMA(9) of MACD Line
Histogram = MACD Line - Signal Line
```

#### 5. Bollinger Bands (1 hour)
```
Middle Band = SMA(20)
Upper Band = SMA(20) + (StdDev × 2)
Lower Band = SMA(20) - (StdDev × 2)
```

#### 6. ATR - Average True Range (1 hour)
```
True Range = Max(High - Low, |High - PrevClose|, |Low - PrevClose|)
ATR = SMA(True Range, 14)
```

#### 7. Stochastic Oscillator (1 hour)
```
%K = 100 × (Close - LowestLow) / (HighestHigh - LowestLow)
%D = SMA(%K, 3)
```

#### 8. Volume (15 min)
Simple pass-through of volume data.

**Common Utilities:**
```csharp
public static class IndicatorHelpers
{
    public static decimal StandardDeviation(IEnumerable<decimal> values)
    {
        var avg = values.Average();
        var sumOfSquares = values.Sum(v => (v - avg) * (v - avg));
        return (decimal)Math.Sqrt((double)(sumOfSquares / values.Count()));
    }
}
```

---

### Task 4: Implement Backtesting Engine (10 hours)

#### 4.1 Engine Interface (30 min)
```csharp
public interface IBacktestEngine
{
    Task<BacktestResults> RunAsync(BacktestConfig config);
    bool ValidateConfig(BacktestConfig config);
    string EngineName { get; }
}
```

#### 4.2 Custom Engine Implementation (8 hours)

**Core Methods:**

1. **FetchHistoricalDataAsync** (2 hours)
   - Query QuestDB for historical candles
   - Filter by symbol and date range
   - Convert to List<Candle>

2. **CalculateIndicatorsAsync** (1 hour)
   - Apply enabled indicators to data
   - Store results in candle extended properties

3. **RunStrategyAsync** (4 hours)
   - Implement SMA crossover strategy
   - Track positions and cash
   - Generate trades list
   - Calculate equity curve
   - Handle commissions and slippage

4. **CalculateMetricsAsync** (1 hour)
   - Call PerformanceCalculator
   - Return complete metrics

**Strategy Logic (SMA Crossover):**
```csharp
private async Task<(List<TradeResult>, List<EquityPoint>)> RunStrategyAsync(
    List<CandleWithIndicators> candles,
    BacktestConfig config)
{
    var trades = new List<TradeResult>();
    var equity = new List<EquityPoint>();

    decimal cash = config.InitialCapital;
    decimal position = 0;
    decimal positionPrice = 0;
    DateTime? entryTime = null;

    decimal peak = config.InitialCapital;

    foreach (var candle in candles)
    {
        var smaFast = candle.Indicators.GetValueOrDefault("sma_20");
        var smaSlow = candle.Indicators.GetValueOrDefault("sma_50");

        if (!smaFast.HasValue || !smaSlow.HasValue)
        {
            AddEquityPoint(equity, candle.Timestamp, cash, 0, peak);
            continue;
        }

        var closePrice = candle.Close;
        var positionsValue = position * closePrice;
        var currentEquity = cash + positionsValue;

        if (currentEquity > peak) peak = currentEquity;
        var drawdown = ((currentEquity - peak) / peak) * 100;

        // Entry: Fast SMA crosses above Slow SMA
        if (smaFast > smaSlow && position == 0)
        {
            var cost = cash * 0.95m; // 95% of cash
            var qty = cost / closePrice;
            var commission = cost * config.Commission;

            if (cash >= cost + commission)
            {
                position = qty;
                positionPrice = closePrice;
                entryTime = candle.Timestamp;
                cash -= (cost + commission);
            }
        }
        // Exit: Fast SMA crosses below Slow SMA
        else if (smaFast < smaSlow && position > 0)
        {
            var proceeds = position * closePrice;
            var commission = proceeds * config.Commission;
            cash += (proceeds - commission);

            var pnl = proceeds - (position * positionPrice);
            var pnlPct = (pnl / (position * positionPrice)) * 100;

            trades.Add(new TradeResult
            {
                EntryTime = entryTime.Value,
                ExitTime = candle.Timestamp,
                EntryPrice = positionPrice,
                ExitPrice = closePrice,
                Quantity = position,
                Side = "long",
                PnL = pnl - (2 * commission),
                PnLPercent = pnlPct,
                DurationMinutes = (int)(candle.Timestamp - entryTime.Value).TotalMinutes,
                ExitReason = "sma_crossover"
            });

            position = 0;
            positionPrice = 0;
            entryTime = null;
        }

        AddEquityPoint(equity, candle.Timestamp, cash, positionsValue, peak);
    }

    // Close any open position at end
    if (position > 0)
    {
        // ... similar exit logic
    }

    return (trades, equity);
}
```

---

### Task 5: Implement Performance Metrics (3 hours)

**14 Metrics to Calculate:**

1. **Total Return** - `(Final Equity - Initial) / Initial × 100`
2. **Annual Return** - `Total Return × (365 / Days)`
3. **Sharpe Ratio** - `Mean(Returns) / StdDev(Returns) × √252`
4. **Sortino Ratio** - `Mean(Returns) / DownsideStdDev × √252`
5. **Max Drawdown** - `Min(Equity - Peak) / Peak × 100`
6. **Win Rate** - `Winning Trades / Total Trades × 100`
7. **Profit Factor** - `Gross Profit / Gross Loss`
8. **Total Trades**
9. **Winning Trades**
10. **Losing Trades**
11. **Average Win**
12. **Average Loss**
13. **Largest Win**
14. **Largest Loss**

```csharp
public class PerformanceCalculator
{
    public static BacktestMetrics Calculate(
        List<TradeResult> trades,
        List<EquityPoint> equityCurve,
        decimal initialCapital)
    {
        if (trades.Count == 0)
            return new BacktestMetrics(); // All zeros

        var winningTrades = trades.Where(t => t.PnL > 0).ToList();
        var losingTrades = trades.Where(t => t.PnL <= 0).ToList();

        var totalPnL = trades.Sum(t => t.PnL);
        var totalReturn = (totalPnL / initialCapital) * 100;

        var days = (equityCurve.Last().Timestamp - equityCurve.First().Timestamp).Days;
        var annualReturn = days > 0 ? (totalReturn / days * 365) : 0;

        var sharpe = CalculateSharpeRatio(equityCurve);
        var sortino = CalculateSortinoRatio(equityCurve);
        var maxDD = equityCurve.Min(e => e.Drawdown);

        return new BacktestMetrics
        {
            TotalReturn = Math.Round(totalReturn, 2),
            AnnualReturn = Math.Round(annualReturn, 2),
            SharpeRatio = Math.Round(sharpe, 2),
            SortinoRatio = Math.Round(sortino, 2),
            MaxDrawdown = Math.Round(maxDD, 2),
            WinRate = Math.Round((decimal)winningTrades.Count / trades.Count * 100, 2),
            ProfitFactor = CalculateProfitFactor(winningTrades, losingTrades),
            TotalTrades = trades.Count,
            WinningTrades = winningTrades.Count,
            LosingTrades = losingTrades.Count,
            AvgWin = winningTrades.Any() ? Math.Round(winningTrades.Average(t => t.PnL), 2) : 0,
            AvgLoss = losingTrades.Any() ? Math.Round(losingTrades.Average(t => Math.Abs(t.PnL)), 2) : 0,
            LargestWin = winningTrades.Any() ? Math.Round(winningTrades.Max(t => t.PnL), 2) : 0,
            LargestLoss = losingTrades.Any() ? Math.Round(losingTrades.Min(t => t.PnL), 2) : 0,
            AvgTradeDuration = trades.Average(t => t.DurationMinutes) / 60.0
        };
    }

    private static decimal CalculateSharpeRatio(List<EquityPoint> equityCurve)
    {
        if (equityCurve.Count < 2) return 0;

        var returns = new List<decimal>();
        for (int i = 1; i < equityCurve.Count; i++)
        {
            var ret = (equityCurve[i].Equity / equityCurve[i-1].Equity) - 1;
            returns.Add(ret);
        }

        if (returns.Count == 0) return 0;

        var mean = returns.Average();
        var stdDev = CalculateStdDev(returns);

        if (stdDev == 0) return 0;

        return (mean / stdDev) * (decimal)Math.Sqrt(252); // Annualized
    }

    private static decimal CalculateStdDev(List<decimal> values)
    {
        var avg = values.Average();
        var sumOfSquares = values.Sum(v => (v - avg) * (v - avg));
        return (decimal)Math.Sqrt((double)(sumOfSquares / values.Count));
    }
}
```

---

### Task 6: Create REST API Endpoints (3 hours)

**6 Endpoints:**

```csharp
[ApiController]
[Route("api/[controller]")]
public class BacktestController : ControllerBase
{
    private readonly IBacktestService _backtestService;

    public BacktestController(IBacktestService backtestService)
    {
        _backtestService = backtestService;
    }

    // GET /api/backtest/config
    [HttpGet("config")]
    public IActionResult GetConfig()
    {
        return Ok(new
        {
            AssetClasses = new[] { "crypto", "futures", "equities" },
            Timeframes = new[] { "tick", "min", "hr", "day", "wk", "mo" },
            Indicators = new[] { "sma", "ema", "rsi", "macd", "bollinger", "atr", "stochastic", "volume" }
        });
    }

    // POST /api/backtest/run
    [HttpPost("run")]
    public async Task<IActionResult> RunBacktest([FromBody] BacktestConfig config)
    {
        var result = await _backtestService.RunBacktestAsync(config);
        return Ok(result);
    }

    // GET /api/backtest/results/{id}
    [HttpGet("results/{id}")]
    public async Task<IActionResult> GetResults(string id)
    {
        var result = await _backtestService.GetResultsAsync(id);
        return result != null ? Ok(result) : NotFound();
    }

    // GET /api/backtest/history
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var history = await _backtestService.GetHistoryAsync();
        return Ok(history);
    }

    // GET /api/backtest/indicators
    [HttpGet("indicators")]
    public IActionResult GetIndicators()
    {
        var indicators = IndicatorMetadata.GetAll();
        return Ok(indicators);
    }

    // DELETE /api/backtest/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBacktest(string id)
    {
        await _backtestService.DeleteBacktestAsync(id);
        return NoContent();
    }
}
```

---

### Task 7: Write Unit Tests (5 hours)

**Test Coverage:**

1. **IndicatorTests.cs** (15 tests, 2 hours)
   - Test each indicator calculation
   - Edge cases (empty data, insufficient data)
   - Known value validation

2. **EngineTests.cs** (15 tests, 1.5 hours)
   - Strategy logic correctness
   - Trade execution
   - Commission/slippage handling
   - Position tracking

3. **MetricsTests.cs** (10 tests, 1 hour)
   - Each metric calculation
   - Edge cases (no trades, all wins, all losses)

4. **BacktestServiceTests.cs** (10 tests, 30 min)
   - Service orchestration
   - Error handling
   - Results storage/retrieval

**Example Test:**
```csharp
[Fact]
public void SMA_WithValidData_ReturnsCorrectAverage()
{
    // Arrange
    var data = new List<decimal> { 10, 11, 12, 13, 14 };
    var period = 3;

    // Act
    var result = SMA.Calculate(data, period);

    // Assert
    Assert.Null(result[0]); // Not enough data
    Assert.Null(result[1]); // Not enough data
    Assert.Equal(11m, result[2]); // (10+11+12)/3
    Assert.Equal(12m, result[3]); // (11+12+13)/3
    Assert.Equal(13m, result[4]); // (12+13+14)/3
}
```

---

### Task 8: Integration & Testing (2 hours)

1. **QuestDB Integration** - Fetch real historical data
2. **End-to-End Test** - Run complete backtest
3. **Performance Test** - Benchmark execution time
4. **Validation** - Compare results with v2.5 Python

---

### Task 9: Documentation (2 hours)

1. **API Documentation** - Swagger/OpenAPI annotations
2. **Usage Guide** - How to run backtests
3. **Developer Guide** - How to add new indicators/strategies
4. **Migration Notes** - Python → C# conversion notes

---

## 📊 Progress Tracking

| Task | Estimated | Actual | Status |
|------|-----------|--------|--------|
| 1. Project Structure | 1h | - | ⏳ Pending |
| 2. Data Models | 2h | - | ⏳ Pending |
| 3. Indicators | 8h | - | ⏳ Pending |
| 4. Engine | 10h | - | ⏳ Pending |
| 5. Metrics | 3h | - | ⏳ Pending |
| 6. API Endpoints | 3h | - | ⏳ Pending |
| 7. Unit Tests | 5h | - | ⏳ Pending |
| 8. Integration | 2h | - | ⏳ Pending |
| 9. Documentation | 2h | - | ⏳ Pending |
| **TOTAL** | **36h** | **0h** | **0%** |

---

## ✅ Definition of Done

- [ ] All 8 indicators implemented and tested
- [ ] Custom backtesting engine functional
- [ ] 14 performance metrics calculated correctly
- [ ] 6 REST API endpoints working
- [ ] 50+ unit tests passing
- [ ] Integration with QuestDB complete
- [ ] Swagger documentation generated
- [ ] Code review completed
- [ ] Performance acceptable (<5 sec for 1 year backtest)
- [ ] Documentation updated

---

## 🚀 Next Steps After Completion

1. **Phase 7C:** Add more strategies (MACD, MFI, VWAP)
2. **Phase 7D:** Parameter optimization
3. **Phase 7E:** Walk-forward analysis
4. **Phase 8:** Dashboard UI with charts

---

**Status:** ✅ PLAN COMPLETE - Ready to begin implementation
**Started:** October 19, 2025
**Target Completion:** October 22-23, 2025 (3-4 days)
