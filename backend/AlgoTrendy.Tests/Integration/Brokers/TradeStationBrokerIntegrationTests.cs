using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Models;
using AlgoTrendy.TradingEngine.Brokers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace AlgoTrendy.Tests.Integration.Brokers;

/// <summary>
/// Integration tests for TradeStation broker
/// These tests connect to TradeStation paper trading (requires API credentials)
/// </summary>
[Trait("Category", "Integration")]
[Trait("Broker", "TradeStation")]
public class TradeStationBrokerIntegrationTests : IDisposable
{
    private readonly TradeStationBroker _broker;
    private readonly ITestOutputHelper _output;

    public TradeStationBrokerIntegrationTests(ITestOutputHelper output)
    {
        _output = output;

        // Get credentials from environment variables
        var apiKey = Environment.GetEnvironmentVariable("TRADESTATION_API_KEY");
        var apiSecret = Environment.GetEnvironmentVariable("TRADESTATION_API_SECRET");
        var accountId = Environment.GetEnvironmentVariable("TRADESTATION_ACCOUNT_ID");
        var usePaper = Environment.GetEnvironmentVariable("TRADESTATION_USE_PAPER")?.ToLower() != "false";

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret) || string.IsNullOrEmpty(accountId))
        {
            throw new SkipException("TradeStation API credentials not configured. Set TRADESTATION_API_KEY, TRADESTATION_API_SECRET, and TRADESTATION_ACCOUNT_ID environment variables.");
        }

        var options = Options.Create(new TradeStationOptions
        {
            ApiKey = apiKey,
            ApiSecret = apiSecret,
            AccountId = accountId,
            UsePaperTrading = usePaper
        });

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        var logger = loggerFactory.CreateLogger<TradeStationBroker>();
        var httpClientFactory = new TestHttpClientFactory();

        _broker = new TradeStationBroker(options, logger, httpClientFactory);

        _output.WriteLine($"Testing TradeStation broker on {(usePaper ? "PAPER TRADING (sim-api)" : "PRODUCTION")}");
    }

    [SkippableFact]
    public async Task Connect_WithValidCredentials_Succeeds()
    {
        // Act
        var result = await _broker.ConnectAsync();

        // Assert
        Assert.True(result, "Failed to connect to TradeStation");
        _output.WriteLine("✅ Successfully connected to TradeStation");
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
    public async Task GetCurrentPrice_ForAAPL_ReturnsValidPrice()
    {
        // Arrange
        await _broker.ConnectAsync();
        var symbol = "AAPL";

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
            Symbol = "AAPL",
            Exchange = "tradestation",
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
    /// Simple test HTTP client factory for TradeStation
    /// </summary>
    private class TestHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return new HttpClient();
        }
    }
}
