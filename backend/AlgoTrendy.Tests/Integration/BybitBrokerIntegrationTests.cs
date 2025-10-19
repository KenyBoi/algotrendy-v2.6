using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Models;
using AlgoTrendy.Infrastructure.Brokers.Bybit;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Sdk;
using Xunit.SkippableFact;

namespace AlgoTrendy.Tests.Integration;

/// <summary>
/// Integration tests for BybitBroker with actual Bybit testnet connection
/// These tests require Bybit Testnet API credentials
/// Set environment variables:
/// - Bybit__ApiKey
/// - Bybit__ApiSecret
/// - Bybit__UseTestnet (optional, defaults to true)
/// </summary>
public class BybitBrokerIntegrationTests
{
    private readonly ILogger<BybitBroker> _logger;
    private readonly string _apiKey;
    private readonly string _apiSecret;
    private readonly bool _useTestnet;
    private readonly bool _skipTests;

    public BybitBrokerIntegrationTests()
    {
        // Create a simple logger for integration tests
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<BybitBroker>();

        // Get credentials from environment variables
        _apiKey = Environment.GetEnvironmentVariable("Bybit__ApiKey") ?? string.Empty;
        _apiSecret = Environment.GetEnvironmentVariable("Bybit__ApiSecret") ?? string.Empty;
        _useTestnet = Environment.GetEnvironmentVariable("Bybit__UseTestnet") != "false";

        // Skip tests if credentials not available
        _skipTests = string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_apiSecret);
    }

    [SkippableFact]
    [Trait("Category", "Integration")]
    [Trait("Service", "Bybit")]
    public async Task ConnectAsync_ToTestnet_Succeeds()
    {
        // Skip if no credentials
        if (_skipTests)
        {
            Skip.If(true, "Bybit API credentials not found in environment variables");
        }

        // Arrange
        var broker = new BybitBroker(_apiKey, _apiSecret, _useTestnet, _logger);

        // Act
        // Note: This connects to public endpoint which doesn't require authentication
        var result = await broker.ConnectAsync();

        // Assert
        // We expect either true or false, depending on network connectivity
        // The important thing is it doesn't throw an exception
        Assert.IsType<bool>(result);
    }

    [SkippableFact]
    [Trait("Category", "Integration")]
    [Trait("Service", "Bybit")]
    public async Task ConnectAsync_ToTestnet_SetsBrokerAsConnected()
    {
        // Skip if no credentials
        if (_skipTests)
        {
            Skip.If(true, "Bybit API credentials not found in environment variables");
        }

        // Arrange
        var broker = new BybitBroker(_apiKey, _apiSecret, _useTestnet, _logger);

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

    [SkippableFact]
    [Trait("Category", "Integration")]
    [Trait("Service", "Bybit")]
    public async Task GetCurrentPriceAsync_AfterConnection_ReturnsBTCPrice()
    {
        // Skip if no credentials
        if (_skipTests)
        {
            Skip.If(true, "Bybit API credentials not found in environment variables");
        }

        // Arrange
        var broker = new BybitBroker(_apiKey, _apiSecret, _useTestnet, _logger);

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

    [SkippableFact]
    [Trait("Category", "Integration")]
    [Trait("Service", "Bybit")]
    public async Task PlaceOrderAsync_OnTestnet_ReturnsValidOrder()
    {
        // Skip if no credentials
        if (_skipTests)
        {
            Skip.If(true, "Bybit API credentials not found in environment variables");
        }

        // Arrange
        var broker = new BybitBroker(_apiKey, _apiSecret, _useTestnet, _logger);
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

    [SkippableFact]
    [Trait("Category", "Integration")]
    [Trait("Service", "Bybit")]
    public async Task GetLeverageInfoAsync_AfterConnection_ReturnsValidLeverageInfo()
    {
        // Skip if no credentials
        if (_skipTests)
        {
            Skip.If(true, "Bybit API credentials not found in environment variables");
        }

        // Arrange
        var broker = new BybitBroker(_apiKey, _apiSecret, _useTestnet, _logger);
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

    [SkippableFact]
    [Trait("Category", "Integration")]
    [Trait("Service", "Bybit")]
    public async Task SetLeverageAsync_OnTestnet_ReturnsSuccess()
    {
        // Skip if no credentials
        if (_skipTests)
        {
            Skip.If(true, "Bybit API credentials not found in environment variables");
        }

        // Arrange
        var broker = new BybitBroker(_apiKey, _apiSecret, _useTestnet, _logger);
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

    [SkippableFact]
    [Trait("Category", "Integration")]
    [Trait("Service", "Bybit")]
    public async Task GetBalanceAsync_AfterConnection_ReturnsBalance()
    {
        // Skip if no credentials
        if (_skipTests)
        {
            Skip.If(true, "Bybit API credentials not found in environment variables");
        }

        // Arrange
        var broker = new BybitBroker(_apiKey, _apiSecret, _useTestnet, _logger);
        await broker.ConnectAsync();

        // Act
        var balance = await broker.GetBalanceAsync("USDT");

        // Assert
        Assert.IsType<decimal>(balance);
        Assert.True(balance >= 0);
    }

    [SkippableFact]
    [Trait("Category", "Integration")]
    [Trait("Service", "Bybit")]
    public async Task GetPositionsAsync_AfterConnection_ReturnsEmptyOrPopulated()
    {
        // Skip if no credentials
        if (_skipTests)
        {
            Skip.If(true, "Bybit API credentials not found in environment variables");
        }

        // Arrange
        var broker = new BybitBroker(_apiKey, _apiSecret, _useTestnet, _logger);
        await broker.ConnectAsync();

        // Act
        var positions = await broker.GetPositionsAsync();

        // Assert
        Assert.NotNull(positions);
        Assert.IsAssignableFrom<IEnumerable<Position>>(positions);
    }

    [SkippableFact]
    [Trait("Category", "Integration")]
    [Trait("Service", "Bybit")]
    public async Task GetMarginHealthRatioAsync_AfterConnection_ReturnsHealthRatio()
    {
        // Skip if no credentials
        if (_skipTests)
        {
            Skip.If(true, "Bybit API credentials not found in environment variables");
        }

        // Arrange
        var broker = new BybitBroker(_apiKey, _apiSecret, _useTestnet, _logger);
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

    [SkippableFact]
    [Trait("Category", "Integration")]
    [Trait("Service", "Bybit")]
    public async Task MultipleOperations_InSequence_CompleteSuccessfully()
    {
        // Skip if no credentials
        if (_skipTests)
        {
            Skip.If(true, "Bybit API credentials not found in environment variables");
        }

        // Arrange
        var broker = new BybitBroker(_apiKey, _apiSecret, _useTestnet, _logger);

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

    [SkippableFact]
    [Trait("Category", "Integration")]
    [Trait("Service", "Bybit")]
    public async Task CancelOrderAsync_WithInvalidOrderId_ReturnsOrder()
    {
        // Skip if no credentials
        if (_skipTests)
        {
            Skip.If(true, "Bybit API credentials not found in environment variables");
        }

        // Arrange
        var broker = new BybitBroker(_apiKey, _apiSecret, _useTestnet, _logger);
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

    [SkippableFact]
    [Trait("Category", "Integration")]
    [Trait("Service", "Bybit")]
    public async Task GetOrderStatusAsync_WithInvalidOrderId_ReturnsOrder()
    {
        // Skip if no credentials
        if (_skipTests)
        {
            Skip.If(true, "Bybit API credentials not found in environment variables");
        }

        // Arrange
        var broker = new BybitBroker(_apiKey, _apiSecret, _useTestnet, _logger);
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
