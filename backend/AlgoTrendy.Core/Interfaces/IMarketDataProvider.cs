using AlgoTrendy.Core.Models;

namespace AlgoTrendy.Core.Interfaces;

/// <summary>
/// Interface for market data providers (historical and real-time data fetching)
/// </summary>
public interface IMarketDataProvider
{
    /// <summary>
    /// Name of the data provider (alphaVantage, yfinance, twelveData, etc.)
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Indicates if this is a free tier provider
    /// </summary>
    bool IsFreeTier { get; }

    /// <summary>
    /// Rate limit in requests per day (null if unlimited)
    /// </summary>
    int? DailyRateLimit { get; }

    /// <summary>
    /// Fetches historical market data for a symbol
    /// </summary>
    /// <param name="symbol">Trading symbol</param>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <param name="interval">Time interval (1d, 1h, 15m, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of OHLCV market data</returns>
    Task<IEnumerable<MarketData>> FetchHistoricalAsync(
        string symbol,
        DateTime startDate,
        DateTime endDate,
        string interval = "1d",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the latest market data for a symbol
    /// </summary>
    /// <param name="symbol">Trading symbol</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Latest market data</returns>
    Task<MarketData?> FetchLatestAsync(
        string symbol,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the provider supports a specific symbol
    /// </summary>
    /// <param name="symbol">Trading symbol</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if supported</returns>
    Task<bool> SupportsSymbolAsync(
        string symbol,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current API usage for the day
    /// </summary>
    /// <returns>Number of API calls made today</returns>
    Task<int> GetCurrentUsageAsync();

    /// <summary>
    /// Gets the remaining API calls available today
    /// </summary>
    /// <returns>Number of remaining calls (null if unlimited)</returns>
    Task<int?> GetRemainingCallsAsync();
}
