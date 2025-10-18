using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Models;

namespace AlgoTrendy.Core.Interfaces;

/// <summary>
/// Repository interface for order operations
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// Creates a new order
    /// </summary>
    Task<Order> CreateAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing order
    /// </summary>
    Task<Order> UpdateAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an order by its ID
    /// </summary>
    Task<Order?> GetByIdAsync(string orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an order by its exchange order ID
    /// </summary>
    Task<Order?> GetByExchangeOrderIdAsync(string exchangeOrderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all orders for a specific symbol
    /// </summary>
    Task<IEnumerable<Order>> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active orders (Open or PartiallyFilled)
    /// </summary>
    Task<IEnumerable<Order>> GetActiveOrdersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets orders by status
    /// </summary>
    Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets orders created within a time range
    /// </summary>
    Task<IEnumerable<Order>> GetByTimeRangeAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets orders by strategy ID
    /// </summary>
    Task<IEnumerable<Order>> GetByStrategyAsync(string strategyId, CancellationToken cancellationToken = default);
}
