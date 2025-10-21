using System.Collections.Concurrent;
using System.Globalization;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AlgoTrendy.DataChannels.Providers;

/// <summary>
/// Polygon.io data provider - FREE tier (5 calls/minute) or PAID tiers available
/// Institutional-grade data for stocks, options, forex, and crypto
/// </summary>
public class PolygonProvider : IMarketDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PolygonProvider> _logger;
    private readonly string _apiKey;
    private const string BaseUrl = "https://api.polygon.io";

    // Rate limiting: FREE tier = 5 calls/minute (1 call every 12 seconds)
    // Add safety buffer: 1 call every 15 seconds
    private readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private DateTime _lastCall = DateTime.MinValue;
    private readonly TimeSpan _minInterval = TimeSpan.FromSeconds(15);

    // Track API usage
    private readonly ConcurrentDictionary<DateTime, int> _usageTracker = new();
    private int _todayUsage = 0;
    private int _minuteUsage = 0;
    private DateTime _minuteStart = DateTime.UtcNow;

    public string ProviderName => "Polygon.io";
    public bool IsFreeTier => true;
    public int? DailyRateLimit => 7200; // 5 calls/min * 60 min * 24 hours (theoretical max)

    public PolygonProvider(
        HttpClient httpClient,
        ILogger<PolygonProvider> logger,
        string apiKey)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _apiKey = !string.IsNullOrWhiteSpace(apiKey)
            ? apiKey
            : throw new ArgumentException("Polygon.io API key is required", nameof(apiKey));
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
            // Convert interval to Polygon format
            var (multiplier, timespan) = ConvertIntervalToPolygon(interval);

            // Format dates
            var fromDate = startDate.ToString("yyyy-MM-dd");
            var toDate = endDate.ToString("yyyy-MM-dd");

            // Build URL - Aggregates (Bars) endpoint
            var url = $"{BaseUrl}/v2/aggs/ticker/{symbol.ToUpperInvariant()}/range/{multiplier}/{timespan}/{fromDate}/{toDate}?" +
                      $"adjusted=true&sort=asc&limit=50000&apiKey={_apiKey}";

            _logger.LogInformation(
                "[Polygon.io] Fetching {Symbol} from {Start} to {End} (interval: {Interval})",
                symbol, fromDate, toDate, interval);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("[Polygon.io] API error: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                    response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    throw new UnauthorizedAccessException("Invalid Polygon.io API key");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    throw new InvalidOperationException("Polygon.io API rate limit exceeded");
                }

                return Array.Empty<MarketData>();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonConvert.DeserializeObject<PolygonAggregatesResponse>(json);

            if (data == null || data.Status != "OK" || data.Results == null || !data.Results.Any())
            {
                _logger.LogWarning("[Polygon.io] No data returned for {Symbol}. Status: {Status}, ResultsCount: {Count}",
                    symbol, data?.Status, data?.ResultsCount ?? 0);
                return Array.Empty<MarketData>();
            }

            var marketDataList = data.Results.Select(bar => new MarketData
            {
                Symbol = symbol,
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(bar.T).UtcDateTime,
                Open = bar.O,
                High = bar.H,
                Low = bar.L,
                Close = bar.C,
                Volume = bar.V,            }).ToList();

            _logger.LogInformation("[Polygon.io] Fetched {Count} candles for {Symbol}",
                marketDataList.Count, symbol);

            return marketDataList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Polygon.io] Error fetching historical data for {Symbol}", symbol);
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
            // Use Previous Close endpoint (most recent complete trading day)
            var url = $"{BaseUrl}/v2/aggs/ticker/{symbol.ToUpperInvariant()}/prev?adjusted=true&apiKey={_apiKey}";

            _logger.LogInformation("[Polygon.io] Fetching latest data for {Symbol}", symbol);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[Polygon.io] Failed to fetch latest for {Symbol}: {StatusCode}",
                    symbol, response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonConvert.DeserializeObject<PolygonAggregatesResponse>(json);

            if (data == null || data.Status != "OK" || data.Results == null || !data.Results.Any())
            {
                return null;
            }

            var bar = data.Results.First();

            return new MarketData
            {
                Symbol = symbol,
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(bar.T).UtcDateTime,
                Open = bar.O,
                High = bar.H,
                Low = bar.L,
                Close = bar.C,
                Volume = bar.V,            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Polygon.io] Error fetching latest data for {Symbol}", symbol);
            return null;
        }
    }

    public async Task<bool> SupportsSymbolAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        await EnforceRateLimitAsync(cancellationToken);

        try
        {
            // Use Ticker Details endpoint
            var url = $"{BaseUrl}/v3/reference/tickers/{symbol.ToUpperInvariant()}?apiKey={_apiKey}";

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var data = JsonConvert.DeserializeObject<PolygonTickerDetailsResponse>(json);
                return data?.Status == "OK" && data.Results != null;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Polygon.io] Error checking symbol support for {Symbol}", symbol);
            return false;
        }
    }

    public Task<int> GetCurrentUsageAsync()
    {
        return Task.FromResult(_todayUsage);
    }

    public Task<int?> GetRemainingCallsAsync()
    {
        var remaining = DailyRateLimit - _todayUsage;
        return Task.FromResult<int?>(remaining > 0 ? remaining : 0);
    }

    private async Task EnforceRateLimitAsync(CancellationToken cancellationToken)
    {
        await _rateLimiter.WaitAsync(cancellationToken);

        try
        {
            // Reset minute counter if minute has passed
            if ((DateTime.UtcNow - _minuteStart).TotalMinutes >= 1)
            {
                _minuteUsage = 0;
                _minuteStart = DateTime.UtcNow;
            }

            // Check per-minute limit (5 calls/minute for free tier)
            if (_minuteUsage >= 5)
            {
                var waitTime = _minuteStart.AddMinutes(1) - DateTime.UtcNow;
                _logger.LogWarning("[Polygon.io] Per-minute rate limit reached. Waiting {Seconds}s",
                    waitTime.TotalSeconds);
                await Task.Delay(waitTime, cancellationToken);
                _minuteUsage = 0;
                _minuteStart = DateTime.UtcNow;
            }

            // Enforce minimum interval between calls (15 seconds safety buffer)
            var timeSinceLastCall = DateTime.UtcNow - _lastCall;
            if (timeSinceLastCall < _minInterval)
            {
                var delay = _minInterval - timeSinceLastCall;
                await Task.Delay(delay, cancellationToken);
            }

            _lastCall = DateTime.UtcNow;
            _todayUsage++;
            _minuteUsage++;

            var today = DateTime.UtcNow.Date;
            _usageTracker.AddOrUpdate(today, 1, (_, count) => count + 1);
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    private static (int multiplier, string timespan) ConvertIntervalToPolygon(string interval)
    {
        // Polygon uses: minute, hour, day, week, month, quarter, year
        return interval.ToLowerInvariant() switch
        {
            "1m" => (1, "minute"),
            "5m" => (5, "minute"),
            "15m" => (15, "minute"),
            "30m" => (30, "minute"),
            "1h" => (1, "hour"),
            "4h" => (4, "hour"),
            "1d" => (1, "day"),
            "1w" => (1, "week"),
            "1M" => (1, "month"),
            _ => (1, "day")
        };
    }

    // DTOs for Polygon.io API responses
    private class PolygonAggregatesResponse
    {
        [JsonProperty("ticker")]
        public string? Ticker { get; set; }

        [JsonProperty("status")]
        public string? Status { get; set; }

        [JsonProperty("adjusted")]
        public bool Adjusted { get; set; }

        [JsonProperty("queryCount")]
        public int QueryCount { get; set; }

        [JsonProperty("resultsCount")]
        public int ResultsCount { get; set; }

        [JsonProperty("results")]
        public List<PolygonBar>? Results { get; set; }
    }

    private class PolygonBar
    {
        [JsonProperty("v")]
        public decimal V { get; set; } // Volume

        [JsonProperty("vw")]
        public decimal? Vw { get; set; } // Volume Weighted Average Price

        [JsonProperty("o")]
        public decimal O { get; set; } // Open

        [JsonProperty("c")]
        public decimal C { get; set; } // Close

        [JsonProperty("h")]
        public decimal H { get; set; } // High

        [JsonProperty("l")]
        public decimal L { get; set; } // Low

        [JsonProperty("t")]
        public long T { get; set; } // Timestamp (Unix milliseconds)

        [JsonProperty("n")]
        public int? N { get; set; } // Number of transactions
    }

    private class PolygonTickerDetailsResponse
    {
        [JsonProperty("status")]
        public string? Status { get; set; }

        [JsonProperty("results")]
        public PolygonTickerDetails? Results { get; set; }
    }

    private class PolygonTickerDetails
    {
        [JsonProperty("ticker")]
        public string? Ticker { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("market")]
        public string? Market { get; set; }

        [JsonProperty("locale")]
        public string? Locale { get; set; }

        [JsonProperty("primary_exchange")]
        public string? PrimaryExchange { get; set; }

        [JsonProperty("type")]
        public string? Type { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("currency_name")]
        public string? CurrencyName { get; set; }
    }
}
