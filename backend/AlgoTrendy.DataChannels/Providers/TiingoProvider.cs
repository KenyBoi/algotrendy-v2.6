using System.Collections.Concurrent;
using System.Globalization;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AlgoTrendy.DataChannels.Providers;

/// <summary>
/// Tiingo data provider - FREE tier (1000 calls/hour, 50,000 calls/month)
/// Provides high-quality stock, crypto, and forex data with 20+ years of history
/// </summary>
public class TiingoProvider : IMarketDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TiingoProvider> _logger;
    private readonly string _apiKey;
    private const string BaseUrl = "https://api.tiingo.com";

    // Rate limiting: 1000 calls/hour = 1 call every 3.6 seconds
    // Add safety buffer: 1 call every 5 seconds
    private readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private DateTime _lastCall = DateTime.MinValue;
    private readonly TimeSpan _minInterval = TimeSpan.FromSeconds(5);

    // Track API usage
    private readonly ConcurrentDictionary<DateTime, int> _usageTracker = new();
    private int _todayUsage = 0;
    private int _hourlyUsage = 0;
    private DateTime _hourStart = DateTime.UtcNow;

    public string ProviderName => "Tiingo";
    public bool IsFreeTier => true;
    public int? DailyRateLimit => 50000; // Monthly limit / 30 days

    public TiingoProvider(
        HttpClient httpClient,
        ILogger<TiingoProvider> logger,
        string apiKey)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _apiKey = !string.IsNullOrWhiteSpace(apiKey)
            ? apiKey
            : throw new ArgumentException("Tiingo API key is required", nameof(apiKey));

        // Set authorization header
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Token {_apiKey}");
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
            // Determine if this is crypto or stock
            var isCrypto = IsCryptoSymbol(symbol);

            string url;
            if (isCrypto)
            {
                // Crypto endpoint
                url = $"{BaseUrl}/tiingo/crypto/prices?" +
                      $"tickers={symbol}&" +
                      $"startDate={startDate:yyyy-MM-dd}&" +
                      $"endDate={endDate:yyyy-MM-dd}&" +
                      $"resampleFreq={ConvertIntervalToTiingoFreq(interval)}";
            }
            else if (interval == "1d")
            {
                // Daily stock data
                url = $"{BaseUrl}/tiingo/daily/{symbol}/prices?" +
                      $"startDate={startDate:yyyy-MM-dd}&" +
                      $"endDate={endDate:yyyy-MM-dd}";
            }
            else
            {
                // Intraday IEX data
                url = $"{BaseUrl}/iex/{symbol}/prices?" +
                      $"startDate={startDate:yyyy-MM-dd}&" +
                      $"endDate={endDate:yyyy-MM-dd}&" +
                      $"resampleFreq={ConvertIntervalToTiingoFreq(interval)}";
            }

            _logger.LogInformation(
                "[Tiingo] Fetching {Symbol} from {Start:yyyy-MM-dd} to {End:yyyy-MM-dd} (interval: {Interval})",
                symbol, startDate, endDate, interval);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("[Tiingo] API error: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException("Invalid Tiingo API key");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    throw new InvalidOperationException("Tiingo API rate limit exceeded");
                }

                return Array.Empty<MarketData>();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(json) || json == "[]")
            {
                _logger.LogWarning("[Tiingo] No data returned for {Symbol}", symbol);
                return Array.Empty<MarketData>();
            }

            var marketDataList = new List<MarketData>();

            if (isCrypto)
            {
                // Parse crypto response
                var cryptoData = JsonConvert.DeserializeObject<List<TiingoCryptoPriceData>>(json);
                if (cryptoData != null)
                {
                    foreach (var item in cryptoData)
                    {
                        if (item.PriceData != null)
                        {
                            foreach (var price in item.PriceData)
                            {
                                marketDataList.Add(new MarketData
                                {
                                    Symbol = symbol,
                                    Timestamp = price.Date,
                                    Open = price.Open,
                                    High = price.High,
                                    Low = price.Low,
                                    Close = price.Close,
                                    Volume = price.Volume,
                                    Source = "tiingo"
                                });
                            }
                        }
                    }
                }
            }
            else
            {
                // Parse stock response
                var stockData = JsonConvert.DeserializeObject<List<TiingoStockPriceData>>(json);
                if (stockData != null)
                {
                    foreach (var item in stockData)
                    {
                        marketDataList.Add(new MarketData
                        {
                            Symbol = symbol,
                            Timestamp = item.Date,
                            Open = item.Open,
                            High = item.High,
                            Low = item.Low,
                            Close = item.Close,
                            Volume = item.Volume,
                            Source = "tiingo"
                        });
                    }
                }
            }

            // Filter by date range
            var filtered = marketDataList
                .Where(m => m.Timestamp >= startDate && m.Timestamp <= endDate)
                .OrderBy(m => m.Timestamp)
                .ToList();

            _logger.LogInformation("[Tiingo] Fetched {Count} candles for {Symbol}",
                filtered.Count, symbol);

            return filtered;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Tiingo] Error fetching historical data for {Symbol}", symbol);
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
            var isCrypto = IsCryptoSymbol(symbol);

            string url;
            if (isCrypto)
            {
                url = $"{BaseUrl}/tiingo/crypto/prices?tickers={symbol}";
            }
            else
            {
                url = $"{BaseUrl}/iex/{symbol}";
            }

            _logger.LogInformation("[Tiingo] Fetching latest data for {Symbol}", symbol);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[Tiingo] Failed to fetch latest for {Symbol}: {StatusCode}",
                    symbol, response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(json) || json == "[]")
            {
                return null;
            }

            if (isCrypto)
            {
                var cryptoData = JsonConvert.DeserializeObject<List<TiingoCryptoLatestData>>(json);
                var latest = cryptoData?.FirstOrDefault();

                if (latest != null)
                {
                    return new MarketData
                    {
                        Symbol = symbol,
                        Timestamp = latest.PriceData?.FirstOrDefault()?.Date ?? DateTime.UtcNow,
                        Open = latest.PriceData?.FirstOrDefault()?.Open ?? 0,
                        High = latest.PriceData?.FirstOrDefault()?.High ?? 0,
                        Low = latest.PriceData?.FirstOrDefault()?.Low ?? 0,
                        Close = latest.PriceData?.FirstOrDefault()?.Close ?? 0,
                        Volume = latest.PriceData?.FirstOrDefault()?.Volume ?? 0,
                        Source = "tiingo"
                    };
                }
            }
            else
            {
                var stockData = JsonConvert.DeserializeObject<List<TiingoIEXLatestData>>(json);
                var latest = stockData?.FirstOrDefault();

                if (latest != null)
                {
                    return new MarketData
                    {
                        Symbol = symbol,
                        Timestamp = latest.Timestamp,
                        Open = latest.Open,
                        High = latest.High,
                        Low = latest.Low,
                        Close = latest.Last,
                        Volume = latest.Volume,
                        Source = "tiingo"
                    };
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Tiingo] Error fetching latest data for {Symbol}", symbol);
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
            var isCrypto = IsCryptoSymbol(symbol);

            string url;
            if (isCrypto)
            {
                url = $"{BaseUrl}/tiingo/crypto?tickers={symbol}";
            }
            else
            {
                url = $"{BaseUrl}/tiingo/daily/{symbol}";
            }

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                return !string.IsNullOrWhiteSpace(json) && json != "[]";
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Tiingo] Error checking symbol support for {Symbol}", symbol);
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
            // Reset hourly counter if hour has passed
            if ((DateTime.UtcNow - _hourStart).TotalHours >= 1)
            {
                _hourlyUsage = 0;
                _hourStart = DateTime.UtcNow;
            }

            // Check hourly limit (1000 calls/hour)
            if (_hourlyUsage >= 1000)
            {
                var waitTime = _hourStart.AddHours(1) - DateTime.UtcNow;
                _logger.LogWarning("[Tiingo] Hourly rate limit reached. Waiting {Seconds}s",
                    waitTime.TotalSeconds);
                await Task.Delay(waitTime, cancellationToken);
                _hourlyUsage = 0;
                _hourStart = DateTime.UtcNow;
            }

            // Check daily limit (50,000 calls/month ≈ 1667/day)
            if (_todayUsage >= DailyRateLimit)
            {
                throw new InvalidOperationException(
                    $"Tiingo daily rate limit exceeded. Used: {_todayUsage}/{DailyRateLimit}");
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
            _hourlyUsage++;

            var today = DateTime.UtcNow.Date;
            _usageTracker.AddOrUpdate(today, 1, (_, count) => count + 1);
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    private static bool IsCryptoSymbol(string symbol)
    {
        // Tiingo crypto symbols are in format: btcusd, ethusd, etc.
        // or with exchange prefix: coinbaseusd, binancebtcusdt
        return symbol.ToLowerInvariant().Contains("usd") ||
               symbol.ToLowerInvariant().Contains("btc") ||
               symbol.ToLowerInvariant().Contains("eth") ||
               symbol.ToLowerInvariant().Contains("usdt");
    }

    private static string ConvertIntervalToTiingoFreq(string interval)
    {
        // Tiingo uses resampleFreq: 1min, 5min, 15min, 30min, 1hour, 4hour, 1day
        return interval.ToLowerInvariant() switch
        {
            "1m" => "1min",
            "5m" => "5min",
            "15m" => "15min",
            "30m" => "30min",
            "1h" => "1hour",
            "4h" => "4hour",
            "1d" => "1day",
            _ => "1day"
        };
    }

    // DTOs for Tiingo API responses
    private class TiingoStockPriceData
    {
        [JsonProperty("date")]
        public DateTime Date { get; set; }

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

        [JsonProperty("adjClose")]
        public decimal AdjClose { get; set; }
    }

    private class TiingoCryptoPriceData
    {
        [JsonProperty("ticker")]
        public string? Ticker { get; set; }

        [JsonProperty("priceData")]
        public List<TiingoCryptoPrice>? PriceData { get; set; }
    }

    private class TiingoCryptoPrice
    {
        [JsonProperty("date")]
        public DateTime Date { get; set; }

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
    }

    private class TiingoCryptoLatestData
    {
        [JsonProperty("ticker")]
        public string? Ticker { get; set; }

        [JsonProperty("priceData")]
        public List<TiingoCryptoPrice>? PriceData { get; set; }
    }

    private class TiingoIEXLatestData
    {
        [JsonProperty("ticker")]
        public string? Ticker { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("open")]
        public decimal Open { get; set; }

        [JsonProperty("high")]
        public decimal High { get; set; }

        [JsonProperty("low")]
        public decimal Low { get; set; }

        [JsonProperty("last")]
        public decimal Last { get; set; }

        [JsonProperty("volume")]
        public decimal Volume { get; set; }
    }
}
