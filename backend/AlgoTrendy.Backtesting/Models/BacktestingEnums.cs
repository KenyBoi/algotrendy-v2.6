namespace AlgoTrendy.Backtesting.Models;

/// <summary>
/// Backtest execution status
/// </summary>
public enum BacktestStatus
{
    /// <summary>Pending execution</summary>
    Pending,

    /// <summary>Currently running</summary>
    Running,

    /// <summary>Successfully completed</summary>
    Completed,

    /// <summary>Failed with error</summary>
    Failed
}

/// <summary>
/// Supported asset classes for backtesting
/// </summary>
public enum AssetClass
{
    /// <summary>Cryptocurrency assets (e.g., BTCUSDT, ETHUSDT)</summary>
    Crypto,

    /// <summary>Futures contracts (e.g., ES, NQ, YM)</summary>
    Futures,

    /// <summary>Equity stocks (e.g., AAPL, GOOGL, MSFT)</summary>
    Equities
}

/// <summary>
/// Supported timeframe types for backtesting
/// </summary>
public enum TimeframeType
{
    /// <summary>Tick-by-tick data</summary>
    Tick,

    /// <summary>Minute bars</summary>
    Minute,

    /// <summary>Hourly bars</summary>
    Hour,

    /// <summary>Daily bars</summary>
    Day,

    /// <summary>Weekly bars</summary>
    Week,

    /// <summary>Monthly bars</summary>
    Month,

    /// <summary>Renko charts</summary>
    Renko,

    /// <summary>Line break charts</summary>
    LineBreak,

    /// <summary>Range bars</summary>
    Range
}

/// <summary>
/// Supported backtesting engines
/// </summary>
public enum BacktesterEngine
{
    /// <summary>QuantConnect cloud-based engine</summary>
    QuantConnect,

    /// <summary>Backtester.com cloud-based engine (deprecated - use BacktestingPy)</summary>
    Backtester,

    /// <summary>Custom built-in engine</summary>
    Custom,

    /// <summary>Backtesting.py Python library via microservice</summary>
    BacktestingPy
}

/// <summary>
/// Indicator types for signal generation
/// </summary>
public enum IndicatorType
{
    /// <summary>Simple Moving Average</summary>
    SMA,

    /// <summary>Exponential Moving Average</summary>
    EMA,

    /// <summary>Relative Strength Index</summary>
    RSI,

    /// <summary>MACD (Moving Average Convergence Divergence)</summary>
    MACD,

    /// <summary>Bollinger Bands</summary>
    BollingerBands,

    /// <summary>Average True Range</summary>
    ATR,

    /// <summary>Stochastic Oscillator</summary>
    Stochastic,

    /// <summary>On-Balance Volume</summary>
    OBV
}

/// <summary>
/// Trade direction/side
/// </summary>
public enum TradeDirection
{
    /// <summary>Long (buy) position</summary>
    Long,

    /// <summary>Short (sell) position</summary>
    Short
}
