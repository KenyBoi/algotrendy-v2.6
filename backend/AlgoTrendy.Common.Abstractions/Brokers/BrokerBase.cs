using AlgoTrendy.Common.Abstractions.Utilities;
using AlgoTrendy.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AlgoTrendy.Common.Abstractions.Brokers;

/// <summary>
/// Base class for all broker implementations.
/// Provides common functionality: connection management, rate limiting, validation, and logging.
/// Eliminates ~150 lines of duplicate code across 5 brokers.
/// </summary>
public abstract class BrokerBase : IBroker, IDisposable
{
    protected readonly ILogger Logger;
    protected readonly RateLimiter RateLimiter;
    protected bool IsConnected;
    protected bool Disposed;

    /// <summary>
    /// Gets the broker name (e.g., "Binance", "Bybit").
    /// </summary>
    public abstract string BrokerName { get; }

    protected BrokerBase(ILogger logger, RateLimiter rateLimiter)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        RateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
        IsConnected = false;
    }

    #region Connection Management

    /// <summary>
    /// Connects to the broker asynchronously.
    /// Derived classes should override ConnectInternalAsync to implement broker-specific logic.
    /// </summary>
    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogInformation("Connecting to {Broker}...", BrokerName);

            var result = await ConnectInternalAsync(cancellationToken);

            if (!result)
            {
                Logger.LogError("Failed to connect to {Broker}", BrokerName);
                IsConnected = false;
                return false;
            }

            IsConnected = true;
            Logger.LogInformation("Connected to {Broker} successfully", BrokerName);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Exception during {Broker} connection", BrokerName);
            IsConnected = false;
            return false;
        }
    }

    /// <summary>
    /// Disconnects from the broker asynchronously.
    /// Derived classes should override DisconnectInternalAsync to implement broker-specific logic.
    /// </summary>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogInformation("Disconnecting from {Broker}...", BrokerName);

            await DisconnectInternalAsync(cancellationToken);

            IsConnected = false;
            Logger.LogInformation("Disconnected from {Broker}", BrokerName);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error disconnecting from {Broker}", BrokerName);
            IsConnected = false;
        }
    }

    /// <summary>
    /// Derived classes implement broker-specific connection logic.
    /// </summary>
    protected abstract Task<bool> ConnectInternalAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Derived classes implement broker-specific disconnection logic.
    /// </summary>
    protected abstract Task DisconnectInternalAsync(CancellationToken cancellationToken);

    #endregion

    #region Rate Limiting & Validation

    /// <summary>
    /// Enforces rate limiting for the specified symbol.
    /// Uses the configured RateLimiter to prevent API throttling.
    /// </summary>
    protected async Task EnforceRateLimitAsync(string symbol, CancellationToken cancellationToken = default)
    {
        await RateLimiter.EnforceAsync(symbol, cancellationToken);
    }

    /// <summary>
    /// Enforces global rate limiting (no symbol-specific tracking).
    /// </summary>
    protected async Task EnforceGlobalRateLimitAsync(CancellationToken cancellationToken = default)
    {
        await RateLimiter.EnforceGlobalAsync(cancellationToken);
    }

    /// <summary>
    /// Ensures the broker is connected before executing operations.
    /// Throws InvalidOperationException if not connected.
    /// </summary>
    protected void EnsureConnected()
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException($"Not connected to {BrokerName}. Call ConnectAsync first.");
        }
    }

    /// <summary>
    /// Validates input parameters and throws ArgumentException if invalid.
    /// </summary>
    protected void ValidateSymbol(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol cannot be null or empty", nameof(symbol));
    }

    /// <summary>
    /// Validates order ID parameters.
    /// </summary>
    protected void ValidateOrderId(string orderId)
    {
        if (string.IsNullOrWhiteSpace(orderId))
            throw new ArgumentException("Order ID cannot be null or empty", nameof(orderId));
    }

    #endregion

    #region Logging Helpers

    /// <summary>
    /// Logs an operation start with standardized formatting.
    /// </summary>
    protected void LogOperationStart(string operation, string symbol)
    {
        Logger.LogInformation("{Broker}: {Operation} for {Symbol}", BrokerName, operation, symbol);
    }

    /// <summary>
    /// Logs an operation success with standardized formatting.
    /// </summary>
    protected void LogOperationSuccess(string operation, string symbol, string? details = null)
    {
        if (string.IsNullOrEmpty(details))
            Logger.LogInformation("{Broker}: {Operation} completed for {Symbol}", BrokerName, operation, symbol);
        else
            Logger.LogInformation("{Broker}: {Operation} completed for {Symbol} - {Details}", BrokerName, operation, symbol, details);
    }

    /// <summary>
    /// Logs an operation error with standardized formatting.
    /// </summary>
    protected void LogOperationError(Exception ex, string operation, string symbol)
    {
        Logger.LogError(ex, "{Broker}: Error during {Operation} for {Symbol}", BrokerName, operation, symbol);
    }

    #endregion

    #region Abstract IBroker Methods

    // Derived classes must implement these broker-specific operations
    public abstract Task<Core.Models.Order> PlaceOrderAsync(Core.Models.OrderRequest request, CancellationToken cancellationToken = default);
    public abstract Task<Core.Models.Order> CancelOrderAsync(string orderId, string symbol, CancellationToken cancellationToken = default);
    public abstract Task<Core.Models.Order> GetOrderStatusAsync(string orderId, string symbol, CancellationToken cancellationToken = default);
    public abstract Task<List<Core.Models.Order>> GetOpenOrdersAsync(string? symbol = null, CancellationToken cancellationToken = default);
    public abstract Task<List<Core.Models.Position>> GetPositionsAsync(CancellationToken cancellationToken = default);
    public abstract Task<Core.Models.Balance> GetBalanceAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Disposal

    protected virtual void Dispose(bool disposing)
    {
        if (Disposed)
            return;

        if (disposing)
        {
            RateLimiter?.Dispose();
            IsConnected = false;
        }

        Disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
