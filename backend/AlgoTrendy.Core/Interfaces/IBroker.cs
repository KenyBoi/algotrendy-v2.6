using AlgoTrendy.Core.Models;

namespace AlgoTrendy.Core.Interfaces;

/// <summary>
/// Broker abstraction interface for multi-exchange trading
/// </summary>
public interface IBroker
{
    /// <summary>
    /// Broker name (binance, okx, coinbase, kraken, bybit)
    /// </summary>
    string BrokerName { get; }

    /// <summary>
    /// Connects to the broker API
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection successful</returns>
    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets account balance for a specific currency
    /// </summary>
    /// <param name="currency">Currency symbol (e.g., "USDT", "BTC")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Available balance</returns>
    Task<decimal> GetBalanceAsync(string currency = "USDT", CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active positions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active positions</returns>
    Task<IEnumerable<Position>> GetPositionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Places an order on the exchange
    /// </summary>
    /// <param name="request">Order request details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Order with exchange order ID</returns>
    Task<Order> PlaceOrderAsync(OrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an active order
    /// </summary>
    /// <param name="orderId">Exchange order ID to cancel</param>
    /// <param name="symbol">Trading symbol</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated order with cancelled status</returns>
    Task<Order> CancelOrderAsync(string orderId, string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of an order from the exchange
    /// </summary>
    /// <param name="orderId">Exchange order ID</param>
    /// <param name="symbol">Trading symbol</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Order with updated status</returns>
    Task<Order> GetOrderStatusAsync(string orderId, string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current market price for a symbol
    /// </summary>
    /// <param name="symbol">Trading symbol (e.g., "BTCUSDT")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current market price</returns>
    Task<decimal> GetCurrentPriceAsync(string symbol, CancellationToken cancellationToken = default);
}
