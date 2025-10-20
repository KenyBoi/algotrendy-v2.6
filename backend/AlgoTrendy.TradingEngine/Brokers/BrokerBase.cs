using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Logging;

namespace AlgoTrendy.TradingEngine.Brokers;

/// <summary>
/// Base class for broker implementations
/// Provides common functionality for connection management, rate limiting, and logging
/// </summary>
public abstract class BrokerBase : IBroker
{
    protected readonly ILogger _logger;
    protected bool _isConnected = false;

    // Rate limiting - subclasses configure via constructor
    protected readonly SemaphoreSlim _rateLimiter;
    protected readonly Dictionary<string, DateTime> _lastRequestTime = new();
    protected readonly object _requestTimeLock = new();
    protected abstract int MinRequestIntervalMs { get; }

    /// <summary>
    /// Broker name (binance, bybit, coinbase, etc.)
    /// </summary>
    public abstract string BrokerName { get; }

    protected BrokerBase(ILogger logger, int maxConcurrentRequests = 10)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rateLimiter = new SemaphoreSlim(maxConcurrentRequests, maxConcurrentRequests);
    }

    /// <summary>
    /// Connects to the broker API
    /// Must be implemented by derived classes with broker-specific logic
    /// </summary>
    public abstract Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets account balance for a specific currency
    /// Must be implemented by derived classes with broker-specific logic
    /// </summary>
    public abstract Task<decimal> GetBalanceAsync(string currency = "USDT", CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active positions
    /// Must be implemented by derived classes with broker-specific logic
    /// </summary>
    public abstract Task<IEnumerable<Position>> GetPositionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Places an order on the exchange
    /// Must be implemented by derived classes with broker-specific logic
    /// </summary>
    public abstract Task<Order> PlaceOrderAsync(OrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an active order
    /// Must be implemented by derived classes with broker-specific logic
    /// </summary>
    public abstract Task<Order> CancelOrderAsync(string orderId, string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of an order from the exchange
    /// Must be implemented by derived classes with broker-specific logic
    /// </summary>
    public abstract Task<Order> GetOrderStatusAsync(string orderId, string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current market price for a symbol
    /// Must be implemented by derived classes with broker-specific logic
    /// </summary>
    public abstract Task<decimal> GetCurrentPriceAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets leverage for a symbol
    /// Must be implemented by derived classes with broker-specific logic
    /// </summary>
    public abstract Task<bool> SetLeverageAsync(
        string symbol,
        decimal leverage,
        MarginType marginType = MarginType.Cross,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current leverage information for a symbol
    /// Must be implemented by derived classes with broker-specific logic
    /// </summary>
    public abstract Task<LeverageInfo> GetLeverageInfoAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets margin health ratio for the account
    /// Must be implemented by derived classes with broker-specific logic
    /// </summary>
    public abstract Task<decimal> GetMarginHealthRatioAsync(CancellationToken cancellationToken = default);

    #region Common Helper Methods

    /// <summary>
    /// Ensures the broker is connected before performing operations
    /// </summary>
    protected void EnsureConnected()
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException($"Not connected to {BrokerName}. Call ConnectAsync first.");
        }
    }

    /// <summary>
    /// Enforces rate limiting to prevent API bans
    /// Uses semaphore for concurrent request limiting and time-based throttling per symbol
    /// </summary>
    /// <param name="symbol">Trading symbol</param>
    /// <param name="cancellationToken">Cancellation token</param>
    protected async Task EnforceRateLimitAsync(string symbol, CancellationToken cancellationToken)
    {
        // Acquire semaphore (limits concurrent requests)
        await _rateLimiter.WaitAsync(cancellationToken);

        try
        {
            // Check last request time for this symbol and enforce minimum interval
            lock (_requestTimeLock)
            {
                var now = DateTime.UtcNow;
                if (_lastRequestTime.TryGetValue(symbol, out var lastTime))
                {
                    var elapsedMs = (now - lastTime).TotalMilliseconds;
                    if (elapsedMs < MinRequestIntervalMs)
                    {
                        var delayMs = (int)(MinRequestIntervalMs - elapsedMs);
                        _logger.LogDebug(
                            "Rate limiting: delaying {DelayMs}ms for {Symbol} on {Broker}",
                            delayMs, symbol, BrokerName);
                        Task.Delay(delayMs, cancellationToken).Wait(cancellationToken);
                    }
                }
                _lastRequestTime[symbol] = DateTime.UtcNow;
            }
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    /// <summary>
    /// Simple rate limiting without symbol-specific throttling
    /// Useful for account-level operations
    /// </summary>
    protected async Task EnforceRateLimitAsync(CancellationToken cancellationToken)
    {
        await _rateLimiter.WaitAsync(cancellationToken);
        try
        {
            await Task.Delay(MinRequestIntervalMs, cancellationToken);
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    #endregion
}
