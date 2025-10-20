using AlgoTrendy.Backtesting.Indicators;
using AlgoTrendy.Backtesting.Metrics;
using AlgoTrendy.Backtesting.Models;
using AlgoTrendy.Core.Interfaces;
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
    private readonly IMarketDataProvider _dataProvider;

    /// <inheritdoc/>
    public string EngineName => "Custom Engine";

    /// <inheritdoc/>
    public string EngineDescription => "Built-in backtesting engine with SMA crossover strategy using real market data";

    /// <summary>
    /// Create instance of custom backtesting engine
    /// </summary>
    public CustomBacktestEngine(
        ILogger<CustomBacktestEngine> logger,
        IMarketDataProvider dataProvider)
    {
        _logger = logger;
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
    }

    /// <inheritdoc/>
    public (bool IsValid, string? ErrorMessage) ValidateConfig(BacktestConfig config)
    {
        // DISABLED: Custom engine is currently disabled pending accuracy verification
        // DO NOT USE until full validation against QuantConnect is complete
        return (false, "Custom Engine is currently disabled. Please use QuantConnect engine instead. The Custom engine requires accuracy verification before use.");
    }

    /// <inheritdoc/>
    public Task<BacktestResults> RunAsync(BacktestConfig config, CancellationToken cancellationToken = default)
    {
        var startedAt = DateTime.UtcNow;

        // DISABLED: Block execution at runtime
        _logger.LogError("Attempted to run Custom Engine which is currently disabled");
        return Task.FromResult(new BacktestResults
        {
            BacktestId = Guid.NewGuid().ToString(),
            Status = BacktestStatus.Failed,
            Config = config,
            StartedAt = startedAt,
            CompletedAt = DateTime.UtcNow,
            ErrorMessage = "Custom Engine is currently disabled. Please use QuantConnect engine instead. The Custom engine requires accuracy verification before use."
        });

        // Original implementation commented out - DO NOT USE until verified
        /*
        var backtestResults = new BacktestResults
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

            // 1. Fetch real historical data
            var candles = await FetchHistoricalDataAsync(config, cancellationToken);
            if (candles.Length == 0)
            {
                backtestResults.Status = BacktestStatus.Failed;
                backtestResults.ErrorMessage = "No historical data available for the specified symbol and date range";
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
        */
    }

    /// <summary>
    /// Fetch real historical OHLCV data from market data provider
    /// </summary>
    private async Task<Candle[]> FetchHistoricalDataAsync(BacktestConfig config, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Fetching historical data for {Symbol} from {StartDate} to {EndDate}",
                config.Symbol, config.StartDate, config.EndDate);

            // Determine interval based on timeframe
            var interval = GetIntervalString(config.Timeframe, config.TimeframeValue ?? 1);

            // Fetch real market data
            var marketData = await _dataProvider.FetchHistoricalAsync(
                config.Symbol,
                config.StartDate,
                config.EndDate,
                interval,
                cancellationToken);

            if (marketData == null || !marketData.Any())
            {
                _logger.LogWarning("No historical data returned for {Symbol}", config.Symbol);
                return Array.Empty<Candle>();
            }

            // Convert MarketData to Candle format
            var candles = marketData.Select(md => new Candle
            {
                Timestamp = md.Timestamp,
                Open = md.Open,
                High = md.High,
                Low = md.Low,
                Close = md.Close,
                Volume = md.Volume
            }).OrderBy(c => c.Timestamp).ToArray();

            _logger.LogInformation("Fetched {Count} candles for backtesting", candles.Length);

            return candles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching historical data for {Symbol}", config.Symbol);
            return Array.Empty<Candle>();
        }
    }

    /// <summary>
    /// Convert timeframe to interval string for data provider
    /// </summary>
    private string GetIntervalString(TimeframeType timeframe, int value)
    {
        return timeframe switch
        {
            TimeframeType.Minute => $"{value}m",
            TimeframeType.Hour => $"{value}h",
            TimeframeType.Day => $"{value}d",
            TimeframeType.Week => $"{value}w",
            TimeframeType.Month => $"{value}mo",
            _ => "1d"
        };
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
        decimal peak = initialCapital; // Track peak equity for drawdown calculation

        for (int i = 51; i < candles.Length; i++) // Start from index 51 (50-period SMA available)
        {
            if (!fastMA[i].HasValue || !slowMA[i].HasValue)
                continue;

            var currentPrice = candles[i].Close;
            var positionValue = position * currentPrice;
            equity = cash + positionValue;

            // Update peak and calculate drawdown
            if (equity > peak)
                peak = equity;

            var drawdown = peak > 0 ? ((equity - peak) / peak) * 100 : 0;

            // Add equity point with Peak and Drawdown tracking
            equityCurve.Add(new EquityPoint
            {
                Timestamp = candles[i].Timestamp,
                Equity = equity,
                Cash = cash,
                PositionsValue = positionValue,
                Peak = peak,
                Drawdown = drawdown
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
    /// Calculate backtest metrics from trades and equity curve using PerformanceCalculator
    /// </summary>
    private BacktestMetrics CalculateMetrics(List<TradeResult> trades, List<EquityPoint> equityCurve, decimal initialCapital)
    {
        // Use the centralized PerformanceCalculator for consistent metrics
        var metrics = PerformanceCalculator.Calculate(trades, equityCurve, initialCapital);

        // Add additional fields for compatibility
        if (equityCurve.Count > 0)
        {
            metrics.FinalValue = equityCurve[^1].Equity;
            metrics.TotalPnL = metrics.FinalValue - initialCapital;
        }
        else
        {
            metrics.FinalValue = initialCapital;
            metrics.TotalPnL = 0;
        }

        // Calculate average trade (not in PerformanceCalculator yet)
        if (trades.Any())
        {
            metrics.AverageTrade = trades.Average(t => t.PnL ?? 0);
        }

        return metrics;
    }

}
