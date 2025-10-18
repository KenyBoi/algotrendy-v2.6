namespace AlgoTrendy.Core.Interfaces;

/// <summary>
/// Interface for market data channels that connect to exchanges
/// </summary>
public interface IMarketDataChannel
{
    /// <summary>
    /// Name of the exchange (binance, okx, coinbase, kraken, bybit)
    /// </summary>
    string ExchangeName { get; }

    /// <summary>
    /// Current connection status
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// List of symbols currently subscribed to
    /// </summary>
    IReadOnlyList<string> SubscribedSymbols { get; }

    /// <summary>
    /// Starts the data channel and connects to the exchange
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the data channel and disconnects from the exchange
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to market data for specific symbols
    /// </summary>
    Task SubscribeAsync(IEnumerable<string> symbols, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribes from market data for specific symbols
    /// </summary>
    Task UnsubscribeAsync(IEnumerable<string> symbols, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the last time data was received
    /// </summary>
    DateTime? LastDataReceivedAt { get; }

    /// <summary>
    /// Gets the total number of messages received
    /// </summary>
    long TotalMessagesReceived { get; }
}
