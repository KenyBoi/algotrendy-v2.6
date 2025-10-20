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

public class MACDStrategyTests
{
    private readonly Mock<IndicatorService> _mockIndicatorService;
    private readonly Mock<ILogger<MACDStrategy>> _mockLogger;
    private readonly MACDStrategyConfig _config;
    private readonly MACDStrategy _strategy;

    public MACDStrategyTests()
    {
        _mockIndicatorService = new Mock<IndicatorService>(
            Mock.Of<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
            Mock.Of<ILogger<IndicatorService>>());

        _mockLogger = new Mock<ILogger<MACDStrategy>>();

        _config = new MACDStrategyConfig
        {
            FastPeriod = 12,
            SlowPeriod = 26,
            SignalPeriod = 9,
            BuyThreshold = 0.0001m,
            SellThreshold = -0.0001m,
            MinVolumeThreshold = 100000m
        };

        _strategy = new MACDStrategy(_config, _mockIndicatorService.Object, _mockLogger.Object);
    }

    [Fact]
    public void StrategyName_ReturnsCorrectName()
    {
        // Act & Assert
        _strategy.StrategyName.Should().Be("MACD");
    }

    [Fact]
    public async Task AnalyzeAsync_WithBullishCrossover_GeneratesBuySignal()
    {
        // Arrange
        var currentData = new MarketDataBuilder()
            .WithClose(50000m)
            .WithVolume(200000m)
            .Build();

        var historicalData = CreateHistoricalData(50);

        var macdResult = new MACDResult
        {
            MACD = 10.5m,
            Signal = 8.2m,
            Histogram = 2.3m // Positive histogram = bullish
        };

        _mockIndicatorService
            .Setup(x => x.CalculateMACDAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<MarketData>>(),
                12,
                26,
                9,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(macdResult);

        // Act
        var signal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Assert
        signal.Should().NotBeNull();
        signal.Action.Should().Be(SignalAction.Buy);
        signal.Confidence.Should().BeGreaterThan(0.5m);
        signal.Reason.Should().Contain("BULLISH CROSSOVER");
        signal.EntryPrice.Should().Be(50000m);
        signal.StopLoss.Should().Be(50000m * 0.97m);
        signal.TakeProfit.Should().Be(50000m * 1.06m);
    }

    [Fact]
    public async Task AnalyzeAsync_WithBearishCrossover_GeneratesSellSignal()
    {
        // Arrange
        var currentData = new MarketDataBuilder()
            .WithClose(50000m)
            .WithVolume(200000m)
            .Build();

        var historicalData = CreateHistoricalData(50);

        var macdResult = new MACDResult
        {
            MACD = 8.2m,
            Signal = 10.5m,
            Histogram = -2.3m // Negative histogram = bearish
        };

        _mockIndicatorService
            .Setup(x => x.CalculateMACDAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<MarketData>>(),
                12,
                26,
                9,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(macdResult);

        // Act
        var signal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Assert
        signal.Action.Should().Be(SignalAction.Sell);
        signal.Confidence.Should().BeGreaterThan(0.5m);
        signal.Reason.Should().Contain("BEARISH CROSSOVER");
        signal.EntryPrice.Should().Be(50000m);
        signal.StopLoss.Should().Be(50000m * 1.03m);
        signal.TakeProfit.Should().Be(50000m * 0.94m);
    }

    [Fact]
    public async Task AnalyzeAsync_WithNeutralMACD_GeneratesHoldSignal()
    {
        // Arrange
        var currentData = new MarketDataBuilder()
            .WithClose(50000m)
            .WithVolume(200000m)
            .Build();

        var historicalData = CreateHistoricalData(50);

        var macdResult = new MACDResult
        {
            MACD = 10.0m,
            Signal = 10.0m,
            Histogram = 0.0m // Neutral
        };

        _mockIndicatorService
            .Setup(x => x.CalculateMACDAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<MarketData>>(),
                12,
                26,
                9,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(macdResult);

        // Act
        var signal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Assert
        signal.Action.Should().Be(SignalAction.Hold);
        signal.Reason.Should().Contain("NEUTRAL");
    }

    [Fact]
    public async Task AnalyzeAsync_WithLowVolume_ReducesConfidence()
    {
        // Arrange
        var currentData = new MarketDataBuilder()
            .WithClose(50000m)
            .WithVolume(50000m) // Below MinVolumeThreshold
            .Build();

        var historicalData = CreateHistoricalData(50);

        var macdResult = new MACDResult
        {
            MACD = 10.5m,
            Signal = 8.2m,
            Histogram = 2.3m
        };

        _mockIndicatorService
            .Setup(x => x.CalculateMACDAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<MarketData>>(),
                12,
                26,
                9,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(macdResult);

        // Act
        var signal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Assert
        signal.Action.Should().Be(SignalAction.Buy);
        signal.Reason.Should().Contain("Low Volume");
        // Confidence should be reduced by 0.7x factor
    }

    [Fact]
    public async Task AnalyzeAsync_WithException_ReturnsHoldSignal()
    {
        // Arrange
        var currentData = new MarketDataBuilder().Build();
        var historicalData = CreateHistoricalData(5);

        _mockIndicatorService
            .Setup(x => x.CalculateMACDAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<MarketData>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var signal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Assert
        signal.Action.Should().Be(SignalAction.Hold);
        signal.Confidence.Should().Be(0.0m);
        signal.Reason.Should().Contain("Error");
    }

    private static List<MarketData> CreateHistoricalData(int count)
    {
        var data = new List<MarketData>();
        var basePrice = 50000m;

        for (int i = 0; i < count; i++)
        {
            data.Add(new MarketDataBuilder()
                .WithSymbol("BTCUSDT")
                .WithTimestamp(DateTime.UtcNow.AddMinutes(-count + i))
                .WithOpen(basePrice + (i * 10))
                .WithHigh(basePrice + (i * 10) + 100)
                .WithLow(basePrice + (i * 10) - 100)
                .WithClose(basePrice + (i * 10) + 50)
                .WithVolume(100000m + (i * 1000))
                .Build());
        }

        return data;
    }
}
