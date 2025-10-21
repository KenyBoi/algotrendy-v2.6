using System.Collections.Concurrent;
using System.Globalization;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AlgoTrendy.DataChannels.Providers;

/// <summary>
/// CoinGecko data provider - FREE tier (10,000 calls/month, 30 calls/min)
/// Comprehensive cryptocurrency data for 10,000+ coins
/// </summary>
public class CoinGeckoProvider : IMarketDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CoinGeckoProvider> _logger;
    private readonly string? _apiKey; // Optional for Demo plan
    private const string BaseUrl = "https://api.coingecko.com/api/v3";
    private const string ProBaseUrl = "https://pro-api.coingecko.com/api/v3";

    // Rate limiting: FREE tier = 30 calls/minute, 10,000 calls/month
    private readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private DateTime _lastCall = DateTime.MinValue;
    private readonly TimeSpan _minInterval = TimeSpan.FromSeconds(2); // 30/min = 1 every 2 seconds

    // Track API usage
    private readonly ConcurrentDictionary<DateTime, int> _usageTracker = new();
    private int _todayUsage = 0;
    private int _minuteUsage = 0;
    private DateTime _minuteStart = DateTime.UtcNow;

    // Coin ID mapping cache (symbol -> coin ID)
    private readonly Dictionary<string, string> _coinIdCache = new();

    public string ProviderName => "CoinGecko";
    public bool IsFreeTier => string.IsNullOrEmpty(_apiKey);
    public int? DailyRateLimit => 333; // 10,000/month ÷ 30 days

    public CoinGeckoProvider(
        HttpClient httpClient,
        ILogger<CoinGeckoProvider> logger,
        string? apiKey = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _apiKey = apiKey;

        // Initialize common coin ID mappings
        InitializeCoinIdCache();
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
            var coinId = await GetCoinIdAsync(symbol, cancellationToken);
            if (string.IsNullOrEmpty(coinId))
            {
                _logger.LogWarning("[CoinGecko] Could not find coin ID for {Symbol}", symbol);
                return Array.Empty<MarketData>();
            }

            var baseUrl = string.IsNullOrEmpty(_apiKey) ? BaseUrl : ProBaseUrl;

            // Market chart range endpoint (historical OHLC)
            var fromTimestamp = ((DateTimeOffset)startDate).ToUnixTimeSeconds();
            var toTimestamp = ((DateTimeOffset)endDate).ToUnixTimeSeconds();

            var url = $"{baseUrl}/coins/{coinId}/market_chart/range?" +
                      $"vs_currency=usd&" +
                      $"from={fromTimestamp}&" +
                      $"to={toTimestamp}";

            if (!string.IsNullOrEmpty(_apiKey))
            {
                url += $"&x_cg_pro_api_key={_apiKey}";
            }

            _logger.LogInformation(
                "[CoinGecko] Fetching {Symbol} ({CoinId}) from {Start} to {End}",
                symbol, coinId, startDate, endDate);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("[CoinGecko] API error: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    throw new InvalidOperationException("CoinGecko API rate limit exceeded");
                }

                return Array.Empty<MarketData>();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonConvert.DeserializeObject<CoinGeckoMarketChartResponse>(json);

            if (data == null || data.Prices == null || !data.Prices.Any())
            {
                _logger.LogWarning("[CoinGecko] No data returned for {Symbol}", symbol);
                return Array.Empty<MarketData>();
            }

            // CoinGecko returns [timestamp, price] arrays
            // We'll create daily candles from the price data
            var marketDataList = new List<MarketData>();

            // Group by day and create OHLC candles
            var dailyGroups = data.Prices
                .Select(p => new
                {
                    Timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)p[0]).UtcDateTime,
                    Price = p[1]
                })
                .GroupBy(x => x.Timestamp.Date)
                .OrderBy(g => g.Key);

            foreach (var group in dailyGroups)
            {
                var prices = group.Select(x => x.Price).ToList();
                var volumes = data.Total_volumes?
                    .Where(v => DateTimeOffset.FromUnixTimeMilliseconds((long)v[0]).UtcDateTime.Date == group.Key)
                    .Select(v => v[1])
                    .FirstOrDefault() ?? 0;

                marketDataList.Add(new MarketData
                {
                    Symbol = symbol,
                    Timestamp = group.Key,
                    Open = prices.First(),
                    High = prices.Max(),
                    Low = prices.Min(),
                    Close = prices.Last(),
                    Volume = (decimal)volumes,
                    Source = "CoinGecko"
                });
            }

            _logger.LogInformation("[CoinGecko] Fetched {Count} candles for {Symbol}",
                marketDataList.Count, symbol);

            return marketDataList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CoinGecko] Error fetching historical data for {Symbol}", symbol);
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
            var coinId = await GetCoinIdAsync(symbol, cancellationToken);
            if (string.IsNullOrEmpty(coinId))
            {
                _logger.LogWarning("[CoinGecko] Could not find coin ID for {Symbol}", symbol);
                return null;
            }

            var baseUrl = string.IsNullOrEmpty(_apiKey) ? BaseUrl : ProBaseUrl;

            // Simple price endpoint
            var url = $"{baseUrl}/simple/price?" +
                      $"ids={coinId}&" +
                      $"vs_currencies=usd&" +
                      $"include_24hr_vol=true&" +
                      $"include_24hr_change=true&" +
                      $"include_last_updated_at=true";

            if (!string.IsNullOrEmpty(_apiKey))
            {
                url += $"&x_cg_pro_api_key={_apiKey}";
            }

            _logger.LogInformation("[CoinGecko] Fetching latest data for {Symbol} ({CoinId})",
                symbol, coinId);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[CoinGecko] Failed to fetch latest for {Symbol}: {StatusCode}",
                    symbol, response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var jObject = JObject.Parse(json);
            var coinData = jObject[coinId];

            if (coinData == null)
            {
                return null;
            }

            var price = coinData["usd"]?.Value<decimal>() ?? 0;
            var volume = coinData["usd_24h_vol"]?.Value<decimal>() ?? 0;
            var change = coinData["usd_24h_change"]?.Value<decimal>() ?? 0;
            var timestamp = coinData["last_updated_at"]?.Value<long>() ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Calculate OHLC from current price and 24h change
            var yesterdayPrice = price / (1 + (change / 100));

            return new MarketData
            {
                Symbol = symbol,
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime,
                Open = yesterdayPrice,
                High = Math.Max(price, yesterdayPrice),
                Low = Math.Min(price, yesterdayPrice),
                Close = price,
                Volume = volume,
                Source = "CoinGecko"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CoinGecko] Error fetching latest data for {Symbol}", symbol);
            return null;
        }
    }

    public async Task<bool> SupportsSymbolAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        var coinId = await GetCoinIdAsync(symbol, cancellationToken);
        return !string.IsNullOrEmpty(coinId);
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

    private async Task<string?> GetCoinIdAsync(string symbol, CancellationToken cancellationToken)
    {
        // Check cache first
        var symbolLower = symbol.ToLowerInvariant().Replace("usd", "").Replace("usdt", "");
        if (_coinIdCache.ContainsKey(symbolLower))
        {
            return _coinIdCache[symbolLower];
        }

        // If not in cache, return null (in production, could query CoinGecko's coins/list endpoint)
        _logger.LogWarning("[CoinGecko] Coin ID for {Symbol} not found in cache", symbol);
        return null;
    }

    private void InitializeCoinIdCache()
    {
        // Common cryptocurrency mappings (symbol -> CoinGecko ID)
        _coinIdCache["btc"] = "bitcoin";
        _coinIdCache["eth"] = "ethereum";
        _coinIdCache["usdt"] = "tether";
        _coinIdCache["bnb"] = "binancecoin";
        _coinIdCache["sol"] = "solana";
        _coinIdCache["xrp"] = "ripple";
        _coinIdCache["ada"] = "cardano";
        _coinIdCache["doge"] = "dogecoin";
        _coinIdCache["dot"] = "polkadot";
        _coinIdCache["matic"] = "matic-network";
        _coinIdCache["avax"] = "avalanche-2";
        _coinIdCache["link"] = "chainlink";
        _coinIdCache["ltc"] = "litecoin";
        _coinIdCache["bch"] = "bitcoin-cash";
        _coinIdCache["algo"] = "algorand";
        _coinIdCache["atom"] = "cosmos";
        _coinIdCache["uni"] = "uniswap";
        _coinIdCache["xlm"] = "stellar";
        _coinIdCache["etc"] = "ethereum-classic";
        _coinIdCache["near"] = "near";
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

            // Check per-minute limit (30 calls/minute for free tier)
            if (_minuteUsage >= 30)
            {
                var waitTime = _minuteStart.AddMinutes(1) - DateTime.UtcNow;
                _logger.LogWarning("[CoinGecko] Per-minute rate limit reached. Waiting {Seconds}s",
                    waitTime.TotalSeconds);
                await Task.Delay(waitTime, cancellationToken);
                _minuteUsage = 0;
                _minuteStart = DateTime.UtcNow;
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
            _minuteUsage++;

            var today = DateTime.UtcNow.Date;
            _usageTracker.AddOrUpdate(today, 1, (_, count) => count + 1);
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    // DTOs for CoinGecko API responses
    private class CoinGeckoMarketChartResponse
    {
        [JsonProperty("prices")]
        public List<List<decimal>>? Prices { get; set; }

        [JsonProperty("market_caps")]
        public List<List<decimal>>? Market_caps { get; set; }

        [JsonProperty("total_volumes")]
        public List<List<decimal>>? Total_volumes { get; set; }
    }
}
