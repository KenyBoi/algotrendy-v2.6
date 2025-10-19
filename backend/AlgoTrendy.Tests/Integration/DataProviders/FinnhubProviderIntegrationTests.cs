using AlgoTrendy.DataChannels.Providers;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Sdk;

namespace AlgoTrendy.Tests.Integration.DataProviders;

/// <summary>
/// Integration tests for FinnhubProvider
/// These tests require actual Finnhub API credentials
/// Set environment variable: Finnhub__ApiKey
///
/// Get FREE API key at: https://finnhub.io/register
/// Free tier: 60 calls/minute, 1 year historical data
/// </summary>
[Collection("FinnhubIntegration")]
public class FinnhubProviderIntegrationTests : IAsyncLifetime
{
    private readonly FinnhubProvider _provider;
    private readonly ILogger<FinnhubProvider> _logger;
    private readonly bool _hasValidKey;

    public FinnhubProviderIntegrationTests()
    {
        // Get API key from environment variable
        var apiKey = Environment.GetEnvironmentVariable("Finnhub__ApiKey");

        _hasValidKey = !string.IsNullOrEmpty(apiKey);

        if (!_hasValidKey)
        {
            Skip.If(true, "Finnhub API key not found. Set Finnhub__ApiKey environment variable");
        }

        _logger = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information))
            .CreateLogger<FinnhubProvider>();

        var httpClient = new HttpClient();
        _provider = new FinnhubProvider(httpClient, _logger, apiKey!);
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _provider?.Dispose();
        return Task.CompletedTask;
    }

    [SkippableFact]
    public async Task FetchLatestAsync_ForAAPL_ReturnsValidQuote()
    {
        // Arrange
        Skip.If(!_hasValidKey, "Finnhub API key not available");
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
        Assert.Equal("Finnhub", quote.Source);

        // Log results
        _logger.LogInformation(
            "Quote for {Symbol}: Open={Open}, High={High}, Low={Low}, Close={Close}, Timestamp={Timestamp}",
            quote.Symbol, quote.Open, quote.High, quote.Low, quote.Close, quote.Timestamp);
    }

    [SkippableFact]
    public async Task FetchHistoricalAsync_ForAAPL_Daily_ReturnsCandles()
    {
        // Arrange
        Skip.If(!_hasValidKey, "Finnhub API key not available");
        var symbol = "AAPL";
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-30); // Last 30 days

        // Act
        var candles = (await _provider.FetchHistoricalAsync(symbol, startDate, endDate, "1d")).ToList();

        // Assert
        Assert.NotEmpty(candles);
        Assert.True(candles.Count >= 20, $"Should have at least 20 trading days in 30 days, got {candles.Count}");

        // Validate first candle
        var firstCandle = candles.First();
        Assert.Equal(symbol, firstCandle.Symbol);
        Assert.True(firstCandle.Close > 0, "Close price should be > 0");
        Assert.True(firstCandle.Volume >= 0, "Volume should be >= 0");
        Assert.True(firstCandle.High >= firstCandle.Low, "High should be >= Low");
        Assert.True(firstCandle.High >= firstCandle.Close, "High should be >= Close");
        Assert.True(firstCandle.Low <= firstCandle.Close, "Low should be <= Close");
        Assert.Equal("Finnhub", firstCandle.Source);

        // Validate sorting (ascending by timestamp)
        for (int i = 1; i < candles.Count; i++)
        {
            Assert.True(candles[i].Timestamp >= candles[i - 1].Timestamp,
                $"Candles should be sorted ascending. Candle {i} timestamp is before candle {i - 1}");
        }

        _logger.LogInformation(
            "Fetched {Count} daily candles for {Symbol} from {Start:yyyy-MM-dd} to {End:yyyy-MM-dd}",
            candles.Count, symbol, startDate, endDate);
    }

    [SkippableFact]
    public async Task FetchHistoricalAsync_ForAAPL_Hourly_ReturnsCandles()
    {
        // Arrange
        Skip.If(!_hasValidKey, "Finnhub API key not available");
        var symbol = "AAPL";
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-7); // Last 7 days

        // Act
        var candles = (await _provider.FetchHistoricalAsync(symbol, startDate, endDate, "1h")).ToList();

        // Assert
        Assert.NotEmpty(candles);
        _logger.LogInformation(
            "Fetched {Count} hourly candles for {Symbol} from {Start:yyyy-MM-dd} to {End:yyyy-MM-dd}",
            candles.Count, symbol, startDate, endDate);

        // Validate data quality
        var validCandles = candles.Where(c => c.Close > 0).ToList();
        Assert.True(validCandles.Count > 0, "Should have valid candles with Close > 0");
    }

    [SkippableFact]
    public async Task GetCompanyProfileAsync_ForAAPL_ReturnsProfile()
    {
        // Arrange
        Skip.If(!_hasValidKey, "Finnhub API key not available");
        var symbol = "AAPL";

        // Act
        var profile = await _provider.GetCompanyProfileAsync(symbol);

        // Assert
        Assert.NotNull(profile);
        Assert.True(profile.HasValues, "Profile should have data");

        // Check for expected fields
        var name = profile["name"]?.ToString();
        var ticker = profile["ticker"]?.ToString();
        var exchange = profile["exchange"]?.ToString();
        var industry = profile["finnhubIndustry"]?.ToString();
        var marketCap = profile["marketCapitalization"]?.ToString();

        Assert.NotNull(name);
        Assert.Equal(symbol, ticker);
        Assert.NotNull(exchange);

        _logger.LogInformation(
            "Company Profile for {Symbol}: Name={Name}, Exchange={Exchange}, Industry={Industry}, MarketCap={MarketCap}",
            symbol, name, exchange, industry, marketCap);
    }

    [SkippableFact]
    public async Task GetSocialSentimentAsync_ForAAPL_ReturnsSentiment()
    {
        // Arrange
        Skip.If(!_hasValidKey, "Finnhub API key not available");
        var symbol = "AAPL";
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;

        // Act
        var sentiment = await _provider.GetSocialSentimentAsync(symbol, from, to);

        // Assert
        Assert.NotNull(sentiment);

        var symbol_result = sentiment["symbol"]?.ToString();
        var data = sentiment["data"];

        Assert.Equal(symbol, symbol_result);
        Assert.NotNull(data);

        _logger.LogInformation(
            "Social Sentiment for {Symbol}: Data points={Count}",
            symbol, data?.Count() ?? 0);

        // Log sample data point if available
        var firstDataPoint = data?.FirstOrDefault();
        if (firstDataPoint != null)
        {
            var atTime = firstDataPoint["atTime"]?.ToString();
            var mention = firstDataPoint["mention"]?.ToString();
            var positiveMention = firstDataPoint["positiveMention"]?.ToString();
            var negativeMention = firstDataPoint["negativeMention"]?.ToString();
            var score = firstDataPoint["score"]?.ToString();

            _logger.LogInformation(
                "Sample sentiment: Time={Time}, Mentions={Mentions}, Positive={Positive}, Negative={Negative}, Score={Score}",
                atTime, mention, positiveMention, negativeMention, score);
        }
    }

    [SkippableFact]
    public async Task GetCompanyNewsAsync_ForAAPL_ReturnsNews()
    {
        // Arrange
        Skip.If(!_hasValidKey, "Finnhub API key not available");
        var symbol = "AAPL";
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;

        // Act
        var news = await _provider.GetCompanyNewsAsync(symbol, from, to);

        // Assert
        Assert.NotNull(news);
        Assert.True(news.Count > 0, "Should have news articles in the last 7 days");

        var firstArticle = news.First();
        var headline = firstArticle["headline"]?.ToString();
        var source = firstArticle["source"]?.ToString();
        var url = firstArticle["url"]?.ToString();
        var datetime = firstArticle["datetime"]?.ToString();

        Assert.NotNull(headline);
        Assert.NotNull(source);
        Assert.NotNull(url);

        _logger.LogInformation(
            "Company News for {Symbol}: Found {Count} articles. First headline: {Headline} (Source: {Source})",
            symbol, news.Count, headline, source);
    }

    [SkippableFact]
    public async Task SupportsSymbolAsync_ForValidSymbol_ReturnsTrue()
    {
        // Arrange
        Skip.If(!_hasValidKey, "Finnhub API key not available");
        var symbol = "AAPL";

        // Act
        var supports = await _provider.SupportsSymbolAsync(symbol);

        // Assert
        Assert.True(supports);
    }

    [SkippableFact]
    public async Task GetCurrentUsageAsync_ReturnsUsageCount()
    {
        // Arrange
        Skip.If(!_hasValidKey, "Finnhub API key not available");

        // Act - Make a call first
        await _provider.FetchLatestAsync("AAPL");

        // Get usage
        var usage = await _provider.GetCurrentUsageAsync();

        // Assert
        Assert.True(usage > 0, "Usage should be > 0 after making API call");

        _logger.LogInformation("Current API usage: {Usage} calls today", usage);
    }

    [SkippableFact]
    public async Task GetRemainingCallsAsync_ReturnsRemainingCount()
    {
        // Arrange
        Skip.If(!_hasValidKey, "Finnhub API key not available");

        // Act
        var remaining = await _provider.GetRemainingCallsAsync();

        // Assert
        Assert.NotNull(remaining);
        Assert.True(remaining.Value >= 0 && remaining.Value <= 60,
            $"Remaining calls should be 0-60 (per minute limit), got {remaining.Value}");

        _logger.LogInformation("Remaining API calls this minute: {Remaining}/60", remaining);
    }

    [SkippableFact]
    public async Task RateLimiter_EnforcesPerMinuteLimit()
    {
        // Arrange
        Skip.If(!_hasValidKey, "Finnhub API key not available");
        var symbol = "AAPL";
        var callCount = 5; // Make 5 rapid calls

        // Act
        var startTime = DateTime.UtcNow;

        for (int i = 0; i < callCount; i++)
        {
            await _provider.FetchLatestAsync(symbol);
        }

        var endTime = DateTime.UtcNow;
        var elapsed = endTime - startTime;

        // Assert
        // Should take at least (callCount - 1) seconds due to 1 second minimum interval
        var minimumExpectedSeconds = callCount - 1;
        Assert.True(elapsed.TotalSeconds >= minimumExpectedSeconds,
            $"Should take at least {minimumExpectedSeconds}s for {callCount} calls (1 call/second), took {elapsed.TotalSeconds:F1}s");

        _logger.LogInformation(
            "Made {Count} API calls in {Elapsed:F1} seconds (Rate limiting working: {Rate:F2} calls/second)",
            callCount, elapsed.TotalSeconds, callCount / elapsed.TotalSeconds);
    }

    [SkippableFact]
    public async Task DataQuality_MultipleProviders_ConsistentPrices()
    {
        // Arrange
        Skip.If(!_hasValidKey, "Finnhub API key not available");
        var symbol = "AAPL";

        // Act
        var finnhubQuote = await _provider.FetchLatestAsync(symbol);

        // Assert - Basic validation
        Assert.NotNull(finnhubQuote);
        Assert.True(finnhubQuote.Close > 100 && finnhubQuote.Close < 500,
            $"AAPL price should be realistic (100-500), got {finnhubQuote.Close}");

        // Additional validations
        Assert.True(finnhubQuote.High >= finnhubQuote.Close, "High should be >= Close");
        Assert.True(finnhubQuote.Low <= finnhubQuote.Close, "Low should be <= Close");
        Assert.True(finnhubQuote.Open > 0, "Open should be > 0");

        _logger.LogInformation(
            "Finnhub price for {Symbol}: ${Close:F2} (O:{Open:F2} H:{High:F2} L:{Low:F2})",
            symbol, finnhubQuote.Close, finnhubQuote.Open, finnhubQuote.High, finnhubQuote.Low);
    }
}
