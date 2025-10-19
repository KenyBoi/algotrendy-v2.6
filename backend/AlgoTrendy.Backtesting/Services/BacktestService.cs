using AlgoTrendy.Backtesting.Engines;
using AlgoTrendy.Backtesting.Models;
using Microsoft.Extensions.Logging;

namespace AlgoTrendy.Backtesting.Services;

/// <summary>
/// Service for managing backtests
/// </summary>
public interface IBacktestService
{
    /// <summary>
    /// Run a backtest with the given configuration
    /// </summary>
    Task<BacktestResults> RunBacktestAsync(BacktestConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get backtest results by ID
    /// </summary>
    Task<BacktestResults?> GetBacktestResultsAsync(string backtestId);

    /// <summary>
    /// Get backtest history (summary)
    /// </summary>
    Task<List<BacktestHistoryItem>> GetBacktestHistoryAsync(int limit = 50);

    /// <summary>
    /// Delete a backtest
    /// </summary>
    Task<bool> DeleteBacktestAsync(string backtestId);

    /// <summary>
    /// Get available configuration options for UI
    /// </summary>
    Task<BacktestConfigOptions> GetConfigOptionsAsync();
}

/// <summary>
/// Implementation of backtest service
/// </summary>
public class BacktestService : IBacktestService
{
    private readonly IBacktestEngine _engine;
    private readonly ILogger<BacktestService> _logger;
    private readonly Dictionary<string, BacktestResults> _backtestCache;
    private readonly Dictionary<string, DateTime> _backtestTimestamps;

    /// <summary>
    /// Create backtest service
    /// </summary>
    public BacktestService(
        IBacktestEngine engine,
        ILogger<BacktestService> logger)
    {
        _engine = engine;
        _logger = logger;
        _backtestCache = new Dictionary<string, BacktestResults>();
        _backtestTimestamps = new Dictionary<string, DateTime>();
    }

    /// <inheritdoc/>
    public async Task<BacktestResults> RunBacktestAsync(BacktestConfig config, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Running backtest for {Symbol} ({AssetClass})", config.Symbol, config.AssetClass);

        try
        {
            // Validate config
            var (isValid, errorMessage) = config.Validate();
            if (!isValid)
            {
                _logger.LogWarning("Invalid backtest configuration: {ErrorMessage}", errorMessage);
                return new BacktestResults
                {
                    BacktestId = Guid.NewGuid().ToString(),
                    Status = BacktestStatus.Failed,
                    Config = config,
                    ErrorMessage = errorMessage,
                    StartedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow
                };
            }

            // Run backtest
            var results = await _engine.RunAsync(config, cancellationToken);

            // Cache results
            if (results.Status == BacktestStatus.Completed)
            {
                _backtestCache[results.BacktestId] = results;
                _backtestTimestamps[results.BacktestId] = DateTime.UtcNow;
                _logger.LogInformation("Backtest completed: {BacktestId}", results.BacktestId);
            }

            return results;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Backtest cancelled");
            return new BacktestResults
            {
                BacktestId = Guid.NewGuid().ToString(),
                Status = BacktestStatus.Failed,
                Config = config,
                ErrorMessage = "Backtest was cancelled",
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backtest failed with exception");
            return new BacktestResults
            {
                BacktestId = Guid.NewGuid().ToString(),
                Status = BacktestStatus.Failed,
                Config = config,
                ErrorMessage = $"Backtest failed: {ex.Message}",
                ErrorDetails = new Dictionary<string, object>
                {
                    { "exceptionType", ex.GetType().Name },
                    { "stackTrace", ex.StackTrace ?? string.Empty }
                },
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };
        }
    }

    /// <inheritdoc/>
    public async Task<BacktestResults?> GetBacktestResultsAsync(string backtestId)
    {
        _logger.LogInformation("Retrieving backtest results for {BacktestId}", backtestId);

        if (_backtestCache.TryGetValue(backtestId, out var results))
        {
            return await Task.FromResult(results);
        }

        _logger.LogWarning("Backtest not found: {BacktestId}", backtestId);
        return null;
    }

    /// <inheritdoc/>
    public async Task<List<BacktestHistoryItem>> GetBacktestHistoryAsync(int limit = 50)
    {
        _logger.LogInformation("Retrieving backtest history (limit: {Limit})", limit);

        var history = _backtestCache.Values
            .OrderByDescending(b => _backtestTimestamps.GetValueOrDefault(b.BacktestId, DateTime.MinValue))
            .Take(limit)
            .Select(b => new BacktestHistoryItem
            {
                BacktestId = b.BacktestId,
                Symbol = b.Config.Symbol,
                AssetClass = b.Config.AssetClass.ToString(),
                Timeframe = b.Config.Timeframe.ToString(),
                StartDate = b.Config.StartDate,
                EndDate = b.Config.EndDate,
                Status = b.Status,
                TotalReturn = b.Metrics?.TotalReturn,
                SharpeRatio = b.Metrics?.SharpeRatio,
                TotalTrades = b.Metrics?.TotalTrades,
                CreatedAt = _backtestTimestamps.GetValueOrDefault(b.BacktestId, DateTime.UtcNow)
            })
            .ToList();

        return await Task.FromResult(history);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteBacktestAsync(string backtestId)
    {
        _logger.LogInformation("Deleting backtest: {BacktestId}", backtestId);

        var removed = _backtestCache.Remove(backtestId);
        _backtestTimestamps.Remove(backtestId);

        if (removed)
        {
            _logger.LogInformation("Backtest deleted: {BacktestId}", backtestId);
        }
        else
        {
            _logger.LogWarning("Backtest not found for deletion: {BacktestId}", backtestId);
        }

        return await Task.FromResult(removed);
    }

    /// <inheritdoc/>
    public async Task<BacktestConfigOptions> GetConfigOptionsAsync()
    {
        _logger.LogDebug("Retrieving backtest configuration options");
        return await Task.FromResult(new BacktestConfigOptions());
    }
}
