namespace AlgoTrendy.Core.Enums;

/// <summary>
/// Asset class types supported by the trading platform
/// </summary>
public enum AssetType
{
    /// <summary>
    /// Cryptocurrency (e.g., BTC, ETH, SOL)
    /// Traded on: Binance, Bybit, OKX, Coinbase, Kraken
    /// </summary>
    Cryptocurrency = 0,

    /// <summary>
    /// Stock/Equity (e.g., AAPL, GOOGL, MSFT)
    /// Traded on: TradeStation, Interactive Brokers, Alpaca
    /// </summary>
    Stock = 1,

    /// <summary>
    /// Futures contracts (e.g., ES, NQ, CL, BTC perpetuals)
    /// Traded on: NinjaTrader, Interactive Brokers, Bybit (crypto futures)
    /// </summary>
    Futures = 2,

    /// <summary>
    /// Options contracts (calls and puts)
    /// Traded on: Interactive Brokers, TradeStation
    /// </summary>
    Options = 3,

    /// <summary>
    /// Exchange-Traded Funds (e.g., SPY, QQQ, IWM)
    /// Traded on: TradeStation, Interactive Brokers, Alpaca
    /// </summary>
    ETF = 4,

    /// <summary>
    /// Foreign Exchange pairs (e.g., EUR/USD, GBP/USD)
    /// Traded on: Interactive Brokers, OANDA
    /// </summary>
    Forex = 5
}
