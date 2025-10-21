namespace AlgoTrendy.MultiCharts.Models;

/// <summary>
/// MultiCharts platform status
/// </summary>
public class MultiChartsPlatformStatus
{
    public bool IsConnected { get; set; }
    public string Version { get; set; } = string.Empty;
    public DateTime ServerTime { get; set; }
    public int ActiveStrategies { get; set; }
    public Dictionary<string, object> AdditionalInfo { get; set; } = new();
}

/// <summary>
/// Backtest request parameters
/// </summary>
public class BacktestRequest
{
    public string StrategyName { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string Timeframe { get; set; } = "1D";
    public decimal InitialCapital { get; set; } = 10000m;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Backtest result
/// </summary>
public class BacktestResult
{
    public string StrategyName { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    // Performance Metrics
    public decimal NetProfit { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal GrossLoss { get; set; }
    public decimal ProfitFactor { get; set; }
    public decimal SharpeRatio { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal MaxDrawdownPercent { get; set; }

    // Trade Statistics
    public int TotalTrades { get; set; }
    public int WinningTrades { get; set; }
    public int LosingTrades { get; set; }
    public decimal WinRate { get; set; }
    public decimal AverageWin { get; set; }
    public decimal AverageLoss { get; set; }
    public decimal LargestWin { get; set; }
    public decimal LargestLoss { get; set; }

    // Additional Metrics
    public decimal InitialCapital { get; set; }
    public decimal FinalCapital { get; set; }
    public decimal ReturnOnInvestment { get; set; }
    public int TotalDays { get; set; }

    // Trades List
    public List<Trade> Trades { get; set; } = new();

    // Equity Curve
    public List<EquityPoint> EquityCurve { get; set; } = new();

    public DateTime CompletedAt { get; set; }
    public TimeSpan ExecutionTime { get; set; }
}

/// <summary>
/// Trade information
/// </summary>
public class Trade
{
    public DateTime EntryTime { get; set; }
    public DateTime ExitTime { get; set; }
    public string Side { get; set; } = string.Empty; // "Long" or "Short"
    public decimal EntryPrice { get; set; }
    public decimal ExitPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal ProfitLoss { get; set; }
    public decimal ProfitLossPercent { get; set; }
    public string ExitReason { get; set; } = string.Empty;
}

/// <summary>
/// Equity curve point
/// </summary>
public class EquityPoint
{
    public DateTime Time { get; set; }
    public decimal Equity { get; set; }
    public decimal DrawdownPercent { get; set; }
}

/// <summary>
/// Walk-forward optimization request
/// </summary>
public class WalkForwardRequest
{
    public string StrategyName { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string Timeframe { get; set; } = "1D";
    public decimal InitialCapital { get; set; } = 10000m;

    // Walk-forward settings
    public int InSamplePeriodDays { get; set; } = 180;
    public int OutOfSamplePeriodDays { get; set; } = 60;
    public int StepDays { get; set; } = 30;

    // Parameters to optimize
    public Dictionary<string, ParameterRange> ParametersToOptimize { get; set; } = new();
}

/// <summary>
/// Parameter range for optimization
/// </summary>
public class ParameterRange
{
    public decimal Start { get; set; }
    public decimal Stop { get; set; }
    public decimal Step { get; set; }
}

/// <summary>
/// Walk-forward optimization result
/// </summary>
public class WalkForwardResult
{
    public string StrategyName { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public List<WalkForwardWindow> Windows { get; set; } = new();
    public BacktestResult AggregatedResult { get; set; } = new();
    public Dictionary<string, object> BestParameters { get; set; } = new();
    public bool IsRobust { get; set; }
    public decimal RobustnessScore { get; set; }
    public DateTime CompletedAt { get; set; }
}

/// <summary>
/// Walk-forward window
/// </summary>
public class WalkForwardWindow
{
    public int WindowNumber { get; set; }
    public DateTime InSampleStart { get; set; }
    public DateTime InSampleEnd { get; set; }
    public DateTime OutOfSampleStart { get; set; }
    public DateTime OutOfSampleEnd { get; set; }
    public BacktestResult InSampleResult { get; set; } = new();
    public BacktestResult OutOfSampleResult { get; set; } = new();
    public Dictionary<string, object> OptimizedParameters { get; set; } = new();
}

/// <summary>
/// Monte Carlo simulation request
/// </summary>
public class MonteCarloRequest
{
    public string StrategyName { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string Timeframe { get; set; } = "1D";
    public decimal InitialCapital { get; set; } = 10000m;
    public int NumberOfRuns { get; set; } = 1000;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Monte Carlo simulation result
/// </summary>
public class MonteCarloResult
{
    public string StrategyName { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public int NumberOfRuns { get; set; }

    // Statistical Results
    public decimal MeanReturn { get; set; }
    public decimal MedianReturn { get; set; }
    public decimal StdDeviation { get; set; }
    public decimal MaxReturn { get; set; }
    public decimal MinReturn { get; set; }

    // Drawdown Statistics
    public decimal MeanMaxDrawdown { get; set; }
    public decimal MedianMaxDrawdown { get; set; }
    public decimal WorstDrawdown { get; set; }

    // Confidence Intervals (95%)
    public decimal ReturnConfidenceIntervalLower { get; set; }
    public decimal ReturnConfidenceIntervalUpper { get; set; }
    public decimal DrawdownConfidenceIntervalLower { get; set; }
    public decimal DrawdownConfidenceIntervalUpper { get; set; }

    // Probability Metrics
    public decimal ProbabilityOfProfit { get; set; }
    public decimal ProbabilityOfLoss { get; set; }

    // Distribution Data
    public List<MonteCarloRun> Runs { get; set; } = new();
    public List<DistributionBucket> ReturnDistribution { get; set; } = new();
    public List<DistributionBucket> DrawdownDistribution { get; set; } = new();

    public DateTime CompletedAt { get; set; }
}

/// <summary>
/// Single Monte Carlo run
/// </summary>
public class MonteCarloRun
{
    public int RunNumber { get; set; }
    public decimal FinalEquity { get; set; }
    public decimal Return { get; set; }
    public decimal MaxDrawdown { get; set; }
}

/// <summary>
/// Distribution bucket for histogram
/// </summary>
public class DistributionBucket
{
    public decimal RangeStart { get; set; }
    public decimal RangeEnd { get; set; }
    public int Count { get; set; }
    public decimal Frequency { get; set; }
}

/// <summary>
/// Strategy deployment request
/// </summary>
public class StrategyDeploymentRequest
{
    public string StrategyName { get; set; } = string.Empty;
    public string StrategyCode { get; set; } = string.Empty;
    public string Language { get; set; } = "CSharp";
    public Dictionary<string, object> DefaultParameters { get; set; } = new();
    public bool Overwrite { get; set; } = false;
}

/// <summary>
/// Strategy deployment result
/// </summary>
public class StrategyDeploymentResult
{
    public bool Success { get; set; }
    public string StrategyName { get; set; } = string.Empty;
    public string StrategyId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<string> CompilationErrors { get; set; } = new();
    public DateTime DeployedAt { get; set; }
}

/// <summary>
/// Strategy information
/// </summary>
public class StrategyInfo
{
    public string Name { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public List<StrategyParameter> Parameters { get; set; } = new();
    public bool IsActive { get; set; }
}

/// <summary>
/// Strategy parameter
/// </summary>
public class StrategyParameter
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public object DefaultValue { get; set; } = new();
    public object MinValue { get; set; } = new();
    public object MaxValue { get; set; } = new();
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Market scan request
/// </summary>
public class ScanRequest
{
    public string ScanName { get; set; } = string.Empty;
    public List<string> Symbols { get; set; } = new();
    public string ScanFormula { get; set; } = string.Empty;
    public string Timeframe { get; set; } = "1D";
    public DateTime? AsOfDate { get; set; }
}

/// <summary>
/// Scan result
/// </summary>
public class ScanResult
{
    public string ScanName { get; set; } = string.Empty;
    public DateTime ScanTime { get; set; }
    public int TotalSymbolsScanned { get; set; }
    public int MatchingSymbols { get; set; }
    public List<ScanMatch> Matches { get; set; } = new();
}

/// <summary>
/// Scan match
/// </summary>
public class ScanMatch
{
    public string Symbol { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal MatchScore { get; set; }
    public Dictionary<string, object> IndicatorValues { get; set; } = new();
    public string MatchReason { get; set; } = string.Empty;
}

/// <summary>
/// Indicator information
/// </summary>
public class IndicatorInfo
{
    public string Name { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<StrategyParameter> Parameters { get; set; } = new();
}

/// <summary>
/// Data request
/// </summary>
public class DataRequest
{
    public string Symbol { get; set; } = string.Empty;
    public string Timeframe { get; set; } = "1D";
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int MaxBars { get; set; } = 5000;
}

/// <summary>
/// OHLCV data point
/// </summary>
public class OHLCVData
{
    public DateTime Time { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
}
