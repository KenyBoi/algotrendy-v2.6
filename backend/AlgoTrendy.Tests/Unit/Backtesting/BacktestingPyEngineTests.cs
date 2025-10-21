using AlgoTrendy.Backtesting.Engines;
using AlgoTrendy.Backtesting.Models;
using AlgoTrendy.Backtesting.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AlgoTrendy.Tests.Unit.Backtesting;

/// <summary>
/// Unit tests for BacktestingPyEngine
/// </summary>
public class BacktestingPyEngineTests
{
    private readonly Mock<IBacktestingPyApiClient> _mockApiClient;
    private readonly Mock<ILogger<BacktestingPyEngine>> _mockLogger;
    private readonly BacktestingPyEngine _engine;

    public BacktestingPyEngineTests()
    {
        _mockApiClient = new Mock<IBacktestingPyApiClient>();
        _mockLogger = new Mock<ILogger<BacktestingPyEngine>>();
        _engine = new BacktestingPyEngine(_mockApiClient.Object, _mockLogger.Object);
    }

    [Fact]
    public void EngineName_ShouldReturnCorrectName()
    {
        // Act
        var name = _engine.EngineName;

        // Assert
        name.Should().Be("Backtesting.py");
    }

    [Fact]
    public void EngineDescription_ShouldReturnNonEmptyDescription()
    {
        // Act
        var description = _engine.EngineDescription;

        // Assert
        description.Should().NotBeNullOrWhiteSpace();
        description.Should().Contain("Python");
        description.Should().Contain("Backtesting.py");
    }

    [Fact]
    public void ValidateConfig_WithValidConfig_ShouldReturnTrue()
    {
        // Arrange
        var config = CreateValidConfig();

        // Act
        var (isValid, errorMessage) = _engine.ValidateConfig(config);

        // Assert
        isValid.Should().BeTrue();
        errorMessage.Should().BeNull();
    }

    [Fact]
    public void ValidateConfig_WithNullConfig_ShouldReturnFalse()
    {
        // Act
        var (isValid, errorMessage) = _engine.ValidateConfig(null!);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Be("Config cannot be null");
    }

    [Fact]
    public void ValidateConfig_WithEmptySymbol_ShouldReturnFalse()
    {
        // Arrange
        var config = CreateValidConfig();
        config.Symbol = "";

        // Act
        var (isValid, errorMessage) = _engine.ValidateConfig(config);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Be("Symbol is required");
    }

    [Fact]
    public void ValidateConfig_WithStartDateAfterEndDate_ShouldReturnFalse()
    {
        // Arrange
        var config = CreateValidConfig();
        config.StartDate = DateTime.UtcNow;
        config.EndDate = DateTime.UtcNow.AddDays(-30);

        // Act
        var (isValid, errorMessage) = _engine.ValidateConfig(config);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Be("Start date must be before end date");
    }

    [Fact]
    public void ValidateConfig_WithZeroInitialCapital_ShouldReturnFalse()
    {
        // Arrange
        var config = CreateValidConfig();
        config.InitialCapital = 0;

        // Act
        var (isValid, errorMessage) = _engine.ValidateConfig(config);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Be("Initial capital must be greater than zero");
    }

    [Fact]
    public void ValidateConfig_WithNegativeInitialCapital_ShouldReturnFalse()
    {
        // Arrange
        var config = CreateValidConfig();
        config.InitialCapital = -1000;

        // Act
        var (isValid, errorMessage) = _engine.ValidateConfig(config);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Be("Initial capital must be greater than zero");
    }

    [Fact]
    public void ValidateConfig_WithDateRangeLessThan30Days_ShouldReturnFalse()
    {
        // Arrange
        var config = CreateValidConfig();
        config.StartDate = DateTime.UtcNow;
        config.EndDate = DateTime.UtcNow.AddDays(29);

        // Act
        var (isValid, errorMessage) = _engine.ValidateConfig(config);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Be("Date range must be at least 30 days for meaningful backtesting");
    }

    [Fact]
    public void ValidateConfig_WithNegativeCommission_ShouldReturnFalse()
    {
        // Arrange
        var config = CreateValidConfig();
        config.Commission = -0.01m;

        // Act
        var (isValid, errorMessage) = _engine.ValidateConfig(config);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Be("Commission must be between 0 and 10%");
    }

    [Fact]
    public void ValidateConfig_WithCommissionGreaterThan10Percent_ShouldReturnFalse()
    {
        // Arrange
        var config = CreateValidConfig();
        config.Commission = 0.11m;

        // Act
        var (isValid, errorMessage) = _engine.ValidateConfig(config);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Be("Commission must be between 0 and 10%");
    }

    [Fact]
    public async Task RunAsync_WithInvalidConfig_ShouldReturnFailedResult()
    {
        // Arrange
        var config = CreateValidConfig();
        config.Symbol = "";

        // Act
        var result = await _engine.RunAsync(config);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(BacktestStatus.Failed);
        result.ErrorMessage.Should().Contain("Symbol is required");
    }

    [Fact]
    public async Task RunAsync_WhenServiceUnhealthy_ShouldReturnFailedResult()
    {
        // Arrange
        var config = CreateValidConfig();
        _mockApiClient.Setup(x => x.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _engine.RunAsync(config);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(BacktestStatus.Failed);
        result.ErrorMessage.Should().Contain("service is unavailable");
    }

    [Fact]
    public async Task RunAsync_WhenApiReturnsNull_ShouldReturnFailedResult()
    {
        // Arrange
        var config = CreateValidConfig();
        _mockApiClient.Setup(x => x.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockApiClient.Setup(x => x.RunBacktestAsync(It.IsAny<BacktestConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BacktestResults?)null);

        // Act
        var result = await _engine.RunAsync(config);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(BacktestStatus.Failed);
        result.ErrorMessage.Should().Contain("null results");
    }

    [Fact]
    public async Task RunAsync_WithValidConfig_ShouldReturnSuccessResult()
    {
        // Arrange
        var config = CreateValidConfig();
        var expectedResults = CreateSuccessResults(config);

        _mockApiClient.Setup(x => x.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockApiClient.Setup(x => x.RunBacktestAsync(It.IsAny<BacktestConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResults);

        // Act
        var result = await _engine.RunAsync(config);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(BacktestStatus.Completed);
        result.BacktestId.Should().Be(expectedResults.BacktestId);
        result.Metrics.Should().NotBeNull();
        result.ExecutionTimeSeconds.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task RunAsync_WhenApiThrowsException_ShouldReturnFailedResult()
    {
        // Arrange
        var config = CreateValidConfig();
        _mockApiClient.Setup(x => x.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockApiClient.Setup(x => x.RunBacktestAsync(It.IsAny<BacktestConfig>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        // Act
        var result = await _engine.RunAsync(config);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(BacktestStatus.Failed);
        result.ErrorMessage.Should().Contain("Unexpected error");
    }

    [Fact]
    public async Task IsAvailableAsync_WhenHealthy_ShouldReturnTrue()
    {
        // Arrange
        _mockApiClient.Setup(x => x.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _engine.IsAvailableAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAvailableAsync_WhenUnhealthy_ShouldReturnFalse()
    {
        // Arrange
        _mockApiClient.Setup(x => x.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _engine.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAvailableAsync_WhenExceptionThrown_ShouldReturnFalse()
    {
        // Arrange
        _mockApiClient.Setup(x => x.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        // Act
        var result = await _engine.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetAvailableStrategiesAsync_WhenSuccessful_ShouldReturnStrategies()
    {
        // Arrange
        var strategies = new List<StrategyInfo>
        {
            new() { Name = "SMA", Description = "Simple Moving Average" },
            new() { Name = "RSI", Description = "Relative Strength Index" }
        };
        _mockApiClient.Setup(x => x.GetStrategiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(strategies);

        // Act
        var result = await _engine.GetAvailableStrategiesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(s => s.Name == "SMA");
        result.Should().Contain(s => s.Name == "RSI");
    }

    [Fact]
    public async Task GetAvailableStrategiesAsync_WhenExceptionThrown_ShouldReturnNull()
    {
        // Arrange
        _mockApiClient.Setup(x => x.GetStrategiesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        // Act
        var result = await _engine.GetAvailableStrategiesAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RunAsync_WithCancellation_ShouldPropagateCancellation()
    {
        // Arrange
        var config = CreateValidConfig();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockApiClient.Setup(x => x.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException());

        // Act
        var result = await _engine.RunAsync(config, cts.Token);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(BacktestStatus.Failed);
    }

    [Fact]
    public async Task RunAsync_ShouldLogInformationMessages()
    {
        // Arrange
        var config = CreateValidConfig();
        var expectedResults = CreateSuccessResults(config);

        _mockApiClient.Setup(x => x.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockApiClient.Setup(x => x.RunBacktestAsync(It.IsAny<BacktestConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResults);

        // Act
        await _engine.RunAsync(config);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting Backtesting.py engine")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    // Helper methods

    private static BacktestConfig CreateValidConfig()
    {
        return new BacktestConfig
        {
            Symbol = "BTCUSDT",
            StartDate = DateTime.UtcNow.AddYears(-1),
            EndDate = DateTime.UtcNow,
            Timeframe = TimeframeType.Day,
            InitialCapital = 10000,
            Commission = 0.001m,
            AssetClass = AssetClass.Crypto,
            Indicators = new Dictionary<string, IndicatorConfig>
            {
                ["SMA"] = new()
                {
                    Name = "SMA",
                    Enabled = true,
                    Parameters = new Dictionary<string, object>
                    {
                        ["fast_period"] = 10,
                        ["slow_period"] = 30
                    }
                }
            }
        };
    }

    private static BacktestResults CreateSuccessResults(BacktestConfig config)
    {
        return new BacktestResults
        {
            BacktestId = Guid.NewGuid().ToString(),
            Status = BacktestStatus.Completed,
            Config = config,
            StartedAt = DateTime.UtcNow.AddMinutes(-5),
            CompletedAt = DateTime.UtcNow,
            ExecutionTimeSeconds = 300,
            Metrics = new BacktestMetrics
            {
                TotalReturn = 25.5m,
                AnnualReturn = 30.2m,
                SharpeRatio = 1.8m,
                SortinoRatio = 2.5m,
                MaxDrawdown = -15.3m,
                WinRate = 0.65m,
                ProfitFactor = 2.1m,
                TotalTrades = 150,
                WinningTrades = 98,
                LosingTrades = 52,
                AverageWin = 1.5m,
                AverageLoss = -0.8m,
                LargestWin = 5.2m,
                LargestLoss = -3.1m
            },
            EquityCurve = new List<EquityPoint>(),
            Trades = new List<TradeResult>()
        };
    }
}
