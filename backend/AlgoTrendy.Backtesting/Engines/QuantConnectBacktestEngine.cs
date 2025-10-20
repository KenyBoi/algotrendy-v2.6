using AlgoTrendy.Backtesting.Models;
using AlgoTrendy.Backtesting.Models.QuantConnect;
using AlgoTrendy.Backtesting.Services;
using Microsoft.Extensions.Logging;

namespace AlgoTrendy.Backtesting.Engines;

/// <summary>
/// QuantConnect cloud backtesting engine implementation
/// </summary>
public class QuantConnectBacktestEngine : IBacktestEngine
{
    private readonly IQuantConnectApiClient _apiClient;
    private readonly ILogger<QuantConnectBacktestEngine> _logger;

    public string EngineName => "QuantConnect";
    public string EngineDescription => "Professional-grade cloud backtesting with institutional data quality";

    public QuantConnectBacktestEngine(
        IQuantConnectApiClient apiClient,
        ILogger<QuantConnectBacktestEngine> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public (bool IsValid, string? ErrorMessage) ValidateConfig(BacktestConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Symbol))
            return (false, "Symbol is required");

        if (config.StartDate >= config.EndDate)
            return (false, "Start date must be before end date");

        if (config.EndDate > DateTime.UtcNow)
            return (false, "End date cannot be in the future");

        var timeSpan = config.EndDate - config.StartDate;
        if (timeSpan.TotalDays < 1)
            return (false, "Backtest period must be at least 1 day");

        return (true, null);
    }

    /// <inheritdoc/>
    public async Task<BacktestResults> RunAsync(BacktestConfig config, CancellationToken cancellationToken = default)
    {
        var backtestId = Guid.NewGuid().ToString();
        var startedAt = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting QuantConnect backtest for {Symbol}", config.Symbol);

            // Validate config
            var (isValid, errorMessage) = ValidateConfig(config);
            if (!isValid)
            {
                return CreateFailedResult(backtestId, config, startedAt, errorMessage!);
            }

            // Step 1: Authenticate
            _logger.LogInformation("Authenticating with QuantConnect API");
            var authenticated = await _apiClient.AuthenticateAsync(cancellationToken);
            if (!authenticated)
            {
                return CreateFailedResult(backtestId, config, startedAt, "Failed to authenticate with QuantConnect API");
            }

            // Step 2: Create project
            _logger.LogInformation("Creating QuantConnect project");
            var projectName = $"AlgoTrendy_{config.Symbol}_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            var projectResponse = await _apiClient.CreateProjectAsync(projectName, "CSharp", cancellationToken);

            if (!projectResponse.Success || projectResponse.Projects.Count == 0)
            {
                var error = string.Join(", ", projectResponse.Errors);
                return CreateFailedResult(backtestId, config, startedAt, $"Failed to create project: {error}");
            }

            var projectId = projectResponse.Projects[0].ProjectId;
            _logger.LogInformation("Created project {ProjectId}", projectId);

            // Step 3: Generate algorithm code
            var algorithmCode = GenerateAlgorithmCode(config);

            // Step 4: Upload algorithm to project
            _logger.LogInformation("Uploading algorithm code to project");
            var fileCreated = await _apiClient.CreateOrUpdateFileAsync(
                projectId,
                "Main.cs",
                algorithmCode,
                cancellationToken);

            if (!fileCreated)
            {
                return CreateFailedResult(backtestId, config, startedAt, "Failed to upload algorithm code");
            }

            // Step 5: Compile project
            _logger.LogInformation("Compiling project");
            var compileResponse = await _apiClient.CompileProjectAsync(projectId, cancellationToken);

            if (!compileResponse.Success)
            {
                var error = string.Join(", ", compileResponse.Errors);
                return CreateFailedResult(backtestId, config, startedAt, $"Compilation failed: {error}");
            }

            var compileId = compileResponse.CompileId;

            // Wait for compilation to complete
            var compileCompleted = false;
            var maxCompileAttempts = 30;
            for (int i = 0; i < maxCompileAttempts; i++)
            {
                await Task.Delay(2000, cancellationToken);
                var compileStatus = await _apiClient.ReadCompileAsync(projectId, compileId, cancellationToken);

                if (compileStatus.State == "BuildSuccess")
                {
                    compileCompleted = true;
                    _logger.LogInformation("Compilation successful");
                    break;
                }
                else if (compileStatus.State == "BuildError")
                {
                    var logs = string.Join("\n", compileStatus.Logs);
                    return CreateFailedResult(backtestId, config, startedAt, $"Compilation error: {logs}");
                }
            }

            if (!compileCompleted)
            {
                return CreateFailedResult(backtestId, config, startedAt, "Compilation timeout");
            }

            // Step 6: Create backtest
            _logger.LogInformation("Creating backtest");
            var backtestName = $"AlgoTrendy_Backtest_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            var backtestResponse = await _apiClient.CreateBacktestAsync(
                projectId,
                compileId,
                backtestName,
                null,
                cancellationToken);

            if (!backtestResponse.Success || backtestResponse.Backtest == null)
            {
                var error = string.Join(", ", backtestResponse.Errors);
                return CreateFailedResult(backtestId, config, startedAt, $"Failed to create backtest: {error}");
            }

            var qcBacktestId = backtestResponse.Backtest.BacktestId;
            _logger.LogInformation("Created backtest {BacktestId}", qcBacktestId);

            // Step 7: Poll for backtest completion
            _logger.LogInformation("Waiting for backtest to complete");
            var maxAttempts = 60; // 10 minutes max
            QCBacktestResponse? finalResult = null;

            for (int i = 0; i < maxAttempts; i++)
            {
                await Task.Delay(10000, cancellationToken); // Poll every 10 seconds
                var status = await _apiClient.ReadBacktestAsync(projectId, qcBacktestId, cancellationToken);

                if (status.Success && status.Backtest != null)
                {
                    var progress = status.Backtest.Progress;
                    _logger.LogInformation("Backtest progress: {Progress}%", progress);

                    if (status.Backtest.Completed)
                    {
                        finalResult = status;
                        _logger.LogInformation("Backtest completed successfully");
                        break;
                    }

                    if (!string.IsNullOrEmpty(status.Backtest.Error))
                    {
                        return CreateFailedResult(backtestId, config, startedAt, $"Backtest error: {status.Backtest.Error}");
                    }
                }
            }

            if (finalResult == null || finalResult.Backtest == null)
            {
                return CreateFailedResult(backtestId, config, startedAt, "Backtest timeout - did not complete within expected time");
            }

            // Step 8: Convert QuantConnect results to AlgoTrendy format
            var results = ConvertToBacktestResults(backtestId, config, finalResult.Backtest, startedAt);

            _logger.LogInformation("QuantConnect backtest completed successfully");
            return results;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Backtest cancelled");
            return CreateFailedResult(backtestId, config, startedAt, "Backtest was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running QuantConnect backtest");
            return CreateFailedResult(backtestId, config, startedAt, $"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Generate C# algorithm code for QuantConnect
    /// </summary>
    private string GenerateAlgorithmCode(BacktestConfig config)
    {
        var symbol = config.Symbol.Replace("USDT", "USD"); // Convert crypto format
        var startDate = config.StartDate.ToString("yyyy, M, d");
        var endDate = config.EndDate.ToString("yyyy, M, d");

        return $@"using System;
using System.Linq;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{{
    public class AlgoTrendyStrategy : QCAlgorithm
    {{
        private Symbol _symbol;
        private SimpleMovingAverage _fastSma;
        private SimpleMovingAverage _slowSma;
        private RelativeStrengthIndex _rsi;

        public override void Initialize()
        {{
            SetStartDate({startDate});
            SetEndDate({endDate});
            SetCash(100000);

            // Add the security
            _symbol = AddEquity(""{symbol}"", Resolution.{config.Timeframe}).Symbol;

            // Initialize indicators
            _fastSma = SMA(_symbol, 20, Resolution.{config.Timeframe});
            _slowSma = SMA(_symbol, 50, Resolution.{config.Timeframe});
            _rsi = RSI(_symbol, 14, MovingAverageType.Simple, Resolution.{config.Timeframe});

            // Set warmup period
            SetWarmUp(TimeSpan.FromDays(60));
        }}

        public override void OnData(Slice data)
        {{
            if (IsWarmingUp) return;
            if (!_fastSma.IsReady || !_slowSma.IsReady || !_rsi.IsReady) return;

            var holdings = Portfolio[_symbol].Quantity;

            // Entry logic: Golden Cross + RSI confirmation
            if (_fastSma > _slowSma && _rsi < 70 && holdings <= 0)
            {{
                SetHoldings(_symbol, 1.0);
            }}
            // Exit logic: Death Cross or overbought RSI
            else if ((_fastSma < _slowSma || _rsi > 70) && holdings > 0)
            {{
                Liquidate(_symbol);
            }}
        }}

        public override void OnEndOfAlgorithm()
        {{
            Debug(""Backtest completed successfully"");
        }}
    }}
}}";
    }

    /// <summary>
    /// Convert QuantConnect results to AlgoTrendy format
    /// </summary>
    private BacktestResults ConvertToBacktestResults(
        string backtestId,
        BacktestConfig config,
        QCBacktest qcBacktest,
        DateTime startedAt)
    {
        var metrics = new BacktestMetrics();

        // Convert trade statistics
        if (qcBacktest.TotalPerformance?.TradeStatistics != null)
        {
            var tradeStats = qcBacktest.TotalPerformance.TradeStatistics;
            metrics.TotalTrades = tradeStats.TotalNumberOfTrades;
            metrics.WinningTrades = tradeStats.NumberOfWinningTrades;
            metrics.LosingTrades = tradeStats.NumberOfLosingTrades;
            metrics.WinRate = tradeStats.WinRate;
            metrics.TotalReturn = tradeStats.TotalProfitLoss;
            metrics.AverageWin = tradeStats.AverageProfit;
            metrics.AverageLoss = tradeStats.AverageLoss;
            metrics.LargestWin = tradeStats.LargestProfit;
            metrics.LargestLoss = tradeStats.LargestLoss;
            metrics.ProfitFactor = tradeStats.ProfitLossRatio == 0 ? 0 : tradeStats.ProfitLossRatio;
        }

        // Convert portfolio statistics
        if (qcBacktest.TotalPerformance?.PortfolioStatistics != null)
        {
            var portfolioStats = qcBacktest.TotalPerformance.PortfolioStatistics;
            metrics.SharpeRatio = portfolioStats.SharpeRatio;
            metrics.MaxDrawdown = portfolioStats.Drawdown;
            metrics.TotalReturn = portfolioStats.TotalNetProfit;
            metrics.AnnualizedReturn = portfolioStats.CompoundingAnnualReturn;
        }

        // Convert trades
        var trades = new List<TradeResult>();
        if (qcBacktest.TotalPerformance?.ClosedTrades != null)
        {
            foreach (var qcTrade in qcBacktest.TotalPerformance.ClosedTrades)
            {
                // Determine trade direction
                var tradeDirection = qcTrade.Direction.ToLower() == "long" ? TradeDirection.Long : TradeDirection.Short;

                trades.Add(new TradeResult
                {
                    EntryTime = qcTrade.EntryTime,
                    ExitTime = qcTrade.ExitTime,
                    EntryPrice = qcTrade.EntryPrice,
                    ExitPrice = qcTrade.ExitPrice,
                    Quantity = qcTrade.Quantity,
                    Side = tradeDirection,
                    PnL = qcTrade.ProfitLoss,
                    PnLPercent = qcTrade.EntryPrice != 0
                        ? ((qcTrade.ExitPrice - qcTrade.EntryPrice) / qcTrade.EntryPrice * 100)
                        : 0,
                    ExitReason = qcTrade.ProfitLoss > 0 ? "Take Profit" : "Stop Loss"
                });
            }
        }

        return new BacktestResults
        {
            BacktestId = backtestId,
            Status = BacktestStatus.Completed,
            Config = config,
            Metrics = metrics,
            Trades = trades.Cast<TradeResult>().ToList(),
            EquityCurve = new List<EquityPoint>(),
            StartedAt = startedAt,
            CompletedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Create a failed backtest result
    /// </summary>
    private BacktestResults CreateFailedResult(
        string backtestId,
        BacktestConfig config,
        DateTime startedAt,
        string errorMessage)
    {
        return new BacktestResults
        {
            BacktestId = backtestId,
            Status = BacktestStatus.Failed,
            Config = config,
            ErrorMessage = errorMessage,
            StartedAt = startedAt,
            CompletedAt = DateTime.UtcNow
        };
    }
}
