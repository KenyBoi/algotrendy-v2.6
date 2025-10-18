using AlgoTrendy.Core.Configuration;
using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AlgoTrendy.Tests.Unit.TradingEngine;

/// <summary>
/// Simplified unit tests for TradingEngine - focused on core functionality
/// </summary>
public class TradingEngineSimpleTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IMarketDataRepository> _marketDataRepositoryMock;
    private readonly Mock<IBroker> _brokerMock;
    private readonly Mock<ILogger<AlgoTrendy.TradingEngine.TradingEngine>> _loggerMock;
    private readonly AlgoTrendy.TradingEngine.TradingEngine _tradingEngine;

    public TradingEngineSimpleTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _marketDataRepositoryMock = new Mock<IMarketDataRepository>();
        _brokerMock = new Mock<IBroker>();
        _loggerMock = new Mock<ILogger<AlgoTrendy.TradingEngine.TradingEngine>>();

        var riskSettings = new RiskSettings
        {
            MaxPositionSizePercent = 10m,
            DefaultStopLossPercent = 5m,
            DefaultTakeProfitPercent = 10m,
            MaxConcurrentPositions = 5,
            MaxTotalExposurePercent = 50m,
            MinOrderSize = 10m,
            MaxOrderSize = 1000m,
            EnableRiskValidation = true
        };

        var options = Options.Create(riskSettings);

        _tradingEngine = new AlgoTrendy.TradingEngine.TradingEngine(
            _orderRepositoryMock.Object,
            _marketDataRepositoryMock.Object,
            _brokerMock.Object,
            options,
            _loggerMock.Object);
    }

    [Fact]
    public async Task SubmitOrderAsync_ValidOrder_ShouldSubmitSuccessfully()
    {
        // Arrange
        var order = new Order
        {
            OrderId = Guid.NewGuid().ToString(),
            Symbol = "BTCUSDT",
            Exchange = "binance",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Status = OrderStatus.Pending,
            Quantity = 0.01m,
            FilledQuantity = 0,
            StrategyId = "test-strategy",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _brokerMock
            .Setup(b => b.GetBalanceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(10000m);

        _brokerMock
            .Setup(b => b.GetCurrentPriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(50000m);

        var placedOrder = new Order
        {
            OrderId = order.OrderId,
            ExchangeOrderId = "12345",
            Symbol = order.Symbol,
            Exchange = order.Exchange,
            Side = order.Side,
            Type = order.Type,
            Status = OrderStatus.Open,
            Quantity = order.Quantity,
            FilledQuantity = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _brokerMock
            .Setup(b => b.PlaceOrderAsync(It.IsAny<OrderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(placedOrder);

        _brokerMock
            .Setup(b => b.GetOrderStatusAsync("12345", order.Symbol, It.IsAny<CancellationToken>()))
            .ReturnsAsync(placedOrder);

        _orderRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken ct) => o);

        // Act
        var result = await _tradingEngine.SubmitOrderAsync(order);

        // Assert
        result.Should().NotBeNull();
        result.ExchangeOrderId.Should().Be("12345");
        result.Status.Should().Be(OrderStatus.Open);
        result.SubmittedAt.Should().NotBeNull();

        _brokerMock.Verify(b => b.PlaceOrderAsync(It.IsAny<OrderRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _orderRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetBalanceAsync_ShouldReturnBrokerBalance()
    {
        // Arrange
        var expectedBalance = 5000m;
        _brokerMock
            .Setup(b => b.GetBalanceAsync("USDT", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedBalance);

        // Act
        var balance = await _tradingEngine.GetBalanceAsync("binance", "USDT");

        // Assert
        balance.Should().Be(expectedBalance);
        _brokerMock.Verify(b => b.GetBalanceAsync("USDT", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void GetActivePositions_NoPositions_ShouldReturnEmpty()
    {
        // Act
        var positions = _tradingEngine.GetActivePositions();

        // Assert
        positions.Should().BeEmpty();
    }

    [Fact]
    public void GetPosition_NonExistentPosition_ShouldReturnNull()
    {
        // Act
        var position = _tradingEngine.GetPosition("binance", "BTCUSDT");

        // Assert
        position.Should().BeNull();
    }
}
