using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using AlgoTrendy.Tests.TestHelpers.Builders;
using AlgoTrendy.TradingEngine.Services;
using AlgoTrendy.TradingEngine.Strategies;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AlgoTrendy.Tests.Unit.TradingEngine;

public class RSIStrategyTests
{
    private readonly Mock<IndicatorService> _mockIndicatorService;
    private readonly Mock<ILogger<RSIStrategy>> _mockLogger;
    private readonly RSIStrategyConfig _config;
    private readonly RSIStrategy _strategy;

    public RSIStrategyTests()
    {
        _mockIndicatorService = new Mock<IndicatorService>(
            Mock.Of<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
            Mock.Of<ILogger<IndicatorService>>());

        _mockLogger = new Mock<ILogger<RSIStrategy>>();

        _config = new RSIStrategyConfig
        {
            Period = 14,
            OversoldThreshold = 30m,
            OverboughtThreshold = 70m
        };

        _strategy = new RSIStrategy(_config, _mockIndicatorService.Object, _mockLogger.Object);
    }

    [Fact]
    public void StrategyName_ReturnsCorrectName()
    {
        // Act & Assert
        _strategy.StrategyName.Should().Be("RSI");
    }

    [Fact]
    public async Task AnalyzeAsync_WithOversoldRSI_GeneratesBuySignal()
    {
        // Arrange
        var currentData = new MarketDataBuilder()
            .WithClose(50000m)
            .Build();

        var historicalData = CreateHistoricalData(50);

        _mockIndicatorService
            .Setup(x => x.CalculateRSIAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<MarketData>>(),
                14,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(25m); // Oversold (below 30)

        // Act
        var signal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Assert
        signal.Should().NotBeNull();
        signal.Action.Should().Be(SignalAction.Buy);
        signal.Confidence.Should().BeGreaterThan(0);
        signal.Reason.Should().Contain("OVERSOLD");
        signal.EntryPrice.Should().Be(50000m);
        signal.StopLoss.Should().Be(50000m * 0.97m);
        signal.TakeProfit.Should().Be(50000m * 1.06m);
    }

    [Fact]
    public async Task AnalyzeAsync_WithOverboughtRSI_GeneratesSellSignal()
    {
        // Arrange
        var currentData = new MarketDataBuilder()
            .WithClose(50000m)
            .Build();

        var historicalData = CreateHistoricalData(50);

        _mockIndicatorService
            .Setup(x => x.CalculateRSIAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<MarketData>>(),
                14,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(75m); // Overbought (above 70)

        // Act
        var signal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Assert
        signal.Action.Should().Be(SignalAction.Sell);
        signal.Confidence.Should().BeGreaterThan(0);
        signal.Reason.Should().Contain("OVERBOUGHT");
        signal.StopLoss.Should().Be(50000m * 1.03m);
        signal.TakeProfit.Should().Be(50000m * 0.94m);
    }

    [Fact]
    public async Task AnalyzeAsync_WithNeutralRSI_GeneratesHoldSignal()
    {
        // Arrange
        var currentData = new MarketDataBuilder().Build();
        var historicalData = CreateHistoricalData(50);

        _mockIndicatorService
            .Setup(x => x.CalculateRSIAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<MarketData>>(),
                14,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(50m); // Neutral (between 30 and 70)

        // Act
        var signal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Assert
        signal.Action.Should().Be(SignalAction.Hold);
        signal.Confidence.Should().Be(0.4m);
        signal.Reason.Should().Contain("NEUTRAL");
    }

    [Theory]
    [InlineData(20, SignalAction.Buy)]   // Deep oversold
    [InlineData(25, SignalAction.Buy)]   // Oversold
    [InlineData(30, SignalAction.Hold)]  // At threshold
    [InlineData(50, SignalAction.Hold)]  // Neutral
    [InlineData(70, SignalAction.Hold)]  // At threshold
    [InlineData(75, SignalAction.Sell)]  // Overbought
    [InlineData(85, SignalAction.Sell)]  // Deep overbought
    public async Task AnalyzeAsync_WithVariousRSIValues_GeneratesCorrectSignal(
        decimal rsi, SignalAction expectedAction)
    {
        // Arrange
        var currentData = new MarketDataBuilder().Build();
        var historicalData = CreateHistoricalData(50);

        _mockIndicatorService
            .Setup(x => x.CalculateRSIAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<MarketData>>(),
                14,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rsi);

        // Act
        var signal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Assert
        signal.Action.Should().Be(expectedAction);
    }

    [Fact]
    public async Task AnalyzeAsync_ConfidenceIncreasesWithDeeperOversold()
    {
        // Arrange
        var currentData = new MarketDataBuilder().Build();
        var historicalData = CreateHistoricalData(50);

        // Setup for moderate oversold (RSI = 25)
        _mockIndicatorService
            .Setup(x => x.CalculateRSIAsync(It.IsAny<string>(), It.IsAny<IEnumerable<MarketData>>(), 14, It.IsAny<CancellationToken>()))
            .ReturnsAsync(25m);
        var moderateSignal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Setup for deep oversold (RSI = 15)
        _mockIndicatorService
            .Setup(x => x.CalculateRSIAsync(It.IsAny<string>(), It.IsAny<IEnumerable<MarketData>>(), 14, It.IsAny<CancellationToken>()))
            .ReturnsAsync(15m);
        var deepSignal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Assert
        deepSignal.Confidence.Should().BeGreaterThan(moderateSignal.Confidence);
    }

    [Fact]
    public async Task AnalyzeAsync_ConfidenceIncreasesWithDeeperOverbought()
    {
        // Arrange
        var currentData = new MarketDataBuilder().Build();
        var historicalData = CreateHistoricalData(50);

        // Setup for moderate overbought (RSI = 75)
        _mockIndicatorService
            .Setup(x => x.CalculateRSIAsync(It.IsAny<string>(), It.IsAny<IEnumerable<MarketData>>(), 14, It.IsAny<CancellationToken>()))
            .ReturnsAsync(75m);
        var moderateSignal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Setup for deep overbought (RSI = 85)
        _mockIndicatorService
            .Setup(x => x.CalculateRSIAsync(It.IsAny<string>(), It.IsAny<IEnumerable<MarketData>>(), 14, It.IsAny<CancellationToken>()))
            .ReturnsAsync(85m);
        var deepSignal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Assert
        deepSignal.Confidence.Should().BeGreaterThan(moderateSignal.Confidence);
    }

    [Fact]
    public async Task AnalyzeAsync_ConfidenceCappedAt0Point9()
    {
        // Arrange
        var currentData = new MarketDataBuilder().Build();
        var historicalData = CreateHistoricalData(50);

        _mockIndicatorService
            .Setup(x => x.CalculateRSIAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<MarketData>>(),
                14,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(5m); // Extremely oversold

        // Act
        var signal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Assert
        signal.Confidence.Should().BeLessThanOrEqualTo(0.9m);
    }

    [Fact]
    public async Task AnalyzeAsync_WithException_ReturnsHoldSignalWithError()
    {
        // Arrange
        var currentData = new MarketDataBuilder().Build();
        var historicalData = CreateHistoricalData(50);

        _mockIndicatorService
            .Setup(x => x.CalculateRSIAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<MarketData>>(),
                14,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var signal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Assert
        signal.Action.Should().Be(SignalAction.Hold);
        signal.Confidence.Should().Be(0.0m);
        signal.Reason.Should().Contain("Error");
    }

    [Fact]
    public async Task AnalyzeAsync_SetsCorrectSymbol()
    {
        // Arrange
        var currentData = new MarketDataBuilder()
            .WithSymbol("ETHUSDT")
            .Build();
        var historicalData = CreateHistoricalData(50);

        _mockIndicatorService
            .Setup(x => x.CalculateRSIAsync(It.IsAny<string>(), It.IsAny<IEnumerable<MarketData>>(), 14, It.IsAny<CancellationToken>()))
            .ReturnsAsync(50m);

        // Act
        var signal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Assert
        signal.Symbol.Should().Be("ETHUSDT");
    }

    [Fact]
    public async Task AnalyzeAsync_SetsTimestamp()
    {
        // Arrange
        var before = DateTime.UtcNow;
        var currentData = new MarketDataBuilder().Build();
        var historicalData = CreateHistoricalData(50);

        _mockIndicatorService
            .Setup(x => x.CalculateRSIAsync(It.IsAny<string>(), It.IsAny<IEnumerable<MarketData>>(), 14, It.IsAny<CancellationToken>()))
            .ReturnsAsync(50m);

        // Act
        var signal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);
        var after = DateTime.UtcNow;

        // Assert
        signal.Timestamp.Should().BeOnOrAfter(before);
        signal.Timestamp.Should().BeOnOrBefore(after);
    }

    [Fact]
    public async Task AnalyzeAsync_BuySignal_SetsCorrectStopLossAndTakeProfit()
    {
        // Arrange
        var currentData = new MarketDataBuilder()
            .WithClose(100m)
            .Build();
        var historicalData = CreateHistoricalData(50);

        _mockIndicatorService
            .Setup(x => x.CalculateRSIAsync(It.IsAny<string>(), It.IsAny<IEnumerable<MarketData>>(), 14, It.IsAny<CancellationToken>()))
            .ReturnsAsync(25m); // Oversold

        // Act
        var signal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Assert
        signal.StopLoss.Should().Be(97m);   // 100 * 0.97
        signal.TakeProfit.Should().Be(106m); // 100 * 1.06
    }

    [Fact]
    public async Task AnalyzeAsync_SellSignal_SetsCorrectStopLossAndTakeProfit()
    {
        // Arrange
        var currentData = new MarketDataBuilder()
            .WithClose(100m)
            .Build();
        var historicalData = CreateHistoricalData(50);

        _mockIndicatorService
            .Setup(x => x.CalculateRSIAsync(It.IsAny<string>(), It.IsAny<IEnumerable<MarketData>>(), 14, It.IsAny<CancellationToken>()))
            .ReturnsAsync(75m); // Overbought

        // Act
        var signal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Assert
        signal.StopLoss.Should().Be(103m);  // 100 * 1.03
        signal.TakeProfit.Should().Be(94m); // 100 * 0.94
    }

    private List<MarketData> CreateHistoricalData(int count)
    {
        var data = new List<MarketData>();
        var baseTime = DateTime.UtcNow.AddMinutes(-count);

        for (int i = 0; i < count; i++)
        {
            data.Add(new MarketDataBuilder()
                .WithTimestamp(baseTime.AddMinutes(i))
                .WithOpen(50000m + i * 10)
                .WithClose(50000m + i * 10)
                .Build());
        }

        return data;
    }
}
