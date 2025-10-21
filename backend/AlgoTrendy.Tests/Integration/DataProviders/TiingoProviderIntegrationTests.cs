using AlgoTrendy.DataChannels.Providers;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Sdk;

namespace AlgoTrendy.Tests.Integration.DataProviders;

/// <summary>
/// Integration tests for TiingoProvider
/// These tests require actual Tiingo API credentials
/// Set environment variable: Tiingo__ApiKey
///
/// Get FREE API key at: https://www.tiingo.com/
/// Free tier: 1000 calls/hour, 50,000 calls/month, stocks + crypto + forex
///
/// WARNING: These tests consume API quota! Run sparingly on free tier.
/// </summary>
[Collection("TiingoIntegration")]
public class TiingoProviderIntegrationTests : IAsyncLifetime
{
    private readonly TiingoProvider _provider;
    private readonly ILogger<TiingoProvider> _logger;
    private readonly bool _hasValidKey;

    public TiingoProviderIntegrationTests()
    {
        // Get API key from environment variable only
        var apiKey = Environment.GetEnvironmentVariable("Tiingo__ApiKey");

        _hasValidKey = !string.IsNullOrEmpty(apiKey);

        _logger = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information))
            .CreateLogger<TiingoProvider>();

        var httpClient = new HttpClient();
        // Use apiKey if available, otherwise use a placeholder (tests will skip)
        _provider = new TiingoProvider(httpClient, _logger, apiKey ?? "no-key");
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [SkippableFact]
    public async Task FetchLatestAsync_ForAAPL_ReturnsValidQuote()
    {
        // Arrange
        Skip.If(!_hasValidKey, "Tiingo API key not available");
        var symbol = "AAPL";

        // Act
        var quote = await _provider.FetchLatestAsync(symbol);

        // Assert
        Assert.NotNull(quote);
        Assert.Equal(symbol, quote.Symbol);
        Assert.True(quote.Close > 0, "Close price should be greater than 0");
        Assert.True(quote.Open > 0, "Open price should be greater than 0");
        Assert.True(quote.High > 0, "High price should be greater than 0");
        Assert.True(quote.Low > 0, "Low price should be greater than 0");
        Assert.True(quote.High >= quote.Low, "High should be >= Low");
        Assert.True(quote.Volume > 0, "Volume should be > 0");

        _logger.LogInformation(
            "[Tiingo Test] Quote for {Symbol}: Open={Open}, High={High}, Low={Low}, Close={Close}, Volume={Volume:N0}, Timestamp={Timestamp}",
            quote.Symbol, quote.Open, quote.High, quote.Low, quote.Close, quote.Volume, quote.Timestamp);
    }

    [SkippableFact]
    public async Task FetchHistoricalAsync_ForAAPL_ReturnsValidData()
    {
        // Arrange
        Skip.If(!_hasValidKey, "Tiingo API key not available");
        var symbol = "AAPL";
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-30); // Last 30 days

        // Act
        var candles = await _provider.FetchHistoricalAsync(symbol, startDate, endDate, "1d");

        // Assert
        Assert.NotNull(candles);
        var candleList = candles.ToList();
        Assert.NotEmpty(candleList);
        Assert.True(candleList.Count > 0, "Should return at least some historical data");
        Assert.True(candleList.Count <= 30, "Should not return more than 30 days of data");

        // Verify first candle
        var firstCandle = candleList.First();
        Assert.Equal(symbol, firstCandle.Symbol);
        Assert.True(firstCandle.Close > 0);
        Assert.True(firstCandle.High >= firstCandle.Low);

        _logger.LogInformation(
            "[Tiingo Test] Fetched {Count} candles for {Symbol} from {Start:yyyy-MM-dd} to {End:yyyy-MM-dd}",
            candleList.Count, symbol, startDate, endDate);
    }

    [SkippableFact]
    public async Task FetchHistoricalAsync_ForBTCUSD_Crypto_ReturnsValidData()
    {
        // Arrange
        Skip.If(!_hasValidKey, "Tiingo API key not available");
        var symbol = "btcusd"; // Crypto symbol
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-7); // Last 7 days

        // Act
        var candles = await _provider.FetchHistoricalAsync(symbol, startDate, endDate, "1d");

        // Assert
        Assert.NotNull(candles);
        var candleList = candles.ToList();
        Assert.NotEmpty(candleList);

        // Verify first candle
        var firstCandle = candleList.First();
        Assert.Equal(symbol, firstCandle.Symbol);
        Assert.True(firstCandle.Close > 0, "BTC price should be > 0");
        Assert.True(firstCandle.High >= firstCandle.Low);

        _logger.LogInformation(
            "[Tiingo Test] Fetched {Count} crypto candles for {Symbol}. Latest close: ${Close:N2}",
            candleList.Count, symbol, candleList.Last().Close);
    }

    [SkippableFact]
    public async Task SupportsSymbolAsync_ForAAPL_ReturnsTrue()
    {
        // Arrange
        Skip.If(!_hasValidKey, "Tiingo API key not available");
        var symbol = "AAPL";

        // Act
        var supported = await _provider.SupportsSymbolAsync(symbol);

        // Assert
        Assert.True(supported, "Tiingo should support AAPL");
        _logger.LogInformation("[Tiingo Test] Symbol {Symbol} supported: {Supported}", symbol, supported);
    }

    [SkippableFact]
    public async Task SupportsSymbolAsync_ForInvalidSymbol_ReturnsFalse()
    {
        // Arrange
        Skip.If(!_hasValidKey, "Tiingo API key not available");
        var symbol = "INVALIDXYZ123";

        // Act
        var supported = await _provider.SupportsSymbolAsync(symbol);

        // Assert
        Assert.False(supported, "Tiingo should not support invalid symbol");
        _logger.LogInformation("[Tiingo Test] Symbol {Symbol} supported: {Supported}", symbol, supported);
    }

    [SkippableFact]
    public async Task GetCurrentUsageAsync_ReturnsNonNegative()
    {
        // Arrange
        Skip.If(!_hasValidKey, "Tiingo API key not available");

        // Act - Make a call first to ensure usage is tracked
        await _provider.FetchLatestAsync("AAPL");
        var usage = await _provider.GetCurrentUsageAsync();

        // Assert
        Assert.True(usage >= 0, "Usage should be non-negative");
        _logger.LogInformation("[Tiingo Test] Current API usage: {Usage} calls", usage);
    }

    [SkippableFact]
    public async Task GetRemainingCallsAsync_ReturnsValidValue()
    {
        // Arrange
        Skip.If(!_hasValidKey, "Tiingo API key not available");

        // Act
        var remaining = await _provider.GetRemainingCallsAsync();

        // Assert
        Assert.NotNull(remaining);
        Assert.True(remaining >= 0, "Remaining calls should be non-negative");
        _logger.LogInformation("[Tiingo Test] Remaining API calls: {Remaining}", remaining);
    }

    [SkippableFact]
    public async Task ProviderProperties_HaveCorrectValues()
    {
        // Arrange
        Skip.If(!_hasValidKey, "Tiingo API key not available");

        // Act & Assert
        Assert.Equal("Tiingo", _provider.ProviderName);
        Assert.True(_provider.IsFreeTier, "Tiingo should be marked as free tier");
        Assert.Equal(50000, _provider.DailyRateLimit);

        _logger.LogInformation(
            "[Tiingo Test] Provider: {Name}, FreeTier: {FreeTier}, DailyLimit: {Limit}",
            _provider.ProviderName, _provider.IsFreeTier, _provider.DailyRateLimit);
    }

    [SkippableFact]
    public async Task FetchHistoricalAsync_WithMultipleCalls_RespectsRateLimit()
    {
        // Arrange
        Skip.If(!_hasValidKey, "Tiingo API key not available");
        var symbol = "AAPL";
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-5);

        // Act - Make 3 rapid calls
        var task1 = _provider.FetchHistoricalAsync(symbol, startDate, endDate, "1d");
        var task2 = _provider.FetchHistoricalAsync(symbol, startDate, endDate, "1d");
        var task3 = _provider.FetchHistoricalAsync(symbol, startDate, endDate, "1d");

        var results = await Task.WhenAll(task1, task2, task3);

        // Assert - All should succeed without rate limit errors
        Assert.All(results, result => Assert.NotEmpty(result));

        _logger.LogInformation("[Tiingo Test] Made 3 rapid calls successfully - rate limiting working");
    }

    [SkippableFact]
    public async Task FetchHistoricalAsync_WithIntraday_ReturnsValidData()
    {
        // Arrange
        Skip.If(!_hasValidKey, "Tiingo API key not available");
        var symbol = "AAPL";
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-1); // Last 24 hours

        // Act - Try intraday data (5 minute intervals)
        var candles = await _provider.FetchHistoricalAsync(symbol, startDate, endDate, "5m");

        // Assert
        Assert.NotNull(candles);
        var candleList = candles.ToList();

        // Note: Intraday data availability depends on market hours
        if (candleList.Any())
        {
            var firstCandle = candleList.First();
            Assert.Equal(symbol, firstCandle.Symbol);
            Assert.True(firstCandle.Close > 0);

            _logger.LogInformation(
                "[Tiingo Test] Fetched {Count} intraday (5m) candles for {Symbol}",
                candleList.Count, symbol);
        }
        else
        {
            _logger.LogWarning(
                "[Tiingo Test] No intraday data returned (may be outside market hours)");
        }
    }
}
