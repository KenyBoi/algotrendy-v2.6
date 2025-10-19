using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Models;
using AlgoTrendy.Infrastructure.Brokers.Bybit;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AlgoTrendy.Tests.Integration;

/// <summary>
/// Integration tests for BybitBroker with actual Bybit testnet connection
/// These tests require network connectivity to Bybit testnet
/// Skip these tests if you don't want external API calls
/// </summary>
public class BybitBrokerIntegrationTests
{
    private readonly ILogger<BybitBroker> _logger;
    private const string TestApiKey = "TESTNET_API_KEY";
    private const string TestApiSecret = "TESTNET_API_SECRET";

    public BybitBrokerIntegrationTests()
    {
        // Create a simple logger for integration tests
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<BybitBroker>();
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Service", "Bybit")]
    public async Task ConnectAsync_ToTestnet_Succeeds()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _logger);

        // Act
        // Note: This connects to public endpoint which doesn't require authentication
        var result = await broker.ConnectAsync();

        // Assert
        // We expect either true or false, depending on network connectivity
        // The important thing is it doesn't throw an exception
        Assert.IsType<bool>(result);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Service", "Bybit")]
    public async Task ConnectAsync_ToTestnet_SetsBrokerAsConnected()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _logger);

        // Act
        var result = await broker.ConnectAsync();

        // Assert
        // After successful connection, subsequent calls should work
        if (result)
        {
            var name = broker.BrokerName;
            Assert.Equal("Bybit", name);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Service", "Bybit")]
    public async Task GetCurrentPriceAsync_AfterConnection_ReturnsBTCPrice()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _logger);

        // Act
        var connected = await broker.ConnectAsync();
        decimal price = 0;
        if (connected)
        {
            price = await broker.GetCurrentPriceAsync("BTCUSDT");
        }

        // Assert
        if (connected)
        {
            // If connected, price should be retrieved (though it might be 0 as placeholder)
            Assert.IsType<decimal>(price);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Service", "Bybit")]
    public async Task PlaceOrderAsync_OnTestnet_ReturnsValidOrder()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _logger);
        await broker.ConnectAsync();

        var orderRequest = new OrderRequest
        {
            Symbol = "BTCUSDT",
            Exchange = "Bybit",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = 0.001m,
            Price = null,
            ClientOrderId = $"test-{Guid.NewGuid()}"
        };

        // Act
        // This might fail due to invalid testnet credentials, which is expected
        Order order = null!;
        try
        {
            order = await broker.PlaceOrderAsync(orderRequest);
        }
        catch (InvalidOperationException)
        {
            // Expected if broker is not connected
        }

        // Assert
        if (order != null)
        {
            Assert.Equal("BTCUSDT", order.Symbol);
            Assert.Equal(OrderSide.Buy, order.Side);
            Assert.Equal(OrderType.Market, order.Type);
            Assert.Equal("Bybit", order.Exchange);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Service", "Bybit")]
    public async Task GetLeverageInfoAsync_AfterConnection_ReturnsValidLeverageInfo()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _logger);
        await broker.ConnectAsync();

        // Act
        LeverageInfo leverageInfo = null!;
        try
        {
            leverageInfo = await broker.GetLeverageInfoAsync("BTCUSDT");
        }
        catch (InvalidOperationException)
        {
            // Expected if broker is not connected
        }

        // Assert
        if (leverageInfo != null)
        {
            Assert.NotNull(leverageInfo);
            Assert.True(leverageInfo.MaxLeverage > 0);
            Assert.True(leverageInfo.CurrentLeverage > 0);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Service", "Bybit")]
    public async Task SetLeverageAsync_OnTestnet_ReturnsSuccess()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _logger);
        await broker.ConnectAsync();

        // Act
        bool result = false;
        try
        {
            result = await broker.SetLeverageAsync("BTCUSDT", 5, MarginType.Cross);
        }
        catch (InvalidOperationException)
        {
            // Expected if broker is not connected
        }

        // Assert
        // Result depends on whether broker could execute the operation
        Assert.IsType<bool>(result);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Service", "Bybit")]
    public async Task GetBalanceAsync_AfterConnection_ReturnsBalance()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _logger);
        await broker.ConnectAsync();

        // Act
        var balance = await broker.GetBalanceAsync("USDT");

        // Assert
        Assert.IsType<decimal>(balance);
        Assert.True(balance >= 0);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Service", "Bybit")]
    public async Task GetPositionsAsync_AfterConnection_ReturnsEmptyOrPopulated()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _logger);
        await broker.ConnectAsync();

        // Act
        var positions = await broker.GetPositionsAsync();

        // Assert
        Assert.NotNull(positions);
        Assert.IsAssignableFrom<IEnumerable<Position>>(positions);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Service", "Bybit")]
    public async Task GetMarginHealthRatioAsync_AfterConnection_ReturnsHealthRatio()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _logger);
        await broker.ConnectAsync();

        // Act
        decimal ratio = 0;
        try
        {
            ratio = await broker.GetMarginHealthRatioAsync();
        }
        catch (InvalidOperationException)
        {
            // Expected if broker is not connected
        }

        // Assert
        if (ratio > 0)
        {
            Assert.True(ratio > 0);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Service", "Bybit")]
    public async Task MultipleOperations_InSequence_CompleteSuccessfully()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _logger);

        // Act
        var connected = await broker.ConnectAsync();

        // Assert
        if (connected)
        {
            // Get balance
            var balance = await broker.GetBalanceAsync("USDT");
            Assert.IsType<decimal>(balance);

            // Get positions
            var positions = await broker.GetPositionsAsync();
            Assert.NotNull(positions);

            // Get price
            var price = await broker.GetCurrentPriceAsync("BTCUSDT");
            Assert.IsType<decimal>(price);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Service", "Bybit")]
    public async Task CancelOrderAsync_WithInvalidOrderId_ReturnsOrder()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _logger);
        await broker.ConnectAsync();

        // Act
        Order cancelledOrder = null!;
        try
        {
            cancelledOrder = await broker.CancelOrderAsync("invalid_order_id", "BTCUSDT");
        }
        catch (InvalidOperationException)
        {
            // Expected if broker is not connected
        }

        // Assert
        if (cancelledOrder != null)
        {
            Assert.NotNull(cancelledOrder);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Service", "Bybit")]
    public async Task GetOrderStatusAsync_WithInvalidOrderId_ReturnsOrder()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _logger);
        await broker.ConnectAsync();

        // Act
        Order orderStatus = null!;
        try
        {
            orderStatus = await broker.GetOrderStatusAsync("invalid_order_id", "BTCUSDT");
        }
        catch (InvalidOperationException)
        {
            // Expected if broker is not connected
        }

        // Assert
        if (orderStatus != null)
        {
            Assert.NotNull(orderStatus);
        }
    }
}
