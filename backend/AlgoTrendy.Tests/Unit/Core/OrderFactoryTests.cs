using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Models;
using Xunit;

namespace AlgoTrendy.Tests.Unit.Core;

/// <summary>
/// Unit tests for OrderFactory - validates ClientOrderId generation and order creation
/// </summary>
public class OrderFactoryTests
{
    [Fact]
    public void GenerateClientOrderId_ShouldReturnUniqueId()
    {
        // Act
        var id1 = OrderFactory.GenerateClientOrderId();
        var id2 = OrderFactory.GenerateClientOrderId();

        // Assert
        Assert.NotNull(id1);
        Assert.NotNull(id2);
        Assert.NotEqual(id1, id2); // Each ID should be unique
    }

    [Fact]
    public void GenerateClientOrderId_ShouldFollowCorrectFormat()
    {
        // Act
        var clientOrderId = OrderFactory.GenerateClientOrderId();

        // Assert
        Assert.StartsWith("AT_", clientOrderId);
        Assert.Contains("_", clientOrderId.Substring(3)); // Has underscore after prefix

        var parts = clientOrderId.Split('_');
        Assert.Equal(3, parts.Length); // Format: AT_{timestamp}_{guid}
        Assert.Equal("AT", parts[0]);

        // Second part should be a valid timestamp (numeric)
        Assert.True(long.TryParse(parts[1], out var timestamp));
        Assert.True(timestamp > 0);

        // Third part should be a valid GUID (32 hex characters without hyphens)
        Assert.Equal(32, parts[2].Length);
        Assert.Matches("^[a-f0-9]{32}$", parts[2]);
    }

    [Fact]
    public void GenerateClientOrderId_ShouldContainTimestamp()
    {
        // Arrange
        var beforeTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Act
        var clientOrderId = OrderFactory.GenerateClientOrderId();

        // Assert
        var afterTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var parts = clientOrderId.Split('_');
        var extractedTimestamp = long.Parse(parts[1]);

        // Timestamp should be between before and after
        Assert.InRange(extractedTimestamp, beforeTimestamp, afterTimestamp);
    }

    [Fact]
    public void GenerateClientOrderId_Concurrent_ShouldGenerateUniqueIds()
    {
        // Arrange
        var iterations = 1000;
        var clientOrderIds = new System.Collections.Concurrent.ConcurrentBag<string>();

        // Act - Generate IDs concurrently
        Parallel.For(0, iterations, _ =>
        {
            var id = OrderFactory.GenerateClientOrderId();
            clientOrderIds.Add(id);
        });

        // Assert - All IDs should be unique
        Assert.Equal(iterations, clientOrderIds.Count);
        Assert.Equal(iterations, clientOrderIds.Distinct().Count());
    }

    [Fact]
    public void CreateOrder_ShouldGenerateUniqueOrderId()
    {
        // Act
        var order1 = OrderFactory.CreateOrder(
            "BTCUSDT", "binance", OrderSide.Buy, OrderType.Market, 0.001m);

        var order2 = OrderFactory.CreateOrder(
            "BTCUSDT", "binance", OrderSide.Buy, OrderType.Market, 0.001m);

        // Assert
        Assert.NotEqual(order1.OrderId, order2.OrderId);
        Assert.NotEqual(order1.ClientOrderId, order2.ClientOrderId);
    }

    [Fact]
    public void CreateOrder_ShouldSetAllRequiredProperties()
    {
        // Arrange
        var symbol = "BTCUSDT";
        var exchange = "binance";
        var side = OrderSide.Buy;
        var type = OrderType.Limit;
        var quantity = 0.5m;
        var price = 50000m;
        var stopPrice = 49000m;
        var strategyId = "strategy_123";
        var metadata = "{\"test\":\"value\"}";

        // Act
        var order = OrderFactory.CreateOrder(
            symbol, exchange, side, type, quantity,
            price, stopPrice, strategyId, metadata);

        // Assert
        Assert.NotNull(order.OrderId);
        Assert.NotNull(order.ClientOrderId);
        Assert.Equal(symbol, order.Symbol);
        Assert.Equal(exchange, order.Exchange);
        Assert.Equal(side, order.Side);
        Assert.Equal(type, order.Type);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(quantity, order.Quantity);
        Assert.Equal(0, order.FilledQuantity);
        Assert.Equal(price, order.Price);
        Assert.Equal(stopPrice, order.StopPrice);
        Assert.Equal(strategyId, order.StrategyId);
        Assert.Equal(metadata, order.Metadata);
        Assert.True(order.CreatedAt <= DateTime.UtcNow);
        Assert.True(order.UpdatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void CreateOrder_WithClientOrderId_ShouldUseProvidedId()
    {
        // Arrange
        var customClientOrderId = "CUSTOM_12345_abcdef";

        // Act
        var order = OrderFactory.CreateOrder(
            "BTCUSDT", "binance", OrderSide.Buy, OrderType.Market, 0.001m,
            clientOrderId: customClientOrderId);

        // Assert
        Assert.Equal(customClientOrderId, order.ClientOrderId);
    }

    [Fact]
    public void CreateOrder_WithoutClientOrderId_ShouldAutoGenerate()
    {
        // Act
        var order = OrderFactory.CreateOrder(
            "BTCUSDT", "binance", OrderSide.Buy, OrderType.Market, 0.001m);

        // Assert
        Assert.NotNull(order.ClientOrderId);
        Assert.StartsWith("AT_", order.ClientOrderId);
    }

    [Fact]
    public void FromRequest_ShouldCreateOrderWithAllProperties()
    {
        // Arrange
        var request = new OrderRequest
        {
            ClientOrderId = "TEST_123_abc",
            Symbol = "ETHUSDT",
            Exchange = "binance",
            Side = OrderSide.Sell,
            Type = OrderType.Limit,
            Quantity = 1.5m,
            Price = 3000m,
            StopPrice = 2900m,
            StrategyId = "strat_456",
            Metadata = "{\"source\":\"api\"}"
        };

        // Act
        var order = OrderFactory.FromRequest(request);

        // Assert
        Assert.Equal(request.ClientOrderId, order.ClientOrderId);
        Assert.Equal(request.Symbol, order.Symbol);
        Assert.Equal(request.Exchange, order.Exchange);
        Assert.Equal(request.Side, order.Side);
        Assert.Equal(request.Type, order.Type);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(request.Quantity, order.Quantity);
        Assert.Equal(0, order.FilledQuantity);
        Assert.Equal(request.Price, order.Price);
        Assert.Equal(request.StopPrice, order.StopPrice);
        Assert.Equal(request.StrategyId, order.StrategyId);
        Assert.Equal(request.Metadata, order.Metadata);
    }

    [Fact]
    public void FromRequest_WithoutClientOrderId_ShouldAutoGenerate()
    {
        // Arrange
        var request = new OrderRequest
        {
            Symbol = "BTCUSDT",
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = 0.01m
        };

        // Act
        var order = OrderFactory.FromRequest(request);

        // Assert
        Assert.NotNull(order.ClientOrderId);
        Assert.StartsWith("AT_", order.ClientOrderId);
    }

    [Fact]
    public void EnsureClientOrderId_WithExistingId_ShouldReturnSameOrder()
    {
        // Arrange
        var existingOrder = OrderFactory.CreateOrder(
            "BTCUSDT", "binance", OrderSide.Buy, OrderType.Market, 0.001m);

        var originalClientOrderId = existingOrder.ClientOrderId;

        // Act
        var result = OrderFactory.EnsureClientOrderId(existingOrder);

        // Assert
        Assert.Equal(originalClientOrderId, result.ClientOrderId);
        Assert.Equal(existingOrder.OrderId, result.OrderId);
    }

    [Fact]
    public void EnsureClientOrderId_WithMissingId_ShouldGenerateNewId()
    {
        // Arrange - Create order with empty ClientOrderId (simulating old order)
        var order = new Order
        {
            OrderId = Guid.NewGuid().ToString(),
            ClientOrderId = "", // Empty - needs generation
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

        // Act
        var result = OrderFactory.EnsureClientOrderId(order);

        // Assert
        Assert.NotNull(result.ClientOrderId);
        Assert.StartsWith("AT_", result.ClientOrderId);
        Assert.NotEmpty(result.ClientOrderId);
    }

    [Fact]
    public void CreateOrder_MarketOrder_ShouldNotRequirePrice()
    {
        // Act
        var order = OrderFactory.CreateOrder(
            "BTCUSDT", "binance", OrderSide.Buy, OrderType.Market, 0.001m);

        // Assert
        Assert.Null(order.Price);
        Assert.Equal(OrderType.Market, order.Type);
    }

    [Fact]
    public void CreateOrder_LimitOrder_ShouldHavePrice()
    {
        // Act
        var order = OrderFactory.CreateOrder(
            "BTCUSDT", "binance", OrderSide.Buy, OrderType.Limit, 0.001m,
            price: 50000m);

        // Assert
        Assert.NotNull(order.Price);
        Assert.Equal(50000m, order.Price);
        Assert.Equal(OrderType.Limit, order.Type);
    }

    [Fact]
    public void CreateOrder_StopLossOrder_ShouldHaveStopPrice()
    {
        // Act
        var order = OrderFactory.CreateOrder(
            "BTCUSDT", "binance", OrderSide.Sell, OrderType.StopLoss, 0.001m,
            stopPrice: 49000m);

        // Assert
        Assert.NotNull(order.StopPrice);
        Assert.Equal(49000m, order.StopPrice);
        Assert.Equal(OrderType.StopLoss, order.Type);
    }

    [Fact]
    public void GenerateClientOrderId_Performance_ShouldBeEfficient()
    {
        // Arrange
        var iterations = 10000;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            _ = OrderFactory.GenerateClientOrderId();
        }

        stopwatch.Stop();

        // Assert - Should complete in less than 1 second
        Assert.True(stopwatch.ElapsedMilliseconds < 1000,
            $"Generated {iterations} IDs in {stopwatch.ElapsedMilliseconds}ms (expected < 1000ms)");
    }
}
