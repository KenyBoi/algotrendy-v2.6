using System.Collections.Concurrent;
using System.Globalization;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AlgoTrendy.DataChannels.Providers;

/// <summary>
/// Twelve Data provider - FREE tier (800 calls/day)
/// Real-time data for stocks, forex, and crypto
/// </summary>
public class TwelveDataProvider : IMarketDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TwelveDataProvider> _logger;
    private readonly string _apiKey;
    private const string BaseUrl = "https://api.twelvedata.com";

    // Rate limiting: FREE tier = 800 calls/day
    private readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private DateTime _lastCall = DateTime.MinValue;
    private readonly TimeSpan _minInterval = TimeSpan.FromSeconds(2); // Safety buffer

    // Track API usage
    private readonly ConcurrentDictionary<DateTime, int> _usageTracker = new();
    private int _todayUsage = 0;

    public string ProviderName => "Twelve Data";
    public bool IsFreeTier => true;
    public int? DailyRateLimit => 800;

    public TwelveDataProvider(
        HttpClient httpClient,
        ILogger<TwelveDataProvider> logger,
        string apiKey)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _apiKey = !string.IsNullOrWhiteSpace(apiKey)
            ? apiKey
            : throw new ArgumentException("Twelve Data API key is required", nameof(apiKey));
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
            var timeInterval = ConvertIntervalToTwelveData(interval);

            // Twelve Data time series endpoint
            var url = $"{BaseUrl}/time_series?" +
                      $"symbol={symbol.ToUpperInvariant()}&" +
                      $"interval={timeInterval}&" +
                      $"start_date={startDate:yyyy-MM-dd}&" +
                      $"end_date={endDate:yyyy-MM-dd}&" +
                      $"apikey={_apiKey}&" +
                      $"format=JSON&" +
                      $"outputsize=5000";

            _logger.LogInformation(
                "[TwelveData] Fetching {Symbol} from {Start} to {End} (interval: {Interval})",
                symbol, startDate, endDate, interval);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("[TwelveData] API error: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                    response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    throw new UnauthorizedAccessException("Invalid Twelve Data API key");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    throw new InvalidOperationException("Twelve Data API rate limit exceeded");
                }

                return Array.Empty<MarketData>();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonConvert.DeserializeObject<TwelveDataTimeSeriesResponse>(json);

            if (data == null || data.Status == "error")
            {
                _logger.LogWarning("[TwelveData] Error in response for {Symbol}: {Message}",
                    symbol, data?.Message ?? "Unknown error");
                return Array.Empty<MarketData>();
            }

            if (data.Values == null || !data.Values.Any())
            {
                _logger.LogWarning("[TwelveData] No data returned for {Symbol}", symbol);
                return Array.Empty<MarketData>();
            }

            var marketDataList = data.Values.Select(bar => new MarketData
            {
                Symbol = symbol,
                Timestamp = DateTime.Parse(bar.Datetime, null, DateTimeStyles.RoundtripKind),
                Open = decimal.Parse(bar.Open, CultureInfo.InvariantCulture),
                High = decimal.Parse(bar.High, CultureInfo.InvariantCulture),
                Low = decimal.Parse(bar.Low, CultureInfo.InvariantCulture),
                Close = decimal.Parse(bar.Close, CultureInfo.InvariantCulture),
                Volume = decimal.Parse(bar.Volume ?? "0", CultureInfo.InvariantCulture),
            }).OrderBy(x => x.Timestamp).ToList();

            _logger.LogInformation("[TwelveData] Fetched {Count} bars for {Symbol}",
                marketDataList.Count, symbol);

            return marketDataList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TwelveData] Error fetching historical data for {Symbol}", symbol);
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
            // Quote endpoint for latest price
            var url = $"{BaseUrl}/quote?symbol={symbol.ToUpperInvariant()}&apikey={_apiKey}";

            _logger.LogInformation("[TwelveData] Fetching latest data for {Symbol}", symbol);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[TwelveData] Failed to fetch latest for {Symbol}: {StatusCode}",
                    symbol, response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonConvert.DeserializeObject<TwelveDataQuoteResponse>(json);

            if (data == null || string.IsNullOrEmpty(data.Close))
            {
                return null;
            }

            return new MarketData
            {
                Symbol = symbol,
                Timestamp = DateTime.Parse(data.Datetime ?? DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    null, DateTimeStyles.RoundtripKind),
                Open = decimal.Parse(data.Open ?? data.Close, CultureInfo.InvariantCulture),
                High = decimal.Parse(data.High ?? data.Close, CultureInfo.InvariantCulture),
                Low = decimal.Parse(data.Low ?? data.Close, CultureInfo.InvariantCulture),
                Close = decimal.Parse(data.Close, CultureInfo.InvariantCulture),
                Volume = decimal.Parse(data.Volume ?? "0", CultureInfo.InvariantCulture),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TwelveData] Error fetching latest data for {Symbol}", symbol);
            return null;
        }
    }

    public async Task<bool> SupportsSymbolAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        // For simplicity, assume support and let API return error if not supported
        await Task.CompletedTask;
        return true;
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
            // Check daily limit
            if (_todayUsage >= DailyRateLimit)
            {
                throw new InvalidOperationException(
                    $"Twelve Data daily limit of {DailyRateLimit} calls exceeded");
            }

            // Enforce minimum interval between calls
            var timeSinceLastCall = DateTime.UtcNow - _lastCall;
            if (timeSinceLastCall < _minInterval)
            {
                var delay = _minInterval - timeSinceLastCall;
                await Task.Delay(delay, cancellationToken);
            }

            _lastCall = DateTime.UtcNow;
            _todayUsage++;

            var today = DateTime.UtcNow.Date;
            _usageTracker.AddOrUpdate(today, 1, (_, count) => count + 1);
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    private static string ConvertIntervalToTwelveData(string interval)
    {
        // Twelve Data uses: 1min, 5min, 15min, 30min, 45min, 1h, 2h, 4h, 1day, 1week, 1month
        return interval.ToLowerInvariant() switch
        {
            "1m" => "1min",
            "5m" => "5min",
            "15m" => "15min",
            "30m" => "30min",
            "1h" => "1h",
            "4h" => "4h",
            "1d" => "1day",
            "1w" => "1week",
            "1M" => "1month",
            _ => "1day"
        };
    }

    // DTOs for Twelve Data API responses
    private class TwelveDataTimeSeriesResponse
    {
        [JsonProperty("meta")]
        public TwelveDataMeta? Meta { get; set; }

        [JsonProperty("values")]
        public List<TwelveDataBar>? Values { get; set; }

        [JsonProperty("status")]
        public string? Status { get; set; }

        [JsonProperty("message")]
        public string? Message { get; set; }
    }

    private class TwelveDataMeta
    {
        [JsonProperty("symbol")]
        public string? Symbol { get; set; }

        [JsonProperty("interval")]
        public string? Interval { get; set; }
    }

    private class TwelveDataBar
    {
        [JsonProperty("datetime")]
        public string Datetime { get; set; } = string.Empty;

        [JsonProperty("open")]
        public string Open { get; set; } = string.Empty;

        [JsonProperty("high")]
        public string High { get; set; } = string.Empty;

        [JsonProperty("low")]
        public string Low { get; set; } = string.Empty;

        [JsonProperty("close")]
        public string Close { get; set; } = string.Empty;

        [JsonProperty("volume")]
        public string? Volume { get; set; }
    }

    private class TwelveDataQuoteResponse
    {
        [JsonProperty("symbol")]
        public string? Symbol { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("datetime")]
        public string? Datetime { get; set; }

        [JsonProperty("open")]
        public string? Open { get; set; }

        [JsonProperty("high")]
        public string? High { get; set; }

        [JsonProperty("low")]
        public string? Low { get; set; }

        [JsonProperty("close")]
        public string Close { get; set; } = string.Empty;

        [JsonProperty("volume")]
        public string? Volume { get; set; }
    }
}
