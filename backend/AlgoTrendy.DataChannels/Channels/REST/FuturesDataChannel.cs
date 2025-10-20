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
public class FuturesDataChannel : IMarketDataChannel
{
    private const string BinanceFuturesUrl = "https://fapi.binance.com";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMarketDataRepository _repository;
    private readonly ILogger<FuturesDataChannel> _logger;
    private readonly IConfiguration _configuration;

    private bool _isConnected = false;
    private List<string> _symbols = new();
    private readonly List<string> _subscribedSymbols = new();

    public string ExchangeName => "futures";
    public bool IsConnected => _isConnected;
    public IReadOnlyList<string> SubscribedSymbols => _subscribedSymbols.AsReadOnly();
    public DateTime? LastDataReceivedAt { get; private set; }
    public long TotalMessagesReceived { get; private set; }

    public FuturesDataChannel(
        IHttpClientFactory httpClientFactory,
        IMarketDataRepository repository,
        ILogger<FuturesDataChannel> logger,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[FuturesDataChannel] Starting futures data channel...");

            // Test Binance Futures API connectivity
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            var response = await client.GetAsync(
                $"{BinanceFuturesUrl}/fapi/v1/ping",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "[FuturesDataChannel] Binance Futures API test failed. Status: {Status}",
                    response.StatusCode);
                throw new InvalidOperationException(
                    $"Binance Futures API not accessible. Status: {response.StatusCode}");
            }

            _isConnected = true;
            _logger.LogInformation(
                "[FuturesDataChannel] Connected successfully to Binance Futures API: {Url}",
                BinanceFuturesUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FuturesDataChannel] Failed to start futures data channel");
            _isConnected = false;
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[FuturesDataChannel] Stopping futures data channel...");
        _isConnected = false;
        _subscribedSymbols.Clear();
        return Task.CompletedTask;
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
                var url = $"{BinanceFuturesUrl}/fapi/v1/klines?symbol={symbol}&interval={interval}&limit={Math.Min(limit, 1000)}";

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
            var url = $"{BinanceFuturesUrl}/fapi/v1/klines?symbol={symbol}&interval={interval}&startTime={startTime}&endTime={endTime}&limit=1500";

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

            var url = $"{BinanceFuturesUrl}/fapi/v1/openInterest?symbol={symbol}";

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

            var url = $"{BinanceFuturesUrl}/fapi/v1/premiumIndex?symbol={symbol}";

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

    public Task SubscribeAsync(IEnumerable<string> symbols, CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("Channel is not connected");
        }

        var symbolList = symbols.ToList();
        _logger.LogInformation("[FuturesDataChannel] Subscribing to {Count} symbols", symbolList.Count);

        _subscribedSymbols.AddRange(symbolList);

        _logger.LogInformation(
            "[FuturesDataChannel] Subscribed to symbols: {Symbols}",
            string.Join(", ", symbolList));

        return Task.CompletedTask;
    }

    public Task UnsubscribeAsync(IEnumerable<string> symbols, CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("Channel is not connected");
        }

        var symbolList = symbols.ToList();
        _logger.LogInformation("[FuturesDataChannel] Unsubscribing from {Count} symbols", symbolList.Count);

        foreach (var symbol in symbolList)
        {
            _subscribedSymbols.Remove(symbol);
        }

        _logger.LogInformation("[FuturesDataChannel] Unsubscribed from symbols: {Symbols}", string.Join(", ", symbolList));

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _isConnected = false;
        _subscribedSymbols.Clear();
    }
}
