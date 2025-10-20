using System.Text.Json.Serialization;

namespace AlgoTrendy.Backtesting.Models.QuantConnect;

/// <summary>
/// Base response from QuantConnect API
/// </summary>
public class QCBaseResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// QuantConnect project response
/// </summary>
public class QCProjectResponse : QCBaseResponse
{
    [JsonPropertyName("projects")]
    public List<QCProject> Projects { get; set; } = new();
}

/// <summary>
/// QuantConnect project details
/// </summary>
public class QCProject
{
    [JsonPropertyName("projectId")]
    public int ProjectId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("created")]
    public DateTime Created { get; set; }

    [JsonPropertyName("modified")]
    public DateTime Modified { get; set; }

    [JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;
}

/// <summary>
/// QuantConnect compile response
/// </summary>
public class QCCompileResponse : QCBaseResponse
{
    [JsonPropertyName("compileId")]
    public string CompileId { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("logs")]
    public List<string> Logs { get; set; } = new();
}

/// <summary>
/// QuantConnect backtest response
/// </summary>
public class QCBacktestResponse : QCBaseResponse
{
    [JsonPropertyName("backtest")]
    public QCBacktest? Backtest { get; set; }

    [JsonPropertyName("debugging")]
    public bool Debugging { get; set; }
}

/// <summary>
/// QuantConnect backtest details
/// </summary>
public class QCBacktest
{
    [JsonPropertyName("backtestId")]
    public string BacktestId { get; set; } = string.Empty;

    [JsonPropertyName("projectId")]
    public int ProjectId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("created")]
    public DateTime Created { get; set; }

    [JsonPropertyName("completed")]
    public bool Completed { get; set; }

    [JsonPropertyName("progress")]
    public decimal Progress { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("stacktrace")]
    public string? Stacktrace { get; set; }

    [JsonPropertyName("statistics")]
    public QCStatistics? Statistics { get; set; }

    [JsonPropertyName("charts")]
    public Dictionary<string, QCChart> Charts { get; set; } = new();

    [JsonPropertyName("runtimeStatistics")]
    public Dictionary<string, string> RuntimeStatistics { get; set; } = new();

    [JsonPropertyName("totalPerformance")]
    public QCAlgorithmPerformance? TotalPerformance { get; set; }
}

/// <summary>
/// QuantConnect backtest statistics
/// </summary>
public class QCStatistics
{
    [JsonPropertyName("Total Trades")]
    public string? TotalTrades { get; set; }

    [JsonPropertyName("Average Win")]
    public string? AverageWin { get; set; }

    [JsonPropertyName("Average Loss")]
    public string? AverageLoss { get; set; }

    [JsonPropertyName("Compounding Annual Return")]
    public string? CompoundingAnnualReturn { get; set; }

    [JsonPropertyName("Drawdown")]
    public string? Drawdown { get; set; }

    [JsonPropertyName("Expectancy")]
    public string? Expectancy { get; set; }

    [JsonPropertyName("Net Profit")]
    public string? NetProfit { get; set; }

    [JsonPropertyName("Sharpe Ratio")]
    public string? SharpeRatio { get; set; }

    [JsonPropertyName("Probabilistic Sharpe Ratio")]
    public string? ProbabilisticSharpeRatio { get; set; }

    [JsonPropertyName("Loss Rate")]
    public string? LossRate { get; set; }

    [JsonPropertyName("Win Rate")]
    public string? WinRate { get; set; }

    [JsonPropertyName("Profit-Loss Ratio")]
    public string? ProfitLossRatio { get; set; }

    [JsonPropertyName("Alpha")]
    public string? Alpha { get; set; }

    [JsonPropertyName("Beta")]
    public string? Beta { get; set; }

    [JsonPropertyName("Annual Standard Deviation")]
    public string? AnnualStandardDeviation { get; set; }

    [JsonPropertyName("Annual Variance")]
    public string? AnnualVariance { get; set; }

    [JsonPropertyName("Information Ratio")]
    public string? InformationRatio { get; set; }

    [JsonPropertyName("Tracking Error")]
    public string? TrackingError { get; set; }

    [JsonPropertyName("Treynor Ratio")]
    public string? TreynorRatio { get; set; }

    [JsonPropertyName("Total Fees")]
    public string? TotalFees { get; set; }

    [JsonPropertyName("Estimated Strategy Capacity")]
    public string? EstimatedStrategyCapacity { get; set; }

    [JsonPropertyName("Lowest Capacity Asset")]
    public string? LowestCapacityAsset { get; set; }

    [JsonPropertyName("Portfolio Turnover")]
    public string? PortfolioTurnover { get; set; }

    [JsonPropertyName("OrderListHash")]
    public string? OrderListHash { get; set; }
}

/// <summary>
/// QuantConnect algorithm performance
/// </summary>
public class QCAlgorithmPerformance
{
    [JsonPropertyName("TradeStatistics")]
    public QCTradeStatistics? TradeStatistics { get; set; }

    [JsonPropertyName("PortfolioStatistics")]
    public QCPortfolioStatistics? PortfolioStatistics { get; set; }

    [JsonPropertyName("ClosedTrades")]
    public List<QCTrade> ClosedTrades { get; set; } = new();
}

/// <summary>
/// Trade statistics from QuantConnect
/// </summary>
public class QCTradeStatistics
{
    [JsonPropertyName("StartDateTime")]
    public DateTime StartDateTime { get; set; }

    [JsonPropertyName("EndDateTime")]
    public DateTime EndDateTime { get; set; }

    [JsonPropertyName("TotalNumberOfTrades")]
    public int TotalNumberOfTrades { get; set; }

    [JsonPropertyName("NumberOfWinningTrades")]
    public int NumberOfWinningTrades { get; set; }

    [JsonPropertyName("NumberOfLosingTrades")]
    public int NumberOfLosingTrades { get; set; }

    [JsonPropertyName("TotalProfitLoss")]
    public decimal TotalProfitLoss { get; set; }

    [JsonPropertyName("TotalProfit")]
    public decimal TotalProfit { get; set; }

    [JsonPropertyName("TotalLoss")]
    public decimal TotalLoss { get; set; }

    [JsonPropertyName("LargestProfit")]
    public decimal LargestProfit { get; set; }

    [JsonPropertyName("LargestLoss")]
    public decimal LargestLoss { get; set; }

    [JsonPropertyName("AverageProfitLoss")]
    public decimal AverageProfitLoss { get; set; }

    [JsonPropertyName("AverageProfit")]
    public decimal AverageProfit { get; set; }

    [JsonPropertyName("AverageLoss")]
    public decimal AverageLoss { get; set; }

    [JsonPropertyName("AverageTradeDuration")]
    public TimeSpan AverageTradeDuration { get; set; }

    [JsonPropertyName("AverageWinningTradeDuration")]
    public TimeSpan AverageWinningTradeDuration { get; set; }

    [JsonPropertyName("AverageLosingTradeDuration")]
    public TimeSpan AverageLosingTradeDuration { get; set; }

    [JsonPropertyName("MedianTradeDuration")]
    public TimeSpan MedianTradeDuration { get; set; }

    [JsonPropertyName("MedianWinningTradeDuration")]
    public TimeSpan MedianWinningTradeDuration { get; set; }

    [JsonPropertyName("MedianLosingTradeDuration")]
    public TimeSpan MedianLosingTradeDuration { get; set; }

    [JsonPropertyName("MaxConsecutiveWinningTrades")]
    public int MaxConsecutiveWinningTrades { get; set; }

    [JsonPropertyName("MaxConsecutiveLosingTrades")]
    public int MaxConsecutiveLosingTrades { get; set; }

    [JsonPropertyName("ProfitLossRatio")]
    public decimal ProfitLossRatio { get; set; }

    [JsonPropertyName("WinLossRatio")]
    public decimal WinLossRatio { get; set; }

    [JsonPropertyName("WinRate")]
    public decimal WinRate { get; set; }

    [JsonPropertyName("LossRate")]
    public decimal LossRate { get; set; }

    [JsonPropertyName("AverageMAE")]
    public decimal AverageMAE { get; set; }

    [JsonPropertyName("AverageMFE")]
    public decimal AverageMFE { get; set; }
}

/// <summary>
/// Portfolio statistics from QuantConnect
/// </summary>
public class QCPortfolioStatistics
{
    [JsonPropertyName("AverageWinRate")]
    public decimal AverageWinRate { get; set; }

    [JsonPropertyName("AverageLossRate")]
    public decimal AverageLossRate { get; set; }

    [JsonPropertyName("ProfitLossRatio")]
    public decimal ProfitLossRatio { get; set; }

    [JsonPropertyName("WinRate")]
    public decimal WinRate { get; set; }

    [JsonPropertyName("LossRate")]
    public decimal LossRate { get; set; }

    [JsonPropertyName("Expectancy")]
    public decimal Expectancy { get; set; }

    [JsonPropertyName("CompoundingAnnualReturn")]
    public decimal CompoundingAnnualReturn { get; set; }

    [JsonPropertyName("Drawdown")]
    public decimal Drawdown { get; set; }

    [JsonPropertyName("TotalNetProfit")]
    public decimal TotalNetProfit { get; set; }

    [JsonPropertyName("SharpeRatio")]
    public decimal SharpeRatio { get; set; }

    [JsonPropertyName("ProbabilisticSharpeRatio")]
    public decimal ProbabilisticSharpeRatio { get; set; }

    [JsonPropertyName("Alpha")]
    public decimal Alpha { get; set; }

    [JsonPropertyName("Beta")]
    public decimal Beta { get; set; }

    [JsonPropertyName("AnnualStandardDeviation")]
    public decimal AnnualStandardDeviation { get; set; }

    [JsonPropertyName("AnnualVariance")]
    public decimal AnnualVariance { get; set; }

    [JsonPropertyName("InformationRatio")]
    public decimal InformationRatio { get; set; }

    [JsonPropertyName("TrackingError")]
    public decimal TrackingError { get; set; }

    [JsonPropertyName("TreynorRatio")]
    public decimal TreynorRatio { get; set; }
}

/// <summary>
/// Individual trade from QuantConnect
/// </summary>
public class QCTrade
{
    [JsonPropertyName("Symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("EntryTime")]
    public DateTime EntryTime { get; set; }

    [JsonPropertyName("EntryPrice")]
    public decimal EntryPrice { get; set; }

    [JsonPropertyName("Direction")]
    public string Direction { get; set; } = string.Empty;

    [JsonPropertyName("Quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("ExitTime")]
    public DateTime ExitTime { get; set; }

    [JsonPropertyName("ExitPrice")]
    public decimal ExitPrice { get; set; }

    [JsonPropertyName("ProfitLoss")]
    public decimal ProfitLoss { get; set; }

    [JsonPropertyName("TotalFees")]
    public decimal TotalFees { get; set; }

    [JsonPropertyName("MAE")]
    public decimal MAE { get; set; }

    [JsonPropertyName("MFE")]
    public decimal MFE { get; set; }

    [JsonPropertyName("Duration")]
    public TimeSpan Duration { get; set; }

    [JsonPropertyName("EndTradeDrawdown")]
    public decimal EndTradeDrawdown { get; set; }
}

/// <summary>
/// Chart data from QuantConnect
/// </summary>
public class QCChart
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Series")]
    public Dictionary<string, QCChartSeries> Series { get; set; } = new();
}

/// <summary>
/// Chart series data
/// </summary>
public class QCChartSeries
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Unit")]
    public string Unit { get; set; } = string.Empty;

    [JsonPropertyName("Values")]
    public List<QCChartPoint> Values { get; set; } = new();
}

/// <summary>
/// Chart point data
/// </summary>
public class QCChartPoint
{
    [JsonPropertyName("x")]
    public long X { get; set; }

    [JsonPropertyName("y")]
    public decimal Y { get; set; }
}

/// <summary>
/// List of backtest summaries
/// </summary>
public class QCBacktestListResponse : QCBaseResponse
{
    [JsonPropertyName("backtests")]
    public List<QCBacktestSummary> Backtests { get; set; } = new();

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

/// <summary>
/// Backtest summary
/// </summary>
public class QCBacktestSummary
{
    [JsonPropertyName("backtestId")]
    public string BacktestId { get; set; } = string.Empty;

    [JsonPropertyName("projectId")]
    public int ProjectId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("created")]
    public DateTime Created { get; set; }

    [JsonPropertyName("progress")]
    public decimal Progress { get; set; }

    [JsonPropertyName("sharpeRatio")]
    public decimal? SharpeRatio { get; set; }

    [JsonPropertyName("compoundingAnnualReturn")]
    public decimal? CompoundingAnnualReturn { get; set; }

    [JsonPropertyName("drawdown")]
    public decimal? Drawdown { get; set; }

    [JsonPropertyName("trades")]
    public int? Trades { get; set; }

    [JsonPropertyName("winRate")]
    public decimal? WinRate { get; set; }

    [JsonPropertyName("netProfit")]
    public decimal? NetProfit { get; set; }
}
