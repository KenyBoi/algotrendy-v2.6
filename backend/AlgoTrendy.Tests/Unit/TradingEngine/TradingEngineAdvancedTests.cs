using AlgoTrendy.Core.Configuration;
using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using AlgoTrendy.Tests.TestHelpers.Builders;
using AlgoTrendy.Tests.TestHelpers.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AlgoTrendy.Tests.Unit.TradingEngine;

/// <summary>
/// Advanced unit tests for TradingEngine - position tracking, PnL, risk management
/// </summary>
public class TradingEngineAdvancedTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IMarketDataRepository> _marketDataRepositoryMock;
    private readonly MockBrokerFixture _brokerFixture;
    private readonly Mock<ILogger<AlgoTrendy.TradingEngine.TradingEngine>> _loggerMock;
    private readonly RiskSettings _riskSettings;
    private readonly AlgoTrendy.TradingEngine.TradingEngine _tradingEngine;

    public TradingEngineAdvancedTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _marketDataRepositoryMock = new Mock<IMarketDataRepository>();
        _brokerFixture = new MockBrokerFixture();
        _loggerMock = new Mock<ILogger<AlgoTrendy.TradingEngine.TradingEngine>>();

        _riskSettings = new RiskSettings
        {
            MaxPositionSizePercent = 10m,
            DefaultStopLossPercent = 5m,
            DefaultTakeProfitPercent = 10m,
            MaxConcurrentPositions = 5,
            MaxTotalExposurePercent = 50m,
            MinOrderSize = 10m,
            MaxOrderSize = 10000m,
            EnableRiskValidation = true
        };

        var options = Options.Create(_riskSettings);

        _tradingEngine = new AlgoTrendy.TradingEngine.TradingEngine(
            _orderRepositoryMock.Object,
            _marketDataRepositoryMock.Object,
            _brokerFixture.BrokerMock.Object,
            options,
            _loggerMock.Object);

        SetupDefaultMocks();
    }

    private void SetupDefaultMocks()
    {
        _brokerFixture
            .WithBalance("USDT", 10000m)
            .WithPrice("BTCUSDT", 50000m);

        _orderRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken ct) => o);

        _orderRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken ct) => o);

        _orderRepositoryMock
            .Setup(r => r.GetActiveOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());
    }

    [Fact]
    public async Task SubmitOrderAsync_WithMarketOrder_FillsImmediately()
    {
        // Arrange
        var order = new OrderBuilder()
            .WithType(OrderType.Market)
            .WithQuantity(0.02m)
            .WithPrice(null)
            .Build();

        // Act
        var result = await _tradingEngine.SubmitOrderAsync(order);

        // Assert
        result.Should().NotBeNull();
        result.ExchangeOrderId.Should().NotBeNullOrEmpty();
        result.Status.Should().Be(OrderStatus.Filled);
        result.FilledQuantity.Should().Be(0.02m);
        result.AverageFillPrice.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SubmitOrderAsync_WithLimitOrder_OpensSuccessfully()
    {
        // Arrange
        var order = new OrderBuilder()
            .WithType(OrderType.Limit)
            .WithQuantity(0.02m)
            .WithPrice(49000m)
            .Build();

        // Act
        var result = await _tradingEngine.SubmitOrderAsync(order);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(OrderStatus.Open);
        result.FilledQuantity.Should().Be(0);
    }

    [Fact]
    public async Task CancelOrderAsync_ActiveOrder_CancelsSuccessfully()
    {
        // Arrange
        var order = new OrderBuilder()
            .WithType(OrderType.Limit)
            .WithQuantity(0.02m)
            .WithPrice(49000m)
            .Build();

        var submittedOrder = await _tradingEngine.SubmitOrderAsync(order);

        _orderRepositoryMock
            .Setup(r => r.GetByIdAsync(submittedOrder.OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submittedOrder);

        // Act
        var result = await _tradingEngine.CancelOrderAsync(submittedOrder.OrderId);

        // Assert
        result.Status.Should().Be(OrderStatus.Cancelled);
        result.ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelOrderAsync_FilledOrder_DoesNotCancel()
    {
        // Arrange
        var order = new OrderBuilder()
            .WithStatus(OrderStatus.Filled)
            .WithExchangeOrderId("EXCH-123")
            .Build();

        _orderRepositoryMock
            .Setup(r => r.GetByIdAsync(order.OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _tradingEngine.CancelOrderAsync(order.OrderId);

        // Assert
        result.Status.Should().Be(OrderStatus.Filled);
        _brokerFixture.BrokerMock.Verify(
            b => b.CancelOrderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateOrderAsync_OrderBelowMinimumSize_Fails()
    {
        // Arrange
        var order = new OrderBuilder()
            .WithQuantity(0.0001m)  // Very small order
            .WithPrice(50000m)
            .Build();

        // Act
        var (isValid, errorMessage) = await _tradingEngine.ValidateOrderAsync(order);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Contain("below minimum");
    }

    [Fact]
    public async Task ValidateOrderAsync_OrderAboveMaximumSize_Fails()
    {
        // Arrange
        _brokerFixture.WithBalance("USDT", 100000m);

        var order = new OrderBuilder()
            .WithQuantity(1.0m)
            .WithPrice(50000m)  // Value: 50000, exceeds MaxOrderSize of 10000
            .Build();

        // Act
        var (isValid, errorMessage) = await _tradingEngine.ValidateOrderAsync(order);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Contain("exceeds maximum");
    }

    [Fact]
    public async Task ValidateOrderAsync_ExceedsMaxPositionSize_Fails()
    {
        // Arrange
        _brokerFixture.WithBalance("USDT", 1000m);

        // Order value would be 500, which is > 10% of 1000 balance
        var order = new OrderBuilder()
            .WithQuantity(0.02m)
            .WithPrice(50000m)  // Value: 1000, exceeds 10% of balance (100)
            .Build();

        // Act
        var (isValid, errorMessage) = await _tradingEngine.ValidateOrderAsync(order);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Contain("exceeds max position size");
    }

    [Fact]
    public async Task ValidateOrderAsync_WithRiskValidationDisabled_AlwaysSucceeds()
    {
        // Arrange
        _riskSettings.EnableRiskValidation = false;

        var order = new OrderBuilder()
            .WithQuantity(100m)  // Absurdly large order
            .WithPrice(50000m)
            .Build();

        // Act
        var (isValid, errorMessage) = await _tradingEngine.ValidateOrderAsync(order);

        // Assert
        isValid.Should().BeTrue();
        errorMessage.Should().BeNull();
    }

    [Fact]
    public async Task GetBalanceAsync_ReturnsCorrectBalance()
    {
        // Arrange
        _brokerFixture.WithBalance("USDT", 5000m);

        // Act
        var balance = await _tradingEngine.GetBalanceAsync("binance", "USDT");

        // Assert
        balance.Should().Be(5000m);
    }

    [Fact]
    public void GetActivePositions_NoPositions_ReturnsEmpty()
    {
        // Act
        var positions = _tradingEngine.GetActivePositions();

        // Assert
        positions.Should().BeEmpty();
    }

    [Fact]
    public void GetPosition_NonExistentSymbol_ReturnsNull()
    {
        // Act
        var position = _tradingEngine.GetPosition("binance", "ETHUSDT");

        // Assert
        position.Should().BeNull();
    }

    [Fact]
    public async Task SubmitOrderAsync_BuyOrder_EventIsRaised()
    {
        // Arrange
        var order = new OrderBuilder()
            .WithType(OrderType.Market)
            .WithSide(OrderSide.Buy)
            .WithQuantity(0.02m)
            .Build();

        Order? raisedOrder = null;
        _tradingEngine.OrderStatusChanged += (sender, o) => raisedOrder = o;

        // Act
        await _tradingEngine.SubmitOrderAsync(order);

        // Assert
        raisedOrder.Should().NotBeNull();
        raisedOrder!.Status.Should().Be(OrderStatus.Filled);
    }

    [Fact]
    public async Task SubmitOrderAsync_RejectedByBroker_ThrowsException()
    {
        // Arrange
        _brokerFixture.BrokerMock
            .Setup(b => b.PlaceOrderAsync(It.IsAny<OrderRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Insufficient funds"));

        // Use small quantity to pass risk validation but test broker rejection
        var order = new OrderBuilder()
            .WithQuantity(0.01m)
            .Build();

        // Act
        Func<Task> act = async () => await _tradingEngine.SubmitOrderAsync(order);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Insufficient funds");

        order.Status.Should().Be(OrderStatus.Rejected);
    }

    [Fact]
    public async Task SyncActiveOrdersAsync_WithMultipleOrders_SyncsAll()
    {
        // Arrange
        var orders = new List<Order>
        {
            new OrderBuilder()
                .WithOrderId("order1")
                .WithExchangeOrderId("EXCH-1")
                .WithStatus(OrderStatus.Open)
                .Build(),
            new OrderBuilder()
                .WithOrderId("order2")
                .WithExchangeOrderId("EXCH-2")
                .WithStatus(OrderStatus.Open)
                .Build()
        };

        _orderRepositoryMock
            .Setup(r => r.GetActiveOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        // Simulate orders being filled
        _brokerFixture.FillOrder("EXCH-1", 50000m);
        _brokerFixture.FillOrder("EXCH-2", 50000m);

        // Act
        var syncedCount = await _tradingEngine.SyncActiveOrdersAsync();

        // Assert
        syncedCount.Should().Be(2);
        _orderRepositoryMock.Verify(
            r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
            Times.AtLeast(2));
    }

    [Fact]
    public async Task GetOrderStatusAsync_TerminalOrder_DoesNotCheckExchange()
    {
        // Arrange
        var order = new OrderBuilder()
            .WithStatus(OrderStatus.Filled)
            .WithExchangeOrderId("EXCH-123")
            .Build();

        _orderRepositoryMock
            .Setup(r => r.GetByIdAsync(order.OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _tradingEngine.GetOrderStatusAsync(order.OrderId);

        // Assert
        result.Status.Should().Be(OrderStatus.Filled);
        _brokerFixture.BrokerMock.Verify(
            b => b.GetOrderStatusAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SubmitOrderAsync_WithStopLossOrder_CreatesCorrectly()
    {
        // Arrange
        var order = new OrderBuilder()
            .WithType(OrderType.StopLoss)
            .WithQuantity(0.02m)
            .WithStopPrice(48000m)
            .WithPrice(47500m)
            .Build();

        // Act
        var result = await _tradingEngine.SubmitOrderAsync(order);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(OrderType.StopLoss);
        result.StopPrice.Should().Be(48000m);
    }

    [Theory]
    [InlineData(OrderType.Market, 0.02, null)]
    [InlineData(OrderType.Limit, 0.02, 49000.0)]
    [InlineData(OrderType.StopLoss, 0.02, 47000.0)]
    public async Task SubmitOrderAsync_VariousOrderTypes_SubmitCorrectly(
        OrderType orderType, double quantity, double? price)
    {
        // Arrange
        var order = new OrderBuilder()
            .WithType(orderType)
            .WithQuantity((decimal)quantity)
            .WithPrice(price.HasValue ? (decimal)price.Value : null)
            .WithStopPrice(orderType == OrderType.StopLoss ? 48000m : null)
            .Build();

        // Act
        var result = await _tradingEngine.SubmitOrderAsync(order);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(orderType);
        result.ExchangeOrderId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SubmitOrderAsync_MultipleOrders_TracksCorrectly()
    {
        // Arrange
        var order1 = new OrderBuilder().WithSymbol("BTCUSDT").WithQuantity(0.02m).Build();
        var order2 = new OrderBuilder().WithSymbol("ETHUSDT").WithQuantity(0.3m).Build(); // 0.3 * 3000 = 900 USDT

        _brokerFixture.WithPrice("ETHUSDT", 3000m);

        // Act
        var result1 = await _tradingEngine.SubmitOrderAsync(order1);
        var result2 = await _tradingEngine.SubmitOrderAsync(order2);

        // Assert
        result1.ExchangeOrderId.Should().NotBe(result2.ExchangeOrderId);
        _brokerFixture.GetPlacedOrders().Should().HaveCount(2);
    }
}
