using AlgoTrendy.Core.Models;

namespace AlgoTrendy.Core.Interfaces;

/// <summary>
/// Service interface for Finnhub API integration
/// </summary>
public interface IFinnhubService
{
    /// <summary>
    /// Gets cryptocurrency candlestick data for a symbol
    /// </summary>
    /// <param name="symbol">Trading symbol (e.g., "BINANCE:BTCUSDT")</param>
    /// <param name="resolution">Candle resolution: 1, 5, 15, 30, 60, D, W, M</param>
    /// <param name="from">Start time (Unix timestamp)</param>
    /// <param name="to">End time (Unix timestamp)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of crypto candles</returns>
    Task<List<CryptoCandle>> GetCryptoCandlesAsync(
        string symbol,
        string resolution,
        long from,
        long to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets list of supported cryptocurrency exchanges
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of crypto exchanges</returns>
    Task<List<CryptoExchange>> GetCryptoExchangesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets list of cryptocurrency symbols for a specific exchange
    /// </summary>
    /// <param name="exchange">Exchange code (e.g., "binance", "coinbase")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of crypto symbols</returns>
    Task<List<CryptoSymbol>> GetCryptoSymbolsAsync(
        string exchange,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest quote for a cryptocurrency symbol
    /// </summary>
    /// <param name="symbol">Trading symbol (e.g., "BINANCE:BTCUSDT")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Latest price quote</returns>
    Task<decimal> GetCryptoQuoteAsync(
        string symbol,
        CancellationToken cancellationToken = default);
}
