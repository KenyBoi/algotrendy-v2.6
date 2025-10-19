namespace AlgoTrendy.Backtesting.Models;

/// <summary>
/// Configuration for a technical indicator
/// </summary>
public class IndicatorConfig
{
    /// <summary>
    /// Indicator name (e.g., "SMA", "RSI", "MACD")
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Whether this indicator is enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Indicator-specific parameters
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Configuration for running a backtest
/// </summary>
public class BacktestConfig
{
    /// <summary>
    /// AI model to use for decision making
    /// </summary>
    public string AIName { get; set; } = "MemGPT AI v1";

    /// <summary>
    /// Backtesting engine to use
    /// </summary>
    public BacktesterEngine Engine { get; set; } = BacktesterEngine.Custom;

    /// <summary>
    /// Asset class to trade
    /// </summary>
    public required AssetClass AssetClass { get; set; }

    /// <summary>
    /// Trading symbol (e.g., "BTCUSDT", "ES", "AAPL")
    /// </summary>
    public required string Symbol { get; set; }

    /// <summary>
    /// Timeframe type (minute, hour, day, etc.)
    /// </summary>
    public TimeframeType Timeframe { get; set; } = TimeframeType.Day;

    /// <summary>
    /// Timeframe value (for minute/hour bars, e.g., 15 for 15-minute bars)
    /// </summary>
    public int? TimeframeValue { get; set; } = 1;

    /// <summary>
    /// Backtest start date (UTC)
    /// </summary>
    public required DateTime StartDate { get; set; }

    /// <summary>
    /// Backtest end date (UTC)
    /// </summary>
    public required DateTime EndDate { get; set; }

    /// <summary>
    /// Initial trading capital in USD
    /// </summary>
    public decimal InitialCapital { get; set; } = 10000m;

    /// <summary>
    /// Enabled indicators with their configurations
    /// </summary>
    public Dictionary<string, IndicatorConfig> Indicators { get; set; } = new();

    /// <summary>
    /// Commission per trade as decimal (0.001 = 0.1%)
    /// </summary>
    public decimal Commission { get; set; } = 0.001m;

    /// <summary>
    /// Slippage per trade as decimal (0.0005 = 0.05%)
    /// </summary>
    public decimal Slippage { get; set; } = 0.0005m;

    /// <summary>
    /// Validate configuration
    /// </summary>
    public (bool IsValid, string? ErrorMessage) Validate()
    {
        if (string.IsNullOrEmpty(Symbol))
            return (false, "Symbol is required");

        if (InitialCapital <= 0)
            return (false, "Initial capital must be greater than 0");

        if (EndDate <= StartDate)
            return (false, "End date must be after start date");

        if (Commission < 0 || Commission > 0.1m)
            return (false, "Commission must be between 0 and 0.1");

        if (Slippage < 0 || Slippage > 0.1m)
            return (false, "Slippage must be between 0 and 0.1");

        return (true, null);
    }
}

/// <summary>
/// Result of a single trade
/// </summary>
public class TradeResult
{
    /// <summary>
    /// Entry time (UTC)
    /// </summary>
    public required DateTime EntryTime { get; set; }

    /// <summary>
    /// Exit time (UTC), null if position still open
    /// </summary>
    public DateTime? ExitTime { get; set; }

    /// <summary>
    /// Entry price
    /// </summary>
    public required decimal EntryPrice { get; set; }

    /// <summary>
    /// Exit price, null if position still open
    /// </summary>
    public decimal? ExitPrice { get; set; }

    /// <summary>
    /// Trade quantity
    /// </summary>
    public required decimal Quantity { get; set; }

    /// <summary>
    /// Trade direction (long or short)
    /// </summary>
    public required TradeDirection Side { get; set; }

    /// <summary>
    /// Profit/loss in USD, null if position still open
    /// </summary>
    public decimal? PnL { get; set; }

    /// <summary>
    /// Profit/loss percentage, null if position still open
    /// </summary>
    public decimal? PnLPercent { get; set; }

    /// <summary>
    /// Trade duration in minutes, null if position still open
    /// </summary>
    public int? DurationMinutes { get; set; }

    /// <summary>
    /// Reason for exit (e.g., "stop loss", "take profit", "signal")
    /// </summary>
    public string? ExitReason { get; set; }
}

/// <summary>
/// Single point in the equity curve
/// </summary>
public class EquityPoint
{
    /// <summary>
    /// Timestamp (UTC)
    /// </summary>
    public required DateTime Timestamp { get; set; }

    /// <summary>
    /// Total account equity
    /// </summary>
    public required decimal Equity { get; set; }

    /// <summary>
    /// Available cash
    /// </summary>
    public required decimal Cash { get; set; }

    /// <summary>
    /// Value of open positions
    /// </summary>
    public required decimal PositionsValue { get; set; }
}

/// <summary>
/// Performance metrics for backtest
/// </summary>
public class BacktestMetrics
{
    /// <summary>
    /// Total return as percentage
    /// </summary>
    public decimal TotalReturn { get; set; }

    /// <summary>
    /// Annualized return as percentage
    /// </summary>
    public decimal AnnualizedReturn { get; set; }

    /// <summary>
    /// Sharpe ratio (risk-adjusted return)
    /// </summary>
    public decimal SharpeRatio { get; set; }

    /// <summary>
    /// Maximum drawdown as percentage
    /// </summary>
    public decimal MaxDrawdown { get; set; }

    /// <summary>
    /// Win rate percentage (0-100)
    /// </summary>
    public decimal WinRate { get; set; }

    /// <summary>
    /// Profit factor (total profit / total loss)
    /// </summary>
    public decimal ProfitFactor { get; set; }

    /// <summary>
    /// Total number of trades
    /// </summary>
    public int TotalTrades { get; set; }

    /// <summary>
    /// Number of winning trades
    /// </summary>
    public int WinningTrades { get; set; }

    /// <summary>
    /// Number of losing trades
    /// </summary>
    public int LosingTrades { get; set; }

    /// <summary>
    /// Average trade profit/loss in USD
    /// </summary>
    public decimal AverageTrade { get; set; }

    /// <summary>
    /// Average winning trade in USD
    /// </summary>
    public decimal AverageWin { get; set; }

    /// <summary>
    /// Average losing trade in USD
    /// </summary>
    public decimal AverageLoss { get; set; }

    /// <summary>
    /// Average trade duration in hours
    /// </summary>
    public decimal AverageTradeDuration { get; set; }

    /// <summary>
    /// Final portfolio value
    /// </summary>
    public decimal FinalValue { get; set; }

    /// <summary>
    /// Total profit/loss in USD
    /// </summary>
    public decimal TotalPnL { get; set; }
}

/// <summary>
/// Complete backtest results
/// </summary>
public class BacktestResults
{
    /// <summary>
    /// Unique backtest identifier
    /// </summary>
    public required string BacktestId { get; set; }

    /// <summary>
    /// Backtest execution status
    /// </summary>
    public BacktestStatus Status { get; set; } = BacktestStatus.Pending;

    /// <summary>
    /// Configuration used for this backtest
    /// </summary>
    public required BacktestConfig Config { get; set; }

    /// <summary>
    /// When backtest started (UTC)
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When backtest completed (UTC)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Execution time in seconds
    /// </summary>
    public double? ExecutionTimeSeconds { get; set; }

    /// <summary>
    /// Performance metrics
    /// </summary>
    public BacktestMetrics? Metrics { get; set; }

    /// <summary>
    /// Equity curve (account value over time)
    /// </summary>
    public List<EquityPoint> EquityCurve { get; set; } = new();

    /// <summary>
    /// All trades executed during backtest
    /// </summary>
    public List<TradeResult> Trades { get; set; } = new();

    /// <summary>
    /// Indicators that were used
    /// </summary>
    public List<string> IndicatorsUsed { get; set; } = new();

    /// <summary>
    /// Error message if backtest failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error details if backtest failed
    /// </summary>
    public Dictionary<string, object> ErrorDetails { get; set; } = new();

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Summary of a past backtest for history listing
/// </summary>
public class BacktestHistoryItem
{
    /// <summary>
    /// Backtest identifier
    /// </summary>
    public required string BacktestId { get; set; }

    /// <summary>
    /// Trading symbol
    /// </summary>
    public required string Symbol { get; set; }

    /// <summary>
    /// Asset class
    /// </summary>
    public required string AssetClass { get; set; }

    /// <summary>
    /// Timeframe
    /// </summary>
    public required string Timeframe { get; set; }

    /// <summary>
    /// Start date
    /// </summary>
    public required DateTime StartDate { get; set; }

    /// <summary>
    /// End date
    /// </summary>
    public required DateTime EndDate { get; set; }

    /// <summary>
    /// Backtest status
    /// </summary>
    public BacktestStatus Status { get; set; }

    /// <summary>
    /// Total return percentage
    /// </summary>
    public decimal? TotalReturn { get; set; }

    /// <summary>
    /// Sharpe ratio
    /// </summary>
    public decimal? SharpeRatio { get; set; }

    /// <summary>
    /// Total number of trades
    /// </summary>
    public int? TotalTrades { get; set; }

    /// <summary>
    /// When backtest was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Available configuration options for backtesting UI
/// </summary>
public class BacktestConfigOptions
{
    /// <summary>
    /// Available AI models
    /// </summary>
    public List<OptionItem> AIModels { get; set; } = new()
    {
        new OptionItem { Value = "memgpt_v1", Label = "MemGPT AI v1", Status = "available" },
        new OptionItem { Value = "memgpt_v2", Label = "MemGPT AI v2", Status = "coming_soon" }
    };

    /// <summary>
    /// Available backtesting engines
    /// </summary>
    public List<OptionItem> BacktestingEngines { get; set; } = new()
    {
        new OptionItem { Value = "custom", Label = "Custom Engine", Status = "available" },
        new OptionItem { Value = "quantconnect", Label = "QuantConnect", Status = "available" },
        new OptionItem { Value = "backtester", Label = "Backtester.com", Status = "available" }
    };

    /// <summary>
    /// Available asset classes and their symbols
    /// </summary>
    public List<AssetClassOption> AssetClasses { get; set; } = new()
    {
        new AssetClassOption
        {
            Value = "crypto",
            Label = "Cryptocurrency",
            Symbols = new[] { "BTCUSDT", "ETHUSDT", "SOLUSDT", "ADAUSDT", "XRPUSDT", "BNBUSDT", "DOGEUSDT", "MATICUSDT", "LINKUSDT", "AVAXUSDT" }
        },
        new AssetClassOption
        {
            Value = "futures",
            Label = "Futures",
            Symbols = new[] { "ES", "NQ", "YM", "RTY", "CL", "GC", "SI", "ZB", "ZN", "ZF" }
        },
        new AssetClassOption
        {
            Value = "equities",
            Label = "Equities",
            Symbols = new[] { "AAPL", "GOOGL", "MSFT", "AMZN", "TSLA", "NVDA", "META", "NFLX", "AMD", "INTC" }
        }
    };

    /// <summary>
    /// Available timeframe types
    /// </summary>
    public List<TimeframeOption> TimeframeTypes { get; set; } = new()
    {
        new TimeframeOption { Value = "tick", Label = "Tick", NeedsValue = false },
        new TimeframeOption { Value = "min", Label = "Minute", NeedsValue = true, ValueLabel = "Minutes" },
        new TimeframeOption { Value = "hr", Label = "Hour", NeedsValue = true, ValueLabel = "Hours" },
        new TimeframeOption { Value = "day", Label = "Day", NeedsValue = false },
        new TimeframeOption { Value = "wk", Label = "Week", NeedsValue = false },
        new TimeframeOption { Value = "mo", Label = "Month", NeedsValue = false },
        new TimeframeOption { Value = "renko", Label = "Renko", NeedsValue = true, ValueLabel = "Brick Size" },
        new TimeframeOption { Value = "line", Label = "Line Break", NeedsValue = true, ValueLabel = "Lines" },
        new TimeframeOption { Value = "range", Label = "Range", NeedsValue = true, ValueLabel = "Range Size" }
    };
}

/// <summary>
/// Generic option item for UI dropdowns
/// </summary>
public class OptionItem
{
    /// <summary>
    /// Option value
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    /// Display label
    /// </summary>
    public required string Label { get; set; }

    /// <summary>
    /// Availability status
    /// </summary>
    public string Status { get; set; } = "available";
}

/// <summary>
/// Asset class option with available symbols
/// </summary>
public class AssetClassOption : OptionItem
{
    /// <summary>
    /// Available symbols for this asset class
    /// </summary>
    public required string[] Symbols { get; set; }
}

/// <summary>
/// Timeframe option with configuration
/// </summary>
public class TimeframeOption : OptionItem
{
    /// <summary>
    /// Whether this timeframe needs a value (e.g., 15 for 15-minute bars)
    /// </summary>
    public bool NeedsValue { get; set; }

    /// <summary>
    /// Label for the value input
    /// </summary>
    public string? ValueLabel { get; set; }
}
