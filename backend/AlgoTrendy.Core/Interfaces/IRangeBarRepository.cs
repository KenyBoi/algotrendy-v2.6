using AlgoTrendy.Core.Models;

namespace AlgoTrendy.Core.Interfaces;

/// <summary>
/// Repository interface for range bar operations
/// Handles storage and retrieval of range-based bars
/// </summary>
public interface IRangeBarRepository
{
    /// <summary>
    /// Inserts a single range bar record
    /// </summary>
    Task<bool> InsertAsync(RangeBar rangeBar, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts multiple range bars in batch
    /// </summary>
    Task<int> InsertBatchAsync(IEnumerable<RangeBar> rangeBars, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets range bars for a specific symbol and range threshold within a time range
    /// </summary>
    Task<IEnumerable<RangeBar>> GetBySymbolAsync(
        string symbol,
        decimal rangeThreshold,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest range bars for a symbol and range threshold
    /// </summary>
    Task<IEnumerable<RangeBar>> GetLatestAsync(
        string symbol,
        decimal rangeThreshold,
        int count = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest range bar for a specific symbol and range threshold
    /// </summary>
    Task<RangeBar?> GetLatestSingleAsync(
        string symbol,
        decimal rangeThreshold,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available range thresholds for a symbol
    /// </summary>
    Task<IEnumerable<decimal>> GetAvailableRangeThresholdsAsync(
        string symbol,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets average bar duration for a symbol and range threshold
    /// Useful for understanding how quickly bars are forming
    /// </summary>
    Task<TimeSpan> GetAverageBarDurationAsync(
        string symbol,
        decimal rangeThreshold,
        int sampleSize = 100,
        CancellationToken cancellationToken = default);
}
