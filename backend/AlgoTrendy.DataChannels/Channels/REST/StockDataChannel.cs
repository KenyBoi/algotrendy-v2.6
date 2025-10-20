using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using AlgoTrendy.DataChannels.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace AlgoTrendy.DataChannels.Channels.REST;

/// <summary>
/// Stock market data channel using FREE tier providers (Alpha Vantage + yfinance)
/// Fetches real-time and historical stock data for US equities and ETFs
/// Cost: $0/month (saves $24,000-30,000/year vs Bloomberg/Refinitiv)
/// </summary>
public class StockDataChannel : IMarketDataChannel
{
    private readonly AlphaVantageProvider _alphaVantageProvider;
    private readonly YFinanceProvider _yfinanceProvider;
    private readonly IMarketDataRepository _repository;
    private readonly ILogger<StockDataChannel> _logger;
    private readonly IConfiguration _configuration;

    private bool _isConnected = false;
    private List<string> _symbols = new();
    private readonly List<string> _subscribedSymbols = new();

    public string ExchangeName => "stocks";
    public bool IsConnected => _isConnected;
    public IReadOnlyList<string> SubscribedSymbols => _subscribedSymbols.AsReadOnly();
    public DateTime? LastDataReceivedAt { get; private set; }
    public long TotalMessagesReceived { get; private set; }

    public StockDataChannel(
        AlphaVantageProvider alphaVantageProvider,
        YFinanceProvider yfinanceProvider,
        IMarketDataRepository repository,
        ILogger<StockDataChannel> logger,
        IConfiguration configuration)
    {
        _alphaVantageProvider = alphaVantageProvider ?? throw new ArgumentNullException(nameof(alphaVantageProvider));
        _yfinanceProvider = yfinanceProvider ?? throw new ArgumentNullException(nameof(yfinanceProvider));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        LoadSymbols();
    }

    /// <summary>
    /// Load stock symbols from configuration
    /// </summary>
    private void LoadSymbols()
    {
        // Get symbols from configuration or use defaults
        var configSymbols = _configuration.GetSection("StockData:Symbols").Get<List<string>>();

        if (configSymbols != null && configSymbols.Any())
        {
            _symbols = configSymbols;
        }
        else
        {
            // Default symbols from v2.5 reference
            _symbols = new List<string>
            {
                // Tech stocks
                "AAPL",   // Apple
                "GOOGL",  // Alphabet
                "MSFT",   // Microsoft
                "NVDA",   // NVIDIA
                "TSLA",   // Tesla
                "META",   // Meta
                "AMZN",   // Amazon
                "AMD",    // AMD

                // Market ETFs (for trend indicators)
                "SPY",    // S&P 500
                "QQQ",    // Nasdaq 100
                "IWM"     // Russell 2000
            };
        }

        _logger.LogInformation(
            "[StockDataChannel] Configured to track {Count} symbols: {Symbols}",
            _symbols.Count,
            string.Join(", ", _symbols));
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[StockDataChannel] Starting stock data channel...");

            // Test yfinance service connectivity
            var testSymbol = "AAPL";
            var testData = await _yfinanceProvider.FetchLatestAsync(testSymbol, cancellationToken);

            if (testData == null)
            {
                _logger.LogWarning(
                    "[StockDataChannel] yfinance service test failed. Is the service running on port 5001?");
                throw new InvalidOperationException(
                    "yfinance service not accessible. Start with: python yfinance_service.py");
            }

            _isConnected = true;
            _logger.LogInformation(
                "[StockDataChannel] Connected successfully. Test quote: {Symbol} = ${Price}",
                testSymbol, testData.Close);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[StockDataChannel] Failed to start stock data channel");
            _isConnected = false;
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[StockDataChannel] Stopping stock data channel...");
        _isConnected = false;
        _subscribedSymbols.Clear();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Fetch latest stock market data
    /// Note: interval and limit parameters are ignored for stock data (always real-time latest)
    /// </summary>
    public async Task<List<MarketData>> FetchDataAsync(
        IEnumerable<string>? symbols = null,
        string interval = "1m",
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
        {
            _logger.LogWarning("[StockDataChannel] Channel not connected. Call StartAsync first.");
            return new List<MarketData>();
        }

        // Use provided symbols or default to configured symbols
        var symbolsToFetch = symbols?.ToList() ?? _symbols;
        var allData = new List<MarketData>();
        var successCount = 0;
        var failCount = 0;

        _logger.LogInformation("[StockDataChannel] Fetching data for {Count} symbols...", symbolsToFetch.Count);

        foreach (var symbol in symbolsToFetch)
        {
            try
            {
                // Use yfinance for real-time quotes (unlimited, free)
                var latestData = await _yfinanceProvider.FetchLatestAsync(symbol, cancellationToken);

                if (latestData != null)
                {
                    // Set asset type based on symbol
                    latestData.AssetType = DetermineAssetType(symbol);

                    allData.Add(latestData);
                    successCount++;

                    // Update statistics
                    LastDataReceivedAt = DateTime.UtcNow;
                    TotalMessagesReceived++;

                    _logger.LogDebug(
                        "[StockDataChannel] {Symbol}: ${Price} (Volume: {Volume:N0})",
                        symbol, latestData.Close, latestData.Volume);
                }
                else
                {
                    failCount++;
                    _logger.LogWarning("[StockDataChannel] No data returned for {Symbol}", symbol);
                }

                // Small delay to be respectful to free API
                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex)
            {
                failCount++;
                _logger.LogError(ex, "[StockDataChannel] Error fetching data for {Symbol}", symbol);
            }
        }

        _logger.LogInformation(
            "[StockDataChannel] Fetch complete: {Success} success, {Fail} failed",
            successCount, failCount);

        return allData;
    }

    public async Task<IEnumerable<MarketData>> FetchHistoricalDataAsync(
        string symbol,
        DateTime startDate,
        DateTime endDate,
        string interval = "1d",
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[StockDataChannel] Fetching historical data for {Symbol} from {Start:yyyy-MM-dd} to {End:yyyy-MM-dd}",
                symbol, startDate, endDate);

            // Use yfinance for historical data (unlimited, free)
            var historicalData = await _yfinanceProvider.FetchHistoricalAsync(
                symbol, startDate, endDate, interval, cancellationToken);

            // Set asset type for all records
            var assetType = DetermineAssetType(symbol);
            foreach (var data in historicalData)
            {
                data.AssetType = assetType;
            }

            _logger.LogInformation(
                "[StockDataChannel] Fetched {Count} historical bars for {Symbol}",
                historicalData.Count(), symbol);

            return historicalData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[StockDataChannel] Error fetching historical data for {Symbol}", symbol);
            return Enumerable.Empty<MarketData>();
        }
    }

    /// <summary>
    /// Determine asset type from symbol
    /// </summary>
    private AssetType DetermineAssetType(string symbol)
    {
        // Common ETFs
        var etfs = new[] { "SPY", "QQQ", "IWM", "GLD", "SLV", "TLT", "VXX", "UVXY" };

        if (etfs.Contains(symbol.ToUpperInvariant()))
        {
            return AssetType.ETF;
        }

        // Default to stock
        return AssetType.Stock;
    }

    /// <summary>
    /// Get available option expiration dates for a symbol
    /// </summary>
    public async Task<IEnumerable<string>> GetOptionsExpirationsAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _yfinanceProvider.GetOptionsExpirationsAsync(symbol, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[StockDataChannel] Error getting options expirations for {Symbol}", symbol);
            return Enumerable.Empty<string>();
        }
    }

    /// <summary>
    /// Get options chain for a symbol and expiration
    /// </summary>
    public async Task<object?> GetOptionsChainAsync(
        string symbol,
        string expiration,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _yfinanceProvider.GetOptionsChainAsync(symbol, expiration, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[StockDataChannel] Error getting options chain for {Symbol}", symbol);
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
        _logger.LogInformation("[StockDataChannel] Subscribing to {Count} symbols", symbolList.Count);

        _subscribedSymbols.AddRange(symbolList);

        _logger.LogInformation(
            "[StockDataChannel] Subscribed to symbols: {Symbols}",
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
        _logger.LogInformation("[StockDataChannel] Unsubscribing from {Count} symbols", symbolList.Count);

        foreach (var symbol in symbolList)
        {
            _subscribedSymbols.Remove(symbol);
        }

        _logger.LogInformation("[StockDataChannel] Unsubscribed from symbols: {Symbols}", string.Join(", ", symbolList));

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _isConnected = false;
        _subscribedSymbols.Clear();
    }
}
