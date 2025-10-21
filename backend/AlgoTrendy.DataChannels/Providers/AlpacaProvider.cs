using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AlgoTrendy.DataChannels.Providers;

/// <summary>
/// Alpaca Markets data provider - FREE tier (200 calls/minute)
/// Real-time IEX data for stocks and crypto
/// </summary>
public class AlpacaProvider : IMarketDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AlpacaProvider> _logger;
    private readonly string _apiKey;
    private readonly string _apiSecret;
    private const string BaseUrlStocks = "https://data.alpaca.markets/v2";
    private const string BaseUrlCrypto = "https://data.alpaca.markets/v1beta3/crypto/us";

    // Rate limiting: FREE tier = 200 calls/minute
    private readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private DateTime _lastCall = DateTime.MinValue;
    private readonly TimeSpan _minInterval = TimeSpan.FromMilliseconds(300); // 200/min = 1 every 300ms

    // Track API usage
    private readonly ConcurrentDictionary<DateTime, int> _usageTracker = new();
    private int _todayUsage = 0;
    private int _minuteUsage = 0;
    private DateTime _minuteStart = DateTime.UtcNow;

    public string ProviderName => "Alpaca Markets";
    public bool IsFreeTier => true;
    public int? DailyRateLimit => 288000; // 200/min * 60 min * 24 hours (theoretical max)

    public AlpacaProvider(
        HttpClient httpClient,
        ILogger<AlpacaProvider> logger,
        string apiKey,
        string apiSecret)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _apiKey = !string.IsNullOrWhiteSpace(apiKey)
            ? apiKey
            : throw new ArgumentException("Alpaca API key is required", nameof(apiKey));
        _apiSecret = !string.IsNullOrWhiteSpace(apiSecret)
            ? apiSecret
            : throw new ArgumentException("Alpaca API secret is required", nameof(apiSecret));

        // Set authentication headers
        _httpClient.DefaultRequestHeaders.Add("APCA-API-KEY-ID", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("APCA-API-SECRET-KEY", _apiSecret);
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
            var isCrypto = IsCryptoSymbol(symbol);
            var timeframe = ConvertIntervalToAlpaca(interval);

            string url;
            if (isCrypto)
            {
                // Crypto endpoint
                url = $"{BaseUrlCrypto}/bars?" +
                      $"symbols={symbol.ToUpperInvariant()}&" +
                      $"timeframe={timeframe}&" +
                      $"start={startDate:yyyy-MM-ddTHH:mm:ssZ}&" +
                      $"end={endDate:yyyy-MM-ddTHH:mm:ssZ}";
            }
            else
            {
                // Stock endpoint (IEX feed)
                url = $"{BaseUrlStocks}/stocks/{symbol.ToUpperInvariant()}/bars?" +
                      $"timeframe={timeframe}&" +
                      $"start={startDate:yyyy-MM-ddTHH:mm:ssZ}&" +
                      $"end={endDate:yyyy-MM-ddTHH:mm:ssZ}&" +
                      $"feed=iex&" +
                      $"limit=10000";
            }

            _logger.LogInformation(
                "[Alpaca] Fetching {Symbol} from {Start} to {End} (interval: {Interval}, crypto: {IsCrypto})",
                symbol, startDate, endDate, interval, isCrypto);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("[Alpaca] API error: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                    response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    throw new UnauthorizedAccessException("Invalid Alpaca API credentials");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    throw new InvalidOperationException("Alpaca API rate limit exceeded");
                }

                return Array.Empty<MarketData>();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonConvert.DeserializeObject<AlpacaBarsResponse>(json);

            if (data == null || data.Bars == null || !data.Bars.Any())
            {
                _logger.LogWarning("[Alpaca] No data returned for {Symbol}", symbol);
                return Array.Empty<MarketData>();
            }

            var bars = isCrypto
                ? data.Bars[symbol.ToUpperInvariant()]
                : data.Bars.ContainsKey(symbol.ToUpperInvariant())
                    ? data.Bars[symbol.ToUpperInvariant()]
                    : new List<AlpacaBar>();

            if (bars == null || !bars.Any())
            {
                _logger.LogWarning("[Alpaca] No bars in response for {Symbol}", symbol);
                return Array.Empty<MarketData>();
            }

            var marketDataList = bars.Select(bar => new MarketData
            {
                Symbol = symbol,
                Timestamp = DateTime.Parse(bar.T, null, DateTimeStyles.RoundtripKind),
                Open = bar.O,
                High = bar.H,
                Low = bar.L,
                Close = bar.C,
                Volume = bar.V,
                Source = "alpaca"
            }).ToList();

            _logger.LogInformation("[Alpaca] Fetched {Count} bars for {Symbol}",
                marketDataList.Count, symbol);

            return marketDataList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Alpaca] Error fetching historical data for {Symbol}", symbol);
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
                // Crypto latest quote
                url = $"{BaseUrlCrypto}/latest/bars?symbols={symbol.ToUpperInvariant()}";
            }
            else
            {
                // Stock latest quote (IEX feed)
                url = $"{BaseUrlStocks}/stocks/{symbol.ToUpperInvariant()}/bars/latest?feed=iex";
            }

            _logger.LogInformation("[Alpaca] Fetching latest data for {Symbol} (crypto: {IsCrypto})",
                symbol, isCrypto);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[Alpaca] Failed to fetch latest for {Symbol}: {StatusCode}",
                    symbol, response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (isCrypto)
            {
                var data = JsonConvert.DeserializeObject<AlpacaBarsResponse>(json);
                if (data?.Bars == null || !data.Bars.ContainsKey(symbol.ToUpperInvariant()))
                {
                    return null;
                }

                var bar = data.Bars[symbol.ToUpperInvariant()].FirstOrDefault();
                if (bar == null) return null;

                return new MarketData
                {
                    Symbol = symbol,
                    Timestamp = DateTime.Parse(bar.T, null, DateTimeStyles.RoundtripKind),
                    Open = bar.O,
                    High = bar.H,
                    Low = bar.L,
                    Close = bar.C,
                    Volume = bar.V,                };
            }
            else
            {
                var data = JsonConvert.DeserializeObject<AlpacaLatestBarResponse>(json);
                if (data?.Bar == null) return null;

                return new MarketData
                {
                    Symbol = symbol,
                    Timestamp = DateTime.Parse(data.Bar.T, null, DateTimeStyles.RoundtripKind),
                    Open = data.Bar.O,
                    High = data.Bar.H,
                    Low = data.Bar.L,
                    Close = data.Bar.C,
                    Volume = data.Bar.V,                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Alpaca] Error fetching latest data for {Symbol}", symbol);
            return null;
        }
    }

    public async Task<bool> SupportsSymbolAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        // Alpaca supports most US stocks (IEX) and major crypto
        // For simplicity, we'll assume support and let the API return empty if not supported
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
            // Reset minute counter if minute has passed
            if ((DateTime.UtcNow - _minuteStart).TotalMinutes >= 1)
            {
                _minuteUsage = 0;
                _minuteStart = DateTime.UtcNow;
            }

            // Check per-minute limit (200 calls/minute for free tier)
            if (_minuteUsage >= 200)
            {
                var waitTime = _minuteStart.AddMinutes(1) - DateTime.UtcNow;
                _logger.LogWarning("[Alpaca] Per-minute rate limit reached. Waiting {Seconds}s",
                    waitTime.TotalSeconds);
                await Task.Delay(waitTime, cancellationToken);
                _minuteUsage = 0;
                _minuteStart = DateTime.UtcNow;
            }

            // Enforce minimum interval between calls (300ms safety buffer)
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

    private static bool IsCryptoSymbol(string symbol)
    {
        // Crypto symbols typically contain USD, USDT, BTC, ETH, etc.
        var cryptoSuffixes = new[] { "USD", "USDT", "BTC", "ETH" };
        return cryptoSuffixes.Any(suffix => symbol.ToUpperInvariant().EndsWith(suffix));
    }

    private static string ConvertIntervalToAlpaca(string interval)
    {
        // Alpaca uses: 1Min, 5Min, 15Min, 1Hour, 1Day, 1Week, 1Month
        return interval.ToLowerInvariant() switch
        {
            "1m" => "1Min",
            "5m" => "5Min",
            "15m" => "15Min",
            "30m" => "30Min",
            "1h" => "1Hour",
            "4h" => "4Hour",
            "1d" => "1Day",
            "1w" => "1Week",
            "1M" => "1Month",
            _ => "1Day"
        };
    }

    // DTOs for Alpaca API responses
    private class AlpacaBarsResponse
    {
        [JsonProperty("bars")]
        public Dictionary<string, List<AlpacaBar>>? Bars { get; set; }
    }

    private class AlpacaLatestBarResponse
    {
        [JsonProperty("bar")]
        public AlpacaBar? Bar { get; set; }
    }

    private class AlpacaBar
    {
        [JsonProperty("t")]
        public string T { get; set; } = string.Empty; // Timestamp

        [JsonProperty("o")]
        public decimal O { get; set; } // Open

        [JsonProperty("h")]
        public decimal H { get; set; } // High

        [JsonProperty("l")]
        public decimal L { get; set; } // Low

        [JsonProperty("c")]
        public decimal C { get; set; } // Close

        [JsonProperty("v")]
        public decimal V { get; set; } // Volume

        [JsonProperty("n")]
        public int? N { get; set; } // Number of trades

        [JsonProperty("vw")]
        public decimal? Vw { get; set; } // Volume Weighted Average Price
    }
}
