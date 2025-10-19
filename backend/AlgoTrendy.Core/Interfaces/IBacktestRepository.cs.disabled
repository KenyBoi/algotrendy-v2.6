using AlgoTrendy.Backtesting.Models;

namespace AlgoTrendy.Core.Interfaces;

/// <summary>
/// Repository interface for persisting and retrieving backtest results
/// </summary>
public interface IBacktestRepository
{
    /// <summary>
    /// Save backtest results to database
    /// </summary>
    /// <param name="results">Backtest results to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Saved backtest ID</returns>
    Task<string> SaveAsync(BacktestResults results, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get backtest results by ID
    /// </summary>
    /// <param name="backtestId">Backtest ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Backtest results or null if not found</returns>
    Task<BacktestResults?> GetByIdAsync(string backtestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recent backtest results with pagination
    /// </summary>
    /// <param name="limit">Maximum number of results to return (default: 50)</param>
    /// <param name="offset">Number of results to skip (default: 0)</param>
    /// <param name="symbol">Optional symbol filter</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of backtest results</returns>
    Task<List<BacktestResults>> GetRecentAsync(
        int limit = 50,
        int offset = 0,
        string? symbol = null,
        BacktestStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get count of backtests matching criteria
    /// </summary>
    /// <param name="symbol">Optional symbol filter</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total count</returns>
    Task<int> GetCountAsync(
        string? symbol = null,
        BacktestStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete backtest results
    /// </summary>
    /// <param name="backtestId">Backtest ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAsync(string backtestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update backtest status (useful for long-running backtests)
    /// </summary>
    /// <param name="backtestId">Backtest ID</param>
    /// <param name="status">New status</param>
    /// <param name="errorMessage">Optional error message if status is Failed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated, false if not found</returns>
    Task<bool> UpdateStatusAsync(
        string backtestId,
        BacktestStatus status,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if backtest exists
    /// </summary>
    /// <param name="backtestId">Backtest ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> ExistsAsync(string backtestId, CancellationToken cancellationToken = default);
}
