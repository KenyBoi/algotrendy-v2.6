using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using AlgoTrendy.Infrastructure.Repositories;
using Npgsql;
using Xunit;

namespace AlgoTrendy.Tests.Integration;

/// <summary>
/// Integration tests for order idempotency with PostgreSQL database
/// Tests database-level unique constraint enforcement
/// </summary>
[Collection("DatabaseCollection")] // Requires database
public class OrderIdempotencyIntegrationTests : IAsyncLifetime
{
    private const string TestConnectionString =
        "Host=localhost;Port=5432;Database=algotrendy_test;Username=postgres;Password=postgres";

    private IOrderRepository? _orderRepository;
    private NpgsqlConnection? _connection;

    public async Task InitializeAsync()
    {
        // Create test database connection
        _connection = new NpgsqlConnection(TestConnectionString);
        await _connection.OpenAsync();

        // Create orders table for testing
        await CreateTestTableAsync();

        // Initialize repository
        _orderRepository = new OrderRepository(TestConnectionString);
    }

    public async Task DisposeAsync()
    {
        // Clean up test data
        if (_connection != null)
        {
            await CleanupTestTableAsync();
            await _connection.DisposeAsync();
        }
    }

    [Fact(Skip = "Requires PostgreSQL database to be running")]
    public async Task CreateOrder_WithUniqueClientOrderId_ShouldSucceed()
    {
        // Arrange
        var order = OrderFactory.CreateOrder(
            "BTCUSDT", "binance", OrderSide.Buy, OrderType.Market, 0.001m);

        // Act
        var result = await _orderRepository!.CreateAsync(order);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(order.ClientOrderId, result.ClientOrderId);

        // Verify in database
        var retrieved = await _orderRepository.GetByClientOrderIdAsync(order.ClientOrderId);
        Assert.NotNull(retrieved);
        Assert.Equal(order.ClientOrderId, retrieved.ClientOrderId);
    }

    [Fact(Skip = "Requires PostgreSQL database to be running")]
    public async Task CreateOrder_WithDuplicateClientOrderId_ShouldThrowException()
    {
        // Arrange
        var clientOrderId = OrderFactory.GenerateClientOrderId();

        var order1 = OrderFactory.CreateOrder(
            "BTCUSDT", "binance", OrderSide.Buy, OrderType.Market, 0.001m,
            clientOrderId: clientOrderId);

        var order2 = OrderFactory.CreateOrder(
            "ETHUSDT", "binance", OrderSide.Buy, OrderType.Market, 0.01m,
            clientOrderId: clientOrderId); // Duplicate!

        // Act
        await _orderRepository!.CreateAsync(order1);

        // Assert - Second insert with same ClientOrderId should fail
        await Assert.ThrowsAsync<PostgresException>(async () =>
        {
            await _orderRepository.CreateAsync(order2);
        });
    }

    [Fact(Skip = "Requires PostgreSQL database to be running")]
    public async Task GetByClientOrderId_ExistingOrder_ShouldReturnOrder()
    {
        // Arrange
        var order = OrderFactory.CreateOrder(
            "BTCUSDT", "binance", OrderSide.Buy, OrderType.Market, 0.001m);

        await _orderRepository!.CreateAsync(order);

        // Act
        var result = await _orderRepository.GetByClientOrderIdAsync(order.ClientOrderId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(order.OrderId, result.OrderId);
        Assert.Equal(order.ClientOrderId, result.ClientOrderId);
        Assert.Equal(order.Symbol, result.Symbol);
        Assert.Equal(order.Quantity, result.Quantity);
    }

    [Fact(Skip = "Requires PostgreSQL database to be running")]
    public async Task GetByClientOrderId_NonExistentOrder_ShouldReturnNull()
    {
        // Arrange
        var nonExistentClientOrderId = "AT_9999999999999_nonexistent";

        // Act
        var result = await _orderRepository!.GetByClientOrderIdAsync(nonExistentClientOrderId);

        // Assert
        Assert.Null(result);
    }

    [Fact(Skip = "Requires PostgreSQL database to be running")]
    public async Task GetByClientOrderId_Performance_ShouldBeEfficient()
    {
        // Arrange - Create 1000 orders
        var orders = new List<Order>();
        for (int i = 0; i < 1000; i++)
        {
            var order = OrderFactory.CreateOrder(
                "BTCUSDT", "binance", OrderSide.Buy, OrderType.Market, 0.001m);
            await _orderRepository!.CreateAsync(order);
            orders.Add(order);
        }

        // Act - Query by ClientOrderId should use index
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

#pragma warning disable CS8602 // Dereference of a possibly null reference - false positive after null filtering
        foreach (var order in orders.Take(100).Where(o => o != null)) // Test 100 lookups
        {
            var result = await _orderRepository.GetByClientOrderIdAsync(order.ClientOrderId);
            Assert.NotNull(result);
        }
#pragma warning restore CS8602

        stopwatch.Stop();

        // Assert - 100 indexed lookups should be fast (< 1 second)
        Assert.True(stopwatch.ElapsedMilliseconds < 1000,
            $"100 lookups took {stopwatch.ElapsedMilliseconds}ms (expected < 1000ms with index)");
    }

    [Fact(Skip = "Requires PostgreSQL database to be running")]
    public async Task UniqueConstraint_ConcurrentInserts_OnlyOneSucceeds()
    {
        // Arrange
        var clientOrderId = OrderFactory.GenerateClientOrderId();
        var concurrentAttempts = 10;

        // Act - Try to insert same ClientOrderId concurrently
        var tasks = Enumerable.Range(0, concurrentAttempts)
            .Select(i => Task.Run(async () =>
            {
                var order = OrderFactory.CreateOrder(
                    "BTCUSDT", "binance", OrderSide.Buy, OrderType.Market, 0.001m,
                    clientOrderId: clientOrderId);

                try
                {
                    await _orderRepository!.CreateAsync(order);
                    return true; // Success
                }
                catch (PostgresException)
                {
                    return false; // Unique constraint violation
                }
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert - Only one should succeed
        var successCount = results.Count(r => r);
        Assert.Equal(1, successCount);
    }

    [Fact(Skip = "Requires PostgreSQL database to be running")]
    public async Task CreateOrder_WithAllProperties_ShouldPersistCorrectly()
    {
        // Arrange
        var order = OrderFactory.CreateOrder(
            symbol: "ETHUSDT",
            exchange: "binance",
            side: OrderSide.Sell,
            type: OrderType.Limit,
            quantity: 1.5m,
            price: 3000m,
            stopPrice: 2900m,
            strategyId: "strategy_123",
            metadata: "{\"test\":\"value\"}"
        );

        // Act
        await _orderRepository!.CreateAsync(order);
        var retrieved = await _orderRepository.GetByClientOrderIdAsync(order.ClientOrderId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(order.ClientOrderId, retrieved.ClientOrderId);
        Assert.Equal(order.Symbol, retrieved.Symbol);
        Assert.Equal(order.Exchange, retrieved.Exchange);
        Assert.Equal(order.Side, retrieved.Side);
        Assert.Equal(order.Type, retrieved.Type);
        Assert.Equal(order.Status, retrieved.Status);
        Assert.Equal(order.Quantity, retrieved.Quantity);
        Assert.Equal(order.Price, retrieved.Price);
        Assert.Equal(order.StopPrice, retrieved.StopPrice);
        Assert.Equal(order.StrategyId, retrieved.StrategyId);
        Assert.Equal(order.Metadata, retrieved.Metadata);
    }

    #region Helper Methods

    private async Task CreateTestTableAsync()
    {
        const string createTableSql = @"
            CREATE TABLE IF NOT EXISTS orders (
                order_id VARCHAR(50) PRIMARY KEY,
                client_order_id VARCHAR(100) NOT NULL UNIQUE,
                exchange_order_id VARCHAR(100),
                symbol VARCHAR(20) NOT NULL,
                exchange VARCHAR(20) NOT NULL,
                side VARCHAR(10) NOT NULL,
                type VARCHAR(20) NOT NULL,
                status VARCHAR(20) NOT NULL,
                quantity DECIMAL(18, 8) NOT NULL,
                filled_quantity DECIMAL(18, 8) NOT NULL DEFAULT 0,
                price DECIMAL(18, 8),
                stop_price DECIMAL(18, 8),
                average_fill_price DECIMAL(18, 8),
                strategy_id VARCHAR(50),
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                submitted_at TIMESTAMP,
                closed_at TIMESTAMP,
                metadata JSONB
            );

            CREATE INDEX IF NOT EXISTS idx_orders_client_order_id
            ON orders (client_order_id);
        ";

        await using var command = new NpgsqlCommand(createTableSql, _connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task CleanupTestTableAsync()
    {
        const string cleanupSql = @"
            DROP TABLE IF EXISTS orders CASCADE;
        ";

        await using var command = new NpgsqlCommand(cleanupSql, _connection);
        await command.ExecuteNonQueryAsync();
    }

    #endregion
}

/// <summary>
/// xUnit collection fixture for database tests
/// </summary>
[CollectionDefinition("DatabaseCollection")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}

/// <summary>
/// Fixture for database setup/teardown
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    public Task InitializeAsync()
    {
        // Database initialization (if needed)
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        // Database cleanup (if needed)
        return Task.CompletedTask;
    }
}
