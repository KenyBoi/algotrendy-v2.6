using AlgoTrendy.MultiCharts.Utilities;

namespace AlgoTrendy.MultiCharts.Tests.Utilities;

public class StrategyConverterTests
{
    [Fact]
    public void ConvertToMultiChartsFormat_ReturnsValidPowerLanguageCode()
    {
        // Arrange
        var algoTrendyStrategy = "Simple test strategy";

        // Act
        var result = StrategyConverter.ConvertToMultiChartsFormat(algoTrendyStrategy);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("namespace PowerLanguage.Strategy", result);
        Assert.Contains(": SignalObject", result);
        Assert.Contains(algoTrendyStrategy, result);
    }

    [Fact]
    public void ExtractParameters_WithValidCode_ReturnsParameters()
    {
        // Arrange
        var strategyCode = @"
using PowerLanguage.Strategy;

public class TestStrategy : SignalObject
{
    [Input]
    public int Period { get; set; }

    [Input]
    public double Threshold { get; set; }
}";

        // Act
        var parameters = StrategyConverter.ExtractParameters(strategyCode);

        // Assert
        Assert.NotNull(parameters);
        Assert.NotEmpty(parameters);
    }

    [Fact]
    public void ValidateStrategy_WithValidCode_ReturnsTrue()
    {
        // Arrange
        var validCode = @"
using PowerLanguage.Strategy;

namespace PowerLanguage.Strategy
{
    public class TestStrategy : SignalObject
    {
        protected override void CalcBar()
        {
            // Strategy logic
        }
    }
}";

        // Act
        var (isValid, errors) = StrategyConverter.ValidateStrategy(validCode);

        // Assert
        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateStrategy_WithEmptyCode_ReturnsFalse()
    {
        // Arrange
        var emptyCode = "";

        // Act
        var (isValid, errors) = StrategyConverter.ValidateStrategy(emptyCode);

        // Assert
        Assert.False(isValid);
        Assert.NotEmpty(errors);
        Assert.Contains("Strategy code cannot be empty", errors);
    }

    [Fact]
    public void ValidateStrategy_WithoutNamespace_ReturnsFalse()
    {
        // Arrange
        var invalidCode = @"
public class TestStrategy : SignalObject
{
    protected override void CalcBar()
    {
        // Strategy logic
    }
}";

        // Act
        var (isValid, errors) = StrategyConverter.ValidateStrategy(invalidCode);

        // Assert
        Assert.False(isValid);
        Assert.Contains("Strategy must be in PowerLanguage.Strategy namespace", errors);
    }

    [Fact]
    public void ValidateStrategy_WithoutSignalObject_ReturnsFalse()
    {
        // Arrange
        var invalidCode = @"
namespace PowerLanguage.Strategy
{
    public class TestStrategy
    {
        protected override void CalcBar()
        {
            // Strategy logic
        }
    }
}";

        // Act
        var (isValid, errors) = StrategyConverter.ValidateStrategy(invalidCode);

        // Assert
        Assert.False(isValid);
        Assert.Contains("Strategy must inherit from SignalObject", errors);
    }

    [Fact]
    public void ValidateStrategy_WithoutCalcBar_ReturnsFalse()
    {
        // Arrange
        var invalidCode = @"
namespace PowerLanguage.Strategy
{
    public class TestStrategy : SignalObject
    {
        // Missing CalcBar method
    }
}";

        // Act
        var (isValid, errors) = StrategyConverter.ValidateStrategy(invalidCode);

        // Assert
        Assert.False(isValid);
        Assert.Contains("Strategy must implement CalcBar() method", errors);
    }

    [Fact]
    public void ValidateStrategy_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var invalidCode = @"
public class TestStrategy
{
    // Invalid code with multiple issues
}";

        // Act
        var (isValid, errors) = StrategyConverter.ValidateStrategy(invalidCode);

        // Assert
        Assert.False(isValid);
        Assert.True(errors.Count >= 3); // Should have at least 3 errors
    }
}
