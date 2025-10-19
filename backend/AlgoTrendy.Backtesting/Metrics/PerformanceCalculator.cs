using AlgoTrendy.Backtesting.Models;

namespace AlgoTrendy.Backtesting.Metrics;

/// <summary>
/// Calculates comprehensive performance metrics for backtest results
/// </summary>
public static class PerformanceCalculator
{
    /// <summary>
    /// Calculate all performance metrics from trades and equity curve
    /// </summary>
    /// <param name="trades">List of completed trades</param>
    /// <param name="equityCurve">Equity curve over time</param>
    /// <param name="initialCapital">Starting capital</param>
    /// <returns>Complete performance metrics</returns>
    public static BacktestMetrics Calculate(
        List<TradeResult> trades,
        List<EquityPoint> equityCurve,
        decimal initialCapital)
    {
        if (!trades.Any() || !equityCurve.Any())
        {
            return new BacktestMetrics
            {
                TotalReturn = 0,
                AnnualReturn = 0,
                SharpeRatio = 0,
                SortinoRatio = 0,
                MaxDrawdown = 0,
                WinRate = 0,
                ProfitFactor = 0,
                TotalTrades = 0,
                WinningTrades = 0,
                LosingTrades = 0,
                AvgWin = 0,
                AvgLoss = 0,
                LargestWin = 0,
                LargestLoss = 0,
                AvgTradeDurationHours = 0
            };
        }

        var winningTrades = trades.Where(t => (t.PnL ?? 0) > 0).ToList();
        var losingTrades = trades.Where(t => (t.PnL ?? 0) <= 0).ToList();

        // 1. Total Return
        var finalEquity = equityCurve.Last().Equity;
        var totalReturn = initialCapital > 0
            ? ((finalEquity - initialCapital) / initialCapital) * 100
            : 0;

        // 2. Annual Return
        var days = (equityCurve.Last().Timestamp - equityCurve.First().Timestamp).Days;
        var annualReturn = days > 0 ? (totalReturn / days * 365) : 0;

        // 3. Sharpe Ratio
        var sharpe = CalculateSharpeRatio(equityCurve);

        // 4. Sortino Ratio
        var sortino = CalculateSortinoRatio(equityCurve);

        // 5. Max Drawdown
        var maxDrawdown = equityCurve.Any() ? equityCurve.Min(e => e.Drawdown) : 0;

        // 6. Win Rate
        var winRate = trades.Count > 0
            ? (decimal)winningTrades.Count / trades.Count * 100
            : 0;

        // 7. Profit Factor
        var grossProfit = winningTrades.Sum(t => t.PnL ?? 0);
        var grossLoss = Math.Abs(losingTrades.Sum(t => t.PnL ?? 0));
        var profitFactor = grossLoss > 0 ? grossProfit / grossLoss : 0;

        // 8-14. Trade statistics
        return new BacktestMetrics
        {
            TotalReturn = Math.Round(totalReturn, 2),
            AnnualReturn = Math.Round(annualReturn, 2),
            SharpeRatio = Math.Round(sharpe, 2),
            SortinoRatio = Math.Round(sortino, 2),
            MaxDrawdown = Math.Round(maxDrawdown, 2),
            WinRate = Math.Round(winRate, 2),
            ProfitFactor = Math.Round(profitFactor, 2),
            TotalTrades = trades.Count,
            WinningTrades = winningTrades.Count,
            LosingTrades = losingTrades.Count,
            AvgWin = winningTrades.Any()
                ? Math.Round(winningTrades.Average(t => t.PnL ?? 0), 2)
                : 0,
            AvgLoss = losingTrades.Any()
                ? Math.Round(losingTrades.Average(t => Math.Abs(t.PnL ?? 0)), 2)
                : 0,
            LargestWin = winningTrades.Any()
                ? Math.Round(winningTrades.Max(t => t.PnL ?? 0), 2)
                : 0,
            LargestLoss = losingTrades.Any()
                ? Math.Round(losingTrades.Min(t => t.PnL ?? 0), 2)
                : 0,
            AvgTradeDurationHours = trades.Any()
                ? Math.Round((decimal)trades.Average(t => t.DurationMinutes ?? 0) / 60.0m, 2)
                : 0
        };
    }

    /// <summary>
    /// Calculate Sharpe Ratio (risk-adjusted return)
    /// Sharpe = (Mean Return / Std Dev of Returns) × √252
    /// </summary>
    private static decimal CalculateSharpeRatio(List<EquityPoint> equityCurve)
    {
        if (equityCurve.Count < 2) return 0;

        var returns = new List<decimal>();
        for (int i = 1; i < equityCurve.Count; i++)
        {
            var prevEquity = equityCurve[i - 1].Equity;
            if (prevEquity == 0) continue;

            var ret = (equityCurve[i].Equity / prevEquity) - 1;
            returns.Add(ret);
        }

        if (!returns.Any()) return 0;

        var mean = returns.Average();
        var stdDev = CalculateStandardDeviation(returns);

        if (stdDev == 0) return 0;

        // Annualized Sharpe: multiply by sqrt(252 trading days)
        return (mean / stdDev) * (decimal)Math.Sqrt(252);
    }

    /// <summary>
    /// Calculate Sortino Ratio (downside risk-adjusted return)
    /// Sortino = (Mean Return / Downside Std Dev) × √252
    /// Only considers negative returns in denominator
    /// </summary>
    private static decimal CalculateSortinoRatio(List<EquityPoint> equityCurve)
    {
        if (equityCurve.Count < 2) return 0;

        var returns = new List<decimal>();
        for (int i = 1; i < equityCurve.Count; i++)
        {
            var prevEquity = equityCurve[i - 1].Equity;
            if (prevEquity == 0) continue;

            var ret = (equityCurve[i].Equity / prevEquity) - 1;
            returns.Add(ret);
        }

        if (!returns.Any()) return 0;

        var mean = returns.Average();

        // Downside deviation: only negative returns
        var negativeReturns = returns.Where(r => r < 0).ToList();
        if (!negativeReturns.Any()) return 0; // No downside = infinite Sortino, return 0

        var downsideDeviation = CalculateStandardDeviation(negativeReturns);

        if (downsideDeviation == 0) return 0;

        // Annualized Sortino
        return (mean / downsideDeviation) * (decimal)Math.Sqrt(252);
    }

    /// <summary>
    /// Calculate standard deviation of a list of values
    /// </summary>
    private static decimal CalculateStandardDeviation(List<decimal> values)
    {
        if (!values.Any()) return 0;

        var avg = values.Average();
        var sumOfSquares = values.Sum(v => (v - avg) * (v - avg));
        var variance = sumOfSquares / values.Count;

        return (decimal)Math.Sqrt((double)variance);
    }

    /// <summary>
    /// Calculate maximum drawdown from peak equity
    /// </summary>
    public static decimal CalculateMaxDrawdown(List<EquityPoint> equityCurve)
    {
        if (!equityCurve.Any()) return 0;

        decimal peak = equityCurve.First().Equity;
        decimal maxDD = 0;

        foreach (var point in equityCurve)
        {
            if (point.Equity > peak)
            {
                peak = point.Equity;
            }

            var drawdown = peak > 0 ? ((point.Equity - peak) / peak) * 100 : 0;
            if (drawdown < maxDD)
            {
                maxDD = drawdown;
            }
        }

        return Math.Round(maxDD, 2);
    }

    /// <summary>
    /// Calculate Calmar Ratio (Annual Return / Max Drawdown)
    /// </summary>
    public static decimal CalculateCalmarRatio(decimal annualReturn, decimal maxDrawdown)
    {
        if (maxDrawdown == 0) return 0;
        return Math.Round(annualReturn / Math.Abs(maxDrawdown), 2);
    }

    /// <summary>
    /// Calculate average holding period in hours
    /// </summary>
    public static decimal CalculateAvgHoldingPeriod(List<TradeResult> trades)
    {
        if (!trades.Any()) return 0;
        return Math.Round((decimal)trades.Average(t => (t.DurationMinutes ?? 0) / 60.0m), 2);
    }

    /// <summary>
    /// Calculate consecutive wins (max streak)
    /// </summary>
    public static int CalculateMaxConsecutiveWins(List<TradeResult> trades)
    {
        if (!trades.Any()) return 0;

        int maxStreak = 0;
        int currentStreak = 0;

        foreach (var trade in trades.OrderBy(t => t.ExitTime))
        {
            if (trade.PnL > 0)
            {
                currentStreak++;
                maxStreak = Math.Max(maxStreak, currentStreak);
            }
            else
            {
                currentStreak = 0;
            }
        }

        return maxStreak;
    }

    /// <summary>
    /// Calculate consecutive losses (max streak)
    /// </summary>
    public static int CalculateMaxConsecutiveLosses(List<TradeResult> trades)
    {
        if (!trades.Any()) return 0;

        int maxStreak = 0;
        int currentStreak = 0;

        foreach (var trade in trades.OrderBy(t => t.ExitTime))
        {
            if (trade.PnL <= 0)
            {
                currentStreak++;
                maxStreak = Math.Max(maxStreak, currentStreak);
            }
            else
            {
                currentStreak = 0;
            }
        }

        return maxStreak;
    }
}
