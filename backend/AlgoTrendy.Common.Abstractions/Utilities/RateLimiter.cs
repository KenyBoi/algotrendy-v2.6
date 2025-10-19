using System.Collections.Concurrent;

namespace AlgoTrendy.Common.Abstractions.Utilities;

/// <summary>
/// Thread-safe rate limiter with configurable limits per resource.
/// Eliminates duplicate rate limiting code across brokers and services.
/// </summary>
public class RateLimiter : IDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private readonly ConcurrentDictionary<string, DateTime> _lastRequestTime = new();
    private readonly int _minIntervalMs;
    private readonly int _maxConcurrentRequests;
    private bool _disposed;

    /// <summary>
    /// Creates a new rate limiter instance.
    /// </summary>
    /// <param name="maxConcurrentRequests">Maximum number of concurrent requests allowed</param>
    /// <param name="minIntervalMs">Minimum milliseconds between requests for the same resource</param>
    public RateLimiter(int maxConcurrentRequests, int minIntervalMs)
    {
        if (maxConcurrentRequests <= 0)
            throw new ArgumentException("Max concurrent requests must be positive", nameof(maxConcurrentRequests));
        if (minIntervalMs < 0)
            throw new ArgumentException("Min interval cannot be negative", nameof(minIntervalMs));

        _maxConcurrentRequests = maxConcurrentRequests;
        _minIntervalMs = minIntervalMs;
        _semaphore = new SemaphoreSlim(maxConcurrentRequests, maxConcurrentRequests);
    }

    /// <summary>
    /// Enforces rate limiting for a specific resource (e.g., symbol).
    /// </summary>
    /// <param name="resourceKey">Unique identifier for the resource (e.g., "BTC/USD")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that completes when the rate limit allows the request</returns>
    public async Task EnforceAsync(string resourceKey, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RateLimiter));

        // Global rate limiting (concurrent requests)
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            // Per-resource rate limiting (minimum interval)
            if (_minIntervalMs > 0)
            {
                var now = DateTime.UtcNow;

                if (_lastRequestTime.TryGetValue(resourceKey, out var lastTime))
                {
                    var elapsed = (now - lastTime).TotalMilliseconds;
                    var remaining = _minIntervalMs - elapsed;

                    if (remaining > 0)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(remaining), cancellationToken);
                        now = DateTime.UtcNow;
                    }
                }

                _lastRequestTime[resourceKey] = now;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Enforces global rate limiting only (ignores resource-specific limits).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task EnforceGlobalAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RateLimiter));

        await _semaphore.WaitAsync(cancellationToken);
        _semaphore.Release();
    }

    /// <summary>
    /// Clears rate limiting history for a specific resource.
    /// </summary>
    /// <param name="resourceKey">Resource to clear</param>
    public void ClearResource(string resourceKey)
    {
        _lastRequestTime.TryRemove(resourceKey, out _);
    }

    /// <summary>
    /// Clears all rate limiting history.
    /// </summary>
    public void ClearAll()
    {
        _lastRequestTime.Clear();
    }

    /// <summary>
    /// Gets configuration information for diagnostics.
    /// </summary>
    public (int MaxConcurrent, int MinIntervalMs, int TrackedResources) GetStats()
    {
        return (_maxConcurrentRequests, _minIntervalMs, _lastRequestTime.Count);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _semaphore?.Dispose();
        _lastRequestTime.Clear();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Factory for creating broker-specific rate limiters with preset configurations.
/// </summary>
public static class RateLimiterPresets
{
    /// <summary>
    /// Binance: 20 requests/second with 50ms minimum interval
    /// </summary>
    public static RateLimiter CreateBinanceRateLimiter()
        => new RateLimiter(maxConcurrentRequests: 20, minIntervalMs: 50);

    /// <summary>
    /// Bybit: 10 requests/second with 100ms minimum interval
    /// </summary>
    public static RateLimiter CreateBybitRateLimiter()
        => new RateLimiter(maxConcurrentRequests: 10, minIntervalMs: 100);

    /// <summary>
    /// Interactive Brokers: Conservative 5 requests/second with 200ms minimum interval
    /// </summary>
    public static RateLimiter CreateInteractiveBrokersRateLimiter()
        => new RateLimiter(maxConcurrentRequests: 5, minIntervalMs: 200);

    /// <summary>
    /// TradeStation: 10 requests/second with 100ms minimum interval
    /// </summary>
    public static RateLimiter CreateTradeStationRateLimiter()
        => new RateLimiter(maxConcurrentRequests: 10, minIntervalMs: 100);

    /// <summary>
    /// NinjaTrader: 10 requests/second with 100ms minimum interval
    /// </summary>
    public static RateLimiter CreateNinjaTraderRateLimiter()
        => new RateLimiter(maxConcurrentRequests: 10, minIntervalMs: 100);

    /// <summary>
    /// Alpha Vantage: 5 requests/minute (500/day limit handled separately)
    /// </summary>
    public static RateLimiter CreateAlphaVantageRateLimiter()
        => new RateLimiter(maxConcurrentRequests: 1, minIntervalMs: 12000); // 12 seconds between requests
}
