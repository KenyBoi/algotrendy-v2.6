using AlgoTrendy.Core.Models;

namespace AlgoTrendy.Core.Interfaces;

/// <summary>
/// Repository interface for tick data operations
/// Handles storage and retrieval of individual trade executions
/// </summary>
public interface ITickDataRepository
{
    /// <summary>
    /// Inserts a single tick data record
    /// </summary>
    Task<bool> InsertAsync(TickData tickData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts multiple tick data records in batch (high-performance)
    /// Critical for handling high-frequency tick streams
    /// </summary>
    Task<int> InsertBatchAsync(IEnumerable<TickData> tickDataList, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tick data for a specific symbol within a time range
    /// Warning: Can return very large datasets for active symbols
    /// </summary>
    Task<IEnumerable<TickData>> GetBySymbolAsync(
        string symbol,
        DateTime startTime,
        DateTime endTime,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest N ticks for a symbol
    /// Useful for recent market activity and order book pressure analysis
    /// </summary>
    Task<IEnumerable<TickData>> GetLatestAsync(
        string symbol,
        int count = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets aggregated tick statistics for a time period
    /// Returns buy/sell volume, tick counts, etc.
    /// </summary>
    Task<TickStatistics> GetStatisticsAsync(
        string symbol,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tick count for a symbol within a time range
    /// Useful for estimating optimal tick bar sizes
    /// </summary>
    Task<long> GetCountAsync(
        string symbol,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Aggregated statistics from tick data
/// </summary>
public class TickStatistics
{
    public required string Symbol { get; init; }
    public required DateTime StartTime { get; init; }
    public required DateTime EndTime { get; init; }
    public long TotalTicks { get; init; }
    public long BuyTicks { get; init; }
    public long SellTicks { get; init; }
    public decimal TotalVolume { get; init; }
    public decimal BuyVolume { get; init; }
    public decimal SellVolume { get; init; }
    public decimal HighPrice { get; init; }
    public decimal LowPrice { get; init; }
    public decimal VolumeDelta => BuyVolume - SellVolume;
    public decimal BuySellRatio => SellVolume > 0 ? BuyVolume / SellVolume : decimal.MaxValue;
}
