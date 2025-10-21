using AlgoTrendy.MultiCharts.Configuration;

namespace AlgoTrendy.MultiCharts.Tests.Configuration;

public class MultiChartsOptionsTests
{
    [Fact]
    public void MultiChartsOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new MultiChartsOptions();

        // Assert
        Assert.True(options.Enabled);
        Assert.NotNull(options.ApiEndpoint);
        Assert.True(options.TimeoutSeconds > 0);
        Assert.True(options.MaxBacktestDurationMinutes > 0);
        Assert.True(options.EnableRetry);
        Assert.True(options.MaxRetryAttempts > 0);
        Assert.True(options.RetryDelayMilliseconds > 0);
        Assert.True(options.EnableLogging);
    }

    [Fact]
    public void MultiChartsOptions_CanSetProperties()
    {
        // Arrange
        var options = new MultiChartsOptions();

        // Act
        options.Enabled = false;
        options.ApiEndpoint = "http://test:9999";
        options.TimeoutSeconds = 600;
        options.MaxBacktestDurationMinutes = 120;
        options.EnableRetry = false;
        options.MaxRetryAttempts = 5;
        options.RetryDelayMilliseconds = 2000;
        options.EnableLogging = false;
        options.ApiKey = "test-key";
        options.ApiSecret = "test-secret";

        // Assert
        Assert.False(options.Enabled);
        Assert.Equal("http://test:9999", options.ApiEndpoint);
        Assert.Equal(600, options.TimeoutSeconds);
        Assert.Equal(120, options.MaxBacktestDurationMinutes);
        Assert.False(options.EnableRetry);
        Assert.Equal(5, options.MaxRetryAttempts);
        Assert.Equal(2000, options.RetryDelayMilliseconds);
        Assert.False(options.EnableLogging);
        Assert.Equal("test-key", options.ApiKey);
        Assert.Equal("test-secret", options.ApiSecret);
    }

    [Fact]
    public void MultiChartsOptions_SectionName_IsCorrect()
    {
        // Assert
        Assert.Equal("MultiCharts", MultiChartsOptions.SectionName);
    }

    [Fact]
    public void MultiChartsOptions_NullableProperties_CanBeNull()
    {
        // Arrange
        var options = new MultiChartsOptions();

        // Assert - Only ApiKey and ApiSecret are truly nullable
        Assert.Null(options.ApiKey);
        Assert.Null(options.ApiSecret);

        // DataPath and StrategyPath have default values
        Assert.NotNull(options.DataPath);
        Assert.NotNull(options.StrategyPath);
    }

    [Fact]
    public void MultiChartsOptions_OptionalPaths_CanBeSet()
    {
        // Arrange
        var options = new MultiChartsOptions();

        // Act
        options.DataPath = "C:\\MultiCharts\\Data";
        options.StrategyPath = "C:\\MultiCharts\\Strategies";

        // Assert
        Assert.Equal("C:\\MultiCharts\\Data", options.DataPath);
        Assert.Equal("C:\\MultiCharts\\Strategies", options.StrategyPath);
    }
}
