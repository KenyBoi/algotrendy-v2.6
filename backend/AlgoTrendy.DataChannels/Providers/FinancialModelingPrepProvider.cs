using System.Collections.Concurrent;
using System.Globalization;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace AlgoTrendy.DataChannels.Providers;

/// <summary>
/// Financial Modeling Prep (FMP) data provider - FREE tier (250 calls/day, 500MB bandwidth/30 days)
/// Includes SEC-audited financial statements, 50+ financial ratios, social sentiment, ESG scores
/// </summary>
public class FinancialModelingPrepProvider : IMarketDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FinancialModelingPrepProvider> _logger;
    private readonly string _apiKey;
    private const string BaseUrlV3 = "https://financialmodelingprep.com/api/v3";
    private const string BaseUrlV4 = "https://financialmodelingprep.com/api/v4";

    // Rate limiting: 250 calls/day = ~10.4 calls/hour = 1 call every 5.76 minutes
    // Conservative: 1 call every 6 minutes = 240 calls/day (safety buffer)
    private readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private DateTime _lastCall = DateTime.MinValue;
    private readonly TimeSpan _minInterval = TimeSpan.FromMinutes(6); // 6 minutes safety buffer

    // Track API usage
    private readonly ConcurrentDictionary<DateTime, int> _usageTracker = new();
    private int _todayUsage = 0;

    public string ProviderName => "FinancialModelingPrep";
    public bool IsFreeTier => true;
    public int? DailyRateLimit => 250;

    public FinancialModelingPrepProvider(
        HttpClient httpClient,
        ILogger<FinancialModelingPrepProvider> logger,
        string apiKey)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _apiKey = !string.IsNullOrWhiteSpace(apiKey)
            ? apiKey
            : throw new ArgumentException("FMP API key is required", nameof(apiKey));
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
            // FMP supports: 1min, 5min, 15min, 30min, 1hour, 4hour, daily
            var fmpInterval = ConvertToFMPInterval(interval);
            string url;

            if (fmpInterval == "daily")
            {
                // Use historical-price-full for daily data
                url = $"{BaseUrlV3}/historical-price-full/{symbol}?from={startDate:yyyy-MM-dd}&to={endDate:yyyy-MM-dd}&apikey={_apiKey}";
            }
            else
            {
                // Use historical-chart for intraday data
                url = $"{BaseUrlV3}/historical-chart/{fmpInterval}/{symbol}?from={startDate:yyyy-MM-dd}&to={endDate:yyyy-MM-dd}&apikey={_apiKey}";
            }

            _logger.LogInformation(
                "[FMP] Fetching {Symbol} from {Start:yyyy-MM-dd} to {End:yyyy-MM-dd} (interval: {Interval})",
                symbol, startDate, endDate, interval);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            // Check for error messages
            if (json.Contains("\"Error Message\"") || json.Contains("Invalid API"))
            {
                _logger.LogWarning("[FMP] API error for {Symbol}. Response: {Response}",
                    symbol, json.Substring(0, Math.Min(200, json.Length)));
                return Array.Empty<MarketData>();
            }

            var result = new List<MarketData>();

            if (fmpInterval == "daily")
            {
                // Parse daily data format
                var data = JObject.Parse(json);
                var historical = data["historical"] as JArray;

                if (historical == null || historical.Count == 0)
                {
                    _logger.LogWarning("[FMP] No historical data found for {Symbol}", symbol);
                    return Array.Empty<MarketData>();
                }

                foreach (var item in historical)
                {
                    var dateStr = item["date"]?.ToString();
                    if (string.IsNullOrEmpty(dateStr) || !DateTime.TryParse(dateStr, out var timestamp))
                        continue;

                    result.Add(new MarketData
                    {
                        Symbol = symbol,
                        Timestamp = DateTime.SpecifyKind(timestamp, DateTimeKind.Utc),
                        Open = ParseDecimal(item["open"]),
                        High = ParseDecimal(item["high"]),
                        Low = ParseDecimal(item["low"]),
                        Close = ParseDecimal(item["close"]),
                        Volume = ParseDecimal(item["volume"]),
                        Source = ProviderName
                    });
                }
            }
            else
            {
                // Parse intraday data format (array)
                var data = JArray.Parse(json);

                if (data.Count == 0)
                {
                    _logger.LogWarning("[FMP] No intraday data found for {Symbol}", symbol);
                    return Array.Empty<MarketData>();
                }

                foreach (var item in data)
                {
                    var dateStr = item["date"]?.ToString();
                    if (string.IsNullOrEmpty(dateStr) || !DateTime.TryParse(dateStr, out var timestamp))
                        continue;

                    result.Add(new MarketData
                    {
                        Symbol = symbol,
                        Timestamp = DateTime.SpecifyKind(timestamp, DateTimeKind.Utc),
                        Open = ParseDecimal(item["open"]),
                        High = ParseDecimal(item["high"]),
                        Low = ParseDecimal(item["low"]),
                        Close = ParseDecimal(item["close"]),
                        Volume = ParseDecimal(item["volume"]),
                        Source = ProviderName
                    });
                }
            }

            // Sort by timestamp ascending
            result.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

            _logger.LogInformation("[FMP] Fetched {Count} bars for {Symbol}", result.Count, symbol);

            IncrementUsage();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FMP] Error fetching historical data for {Symbol}", symbol);
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
            var url = $"{BaseUrlV3}/quote/{symbol}?apikey={_apiKey}";

            _logger.LogInformation("[FMP] Fetching latest quote for {Symbol}", symbol);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JArray.Parse(json);

            if (data.Count == 0)
            {
                _logger.LogWarning("[FMP] No quote data found for {Symbol}", symbol);
                return null;
            }

            var quote = data[0];

            // FMP quote fields: symbol, price, volume, timestamp, open, previousClose, dayLow, dayHigh, etc.
            var timestampUnix = quote["timestamp"]?.ToObject<long>() ?? 0;
            var timestamp = timestampUnix > 0
                ? DateTimeOffset.FromUnixTimeSeconds(timestampUnix).UtcDateTime
                : DateTime.UtcNow;

            IncrementUsage();

            return new MarketData
            {
                Symbol = symbol,
                Timestamp = timestamp,
                Open = ParseDecimal(quote["open"]),
                High = ParseDecimal(quote["dayHigh"]),
                Low = ParseDecimal(quote["dayLow"]),
                Close = ParseDecimal(quote["price"]),
                Volume = ParseDecimal(quote["volume"]),
                Source = ProviderName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FMP] Error fetching latest quote for {Symbol}", symbol);
            throw;
        }
    }

    public Task<bool> SupportsSymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        // FMP supports 50,000+ stocks, ETFs, mutual funds
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

    /// <summary>
    /// Fetches company profile/fundamentals
    /// Additional endpoint not in IMarketDataProvider interface
    /// </summary>
    public async Task<JArray?> GetCompanyProfileAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        await EnforceRateLimitAsync(cancellationToken);

        try
        {
            var url = $"{BaseUrlV3}/profile/{symbol}?apikey={_apiKey}";

            _logger.LogInformation("[FMP] Fetching company profile for {Symbol}", symbol);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JArray.Parse(json);

            IncrementUsage();

            return data.Count > 0 ? data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FMP] Error fetching company profile for {Symbol}", symbol);
            return null;
        }
    }

    /// <summary>
    /// Fetches income statement (SEC-audited)
    /// Additional endpoint not in IMarketDataProvider interface
    /// </summary>
    public async Task<JArray?> GetIncomeStatementAsync(
        string symbol,
        string period = "annual",
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        await EnforceRateLimitAsync(cancellationToken);

        try
        {
            var url = $"{BaseUrlV3}/income-statement/{symbol}?period={period}&apikey={_apiKey}";
            if (limit.HasValue)
                url += $"&limit={limit.Value}";

            _logger.LogInformation("[FMP] Fetching income statement for {Symbol} ({Period})", symbol, period);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JArray.Parse(json);

            IncrementUsage();

            return data.Count > 0 ? data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FMP] Error fetching income statement for {Symbol}", symbol);
            return null;
        }
    }

    /// <summary>
    /// Fetches balance sheet (SEC-audited)
    /// Additional endpoint not in IMarketDataProvider interface
    /// </summary>
    public async Task<JArray?> GetBalanceSheetAsync(
        string symbol,
        string period = "annual",
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        await EnforceRateLimitAsync(cancellationToken);

        try
        {
            var url = $"{BaseUrlV3}/balance-sheet-statement/{symbol}?period={period}&apikey={_apiKey}";
            if (limit.HasValue)
                url += $"&limit={limit.Value}";

            _logger.LogInformation("[FMP] Fetching balance sheet for {Symbol} ({Period})", symbol, period);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JArray.Parse(json);

            IncrementUsage();

            return data.Count > 0 ? data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FMP] Error fetching balance sheet for {Symbol}", symbol);
            return null;
        }
    }

    /// <summary>
    /// Fetches cash flow statement (SEC-audited)
    /// Additional endpoint not in IMarketDataProvider interface
    /// </summary>
    public async Task<JArray?> GetCashFlowStatementAsync(
        string symbol,
        string period = "annual",
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        await EnforceRateLimitAsync(cancellationToken);

        try
        {
            var url = $"{BaseUrlV3}/cash-flow-statement/{symbol}?period={period}&apikey={_apiKey}";
            if (limit.HasValue)
                url += $"&limit={limit.Value}";

            _logger.LogInformation("[FMP] Fetching cash flow statement for {Symbol} ({Period})", symbol, period);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JArray.Parse(json);

            IncrementUsage();

            return data.Count > 0 ? data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FMP] Error fetching cash flow statement for {Symbol}", symbol);
            return null;
        }
    }

    /// <summary>
    /// Fetches financial ratios (50+ ratios calculated)
    /// Additional endpoint not in IMarketDataProvider interface
    /// </summary>
    public async Task<JArray?> GetFinancialRatiosAsync(
        string symbol,
        string period = "annual",
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        await EnforceRateLimitAsync(cancellationToken);

        try
        {
            var url = $"{BaseUrlV3}/ratios/{symbol}?period={period}&apikey={_apiKey}";
            if (limit.HasValue)
                url += $"&limit={limit.Value}";

            _logger.LogInformation("[FMP] Fetching financial ratios for {Symbol} ({Period})", symbol, period);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JArray.Parse(json);

            IncrementUsage();

            return data.Count > 0 ? data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FMP] Error fetching financial ratios for {Symbol}", symbol);
            return null;
        }
    }

    /// <summary>
    /// Fetches social sentiment from Reddit, Twitter, StockTwits, Yahoo Finance
    /// Additional endpoint not in IMarketDataProvider interface
    /// </summary>
    public async Task<JArray?> GetSocialSentimentAsync(
        string symbol,
        int page = 0,
        CancellationToken cancellationToken = default)
    {
        await EnforceRateLimitAsync(cancellationToken);

        try
        {
            var url = $"{BaseUrlV4}/historical/social-sentiment?symbol={symbol}&page={page}&apikey={_apiKey}";

            _logger.LogInformation("[FMP] Fetching social sentiment for {Symbol} (page: {Page})", symbol, page);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JArray.Parse(json);

            IncrementUsage();

            return data.Count > 0 ? data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FMP] Error fetching social sentiment for {Symbol}", symbol);
            return null;
        }
    }

    /// <summary>
    /// Fetches ESG (Environmental, Social, Governance) scores
    /// Additional endpoint not in IMarketDataProvider interface
    /// </summary>
    public async Task<JArray?> GetESGScoresAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        await EnforceRateLimitAsync(cancellationToken);

        try
        {
            var url = $"{BaseUrlV4}/esg-environmental-social-governance-data?symbol={symbol}&apikey={_apiKey}";

            _logger.LogInformation("[FMP] Fetching ESG scores for {Symbol}", symbol);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JArray.Parse(json);

            IncrementUsage();

            return data.Count > 0 ? data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FMP] Error fetching ESG scores for {Symbol}", symbol);
            return null;
        }
    }

    /// <summary>
    /// Fetches institutional holdings (13F filings)
    /// Additional endpoint not in IMarketDataProvider interface
    /// </summary>
    public async Task<JArray?> GetInstitutionalHoldingsAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        await EnforceRateLimitAsync(cancellationToken);

        try
        {
            var url = $"{BaseUrlV3}/institutional-holder/{symbol}?apikey={_apiKey}";

            _logger.LogInformation("[FMP] Fetching institutional holdings for {Symbol}", symbol);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JArray.Parse(json);

            IncrementUsage();

            return data.Count > 0 ? data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FMP] Error fetching institutional holdings for {Symbol}", symbol);
            return null;
        }
    }

    private async Task EnforceRateLimitAsync(CancellationToken cancellationToken)
    {
        await _rateLimiter.WaitAsync(cancellationToken);
        try
        {
            // Check daily limit
            if (_todayUsage >= DailyRateLimit)
            {
                _logger.LogWarning("[FMP] Daily rate limit reached: {Usage}/{Limit}",
                    _todayUsage, DailyRateLimit);
                throw new InvalidOperationException($"FMP daily rate limit reached: {_todayUsage}/{DailyRateLimit}");
            }

            // Enforce minimum interval between calls (6 minutes)
            var timeSinceLastCall = DateTime.UtcNow - _lastCall;
            if (timeSinceLastCall < _minInterval)
            {
                var delay = _minInterval - timeSinceLastCall;
                _logger.LogInformation("[FMP] Rate limiting: waiting {Seconds:F1}s before next call (conservative 6-minute interval)",
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

        _logger.LogDebug("[FMP] API usage: {Usage}/{Limit} today ({Remaining} remaining)",
            _todayUsage, DailyRateLimit, DailyRateLimit - _todayUsage);
    }

    private static string ConvertToFMPInterval(string interval)
    {
        return interval.ToLower() switch
        {
            "1m" => "1min",
            "5m" => "5min",
            "15m" => "15min",
            "30m" => "30min",
            "1h" or "60m" => "1hour",
            "4h" => "4hour",
            "1d" or "daily" => "daily",
            _ => "daily"  // Default to daily
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
        _rateLimiter?.Dispose();
    }
}
