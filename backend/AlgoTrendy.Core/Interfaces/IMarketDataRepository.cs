using AlgoTrendy.Core.Models;

namespace AlgoTrendy.Core.Interfaces;

/// <summary>
/// Repository interface for market data operations
/// </summary>
public interface IMarketDataRepository
{
    /// <summary>
    /// Inserts a single market data record
    /// </summary>
    Task<bool> InsertAsync(MarketData marketData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts multiple market data records in batch
    /// </summary>
    Task<int> InsertBatchAsync(IEnumerable<MarketData> marketDataList, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets market data for a specific symbol within a time range
    /// </summary>
    Task<IEnumerable<MarketData>> GetBySymbolAsync(
        string symbol,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest market data for a symbol
    /// </summary>
    Task<MarketData?> GetLatestAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest market data for multiple symbols
    /// </summary>
    Task<IReadOnlyDictionary<string, MarketData>> GetLatestBatchAsync(
        IEnumerable<string> symbols,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets aggregated market data (e.g., hourly, daily) for a symbol
    /// </summary>
    Task<IEnumerable<MarketData>> GetAggregatedAsync(
        string symbol,
        string interval, // "1h", "1d", "1w"
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if market data exists for a specific symbol and timestamp
    /// </summary>
    Task<bool> ExistsAsync(string symbol, DateTime timestamp, CancellationToken cancellationToken = default);
}
