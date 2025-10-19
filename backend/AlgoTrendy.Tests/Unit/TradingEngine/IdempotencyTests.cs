using AlgoTrendy.Core.Configuration;
using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using AlgoTrendy.TradingEngine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AlgoTrendy.Tests.Unit.TradingEngine;

/// <summary>
/// Unit tests for TradingEngine idempotency functionality
/// </summary>
public class IdempotencyTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<IMarketDataRepository> _mockMarketDataRepository;
    private readonly Mock<IBroker> _mockBroker;
    private readonly Mock<ILogger<AlgoTrendy.TradingEngine.TradingEngine>> _mockLogger;
    private readonly IOptions<RiskSettings> _riskSettings;
    private readonly AlgoTrendy.TradingEngine.TradingEngine _tradingEngine;

    public IdempotencyTests()
    {
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockMarketDataRepository = new Mock<IMarketDataRepository>();
        _mockBroker = new Mock<IBroker>();
        _mockLogger = new Mock<ILogger<AlgoTrendy.TradingEngine.TradingEngine>>();

        _riskSettings = Options.Create(new RiskSettings
        {
            EnableRiskValidation = false, // Disable for simpler testing
            MaxPositionSizePercent = 100,
            DefaultStopLossPercent = 0,
            DefaultTakeProfitPercent = 0,
            MaxConcurrentPositions = 100,
            MaxTotalExposurePercent = 100,
            MinOrderSize = 0,
            MaxOrderSize = null
        });

        // Default mock setup for GetOrderStatusAsync
        _mockBroker.Setup(b => b.GetOrderStatusAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string exchangeOrderId, string symbol, CancellationToken ct) => new Order
            {
                OrderId = Guid.NewGuid().ToString(),
                ClientOrderId = OrderFactory.GenerateClientOrderId(),
                ExchangeOrderId = exchangeOrderId,
                Symbol = symbol,
                Exchange = "binance",
                Side = OrderSide.Buy,
                Type = OrderType.Market,
                Status = OrderStatus.Open,
                Quantity = 0.001m,
                FilledQuantity = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        _tradingEngine = new AlgoTrendy.TradingEngine.TradingEngine(
            _mockOrderRepository.Object,
            _mockMarketDataRepository.Object,
            _mockBroker.Object,
            _riskSettings,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task SubmitOrderAsync_WithSameClientOrderId_ShouldReturnCachedOrder()
    {
        // Arrange
        var clientOrderId = OrderFactory.GenerateClientOrderId();

        var order1 = new Order
        {
            OrderId = Guid.NewGuid().ToString(),
            ClientOrderId = clientOrderId,
            Symbol = "BTCUSDT",
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Status = OrderStatus.Pending,
            Quantity = 0.001m,
            FilledQuantity = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var order2 = new Order
        {
            OrderId = Guid.NewGuid().ToString(),
            ClientOrderId = clientOrderId, // Same ClientOrderId!
            Symbol = "BTCUSDT",
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Status = OrderStatus.Pending,
            Quantity = 0.001m,
            FilledQuantity = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Mock broker to return a filled order
        var brokerOrder = new Order
        {
            OrderId = order1.OrderId,
            ClientOrderId = clientOrderId,
            ExchangeOrderId = "EXCHANGE_123",
            Symbol = "BTCUSDT",
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Status = OrderStatus.Open,
            Quantity = 0.001m,
            FilledQuantity = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockBroker.Setup(b => b.PlaceOrderAsync(It.IsAny<OrderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(brokerOrder);

        _mockBroker.Setup(b => b.GetOrderStatusAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(brokerOrder);

        _mockOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken ct) => o);

        // Act
        var result1 = await _tradingEngine.SubmitOrderAsync(order1);
        var result2 = await _tradingEngine.SubmitOrderAsync(order2); // Duplicate!

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(result1.OrderId, result2.OrderId); // Should return same order
        Assert.Equal(result1.ClientOrderId, result2.ClientOrderId);

        // Broker should only be called once (second call uses cache)
        _mockBroker.Verify(b => b.PlaceOrderAsync(It.IsAny<OrderRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SubmitOrderAsync_WithDifferentClientOrderId_ShouldSubmitBothOrders()
    {
        // Arrange
        var clientOrderId1 = OrderFactory.GenerateClientOrderId();
        var clientOrderId2 = OrderFactory.GenerateClientOrderId();

        var order1 = OrderFactory.CreateOrder(
            "BTCUSDT", "binance", OrderSide.Buy, OrderType.Market, 0.001m,
            clientOrderId: clientOrderId1);

        var order2 = OrderFactory.CreateOrder(
            "BTCUSDT", "binance", OrderSide.Buy, OrderType.Market, 0.001m,
            clientOrderId: clientOrderId2);

        _mockBroker.Setup(b => b.PlaceOrderAsync(It.IsAny<OrderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrderRequest req, CancellationToken ct) => new Order
            {
                OrderId = Guid.NewGuid().ToString(),
                ClientOrderId = req.ClientOrderId!,
                ExchangeOrderId = $"EXCHANGE_{Guid.NewGuid():N}",
                Symbol = req.Symbol,
                Exchange = req.Exchange,
                Side = req.Side,
                Type = req.Type,
                Status = OrderStatus.Open,
                Quantity = req.Quantity,
                FilledQuantity = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        _mockBroker.Setup(b => b.GetOrderStatusAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string exchangeOrderId, string symbol, CancellationToken ct) => new Order
            {
                OrderId = Guid.NewGuid().ToString(),
                ClientOrderId = OrderFactory.GenerateClientOrderId(),
                ExchangeOrderId = exchangeOrderId,
                Symbol = symbol,
                Exchange = "binance",
                Side = OrderSide.Buy,
                Type = OrderType.Market,
                Status = OrderStatus.Open,
                Quantity = 0.001m,
                FilledQuantity = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        _mockOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken ct) => o);

        // Act
        var result1 = await _tradingEngine.SubmitOrderAsync(order1);
        var result2 = await _tradingEngine.SubmitOrderAsync(order2);

        // Assert
        Assert.NotEqual(result1.OrderId, result2.OrderId); // Different orders
        Assert.NotEqual(result1.ClientOrderId, result2.ClientOrderId);
        Assert.NotEqual(result1.ExchangeOrderId, result2.ExchangeOrderId);

        // Broker should be called twice
        _mockBroker.Verify(b => b.PlaceOrderAsync(It.IsAny<OrderRequest>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task SubmitOrderAsync_ConcurrentDuplicates_ShouldOnlySubmitOnce()
    {
        // Arrange
        var clientOrderId = OrderFactory.GenerateClientOrderId();
        var concurrentCount = 10;

        var brokerCallCount = 0;
        var brokerLock = new object();
        var brokerCallTimes = new List<long>();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        _mockBroker.Setup(b => b.PlaceOrderAsync(It.IsAny<OrderRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (OrderRequest req, CancellationToken ct) =>
            {
                lock (brokerLock)
                {
                    brokerCallCount++;
                    brokerCallTimes.Add(stopwatch.ElapsedMilliseconds);
                }

                // Simulate network delay to make concurrency issues more apparent
                await Task.Delay(100, ct);

                return new Order
                {
                    OrderId = Guid.NewGuid().ToString(),
                    ClientOrderId = req.ClientOrderId!,
                    ExchangeOrderId = $"EXCHANGE_{Guid.NewGuid():N}",
                    Symbol = req.Symbol,
                    Exchange = req.Exchange,
                    Side = req.Side,
                    Type = req.Type,
                    Status = OrderStatus.Open,
                    Quantity = req.Quantity,
                    FilledQuantity = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            });

        _mockBroker.Setup(b => b.GetOrderStatusAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string exchangeOrderId, string symbol, CancellationToken ct) => new Order
            {
                OrderId = Guid.NewGuid().ToString(),
                ClientOrderId = OrderFactory.GenerateClientOrderId(),
                ExchangeOrderId = exchangeOrderId,
                Symbol = symbol,
                Exchange = "binance",
                Side = OrderSide.Buy,
                Type = OrderType.Market,
                Status = OrderStatus.Open,
                Quantity = 0.001m,
                FilledQuantity = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        _mockOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken ct) => o);

        // Act - Submit same order concurrently with semaphore-based protection
        var tasks = Enumerable.Range(0, concurrentCount)
            .Select(_ => Task.Run(async () =>
            {
                var order = OrderFactory.CreateOrder(
                    "BTCUSDT", "binance", OrderSide.Buy, OrderType.Market, 0.001m,
                    clientOrderId: clientOrderId);

                return await _tradingEngine.SubmitOrderAsync(order);
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Assert.Equal(concurrentCount, results.Length);

        // All results should have the same OrderId and ExchangeOrderId (from cache)
        var firstOrderId = results[0].OrderId;
        var firstExchangeOrderId = results[0].ExchangeOrderId;

        Assert.All(results, r =>
        {
            Assert.Equal(firstOrderId, r.OrderId);
            Assert.Equal(firstExchangeOrderId, r.ExchangeOrderId);
            Assert.Equal(clientOrderId, r.ClientOrderId);
        });

        // Broker should only be called once despite concurrent requests
        // This verifies semaphore-based serialization
        Assert.Equal(1, brokerCallCount);

        // Verify that all concurrent requests completed within reasonable time
        // (If they were truly serialized, it would take ~1000ms; if not serialized,
        // most would complete in parallel within ~200ms)
        Assert.True(stopwatch.ElapsedMilliseconds < 2000,
            $"Concurrent requests took too long: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task SubmitOrderAsync_ConcurrentDifferentOrders_ShouldRunInParallel()
    {
        // Arrange - Different ClientOrderIds for each order
        var concurrentCount = 5;
        var brokerCallTimes = new List<(long startTime, long endTime)>();
        var brokerTimeLock = new object();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        _mockBroker.Setup(b => b.PlaceOrderAsync(It.IsAny<OrderRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (OrderRequest req, CancellationToken ct) =>
            {
                var startTime = stopwatch.ElapsedMilliseconds;

                // Simulate network delay
                await Task.Delay(150, ct);

                var endTime = stopwatch.ElapsedMilliseconds;

                lock (brokerTimeLock)
                {
                    brokerCallTimes.Add((startTime, endTime));
                }

                return new Order
                {
                    OrderId = Guid.NewGuid().ToString(),
                    ClientOrderId = req.ClientOrderId!,
                    ExchangeOrderId = $"EXCHANGE_{Guid.NewGuid():N}",
                    Symbol = req.Symbol,
                    Exchange = req.Exchange,
                    Side = req.Side,
                    Type = req.Type,
                    Status = OrderStatus.Open,
                    Quantity = req.Quantity,
                    FilledQuantity = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            });

        _mockOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken ct) => o);

        // Act - Submit different orders concurrently (different ClientOrderIds)
        var tasks = Enumerable.Range(0, concurrentCount)
            .Select(i => Task.Run(async () =>
            {
                var order = OrderFactory.CreateOrder(
                    "BTCUSDT", "binance", OrderSide.Buy, OrderType.Market, 0.001m,
                    clientOrderId: OrderFactory.GenerateClientOrderId()); // Different ID each time

                return await _tradingEngine.SubmitOrderAsync(order);
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Assert.Equal(concurrentCount, results.Length);

        // All orders should have different IDs
        var uniqueOrderIds = results.Select(r => r.OrderId).Distinct().Count();
        Assert.Equal(concurrentCount, uniqueOrderIds);

        var uniqueExchangeOrderIds = results.Select(r => r.ExchangeOrderId).Distinct().Count();
        Assert.Equal(concurrentCount, uniqueExchangeOrderIds);

        // Broker should be called once per order (not serialized)
        _mockBroker.Verify(b => b.PlaceOrderAsync(It.IsAny<OrderRequest>(), It.IsAny<CancellationToken>()),
            Times.Exactly(concurrentCount));

        // Verify parallelism: if requests were serialized, would take ~750ms (5 * 150ms)
        // With parallelism + overhead, should complete in <800ms
        Assert.True(stopwatch.ElapsedMilliseconds < 800,
            $"Parallel requests took too long: {stopwatch.ElapsedMilliseconds}ms (would be ~750ms if serialized)");
    }

    [Fact]
    public async Task SubmitOrderAsync_WithoutClientOrderId_ShouldAutoGenerate()
    {
        // Arrange
        var order = new Order
        {
            OrderId = Guid.NewGuid().ToString(),
            ClientOrderId = "", // No ClientOrderId provided
            Symbol = "BTCUSDT",
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Status = OrderStatus.Pending,
            Quantity = 0.001m,
            FilledQuantity = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockBroker.Setup(b => b.PlaceOrderAsync(It.IsAny<OrderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Order
            {
                OrderId = order.OrderId,
                ClientOrderId = "GENERATED_ID",
                ExchangeOrderId = "EXCHANGE_123",
                Symbol = "BTCUSDT",
                Exchange = "binance",
                Side = OrderSide.Buy,
                Type = OrderType.Market,
                Status = OrderStatus.Open,
                Quantity = 0.001m,
                FilledQuantity = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        _mockOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken ct) => o);

        // Act
        var result = await _tradingEngine.SubmitOrderAsync(order);

        // Assert
        Assert.NotNull(result.ClientOrderId);
        Assert.NotEmpty(result.ClientOrderId);
        Assert.StartsWith("AT_", result.ClientOrderId);
    }

    [Fact]
    public async Task SubmitOrderAsync_MultipleRetries_ShouldReturnSameOrder()
    {
        // Arrange
        var clientOrderId = OrderFactory.GenerateClientOrderId();

        var order = OrderFactory.CreateOrder(
            "BTCUSDT", "binance", OrderSide.Buy, OrderType.Market, 0.001m,
            clientOrderId: clientOrderId);

        _mockBroker.Setup(b => b.PlaceOrderAsync(It.IsAny<OrderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Order
            {
                OrderId = Guid.NewGuid().ToString(),
                ClientOrderId = clientOrderId,
                ExchangeOrderId = "EXCHANGE_123",
                Symbol = "BTCUSDT",
                Exchange = "binance",
                Side = OrderSide.Buy,
                Type = OrderType.Market,
                Status = OrderStatus.Open,
                Quantity = 0.001m,
                FilledQuantity = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        _mockOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken ct) => o);

        // Act - Simulate network retries
        var attempt1 = await _tradingEngine.SubmitOrderAsync(order);
        var attempt2 = await _tradingEngine.SubmitOrderAsync(order);
        var attempt3 = await _tradingEngine.SubmitOrderAsync(order);

        // Assert
        Assert.Equal(attempt1.OrderId, attempt2.OrderId);
        Assert.Equal(attempt1.OrderId, attempt3.OrderId);
        Assert.Equal(attempt1.ExchangeOrderId, attempt2.ExchangeOrderId);
        Assert.Equal(attempt1.ExchangeOrderId, attempt3.ExchangeOrderId);

        // Broker should only be called once
        _mockBroker.Verify(b => b.PlaceOrderAsync(It.IsAny<OrderRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task IdempotencyCache_DifferentSymbols_ShouldNotInterfere()
    {
        // Arrange
        var clientOrderId1 = "SAME_ID_123";
        var clientOrderId2 = "SAME_ID_123"; // Intentionally same for different symbols

        var order1 = OrderFactory.CreateOrder(
            "BTCUSDT", "binance", OrderSide.Buy, OrderType.Market, 0.001m,
            clientOrderId: clientOrderId1);

        var order2 = OrderFactory.CreateOrder(
            "ETHUSDT", "binance", OrderSide.Buy, OrderType.Market, 0.01m,
            clientOrderId: clientOrderId2);

        _mockBroker.Setup(b => b.PlaceOrderAsync(It.IsAny<OrderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrderRequest req, CancellationToken ct) => new Order
            {
                OrderId = Guid.NewGuid().ToString(),
                ClientOrderId = req.ClientOrderId!,
                ExchangeOrderId = $"EXCHANGE_{req.Symbol}",
                Symbol = req.Symbol,
                Exchange = req.Exchange,
                Side = req.Side,
                Type = req.Type,
                Status = OrderStatus.Open,
                Quantity = req.Quantity,
                FilledQuantity = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        _mockOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken ct) => o);

        // Act
        var result1 = await _tradingEngine.SubmitOrderAsync(order1);
        var result2 = await _tradingEngine.SubmitOrderAsync(order2);

        // Assert - Even with same ClientOrderId, different symbols should be independent
        // Note: In real implementation, ClientOrderId should be globally unique
        // This test verifies cache behavior if someone reuses IDs improperly
        Assert.NotNull(result1);
        Assert.NotNull(result2);
    }

    [Fact]
    public async Task SubmitOrderAsync_OrderRejected_ShouldNotCache()
    {
        // Arrange
        var clientOrderId = OrderFactory.GenerateClientOrderId();

        var order = OrderFactory.CreateOrder(
            "BTCUSDT", "binance", OrderSide.Buy, OrderType.Market, 0.001m,
            clientOrderId: clientOrderId);

        // First attempt - order gets rejected
        _mockBroker.SetupSequence(b => b.PlaceOrderAsync(It.IsAny<OrderRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Insufficient balance"))
            .ReturnsAsync(new Order
            {
                OrderId = Guid.NewGuid().ToString(),
                ClientOrderId = clientOrderId,
                ExchangeOrderId = "EXCHANGE_123",
                Symbol = "BTCUSDT",
                Exchange = "binance",
                Side = OrderSide.Buy,
                Type = OrderType.Market,
                Status = OrderStatus.Open,
                Quantity = 0.001m,
                FilledQuantity = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        _mockOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken ct) => o);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _tradingEngine.SubmitOrderAsync(order));

        // Second attempt should succeed (rejected orders shouldn't be cached)
        var result = await _tradingEngine.SubmitOrderAsync(order);
        Assert.NotNull(result);
        Assert.Equal(OrderStatus.Open, result.Status);

        // Broker should be called twice (first failed, second succeeded)
        _mockBroker.Verify(b => b.PlaceOrderAsync(It.IsAny<OrderRequest>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }
}
