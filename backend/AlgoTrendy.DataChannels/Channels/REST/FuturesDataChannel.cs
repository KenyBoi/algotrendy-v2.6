using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace AlgoTrendy.DataChannels.Channels.REST;

/// <summary>
/// Crypto perpetual futures data channel
/// Fetches real-time and historical data for BTC, ETH, SOL and other crypto perpetuals
/// Uses Binance Futures API (free, unlimited for market data)
/// Cost: $0/month
/// </summary>
public class FuturesDataChannel : RestChannelBase
{
    protected override string BaseUrl => "https://fapi.binance.com";
    public override string ExchangeName => "futures";

    private readonly IConfiguration _configuration;
    private List<string> _symbols = new();

    public FuturesDataChannel(
        IHttpClientFactory httpClientFactory,
        IMarketDataRepository marketDataRepository,
        ILogger<FuturesDataChannel> logger,
        IConfiguration configuration)
        : base(httpClientFactory, marketDataRepository, logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        LoadSymbols();
    }

    /// <summary>
    /// Load futures symbols from configuration
    /// </summary>
    private void LoadSymbols()
    {
        // Get symbols from configuration or use defaults
        var configSymbols = _configuration.GetSection("FuturesData:Symbols").Get<List<string>>();

        if (configSymbols != null && configSymbols.Any())
        {
            _symbols = configSymbols;
        }
        else
        {
            // Default crypto perpetual futures from v2.5 reference
            // Binance uses "USDT" suffix for perpetuals instead of "_USD_PERP"
            _symbols = new List<string>
            {
                "BTCUSDT",   // Bitcoin perpetual
                "ETHUSDT",   // Ethereum perpetual
                "SOLUSDT",   // Solana perpetual
                "ADAUSDT",   // Cardano perpetual
                "DOGEUSDT",  // Dogecoin perpetual
                "BNBUSDT",   // Binance Coin perpetual
                "XRPUSDT",   // Ripple perpetual
                "DOTUSDT",   // Polkadot perpetual
                "MATICUSDT", // Polygon perpetual
                "AVAXUSDT"   // Avalanche perpetual
            };
        }

        _logger.LogInformation(
            "[FuturesDataChannel] Configured to track {Count} perpetual futures: {Symbols}",
            _symbols.Count,
            string.Join(", ", _symbols));
    }

    /// <summary>
    /// Test connection to Binance Futures API
    /// </summary>
    protected override async Task<bool> TestConnectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            var response = await client.GetAsync(
                $"{BaseUrl}/fapi/v1/ping",
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FuturesDataChannel] Connection test failed");
            return false;
        }
    }

    /// <summary>
    /// Fetch latest futures market data
    /// </summary>
    public async Task<List<MarketData>> FetchDataAsync(
        IEnumerable<string>? symbols = null,
        string interval = "1m",
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
        {
            _logger.LogWarning("[FuturesDataChannel] Channel not connected. Call StartAsync first.");
            return new List<MarketData>();
        }

        // Use provided symbols or default to configured symbols
        var symbolsToFetch = symbols?.ToList() ?? _symbols;
        var allData = new List<MarketData>();
        var successCount = 0;
        var failCount = 0;

        _logger.LogInformation("[FuturesDataChannel] Fetching data for {Count} futures symbols...", symbolsToFetch.Count);

        using var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(10);

        foreach (var symbol in symbolsToFetch)
        {
            try
            {
                // Fetch klines from Binance Futures API
                // Endpoint: /fapi/v1/klines
                var url = $"{BaseUrl}/fapi/v1/klines?symbol={symbol}&interval={interval}&limit={Math.Min(limit, 1000)}";

                var response = await client.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var klines = JsonSerializer.Deserialize<List<List<JsonElement>>>(json);

                if (klines == null || klines.Count == 0)
                {
                    failCount++;
                    _logger.LogWarning("[FuturesDataChannel] No data returned for {Symbol}", symbol);
                    continue;
                }

                // Parse each kline
                foreach (var kline in klines)
                {
                    try
                    {
                        var marketData = new MarketData
                        {
                            Symbol = symbol,
                            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(kline[0].GetInt64()).UtcDateTime,
                            Open = decimal.Parse(kline[1].GetString()!),
                            High = decimal.Parse(kline[2].GetString()!),
                            Low = decimal.Parse(kline[3].GetString()!),
                            Close = decimal.Parse(kline[4].GetString()!),
                            Volume = decimal.Parse(kline[5].GetString()!),
                            QuoteVolume = decimal.Parse(kline[7].GetString()!),
                            TradesCount = kline[8].GetInt64(),
                            Source = "binance_futures"
                        };

                        // Set asset type to Futures
                        marketData.AssetType = AssetType.Futures;

                        allData.Add(marketData);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[FuturesDataChannel] Error parsing kline for {Symbol}", symbol);
                    }
                }

                successCount++;

                // Update statistics
                LastDataReceivedAt = DateTime.UtcNow;
                TotalMessagesReceived++;

                _logger.LogDebug(
                    "[FuturesDataChannel] {Symbol}: {Count} bars fetched",
                    symbol, klines.Count);

                // Small delay to respect rate limits
                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex)
            {
                failCount++;
                _logger.LogError(ex, "[FuturesDataChannel] Error fetching data for {Symbol}", symbol);
            }
        }

        _logger.LogInformation(
            "[FuturesDataChannel] Fetch complete: {Success} success, {Fail} failed, {Total} bars",
            successCount, failCount, allData.Count);

        return allData;
    }

    /// <summary>
    /// Fetch historical futures data
    /// </summary>
    public async Task<IEnumerable<MarketData>> FetchHistoricalDataAsync(
        string symbol,
        DateTime startDate,
        DateTime endDate,
        string interval = "1m",
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[FuturesDataChannel] Fetching historical data for {Symbol} from {Start:yyyy-MM-dd} to {End:yyyy-MM-dd}",
                symbol, startDate, endDate);

            var allData = new List<MarketData>();
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            var startTime = new DateTimeOffset(startDate).ToUnixTimeMilliseconds();
            var endTime = new DateTimeOffset(endDate).ToUnixTimeMilliseconds();

            // Binance Futures allows max 1500 klines per request
            var url = $"{BaseUrl}/fapi/v1/klines?symbol={symbol}&interval={interval}&startTime={startTime}&endTime={endTime}&limit=1500";

            var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var klines = JsonSerializer.Deserialize<List<List<JsonElement>>>(json);

            if (klines == null || klines.Count == 0)
            {
                _logger.LogWarning("[FuturesDataChannel] No historical data returned for {Symbol}", symbol);
                return Enumerable.Empty<MarketData>();
            }

            // Parse each kline
            foreach (var kline in klines)
            {
                try
                {
                    var marketData = new MarketData
                    {
                        Symbol = symbol,
                        Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(kline[0].GetInt64()).UtcDateTime,
                        Open = decimal.Parse(kline[1].GetString()!),
                        High = decimal.Parse(kline[2].GetString()!),
                        Low = decimal.Parse(kline[3].GetString()!),
                        Close = decimal.Parse(kline[4].GetString()!),
                        Volume = decimal.Parse(kline[5].GetString()!),
                        QuoteVolume = decimal.Parse(kline[7].GetString()!),
                        TradesCount = kline[8].GetInt64(),
                        Source = "binance_futures"
                    };

                    // Set asset type to Futures
                    marketData.AssetType = AssetType.Futures;

                    allData.Add(marketData);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[FuturesDataChannel] Error parsing historical kline for {Symbol}", symbol);
                }
            }

            _logger.LogInformation(
                "[FuturesDataChannel] Fetched {Count} historical bars for {Symbol}",
                allData.Count, symbol);

            return allData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FuturesDataChannel] Error fetching historical data for {Symbol}", symbol);
            return Enumerable.Empty<MarketData>();
        }
    }

    /// <summary>
    /// Get open interest for a futures symbol
    /// </summary>
    public async Task<decimal?> GetOpenInterestAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            var url = $"{BaseUrl}/fapi/v1/openInterest?symbol={symbol}";

            var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            if (data.TryGetProperty("openInterest", out var oiElement))
            {
                return decimal.Parse(oiElement.GetString()!);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FuturesDataChannel] Error getting open interest for {Symbol}", symbol);
            return null;
        }
    }

    /// <summary>
    /// Get funding rate for a perpetual futures symbol
    /// </summary>
    public async Task<decimal?> GetFundingRateAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            var url = $"{BaseUrl}/fapi/v1/premiumIndex?symbol={symbol}";

            var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            if (data.TryGetProperty("lastFundingRate", out var frElement))
            {
                return decimal.Parse(frElement.GetString()!);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FuturesDataChannel] Error getting funding rate for {Symbol}", symbol);
            return null;
        }
    }
}
