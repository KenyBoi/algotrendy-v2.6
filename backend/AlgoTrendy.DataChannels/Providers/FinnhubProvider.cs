using System.Collections.Concurrent;
using System.Globalization;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace AlgoTrendy.DataChannels.Providers;

/// <summary>
/// Finnhub data provider - FREE tier (60 calls/min, 1 year historical data)
/// Includes real-time quotes, fundamentals, news, social sentiment, and alternative data
/// </summary>
public class FinnhubProvider : IMarketDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FinnhubProvider> _logger;
    private readonly string _apiKey;
    private const string BaseUrl = "https://finnhub.io/api/v1";

    // Rate limiting: 60 calls/minute = 1 call per second
    // Use token bucket algorithm with 60 tokens, refill 1/second
    private readonly SemaphoreSlim _rateLimiter = new(60, 60);
    private readonly System.Threading.Timer _refillTimer;
    private DateTime _lastCall = DateTime.MinValue;
    private readonly TimeSpan _minInterval = TimeSpan.FromSeconds(1); // 1 second between calls

    // Track API usage
    private readonly ConcurrentDictionary<DateTime, int> _usageTracker = new();
    private int _todayUsage = 0;
    private int _currentMinuteUsage = 0;
    private DateTime _currentMinuteStart = DateTime.UtcNow;

    public string ProviderName => "Finnhub";
    public bool IsFreeTier => true;
    public int? DailyRateLimit => null; // Unlimited daily, but 60/min limit

    public FinnhubProvider(
        HttpClient httpClient,
        ILogger<FinnhubProvider> logger,
        string apiKey)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _apiKey = !string.IsNullOrWhiteSpace(apiKey)
            ? apiKey
            : throw new ArgumentException("Finnhub API key is required", nameof(apiKey));

        // Refill token bucket every second
        _refillTimer = new System.Threading.Timer(
            callback: _ => RefillTokenBucket(),
            state: null,
            dueTime: TimeSpan.FromSeconds(1),
            period: TimeSpan.FromSeconds(1));
    }

    public async Task<IEnumerable<MarketData>> FetchHistoricalAsync(
        string symbol,
        DateTime startDate,
        DateTime endDate,
        string interval = "1d",
        CancellationToken cancellationToken = default)
    {
        await EnforceRateLimitAsync(cancellationToken);

        try
        {
            var resolution = ConvertToFinnhubResolution(interval);
            var from = new DateTimeOffset(startDate).ToUnixTimeSeconds();
            var to = new DateTimeOffset(endDate).ToUnixTimeSeconds();

            var url = $"{BaseUrl}/stock/candle?symbol={symbol}&resolution={resolution}&from={from}&to={to}&token={_apiKey}";

            _logger.LogInformation(
                "[Finnhub] Fetching {Symbol} from {Start:yyyy-MM-dd} to {End:yyyy-MM-dd} (resolution: {Resolution})",
                symbol, startDate, endDate, resolution);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            // Check for error response
            if (json.Contains("\"s\":\"no_data\""))
            {
                _logger.LogWarning("[Finnhub] No data available for {Symbol}", symbol);
                return Array.Empty<MarketData>();
            }

            var data = JObject.Parse(json);

            // Check status
            var status = data["s"]?.ToString();
            if (status != "ok")
            {
                _logger.LogWarning("[Finnhub] API returned status: {Status} for {Symbol}", status, symbol);
                return Array.Empty<MarketData>();
            }

            // Parse candle data
            var timestamps = data["t"]?.ToObject<long[]>();
            var opens = data["o"]?.ToObject<decimal[]>();
            var highs = data["h"]?.ToObject<decimal[]>();
            var lows = data["l"]?.ToObject<decimal[]>();
            var closes = data["c"]?.ToObject<decimal[]>();
            var volumes = data["v"]?.ToObject<decimal[]>();

            if (timestamps == null || opens == null || highs == null ||
                lows == null || closes == null || volumes == null)
            {
                _logger.LogWarning("[Finnhub] Missing candle data arrays for {Symbol}", symbol);
                return Array.Empty<MarketData>();
            }

            var result = new List<MarketData>();
            for (int i = 0; i < timestamps.Length; i++)
            {
                var timestamp = DateTimeOffset.FromUnixTimeSeconds(timestamps[i]).UtcDateTime;

                result.Add(new MarketData
                {
                    Symbol = symbol,
                    Timestamp = timestamp,
                    Open = opens[i],
                    High = highs[i],
                    Low = lows[i],
                    Close = closes[i],
                    Volume = volumes[i],
                    Source = ProviderName
                });
            }

            _logger.LogInformation("[Finnhub] Fetched {Count} candles for {Symbol}", result.Count, symbol);

            IncrementUsage();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Finnhub] Error fetching historical data for {Symbol}", symbol);
            throw;
        }
    }

    public async Task<MarketData?> FetchLatestAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        await EnforceRateLimitAsync(cancellationToken);

        try
        {
            var url = $"{BaseUrl}/quote?symbol={symbol}&token={_apiKey}";

            _logger.LogInformation("[Finnhub] Fetching latest quote for {Symbol}", symbol);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JObject.Parse(json);

            // Finnhub quote response:
            // { "c": 178.72, "d": 0.41, "dp": 0.2299, "h": 179.63, "l": 177.25, "o": 177.84, "pc": 178.31, "t": 1698091200 }
            // c = current price, d = change, dp = percent change, h = high, l = low, o = open, pc = previous close, t = timestamp

            var currentPrice = ParseDecimal(data["c"]);
            if (currentPrice == 0m)
            {
                _logger.LogWarning("[Finnhub] No quote data found for {Symbol}", symbol);
                return null;
            }

            var timestamp = data["t"]?.ToObject<long>() ?? 0;
            var timestampUtc = timestamp > 0
                ? DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime
                : DateTime.UtcNow;

            IncrementUsage();

            return new MarketData
            {
                Symbol = symbol,
                Timestamp = timestampUtc,
                Open = ParseDecimal(data["o"]),
                High = ParseDecimal(data["h"]),
                Low = ParseDecimal(data["l"]),
                Close = currentPrice,
                Volume = 0m, // Quote endpoint doesn't include volume
                Source = ProviderName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Finnhub] Error fetching latest quote for {Symbol}", symbol);
            throw;
        }
    }

    public Task<bool> SupportsSymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        // Finnhub supports 60,000+ US stocks, international stocks, forex, crypto
        // For now, return true (validation happens during fetch)
        return Task.FromResult(true);
    }

    public Task<int> GetCurrentUsageAsync()
    {
        return Task.FromResult(_todayUsage);
    }

    public Task<int?> GetRemainingCallsAsync()
    {
        // Finnhub has per-minute limits, not daily limits
        // Return remaining calls this minute
        var remaining = 60 - _currentMinuteUsage;
        return Task.FromResult<int?>(Math.Max(0, remaining));
    }

    /// <summary>
    /// Fetches company profile/fundamentals
    /// Additional endpoint not in IMarketDataProvider interface
    /// </summary>
    public async Task<JObject?> GetCompanyProfileAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        await EnforceRateLimitAsync(cancellationToken);

        try
        {
            var url = $"{BaseUrl}/stock/profile2?symbol={symbol}&token={_apiKey}";

            _logger.LogInformation("[Finnhub] Fetching company profile for {Symbol}", symbol);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JObject.Parse(json);

            IncrementUsage();

            return data.HasValues ? data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Finnhub] Error fetching company profile for {Symbol}", symbol);
            return null;
        }
    }

    /// <summary>
    /// Fetches social sentiment data (Reddit, Twitter, StockTwits)
    /// Additional endpoint not in IMarketDataProvider interface
    /// </summary>
    public async Task<JObject?> GetSocialSentimentAsync(
        string symbol,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        await EnforceRateLimitAsync(cancellationToken);

        try
        {
            var fromDate = (from ?? DateTime.UtcNow.AddDays(-7)).ToString("yyyy-MM-dd");
            var toDate = (to ?? DateTime.UtcNow).ToString("yyyy-MM-dd");

            var url = $"{BaseUrl}/stock/social-sentiment?symbol={symbol}&from={fromDate}&to={toDate}&token={_apiKey}";

            _logger.LogInformation("[Finnhub] Fetching social sentiment for {Symbol}", symbol);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JObject.Parse(json);

            IncrementUsage();

            return data.HasValues ? data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Finnhub] Error fetching social sentiment for {Symbol}", symbol);
            return null;
        }
    }

    /// <summary>
    /// Fetches company news
    /// Additional endpoint not in IMarketDataProvider interface
    /// </summary>
    public async Task<JArray?> GetCompanyNewsAsync(
        string symbol,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        await EnforceRateLimitAsync(cancellationToken);

        try
        {
            var fromDate = (from ?? DateTime.UtcNow.AddDays(-7)).ToString("yyyy-MM-dd");
            var toDate = (to ?? DateTime.UtcNow).ToString("yyyy-MM-dd");

            var url = $"{BaseUrl}/company-news?symbol={symbol}&from={fromDate}&to={toDate}&token={_apiKey}";

            _logger.LogInformation("[Finnhub] Fetching company news for {Symbol}", symbol);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JArray.Parse(json);

            IncrementUsage();

            return data.Count > 0 ? data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Finnhub] Error fetching company news for {Symbol}", symbol);
            return null;
        }
    }

    private async Task EnforceRateLimitAsync(CancellationToken cancellationToken)
    {
        // Wait for available token from bucket
        await _rateLimiter.WaitAsync(cancellationToken);

        try
        {
            // Reset minute counter if we're in a new minute
            var now = DateTime.UtcNow;
            if ((now - _currentMinuteStart).TotalMinutes >= 1)
            {
                _currentMinuteStart = now;
                _currentMinuteUsage = 0;
            }

            // Check per-minute limit
            if (_currentMinuteUsage >= 60)
            {
                var waitTime = TimeSpan.FromMinutes(1) - (now - _currentMinuteStart);
                _logger.LogWarning("[Finnhub] Per-minute rate limit reached (60/min). Waiting {Seconds:F1}s",
                    waitTime.TotalSeconds);
                await Task.Delay(waitTime, cancellationToken);
                _currentMinuteStart = DateTime.UtcNow;
                _currentMinuteUsage = 0;
            }

            // Enforce minimum interval between calls (1 second)
            var timeSinceLastCall = DateTime.UtcNow - _lastCall;
            if (timeSinceLastCall < _minInterval)
            {
                var delay = _minInterval - timeSinceLastCall;
                await Task.Delay(delay, cancellationToken);
            }

            _lastCall = DateTime.UtcNow;
            _currentMinuteUsage++;
        }
        finally
        {
            // Token will be refilled by timer, don't release immediately
            // This prevents burst over 60 calls/minute
        }
    }

    private void RefillTokenBucket()
    {
        // Refill 1 token per second (up to max 60)
        if (_rateLimiter.CurrentCount < 60)
        {
            _rateLimiter.Release();
        }
    }

    private void IncrementUsage()
    {
        var today = DateTime.UtcNow.Date;

        // Reset counter if it's a new day
        if (_usageTracker.Keys.Any() && _usageTracker.Keys.Max() < today)
        {
            _usageTracker.Clear();
            _todayUsage = 0;
        }

        _usageTracker.AddOrUpdate(today, 1, (key, value) => value + 1);
        _todayUsage = _usageTracker.GetValueOrDefault(today, 0);

        _logger.LogDebug("[Finnhub] API usage: {MinuteUsage}/60 this minute, {TotalUsage} today",
            _currentMinuteUsage, _todayUsage);
    }

    private static string ConvertToFinnhubResolution(string interval)
    {
        return interval.ToLower() switch
        {
            "1m" => "1",     // 1 minute
            "5m" => "5",     // 5 minutes
            "15m" => "15",   // 15 minutes
            "30m" => "30",   // 30 minutes
            "1h" or "60m" => "60",  // 1 hour
            "1d" or "daily" => "D",   // Daily
            "1wk" or "weekly" => "W", // Weekly
            "1mo" or "monthly" => "M", // Monthly
            _ => "D"  // Default to daily
        };
    }

    private static decimal ParseDecimal(JToken? token)
    {
        if (token == null) return 0m;

        var value = token.ToString();
        if (string.IsNullOrWhiteSpace(value)) return 0m;

        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result
            : 0m;
    }

    public void Dispose()
    {
        _refillTimer?.Dispose();
        _rateLimiter?.Dispose();
    }
}
