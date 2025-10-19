using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Models;
using AlgoTrendy.TradingEngine.Brokers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace AlgoTrendy.Tests.Integration.Brokers;

/// <summary>
/// Integration tests for NinjaTrader broker
/// These tests connect to NinjaTrader 8 via REST API (requires NT8 running with ATI enabled)
/// </summary>
[Trait("Category", "Integration")]
[Trait("Broker", "NinjaTrader")]
public class NinjaTraderBrokerIntegrationTests : IDisposable
{
    private readonly NinjaTraderBroker _broker;
    private readonly ITestOutputHelper _output;

    public NinjaTraderBrokerIntegrationTests(ITestOutputHelper output)
    {
        _output = output;

        // Get credentials from environment variables
        var username = Environment.GetEnvironmentVariable("NINJATRADER_USERNAME");
        var password = Environment.GetEnvironmentVariable("NINJATRADER_PASSWORD");
        var accountId = Environment.GetEnvironmentVariable("NINJATRADER_ACCOUNT_ID");
        var host = Environment.GetEnvironmentVariable("NINJATRADER_HOST") ?? "localhost";
        var port = int.Parse(Environment.GetEnvironmentVariable("NINJATRADER_PORT") ?? "36973");

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(accountId))
        {
            throw new SkipException("NinjaTrader credentials not configured. Set NINJATRADER_USERNAME, NINJATRADER_PASSWORD, and NINJATRADER_ACCOUNT_ID environment variables.");
        }

        var options = Options.Create(new NinjaTraderOptions
        {
            Username = username,
            Password = password,
            AccountId = accountId,
            Host = host,
            Port = port,
            ConnectionType = "REST"
        });

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        var logger = loggerFactory.CreateLogger<NinjaTraderBroker>();
        var httpClientFactory = new TestHttpClientFactory();

        _broker = new NinjaTraderBroker(options, logger, httpClientFactory);

        _output.WriteLine($"Testing NinjaTrader broker at {host}:{port}");
        _output.WriteLine("⚠️ IMPORTANT: NinjaTrader 8 must be running with ATI (Automated Trading Interface) enabled");
    }

    [SkippableFact]
    public async Task Connect_WithNT8Running_Succeeds()
    {
        // Act
        var result = await _broker.ConnectAsync();

        // Assert
        Assert.True(result, "Failed to connect to NinjaTrader. Ensure NT8 is running with ATI enabled.");
        _output.WriteLine("✅ Successfully connected to NinjaTrader");
    }

    [SkippableFact]
    public async Task GetBalance_AfterConnection_ReturnsBalance()
    {
        // Arrange
        await _broker.ConnectAsync();

        // Act
        var balance = await _broker.GetBalanceAsync("USD");

        // Assert
        Assert.True(balance >= 0, "Balance should be >= 0");
        _output.WriteLine($"✅ Balance: ${balance:N2} USD");
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
    }

    [SkippableFact]
    public async Task GetCurrentPrice_ForESFuture_ReturnsValidPrice()
    {
        // Arrange
        await _broker.ConnectAsync();
        var symbol = "ES 12-25"; // E-mini S&P 500 December 2025

        // Act
        var price = await _broker.GetCurrentPriceAsync(symbol);

        // Assert
        Assert.True(price > 0, $"Price for {symbol} should be > 0");
        _output.WriteLine($"✅ Current price for {symbol}: ${price:N2}");
    }

    [SkippableFact]
    public async Task PlaceOrder_LimitOrder_CreatesOrder()
    {
        // Arrange
        await _broker.ConnectAsync();

        var request = new OrderRequest
        {
            Symbol = "ES 12-25", // E-mini S&P 500
            Exchange = "ninjatrader",
            Side = OrderSide.Buy,
            Type = OrderType.Limit,
            Quantity = 1, // 1 contract
            Price = 3000, // Far below market to avoid execution
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
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    /// <summary>
    /// Simple test HTTP client factory for NinjaTrader
    /// </summary>
    private class TestHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return new HttpClient();
        }
    }
}
