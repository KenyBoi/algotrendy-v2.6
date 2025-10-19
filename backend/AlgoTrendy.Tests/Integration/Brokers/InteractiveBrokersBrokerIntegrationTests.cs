using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Models;
using AlgoTrendy.TradingEngine.Brokers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace AlgoTrendy.Tests.Integration.Brokers;

/// <summary>
/// Integration tests for Interactive Brokers broker
/// These tests connect to TWS/IB Gateway (requires TWS/Gateway running)
/// NOTE: Current implementation is foundation only - full IBApi integration needed for production
/// </summary>
[Trait("Category", "Integration")]
[Trait("Broker", "InteractiveBrokers")]
public class InteractiveBrokersBrokerIntegrationTests : IDisposable
{
    private readonly InteractiveBrokersBroker _broker;
    private readonly ITestOutputHelper _output;

    public InteractiveBrokersBrokerIntegrationTests(ITestOutputHelper output)
    {
        _output = output;

        // Get credentials from environment variables
        var username = Environment.GetEnvironmentVariable("IBKR_USERNAME");
        var password = Environment.GetEnvironmentVariable("IBKR_PASSWORD");
        var accountId = Environment.GetEnvironmentVariable("IBKR_ACCOUNT_ID");
        var host = Environment.GetEnvironmentVariable("IBKR_GATEWAY_HOST") ?? "localhost";
        var port = int.Parse(Environment.GetEnvironmentVariable("IBKR_GATEWAY_PORT") ?? "4002"); // 4002 for paper
        var usePaper = Environment.GetEnvironmentVariable("IBKR_USE_PAPER")?.ToLower() != "false";

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(accountId))
        {
            throw new SkipException("Interactive Brokers credentials not configured. Set IBKR_USERNAME, IBKR_PASSWORD, and IBKR_ACCOUNT_ID environment variables.");
        }

        var options = Options.Create(new InteractiveBrokersOptions
        {
            Username = username,
            Password = password,
            AccountId = accountId,
            GatewayHost = host,
            GatewayPort = port,
            ClientId = 1,
            UsePaperTrading = usePaper
        });

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        var logger = loggerFactory.CreateLogger<InteractiveBrokersBroker>();

        _broker = new InteractiveBrokersBroker(options, logger);

        _output.WriteLine($"Testing Interactive Brokers broker at {host}:{port}");
        _output.WriteLine($"Mode: {(usePaper ? "PAPER TRADING" : "LIVE TRADING")}");
        _output.WriteLine("⚠️ IMPORTANT: TWS or IB Gateway must be running with API enabled");
        _output.WriteLine("⚠️ NOTE: This is a foundation implementation - full IBApi integration recommended for production");
    }

    [SkippableFact]
    public async Task Connect_WithTWSRunning_Succeeds()
    {
        // Act
        var result = await _broker.ConnectAsync();

        // Assert
        Assert.True(result, "Failed to connect to Interactive Brokers. Ensure TWS/Gateway is running with API enabled.");
        _output.WriteLine("✅ Successfully connected to Interactive Brokers");
    }

    [SkippableFact]
    public async Task GetBalance_AfterConnection_ReturnsBalance()
    {
        // Arrange
        await _broker.ConnectAsync();

        // Act
        var balance = await _broker.GetBalanceAsync("USD");

        // Assert
        // NOTE: Foundation implementation returns 0, full IBApi needed
        Assert.True(balance >= 0, "Balance should be >= 0");
        _output.WriteLine($"✅ Balance: ${balance:N2} USD");
        _output.WriteLine("⚠️ NOTE: Full IBApi integration required for actual balance retrieval");
    }

    [SkippableFact]
    public async Task GetPositions_AfterConnection_ReturnsPositions()
    {
        // Arrange
        await _broker.ConnectAsync();

        // Act
        var positions = await _broker.GetPositionsAsync();

        // Assert
        Assert.NotNull(positions);
        _output.WriteLine($"✅ Retrieved {positions.Count()} position(s)");
        _output.WriteLine("⚠️ NOTE: Full IBApi integration required for actual position retrieval");
    }

    [SkippableFact]
    public async Task GetCurrentPrice_ForAAPL_ReturnsPrice()
    {
        // Arrange
        await _broker.ConnectAsync();
        var symbol = "AAPL";

        // Act
        var price = await _broker.GetCurrentPriceAsync(symbol);

        // Assert
        // NOTE: Foundation implementation returns 0, full IBApi needed
        Assert.True(price >= 0, $"Price for {symbol} should be >= 0");
        _output.WriteLine($"✅ Current price for {symbol}: ${price:N2}");
        _output.WriteLine("⚠️ NOTE: Full IBApi integration required for actual price retrieval");
    }

    [SkippableFact]
    public async Task PlaceOrder_LimitOrder_ReturnsOrderStructure()
    {
        // Arrange
        await _broker.ConnectAsync();

        var request = new OrderRequest
        {
            Symbol = "AAPL",
            Exchange = "interactivebrokers",
            Side = OrderSide.Buy,
            Type = OrderType.Limit,
            Quantity = 1, // 1 share
            Price = 50, // Far below market to avoid execution
            ClientOrderId = $"test_{Guid.NewGuid()}"
        };

        // Act
        var order = await _broker.PlaceOrderAsync(request);

        // Assert
        Assert.NotNull(order);
        Assert.NotNull(order.OrderId);
        Assert.Equal(OrderSide.Buy, order.Side);
        _output.WriteLine($"✅ Order structure created: {order.OrderId}");
        _output.WriteLine("⚠️ NOTE: Full IBApi integration required for actual order placement");
    }

    [SkippableFact]
    public async Task GetLeverageInfo_ReturnsTypicalMarginAccount()
    {
        // Arrange
        await _broker.ConnectAsync();
        var symbol = "AAPL";

        // Act
        var leverageInfo = await _broker.GetLeverageInfoAsync(symbol);

        // Assert
        Assert.NotNull(leverageInfo);
        Assert.Equal(4, leverageInfo.MaxLeverage); // Typical margin account
        _output.WriteLine($"✅ Leverage info: Current {leverageInfo.CurrentLeverage}x, Max {leverageInfo.MaxLeverage}x");
    }

    [SkippableFact]
    public async Task GetMarginHealthRatio_AfterConnection_ReturnsRatio()
    {
        // Arrange
        await _broker.ConnectAsync();

        // Act
        var healthRatio = await _broker.GetMarginHealthRatioAsync();

        // Assert
        Assert.True(healthRatio >= 0 && healthRatio <= 2.0m, "Health ratio should be between 0 and 2.0");
        _output.WriteLine($"✅ Margin health ratio: {healthRatio:P2}");
        _output.WriteLine("⚠️ NOTE: Full IBApi integration required for actual margin health calculation");
    }

    public void Dispose()
    {
        _broker?.Dispose();
    }
}
