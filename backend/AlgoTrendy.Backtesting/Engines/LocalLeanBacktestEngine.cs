using System.Diagnostics;
using System.Text.Json;
using AlgoTrendy.Backtesting.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlgoTrendy.Backtesting.Engines;

/// <summary>
/// Configuration for local LEAN engine
/// </summary>
public class LocalLeanConfig
{
    /// <summary>
    /// Path to LEAN algorithms directory (mounted in Docker)
    /// </summary>
    public string AlgorithmsPath { get; set; } = "/tmp/algotrendy/lean/algorithms";

    /// <summary>
    /// Path to LEAN results directory (mounted in Docker)
    /// </summary>
    public string ResultsPath { get; set; } = "/tmp/algotrendy/lean/results";

    /// <summary>
    /// Path to LEAN data directory (mounted in Docker)
    /// </summary>
    public string DataPath { get; set; } = "/tmp/algotrendy/lean/data";

    /// <summary>
    /// Docker container name for LEAN engine
    /// </summary>
    public string ContainerName { get; set; } = "algotrendy-lean";

    /// <summary>
    /// Docker image name
    /// </summary>
    public string ImageName { get; set; } = "algotrendy/lean:latest";

    /// <summary>
    /// Maximum execution time in minutes
    /// </summary>
    public int MaxExecutionMinutes { get; set; } = 30;

    /// <summary>
    /// Enable local LEAN engine
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Local LEAN backtesting engine - runs LEAN in Docker container
/// </summary>
public class LocalLeanBacktestEngine : IBacktestEngine
{
    private readonly LocalLeanConfig _config;
    private readonly ILogger<LocalLeanBacktestEngine> _logger;

    public string EngineName => "LocalLEAN";
    public string EngineDescription => "Local LEAN engine running in Docker - free, private, and fast";

    public LocalLeanBacktestEngine(
        IOptions<LocalLeanConfig> config,
        ILogger<LocalLeanBacktestEngine> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public (bool IsValid, string? ErrorMessage) ValidateConfig(BacktestConfig config)
    {
        if (!_config.Enabled)
            return (false, "Local LEAN engine is not enabled");

        if (string.IsNullOrWhiteSpace(config.Symbol))
            return (false, "Symbol is required");

        if (config.StartDate >= config.EndDate)
            return (false, "Start date must be before end date");

        if (config.EndDate > DateTime.UtcNow)
            return (false, "End date cannot be in the future");

        var timeSpan = config.EndDate - config.StartDate;
        if (timeSpan.TotalDays < 1)
            return (false, "Backtest period must be at least 1 day");

        // Check if Docker is available
        if (!IsDockerAvailable())
            return (false, "Docker is not available. Please ensure Docker is installed and running.");

        return (true, null);
    }

    /// <inheritdoc/>
    public async Task<BacktestResults> RunAsync(BacktestConfig config, CancellationToken cancellationToken = default)
    {
        var backtestId = Guid.NewGuid().ToString();
        var startedAt = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting local LEAN backtest for {Symbol}", config.Symbol);

            // Validate config
            var (isValid, errorMessage) = ValidateConfig(config);
            if (!isValid)
            {
                return CreateFailedResult(backtestId, config, startedAt, errorMessage!);
            }

            // Ensure directories exist
            EnsureDirectoriesExist();

            // Generate algorithm code
            var algorithmCode = GenerateAlgorithmCode(config);
            var algorithmPath = Path.Combine(_config.AlgorithmsPath, $"{backtestId}.cs");
            await File.WriteAllTextAsync(algorithmPath, algorithmCode, cancellationToken);

            _logger.LogInformation("Algorithm written to {Path}", algorithmPath);

            // Run LEAN in Docker
            var resultsPath = Path.Combine(_config.ResultsPath, backtestId);
            Directory.CreateDirectory(resultsPath);

            var dockerRunSuccess = await RunLeanInDockerAsync(backtestId, algorithmPath, resultsPath, cancellationToken);

            if (!dockerRunSuccess)
            {
                return CreateFailedResult(backtestId, config, startedAt, "LEAN execution failed");
            }

            // Parse results
            var results = await ParseResultsAsync(backtestId, config, resultsPath, startedAt, cancellationToken);

            _logger.LogInformation("Local LEAN backtest completed successfully");
            return results;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Backtest cancelled");
            return CreateFailedResult(backtestId, config, startedAt, "Backtest was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running local LEAN backtest");
            return CreateFailedResult(backtestId, config, startedAt, $"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if Docker is available
    /// </summary>
    private bool IsDockerAvailable()
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Ensure required directories exist
    /// </summary>
    private void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(_config.AlgorithmsPath);
        Directory.CreateDirectory(_config.ResultsPath);
        Directory.CreateDirectory(_config.DataPath);
    }

    /// <summary>
    /// Generate LEAN algorithm code
    /// </summary>
    private string GenerateAlgorithmCode(BacktestConfig config)
    {
        var symbol = config.Symbol.Replace("USDT", "USD");
        var startDate = config.StartDate.ToString("yyyy, M, d");
        var endDate = config.EndDate.ToString("yyyy, M, d");
        var initialCash = config.InitialCapital;

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
            SetCash({initialCash});

            // Add the security
            _symbol = AddCrypto(""{symbol}"", Resolution.{config.Timeframe}).Symbol;

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
                Debug($""BUY: SMA Fast={{_fastSma}}, SMA Slow={{_slowSma}}, RSI={{_rsi}}"");
            }}
            // Exit logic: Death Cross or overbought RSI
            else if ((_fastSma < _slowSma || _rsi > 70) && holdings > 0)
            {{
                Liquidate(_symbol);
                Debug($""SELL: SMA Fast={{_fastSma}}, SMA Slow={{_slowSma}}, RSI={{_rsi}}"");
            }}
        }}

        public override void OnEndOfAlgorithm()
        {{
            Debug(""Backtest completed successfully"");
            Debug($""Total Portfolio Value: {{Portfolio.TotalPortfolioValue}}"");
        }}
    }}
}}";
    }

    /// <summary>
    /// Run LEAN engine in Docker container
    /// </summary>
    private async Task<bool> RunLeanInDockerAsync(
        string backtestId,
        string algorithmPath,
        string resultsPath,
        CancellationToken cancellationToken)
    {
        try
        {
            var dockerArgs = $"run --rm " +
                $"-v {_config.AlgorithmsPath}:/algorithms " +
                $"-v {_config.ResultsPath}:/results " +
                $"-v {_config.DataPath}:/data " +
                $"--name {_config.ContainerName}-{backtestId} " +
                $"{_config.ImageName} " +
                $"--algorithm-location /algorithms/{backtestId}.cs " +
                $"--results-destination-folder /results/{backtestId}";

            _logger.LogInformation("Running Docker: docker {Args}", dockerArgs);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = dockerArgs,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            var timeout = TimeSpan.FromMinutes(_config.MaxExecutionMinutes);
            var completed = await Task.Run(() => process.WaitForExit((int)timeout.TotalMilliseconds), cancellationToken);

            if (!completed)
            {
                _logger.LogError("LEAN execution timeout after {Minutes} minutes", _config.MaxExecutionMinutes);
                try { process.Kill(); } catch { }
                return false;
            }

            var stdout = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();

            _logger.LogDebug("LEAN stdout: {Output}", stdout);
            if (!string.IsNullOrEmpty(stderr))
                _logger.LogWarning("LEAN stderr: {Error}", stderr);

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running LEAN in Docker");
            return false;
        }
    }

    /// <summary>
    /// Parse LEAN results from JSON files
    /// </summary>
    private async Task<BacktestResults> ParseResultsAsync(
        string backtestId,
        BacktestConfig config,
        string resultsPath,
        DateTime startedAt,
        CancellationToken cancellationToken)
    {
        try
        {
            // LEAN outputs results to a JSON file
            var resultFile = Path.Combine(resultsPath, "result.json");

            if (!File.Exists(resultFile))
            {
                _logger.LogError("Results file not found: {Path}", resultFile);
                return CreateFailedResult(backtestId, config, startedAt, "Results file not found");
            }

            var jsonContent = await File.ReadAllTextAsync(resultFile, cancellationToken);
            var leanResults = JsonSerializer.Deserialize<LeanBacktestResult>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (leanResults == null)
            {
                return CreateFailedResult(backtestId, config, startedAt, "Failed to parse results");
            }

            // Convert LEAN results to AlgoTrendy format
            return ConvertLeanResults(backtestId, config, leanResults, startedAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing LEAN results");
            return CreateFailedResult(backtestId, config, startedAt, $"Error parsing results: {ex.Message}");
        }
    }

    /// <summary>
    /// Convert LEAN results to AlgoTrendy format
    /// </summary>
    private BacktestResults ConvertLeanResults(
        string backtestId,
        BacktestConfig config,
        LeanBacktestResult leanResults,
        DateTime startedAt)
    {
        var metrics = new BacktestMetrics
        {
            TotalReturn = leanResults.TotalPerformance?.PortfolioStatistics?.TotalNetProfit ?? 0,
            AnnualizedReturn = leanResults.TotalPerformance?.PortfolioStatistics?.CompoundingAnnualReturn ?? 0,
            SharpeRatio = leanResults.TotalPerformance?.PortfolioStatistics?.SharpeRatio ?? 0,
            MaxDrawdown = leanResults.TotalPerformance?.PortfolioStatistics?.Drawdown ?? 0,
            TotalTrades = leanResults.TotalPerformance?.TradeStatistics?.TotalNumberOfTrades ?? 0,
            WinningTrades = leanResults.TotalPerformance?.TradeStatistics?.NumberOfWinningTrades ?? 0,
            LosingTrades = leanResults.TotalPerformance?.TradeStatistics?.NumberOfLosingTrades ?? 0,
            WinRate = leanResults.TotalPerformance?.TradeStatistics?.WinRate ?? 0,
            AverageWin = leanResults.TotalPerformance?.TradeStatistics?.AverageProfit ?? 0,
            AverageLoss = leanResults.TotalPerformance?.TradeStatistics?.AverageLoss ?? 0,
            ProfitFactor = leanResults.TotalPerformance?.TradeStatistics?.ProfitLossRatio ?? 0
        };

        return new BacktestResults
        {
            BacktestId = backtestId,
            Status = BacktestStatus.Completed,
            Config = config,
            Metrics = metrics,
            Trades = new List<TradeResult>(),
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

/// <summary>
/// LEAN backtest result structure (simplified)
/// </summary>
internal class LeanBacktestResult
{
    public LeanPerformance? TotalPerformance { get; set; }
}

internal class LeanPerformance
{
    public LeanPortfolioStatistics? PortfolioStatistics { get; set; }
    public LeanTradeStatistics? TradeStatistics { get; set; }
}

internal class LeanPortfolioStatistics
{
    public decimal TotalNetProfit { get; set; }
    public decimal CompoundingAnnualReturn { get; set; }
    public decimal SharpeRatio { get; set; }
    public decimal Drawdown { get; set; }
}

internal class LeanTradeStatistics
{
    public int TotalNumberOfTrades { get; set; }
    public int NumberOfWinningTrades { get; set; }
    public int NumberOfLosingTrades { get; set; }
    public decimal WinRate { get; set; }
    public decimal AverageProfit { get; set; }
    public decimal AverageLoss { get; set; }
    public decimal ProfitLossRatio { get; set; }
}
