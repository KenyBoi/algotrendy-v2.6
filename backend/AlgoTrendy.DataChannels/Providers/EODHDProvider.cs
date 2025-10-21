using System.Collections.Concurrent;
using System.Globalization;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AlgoTrendy.DataChannels.Providers;

/// <summary>
/// EODHD (End of Day Historical Data) provider - FREE tier (20 calls/day, 1 year history)
/// Comprehensive data for stocks, ETFs, crypto, forex, bonds
/// </summary>
public class EODHDProvider : IMarketDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EODHDProvider> _logger;
    private readonly string _apiToken;
    private const string BaseUrl = "https://eodhd.com/api";

    // Rate limiting: FREE tier = 20 calls/day
    private readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private DateTime _lastCall = DateTime.MinValue;
    private readonly TimeSpan _minInterval = TimeSpan.FromSeconds(5); // Safety buffer

    // Track API usage
    private readonly ConcurrentDictionary<DateTime, int> _usageTracker = new();
    private int _todayUsage = 0;

    public string ProviderName => "EODHD";
    public bool IsFreeTier => true;
    public int? DailyRateLimit => 20;

    public EODHDProvider(
        HttpClient httpClient,
        ILogger<EODHDProvider> logger,
        string apiToken)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _apiToken = !string.IsNullOrWhiteSpace(apiToken)
            ? apiToken
            : throw new ArgumentException("EODHD API token is required", nameof(apiToken));
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
            // EODHD uses symbol.EXCHANGE format (e.g., AAPL.US, BTC-USD.CC)
            var formattedSymbol = FormatSymbol(symbol);
            var period = ConvertIntervalToEODHD(interval);

            // EOD endpoint
            var url = $"{BaseUrl}/eod/{formattedSymbol}?" +
                      $"from={startDate:yyyy-MM-dd}&" +
                      $"to={endDate:yyyy-MM-dd}&" +
                      $"period={period}&" +
                      $"api_token={_apiToken}&" +
                      $"fmt=json";

            _logger.LogInformation(
                "[EODHD] Fetching {Symbol} from {Start} to {End} (interval: {Interval})",
                formattedSymbol, startDate, endDate, interval);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("[EODHD] API error: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                    response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    throw new UnauthorizedAccessException("Invalid EODHD API token");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    throw new InvalidOperationException("EODHD API rate limit exceeded");
                }

                return Array.Empty<MarketData>();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonConvert.DeserializeObject<List<EODHDBar>>(json);

            if (data == null || !data.Any())
            {
                _logger.LogWarning("[EODHD] No data returned for {Symbol}", formattedSymbol);
                return Array.Empty<MarketData>();
            }

            var marketDataList = data.Select(bar => new MarketData
            {
                Symbol = symbol,
                Timestamp = DateTime.Parse(bar.Date, null, DateTimeStyles.RoundtripKind),
                Open = bar.Open,
                High = bar.High,
                Low = bar.Low,
                Close = bar.Close,
                Volume = bar.Volume,            }).ToList();

            _logger.LogInformation("[EODHD] Fetched {Count} bars for {Symbol}",
                marketDataList.Count, formattedSymbol);

            return marketDataList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EODHD] Error fetching historical data for {Symbol}", symbol);
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
            var formattedSymbol = FormatSymbol(symbol);

            // Real-time endpoint
            var url = $"{BaseUrl}/real-time/{formattedSymbol}?api_token={_apiToken}&fmt=json";

            _logger.LogInformation("[EODHD] Fetching latest data for {Symbol}", formattedSymbol);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[EODHD] Failed to fetch latest for {Symbol}: {StatusCode}",
                    formattedSymbol, response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonConvert.DeserializeObject<EODHDRealTimeResponse>(json);

            if (data == null || data.Close == 0)
            {
                return null;
            }

            return new MarketData
            {
                Symbol = symbol,
                Timestamp = data.Timestamp > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(data.Timestamp).UtcDateTime
                    : DateTime.UtcNow,
                Open = data.Open,
                High = data.High,
                Low = data.Low,
                Close = data.Close,
                Volume = data.Volume,            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EODHD] Error fetching latest data for {Symbol}", symbol);
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
                    $"EODHD daily limit of {DailyRateLimit} calls exceeded");
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

    private static string FormatSymbol(string symbol)
    {
        // EODHD requires symbol.EXCHANGE format
        // Crypto: BTC-USD.CC, ETH-USD.CC
        // Stocks: AAPL.US, TSLA.US
        // Forex: EUR-USD.FOREX

        var symbolUpper = symbol.ToUpperInvariant();

        // Check if already formatted
        if (symbolUpper.Contains('.'))
        {
            return symbolUpper;
        }

        // Crypto detection
        if (symbolUpper.Contains("BTC") || symbolUpper.Contains("ETH") ||
            symbolUpper.Contains("USDT") || symbolUpper.Contains("USD") && symbolUpper.Length <= 7)
        {
            // Format as crypto: BTC-USD.CC
            if (!symbolUpper.Contains('-'))
            {
                // Assume BTCUSD format, convert to BTC-USD.CC
                var baseCurrency = symbolUpper.Replace("USDT", "").Replace("USD", "");
                return $"{baseCurrency}-USD.CC";
            }
            return $"{symbolUpper}.CC";
        }

        // Forex detection
        if (symbolUpper.Length == 6 && !symbolUpper.Any(char.IsDigit))
        {
            // Likely forex pair like EURUSD
            return $"{symbolUpper.Substring(0, 3)}-{symbolUpper.Substring(3)}.FOREX";
        }

        // Default to US stocks
        return $"{symbolUpper}.US";
    }

    private static string ConvertIntervalToEODHD(string interval)
    {
        // EODHD supports: d (daily), w (weekly), m (monthly)
        return interval.ToLowerInvariant() switch
        {
            "1d" => "d",
            "1w" => "w",
            "1M" => "m",
            _ => "d"
        };
    }

    // DTOs for EODHD API responses
    private class EODHDBar
    {
        [JsonProperty("date")]
        public string Date { get; set; } = string.Empty;

        [JsonProperty("open")]
        public decimal Open { get; set; }

        [JsonProperty("high")]
        public decimal High { get; set; }

        [JsonProperty("low")]
        public decimal Low { get; set; }

        [JsonProperty("close")]
        public decimal Close { get; set; }

        [JsonProperty("adjusted_close")]
        public decimal Adjusted_close { get; set; }

        [JsonProperty("volume")]
        public decimal Volume { get; set; }
    }

    private class EODHDRealTimeResponse
    {
        [JsonProperty("code")]
        public string? Code { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonProperty("gmtoffset")]
        public int GmtOffset { get; set; }

        [JsonProperty("open")]
        public decimal Open { get; set; }

        [JsonProperty("high")]
        public decimal High { get; set; }

        [JsonProperty("low")]
        public decimal Low { get; set; }

        [JsonProperty("close")]
        public decimal Close { get; set; }

        [JsonProperty("volume")]
        public decimal Volume { get; set; }

        [JsonProperty("previousClose")]
        public decimal PreviousClose { get; set; }

        [JsonProperty("change")]
        public decimal Change { get; set; }
    }
}
