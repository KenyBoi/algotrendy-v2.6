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

public class MomentumStrategyTests
{
    private readonly Mock<IndicatorService> _mockIndicatorService;
    private readonly Mock<ILogger<MomentumStrategy>> _mockLogger;
    private readonly MomentumStrategyConfig _config;
    private readonly MomentumStrategy _strategy;

    public MomentumStrategyTests()
    {
        _mockIndicatorService = new Mock<IndicatorService>(
            Mock.Of<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
            Mock.Of<ILogger<IndicatorService>>());

        _mockLogger = new Mock<ILogger<MomentumStrategy>>();

        _config = new MomentumStrategyConfig
        {
            BuyThreshold = 2.0m,
            SellThreshold = -2.0m,
            VolatilityFilter = 0.15m,
            MinVolumeThreshold = 100000m
        };

        _strategy = new MomentumStrategy(_config, _mockIndicatorService.Object, _mockLogger.Object);
    }

    [Fact]
    public void StrategyName_ReturnsCorrectName()
    {
        // Act & Assert
        _strategy.StrategyName.Should().Be("Momentum");
    }

    [Fact]
    public async Task AnalyzeAsync_WithStrongUpwardMomentum_GeneratesBuySignal()
    {
        // Arrange
        var currentData = new MarketDataBuilder()
            .WithOpen(50000m)
            .WithClose(51500m)  // +3% change (above 2% threshold)
            .WithVolume(150000m)
            .Build();

        var historicalData = CreateHistoricalData(50);

        _mockIndicatorService
            .Setup(x => x.CalculateVolatilityAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<MarketData>>(),
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0.08m); // Low volatility

        // Act
        var signal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Assert
        signal.Should().NotBeNull();
        signal.Action.Should().Be(SignalAction.Buy);
        signal.Confidence.Should().BeGreaterThan(0.3m);
        signal.Reason.Should().Contain("STRONG UPWARD");
        signal.EntryPrice.Should().Be(51500m);
        signal.StopLoss.Should().Be(51500m * 0.98m);
        signal.TakeProfit.Should().Be(51500m * 1.05m);
    }

    [Fact]
    public async Task AnalyzeAsync_WithStrongDownwardMomentum_GeneratesSellSignal()
    {
        // Arrange
        var currentData = new MarketDataBuilder()
            .WithOpen(50000m)
            .WithClose(48500m)  // -3% change (below -2% threshold)
            .WithVolume(150000m)
            .Build();

        var historicalData = CreateHistoricalData(50);

        _mockIndicatorService
            .Setup(x => x.CalculateVolatilityAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<MarketData>>(),
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0.08m);

        // Act
        var signal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Assert
        signal.Action.Should().Be(SignalAction.Sell);
        signal.Confidence.Should().BeGreaterThan(0.3m);
        signal.Reason.Should().Contain("STRONG DOWNWARD");
        signal.StopLoss.Should().Be(48500m * 1.02m);
        signal.TakeProfit.Should().Be(48500m * 0.95m);
    }

    [Fact]
    public async Task AnalyzeAsync_WithModerateChange_GeneratesHoldSignal()
    {
        // Arrange
        var currentData = new MarketDataBuilder()
            .WithOpen(50000m)
            .WithClose(50500m)  // +1% change (below threshold)
            .WithVolume(150000m)
            .Build();

        var historicalData = CreateHistoricalData(50);

        _mockIndicatorService
            .Setup(x => x.CalculateVolatilityAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<MarketData>>(),
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0.08m);

        // Act
        var signal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Assert
        signal.Action.Should().Be(SignalAction.Hold);
        signal.Confidence.Should().Be(0.3m);
    }

    [Fact]
    public async Task AnalyzeAsync_WithHighVolatility_GeneratesHoldSignal()
    {
        // Arrange
        var currentData = new MarketDataBuilder()
            .WithOpen(50000m)
            .WithClose(51500m)  // +3% change
            .WithVolume(150000m)
            .Build();

        var historicalData = CreateHistoricalData(50);

        _mockIndicatorService
            .Setup(x => x.CalculateVolatilityAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<MarketData>>(),
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0.20m); // High volatility (above 0.15 filter)

        // Act
        var signal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Assert
        signal.Action.Should().Be(SignalAction.Hold);
        signal.Reason.Should().Contain("High Volatility");
        signal.Reason.Should().Contain("FILTERED");
    }

    [Fact]
    public async Task AnalyzeAsync_WithLowVolume_ReducesConfidence()
    {
        // Arrange
        var currentData = new MarketDataBuilder()
            .WithOpen(50000m)
            .WithClose(51500m)  // +3% change
            .WithVolume(50000m)  // Below min threshold of 100,000
            .Build();

        var historicalData = CreateHistoricalData(50);

        _mockIndicatorService
            .Setup(x => x.CalculateVolatilityAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<MarketData>>(),
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0.08m);

        // Act
        var signal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Assert
        signal.Action.Should().Be(SignalAction.Buy);
        signal.Reason.Should().Contain("Low Volume");
        // Confidence should be reduced by 30% (multiplied by 0.7)
        signal.Confidence.Should().BeLessThan(0.6m);
    }

    [Theory]
    [InlineData(50000, 51000, SignalAction.Hold)] // +2% exactly at threshold (not triggered, needs >)
    [InlineData(50000, 51500, SignalAction.Buy)]  // +3% above threshold
    [InlineData(50000, 49000, SignalAction.Hold)] // -2% exactly at threshold (not triggered, needs <)
    [InlineData(50000, 48500, SignalAction.Sell)] // -3% below threshold
    [InlineData(50000, 50500, SignalAction.Hold)] // +1% below threshold
    [InlineData(50000, 49500, SignalAction.Hold)] // -1% above threshold
    public async Task AnalyzeAsync_WithVariousMomentums_GeneratesCorrectSignal(
        decimal open, decimal close, SignalAction expectedAction)
    {
        // Arrange
        var currentData = new MarketDataBuilder()
            .WithOpen(open)
            .WithClose(close)
            .WithVolume(150000m)
            .Build();

        var historicalData = CreateHistoricalData(50);

        _mockIndicatorService
            .Setup(x => x.CalculateVolatilityAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<MarketData>>(),
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0.08m);

        // Act
        var signal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Assert
        signal.Action.Should().Be(expectedAction);
    }

    [Fact]
    public async Task AnalyzeAsync_ConfidenceScale_IncreasesWithStrongerMomentum()
    {
        // Arrange
        var weakData = new MarketDataBuilder().WithOpen(50000m).WithClose(51000m).WithVolume(150000m).Build(); // +2%
        var strongData = new MarketDataBuilder().WithOpen(50000m).WithClose(52500m).WithVolume(150000m).Build(); // +5%
        var historicalData = CreateHistoricalData(50);

        _mockIndicatorService
            .Setup(x => x.CalculateVolatilityAsync(It.IsAny<string>(), It.IsAny<IEnumerable<MarketData>>(), 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0.08m);

        // Act
        var weakSignal = await _strategy.AnalyzeAsync(weakData, historicalData, CancellationToken.None);
        var strongSignal = await _strategy.AnalyzeAsync(strongData, historicalData, CancellationToken.None);

        // Assert
        strongSignal.Confidence.Should().BeGreaterThan(weakSignal.Confidence);
        strongSignal.Confidence.Should().BeLessThanOrEqualTo(0.95m); // Capped at 0.95
    }

    [Fact]
    public async Task AnalyzeAsync_WithException_ReturnsHoldSignalWithError()
    {
        // Arrange
        var currentData = new MarketDataBuilder().Build();
        var historicalData = CreateHistoricalData(50);

        _mockIndicatorService
            .Setup(x => x.CalculateVolatilityAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<MarketData>>(),
                20,
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
    public async Task AnalyzeAsync_SetsTimestamp()
    {
        // Arrange
        var before = DateTime.UtcNow;
        var currentData = new MarketDataBuilder().WithVolume(150000m).Build();
        var historicalData = CreateHistoricalData(50);

        _mockIndicatorService
            .Setup(x => x.CalculateVolatilityAsync(It.IsAny<string>(), It.IsAny<IEnumerable<MarketData>>(), 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0.08m);

        // Act
        var signal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);
        var after = DateTime.UtcNow;

        // Assert
        signal.Timestamp.Should().BeOnOrAfter(before);
        signal.Timestamp.Should().BeOnOrBefore(after);
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
                .WithVolume(100000m)
                .Build());
        }

        return data;
    }
}
