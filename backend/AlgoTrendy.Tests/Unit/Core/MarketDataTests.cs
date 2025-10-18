using AlgoTrendy.Core.Models;
using AlgoTrendy.Tests.TestHelpers.Builders;
using FluentAssertions;
using Xunit;

namespace AlgoTrendy.Tests.Unit.Core;

public class MarketDataTests
{
    [Fact]
    public void MarketData_Creation_WithValidData_Succeeds()
    {
        // Arrange & Act
        var marketData = new MarketDataBuilder()
            .WithSymbol("ETHUSDT")
            .WithOpen(3000m)
            .WithHigh(3100m)
            .WithLow(2950m)
            .WithClose(3050m)
            .WithVolume(1000m)
            .Build();

        // Assert
        marketData.Should().NotBeNull();
        marketData.Symbol.Should().Be("ETHUSDT");
        marketData.Open.Should().Be(3000m);
        marketData.High.Should().Be(3100m);
        marketData.Low.Should().Be(2950m);
        marketData.Close.Should().Be(3050m);
        marketData.Volume.Should().Be(1000m);
    }

    [Fact]
    public void ChangePercent_WithPriceIncrease_ReturnsPositiveValue()
    {
        // Arrange
        var marketData = new MarketDataBuilder()
            .WithOpen(50000m)
            .WithClose(51000m)
            .Build();

        // Act
        var changePercent = marketData.ChangePercent;

        // Assert
        changePercent.Should().BeApproximately(2.0m, 0.01m);
    }

    [Fact]
    public void ChangePercent_WithPriceDecrease_ReturnsNegativeValue()
    {
        // Arrange
        var marketData = new MarketDataBuilder()
            .WithOpen(50000m)
            .WithClose(49000m)
            .Build();

        // Act
        var changePercent = marketData.ChangePercent;

        // Assert
        changePercent.Should().BeApproximately(-2.0m, 0.01m);
    }

    [Fact]
    public void ChangePercent_WithNoChange_ReturnsZero()
    {
        // Arrange
        var marketData = new MarketDataBuilder()
            .WithOpen(50000m)
            .WithClose(50000m)
            .Build();

        // Act
        var changePercent = marketData.ChangePercent;

        // Assert
        changePercent.Should().Be(0m);
    }

    [Fact]
    public void ChangePercent_WithZeroOpen_ReturnsZero()
    {
        // Arrange
        var marketData = new MarketDataBuilder()
            .WithOpen(0m)
            .WithClose(100m)
            .Build();

        // Act
        var changePercent = marketData.ChangePercent;

        // Assert
        changePercent.Should().Be(0m);
    }

    [Fact]
    public void Range_CalculatesCorrectly()
    {
        // Arrange
        var marketData = new MarketDataBuilder()
            .WithHigh(51000m)
            .WithLow(49000m)
            .Build();

        // Act
        var range = marketData.Range;

        // Assert
        range.Should().Be(2000m);
    }

    [Fact]
    public void Range_WithNoMovement_ReturnsZero()
    {
        // Arrange
        var marketData = new MarketDataBuilder()
            .WithHigh(50000m)
            .WithLow(50000m)
            .Build();

        // Act
        var range = marketData.Range;

        // Assert
        range.Should().Be(0m);
    }

    [Fact]
    public void IsBullish_WhenCloseGreaterThanOpen_ReturnsTrue()
    {
        // Arrange
        var marketData = new MarketDataBuilder()
            .Bullish()
            .Build();

        // Act & Assert
        marketData.IsBullish.Should().BeTrue();
        marketData.Close.Should().BeGreaterThan(marketData.Open);
    }

    [Fact]
    public void IsBullish_WhenCloseLessThanOpen_ReturnsFalse()
    {
        // Arrange
        var marketData = new MarketDataBuilder()
            .Bearish()
            .Build();

        // Act & Assert
        marketData.IsBullish.Should().BeFalse();
    }

    [Fact]
    public void IsBearish_WhenCloseLessThanOpen_ReturnsTrue()
    {
        // Arrange
        var marketData = new MarketDataBuilder()
            .Bearish()
            .Build();

        // Act & Assert
        marketData.IsBearish.Should().BeTrue();
        marketData.Close.Should().BeLessThan(marketData.Open);
    }

    [Fact]
    public void IsBearish_WhenCloseGreaterThanOpen_ReturnsFalse()
    {
        // Arrange
        var marketData = new MarketDataBuilder()
            .Bullish()
            .Build();

        // Act & Assert
        marketData.IsBearish.Should().BeFalse();
    }

    [Fact]
    public void MarketData_Doji_IsNeitherBullishNorBearish()
    {
        // Arrange
        var marketData = new MarketDataBuilder()
            .Neutral()
            .Build();

        // Act & Assert
        marketData.IsBullish.Should().BeFalse();
        marketData.IsBearish.Should().BeFalse();
        marketData.Open.Should().Be(marketData.Close);
    }

    [Fact]
    public void MarketData_WithQuoteVolume_StoresCorrectly()
    {
        // Arrange & Act
        var marketData = new MarketDataBuilder()
            .WithVolume(100m)
            .WithQuoteVolume(5000000m)
            .Build();

        // Assert
        marketData.Volume.Should().Be(100m);
        marketData.QuoteVolume.Should().Be(5000000m);
    }

    [Fact]
    public void MarketData_WithTradesCount_StoresCorrectly()
    {
        // Arrange & Act
        var marketData = new MarketDataBuilder()
            .WithTradesCount(1500)
            .Build();

        // Assert
        marketData.TradesCount.Should().Be(1500);
    }

    [Fact]
    public void MarketData_WithSource_StoresExchangeName()
    {
        // Arrange & Act
        var marketData = new MarketDataBuilder()
            .WithSource("binance")
            .Build();

        // Assert
        marketData.Source.Should().Be("binance");
    }

    [Theory]
    [InlineData(40000, 42000, 39000, 41000, 2.5)]
    [InlineData(100, 105, 98, 102, 2.0)]
    [InlineData(1000, 950, 900, 920, -8.0)]
    public void ChangePercent_CalculatesCorrectly_ForVariousScenarios(
        decimal open, decimal high, decimal low, decimal close, decimal expectedPercent)
    {
        // Arrange
        var marketData = new MarketDataBuilder()
            .WithOpen(open)
            .WithHigh(high)
            .WithLow(low)
            .WithClose(close)
            .Build();

        // Act
        var changePercent = marketData.ChangePercent;

        // Assert
        changePercent.Should().BeApproximately(expectedPercent, 0.01m);
    }

    [Fact]
    public void MarketData_OHLC_MaintainsRelationship()
    {
        // Arrange & Act
        var marketData = new MarketDataBuilder()
            .WithOpen(50000m)
            .WithHigh(51000m)
            .WithLow(49000m)
            .WithClose(50500m)
            .Build();

        // Assert - High should be >= all prices
        marketData.High.Should().BeGreaterThanOrEqualTo(marketData.Open);
        marketData.High.Should().BeGreaterThanOrEqualTo(marketData.Close);
        marketData.High.Should().BeGreaterThanOrEqualTo(marketData.Low);

        // Low should be <= all prices
        marketData.Low.Should().BeLessThanOrEqualTo(marketData.Open);
        marketData.Low.Should().BeLessThanOrEqualTo(marketData.Close);
        marketData.Low.Should().BeLessThanOrEqualTo(marketData.High);
    }

    [Fact]
    public void MarketData_WithTimestamp_StoresCorrectly()
    {
        // Arrange
        var timestamp = DateTime.UtcNow.AddHours(-1);

        // Act
        var marketData = new MarketDataBuilder()
            .WithTimestamp(timestamp)
            .Build();

        // Assert
        marketData.Timestamp.Should().Be(timestamp);
    }

    [Fact]
    public void MarketData_WithMetadata_StoresJsonString()
    {
        // Arrange & Act
        var metadata = "{\"source_feed\":\"websocket\"}";
        var marketData = new MarketDataBuilder()
            .WithMetadata(metadata)
            .Build();

        // Assert
        marketData.Metadata.Should().Be(metadata);
    }
}
