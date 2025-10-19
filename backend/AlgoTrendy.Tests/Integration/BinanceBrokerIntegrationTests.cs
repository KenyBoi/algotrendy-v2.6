using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Models;
using AlgoTrendy.TradingEngine.Brokers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Sdk;
using Xunit.SkippableFact;

namespace AlgoTrendy.Tests.Integration;

/// <summary>
/// Integration tests for BinanceBroker (supports Binance and Binance US)
/// These tests require actual Binance Testnet API credentials
/// Set environment variables or user secrets:
/// - Binance__ApiKey
/// - Binance__ApiSecret
/// - Binance__UseTestnet=true (default)
/// - Binance__UseBinanceUS=true (for Binance US)
/// </summary>
[Collection("BinanceIntegration")]
public class BinanceBrokerIntegrationTests : IAsyncLifetime
{
    private readonly BinanceBroker _broker;
    private readonly ILogger<BinanceBroker> _logger;
    private bool _isConnected;

    public BinanceBrokerIntegrationTests()
    {
        // Get credentials from environment variables
        var apiKey = Environment.GetEnvironmentVariable("Binance__ApiKey");
        var apiSecret = Environment.GetEnvironmentVariable("Binance__ApiSecret");
        var useTestnet = Environment.GetEnvironmentVariable("Binance__UseTestnet") != "false";
        var useBinanceUS = Environment.GetEnvironmentVariable("Binance__UseBinanceUS") == "true";

        // Skip if credentials not available
        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
        {
            Skip.If(true, "Binance API credentials not found in environment variables");
        }

        var options = Options.Create(new BinanceOptions
        {
            UseTestnet = useTestnet,
            UseBinanceUS = useBinanceUS,
            ApiKey = apiKey!,
            ApiSecret = apiSecret!
        });

        _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<BinanceBroker>();
        _broker = new BinanceBroker(options, _logger);
    }

    public async Task InitializeAsync()
    {
        // Connect to Binance before running tests
        _isConnected = await _broker.ConnectAsync();
        if (!_isConnected)
        {
            throw new Exception("Failed to connect to Binance. Check your credentials and network connection.");
        }
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [SkippableFact]
    public async Task ConnectAsync_WithValidCredentials_Succeeds()
    {
        // Arrange & Act
        var result = await _broker.ConnectAsync();

        // Assert
        Assert.True(result);
    }

    [SkippableFact]
    public async Task GetBalanceAsync_WhenConnected_ReturnsBalance()
    {
        // Arrange
        Skip.If(!_isConnected, "Not connected to Binance");

        // Act
        var balance = await _broker.GetBalanceAsync("USDT");

        // Assert
        Assert.True(balance >= 0);
    }

    [SkippableFact]
    public async Task GetCurrentPriceAsync_ForBTCUSDT_ReturnsValidPrice()
    {
        // Arrange
        Skip.If(!_isConnected, "Not connected to Binance");
        var symbol = "BTCUSDT";

        // Act
        var price = await _broker.GetCurrentPriceAsync(symbol);

        // Assert
        Assert.True(price > 0);
        Assert.True(price > 10000); // BTC should be above $10k
        Assert.True(price < 1000000); // BTC should be below $1M (sanity check)
    }

    [SkippableFact]
    public async Task GetPositionsAsync_ForSpotTrading_ReturnsEmpty()
    {
        // Arrange
        Skip.If(!_isConnected, "Not connected to Binance");

        // Act
        var positions = await _broker.GetPositionsAsync();

        // Assert
        Assert.NotNull(positions);
        Assert.Empty(positions); // Spot trading doesn't have positions
    }

    [Fact(Skip = "Requires testnet with sufficient balance")]
    public async Task PlaceOrderAsync_MarketOrder_PlacesSuccessfully()
    {
        // WARNING: This test places actual orders on testnet
        // Only run if you have sufficient testnet balance

        // Arrange
        Skip.If(!_isConnected, "Not connected to Binance");

        var request = new OrderRequest
        {
            Symbol = "BTCUSDT",
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = 0.001m // Small test order
        };

        // Act
        var order = await _broker.PlaceOrderAsync(request);

        // Assert
        Assert.NotNull(order);
        Assert.NotNull(order.ExchangeOrderId);
        Assert.Equal("binance", order.Exchange);
        Assert.Equal("BTCUSDT", order.Symbol);
        Assert.True(order.Status == OrderStatus.Filled || order.Status == OrderStatus.Open);
    }

    [Fact(Skip = "Requires testnet with sufficient balance")]
    public async Task PlaceOrderAsync_LimitOrder_PlacesSuccessfully()
    {
        // WARNING: This test places actual orders on testnet
        // Only run if you have sufficient testnet balance

        // Arrange
        Skip.If(!_isConnected, "Not connected to Binance");

        var currentPrice = await _broker.GetCurrentPriceAsync("BTCUSDT");
        var limitPrice = currentPrice * 0.95m; // 5% below current price

        var request = new OrderRequest
        {
            Symbol = "BTCUSDT",
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Limit,
            Quantity = 0.001m,
            Price = limitPrice
        };

        // Act
        var order = await _broker.PlaceOrderAsync(request);

        // Assert
        Assert.NotNull(order);
        Assert.NotNull(order.ExchangeOrderId);
        Assert.Equal(OrderStatus.Open, order.Status);

        // Clean up - cancel the order
        try
        {
            await _broker.CancelOrderAsync(order.ExchangeOrderId!, "BTCUSDT");
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact(Skip = "Requires testnet with active order")]
    public async Task CancelOrderAsync_ActiveOrder_CancelsSuccessfully()
    {
        // WARNING: This test requires an active order to cancel
        // First create an order, then cancel it

        // Arrange
        Skip.If(!_isConnected, "Not connected to Binance");

        // First place a limit order
        var currentPrice = await _broker.GetCurrentPriceAsync("BTCUSDT");
        var limitPrice = currentPrice * 0.95m;

        var request = new OrderRequest
        {
            Symbol = "BTCUSDT",
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Limit,
            Quantity = 0.001m,
            Price = limitPrice
        };

        var placedOrder = await _broker.PlaceOrderAsync(request);

        // Act
        var cancelledOrder = await _broker.CancelOrderAsync(placedOrder.ExchangeOrderId!, "BTCUSDT");

        // Assert
        Assert.NotNull(cancelledOrder);
        Assert.Equal(OrderStatus.Cancelled, cancelledOrder.Status);
    }

    [Fact(Skip = "Requires testnet with filled order")]
    public async Task GetOrderStatusAsync_ExistingOrder_ReturnsStatus()
    {
        // WARNING: This test requires an existing order ID
        // Replace with actual order ID from your testnet account

        // Arrange
        Skip.If(!_isConnected, "Not connected to Binance");

        // You need to replace this with an actual order ID
        var exchangeOrderId = "12345678";
        var symbol = "BTCUSDT";

        // Act
        var order = await _broker.GetOrderStatusAsync(exchangeOrderId, symbol);

        // Assert
        Assert.NotNull(order);
        Assert.Equal(exchangeOrderId, order.ExchangeOrderId);
    }
}

/// <summary>
/// Helper class for skipping tests conditionally
/// </summary>
public static class Skip
{
    public static void If(bool condition, string reason)
    {
        if (condition)
        {
            throw new SkipException(reason);
        }
    }
}

public class SkipException : Exception
{
    public SkipException(string message) : base(message) { }
}
