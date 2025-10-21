using AlgoTrendy.Backtesting.Models;

namespace AlgoTrendy.Backtesting.Services;

/// <summary>
/// Interface for communicating with Backtesting.py microservice
/// </summary>
public interface IBacktestingPyApiClient
{
    /// <summary>
    /// Check if the Backtesting.py service is healthy
    /// </summary>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of available strategies from Backtesting.py
    /// </summary>
    Task<List<StrategyInfo>?> GetStrategiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Run a backtest using Backtesting.py
    /// </summary>
    Task<BacktestResults?> RunBacktestAsync(BacktestConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get backtest results by ID
    /// </summary>
    Task<BacktestResults?> GetResultsAsync(string backtestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get backtest history
    /// </summary>
    Task<List<BacktestSummary>?> GetHistoryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a backtest by ID
    /// </summary>
    Task<bool> DeleteBacktestAsync(string backtestId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Strategy information from Backtesting.py service
/// </summary>
public class StrategyInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, StrategyParameter> Parameters { get; set; } = new();
}

/// <summary>
/// Strategy parameter definition
/// </summary>
public class StrategyParameter
{
    public string Type { get; set; } = string.Empty;
    public object? Default { get; set; }
    public int[]? Range { get; set; }
}

/// <summary>
/// Backtest summary for history list
/// </summary>
public class BacktestSummary
{
    public string BacktestId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Strategy { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public decimal TotalReturn { get; set; }
    public string Timestamp { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
