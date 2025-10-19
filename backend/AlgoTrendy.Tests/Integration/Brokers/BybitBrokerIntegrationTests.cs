using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Models;
using AlgoTrendy.TradingEngine.Brokers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace AlgoTrendy.Tests.Integration.Brokers;

/// <summary>
/// Integration tests for Bybit broker
/// These tests connect to Bybit testnet (requires API credentials)
/// </summary>
[Trait("Category", "Integration")]
[Trait("Broker", "Bybit")]
public class BybitBrokerIntegrationTests : IDisposable
{
    private readonly BybitBroker _broker;
    private readonly ITestOutputHelper _output;

    public BybitBrokerIntegrationTests(ITestOutputHelper output)
    {
        _output = output;

        // Get credentials from environment variables
        var apiKey = Environment.GetEnvironmentVariable("BYBIT_API_KEY");
        var apiSecret = Environment.GetEnvironmentVariable("BYBIT_API_SECRET");
        var useTestnet = Environment.GetEnvironmentVariable("BYBIT_TESTNET")?.ToLower() != "false";

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
        {
            throw new SkipException("Bybit API credentials not configured. Set BYBIT_API_KEY and BYBIT_API_SECRET environment variables.");
        }

        var options = Options.Create(new BybitOptions
        {
            ApiKey = apiKey,
            ApiSecret = apiSecret,
            UseTestnet = useTestnet
        });

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        var logger = loggerFactory.CreateLogger<BybitBroker>();

        _broker = new BybitBroker(options, logger);

        _output.WriteLine($"Testing Bybit broker on {(useTestnet ? "TESTNET" : "PRODUCTION")}");
    }

    [Fact]
    public async Task Connect_WithValidCredentials_Succeeds()
    {
        // Act
        var result = await _broker.ConnectAsync();

        // Assert
        Assert.True(result, "Failed to connect to Bybit");
        _output.WriteLine("✅ Successfully connected to Bybit");
    }

    [Fact]
    public async Task GetBalance_AfterConnection_ReturnsBalance()
    {
        // Arrange
        await _broker.ConnectAsync();

        // Act
        var balance = await _broker.GetBalanceAsync("USDT");

        // Assert
        Assert.True(balance >= 0, "Balance should be >= 0");
        _output.WriteLine($"✅ Balance: {balance} USDT");
    }

    [Fact]
    public async Task GetPositions_AfterConnection_ReturnsPositions()
    {
        // Arrange
        await _broker.ConnectAsync();

        // Act
        var positions = await _broker.GetPositionsAsync();

        // Assert
        Assert.NotNull(positions);
        _output.WriteLine($"✅ Retrieved {positions.Count()} position(s)");
    }

    [Fact]
    public async Task GetCurrentPrice_ForBTCUSDT_ReturnsValidPrice()
    {
        // Arrange
        await _broker.ConnectAsync();
        var symbol = "BTCUSDT";

        // Act
        var price = await _broker.GetCurrentPriceAsync(symbol);

        // Assert
        Assert.True(price > 0, $"Price for {symbol} should be > 0");
        _output.WriteLine($"✅ Current price for {symbol}: ${price}");
    }

    [Fact]
    public async Task PlaceOrder_LimitOrder_CreatesOrder()
    {
        // Arrange
        await _broker.ConnectAsync();

        var request = new OrderRequest
        {
            Symbol = "BTCUSDT",
            Exchange = "bybit",
            Side = OrderSide.Buy,
            Type = OrderType.Limit,
            Quantity = 0.001m, // Small test amount
            Price = 10000, // Far below market to avoid execution
            ClientOrderId = $"test_{Guid.NewGuid()}"
        };

        // Act
        var order = await _broker.PlaceOrderAsync(request);

        // Assert
        Assert.NotNull(order);
        Assert.NotNull(order.OrderId);
        Assert.Equal(OrderSide.Buy, order.Side);
        _output.WriteLine($"✅ Order placed: {order.OrderId}");

        // Cleanup: Cancel the order
        try
        {
            await _broker.CancelOrderAsync(order.OrderId, request.Symbol);
            _output.WriteLine($"✅ Order cancelled: {order.OrderId}");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"⚠️ Failed to cancel order: {ex.Message}");
        }
    }

    [Fact]
    public async Task SetLeverage_ForSymbol_Succeeds()
    {
        // Arrange
        await _broker.ConnectAsync();
        var symbol = "BTCUSDT";
        var leverage = 5m;

        // Act
        var result = await _broker.SetLeverageAsync(symbol, leverage, MarginType.Cross);

        // Assert
        Assert.True(result, "Failed to set leverage");
        _output.WriteLine($"✅ Leverage set to {leverage}x for {symbol}");
    }

    [Fact]
    public async Task GetLeverageInfo_ForSymbol_ReturnsInfo()
    {
        // Arrange
        await _broker.ConnectAsync();
        var symbol = "BTCUSDT";

        // Act
        var leverageInfo = await _broker.GetLeverageInfoAsync(symbol);

        // Assert
        Assert.NotNull(leverageInfo);
        _output.WriteLine($"✅ Leverage info: {leverageInfo.CurrentLeverage}x (max: {leverageInfo.MaxLeverage}x)");
    }

    [Fact]
    public async Task GetMarginHealthRatio_AfterConnection_ReturnsRatio()
    {
        // Arrange
        await _broker.ConnectAsync();

        // Act
        var healthRatio = await _broker.GetMarginHealthRatioAsync();

        // Assert
        Assert.True(healthRatio >= 0 && healthRatio <= 1.5m, "Health ratio should be between 0 and 1.5");
        _output.WriteLine($"✅ Margin health ratio: {healthRatio:P2}");
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}

/// <summary>
/// Exception to skip tests when credentials are not available
/// </summary>
public class SkipException : Exception
{
    public SkipException(string message) : base(message) { }
}
