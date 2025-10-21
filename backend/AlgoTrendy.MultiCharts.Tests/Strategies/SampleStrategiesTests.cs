using AlgoTrendy.MultiCharts.Strategies;
using AlgoTrendy.MultiCharts.Utilities;

namespace AlgoTrendy.MultiCharts.Tests.Strategies;

public class SampleStrategiesTests
{
    [Fact]
    public void SMACrossover_IsValidStrategy()
    {
        // Arrange
        var strategyCode = SampleStrategies.SMACrossover;

        // Act
        var (isValid, errors) = StrategyConverter.ValidateStrategy(strategyCode);

        // Assert
        Assert.True(isValid, $"Validation errors: {string.Join(", ", errors)}");
        Assert.Empty(errors);
    }

    [Fact]
    public void RSIMeanReversion_IsValidStrategy()
    {
        // Arrange
        var strategyCode = SampleStrategies.RSIMeanReversion;

        // Act
        var (isValid, errors) = StrategyConverter.ValidateStrategy(strategyCode);

        // Assert
        Assert.True(isValid, $"Validation errors: {string.Join(", ", errors)}");
        Assert.Empty(errors);
    }

    [Fact]
    public void BollingerBreakout_IsValidStrategy()
    {
        // Arrange
        var strategyCode = SampleStrategies.BollingerBreakout;

        // Act
        var (isValid, errors) = StrategyConverter.ValidateStrategy(strategyCode);

        // Assert
        Assert.True(isValid, $"Validation errors: {string.Join(", ", errors)}");
        Assert.Empty(errors);
    }

    [Fact]
    public void GetStrategy_WithValidName_ReturnsStrategyCode()
    {
        // Act
        var strategy = SampleStrategies.GetStrategy("SMA_Crossover");

        // Assert
        Assert.NotNull(strategy);
        Assert.Contains("SMA_Crossover", strategy);
    }

    [Fact]
    public void GetStrategy_WithInvalidName_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            SampleStrategies.GetStrategy("InvalidStrategy"));
    }

    [Fact]
    public void GetAvailableStrategies_ReturnsAllStrategies()
    {
        // Act
        var strategies = SampleStrategies.GetAvailableStrategies();

        // Assert
        Assert.NotNull(strategies);
        Assert.Equal(3, strategies.Count);
        Assert.Contains("SMA_Crossover", strategies);
        Assert.Contains("RSI_MeanReversion", strategies);
        Assert.Contains("Bollinger_Breakout", strategies);
    }

    [Theory]
    [InlineData("SMA_Crossover")]
    [InlineData("sma crossover")]
    [InlineData("RSI_MeanReversion")]
    [InlineData("rsi mean reversion")]
    [InlineData("Bollinger_Breakout")]
    [InlineData("bollinger breakout")]
    public void GetStrategy_AcceptsDifferentNameFormats(string strategyName)
    {
        // Act
        var strategy = SampleStrategies.GetStrategy(strategyName);

        // Assert
        Assert.NotNull(strategy);
        Assert.NotEmpty(strategy);
    }

    [Fact]
    public void SMACrossover_ContainsRequiredInputs()
    {
        // Arrange
        var strategyCode = SampleStrategies.SMACrossover;

        // Assert
        Assert.Contains("[Input]", strategyCode);
        Assert.Contains("FastPeriod", strategyCode);
        Assert.Contains("SlowPeriod", strategyCode);
    }

    [Fact]
    public void RSIMeanReversion_ContainsRequiredInputs()
    {
        // Arrange
        var strategyCode = SampleStrategies.RSIMeanReversion;

        // Assert
        Assert.Contains("[Input]", strategyCode);
        Assert.Contains("RSIPeriod", strategyCode);
        Assert.Contains("OversoldLevel", strategyCode);
        Assert.Contains("OverboughtLevel", strategyCode);
    }

    [Fact]
    public void BollingerBreakout_ContainsRequiredInputs()
    {
        // Arrange
        var strategyCode = SampleStrategies.BollingerBreakout;

        // Assert
        Assert.Contains("[Input]", strategyCode);
        Assert.Contains("Period", strategyCode);
        Assert.Contains("NumStdDev", strategyCode);
    }
}
