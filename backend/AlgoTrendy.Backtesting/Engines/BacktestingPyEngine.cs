using AlgoTrendy.Backtesting.Models;
using AlgoTrendy.Backtesting.Services;
using Microsoft.Extensions.Logging;

namespace AlgoTrendy.Backtesting.Engines;

/// <summary>
/// Backtesting engine using Backtesting.py library via Python microservice
/// The 4th engine in AlgoTrendy's QUAD-ENGINE backtesting system
/// </summary>
public class BacktestingPyEngine : IBacktestEngine
{
    private readonly IBacktestingPyApiClient _apiClient;
    private readonly ILogger<BacktestingPyEngine> _logger;

    public BacktestingPyEngine(
        IBacktestingPyApiClient apiClient,
        ILogger<BacktestingPyEngine> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public string EngineName => "Backtesting.py";

    /// <inheritdoc />
    public string EngineDescription =>
        "Professional Python-based backtesting using Backtesting.py library. " +
        "Open-source, fast, and feature-rich with comprehensive metrics.";

    /// <inheritdoc />
    public (bool IsValid, string? ErrorMessage) ValidateConfig(BacktestConfig config)
    {
        if (config == null)
            return (false, "Config cannot be null");

        if (string.IsNullOrWhiteSpace(config.Symbol))
            return (false, "Symbol is required");

        if (config.StartDate >= config.EndDate)
            return (false, "Start date must be before end date");

        if (config.InitialCapital <= 0)
            return (false, "Initial capital must be greater than zero");

        var dateRange = (config.EndDate - config.StartDate).TotalDays;
        if (dateRange < 30)
            return (false, "Date range must be at least 30 days for meaningful backtesting");

        if (config.Commission < 0 || config.Commission > 0.1m)
            return (false, "Commission must be between 0 and 10%");

        return (true, null);
    }

    /// <inheritdoc />
    public async Task<BacktestResults> RunAsync(BacktestConfig config, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation(
                "Starting Backtesting.py engine: {Symbol} from {Start} to {End}",
                config.Symbol, config.StartDate, config.EndDate);

            // Validate config
            var (isValid, errorMessage) = ValidateConfig(config);
            if (!isValid)
            {
                _logger.LogError("Invalid backtest configuration: {Error}", errorMessage);
                return CreateErrorResult(config, startTime, errorMessage!);
            }

            // Check service health
            var isHealthy = await _apiClient.IsHealthyAsync(cancellationToken);
            if (!isHealthy)
            {
                _logger.LogError("Backtesting.py service is not healthy");
                return CreateErrorResult(
                    config,
                    startTime,
                    "Backtesting.py service is unavailable. Please ensure the service is running on port 5004.");
            }

            _logger.LogDebug("Backtesting.py service health check passed");

            // Run backtest via API
            var results = await _apiClient.RunBacktestAsync(config, cancellationToken);

            if (results == null)
            {
                _logger.LogError("Received null results from Backtesting.py service");
                return CreateErrorResult(config, startTime, "Service returned null results");
            }

            // Update execution time
            var endTime = DateTime.UtcNow;
            results.ExecutionTimeSeconds = (endTime - startTime).TotalSeconds;

            _logger.LogInformation(
                "Backtesting.py backtest completed: Return={Return:F2}%, Trades={Trades}, Time={Time:F1}s",
                results.Metrics?.TotalReturn ?? 0,
                results.Metrics?.TotalTrades ?? 0,
                results.ExecutionTimeSeconds);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running Backtesting.py backtest");
            return CreateErrorResult(config, startTime, $"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if Backtesting.py service is available
    /// </summary>
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _apiClient.IsHealthyAsync(cancellationToken);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get available strategies from Backtesting.py service
    /// </summary>
    public async Task<List<StrategyInfo>?> GetAvailableStrategiesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _apiClient.GetStrategiesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available strategies");
            return null;
        }
    }

    private BacktestResults CreateErrorResult(BacktestConfig config, DateTime startTime, string errorMessage)
    {
        return new BacktestResults
        {
            BacktestId = Guid.NewGuid().ToString(),
            Status = BacktestStatus.Failed,
            Config = config,
            StartedAt = startTime,
            CompletedAt = DateTime.UtcNow,
            ExecutionTimeSeconds = (DateTime.UtcNow - startTime).TotalSeconds,
            ErrorMessage = errorMessage,
            ErrorDetails = new Dictionary<string, object>
            {
                ["engine"] = "Backtesting.py",
                ["timestamp"] = DateTime.UtcNow.ToString("O")
            }
        };
    }
}
