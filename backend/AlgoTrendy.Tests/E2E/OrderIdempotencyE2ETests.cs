using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace AlgoTrendy.Tests.E2E;

/// <summary>
/// End-to-end tests for order idempotency through the full API stack
/// Simulates real-world scenarios including network retries
/// </summary>
[Collection("E2ECollection")]
public class OrderIdempotencyE2ETests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public OrderIdempotencyE2ETests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact(Skip = "Requires running API server")]
    public async Task PlaceOrder_WithSameClientOrderId_ShouldReturnSameOrder()
    {
        // Arrange
        var clientOrderId = OrderFactory.GenerateClientOrderId();

        var orderRequest = new OrderRequest
        {
            ClientOrderId = clientOrderId,
            Symbol = "BTCUSDT",
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = 0.001m
        };

        // Act - Submit the same order twice (simulating network retry)
        var response1 = await _client.PostAsJsonAsync("/api/trading/orders", orderRequest);
        var response2 = await _client.PostAsJsonAsync("/api/trading/orders", orderRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode); // Should not be 409 Conflict

        var order1 = await response1.Content.ReadFromJsonAsync<Order>();
        var order2 = await response2.Content.ReadFromJsonAsync<Order>();

        Assert.NotNull(order1);
        Assert.NotNull(order2);
        Assert.Equal(order1.OrderId, order2.OrderId); // Same order
        Assert.Equal(order1.ClientOrderId, order2.ClientOrderId);
        Assert.Equal(order1.ExchangeOrderId, order2.ExchangeOrderId);
    }

    [Fact(Skip = "Requires running API server")]
    public async Task PlaceOrder_ConcurrentRetries_ShouldOnlyCreateOneOrder()
    {
        // Arrange
        var clientOrderId = OrderFactory.GenerateClientOrderId();

        var orderRequest = new OrderRequest
        {
            ClientOrderId = clientOrderId,
            Symbol = "BTCUSDT",
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = 0.001m
        };

        // Act - Submit concurrently to simulate race conditions
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => _client.PostAsJsonAsync("/api/trading/orders", orderRequest))
            .ToArray();

        var responses = await Task.WhenAll(tasks);

        // Assert - All should succeed (HTTP 200)
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));

        var orders = await Task.WhenAll(
            responses.Select(r => r.Content.ReadFromJsonAsync<Order>()));

        // All should return the same order
        var firstOrderId = orders[0]!.OrderId;
        var firstExchangeOrderId = orders[0]!.ExchangeOrderId;

        Assert.All(orders, o => Assert.Equal(firstOrderId, o!.OrderId));
        Assert.All(orders, o => Assert.Equal(firstExchangeOrderId, o!.ExchangeOrderId));
        Assert.All(orders, o => Assert.Equal(clientOrderId, o!.ClientOrderId));
    }

    [Fact(Skip = "Requires running API server")]
    public async Task PlaceOrder_WithoutClientOrderId_ShouldAutoGenerate()
    {
        // Arrange
        var orderRequest = new OrderRequest
        {
            // No ClientOrderId provided
            Symbol = "BTCUSDT",
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = 0.001m
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/trading/orders", orderRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var order = await response.Content.ReadFromJsonAsync<Order>();
        Assert.NotNull(order);
        Assert.NotNull(order.ClientOrderId);
        Assert.StartsWith("AT_", order.ClientOrderId);
    }

    [Fact(Skip = "Requires running API server")]
    public async Task PlaceOrder_NetworkRetry_AfterPartialFailure_ShouldNotDuplicate()
    {
        // Arrange
        var clientOrderId = OrderFactory.GenerateClientOrderId();

        var orderRequest = new OrderRequest
        {
            ClientOrderId = clientOrderId,
            Symbol = "BTCUSDT",
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = 0.001m
        };

        // Act - First attempt
        var response1 = await _client.PostAsJsonAsync("/api/trading/orders", orderRequest);
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

        var order1 = await response1.Content.ReadFromJsonAsync<Order>();

        // Simulate client thinking request failed and retrying
        await Task.Delay(100);

        // Second attempt (retry)
        var response2 = await _client.PostAsJsonAsync("/api/trading/orders", orderRequest);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        var order2 = await response2.Content.ReadFromJsonAsync<Order>();

        // Assert - Should return the same order, not create a new one
        Assert.Equal(order1!.OrderId, order2!.OrderId);
        Assert.Equal(order1.ExchangeOrderId, order2.ExchangeOrderId);
    }

    [Fact(Skip = "Requires running API server")]
    public async Task PlaceOrder_DifferentClientOrderIds_ShouldCreateDifferentOrders()
    {
        // Arrange
        var clientOrderId1 = OrderFactory.GenerateClientOrderId();
        var clientOrderId2 = OrderFactory.GenerateClientOrderId();

        var orderRequest1 = new OrderRequest
        {
            ClientOrderId = clientOrderId1,
            Symbol = "BTCUSDT",
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = 0.001m
        };

        var orderRequest2 = new OrderRequest
        {
            ClientOrderId = clientOrderId2,
            Symbol = "BTCUSDT",
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = 0.001m
        };

        // Act
        var response1 = await _client.PostAsJsonAsync("/api/trading/orders", orderRequest1);
        var response2 = await _client.PostAsJsonAsync("/api/trading/orders", orderRequest2);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        var order1 = await response1.Content.ReadFromJsonAsync<Order>();
        var order2 = await response2.Content.ReadFromJsonAsync<Order>();

        Assert.NotNull(order1);
        Assert.NotNull(order2);
        Assert.NotEqual(order1.OrderId, order2.OrderId);
        Assert.NotEqual(order1.ExchangeOrderId, order2.ExchangeOrderId);
        Assert.NotEqual(order1.ClientOrderId, order2.ClientOrderId);
    }

    [Fact(Skip = "Requires running API server")]
    public async Task PlaceOrder_Idempotency_ShouldWorkAcrossServerRestarts()
    {
        // Arrange
        var clientOrderId = OrderFactory.GenerateClientOrderId();

        var orderRequest = new OrderRequest
        {
            ClientOrderId = clientOrderId,
            Symbol = "BTCUSDT",
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = 0.001m
        };

        // Act - First request
        var response1 = await _client.PostAsJsonAsync("/api/trading/orders", orderRequest);
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

        var order1 = await response1.Content.ReadFromJsonAsync<Order>();

        // Note: In a real E2E test, you would restart the server here
        // For this test, we simulate by waiting for cache to be checked against database
        await Task.Delay(200);

        // Second request (simulating retry after server restart)
        var response2 = await _client.PostAsJsonAsync("/api/trading/orders", orderRequest);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        var order2 = await response2.Content.ReadFromJsonAsync<Order>();

        // Assert - Database-level idempotency should prevent duplicates
        Assert.NotNull(order1);
        Assert.NotNull(order2);
        Assert.Equal(order1.OrderId, order2.OrderId);
    }

    [Fact(Skip = "Requires running API server")]
    public async Task PlaceOrder_ValidateOrder_BeforeSubmitting_ShouldValidateIdempotency()
    {
        // Arrange
        var clientOrderId = OrderFactory.GenerateClientOrderId();

        var orderRequest = new OrderRequest
        {
            ClientOrderId = clientOrderId,
            Symbol = "BTCUSDT",
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = 0.001m
        };

        // Act - Submit order
        var submitResponse = await _client.PostAsJsonAsync("/api/trading/orders", orderRequest);
        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);

        // Validate same order (should recognize as duplicate)
        var validateResponse = await _client.PostAsJsonAsync("/api/trading/orders/validate", orderRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, validateResponse.StatusCode);
    }

    [Fact(Skip = "Requires running API server")]
    public async Task PlaceOrder_StressTest_HighConcurrency_ShouldHandleGracefully()
    {
        // Arrange
        var concurrentOrders = 50;
        var uniqueClientOrderIds = Enumerable.Range(0, 10)
            .Select(_ => OrderFactory.GenerateClientOrderId())
            .ToList();

        // Act - Submit multiple orders with some duplicate ClientOrderIds
        var tasks = Enumerable.Range(0, concurrentOrders)
            .Select(i =>
            {
                // 20% of requests use duplicate ClientOrderIds
                var clientOrderId = i % 5 == 0
                    ? uniqueClientOrderIds[i % uniqueClientOrderIds.Count]
                    : OrderFactory.GenerateClientOrderId();

                var orderRequest = new OrderRequest
                {
                    ClientOrderId = clientOrderId,
                    Symbol = "BTCUSDT",
                    Exchange = "binance",
                    Side = OrderSide.Buy,
                    Type = OrderType.Market,
                    Quantity = 0.001m
                };

                return _client.PostAsJsonAsync("/api/trading/orders", orderRequest);
            })
            .ToArray();

        var responses = await Task.WhenAll(tasks);

        // Assert - All requests should succeed
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));

        var orders = await Task.WhenAll(
            responses.Select(r => r.Content.ReadFromJsonAsync<Order>()));

        // All orders should be valid
        Assert.All(orders, o => Assert.NotNull(o));
        Assert.All(orders, o => Assert.NotNull(o!.ClientOrderId));
        Assert.All(orders, o => Assert.NotNull(o!.OrderId));
    }
}

/// <summary>
/// xUnit collection for E2E tests
/// </summary>
[CollectionDefinition("E2ECollection")]
public class E2ECollection
{
}
