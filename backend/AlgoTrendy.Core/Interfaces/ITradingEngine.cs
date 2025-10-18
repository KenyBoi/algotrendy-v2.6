using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Models;

namespace AlgoTrendy.Core.Interfaces;

/// <summary>
/// Core trading engine interface for order management and execution
/// </summary>
public interface ITradingEngine
{
    /// <summary>
    /// Submits a new order to the trading engine
    /// </summary>
    /// <param name="order">Order to submit</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Submitted order with exchange order ID</returns>
    Task<Order> SubmitOrderAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an active order
    /// </summary>
    /// <param name="orderId">Order ID to cancel</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cancelled order</returns>
    Task<Order> CancelOrderAsync(string orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of an order from the exchange
    /// </summary>
    /// <param name="orderId">Order ID to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated order status</returns>
    Task<Order> GetOrderStatusAsync(string orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Syncs all active orders with the exchange
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of orders synced</returns>
    Task<int> SyncActiveOrdersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an order before submission (risk checks, balance checks, etc.)
    /// </summary>
    /// <param name="order">Order to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if order is valid, false otherwise</returns>
    Task<(bool IsValid, string? ErrorMessage)> ValidateOrderAsync(
        Order order,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets account balance for a specific currency
    /// </summary>
    /// <param name="exchange">Exchange name</param>
    /// <param name="currency">Currency symbol (e.g., "USDT", "BTC")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Available balance</returns>
    Task<decimal> GetBalanceAsync(
        string exchange,
        string currency,
        CancellationToken cancellationToken = default);
}
