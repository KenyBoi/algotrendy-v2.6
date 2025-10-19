using AlgoTrendy.Backtesting.Indicators;
using AlgoTrendy.Backtesting.Models;
using Microsoft.Extensions.Logging;

namespace AlgoTrendy.Backtesting.Engines;

/// <summary>
/// Custom built-in backtesting engine
/// Implements a simple SMA crossover strategy for demonstration
/// Can be extended with more sophisticated strategies
/// </summary>
public class CustomBacktestEngine : IBacktestEngine
{
    private readonly ILogger<CustomBacktestEngine> _logger;

    /// <inheritdoc/>
    public string EngineName => "Custom Engine";

    /// <inheritdoc/>
    public string EngineDescription => "Built-in backtesting engine with SMA crossover strategy";

    /// <summary>
    /// Create instance of custom backtesting engine
    /// </summary>
    public CustomBacktestEngine(ILogger<CustomBacktestEngine> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public (bool IsValid, string? ErrorMessage) ValidateConfig(BacktestConfig config)
    {
        if (string.IsNullOrEmpty(config.Symbol))
            return (false, "Symbol is required");

        if (config.InitialCapital <= 0)
            return (false, "Initial capital must be greater than 0");

        if (config.EndDate <= config.StartDate)
            return (false, "End date must be after start date");

        return (true, null);
    }

    /// <inheritdoc/>
    public async Task<BacktestResults> RunAsync(BacktestConfig config, CancellationToken cancellationToken = default)
    {
        var startedAt = DateTime.UtcNow;
        var backtest Results = new BacktestResults
        {
            BacktestId = Guid.NewGuid().ToString(),
            Status = BacktestStatus.Running,
            Config = config,
            StartedAt = startedAt
        };

        try
        {
            _logger.LogInformation("Starting custom backtest for {Symbol}", config.Symbol);

            // Simulate async work
            await Task.Yield();

            // 1. Generate historical data
            var candles = GenerateHistoricalData(config);
            if (candles.Length == 0)
            {
                backtestResults.Status = BacktestStatus.Failed;
                backtestResults.ErrorMessage = "No historical data generated";
                return backtestResults;
            }

            // 2. Calculate indicators
            var closes = candles.Select(c => c.Close).ToArray();

            // Calculate SMAs for crossover strategy
            var sma20 = TechnicalIndicators.CalculateSMA(closes, 20);
            var sma50 = TechnicalIndicators.CalculateSMA(closes, 50);

            // 3. Run strategy
            var (trades, equityCurve) = RunSMACrossoverStrategy(
                candles,
                sma20,
                sma50,
                config.InitialCapital,
                config.Commission,
                config.Slippage
            );

            // 4. Calculate metrics
            var metrics = CalculateMetrics(trades, equityCurve, config.InitialCapital);

            // 5. Build results
            var completedAt = DateTime.UtcNow;
            var executionTime = (completedAt - startedAt).TotalSeconds;

            backtestResults = new BacktestResults
            {
                BacktestId = Guid.NewGuid().ToString(),
                Status = BacktestStatus.Completed,
                Config = config,
                StartedAt = startedAt,
                CompletedAt = completedAt,
                ExecutionTimeSeconds = executionTime,
                Metrics = metrics,
                EquityCurve = equityCurve,
                Trades = trades,
                IndicatorsUsed = new List<string> { "SMA(20)", "SMA(50)" },
                Metadata = new Dictionary<string, object>
                {
                    { "engine", "custom" },
                    { "dataPoints", candles.Length },
                    { "strategy", "SMA Crossover" },
                    { "assetClass", config.AssetClass.ToString() }
                }
            };

            _logger.LogInformation(
                "Backtest completed: {TradeCount} trades, {Return:P}% return",
                metrics.TotalTrades,
                metrics.TotalReturn / 100
            );

            return backtestResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backtest failed");
            return new BacktestResults
            {
                BacktestId = Guid.NewGuid().ToString(),
                Status = BacktestStatus.Failed,
                Config = config,
                StartedAt = startedAt,
                CompletedAt = DateTime.UtcNow,
                ErrorMessage = ex.Message,
                ErrorDetails = new Dictionary<string, object>
                {
                    { "errorType", ex.GetType().Name },
                    { "stackTrace", ex.StackTrace ?? string.Empty }
                }
            };
        }
    }

    /// <summary>
    /// Generate mock historical OHLCV data for backtesting
    /// In production, this would fetch real historical data
    /// </summary>
    private Candle[] GenerateHistoricalData(BacktestConfig config)
    {
        var candles = new List<Candle>();
        var currentDate = config.StartDate;
        var basePrice = GetBasePrice(config.Symbol);

        var random = new Random(42); // Seed for reproducibility

        while (currentDate <= config.EndDate && candles.Count < 500) // Limit to 500 candles for demo
        {
            // Skip weekends for daily data
            if (config.Timeframe == TimeframeType.Day && (currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday))
            {
                currentDate = currentDate.AddDays(1);
                continue;
            }

            // Generate random price movement
            var dailyReturn = random.NextDouble() * 0.04 - 0.02; // -2% to +2%
            basePrice *= (decimal)(1 + dailyReturn);

            var candle = new Candle
            {
                Timestamp = currentDate,
                Open = basePrice,
                High = basePrice * (1 + (decimal)(random.NextDouble() * 0.01)),
                Low = basePrice * (1 - (decimal)(random.NextDouble() * 0.01)),
                Close = basePrice,
                Volume = (decimal)(random.Next(100000, 1000000))
            };

            candles.Add(candle);
            currentDate = AddTimeframe(currentDate, config.Timeframe, config.TimeframeValue ?? 1);
        }

        return candles.ToArray();
    }

    /// <summary>
    /// Run Simple Moving Average crossover strategy
    /// Buy when fast MA crosses above slow MA
    /// Sell when fast MA crosses below slow MA
    /// </summary>
    private (List<TradeResult> Trades, List<EquityPoint> EquityCurve) RunSMACrossoverStrategy(
        Candle[] candles,
        decimal?[] fastMA,
        decimal?[] slowMA,
        decimal initialCapital,
        decimal commission,
        decimal slippage)
    {
        var trades = new List<TradeResult>();
        var equityCurve = new List<EquityPoint>();

        decimal cash = initialCapital;
        decimal position = 0; // Current position quantity
        decimal entryPrice = 0;
        DateTime? entryTime = null;
        decimal equity = initialCapital;

        for (int i = 51; i < candles.Length; i++) // Start from index 51 (50-period SMA available)
        {
            if (!fastMA[i].HasValue || !slowMA[i].HasValue)
                continue;

            var currentPrice = candles[i].Close;
            var positionValue = position * currentPrice;
            equity = cash + positionValue;

            // Add equity point
            equityCurve.Add(new EquityPoint
            {
                Timestamp = candles[i].Timestamp,
                Equity = equity,
                Cash = cash,
                PositionsValue = positionValue
            });

            // Check for crossover
            bool bullishCrossover = fastMA[i] > slowMA[i] &&
                                  (i > 0 && fastMA[i - 1] <= slowMA[i - 1]);

            bool bearishCrossover = fastMA[i] < slowMA[i] &&
                                   (i > 0 && fastMA[i - 1] >= slowMA[i - 1]);

            // Entry: Buy on bullish crossover
            if (bullishCrossover && position == 0 && cash > 0)
            {
                decimal quantity = (cash * 0.95m) / (currentPrice * (1 + commission + slippage));
                position = quantity;
                entryPrice = currentPrice * (1 + commission + slippage);
                entryTime = candles[i].Timestamp;
                cash -= quantity * entryPrice;
            }

            // Exit: Sell on bearish crossover
            else if (bearishCrossover && position > 0)
            {
                decimal exitPrice = currentPrice * (1 - commission - slippage);
                decimal pnl = (exitPrice - entryPrice) * position;
                decimal pnlPercent = (exitPrice - entryPrice) / entryPrice * 100;
                decimal durationMinutes = (int)(candles[i].Timestamp - entryTime!.Value).TotalMinutes;

                trades.Add(new TradeResult
                {
                    EntryTime = entryTime!.Value,
                    ExitTime = candles[i].Timestamp,
                    EntryPrice = entryPrice,
                    ExitPrice = exitPrice,
                    Quantity = position,
                    Side = TradeDirection.Long,
                    PnL = pnl,
                    PnLPercent = pnlPercent,
                    DurationMinutes = (int)durationMinutes,
                    ExitReason = "SMA crossover"
                });

                cash += position * exitPrice;
                position = 0;
                entryPrice = 0;
                entryTime = null;
            }
        }

        // Close any open position at the end
        if (position > 0 && candles.Length > 0)
        {
            var finalPrice = candles[^1].Close;
            decimal pnl = (finalPrice - entryPrice) * position;
            decimal pnlPercent = (finalPrice - entryPrice) / entryPrice * 100;
            decimal durationMinutes = (int)(candles[^1].Timestamp - entryTime!.Value).TotalMinutes;

            trades.Add(new TradeResult
            {
                EntryTime = entryTime!.Value,
                ExitTime = candles[^1].Timestamp,
                EntryPrice = entryPrice,
                ExitPrice = finalPrice,
                Quantity = position,
                Side = TradeDirection.Long,
                PnL = pnl,
                PnLPercent = pnlPercent,
                DurationMinutes = (int)durationMinutes,
                ExitReason = "End of backtest"
            });

            position = 0;
        }

        return (trades, equityCurve);
    }

    /// <summary>
    /// Calculate backtest metrics from trades and equity curve
    /// </summary>
    private BacktestMetrics CalculateMetrics(List<TradeResult> trades, List<EquityPoint> equityCurve, decimal initialCapital)
    {
        var metrics = new BacktestMetrics();

        if (trades.Count == 0)
        {
            metrics.TotalReturn = 0;
            metrics.TotalTrades = 0;
            metrics.SharpeRatio = 0;
            metrics.MaxDrawdown = 0;
            metrics.WinRate = 0;
            metrics.ProfitFactor = 0;
            metrics.FinalValue = initialCapital;
            return metrics;
        }

        // Basic metrics
        metrics.TotalTrades = trades.Count;
        metrics.WinningTrades = trades.Count(t => t.PnL > 0);
        metrics.LosingTrades = trades.Count(t => t.PnL < 0);
        metrics.WinRate = (decimal)metrics.WinningTrades / metrics.TotalTrades * 100;

        // Profit metrics
        decimal totalWins = trades.Where(t => t.PnL > 0).Sum(t => t.PnL ?? 0);
        decimal totalLosses = Math.Abs(trades.Where(t => t.PnL < 0).Sum(t => t.PnL ?? 0));
        metrics.ProfitFactor = totalLosses > 0 ? totalWins / totalLosses : (totalWins > 0 ? 1 : 0);

        // Average trade metrics
        metrics.AverageTrade = trades.Count > 0 ? (decimal)(trades.Average(t => (double?)t.PnL) ?? 0) : 0;
        metrics.AverageWin = metrics.WinningTrades > 0 ? trades.Where(t => t.PnL > 0).Average(t => (double?)t.PnL) ?? 0 : 0;
        metrics.AverageLoss = metrics.LosingTrades > 0 ? Math.Abs((decimal)(trades.Where(t => t.PnL < 0).Average(t => (double?)t.PnL) ?? 0)) : 0;
        metrics.AverageTradeDuration = trades.Count > 0 ? (decimal?)trades.Average(t => t.DurationMinutes ?? 0) ?? 0 : 0;

        // Final equity metrics
        if (equityCurve.Count > 0)
        {
            metrics.FinalValue = equityCurve[^1].Equity;
            metrics.TotalReturn = ((metrics.FinalValue - initialCapital) / initialCapital) * 100;
            metrics.TotalPnL = metrics.FinalValue - initialCapital;

            // Calculate maximum drawdown
            decimal peakEquity = initialCapital;
            decimal maxDD = 0;
            foreach (var point in equityCurve)
            {
                if (point.Equity > peakEquity)
                    peakEquity = point.Equity;

                decimal dd = (peakEquity - point.Equity) / peakEquity;
                if (dd > maxDD)
                    maxDD = dd;
            }
            metrics.MaxDrawdown = maxDD * 100;

            // Simple Sharpe ratio calculation
            if (equityCurve.Count > 1)
            {
                var returns = new decimal[equityCurve.Count - 1];
                for (int i = 1; i < equityCurve.Count; i++)
                {
                    returns[i - 1] = (equityCurve[i].Equity - equityCurve[i - 1].Equity) / equityCurve[i - 1].Equity;
                }

                decimal avgReturn = returns.Average();
                decimal stdDev = (decimal)Math.Sqrt((double)returns.Sum(r => (r - avgReturn) * (r - avgReturn)) / returns.Length);
                metrics.SharpeRatio = stdDev > 0 ? (avgReturn / stdDev) * (decimal)Math.Sqrt(252) : 0; // Annualized
                metrics.AnnualizedReturn = metrics.TotalReturn * 252 / equityCurve.Count; // Simple annualization
            }
        }

        return metrics;
    }

    /// <summary>
    /// Get base price for a symbol (for data generation)
    /// </summary>
    private decimal GetBasePrice(string symbol) =>
        symbol switch
        {
            var s when s.Contains("BTC") => 40000m,
            var s when s.Contains("ETH") => 2500m,
            var s when s.Contains("ES") || s.Contains("NQ") || s.Contains("YM") => 5000m,
            var s when s.Contains("CL") => 80m,
            _ => 100m
        };

    /// <summary>
    /// Add a timeframe to a date
    /// </summary>
    private DateTime AddTimeframe(DateTime date, TimeframeType timeframe, int value) =>
        timeframe switch
        {
            TimeframeType.Minute => date.AddMinutes(value),
            TimeframeType.Hour => date.AddHours(value),
            TimeframeType.Day => date.AddDays(value),
            TimeframeType.Week => date.AddDays(value * 7),
            TimeframeType.Month => date.AddMonths(value),
            _ => date.AddDays(1)
        };
}
