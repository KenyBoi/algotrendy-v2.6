using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AlgoTrendy.DataChannels.Channels.REST;

/// <summary>
/// Binance REST API market data channel
/// Direct C# port of v2.5 binance.py implementation
/// Fetches OHLCV data from Binance REST API
/// </summary>
public class BinanceRestChannel : RestChannelBase
{
    protected override string BaseUrl => "https://api.binance.com";
    public override string ExchangeName => "binance";

    // Default symbols matching v2.5
    private static readonly string[] DefaultSymbols = new[]
    {
        "BTCUSDT", "ETHUSDT", "BNBUSDT", "SOLUSDT", "ADAUSDT",
        "XRPUSDT", "DOGEUSDT", "DOTUSDT", "MATICUSDT", "AVAXUSDT"
    };

    public BinanceRestChannel(
        IHttpClientFactory httpClientFactory,
        IMarketDataRepository marketDataRepository,
        ILogger<BinanceRestChannel> logger)
        : base(httpClientFactory, marketDataRepository, logger)
    {
    }

    /// <summary>
    /// Test connection to Binance API
    /// </summary>
    protected override async Task<bool> TestConnectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            var response = await client.GetAsync(
                $"{BaseUrl}/api/v3/ping",
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
    /// Fetch OHLCV klines from Binance
    /// </summary>
    /// <param name="symbols">List of trading pairs (default: top 10 crypto)</param>
    /// <param name="interval">Kline interval (1m, 5m, 15m, 1h, 4h, 1d)</param>
    /// <param name="limit">Number of klines to fetch (max 1000)</param>
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

        using var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(10);

        foreach (var symbol in symbols)
        {
            try
            {
                var url = $"{BaseUrl}/api/v3/klines?symbol={symbol}&interval={interval}&limit={Math.Min(limit, 1000)}";
                var response = await client.GetAsync(url, cancellationToken);

                // Check rate limits (Binance limit is 1200/min)
                if (response.Headers.TryGetValues("X-MBX-USED-WEIGHT-1M", out var weightValues))
                {
                    if (int.TryParse(weightValues.First(), out int weight) && weight > 1000)
                    {
                        _logger.LogWarning("High rate limit usage: {Weight}/1200", weight);
                    }
                }

                // Handle rate limit exceeded
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(60);
                    _logger.LogError("Binance rate limit exceeded for {Symbol}, retry after {RetryAfter}s",
                        symbol, retryAfter.TotalSeconds);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("HTTP {StatusCode} for {Symbol}", response.StatusCode, symbol);
                    continue;
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var klines = JsonSerializer.Deserialize<JsonElement[]>(json);

                if (klines == null)
                {
                    _logger.LogWarning("No klines data for {Symbol}", symbol);
                    continue;
                }

                // Transform kline data
                foreach (var kline in klines)
                {
                    var rawData = ParseKlineData(kline, symbol);

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

        _logger.LogInformation("Fetched {Count} klines from {SymbolCount} symbols", allData.Count, symbols.Count());
        return allData;
    }

    /// <summary>
    /// Parse Binance kline array into dictionary
    /// Binance format: [openTime, open, high, low, close, volume, closeTime, quoteVolume, trades, takerBuyBase, takerBuyQuote, ignore]
    /// </summary>
    private Dictionary<string, object> ParseKlineData(JsonElement kline, string symbol)
    {
        var array = kline.EnumerateArray().ToArray();

        return new Dictionary<string, object>
        {
            ["symbol"] = symbol,
            ["timestamp"] = DateTimeOffset.FromUnixTimeMilliseconds(array[0].GetInt64()).UtcDateTime,
            ["open"] = decimal.Parse(array[1].GetString()!),
            ["high"] = decimal.Parse(array[2].GetString()!),
            ["low"] = decimal.Parse(array[3].GetString()!),
            ["close"] = decimal.Parse(array[4].GetString()!),
            ["volume"] = decimal.Parse(array[5].GetString()!),
            ["close_time"] = DateTimeOffset.FromUnixTimeMilliseconds(array[6].GetInt64()).UtcDateTime,
            ["quote_volume"] = decimal.Parse(array[7].GetString()!),
            ["trades_count"] = array[8].GetInt64(),
            ["taker_buy_base_volume"] = decimal.Parse(array[9].GetString()!),
            ["taker_buy_quote_volume"] = decimal.Parse(array[10].GetString()!)
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
        // Calculate VWAP: quote_volume / volume
        decimal vwap;
        try
        {
            var quoteVolume = (decimal)rawData["quote_volume"];
            var volume = (decimal)rawData["volume"];
            vwap = volume > 0 ? quoteVolume / volume : ((decimal)rawData["high"] + (decimal)rawData["low"] + (decimal)rawData["close"]) / 3;
        }
        catch (Exception)
        {
            vwap = ((decimal)rawData["high"] + (decimal)rawData["low"] + (decimal)rawData["close"]) / 3;
        }

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
            TradesCount = (long)rawData["trades_count"],
            // Metadata stored as JSON in v2.5 - we'll add this to MarketData.Metadata if needed
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
