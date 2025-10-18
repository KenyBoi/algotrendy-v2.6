using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Moq;

namespace AlgoTrendy.Tests.TestHelpers.Fixtures;

/// <summary>
/// Provides a configured mock broker for testing
/// </summary>
public class MockBrokerFixture
{
    public Mock<IBroker> BrokerMock { get; }

    private readonly Dictionary<string, decimal> _balances = new();
    private readonly Dictionary<string, decimal> _prices = new();
    private readonly Dictionary<string, Order> _orders = new();
    private int _orderCounter = 1;

    public MockBrokerFixture()
    {
        BrokerMock = new Mock<IBroker>();
        SetupDefaultBehavior();
    }

    /// <summary>
    /// Sets up default mock behavior for common broker operations
    /// </summary>
    private void SetupDefaultBehavior()
    {
        // Default balance
        BrokerMock
            .Setup(b => b.GetBalanceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string currency, CancellationToken ct) =>
                _balances.TryGetValue(currency, out var balance) ? balance : 10000m);

        // Default price
        BrokerMock
            .Setup(b => b.GetCurrentPriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string symbol, CancellationToken ct) =>
                _prices.TryGetValue(symbol, out var price) ? price : 50000m);

        // Place order
        BrokerMock
            .Setup(b => b.PlaceOrderAsync(It.IsAny<OrderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrderRequest request, CancellationToken ct) =>
            {
                var exchangeOrderId = $"EXCH-{_orderCounter++}";
                var order = new Order
                {
                    OrderId = Guid.NewGuid().ToString(),
                    ExchangeOrderId = exchangeOrderId,
                    Symbol = request.Symbol,
                    Exchange = request.Exchange,
                    Side = request.Side,
                    Type = request.Type,
                    Status = request.Type == OrderType.Market ? OrderStatus.Filled : OrderStatus.Open,
                    Quantity = request.Quantity,
                    FilledQuantity = request.Type == OrderType.Market ? request.Quantity : 0,
                    Price = request.Price,
                    StopPrice = request.StopPrice,
                    AverageFillPrice = request.Type == OrderType.Market ? _prices.GetValueOrDefault(request.Symbol, 50000m) : null,
                    StrategyId = request.StrategyId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    SubmittedAt = DateTime.UtcNow,
                    ClosedAt = request.Type == OrderType.Market ? DateTime.UtcNow : null,
                    Metadata = request.Metadata
                };

                _orders[exchangeOrderId] = order;
                return order;
            });

        // Get order status
        BrokerMock
            .Setup(b => b.GetOrderStatusAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string exchangeOrderId, string symbol, CancellationToken ct) =>
            {
                if (_orders.TryGetValue(exchangeOrderId, out var order))
                {
                    return order;
                }

                throw new InvalidOperationException($"Order {exchangeOrderId} not found");
            });

        // Cancel order
        BrokerMock
            .Setup(b => b.CancelOrderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string exchangeOrderId, string symbol, CancellationToken ct) =>
            {
                if (_orders.TryGetValue(exchangeOrderId, out var order))
                {
                    order.Status = OrderStatus.Cancelled;
                    order.UpdatedAt = DateTime.UtcNow;
                    order.ClosedAt = DateTime.UtcNow;
                    return order;
                }

                throw new InvalidOperationException($"Order {exchangeOrderId} not found");
            });
    }

    /// <summary>
    /// Sets the balance for a specific currency
    /// </summary>
    public MockBrokerFixture WithBalance(string currency, decimal amount)
    {
        _balances[currency] = amount;
        return this;
    }

    /// <summary>
    /// Sets the current price for a specific symbol
    /// </summary>
    public MockBrokerFixture WithPrice(string symbol, decimal price)
    {
        _prices[symbol] = price;
        return this;
    }

    /// <summary>
    /// Simulates an order fill by updating its status
    /// </summary>
    public void FillOrder(string exchangeOrderId, decimal fillPrice)
    {
        if (_orders.TryGetValue(exchangeOrderId, out var order))
        {
            order.Status = OrderStatus.Filled;
            order.FilledQuantity = order.Quantity;
            order.AverageFillPrice = fillPrice;
            order.UpdatedAt = DateTime.UtcNow;
            order.ClosedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Simulates a partial order fill
    /// </summary>
    public void PartiallyFillOrder(string exchangeOrderId, decimal filledQuantity, decimal fillPrice)
    {
        if (_orders.TryGetValue(exchangeOrderId, out var order))
        {
            order.Status = OrderStatus.PartiallyFilled;
            order.FilledQuantity = filledQuantity;
            order.AverageFillPrice = fillPrice;
            order.UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Gets all placed orders
    /// </summary>
    public IReadOnlyCollection<Order> GetPlacedOrders()
    {
        return _orders.Values.ToList();
    }

    /// <summary>
    /// Resets the mock broker to initial state
    /// </summary>
    public void Reset()
    {
        _balances.Clear();
        _prices.Clear();
        _orders.Clear();
        _orderCounter = 1;
        BrokerMock.Reset();
        SetupDefaultBehavior();
    }
}
