using System.Net;
using System.Text;
using AlgoTrendy.MultiCharts.Configuration;
using AlgoTrendy.MultiCharts.Interfaces;
using AlgoTrendy.MultiCharts.Models;
using AlgoTrendy.MultiCharts.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;

namespace AlgoTrendy.MultiCharts.Tests.Services;

public class MultiChartsClientTests
{
    private readonly Mock<ILogger<MultiChartsClient>> _mockLogger;
    private readonly Mock<IOptions<MultiChartsOptions>> _mockOptions;
    private readonly MultiChartsOptions _options;

    public MultiChartsClientTests()
    {
        _mockLogger = new Mock<ILogger<MultiChartsClient>>();
        _options = new MultiChartsOptions
        {
            Enabled = true,
            ApiEndpoint = "http://localhost:8899",
            TimeoutSeconds = 300,
            EnableRetry = true,
            MaxRetryAttempts = 3,
            RetryDelayMilliseconds = 1000
        };
        _mockOptions = new Mock<IOptions<MultiChartsOptions>>();
        _mockOptions.Setup(x => x.Value).Returns(_options);
    }

    [Fact]
    public async Task TestConnectionAsync_WhenSuccessful_ReturnsTrue()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"status\":\"connected\"}", Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri(_options.ApiEndpoint)
        };

        var client = new MultiChartsClient(httpClient, _mockOptions.Object, _mockLogger.Object);

        // Act
        var result = await client.TestConnectionAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task TestConnectionAsync_WhenFailed_ReturnsFalse()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri(_options.ApiEndpoint)
        };

        var client = new MultiChartsClient(httpClient, _mockOptions.Object, _mockLogger.Object);

        // Act
        var result = await client.TestConnectionAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RunBacktestAsync_ReturnsValidResult()
    {
        // Arrange
        var request = new BacktestRequest
        {
            StrategyName = "SMA_Crossover",
            Symbol = "BTCUSDT",
            FromDate = DateTime.Parse("2024-01-01"),
            ToDate = DateTime.Parse("2025-01-01"),
            Timeframe = "1D",
            InitialCapital = 10000m,
            Parameters = new Dictionary<string, object>
            {
                { "FastPeriod", 10 },
                { "SlowPeriod", 30 }
            }
        };

        var expectedResult = new BacktestResult
        {
            StrategyName = "SMA_Crossover",
            Symbol = "BTCUSDT",
            NetProfit = 2456.78m,
            TotalTrades = 45,
            WinRate = 0.62m,
            SharpeRatio = 1.85m,
            MaxDrawdown = -15.3m,
            ProfitFactor = 1.92m,
            CompletedAt = DateTime.UtcNow
        };

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(expectedResult), Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri(_options.ApiEndpoint)
        };

        var client = new MultiChartsClient(httpClient, _mockOptions.Object, _mockLogger.Object);

        // Act
        var result = await client.RunBacktestAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SMA_Crossover", result.StrategyName);
        Assert.Equal("BTCUSDT", result.Symbol);
        Assert.Equal(2456.78m, result.NetProfit);
        Assert.Equal(45, result.TotalTrades);
        Assert.Equal(0.62m, result.WinRate);
    }

    [Fact]
    public async Task RunWalkForwardOptimizationAsync_ReturnsValidResult()
    {
        // Arrange
        var request = new WalkForwardRequest
        {
            StrategyName = "SMA_Crossover",
            Symbol = "BTCUSDT",
            FromDate = DateTime.Parse("2024-01-01"),
            ToDate = DateTime.Parse("2025-01-01"),
            InSamplePeriodDays = 180,
            OutOfSamplePeriodDays = 60,
            StepDays = 30,
            ParametersToOptimize = new Dictionary<string, ParameterRange>
            {
                { "FastPeriod", new ParameterRange { Start = 5m, Stop = 20m, Step = 1m } },
                { "SlowPeriod", new ParameterRange { Start = 20m, Stop = 50m, Step = 2m } }
            }
        };

        var expectedResult = new WalkForwardResult
        {
            StrategyName = "SMA_Crossover",
            Symbol = "BTCUSDT",
            Windows = new List<WalkForwardWindow>(),
            BestParameters = new Dictionary<string, object>
            {
                { "FastPeriod", 10 },
                { "SlowPeriod", 30 }
            },
            IsRobust = true,
            RobustnessScore = 0.88m
        };

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(expectedResult), Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri(_options.ApiEndpoint)
        };

        var client = new MultiChartsClient(httpClient, _mockOptions.Object, _mockLogger.Object);

        // Act
        var result = await client.RunWalkForwardOptimizationAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SMA_Crossover", result.StrategyName);
        Assert.True(result.IsRobust);
        Assert.Equal(0.88m, result.RobustnessScore);
    }

    [Fact]
    public async Task RunMonteCarloSimulationAsync_ReturnsValidResult()
    {
        // Arrange
        var request = new MonteCarloRequest
        {
            StrategyName = "SMA_Crossover",
            Symbol = "BTCUSDT",
            FromDate = DateTime.Parse("2024-01-01"),
            ToDate = DateTime.Parse("2025-01-01"),
            NumberOfRuns = 1000,
            Parameters = new Dictionary<string, object>
            {
                { "FastPeriod", 10 },
                { "SlowPeriod", 30 }
            }
        };

        var expectedResult = new MonteCarloResult
        {
            MeanReturn = 24.5m,
            MedianReturn = 22.3m,
            StdDeviation = 8.9m,
            ProbabilityOfProfit = 0.68m,
            MeanMaxDrawdown = -18.2m,
            ReturnConfidenceIntervalLower = 15.2m,
            ReturnConfidenceIntervalUpper = 33.8m
        };

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(expectedResult), Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri(_options.ApiEndpoint)
        };

        var client = new MultiChartsClient(httpClient, _mockOptions.Object, _mockLogger.Object);

        // Act
        var result = await client.RunMonteCarloSimulationAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(24.5m, result.MeanReturn);
        Assert.Equal(22.3m, result.MedianReturn);
        Assert.Equal(0.68m, result.ProbabilityOfProfit);
    }

    [Fact]
    public async Task GetPlatformStatusAsync_ReturnsValidStatus()
    {
        // Arrange
        var expectedStatus = new MultiChartsPlatformStatus
        {
            IsConnected = true,
            Version = "14.5.0",
            ServerTime = DateTime.UtcNow,
            ActiveStrategies = 3
        };

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(expectedStatus), Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri(_options.ApiEndpoint)
        };

        var client = new MultiChartsClient(httpClient, _mockOptions.Object, _mockLogger.Object);

        // Act
        var result = await client.GetPlatformStatusAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsConnected);
        Assert.Equal("14.5.0", result.Version);
        Assert.Equal(3, result.ActiveStrategies);
    }

    [Fact]
    public async Task RunMarketScanAsync_ReturnsValidResults()
    {
        // Arrange
        var request = new ScanRequest
        {
            ScanName = "RSI_Oversold",
            Symbols = new List<string> { "BTCUSDT", "ETHUSDT", "BNBUSDT" },
            ScanFormula = "RSI(14) < 30",
            Timeframe = "1D"
        };

        var expectedResult = new ScanResult
        {
            ScanName = "RSI_Oversold",
            TotalSymbolsScanned = 3,
            MatchingSymbols = 2,
            Matches = new List<ScanMatch>
            {
                new ScanMatch { Symbol = "BTCUSDT", CurrentPrice = 65000m },
                new ScanMatch { Symbol = "ETHUSDT", CurrentPrice = 3200m }
            }
        };

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(expectedResult), Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri(_options.ApiEndpoint)
        };

        var client = new MultiChartsClient(httpClient, _mockOptions.Object, _mockLogger.Object);

        // Act
        var result = await client.RunMarketScanAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("RSI_Oversold", result.ScanName);
        Assert.Equal(3, result.TotalSymbolsScanned);
        Assert.Equal(2, result.MatchingSymbols);
        Assert.Equal(2, result.Matches.Count);
    }
}
