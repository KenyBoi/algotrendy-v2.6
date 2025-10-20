using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AlgoTrendy.DataChannels.Channels.REST;

/// <summary>
/// OKX REST API market data channel
/// Direct C# port of v2.5 okx.py implementation
/// Fetches OHLCV candlestick data from OKX REST API
/// </summary>
public class OKXRestChannel : RestChannelBase
{
    protected override string BaseUrl => "https://www.okx.com";
    public override string ExchangeName => "okx";

    // Default symbols matching v2.5 (OKX format: BTC-USDT)
    private static readonly string[] DefaultSymbols = new[]
    {
        "BTC-USDT", "ETH-USDT", "SOL-USDT", "ADA-USDT", "XRP-USDT",
        "DOGE-USDT", "DOT-USDT", "MATIC-USDT", "AVAX-USDT", "LINK-USDT"
    };

    // OKX interval mapping
    private static readonly Dictionary<string, string> IntervalMap = new()
    {
        ["1m"] = "1m",
        ["5m"] = "5m",
        ["15m"] = "15m",
        ["1h"] = "1H",
        ["4h"] = "4H",
        ["1d"] = "1D"
    };

    public OKXRestChannel(
        IHttpClientFactory httpClientFactory,
        IMarketDataRepository marketDataRepository,
        ILogger<OKXRestChannel> logger)
        : base(httpClientFactory, marketDataRepository, logger)
    {
    }

    /// <summary>
    /// Test connection to OKX API
    /// </summary>
    protected override async Task<bool> TestConnectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            var response = await client.GetAsync(
                $"{BaseUrl}/api/v5/public/time",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                return false;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var code = doc.RootElement.GetProperty("code").GetString();

            return code == "0";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed");
            return false;
        }
    }

    /// <summary>
    /// Fetch OHLCV candlestick data from OKX
    /// </summary>
    /// <param name="symbols">List of trading pairs (OKX format: BTC-USDT)</param>
    /// <param name="interval">Bar interval (1m, 5m, 15m, 1h, 4h, 1d)</param>
    /// <param name="limit">Number of bars (max 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of market data records</returns>
    public async Task<List<MarketData>> FetchDataAsync(
        IEnumerable<string>? symbols = null,
        string interval = "1m",
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        symbols ??= _subscribedSymbols.Any() ? _subscribedSymbols : DefaultSymbols;
        var allData = new List<MarketData>();

        // Map interval to OKX format
        var barInterval = IntervalMap.GetValueOrDefault(interval, "1m");

        using var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(10);

        foreach (var symbol in symbols)
        {
            try
            {
                var url = $"{BaseUrl}/api/v5/market/candles?instId={symbol}&bar={barInterval}&limit={Math.Min(limit, 100)}";
                var response = await client.GetAsync(url, cancellationToken);

                // Handle rate limit exceeded
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    _logger.LogError("OKX rate limit exceeded for {Symbol}", symbol);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("HTTP {StatusCode} for {Symbol}", response.StatusCode, symbol);
                    continue;
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var code = root.GetProperty("code").GetString();
                if (code != "0")
                {
                    var msg = root.GetProperty("msg").GetString();
                    _logger.LogError("OKX API error for {Symbol}: {Message}", symbol, msg);
                    continue;
                }

                if (!root.TryGetProperty("data", out var dataArray))
                {
                    _logger.LogWarning("No data for {Symbol}", symbol);
                    continue;
                }

                // Transform candle data
                // OKX format: [timestamp, open, high, low, close, volume, volCcy, volCcyQuote, confirm]
                foreach (var candle in dataArray.EnumerateArray())
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
    /// Parse OKX candle array into dictionary
    /// OKX format: [timestamp, open, high, low, close, volume, volCcy, volCcyQuote, confirm]
    /// </summary>
    private Dictionary<string, object> ParseCandleData(JsonElement candle, string symbol)
    {
        var array = candle.EnumerateArray().ToArray();

        return new Dictionary<string, object>
        {
            ["symbol"] = symbol.Replace("-", ""),  // Convert BTC-USDT to BTCUSDT
            ["timestamp"] = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(array[0].GetString()!)).UtcDateTime,
            ["open"] = decimal.Parse(array[1].GetString()!),
            ["high"] = decimal.Parse(array[2].GetString()!),
            ["low"] = decimal.Parse(array[3].GetString()!),
            ["close"] = decimal.Parse(array[4].GetString()!),
            ["volume"] = decimal.Parse(array[5].GetString()!),
            ["quote_volume"] = array.Length > 6 ? decimal.Parse(array[6].GetString()!) : 0m,
            ["confirmed"] = array.Length > 8 && array[8].GetString() == "1"
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
        // Calculate simple VWAP for OKX
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
            QuoteVolume = (decimal)rawData["quote_volume"],
            TradesCount = null,  // OKX doesn't provide trades count
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
