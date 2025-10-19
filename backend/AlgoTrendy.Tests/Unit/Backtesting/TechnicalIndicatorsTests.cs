using AlgoTrendy.Backtesting.Indicators;
using FluentAssertions;
using Xunit;

namespace AlgoTrendy.Tests.Unit.Backtesting;

/// <summary>
/// Unit tests for technical indicators calculations
/// </summary>
public class TechnicalIndicatorsTests
{
    #region Simple Moving Average (SMA) Tests

    [Fact]
    public void CalculateSMA_WithValidData_ReturnsCorrectAverages()
    {
        // Arrange
        var prices = new decimal[] { 100m, 102m, 104m, 106m, 108m, 110m };
        var period = 3;

        // Act
        var sma = TechnicalIndicators.CalculateSMA(prices, period);

        // Assert
        sma.Should().HaveCount(6);
        sma[0].Should().BeNull();
        sma[1].Should().BeNull();
        sma[2].Should().Be(102m);
        sma[3].Should().Be(104m);
        sma[4].Should().Be(106m);
        sma[5].Should().Be(108m);
    }

    [Fact]
    public void CalculateSMA_WithPeriodOne_ReturnsPricesAsSMA()
    {
        // Arrange
        var prices = new decimal[] { 100m, 102m, 104m };
        var period = 1;

        // Act
        var sma = TechnicalIndicators.CalculateSMA(prices, period);

        // Assert
        sma.Should().ContainInOrder(100m, 102m, 104m);
    }

    [Fact]
    public void CalculateSMA_WithEmptyArray_ReturnsEmptyArray()
    {
        // Arrange
        var prices = new decimal[] { };

        // Act
        var sma = TechnicalIndicators.CalculateSMA(prices, 5);

        // Assert
        sma.Should().BeEmpty();
    }

    #endregion

    #region Exponential Moving Average (EMA) Tests

    [Fact]
    public void CalculateEMA_WithValidData_ReturnsCorrectValues()
    {
        // Arrange
        var prices = new decimal[] { 100m, 102m, 104m, 106m, 108m, 110m };
        var period = 3;

        // Act
        var ema = TechnicalIndicators.CalculateEMA(prices, period);

        // Assert
        ema.Should().HaveCount(6);
        for (int i = 0; i < period - 1; i++)
            ema[i].Should().BeNull();

        ema[period - 1].Should().NotBeNull();
        ema[period].Should().NotBeNull();
    }

    [Fact]
    public void CalculateEMA_WithConstantPrices_ReturnsConstantEMA()
    {
        // Arrange
        var prices = new decimal[] { 100m, 100m, 100m, 100m, 100m };
        var period = 3;

        // Act
        var ema = TechnicalIndicators.CalculateEMA(prices, period);

        // Assert
        for (int i = period - 1; i < ema.Length; i++)
            ema[i].Should().Be(100m);
    }

    #endregion

    #region Relative Strength Index (RSI) Tests

    [Fact]
    public void CalculateRSI_WithValidData_ReturnsValidRange()
    {
        // Arrange
        var prices = new decimal[] { 44m, 44.34m, 44.09m, 44.15m, 43.61m, 44.33m, 44.83m, 45.10m, 45.42m, 45.84m };
        var period = 14;

        // Act
        var rsi = TechnicalIndicators.CalculateRSI(prices, period);

        // Assert
        rsi.Should().HaveCount(10);
        for (int i = 0; i < rsi.Length; i++)
        {
            if (rsi[i].HasValue)
            {
                rsi[i].Should().BeGreaterThanOrEqualTo(0m);
                rsi[i].Should().BeLessThanOrEqualTo(100m);
            }
        }
    }

    #endregion

    #region Bollinger Bands Tests

    [Fact]
    public void CalculateBollingerBands_WithValidData_ReturnsBands()
    {
        // Arrange
        var prices = new decimal[] { 100m, 102m, 101m, 103m, 102m, 104m, 103m, 105m, 104m, 106m,
                                      105m, 107m, 106m, 108m, 107m, 109m, 108m, 110m, 109m, 111m };

        // Act
        var (upper, middle, lower) = TechnicalIndicators.CalculateBollingerBands(prices, 20, 2);

        // Assert
        upper.Should().HaveCount(20);
        middle.Should().HaveCount(20);
        lower.Should().HaveCount(20);

        for (int i = 0; i < upper.Length; i++)
        {
            if (upper[i].HasValue && middle[i].HasValue)
                upper[i]!.Value.Should().BeGreaterThanOrEqualTo(middle[i]!.Value);
        }

        for (int i = 0; i < lower.Length; i++)
        {
            if (lower[i].HasValue && middle[i].HasValue)
                lower[i]!.Value.Should().BeLessThanOrEqualTo(middle[i]!.Value);
        }
    }

    #endregion

    #region ATR Tests

    [Fact]
    public void CalculateATR_WithValidCandles_ReturnsATRValues()
    {
        // Arrange
        var candles = new Candle[]
        {
            new Candle { Timestamp = DateTime.UtcNow.AddDays(-5), Open = 100m, High = 105m, Low = 98m, Close = 102m, Volume = 1000m },
            new Candle { Timestamp = DateTime.UtcNow.AddDays(-4), Open = 102m, High = 108m, Low = 100m, Close = 106m, Volume = 1000m },
            new Candle { Timestamp = DateTime.UtcNow.AddDays(-3), Open = 106m, High = 110m, Low = 104m, Close = 107m, Volume = 1000m },
            new Candle { Timestamp = DateTime.UtcNow.AddDays(-2), Open = 107m, High = 112m, Low = 105m, Close = 111m, Volume = 1000m },
            new Candle { Timestamp = DateTime.UtcNow.AddDays(-1), Open = 111m, High = 115m, Low = 109m, Close = 113m, Volume = 1000m },
            new Candle { Timestamp = DateTime.UtcNow, Open = 113m, High = 118m, Low = 111m, Close = 116m, Volume = 1000m },
        };

        // Act
        var atr = TechnicalIndicators.CalculateATR(candles, 5);

        // Assert
        atr.Should().HaveCount(6);
        atr.Where(a => a.HasValue).Should().NotBeEmpty();
    }

    #endregion

    #region Candle Model Tests

    [Fact]
    public void Candle_WithValidData_CreatesSuccessfully()
    {
        // Arrange & Act
        var candle = new Candle
        {
            Timestamp = DateTime.UtcNow,
            Open = 100m,
            High = 105m,
            Low = 98m,
            Close = 102m,
            Volume = 1000m
        };

        // Assert
        candle.Should().NotBeNull();
        candle.Timestamp.Should().NotBe(default);
        candle.Open.Should().Be(100m);
        candle.High.Should().Be(105m);
        candle.Low.Should().Be(98m);
        candle.Close.Should().Be(102m);
        candle.Volume.Should().Be(1000m);
    }

    #endregion
}
