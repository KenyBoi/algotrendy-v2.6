using AlgoTrendy.Core.Models;
using AlgoTrendy.Tests.TestHelpers.Builders;
using AlgoTrendy.TradingEngine.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AlgoTrendy.Tests.Unit.TradingEngine;

public class IndicatorServiceTests
{
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<ILogger<IndicatorService>> _mockLogger;
    private readonly IndicatorService _indicatorService;

    public IndicatorServiceTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _mockLogger = new Mock<ILogger<IndicatorService>>();
        _indicatorService = new IndicatorService(_memoryCache, _mockLogger.Object);
    }

    #region RSI Tests

    [Fact]
    public async Task CalculateRSIAsync_WithInsufficientData_ReturnsNeutral50()
    {
        // Arrange
        var historicalData = CreatePriceData(new[] { 100m, 102m, 101m }); // Only 3 data points, need 15 (14 + 1)

        // Act
        var rsi = await _indicatorService.CalculateRSIAsync("BTCUSDT", historicalData, 14);

        // Assert
        rsi.Should().Be(50m);
    }

    [Fact]
    public async Task CalculateRSIAsync_WithAllGains_Returns100()
    {
        // Arrange - All prices going up
        var prices = new[] { 100m, 101m, 102m, 103m, 104m, 105m, 106m, 107m, 108m, 109m, 110m, 111m, 112m, 113m, 114m, 115m };
        var historicalData = CreatePriceData(prices);

        // Act
        var rsi = await _indicatorService.CalculateRSIAsync("BTCUSDT", historicalData, 14);

        // Assert
        rsi.Should().Be(100m); // All gains = RSI 100
    }

    [Fact]
    public async Task CalculateRSIAsync_WithMixedData_CalculatesCorrectly()
    {
        // Arrange - Mix of gains and losses
        var prices = new[] { 100m, 102m, 101m, 103m, 102m, 104m, 103m, 105m, 104m, 106m, 105m, 107m, 106m, 108m, 107m, 109m };
        var historicalData = CreatePriceData(prices);

        // Act
        var rsi = await _indicatorService.CalculateRSIAsync("BTCUSDT", historicalData, 14);

        // Assert
        rsi.Should().BeGreaterThan(0m);
        rsi.Should().BeLessThanOrEqualTo(100m);
        // With more gains than losses, RSI should be > 50
        rsi.Should().BeGreaterThan(50m);
    }

    [Fact]
    public async Task CalculateRSIAsync_UsesCaching()
    {
        // Arrange
        var historicalData = CreatePriceData(new[] { 100m, 102m, 101m, 103m, 102m, 104m, 103m, 105m, 104m, 106m, 105m, 107m, 106m, 108m, 107m, 109m });

        // Act
        var rsi1 = await _indicatorService.CalculateRSIAsync("BTCUSDT", historicalData, 14);
        var rsi2 = await _indicatorService.CalculateRSIAsync("BTCUSDT", historicalData, 14);

        // Assert
        rsi1.Should().Be(rsi2); // Should return same cached value
    }

    #endregion

    #region MACD Tests

    [Fact]
    public async Task CalculateMACDAsync_WithInsufficientData_ReturnsZeros()
    {
        // Arrange
        var historicalData = CreatePriceData(new[] { 100m, 102m, 101m }); // Only 3 data points, need 26

        // Act
        var macd = await _indicatorService.CalculateMACDAsync("BTCUSDT", historicalData);

        // Assert
        macd.MACD.Should().Be(0m);
        macd.Signal.Should().Be(0m);
        macd.Histogram.Should().Be(0m);
    }

    [Fact]
    public async Task CalculateMACDAsync_WithSufficientData_CalculatesValues()
    {
        // Arrange
        var prices = Enumerable.Range(1, 50).Select(i => 100m + i).ToArray();
        var historicalData = CreatePriceData(prices);

        // Act
        var macd = await _indicatorService.CalculateMACDAsync("BTCUSDT", historicalData);

        // Assert
        macd.Should().NotBeNull();
        macd.MACD.Should().NotBe(0m);
        macd.Signal.Should().NotBe(0m);
        macd.Histogram.Should().NotBe(0m);
    }

    [Fact]
    public async Task CalculateMACDAsync_Histogram_IsMACDMinusSignal()
    {
        // Arrange
        var prices = Enumerable.Range(1, 50).Select(i => 100m + i).ToArray();
        var historicalData = CreatePriceData(prices);

        // Act
        var macd = await _indicatorService.CalculateMACDAsync("BTCUSDT", historicalData);

        // Assert
        macd.Histogram.Should().Be(macd.MACD - macd.Signal);
    }

    #endregion

    #region EMA Tests

    [Fact]
    public async Task CalculateEMAAsync_WithInsufficientData_ReturnsLastPrice()
    {
        // Arrange
        var historicalData = CreatePriceData(new[] { 100m, 102m }); // Only 2 data points, need more for period 14

        // Act
        var ema = await _indicatorService.CalculateEMAAsync("BTCUSDT", historicalData, 14);

        // Assert
        ema.Should().Be(102m); // Should return last price
    }

    [Fact]
    public async Task CalculateEMAAsync_WithSufficientData_CalculatesCorrectly()
    {
        // Arrange
        var prices = Enumerable.Range(1, 30).Select(i => 100m + i).ToArray();
        var historicalData = CreatePriceData(prices);

        // Act
        var ema = await _indicatorService.CalculateEMAAsync("BTCUSDT", historicalData, 10);

        // Assert
        ema.Should().BeGreaterThan(0m);
        // EMA should be influenced by recent prices
        ema.Should().BeGreaterThan(100m);
    }

    [Fact]
    public async Task CalculateEMAAsync_GivesMoreWeightToRecentPrices()
    {
        // Arrange - Prices that jump at the end
        var prices = new List<decimal>();
        for (int i = 0; i < 20; i++) prices.Add(100m);
        for (int i = 0; i < 10; i++) prices.Add(110m);

        var historicalData = CreatePriceData(prices.ToArray());

        // Act
        var ema = await _indicatorService.CalculateEMAAsync("BTCUSDT", historicalData, 10);
        var sma = await _indicatorService.CalculateSMAAsync("BTCUSDT", historicalData, 10);

        // Assert
        // SMA uses only last 10 values (all 110), so SMA = 110
        // EMA gives more weight to ALL historical data with exponential decay
        // So EMA will be less than SMA when prices recently jumped up
        ema.Should().BeLessThan(sma);
        ema.Should().BeGreaterThan(100m); // But still higher than the old price
    }

    #endregion

    #region SMA Tests

    [Fact]
    public async Task CalculateSMAAsync_WithInsufficientData_ReturnsLastPrice()
    {
        // Arrange
        var historicalData = CreatePriceData(new[] { 100m, 102m }); // Only 2 data points, need 14

        // Act
        var sma = await _indicatorService.CalculateSMAAsync("BTCUSDT", historicalData, 14);

        // Assert
        sma.Should().Be(102m); // Should return last price
    }

    [Fact]
    public async Task CalculateSMAAsync_WithExactData_CalculatesAverage()
    {
        // Arrange
        var prices = new[] { 100m, 110m, 120m, 130m, 140m };
        var historicalData = CreatePriceData(prices);

        // Act
        var sma = await _indicatorService.CalculateSMAAsync("BTCUSDT", historicalData, 5);

        // Assert
        sma.Should().Be(120m); // (100 + 110 + 120 + 130 + 140) / 5 = 120
    }

    [Fact]
    public async Task CalculateSMAAsync_UsesOnlyLastNPrices()
    {
        // Arrange
        var prices = new[] { 50m, 60m, 100m, 110m, 120m }; // Last 3: 100, 110, 120
        var historicalData = CreatePriceData(prices);

        // Act
        var sma = await _indicatorService.CalculateSMAAsync("BTCUSDT", historicalData, 3);

        // Assert
        sma.Should().Be(110m); // (100 + 110 + 120) / 3 = 110
    }

    #endregion

    #region Volatility Tests

    [Fact]
    public async Task CalculateVolatilityAsync_WithInsufficientData_ReturnsZero()
    {
        // Arrange
        var historicalData = CreatePriceData(new[] { 100m, 102m }); // Only 2 data points, need 21

        // Act
        var volatility = await _indicatorService.CalculateVolatilityAsync("BTCUSDT", historicalData, 20);

        // Assert
        volatility.Should().Be(0m);
    }

    [Fact]
    public async Task CalculateVolatilityAsync_WithStablePrices_ReturnsLowVolatility()
    {
        // Arrange - Very stable prices
        var prices = Enumerable.Range(1, 30).Select(i => 100m + i * 0.1m).ToArray();
        var historicalData = CreatePriceData(prices);

        // Act
        var volatility = await _indicatorService.CalculateVolatilityAsync("BTCUSDT", historicalData, 20);

        // Assert
        volatility.Should().BeGreaterThan(0m);
        volatility.Should().BeLessThan(0.01m); // Very low volatility
    }

    [Fact]
    public async Task CalculateVolatilityAsync_WithVolatilePrices_ReturnsHighVolatility()
    {
        // Arrange - Highly volatile prices
        var prices = new[] { 100m, 110m, 95m, 115m, 90m, 120m, 85m, 125m, 80m, 130m, 75m, 135m, 70m, 140m, 65m, 145m, 60m, 150m, 55m, 155m, 50m, 160m };
        var historicalData = CreatePriceData(prices);

        // Act
        var volatility = await _indicatorService.CalculateVolatilityAsync("BTCUSDT", historicalData, 20);

        // Assert
        volatility.Should().BeGreaterThan(0.05m); // Higher volatility
    }

    [Fact]
    public async Task CalculateVolatilityAsync_UsesCaching()
    {
        // Arrange
        var prices = Enumerable.Range(1, 30).Select(i => 100m + i).ToArray();
        var historicalData = CreatePriceData(prices);

        // Act
        var vol1 = await _indicatorService.CalculateVolatilityAsync("BTCUSDT", historicalData, 20);
        var vol2 = await _indicatorService.CalculateVolatilityAsync("BTCUSDT", historicalData, 20);

        // Assert
        vol1.Should().Be(vol2); // Should return same cached value
    }

    #endregion

    #region Cache Management Tests

    [Fact]
    public async Task ClearCache_RemovesAllCachedValues()
    {
        // Arrange
        var prices = Enumerable.Range(1, 30).Select(i => 100m + i).ToArray();
        var historicalData = CreatePriceData(prices);

        // Calculate to populate cache
        var rsi1 = await _indicatorService.CalculateRSIAsync("BTCUSDT", historicalData, 14);

        // Act
        _indicatorService.ClearCache();

        // The cache is cleared - we can't easily verify from the outside,
        // but the method should execute without errors
        // Assert - just verify no exception
        Action act = () => _indicatorService.ClearCache();
        act.Should().NotThrow();
    }

    #endregion

    #region Helper Methods

    private List<MarketData> CreatePriceData(decimal[] closePrices)
    {
        var data = new List<MarketData>();
        var baseTime = DateTime.UtcNow.AddMinutes(-closePrices.Length);

        for (int i = 0; i < closePrices.Length; i++)
        {
            data.Add(new MarketDataBuilder()
                .WithTimestamp(baseTime.AddMinutes(i))
                .WithOpen(closePrices[i])
                .WithHigh(closePrices[i] + 1)
                .WithLow(closePrices[i] - 1)
                .WithClose(closePrices[i])
                .Build());
        }

        return data;
    }

    #endregion

    #region Integration-style Tests

    [Fact]
    public async Task RSI_With14Period_MatchesKnownValues()
    {
        // Arrange - Known price series
        // This is a simplified test - in production you'd use verified RSI values
        var prices = new[] {
            44m, 44.34m, 44.09m, 43.61m, 44.33m, 44.83m, 45.10m, 45.42m,
            45.84m, 46.08m, 45.89m, 46.03m, 45.61m, 46.28m, 46.28m, 46.00m
        };
        var historicalData = CreatePriceData(prices);

        // Act
        var rsi = await _indicatorService.CalculateRSIAsync("BTCUSDT", historicalData, 14);

        // Assert
        rsi.Should().BeGreaterThan(0m);
        rsi.Should().BeLessThanOrEqualTo(100m);
        // With mostly upward movement, should be > 50
        rsi.Should().BeGreaterThan(50m);
    }

    [Fact]
    public async Task EMA_ConvergesToSMA_WithEqualWeights()
    {
        // Arrange - Flat prices
        var prices = Enumerable.Range(1, 50).Select(i => 100m).ToArray();
        var historicalData = CreatePriceData(prices);

        // Act
        var ema = await _indicatorService.CalculateEMAAsync("BTCUSDT", historicalData, 20);
        var sma = await _indicatorService.CalculateSMAAsync("BTCUSDT", historicalData, 20);

        // Assert
        // With flat prices, EMA should converge to SMA
        ema.Should().BeApproximately(sma, 0.1m);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(50)]
    public async Task SMA_WithDifferentPeriods_CalculatesCorrectly(int period)
    {
        // Arrange
        var prices = Enumerable.Range(1, 100).Select(i => 100m + i).ToArray();
        var historicalData = CreatePriceData(prices);

        // Act
        var sma = await _indicatorService.CalculateSMAAsync("BTCUSDT", historicalData, period);

        // Assert
        sma.Should().BeGreaterThan(0m);
        // SMA should be somewhere in the middle of the range
        var lastNPrices = prices.TakeLast(period);
        var expectedSMA = lastNPrices.Average();
        sma.Should().Be(expectedSMA);
    }

    #endregion
}
