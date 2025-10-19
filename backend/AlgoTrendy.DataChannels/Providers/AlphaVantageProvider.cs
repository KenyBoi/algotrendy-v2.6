using System.Collections.Concurrent;
using System.Globalization;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace AlgoTrendy.DataChannels.Providers;

/// <summary>
/// Alpha Vantage data provider - FREE tier (500 calls/day, 25/day for premium endpoints)
/// Official NASDAQ vendor with 20+ years of historical data
/// </summary>
public class AlphaVantageProvider : IMarketDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AlphaVantageProvider> _logger;
    private readonly string _apiKey;
    private const string BaseUrl = "https://www.alphavantage.co/query";

    // Rate limiting: 500 calls/day = 1 call every 172.8 seconds (~3 minutes)
    private readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private DateTime _lastCall = DateTime.MinValue;
    private readonly TimeSpan _minInterval = TimeSpan.FromSeconds(180); // 3 minutes safety buffer

    // Track API usage
    private readonly ConcurrentDictionary<DateTime, int> _usageTracker = new();
    private int _todayUsage = 0;

    public string ProviderName => "AlphaVantage";
    public bool IsFreeTier => true;
    public int? DailyRateLimit => 500;

    public AlphaVantageProvider(
        HttpClient httpClient,
        ILogger<AlphaVantageProvider> logger,
        string apiKey)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _apiKey = !string.IsNullOrWhiteSpace(apiKey)
            ? apiKey
            : throw new ArgumentException("Alpha Vantage API key is required", nameof(apiKey));
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
            var function = GetFunctionForInterval(interval);
            var url = $"{BaseUrl}?function={function}&symbol={symbol}&outputsize=full&apikey={_apiKey}";

            // For intraday, add interval parameter
            if (function == "TIME_SERIES_INTRADAY")
            {
                url += $"&interval={ConvertToAlphaVantageInterval(interval)}";
            }

            _logger.LogInformation(
                "[AlphaVantage] Fetching {Symbol} from {Start:yyyy-MM-dd} to {End:yyyy-MM-dd} (interval: {Interval})",
                symbol, startDate, endDate, interval);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            // Check for rate limit message
            if (json.Contains("Thank you for using Alpha Vantage") ||
                json.Contains("higher API call frequency"))
            {
                _logger.LogWarning("[AlphaVantage] API rate limit reached");
                throw new InvalidOperationException("Alpha Vantage API rate limit exceeded. Current usage: " + _todayUsage);
            }

            // Check for invalid API key
            if (json.Contains("Invalid API call") || json.Contains("Error Message"))
            {
                _logger.LogError("[AlphaVantage] Invalid API call. Response: {Response}", json.Substring(0, Math.Min(200, json.Length)));
                throw new InvalidOperationException("Invalid Alpha Vantage API call");
            }

            var data = JObject.Parse(json);

            // Find the time series key (varies by function)
            var timeSeriesKey = data.Properties()
                .FirstOrDefault(p => p.Name.StartsWith("Time Series"))?.Name;

            if (string.IsNullOrEmpty(timeSeriesKey))
            {
                _logger.LogWarning("[AlphaVantage] No time series data found for {Symbol}. Response keys: {Keys}",
                    symbol, string.Join(", ", data.Properties().Select(p => p.Name)));
                return Array.Empty<MarketData>();
            }

            var timeSeries = data[timeSeriesKey] as JObject;
            if (timeSeries == null)
            {
                _logger.LogWarning("[AlphaVantage] Time series is null for {Symbol}", symbol);
                return Array.Empty<MarketData>();
            }

            var result = new List<MarketData>();

            foreach (var item in timeSeries)
            {
                if (!DateTime.TryParse(item.Key, out var timestamp))
                {
                    _logger.LogWarning("[AlphaVantage] Could not parse timestamp: {Timestamp}", item.Key);
                    continue;
                }

                // Filter by date range
                if (timestamp < startDate || timestamp > endDate)
                    continue;

                var values = item.Value as JObject;
                if (values == null) continue;

                try
                {
                    result.Add(new MarketData
                    {
                        Symbol = symbol,
                        Timestamp = DateTime.SpecifyKind(timestamp, DateTimeKind.Utc),
                        Open = ParseDecimal(values["1. open"]),
                        High = ParseDecimal(values["2. high"]),
                        Low = ParseDecimal(values["3. low"]),
                        Close = ParseDecimal(values["4. close"]),
                        Volume = ParseDecimal(values["5. volume"]),
                        Source = ProviderName
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[AlphaVantage] Error parsing data point for {Symbol} at {Timestamp}",
                        symbol, timestamp);
                }
            }

            // Sort by timestamp ascending
            result.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

            _logger.LogInformation("[AlphaVantage] Fetched {Count} bars for {Symbol}", result.Count, symbol);

            // Track usage
            IncrementUsage();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AlphaVantage] Error fetching data for {Symbol}", symbol);
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
            var url = $"{BaseUrl}?function=GLOBAL_QUOTE&symbol={symbol}&apikey={_apiKey}";

            _logger.LogInformation("[AlphaVantage] Fetching latest quote for {Symbol}", symbol);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JObject.Parse(json);

            var quote = data["Global Quote"] as JObject;
            if (quote == null || !quote.HasValues)
            {
                _logger.LogWarning("[AlphaVantage] No quote data found for {Symbol}", symbol);
                return null;
            }

            var latestTradingDay = quote["07. latest trading day"]?.ToString();
            if (string.IsNullOrEmpty(latestTradingDay))
            {
                _logger.LogWarning("[AlphaVantage] No latest trading day in quote for {Symbol}", symbol);
                return null;
            }

            IncrementUsage();

            return new MarketData
            {
                Symbol = symbol,
                Timestamp = DateTime.SpecifyKind(DateTime.Parse(latestTradingDay), DateTimeKind.Utc),
                Open = ParseDecimal(quote["02. open"]),
                High = ParseDecimal(quote["03. high"]),
                Low = ParseDecimal(quote["04. low"]),
                Close = ParseDecimal(quote["05. price"]),
                Volume = ParseDecimal(quote["06. volume"]),
                Source = ProviderName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AlphaVantage] Error fetching latest quote for {Symbol}", symbol);
            throw;
        }
    }

    public Task<bool> SupportsSymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        // Alpha Vantage supports 200,000+ tickers globally
        // US stocks, forex, crypto
        // For now, return true (validation happens during fetch)
        return Task.FromResult(true);
    }

    public Task<int> GetCurrentUsageAsync()
    {
        return Task.FromResult(_todayUsage);
    }

    public Task<int?> GetRemainingCallsAsync()
    {
        var remaining = DailyRateLimit - _todayUsage;
        return Task.FromResult<int?>(Math.Max(0, remaining.GetValueOrDefault()));
    }

    private async Task EnforceRateLimitAsync(CancellationToken cancellationToken)
    {
        await _rateLimiter.WaitAsync(cancellationToken);
        try
        {
            // Check daily limit
            if (_todayUsage >= DailyRateLimit)
            {
                _logger.LogWarning("[AlphaVantage] Daily rate limit reached: {Usage}/{Limit}",
                    _todayUsage, DailyRateLimit);
                throw new InvalidOperationException($"Alpha Vantage daily rate limit reached: {_todayUsage}/{DailyRateLimit}");
            }

            // Enforce minimum interval between calls
            var timeSinceLastCall = DateTime.UtcNow - _lastCall;
            if (timeSinceLastCall < _minInterval)
            {
                var delay = _minInterval - timeSinceLastCall;
                _logger.LogInformation("[AlphaVantage] Rate limiting: waiting {Seconds:F1}s before next call",
                    delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }

            _lastCall = DateTime.UtcNow;
        }
        finally
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

        _logger.LogDebug("[AlphaVantage] API usage: {Usage}/{Limit} today", _todayUsage, DailyRateLimit);
    }

    private static string GetFunctionForInterval(string interval)
    {
        return interval.ToLower() switch
        {
            "1d" or "daily" => "TIME_SERIES_DAILY",
            "1wk" or "weekly" => "TIME_SERIES_WEEKLY",
            "1mo" or "monthly" => "TIME_SERIES_MONTHLY",
            _ => "TIME_SERIES_INTRADAY" // 1m, 5m, 15m, 30m, 60m
        };
    }

    private static string ConvertToAlphaVantageInterval(string interval)
    {
        return interval.ToLower() switch
        {
            "1m" => "1min",
            "5m" => "5min",
            "15m" => "15min",
            "30m" => "30min",
            "1h" or "60m" => "60min",
            _ => "5min"
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
}
