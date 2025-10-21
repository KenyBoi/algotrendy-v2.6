using AlgoTrendy.Core.Models;

namespace AlgoTrendy.Core.Interfaces;

/// <summary>
/// Repository interface for tick bar operations
/// Handles storage and retrieval of volume-based bars (100-tick, 500-tick, 1000-tick, etc.)
/// </summary>
public interface ITickBarRepository
{
    /// <summary>
    /// Inserts a single tick bar record
    /// </summary>
    Task<bool> InsertAsync(TickBar tickBar, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts multiple tick bars in batch
    /// </summary>
    Task<int> InsertBatchAsync(IEnumerable<TickBar> tickBars, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tick bars for a specific symbol and tick size within a time range
    /// </summary>
    Task<IEnumerable<TickBar>> GetBySymbolAsync(
        string symbol,
        int tickSize,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest tick bars for a symbol and tick size
    /// </summary>
    Task<IEnumerable<TickBar>> GetLatestAsync(
        string symbol,
        int tickSize,
        int count = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest tick bar for a specific symbol and tick size
    /// </summary>
    Task<TickBar?> GetLatestSingleAsync(
        string symbol,
        int tickSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available tick sizes for a symbol
    /// </summary>
    Task<IEnumerable<int>> GetAvailableTickSizesAsync(
        string symbol,
        CancellationToken cancellationToken = default);
}
