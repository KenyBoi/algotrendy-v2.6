using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AlgoTrendy.DataChannels.Channels.REST;

/// <summary>
/// Coinbase Advanced Trade REST API market data channel
/// Direct C# port of v2.5 coinbase.py implementation
/// Fetches OHLCV candlestick data from Coinbase Exchange API
/// </summary>
public class CoinbaseRestChannel : IMarketDataChannel
{
    private const string BaseUrl = "https://api.exchange.coinbase.com";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMarketDataRepository _marketDataRepository;
    private readonly ILogger<CoinbaseRestChannel> _logger;
    private readonly List<string> _subscribedSymbols = new();
    private bool _isConnected;

    // Default symbols matching v2.5 (Coinbase format: BTC-USD)
    private static readonly string[] DefaultSymbols = new[]
    {
        "BTC-USD", "ETH-USD", "SOL-USD", "ADA-USD", "XRP-USD",
        "DOGE-USD", "DOT-USD", "MATIC-USD", "AVAX-USD", "LINK-USD"
    };

    // Valid granularities in seconds
    private static readonly int[] ValidGranularities = { 60, 300, 900, 3600, 21600, 86400 };

    public string ExchangeName => "coinbase";
    public bool IsConnected => _isConnected;
    public IReadOnlyList<string> SubscribedSymbols => _subscribedSymbols.AsReadOnly();
    public DateTime? LastDataReceivedAt { get; private set; }
    public long TotalMessagesReceived { get; private set; }

    public CoinbaseRestChannel(
        IHttpClientFactory httpClientFactory,
        IMarketDataRepository marketDataRepository,
        ILogger<CoinbaseRestChannel> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _marketDataRepository = marketDataRepository ?? throw new ArgumentNullException(nameof(marketDataRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isConnected)
        {
            _logger.LogWarning("Coinbase channel is already connected");
            return;
        }

        _logger.LogInformation("Starting Coinbase REST channel");

        // Test connection to Coinbase API
        var connected = await TestConnectionAsync(cancellationToken);
        if (!connected)
        {
            throw new InvalidOperationException("Failed to connect to Coinbase API");
        }

        _isConnected = true;
        _logger.LogInformation("Connected to Coinbase REST API: {Url}", BaseUrl);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping Coinbase REST channel");
        _isConnected = false;
        _subscribedSymbols.Clear();
        return Task.CompletedTask;
    }

    public Task SubscribeAsync(IEnumerable<string> symbols, CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("Channel is not connected");
        }

        var symbolList = symbols.ToList();
        _logger.LogInformation("Subscribing to {Count} symbols on Coinbase", symbolList.Count);

        _subscribedSymbols.AddRange(symbolList);

        _logger.LogInformation(
            "Subscribed to symbols: {Symbols}",
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
        _logger.LogInformation("Unsubscribing from {Count} symbols on Coinbase", symbolList.Count);

        foreach (var symbol in symbolList)
        {
            _subscribedSymbols.Remove(symbol);
        }

        _logger.LogInformation("Unsubscribed from symbols: {Symbols}", string.Join(", ", symbolList));

        return Task.CompletedTask;
    }

    /// <summary>
    /// Test connection to Coinbase API
    /// </summary>
    private async Task<bool> TestConnectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            var response = await client.GetAsync(
                $"{BaseUrl}/time",
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed");
            return false;
        }
    }

    /// <summary>
    /// Fetch OHLCV candles from Coinbase
    /// </summary>
    /// <param name="symbols">List of trading pairs (Coinbase format: BTC-USD)</param>
    /// <param name="granularity">Granularity in seconds (60, 300, 900, 3600, 21600, 86400)</param>
    /// <param name="limit">Number of candles (max 300)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of market data records</returns>
    public async Task<List<MarketData>> FetchDataAsync(
        IEnumerable<string>? symbols = null,
        int granularity = 60,
        int limit = 300,
        CancellationToken cancellationToken = default)
    {
        symbols ??= _subscribedSymbols.Any() ? _subscribedSymbols : DefaultSymbols;
        var allData = new List<MarketData>();

        // Ensure granularity is valid (find closest match)
        if (!ValidGranularities.Contains(granularity))
        {
            granularity = ValidGranularities.MinBy(x => Math.Abs(x - granularity));
            _logger.LogInformation("Adjusted granularity to nearest valid value: {Granularity}s", granularity);
        }

        using var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(10);

        foreach (var symbol in symbols)
        {
            try
            {
                // Calculate time range
                var endTime = DateTime.UtcNow;
                var startTime = endTime.AddSeconds(-granularity * limit);

                var url = $"{BaseUrl}/products/{symbol}/candles?granularity={granularity}&start={startTime:O}&end={endTime:O}";
                var response = await client.GetAsync(url, cancellationToken);

                // Handle rate limit exceeded
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    _logger.LogError("Coinbase rate limit exceeded for {Symbol}", symbol);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("HTTP {StatusCode} for {Symbol}", response.StatusCode, symbol);
                    continue;
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var candles = JsonSerializer.Deserialize<JsonElement[]>(json);

                if (candles == null || candles.Length == 0)
                {
                    _logger.LogWarning("No candles data for {Symbol}", symbol);
                    continue;
                }

                // Transform candle data
                // Coinbase format: [timestamp, low, high, open, close, volume]
                foreach (var candle in candles)
                {
                    var rawData = ParseCandleData(candle, symbol);

                    if (ValidateData(rawData))
                    {
                        var marketData = TransformData(rawData);
                        allData.Add(marketData);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error fetching {Symbol}", symbol);
                continue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching {Symbol}", symbol);
                continue;
            }
        }

        TotalMessagesReceived += allData.Count;
        LastDataReceivedAt = DateTime.UtcNow;

        _logger.LogInformation("Fetched {Count} candles from {SymbolCount} symbols", allData.Count, symbols.Count());
        return allData;
    }

    /// <summary>
    /// Parse Coinbase candle array into dictionary
    /// Coinbase format: [timestamp, low, high, open, close, volume]
    /// </summary>
    private Dictionary<string, object> ParseCandleData(JsonElement candle, string symbol)
    {
        var array = candle.EnumerateArray().ToArray();

        return new Dictionary<string, object>
        {
            ["symbol"] = symbol.Replace("-", ""),  // Convert BTC-USD to BTCUSD
            ["timestamp"] = DateTimeOffset.FromUnixTimeSeconds(array[0].GetInt64()).UtcDateTime,
            ["low"] = array[1].GetDecimal(),
            ["high"] = array[2].GetDecimal(),
            ["open"] = array[3].GetDecimal(),
            ["close"] = array[4].GetDecimal(),
            ["volume"] = array[5].GetDecimal()
        };
    }

    /// <summary>
    /// Validate OHLCV data (same logic as v2.5)
    /// </summary>
    private bool ValidateData(Dictionary<string, object> data)
    {
        string[] requiredFields = { "symbol", "timestamp", "open", "high", "low", "close", "volume" };

        // Check required fields
        if (!requiredFields.All(field => data.ContainsKey(field)))
        {
            return false;
        }

        // Validate OHLC relationship
        try
        {
            var open = (decimal)data["open"];
            var high = (decimal)data["high"];
            var low = (decimal)data["low"];
            var close = (decimal)data["close"];
            var volume = (decimal)data["volume"];

            if (!(low <= open && open <= high))
                return false;
            if (!(low <= close && close <= high))
                return false;
            if (high < low)
                return false;
            if (volume < 0)
                return false;
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Transform to standardized MarketData format (matching v2.5 schema)
    /// </summary>
    private MarketData TransformData(Dictionary<string, object> rawData)
    {
        // Calculate simple VWAP for Coinbase (no quote volume provided)
        var vwap = ((decimal)rawData["high"] + (decimal)rawData["low"] + (decimal)rawData["close"]) / 3;

        return new MarketData
        {
            Timestamp = (DateTime)rawData["timestamp"],
            Symbol = (string)rawData["symbol"],
            Source = ExchangeName,
            Open = (decimal)rawData["open"],
            High = (decimal)rawData["high"],
            Low = (decimal)rawData["low"],
            Close = (decimal)rawData["close"],
            Volume = (decimal)rawData["volume"],
            QuoteVolume = null,  // Coinbase doesn't provide quote volume
            TradesCount = null,  // Coinbase doesn't provide trades count
        };
    }

    /// <summary>
    /// Save fetched data to QuestDB
    /// </summary>
    public async Task<int> SaveToDbAsync(CancellationToken cancellationToken = default)
    {
        var data = await FetchDataAsync(cancellationToken: cancellationToken);

        if (!data.Any())
        {
            return 0;
        }

        // Use batch insert for efficiency
        await _marketDataRepository.InsertBatchAsync(data, cancellationToken);

        _logger.LogInformation("Saved {Count} records to QuestDB", data.Count);
        return data.Count;
    }
}
