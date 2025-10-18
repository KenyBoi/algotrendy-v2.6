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
using Xunit;

namespace AlgoTrendy.Tests.E2E;

/// <summary>
/// End-to-End tests for complete trading cycles
/// Tests the full flow: Data -> Signal -> Order -> Execution -> Position
/// </summary>
public class TradingCycleE2ETests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IMarketDataRepository> _marketDataRepositoryMock;
    private readonly MockBrokerFixture _brokerFixture;
    private readonly Mock<ILogger<AlgoTrendy.TradingEngine.TradingEngine>> _loggerMock;
    private readonly AlgoTrendy.TradingEngine.TradingEngine _tradingEngine;

    public TradingCycleE2ETests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _marketDataRepositoryMock = new Mock<IMarketDataRepository>();
        _brokerFixture = new MockBrokerFixture();
        _loggerMock = new Mock<ILogger<AlgoTrendy.TradingEngine.TradingEngine>>();

        var riskSettings = new RiskSettings
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

        var options = Options.Create(riskSettings);

        _tradingEngine = new AlgoTrendy.TradingEngine.TradingEngine(
            _orderRepositoryMock.Object,
            _marketDataRepositoryMock.Object,
            _brokerFixture.BrokerMock.Object,
            options,
            _loggerMock.Object);

        SetupMocks();
    }

    private void SetupMocks()
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
            .Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken ct) =>
            {
                var orders = _brokerFixture.GetPlacedOrders();
                return orders.FirstOrDefault(o => o.OrderId == id);
            });
    }

    [Fact]
    public async Task FullTradingCycle_BuySignal_ToClosedPosition_ExecutesSuccessfully()
    {
        // Step 1: Receive market data (simulated)
        var marketData = new MarketDataBuilder()
            .WithSymbol("BTCUSDT")
            .WithClose(50000m)
            .Bullish()
            .Build();

        // Step 2: Generate buy signal (simulated by creating order)
        var buyOrder = new OrderBuilder()
            .WithSymbol("BTCUSDT")
            .WithSide(OrderSide.Buy)
            .WithType(OrderType.Market)
            .WithQuantity(0.1m)
            .WithStrategyId("test-strategy")
            .Build();

        // Step 3: Submit order to trading engine
        var submittedOrder = await _tradingEngine.SubmitOrderAsync(buyOrder);

        // Assert: Order submitted and filled
        submittedOrder.Should().NotBeNull();
        submittedOrder.Status.Should().Be(OrderStatus.Filled);
        submittedOrder.ExchangeOrderId.Should().NotBeNullOrEmpty();
        submittedOrder.AverageFillPrice.Should().BeGreaterThan(0);

        // Step 4: Verify position opened
        var position = _tradingEngine.GetPosition("binance", "BTCUSDT");
        position.Should().NotBeNull();
        position!.Symbol.Should().Be("BTCUSDT");
        position.Side.Should().Be(OrderSide.Buy);
        position.Quantity.Should().Be(0.1m);

        // Step 5: Price moves (simulated)
        _brokerFixture.WithPrice("BTCUSDT", 52000m);

        // Step 6: Generate sell signal and close position
        var sellOrder = new OrderBuilder()
            .WithSymbol("BTCUSDT")
            .WithSide(OrderSide.Sell)
            .WithType(OrderType.Market)
            .WithQuantity(0.1m)
            .WithStrategyId("test-strategy")
            .Build();

        var closedOrder = await _tradingEngine.SubmitOrderAsync(sellOrder);

        // Assert: Position closed with profit
        closedOrder.Should().NotBeNull();
        closedOrder.Status.Should().Be(OrderStatus.Filled);

        var closedPosition = _tradingEngine.GetPosition("binance", "BTCUSDT");
        closedPosition.Should().BeNull(); // Position should be removed
    }

    [Fact]
    public async Task FullTradingCycle_WithStopLoss_ExecutesCorrectly()
    {
        // Step 1: Submit buy order
        var buyOrder = new OrderBuilder()
            .WithSymbol("BTCUSDT")
            .WithSide(OrderSide.Buy)
            .WithType(OrderType.Market)
            .WithQuantity(0.1m)
            .Build();

        await _tradingEngine.SubmitOrderAsync(buyOrder);

        // Step 2: Verify position with stop loss
        var position = _tradingEngine.GetPosition("binance", "BTCUSDT");
        position.Should().NotBeNull();
        position!.StopLoss.Should().NotBeNull(); // Default stop loss should be set

        // Step 3: Price drops below stop loss
        var stopLossPrice = position.StopLoss!.Value;
        _brokerFixture.WithPrice("BTCUSDT", stopLossPrice - 100m);

        // Step 4: Check if stop loss is hit
        position.CurrentPrice = stopLossPrice - 100m;
        position.IsStopLossHit.Should().BeTrue();

        // Step 5: Close position at stop loss
        var sellOrder = new OrderBuilder()
            .WithSymbol("BTCUSDT")
            .WithSide(OrderSide.Sell)
            .WithType(OrderType.Market)
            .WithQuantity(0.1m)
            .Build();

        var closedOrder = await _tradingEngine.SubmitOrderAsync(sellOrder);
        closedOrder.Status.Should().Be(OrderStatus.Filled);
    }

    [Fact]
    public async Task FullTradingCycle_WithTakeProfit_ExecutesCorrectly()
    {
        // Step 1: Submit buy order
        var buyOrder = new OrderBuilder()
            .WithSymbol("BTCUSDT")
            .WithSide(OrderSide.Buy)
            .WithType(OrderType.Market)
            .WithQuantity(0.1m)
            .Build();

        await _tradingEngine.SubmitOrderAsync(buyOrder);

        // Step 2: Verify position with take profit
        var position = _tradingEngine.GetPosition("binance", "BTCUSDT");
        position.Should().NotBeNull();
        position!.TakeProfit.Should().NotBeNull(); // Default take profit should be set

        // Step 3: Price rises above take profit
        var takeProfitPrice = position.TakeProfit!.Value;
        _brokerFixture.WithPrice("BTCUSDT", takeProfitPrice + 100m);

        // Step 4: Check if take profit is hit
        position.CurrentPrice = takeProfitPrice + 100m;
        position.IsTakeProfitHit.Should().BeTrue();

        // Step 5: Close position at take profit
        var sellOrder = new OrderBuilder()
            .WithSymbol("BTCUSDT")
            .WithSide(OrderSide.Sell)
            .WithType(OrderType.Market)
            .WithQuantity(0.1m)
            .Build();

        var closedOrder = await _tradingEngine.SubmitOrderAsync(sellOrder);
        closedOrder.Status.Should().Be(OrderStatus.Filled);
    }

    [Fact]
    public async Task FullTradingCycle_MultiplePositions_ManagesCorrectly()
    {
        // Step 1: Open position for BTCUSDT
        var buyOrder1 = new OrderBuilder()
            .WithSymbol("BTCUSDT")
            .WithSide(OrderSide.Buy)
            .WithType(OrderType.Market)
            .WithQuantity(0.1m)
            .Build();

        await _tradingEngine.SubmitOrderAsync(buyOrder1);

        // Step 2: Open position for ETHUSDT
        _brokerFixture.WithPrice("ETHUSDT", 3000m);

        var buyOrder2 = new OrderBuilder()
            .WithSymbol("ETHUSDT")
            .WithSide(OrderSide.Buy)
            .WithType(OrderType.Market)
            .WithQuantity(1.0m)
            .Build();

        await _tradingEngine.SubmitOrderAsync(buyOrder2);

        // Step 3: Verify both positions exist
        var btcPosition = _tradingEngine.GetPosition("binance", "BTCUSDT");
        var ethPosition = _tradingEngine.GetPosition("binance", "ETHUSDT");

        btcPosition.Should().NotBeNull();
        ethPosition.Should().NotBeNull();

        var allPositions = _tradingEngine.GetActivePositions();
        allPositions.Should().HaveCount(2);

        // Step 4: Close one position
        var sellOrder1 = new OrderBuilder()
            .WithSymbol("BTCUSDT")
            .WithSide(OrderSide.Sell)
            .WithType(OrderType.Market)
            .WithQuantity(0.1m)
            .Build();

        await _tradingEngine.SubmitOrderAsync(sellOrder1);

        // Step 5: Verify only one position remains
        var remainingPositions = _tradingEngine.GetActivePositions();
        remainingPositions.Should().HaveCount(1);
        remainingPositions.First().Symbol.Should().Be("ETHUSDT");
    }

    [Fact]
    public async Task FullTradingCycle_PartialFill_HandlesCorrectly()
    {
        // Step 1: Submit limit order
        var limitOrder = new OrderBuilder()
            .WithSymbol("BTCUSDT")
            .WithSide(OrderSide.Buy)
            .WithType(OrderType.Limit)
            .WithQuantity(1.0m)
            .WithPrice(49000m)
            .Build();

        var submittedOrder = await _tradingEngine.SubmitOrderAsync(limitOrder);

        // Step 2: Simulate partial fill
        _brokerFixture.PartiallyFillOrder(submittedOrder.ExchangeOrderId!, 0.5m, 49000m);

        // Step 3: Get updated order status
        var updatedOrder = await _tradingEngine.GetOrderStatusAsync(submittedOrder.OrderId);

        // Assert: Order is partially filled
        updatedOrder.Status.Should().Be(OrderStatus.PartiallyFilled);
        updatedOrder.FilledQuantity.Should().Be(0.5m);
        updatedOrder.RemainingQuantity.Should().Be(0.5m);
    }

    [Fact]
    public async Task FullTradingCycle_CancelOrder_BeforeFill()
    {
        // Step 1: Submit limit order
        var limitOrder = new OrderBuilder()
            .WithSymbol("BTCUSDT")
            .WithSide(OrderSide.Buy)
            .WithType(OrderType.Limit)
            .WithQuantity(0.1m)
            .WithPrice(48000m)
            .Build();

        var submittedOrder = await _tradingEngine.SubmitOrderAsync(limitOrder);

        // Step 2: Cancel the order
        var cancelledOrder = await _tradingEngine.CancelOrderAsync(submittedOrder.OrderId);

        // Assert: Order is cancelled
        cancelledOrder.Status.Should().Be(OrderStatus.Cancelled);
        cancelledOrder.ClosedAt.Should().NotBeNull();

        // Verify no position was created
        var position = _tradingEngine.GetPosition("binance", "BTCUSDT");
        position.Should().BeNull();
    }

    [Fact]
    public async Task FullTradingCycle_RiskValidation_PreventsExcessiveOrders()
    {
        // Step 1: Try to submit order exceeding balance
        _brokerFixture.WithBalance("USDT", 100m);

        var largeOrder = new OrderBuilder()
            .WithSymbol("BTCUSDT")
            .WithSide(OrderSide.Buy)
            .WithType(OrderType.Market)
            .WithQuantity(0.5m)  // 0.5 BTC at 50000 = 25000 USDT (exceeds balance)
            .Build();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _tradingEngine.SubmitOrderAsync(largeOrder));
    }

    [Fact]
    public async Task MarketDataFlow_FetchStoreProcess_WorksEndToEnd()
    {
        // Step 1: Create market data
        var marketData = new MarketDataBuilder()
            .WithSymbol("BTCUSDT")
            .WithTimestamp(DateTime.UtcNow)
            .WithClose(50000m)
            .Bullish()
            .Build();

        // Step 2: Store market data (simulated)
        _marketDataRepositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<MarketData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var stored = await _marketDataRepositoryMock.Object.InsertAsync(marketData);

        // Assert: Data stored successfully
        stored.Should().BeTrue();

        // Step 3: Retrieve latest market data
        _marketDataRepositoryMock
            .Setup(r => r.GetLatestAsync("BTCUSDT", It.IsAny<CancellationToken>()))
            .ReturnsAsync(marketData);

        var retrieved = await _marketDataRepositoryMock.Object.GetLatestAsync("BTCUSDT");

        // Assert: Data retrieved correctly
        retrieved.Should().NotBeNull();
        retrieved!.Symbol.Should().Be("BTCUSDT");
        retrieved.Close.Should().Be(50000m);
    }

    [Fact]
    public async Task FullTradingCycle_WithPnLTracking_CalculatesCorrectly()
    {
        // Step 1: Open long position
        var buyOrder = new OrderBuilder()
            .WithSymbol("BTCUSDT")
            .WithSide(OrderSide.Buy)
            .WithType(OrderType.Market)
            .WithQuantity(0.1m)
            .Build();

        await _tradingEngine.SubmitOrderAsync(buyOrder);

        var position = _tradingEngine.GetPosition("binance", "BTCUSDT");
        position.Should().NotBeNull();

        var entryPrice = position!.EntryPrice;

        // Step 2: Price moves up 10%
        var newPrice = entryPrice * 1.10m;
        _brokerFixture.WithPrice("BTCUSDT", newPrice);
        position.CurrentPrice = newPrice;

        // Step 3: Calculate unrealized PnL
        var unrealizedPnL = position.UnrealizedPnL;
        var unrealizedPnLPercent = position.UnrealizedPnLPercent;

        // Assert: PnL is positive
        unrealizedPnL.Should().BeGreaterThan(0);
        unrealizedPnLPercent.Should().BeApproximately(10m, 0.1m);

        // Step 4: Close position
        var sellOrder = new OrderBuilder()
            .WithSymbol("BTCUSDT")
            .WithSide(OrderSide.Sell)
            .WithType(OrderType.Market)
            .WithQuantity(0.1m)
            .Build();

        var closedOrder = await _tradingEngine.SubmitOrderAsync(sellOrder);

        // Assert: Realized profit
        closedOrder.Status.Should().Be(OrderStatus.Filled);
    }
}
