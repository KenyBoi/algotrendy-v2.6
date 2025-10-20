using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AlgoTrendy.DataChannels.Channels.REST;

/// <summary>
/// Kraken REST API market data channel
/// Direct C# port of v2.5 kraken.py implementation
/// Fetches OHLCV data from Kraken REST API
/// </summary>
public class KrakenRestChannel : RestChannelBase
{
    protected override string BaseUrl => "https://api.kraken.com";
    public override string ExchangeName => "kraken";

    // Default symbols matching v2.5 (Kraken format)
    private static readonly string[] DefaultSymbols = new[]
    {
        "XXBTZUSD", "XETHZUSD", "SOLUSD", "ADAUSD", "XXRPZUSD",
        "DOGEUSD", "DOTUSD", "MATICUSD", "AVAXUSD", "LINKUSD"
    };

    // Symbol mapping for standardization (Kraken uses unique pair names)
    private static readonly Dictionary<string, string> SymbolMap = new()
    {
        ["XXBTZUSD"] = "BTCUSD",
        ["XETHZUSD"] = "ETHUSD",
        ["XXRPZUSD"] = "XRPUSD",
        ["SOLUSD"] = "SOLUSD",
        ["ADAUSD"] = "ADAUSD",
        ["DOGEUSD"] = "DOGEUSD",
        ["DOTUSD"] = "DOTUSD",
        ["MATICUSD"] = "MATICUSD",
        ["AVAXUSD"] = "AVAXUSD",
        ["LINKUSD"] = "LINKUSD"
    };

    // Kraken interval mapping (in minutes)
    private static readonly int[] ValidIntervals = { 1, 5, 15, 30, 60, 240, 1440, 10080, 21600 };

    public KrakenRestChannel(
        IHttpClientFactory httpClientFactory,
        IMarketDataRepository marketDataRepository,
        ILogger<KrakenRestChannel> logger)
        : base(httpClientFactory, marketDataRepository, logger)
    {
    }

    /// <summary>
    /// Test connection to Kraken API
    /// </summary>
    protected override async Task<bool> TestConnectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            var response = await client.GetAsync(
                $"{BaseUrl}/0/public/Time",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                return false;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Check for errors - Kraken returns empty error array on success
            if (root.TryGetProperty("error", out var errors))
            {
                return errors.GetArrayLength() == 0;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed");
            return false;
        }
    }

    /// <summary>
    /// Fetch OHLCV data from Kraken
    /// </summary>
    /// <param name="symbols">List of trading pairs (Kraken format: XXBTZUSD)</param>
    /// <param name="interval">Time frame in minutes (1, 5, 15, 30, 60, 240, 1440, 10080, 21600)</param>
    /// <param name="limit">Not used by Kraken (included for interface compatibility)</param>
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

        // Convert interval string to minutes
        var intervalMinutes = ParseInterval(interval);

        // Find closest valid Kraken interval
        var krakenInterval = ValidIntervals
            .OrderBy(x => Math.Abs(x - intervalMinutes))
            .First();

        using var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(10);

        foreach (var symbol in symbols)
        {
            try
            {
                var url = $"{BaseUrl}/0/public/OHLC?pair={symbol}&interval={krakenInterval}";
                var response = await client.GetAsync(url, cancellationToken);

                // Handle rate limit exceeded (HTTP 429)
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    _logger.LogError("Kraken rate limit exceeded for {Symbol}, retry after 60s", symbol);
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

                // Check for API errors
                if (root.TryGetProperty("error", out var errors) && errors.GetArrayLength() > 0)
                {
                    var errorMsg = string.Join(", ", errors.EnumerateArray().Select(e => e.GetString()));
                    _logger.LogError("Kraken API error for {Symbol}: {Error}", symbol, errorMsg);
                    continue;
                }

                if (!root.TryGetProperty("result", out var result))
                {
                    _logger.LogWarning("No result data for {Symbol}", symbol);
                    continue;
                }

                // Kraken returns data under the pair name as a dynamic key
                // Find the actual pair key (Kraken sometimes changes it)
                JsonElement? ohlcData = null;
                foreach (var property in result.EnumerateObject())
                {
                    // Skip the "last" property
                    if (property.Name != "last" && property.Value.ValueKind == JsonValueKind.Array)
                    {
                        ohlcData = property.Value;
                        break;
                    }
                }

                if (ohlcData == null)
                {
                    _logger.LogWarning("No OHLC data for {Symbol}", symbol);
                    continue;
                }

                // Transform OHLC data
                // Kraken format: [time, open, high, low, close, vwap, volume, count]
                foreach (var ohlc in ohlcData.Value.EnumerateArray())
                {
                    var rawData = ParseOhlcData(ohlc, symbol);

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

        _logger.LogInformation("Fetched {Count} OHLC records from {SymbolCount} symbols", allData.Count, symbols.Count());
        return allData;
    }

    /// <summary>
    /// Parse interval string to minutes
    /// </summary>
    private int ParseInterval(string interval)
    {
        return interval.ToLower() switch
        {
            "1m" => 1,
            "5m" => 5,
            "15m" => 15,
            "30m" => 30,
            "1h" => 60,
            "4h" => 240,
            "1d" => 1440,
            "1w" => 10080,
            _ => 1
        };
    }

    /// <summary>
    /// Parse Kraken OHLC array into dictionary
    /// Kraken format: [time, open, high, low, close, vwap, volume, count]
    /// </summary>
    private Dictionary<string, object> ParseOhlcData(JsonElement ohlc, string symbol)
    {
        var array = ohlc.EnumerateArray().ToArray();

        // Get standardized symbol from mapping
        var standardSymbol = SymbolMap.GetValueOrDefault(symbol, symbol);

        return new Dictionary<string, object>
        {
            ["symbol"] = standardSymbol,
            ["timestamp"] = DateTimeOffset.FromUnixTimeSeconds(array[0].GetInt64()).UtcDateTime,
            ["open"] = decimal.Parse(array[1].GetString()!),
            ["high"] = decimal.Parse(array[2].GetString()!),
            ["low"] = decimal.Parse(array[3].GetString()!),
            ["close"] = decimal.Parse(array[4].GetString()!),
            ["vwap"] = decimal.Parse(array[5].GetString()!),  // Kraken provides VWAP directly
            ["volume"] = decimal.Parse(array[6].GetString()!),
            ["trades_count"] = array[7].GetInt64()
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
            QuoteVolume = null,  // Kraken doesn't provide quote volume
            TradesCount = (long)rawData["trades_count"],
            Metadata = JsonSerializer.Serialize(new
            {
                exchange = "kraken",
                vwap = rawData["vwap"]  // Store VWAP in metadata since MarketData doesn't have it
            })
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
