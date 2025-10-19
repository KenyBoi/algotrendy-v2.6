using System.Net.Http.Json;
using System.Text.Json;
using AlgoTrendy.Core.Configuration;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CryptoExchangeModel = AlgoTrendy.Core.Models.CryptoExchange;

namespace AlgoTrendy.Infrastructure.Services;

/// <summary>
/// Finnhub API service implementation for cryptocurrency market data
/// </summary>
public class FinnhubService : IFinnhubService
{
    private readonly HttpClient _httpClient;
    private readonly FinnhubSettings _settings;
    private readonly ILogger<FinnhubService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public FinnhubService(
        HttpClient httpClient,
        IOptions<FinnhubSettings> settings,
        ILogger<FinnhubService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (!_settings.IsValid())
        {
            throw new InvalidOperationException(
                "Finnhub settings are not properly configured. Please check appsettings.json or environment variables.");
        }

        // Configure HTTP client
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        _httpClient.DefaultRequestHeaders.Add("X-Finnhub-Token", _settings.ApiKey);

        // JSON serialization options
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        _logger.LogInformation("FinnhubService initialized with base URL: {BaseUrl}", _settings.BaseUrl);
    }

    /// <summary>
    /// Gets cryptocurrency candlestick data for a symbol
    /// </summary>
    public async Task<List<CryptoCandle>> GetCryptoCandlesAsync(
        string symbol,
        string resolution,
        long from,
        long to,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol cannot be null or empty", nameof(symbol));

        if (string.IsNullOrWhiteSpace(resolution))
            throw new ArgumentException("Resolution cannot be null or empty", nameof(resolution));

        try
        {
            var endpoint = $"/crypto/candle?symbol={symbol}&resolution={resolution}&from={from}&to={to}";

            if (_settings.EnableLogging)
            {
                _logger.LogDebug("Fetching crypto candles: {Endpoint}", endpoint);
            }

            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var candleData = JsonSerializer.Deserialize<FinnhubCandleResponse>(content, _jsonOptions);

            if (candleData == null || candleData.Status == "no_data")
            {
                _logger.LogWarning("No candle data returned for {Symbol}", symbol);
                return new List<CryptoCandle>();
            }

            // Transform API response into CryptoCandle objects
            var candles = new List<CryptoCandle>();
            for (int i = 0; i < candleData.Timestamps.Count; i++)
            {
                candles.Add(new CryptoCandle
                {
                    Symbol = symbol,
                    Timestamp = candleData.Timestamps[i],
                    Open = candleData.Opens[i],
                    High = candleData.Highs[i],
                    Low = candleData.Lows[i],
                    Close = candleData.Closes[i],
                    Volume = candleData.Volumes[i],
                    Resolution = resolution
                });
            }

            _logger.LogInformation("Retrieved {Count} candles for {Symbol}", candles.Count, symbol);
            return candles;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching crypto candles for {Symbol}", symbol);
            throw new InvalidOperationException($"Failed to fetch crypto candles for {symbol}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error for {Symbol}", symbol);
            throw new InvalidOperationException($"Failed to parse crypto candles response for {symbol}", ex);
        }
    }

    /// <summary>
    /// Gets list of supported cryptocurrency exchanges
    /// </summary>
    public async Task<List<CryptoExchangeModel>> GetCryptoExchangesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = "/crypto/exchange";

            if (_settings.EnableLogging)
            {
                _logger.LogDebug("Fetching crypto exchanges from Finnhub");
            }

            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            // Use default JSON options for simple string array
            var exchanges = await response.Content.ReadFromJsonAsync<List<string>>(cancellationToken);

            if (exchanges == null || exchanges.Count == 0)
            {
                _logger.LogWarning("No crypto exchanges returned from API");
                return new List<CryptoExchangeModel>();
            }

            // Transform exchange codes into CryptoExchange objects
            var result = exchanges.Select(code => new CryptoExchangeModel
            {
                Code = code.ToLowerInvariant(),
                Name = CapitalizeExchangeName(code)
            }).ToList();

            _logger.LogInformation("Retrieved {Count} crypto exchanges", result.Count);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching crypto exchanges");
            throw new InvalidOperationException("Failed to fetch crypto exchanges", ex);
        }
    }

    /// <summary>
    /// Gets list of cryptocurrency symbols for a specific exchange
    /// </summary>
    public async Task<List<CryptoSymbol>> GetCryptoSymbolsAsync(
        string exchange,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(exchange))
            throw new ArgumentException("Exchange cannot be null or empty", nameof(exchange));

        try
        {
            var endpoint = $"/crypto/symbol?exchange={exchange.ToUpperInvariant()}";

            if (_settings.EnableLogging)
            {
                _logger.LogDebug("Fetching crypto symbols for {Exchange}: {Endpoint}", exchange, endpoint);
            }

            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var symbols = await response.Content.ReadFromJsonAsync<List<CryptoSymbol>>(_jsonOptions, cancellationToken);

            if (symbols == null || symbols.Count == 0)
            {
                _logger.LogWarning("No symbols returned for exchange {Exchange}", exchange);
                return new List<CryptoSymbol>();
            }

            _logger.LogInformation("Retrieved {Count} symbols for {Exchange}", symbols.Count, exchange);
            return symbols;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching symbols for exchange {Exchange}", exchange);
            throw new InvalidOperationException($"Failed to fetch symbols for exchange {exchange}", ex);
        }
    }

    /// <summary>
    /// Gets the latest quote for a cryptocurrency symbol
    /// </summary>
    public async Task<decimal> GetCryptoQuoteAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol cannot be null or empty", nameof(symbol));

        try
        {
            var endpoint = $"/quote?symbol={symbol}";

            if (_settings.EnableLogging)
            {
                _logger.LogDebug("Fetching quote for {Symbol}: {Endpoint}", symbol, endpoint);
            }

            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var quoteData = JsonSerializer.Deserialize<FinnhubQuoteResponse>(content, _jsonOptions);

            if (quoteData == null)
            {
                _logger.LogWarning("No quote data returned for {Symbol}", symbol);
                return 0;
            }

            _logger.LogDebug("Retrieved quote for {Symbol}: {Price}", symbol, quoteData.CurrentPrice);
            return quoteData.CurrentPrice;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching quote for {Symbol}", symbol);
            throw new InvalidOperationException($"Failed to fetch quote for {symbol}", ex);
        }
    }

    /// <summary>
    /// Capitalizes exchange name for display
    /// </summary>
    private static string CapitalizeExchangeName(string exchangeCode)
    {
        return exchangeCode switch
        {
            var c when c.Equals("binance", StringComparison.OrdinalIgnoreCase) => "Binance",
            var c when c.Equals("coinbase", StringComparison.OrdinalIgnoreCase) => "Coinbase",
            var c when c.Equals("kraken", StringComparison.OrdinalIgnoreCase) => "Kraken",
            var c when c.Equals("bitfinex", StringComparison.OrdinalIgnoreCase) => "Bitfinex",
            var c when c.Equals("okex", StringComparison.OrdinalIgnoreCase) => "OKX",
            var c when c.Equals("huobi", StringComparison.OrdinalIgnoreCase) => "Huobi",
            var c when c.Equals("kucoin", StringComparison.OrdinalIgnoreCase) => "KuCoin",
            var c when c.Equals("bybit", StringComparison.OrdinalIgnoreCase) => "Bybit",
            _ => System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(exchangeCode.ToLower())
        };
    }
}

/// <summary>
/// Internal DTO for Finnhub candle API response
/// </summary>
internal class FinnhubCandleResponse
{
    public List<decimal> Closes { get; set; } = new();
    public List<decimal> Highs { get; set; } = new();
    public List<decimal> Lows { get; set; } = new();
    public List<decimal> Opens { get; set; } = new();
    public string Status { get; set; } = string.Empty;
    public List<long> Timestamps { get; set; } = new();
    public List<decimal> Volumes { get; set; } = new();

    // Properties with alternate names (Finnhub uses 'c', 'h', 'l', 'o', 's', 't', 'v')
    public List<decimal> C { get => Closes; set => Closes = value; }
    public List<decimal> H { get => Highs; set => Highs = value; }
    public List<decimal> L { get => Lows; set => Lows = value; }
    public List<decimal> O { get => Opens; set => Opens = value; }
    public string S { get => Status; set => Status = value; }
    public List<long> T { get => Timestamps; set => Timestamps = value; }
    public List<decimal> V { get => Volumes; set => Volumes = value; }
}

/// <summary>
/// Internal DTO for Finnhub quote API response
/// </summary>
internal class FinnhubQuoteResponse
{
    public decimal CurrentPrice { get; set; }
    public decimal Change { get; set; }
    public decimal PercentChange { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Open { get; set; }
    public decimal PreviousClose { get; set; }

    // Properties with alternate names (Finnhub uses 'c', 'd', 'dp', 'h', 'l', 'o', 'pc')
    public decimal C { get => CurrentPrice; set => CurrentPrice = value; }
    public decimal D { get => Change; set => Change = value; }
    public decimal Dp { get => PercentChange; set => PercentChange = value; }
    public decimal H { get => High; set => High = value; }
    public decimal L { get => Low; set => Low = value; }
    public decimal O { get => Open; set => Open = value; }
    public decimal Pc { get => PreviousClose; set => PreviousClose = value; }
}
