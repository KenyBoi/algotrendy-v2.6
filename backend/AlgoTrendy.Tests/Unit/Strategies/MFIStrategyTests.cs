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

public class MFIStrategyTests
{
    private readonly Mock<IndicatorService> _mockIndicatorService;
    private readonly Mock<ILogger<MFIStrategy>> _mockLogger;
    private readonly MFIStrategyConfig _config;
    private readonly MFIStrategy _strategy;

    public MFIStrategyTests()
    {
        _mockIndicatorService = new Mock<IndicatorService>(
            Mock.Of<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
            Mock.Of<ILogger<IndicatorService>>());

        _mockLogger = new Mock<ILogger<MFIStrategy>>();

        _config = new MFIStrategyConfig
        {
            Period = 14,
            OversoldThreshold = 20m,
            OverboughtThreshold = 80m,
            MinVolumeThreshold = 50000m
        };

        _strategy = new MFIStrategy(_config, _mockIndicatorService.Object, _mockLogger.Object);
    }

    [Fact]
    public void StrategyName_ReturnsCorrectName()
    {
        // Act & Assert
        _strategy.StrategyName.Should().Be("MFI");
    }

    [Fact]
    public async Task AnalyzeAsync_WithOversoldMFI_GeneratesBuySignal()
    {
        // Arrange
        var currentData = new MarketDataBuilder()
            .WithClose(50000m)
            .WithVolume(100000m)
            .Build();

        var historicalData = CreateHistoricalData(50);

        _mockIndicatorService
            .Setup(x => x.CalculateMFIAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<MarketData>>(),
                14,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(15m); // Oversold (below 20)

        // Act
        var signal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Assert
        signal.Should().NotBeNull();
        signal.Action.Should().Be(SignalAction.Buy);
        signal.Confidence.Should().BeGreaterThan(0.5m);
        signal.Reason.Should().Contain("OVERSOLD");
        signal.Reason.Should().Contain("Money flowing out");
        signal.EntryPrice.Should().Be(50000m);
        signal.StopLoss.Should().Be(50000m * 0.97m);
        signal.TakeProfit.Should().Be(50000m * 1.06m);
    }

    [Fact]
    public async Task AnalyzeAsync_WithOverboughtMFI_GeneratesSellSignal()
    {
        // Arrange
        var currentData = new MarketDataBuilder()
            .WithClose(50000m)
            .WithVolume(100000m)
            .Build();

        var historicalData = CreateHistoricalData(50);

        _mockIndicatorService
            .Setup(x => x.CalculateMFIAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<MarketData>>(),
                14,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(85m); // Overbought (above 80)

        // Act
        var signal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Assert
        signal.Action.Should().Be(SignalAction.Sell);
        signal.Confidence.Should().BeGreaterThan(0.5m);
        signal.Reason.Should().Contain("OVERBOUGHT");
        signal.Reason.Should().Contain("Heavy buying");
        signal.EntryPrice.Should().Be(50000m);
        signal.StopLoss.Should().Be(50000m * 1.03m);
        signal.TakeProfit.Should().Be(50000m * 0.94m);
    }

    [Fact]
    public async Task AnalyzeAsync_WithNeutralMFI_GeneratesHoldSignal()
    {
        // Arrange
        var currentData = new MarketDataBuilder()
            .WithClose(50000m)
            .WithVolume(100000m)
            .Build();

        var historicalData = CreateHistoricalData(50);

        _mockIndicatorService
            .Setup(x => x.CalculateMFIAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<MarketData>>(),
                14,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(50m); // Neutral (between 20 and 80)

        // Act
        var signal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Assert
        signal.Action.Should().Be(SignalAction.Hold);
        signal.Reason.Should().Contain("NEUTRAL");
        signal.Reason.Should().Contain("Balanced money flow");
    }

    [Fact]
    public async Task AnalyzeAsync_WithExtremeOversold_HasHighConfidence()
    {
        // Arrange
        var currentData = new MarketDataBuilder()
            .WithClose(50000m)
            .WithVolume(100000m)
            .Build();

        var historicalData = CreateHistoricalData(50);

        _mockIndicatorService
            .Setup(x => x.CalculateMFIAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<MarketData>>(),
                14,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(5m); // Extremely oversold

        // Act
        var signal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Assert
        signal.Action.Should().Be(SignalAction.Buy);
        signal.Confidence.Should().BeGreaterThan(0.7m);
        // (20 - 5) / 20 = 0.75
    }

    [Fact]
    public async Task AnalyzeAsync_WithLowVolume_ReducesConfidence()
    {
        // Arrange
        var currentData = new MarketDataBuilder()
            .WithClose(50000m)
            .WithVolume(30000m) // Below MinVolumeThreshold
            .Build();

        var historicalData = CreateHistoricalData(50);

        _mockIndicatorService
            .Setup(x => x.CalculateMFIAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<MarketData>>(),
                14,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(15m);

        // Act
        var signal = await _strategy.AnalyzeAsync(currentData, historicalData, CancellationToken.None);

        // Assert
        signal.Action.Should().Be(SignalAction.Buy);
        signal.Reason.Should().Contain("Low Volume");
        // Confidence should be reduced by 0.8x factor (less penalty than other strategies)
    }

    [Fact]
    public async Task AnalyzeAsync_WithException_ReturnsHoldSignal()
    {
        // Arrange
        var currentData = new MarketDataBuilder().Build();
        var historicalData = CreateHistoricalData(5);

        _mockIndicatorService
            .Setup(x => x.CalculateMFIAsync(
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
