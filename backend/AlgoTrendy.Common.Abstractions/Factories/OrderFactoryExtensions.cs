using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Models;

namespace AlgoTrendy.Common.Abstractions.Factories;

/// <summary>
/// Factory extensions for creating Order objects with common patterns.
/// Eliminates ~60 lines of duplicate order creation code across brokers.
/// </summary>
public static class OrderFactoryExtensions
{
    /// <summary>
    /// Creates a placeholder cancelled order when cancel confirmation doesn't return full order details.
    /// Used by brokers that only confirm cancellation without returning order data.
    /// </summary>
    /// <param name="orderId">The broker's order ID</param>
    /// <param name="clientOrderId">Client order ID (optional, defaults to orderId)</param>
    /// <param name="symbol">Trading symbol</param>
    /// <param name="exchange">Exchange/broker name</param>
    /// <returns>An Order object with Cancelled status and minimal known information</returns>
    public static Order CreateCancelledOrderPlaceholder(
        string orderId,
        string? clientOrderId,
        string symbol,
        string exchange)
    {
        return new Order
        {
            OrderId = orderId,
            ClientOrderId = clientOrderId ?? orderId, // Use orderId as fallback
            Symbol = symbol,
            Exchange = exchange,
            Side = OrderSide.Buy, // Unknown, using default
            Type = OrderType.Limit, // Unknown, using default
            Quantity = 0, // Unknown
            Status = OrderStatus.Cancelled,
            CreatedAt = DateTime.UtcNow, // Unknown, using current time
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a placeholder cancelled order with known side and type.
    /// </summary>
    public static Order CreateCancelledOrderPlaceholder(
        string orderId,
        string? clientOrderId,
        string symbol,
        string exchange,
        OrderSide side,
        OrderType type)
    {
        return new Order
        {
            OrderId = orderId,
            ClientOrderId = clientOrderId ?? orderId,
            Symbol = symbol,
            Exchange = exchange,
            Side = side,
            Type = type,
            Quantity = 0, // Unknown
            Status = OrderStatus.Cancelled,
            CreatedAt = DateTime.UtcNow, // Unknown, using current time
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a complete cancelled order with all known details.
    /// </summary>
    public static Order CreateCancelledOrder(
        string orderId,
        string? clientOrderId,
        string symbol,
        string exchange,
        OrderSide side,
        OrderType type,
        decimal quantity,
        decimal? price = null,
        DateTime? createdAt = null,
        DateTime? cancelledAt = null)
    {
        return new Order
        {
            OrderId = orderId,
            ClientOrderId = clientOrderId ?? orderId,
            Symbol = symbol,
            Exchange = exchange,
            Side = side,
            Type = type,
            Quantity = quantity,
            Price = price,
            Status = OrderStatus.Cancelled,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            UpdatedAt = cancelledAt ?? DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates an order from a broker-specific order object using a mapping function.
    /// Provides a standardized pattern for broker order conversion.
    /// </summary>
    public static Order CreateFromBrokerOrder<TBrokerOrder>(
        TBrokerOrder brokerOrder,
        string exchange,
        Func<TBrokerOrder, (
            string orderId,
            string? clientOrderId,
            string symbol,
            OrderSide side,
            OrderType type,
            OrderStatus status,
            decimal quantity,
            decimal? price,
            decimal? filledQuantity,
            decimal? averagePrice,
            DateTime? createdAt,
            DateTime? updatedAt
        )> mapFunction)
    {
        var mapped = mapFunction(brokerOrder);

        return new Order
        {
            OrderId = mapped.orderId,
            ClientOrderId = mapped.clientOrderId ?? mapped.orderId,
            Symbol = mapped.symbol,
            Exchange = exchange,
            Side = mapped.side,
            Type = mapped.type,
            Status = mapped.status,
            Quantity = mapped.quantity,
            Price = mapped.price,
            FilledQuantity = mapped.filledQuantity ?? 0,
            AveragePrice = mapped.averagePrice,
            CreatedAt = mapped.createdAt ?? DateTime.UtcNow,
            UpdatedAt = mapped.updatedAt ?? DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a Position from broker-specific position data.
    /// </summary>
    public static Position CreateFromBrokerPosition<TBrokerPosition>(
        TBrokerPosition brokerPosition,
        string exchange,
        Func<TBrokerPosition, (
            string symbol,
            decimal quantity,
            decimal entryPrice,
            decimal currentPrice,
            decimal unrealizedPnl,
            decimal? leverage,
            string? side
        )> mapFunction)
    {
        var mapped = mapFunction(brokerPosition);

        return new Position
        {
            Symbol = mapped.symbol,
            Exchange = exchange,
            Quantity = mapped.quantity,
            EntryPrice = mapped.entryPrice,
            CurrentPrice = mapped.currentPrice,
            UnrealizedPnl = mapped.unrealizedPnl,
            Leverage = mapped.leverage,
            Side = mapped.side ?? (mapped.quantity >= 0 ? "Long" : "Short")
        };
    }

    /// <summary>
    /// Updates an existing order with new status information.
    /// Preserves existing data while updating status-related fields.
    /// </summary>
    public static Order UpdateOrderStatus(
        Order existingOrder,
        OrderStatus newStatus,
        decimal? filledQuantity = null,
        decimal? averagePrice = null)
    {
        return new Order
        {
            OrderId = existingOrder.OrderId,
            ClientOrderId = existingOrder.ClientOrderId,
            Symbol = existingOrder.Symbol,
            Exchange = existingOrder.Exchange,
            Side = existingOrder.Side,
            Type = existingOrder.Type,
            Quantity = existingOrder.Quantity,
            Price = existingOrder.Price,
            Status = newStatus,
            FilledQuantity = filledQuantity ?? existingOrder.FilledQuantity,
            AveragePrice = averagePrice ?? existingOrder.AveragePrice,
            CreatedAt = existingOrder.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Validates that an order has all required fields populated.
    /// </summary>
    public static bool IsValid(Order order, out string? validationError)
    {
        if (string.IsNullOrWhiteSpace(order.OrderId))
        {
            validationError = "OrderId is required";
            return false;
        }

        if (string.IsNullOrWhiteSpace(order.Symbol))
        {
            validationError = "Symbol is required";
            return false;
        }

        if (order.Quantity <= 0)
        {
            validationError = "Quantity must be positive";
            return false;
        }

        validationError = null;
        return true;
    }
}
