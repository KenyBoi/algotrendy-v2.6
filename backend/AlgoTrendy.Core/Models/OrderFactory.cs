using AlgoTrendy.Core.Enums;

namespace AlgoTrendy.Core.Models;

/// <summary>
/// Factory for creating Order instances with proper idempotency support
/// </summary>
public static class OrderFactory
{
    /// <summary>
    /// Generates a unique client order ID for idempotency
    /// Format: "AT_{timestamp}_{guid}"
    /// </summary>
    /// <returns>Unique client order ID</returns>
    public static string GenerateClientOrderId()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var guid = Guid.NewGuid().ToString("N"); // No hyphens
        return $"AT_{timestamp}_{guid}";
    }

    /// <summary>
    /// Creates a new Order with auto-generated IDs
    /// </summary>
    /// <param name="symbol">Trading symbol (e.g., "BTCUSDT")</param>
    /// <param name="exchange">Exchange name</param>
    /// <param name="side">Order side (Buy or Sell)</param>
    /// <param name="type">Order type (Market, Limit, etc.)</param>
    /// <param name="quantity">Order quantity</param>
    /// <param name="price">Limit price (optional)</param>
    /// <param name="stopPrice">Stop price (optional)</param>
    /// <param name="strategyId">Strategy ID (optional)</param>
    /// <param name="metadata">Additional metadata (optional)</param>
    /// <param name="clientOrderId">Client order ID (auto-generated if not provided)</param>
    /// <returns>New Order instance</returns>
    public static Order CreateOrder(
        string symbol,
        string exchange,
        OrderSide side,
        OrderType type,
        decimal quantity,
        decimal? price = null,
        decimal? stopPrice = null,
        string? strategyId = null,
        string? metadata = null,
        string? clientOrderId = null)
    {
        return new Order
        {
            OrderId = Guid.NewGuid().ToString(),
            ClientOrderId = clientOrderId ?? GenerateClientOrderId(),
            Symbol = symbol,
            Exchange = exchange,
            Side = side,
            Type = type,
            Status = OrderStatus.Pending,
            Quantity = quantity,
            FilledQuantity = 0,
            Price = price,
            StopPrice = stopPrice,
            StrategyId = strategyId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Metadata = metadata
        };
    }

    /// <summary>
    /// Creates an Order from an OrderRequest with auto-generated IDs
    /// </summary>
    /// <param name="request">Order request</param>
    /// <returns>New Order instance</returns>
    public static Order FromRequest(OrderRequest request)
    {
        return new Order
        {
            OrderId = Guid.NewGuid().ToString(),
            ClientOrderId = request.ClientOrderId ?? GenerateClientOrderId(),
            Symbol = request.Symbol,
            Exchange = request.Exchange,
            Side = request.Side,
            Type = request.Type,
            Status = OrderStatus.Pending,
            Quantity = request.Quantity,
            FilledQuantity = 0,
            Price = request.Price,
            StopPrice = request.StopPrice,
            StrategyId = request.StrategyId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Metadata = request.Metadata
        };
    }

    /// <summary>
    /// Ensures an Order has a valid ClientOrderId (generates if missing)
    /// </summary>
    /// <param name="order">Order to validate</param>
    /// <returns>Order with valid ClientOrderId</returns>
    public static Order EnsureClientOrderId(Order order)
    {
        if (string.IsNullOrWhiteSpace(order.ClientOrderId))
        {
            // Cannot modify init-only property, so return a new instance
            return new Order
            {
                OrderId = order.OrderId,
                ClientOrderId = GenerateClientOrderId(),
                ExchangeOrderId = order.ExchangeOrderId,
                Symbol = order.Symbol,
                Exchange = order.Exchange,
                Side = order.Side,
                Type = order.Type,
                Status = order.Status,
                Quantity = order.Quantity,
                FilledQuantity = order.FilledQuantity,
                Price = order.Price,
                StopPrice = order.StopPrice,
                AverageFillPrice = order.AverageFillPrice,
                StrategyId = order.StrategyId,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                SubmittedAt = order.SubmittedAt,
                ClosedAt = order.ClosedAt,
                Metadata = order.Metadata
            };
        }

        return order;
    }
}
