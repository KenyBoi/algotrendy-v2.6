using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Logging;

namespace AlgoTrendy.DataChannels.Channels.REST;

/// <summary>
/// Base class for REST API market data channels
/// Provides common functionality for connection management, subscriptions, and logging
/// </summary>
public abstract class RestChannelBase : IMarketDataChannel
{
    protected readonly IHttpClientFactory _httpClientFactory;
    protected readonly IMarketDataRepository _marketDataRepository;
    protected readonly ILogger _logger;
    protected readonly List<string> _subscribedSymbols = new();
    protected bool _isConnected;

    /// <summary>
    /// Base URL for the exchange API (e.g., "https://api.binance.com")
    /// </summary>
    protected abstract string BaseUrl { get; }

    /// <summary>
    /// Name of the exchange (e.g., "binance", "okx")
    /// </summary>
    public abstract string ExchangeName { get; }

    public bool IsConnected => _isConnected;
    public IReadOnlyList<string> SubscribedSymbols => _subscribedSymbols.AsReadOnly();
    public DateTime? LastDataReceivedAt { get; protected set; }
    public long TotalMessagesReceived { get; protected set; }

    protected RestChannelBase(
        IHttpClientFactory httpClientFactory,
        IMarketDataRepository marketDataRepository,
        ILogger logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _marketDataRepository = marketDataRepository ?? throw new ArgumentNullException(nameof(marketDataRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public virtual async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isConnected)
        {
            _logger.LogWarning("{Exchange} channel is already connected", ExchangeName);
            return;
        }

        _logger.LogInformation("Starting {Exchange} REST channel", ExchangeName);

        // Test connection to API
        var connected = await TestConnectionAsync(cancellationToken);
        if (!connected)
        {
            throw new InvalidOperationException($"Failed to connect to {ExchangeName} API");
        }

        _isConnected = true;
        _logger.LogInformation("Connected to {Exchange} REST API: {Url}", ExchangeName, BaseUrl);
    }

    public virtual Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping {Exchange} REST channel", ExchangeName);
        _isConnected = false;
        _subscribedSymbols.Clear();
        return Task.CompletedTask;
    }

    public virtual Task SubscribeAsync(IEnumerable<string> symbols, CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("Channel is not connected");
        }

        var symbolList = symbols.ToList();
        _logger.LogInformation("Subscribing to {Count} symbols on {Exchange}", symbolList.Count, ExchangeName);

        _subscribedSymbols.AddRange(symbolList);

        _logger.LogInformation(
            "Subscribed to symbols: {Symbols}",
            string.Join(", ", symbolList));

        return Task.CompletedTask;
    }

    public virtual Task UnsubscribeAsync(IEnumerable<string> symbols, CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("Channel is not connected");
        }

        var symbolList = symbols.ToList();
        _logger.LogInformation("Unsubscribing from {Count} symbols on {Exchange}", symbolList.Count, ExchangeName);

        foreach (var symbol in symbolList)
        {
            _subscribedSymbols.Remove(symbol);
        }

        _logger.LogInformation(
            "Unsubscribed from symbols: {Symbols}",
            string.Join(", ", symbolList));

        return Task.CompletedTask;
    }

    /// <summary>
    /// Tests the connection to the exchange API
    /// Should be implemented by derived classes to perform exchange-specific health checks
    /// </summary>
    protected abstract Task<bool> TestConnectionAsync(CancellationToken cancellationToken);
}
