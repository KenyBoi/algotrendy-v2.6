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

public class VWAPStrategyTests
{
    private readonly Mock<IndicatorService> _mockIndicatorService;
    private readonly Mock<ILogger<VWAPStrategy>> _mockLogger;
    private readonly VWAPStrategyConfig _config;
    private readonly VWAPStrategy _strategy;

    public VWAPStrategyTests()
    {
        _mockIndicatorService = new Mock<IndicatorService>(
            Mock.Of<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
            Mock.Of<ILogger<IndicatorService>>());

        _mockLogger = new Mock<ILogger<VWAPStrategy>>();

        _config = new VWAPStrategyConfig
        {
            Period = 20,
            BuyDeviationThreshold = -2.0m,
            SellDeviationThreshold = 2.0m,
            UseVolumeConfirmation = true
        };

        _strategy = new VWAPStrategy(_config, _mockIndicatorService.Object, _mockLogger.Object);
    }

    [Fact]
    public void StrategyName_ReturnsCorrectName()
    {
        // Act & Assert
        _strategy.StrategyName.Should().Be("VWAP");
    }

    [Fact]
    public async Task AnalyzeAsync_WithPriceBelowVWAP_GeneratesBuySignal()
    {
        // Arrange
        var currentData = new MarketDataBuilder()
            .WithClose(48000m) // Price below VWAP
            .WithVolume(100000m)
            .Build();

        var historicalData = CreateHistoricalData(50);

        _mockIndicatorService
            .Setup(x => x.CalculateVWAPAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<MarketData>>(),
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(50000m); // VWAP

        // Act
        var signal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Assert
        signal.Should().NotBeNull();
        signal.Action.Should().Be(SignalAction.Buy);
        // (48000 - 50000) / 50000 * 100 = -4% (below -2% threshold)
        signal.Confidence.Should().BeGreaterThanOrEqualTo(0.5m);
        signal.Reason.Should().Contain("DISCOUNT");
        signal.EntryPrice.Should().Be(48000m);
        signal.StopLoss.Should().Be(48000m * 0.97m);
        // Take profit should be near VWAP
        signal.TakeProfit.Should().Be(50000m * 1.005m);
    }

    [Fact]
    public async Task AnalyzeAsync_WithPriceAboveVWAP_GeneratesSellSignal()
    {
        // Arrange
        var currentData = new MarketDataBuilder()
            .WithClose(52000m) // Price above VWAP
            .WithVolume(100000m)
            .Build();

        var historicalData = CreateHistoricalData(50);

        _mockIndicatorService
            .Setup(x => x.CalculateVWAPAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<MarketData>>(),
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(50000m); // VWAP

        // Act
        var signal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Assert
        signal.Action.Should().Be(SignalAction.Sell);
        // (52000 - 50000) / 50000 * 100 = +4% (above +2% threshold)
        signal.Confidence.Should().BeGreaterThanOrEqualTo(0.5m);
        signal.Reason.Should().Contain("PREMIUM");
        signal.EntryPrice.Should().Be(52000m);
        signal.StopLoss.Should().Be(52000m * 1.03m);
        signal.TakeProfit.Should().Be(50000m * 0.995m);
    }

    [Fact]
    public async Task AnalyzeAsync_WithPriceNearVWAP_GeneratesHoldSignal()
    {
        // Arrange
        var currentData = new MarketDataBuilder()
            .WithClose(50500m) // Price near VWAP (+1%)
            .WithVolume(100000m)
            .Build();

        var historicalData = CreateHistoricalData(50);

        _mockIndicatorService
            .Setup(x => x.CalculateVWAPAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<MarketData>>(),
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(50000m); // VWAP

        // Act
        var signal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Assert
        signal.Action.Should().Be(SignalAction.Hold);
        signal.Reason.Should().Contain("FAIR VALUE");
    }

    [Fact]
    public async Task AnalyzeAsync_WithHighVolume_IncreasesConfidence()
    {
        // Arrange
        var currentData = new MarketDataBuilder()
            .WithClose(48000m)
            .WithVolume(200000m) // High volume (2x average)
            .Build();

        var historicalData = CreateHistoricalData(50);

        _mockIndicatorService
            .Setup(x => x.CalculateVWAPAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<MarketData>>(),
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(50000m);

        // Act
        var signal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Assert
        signal.Action.Should().Be(SignalAction.Buy);
        signal.Reason.Should().Contain("High Volume Confirmation");
        // Confidence should be increased by 1.1x factor (capped at 0.95)
    }

    [Fact]
    public async Task AnalyzeAsync_WithLowVolume_ReducesConfidence()
    {
        // Arrange
        var currentData = new MarketDataBuilder()
            .WithClose(48000m)
            .WithVolume(40000m) // Low volume (0.4x average)
            .Build();

        var historicalData = CreateHistoricalData(50);

        _mockIndicatorService
            .Setup(x => x.CalculateVWAPAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<MarketData>>(),
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(50000m);

        // Act
        var signal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Assert
        signal.Action.Should().Be(SignalAction.Buy);
        signal.Reason.Should().Contain("Low Volume");
        // Confidence should be reduced by 0.8x factor
    }

    [Fact]
    public async Task AnalyzeAsync_WithLargeDeviation_HasHighConfidence()
    {
        // Arrange
        var currentData = new MarketDataBuilder()
            .WithClose(45000m) // -10% from VWAP
            .WithVolume(100000m)
            .Build();

        var historicalData = CreateHistoricalData(50);

        _mockIndicatorService
            .Setup(x => x.CalculateVWAPAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<MarketData>>(),
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(50000m);

        // Act
        var signal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Assert
        signal.Action.Should().Be(SignalAction.Buy);
        // Larger deviation should result in higher confidence
        signal.Confidence.Should().BeGreaterThan(0.7m);
    }

    [Fact]
    public async Task AnalyzeAsync_WithException_ReturnsHoldSignal()
    {
        // Arrange
        var currentData = new MarketDataBuilder().Build();
        var historicalData = CreateHistoricalData(5);

        _mockIndicatorService
            .Setup(x => x.CalculateVWAPAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<MarketData>>(),
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
