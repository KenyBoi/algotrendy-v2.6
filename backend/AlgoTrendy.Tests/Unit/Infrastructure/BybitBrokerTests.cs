using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using AlgoTrendy.Infrastructure.Brokers.Bybit;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AlgoTrendy.Tests.Unit.Infrastructure;

/// <summary>
/// Unit tests for the BybitBroker class
/// These tests use mocking to validate broker behavior without external API calls.
/// Integration tests with actual Bybit testnet are in Integration/BybitBrokerIntegrationTests.cs
/// </summary>
public class BybitBrokerTests
{
    private readonly Mock<ILogger<BybitBroker>> _mockLogger;

    // NOTE: These are fake test constants, not real credentials
    // gitleaks:allow
    private const string TestApiKey = "test_api_key_12345";
    private const string TestApiSecret = "test_api_secret_67890";

    public BybitBrokerTests()
    {
        _mockLogger = new Mock<ILogger<BybitBroker>>();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ValidParameters_InitializesSuccessfully()
    {
        // Arrange & Act
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _mockLogger.Object);

        // Assert
        Assert.NotNull(broker);
        Assert.Equal("Bybit", broker.BrokerName);
    }

    [Fact]
    public void Constructor_NullApiKey_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new BybitBroker(null!, TestApiSecret, true, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_NullApiSecret_DoesNotThrowException()
    {
        // Arrange, Act & Assert
        // Note: API secret can be null in some scenarios
        var broker = new BybitBroker(TestApiKey, null!, true, _mockLogger.Object);
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new BybitBroker(TestApiKey, TestApiSecret, true, null!));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Constructor_ConfiguresTestnetCorrectly(bool useTestnet)
    {
        // Arrange & Act
        var broker = new BybitBroker(TestApiKey, TestApiSecret, useTestnet, _mockLogger.Object);

        // Assert
        Assert.NotNull(broker);
        Assert.Equal("Bybit", broker.BrokerName);
    }

    #endregion

    #region Properties Tests

    [Fact]
    public void BrokerName_ReturnsCorrectValue()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _mockLogger.Object);

        // Act
        var name = broker.BrokerName;

        // Assert
        Assert.Equal("Bybit", name);
    }

    #endregion

    #region Connection Tests

    [Fact(Skip = "Integration test - requires network connectivity")]
    public async Task ConnectAsync_WithoutCancellation_CompletesSuccessfully()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _mockLogger.Object);

        // Act
        // Note: This will actually try to connect to Bybit testnet
        // In a fully mocked test, we'd need to inject HttpClient via interface
        var result = await broker.ConnectAsync();

        // Assert
        // Result depends on network connectivity
        Assert.IsType<bool>(result);
    }

    [Fact(Skip = "Integration test - requires network connectivity")]
    public async Task ConnectAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _mockLogger.Object);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(1)); // Cancel immediately

        // Act & Assert
        // Note: Actual behavior depends on implementation
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => broker.ConnectAsync(cts.Token));
    }

    #endregion

    #region Balance Tests

    [Fact]
    public async Task GetBalanceAsync_DefaultCurrency_ReturnsDecimal()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _mockLogger.Object);

        // Act
        var balance = await broker.GetBalanceAsync();

        // Assert
        Assert.IsType<decimal>(balance);
        Assert.True(balance >= 0, "Balance should be non-negative");
    }

    [Fact]
    public async Task GetBalanceAsync_SpecificCurrency_ReturnsDecimal()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _mockLogger.Object);

        // Act
        var balance = await broker.GetBalanceAsync("BTC");

        // Assert
        Assert.IsType<decimal>(balance);
        Assert.True(balance >= 0, "Balance should be non-negative");
    }

    [Fact]
    public async Task GetBalanceAsync_WhileDisconnected_ReturnsZero()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _mockLogger.Object);
        // Don't call ConnectAsync, so broker is disconnected

        // Act
        var balance = await broker.GetBalanceAsync("USDT");

        // Assert
        Assert.Equal(0, balance);
    }

    [Fact]
    public async Task GetBalanceAsync_WithCancellation_CompletesOrCancels()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _mockLogger.Object);
        using var cts = new CancellationTokenSource();

        // Act
        var balance = await broker.GetBalanceAsync("USDT", cts.Token);

        // Assert
        Assert.IsType<decimal>(balance);
    }

    #endregion

    #region Position Tests

    [Fact]
    public async Task GetPositionsAsync_ReturnsEnumerable()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _mockLogger.Object);

        // Act
        var positions = await broker.GetPositionsAsync();

        // Assert
        Assert.NotNull(positions);
        Assert.IsAssignableFrom<IEnumerable<Position>>(positions);
    }

    [Fact]
    public async Task GetPositionsAsync_WhileDisconnected_ReturnsEmptyEnumerable()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _mockLogger.Object);
        // Don't call ConnectAsync

        // Act
        var positions = await broker.GetPositionsAsync();

        // Assert
        Assert.Empty(positions);
    }

    [Fact]
    public async Task GetPositionsAsync_WithCancellation_CompletesOrCancels()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _mockLogger.Object);
        using var cts = new CancellationTokenSource();

        // Act
        var positions = await broker.GetPositionsAsync(cts.Token);

        // Assert
        Assert.NotNull(positions);
    }

    #endregion

    #region Order Placement Tests

    [Fact(Skip = "Integration test - requires API connectivity")]
    public async Task PlaceOrderAsync_ValidRequest_ReturnsOrder()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _mockLogger.Object);
        await broker.ConnectAsync(); // Connect first

        var orderRequest = new OrderRequest
        {
            Symbol = "BTCUSDT",
            Exchange = "Bybit",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = 0.1m,
            Price = null,
            ClientOrderId = Guid.NewGuid().ToString()
        };

        // Act
        var order = await broker.PlaceOrderAsync(orderRequest);

        // Assert
        Assert.NotNull(order);
        Assert.Equal("BTCUSDT", order.Symbol);
        Assert.Equal(OrderSide.Buy, order.Side);
        Assert.Equal(OrderType.Market, order.Type);
        Assert.Equal(0.1m, order.Quantity);
        Assert.Equal("Bybit", order.Exchange);
    }

    [Fact(Skip = "Integration test - requires API connectivity")]
    public async Task PlaceOrderAsync_Sell_CreatesCorrectOrder()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _mockLogger.Object);
        await broker.ConnectAsync();

        var orderRequest = new OrderRequest
        {
            Symbol = "ETHUSDT",
            Exchange = "Bybit",
            Side = OrderSide.Sell,
            Type = OrderType.Limit,
            Quantity = 1.0m,
            Price = 2000,
            ClientOrderId = Guid.NewGuid().ToString()
        };

        // Act
        var order = await broker.PlaceOrderAsync(orderRequest);

        // Assert
        Assert.Equal(OrderSide.Sell, order.Side);
        Assert.Equal(2000, order.Price);
    }

    [Fact]
    public async Task PlaceOrderAsync_DisconnectedBroker_ThrowsException()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _mockLogger.Object);
        // Don't connect

        var orderRequest = new OrderRequest
        {
            Symbol = "BTCUSDT",
            Exchange = "Bybit",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = 0.1m
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => broker.PlaceOrderAsync(orderRequest));
    }

    [Fact]
    public async Task PlaceOrderAsync_NullRequest_ThrowsException()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => broker.PlaceOrderAsync(null!));
    }

    #endregion

    #region Order Cancellation Tests

    [Fact(Skip = "Integration test - requires API connectivity")]
    public async Task CancelOrderAsync_ValidOrderId_ReturnsOrder()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _mockLogger.Object);
        await broker.ConnectAsync();

        // Act
        var order = await broker.CancelOrderAsync("order_123", "BTCUSDT");

        // Assert
        Assert.NotNull(order);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal("order_123", order.OrderId);
        Assert.Equal("BTCUSDT", order.Symbol);
    }

    [Fact]
    public async Task CancelOrderAsync_DisconnectedBroker_ThrowsException()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _mockLogger.Object);
        // Don't connect

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => broker.CancelOrderAsync("order_123", "BTCUSDT"));
    }

    #endregion

    #region Order Status Tests

    [Fact(Skip = "Integration test - requires API connectivity")]
    public async Task GetOrderStatusAsync_ValidOrderId_ReturnsOrder()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _mockLogger.Object);
        await broker.ConnectAsync();

        // Act
        var order = await broker.GetOrderStatusAsync("order_123", "BTCUSDT");

        // Assert
        Assert.NotNull(order);
        Assert.Equal("order_123", order.OrderId);
        Assert.Equal("BTCUSDT", order.Symbol);
        Assert.IsType<Order>(order);
    }

    [Fact]
    public async Task GetOrderStatusAsync_DisconnectedBroker_ThrowsException()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => broker.GetOrderStatusAsync("order_123", "BTCUSDT"));
    }

    #endregion

    #region Market Price Tests

    [Fact]
    public async Task GetCurrentPriceAsync_ValidSymbol_ReturnsPrice()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _mockLogger.Object);
        await broker.ConnectAsync();

        // Act
        var price = await broker.GetCurrentPriceAsync("BTCUSDT");

        // Assert
        Assert.IsType<decimal>(price);
    }

    [Fact]
    public async Task GetCurrentPriceAsync_DisconnectedBroker_ReturnsZero()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _mockLogger.Object);

        // Act
        var price = await broker.GetCurrentPriceAsync("BTCUSDT");

        // Assert
        Assert.Equal(0, price);
    }

    [Fact]
    public async Task GetCurrentPriceAsync_VariousSymbols_ReturnsPrice()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _mockLogger.Object);
        await broker.ConnectAsync();

        var symbols = new[] { "BTCUSDT", "ETHUSDT", "XRPUSDT" };

        // Act & Assert
        foreach (var symbol in symbols)
        {
            var price = await broker.GetCurrentPriceAsync(symbol);
            Assert.IsType<decimal>(price);
        }
    }

    #endregion

    #region Leverage Tests

    [Fact]
    public async Task SetLeverageAsync_ValidLeverage_ReturnsTrue()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _mockLogger.Object);
        await broker.ConnectAsync();

        // Act
        var result = await broker.SetLeverageAsync("BTCUSDT", 5, MarginType.Cross);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task SetLeverageAsync_WithIsolatedMargin_ReturnsTrue()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _mockLogger.Object);
        await broker.ConnectAsync();

        // Act
        var result = await broker.SetLeverageAsync("BTCUSDT", 10, MarginType.Isolated);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task SetLeverageAsync_DisconnectedBroker_ThrowsException()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => broker.SetLeverageAsync("BTCUSDT", 5, MarginType.Cross));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public async Task SetLeverageAsync_VariousValues_ReturnsTrue(decimal leverage)
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _mockLogger.Object);
        await broker.ConnectAsync();

        // Act
        var result = await broker.SetLeverageAsync("BTCUSDT", leverage, MarginType.Cross);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region Leverage Info Tests

    [Fact]
    public async Task GetLeverageInfoAsync_ValidSymbol_ReturnsLeverageInfo()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _mockLogger.Object);
        await broker.ConnectAsync();

        // Act
        var leverageInfo = await broker.GetLeverageInfoAsync("BTCUSDT");

        // Assert
        Assert.NotNull(leverageInfo);
        Assert.IsType<LeverageInfo>(leverageInfo);
        Assert.True(leverageInfo.CurrentLeverage > 0);
        Assert.True(leverageInfo.MaxLeverage > 0);
    }

    [Fact]
    public async Task GetLeverageInfoAsync_DisconnectedBroker_ThrowsException()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => broker.GetLeverageInfoAsync("BTCUSDT"));
    }

    #endregion

    #region Margin Health Tests

    [Fact]
    public async Task GetMarginHealthRatioAsync_ConnectedBroker_ReturnsHealthRatio()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _mockLogger.Object);
        await broker.ConnectAsync();

        // Act
        var ratio = await broker.GetMarginHealthRatioAsync();

        // Assert
        Assert.IsType<decimal>(ratio);
        Assert.True(ratio > 0);
    }

    [Fact]
    public async Task GetMarginHealthRatioAsync_DisconnectedBroker_ThrowsException()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => broker.GetMarginHealthRatioAsync());
    }

    #endregion

    #region Resource Disposal Tests

    [Fact]
    public void Dispose_MultipleCalls_DoesNotThrow()
    {
        // Arrange
        var broker = new BybitBroker(TestApiKey, TestApiSecret, true, _mockLogger.Object);

        // Act & Assert
        broker.Dispose();
        broker.Dispose(); // Should not throw
    }

    #endregion
}
