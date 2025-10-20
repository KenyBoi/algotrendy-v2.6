using AlgoTrendy.Backtesting.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace AlgoTrendy.Backtesting.Services;

/// <summary>
/// Interface for MEM (MemGPT) integration
/// </summary>
public interface IMEMIntegrationService
{
    /// <summary>
    /// Send backtest results to MEM for learning and analysis
    /// </summary>
    Task<MEMAnalysisResult> SendBacktestResultsToMEMAsync(BacktestResults results, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get strategy recommendations from MEM based on backtest results
    /// </summary>
    Task<MEMStrategyRecommendation> GetStrategyRecommendationAsync(BacktestResults results, CancellationToken cancellationToken = default);

    /// <summary>
    /// Store backtest results in MEM's persistent memory
    /// </summary>
    Task<bool> StorInMEMMemoryAsync(BacktestResults results, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get MEM's confidence score for a strategy
    /// </summary>
    Task<double> GetMEMConfidenceScoreAsync(BacktestConfig config, CancellationToken cancellationToken = default);
}

/// <summary>
/// MEM analysis result
/// </summary>
public class MEMAnalysisResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Analysis { get; set; }
    public double ConfidenceScore { get; set; }
    public List<string> Insights { get; set; } = new();
    public Dictionary<string, object> Recommendations { get; set; } = new();
}

/// <summary>
/// MEM strategy recommendation
/// </summary>
public class MEMStrategyRecommendation
{
    public string StrategyName { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public List<string> Reasoning { get; set; } = new();
    public string RiskLevel { get; set; } = "Medium";
    public double ExpectedReturn { get; set; }
    public double ExpectedSharpe { get; set; }
}

/// <summary>
/// Implementation of MEM integration service
/// </summary>
public class MEMIntegrationService : IMEMIntegrationService
{
    private readonly ILogger<MEMIntegrationService> _logger;
    private readonly string _memKnowledgePath;
    private readonly string _coreMemoryPath;
    private readonly string _parameterUpdatesPath;

    public MEMIntegrationService(ILogger<MEMIntegrationService> logger)
    {
        _logger = logger;

        // MEM knowledge directory paths
        var baseDir = Environment.GetEnvironmentVariable("ALGOTRENDY_DATA_PATH")
            ?? "/root/AlgoTrendy_v2.6";
        _memKnowledgePath = Path.Combine(baseDir, "data", "mem_knowledge");
        _coreMemoryPath = Path.Combine(_memKnowledgePath, "core_memory_updates.txt");
        _parameterUpdatesPath = Path.Combine(_memKnowledgePath, "parameter_updates.json");

        // Ensure directories exist
        Directory.CreateDirectory(_memKnowledgePath);
    }

    /// <inheritdoc/>
    public async Task<MEMAnalysisResult> SendBacktestResultsToMEMAsync(
        BacktestResults results,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending backtest results to MEM for analysis: {BacktestId}", results.BacktestId);

            // Store results in MEM's memory
            await StorInMEMMemoryAsync(results, cancellationToken);

            // Analyze results
            var analysis = AnalyzeBacktestResults(results);
            var insights = GenerateInsights(results);
            var recommendations = GenerateRecommendations(results);
            var confidenceScore = CalculateConfidenceScore(results);

            _logger.LogInformation(
                "MEM analysis complete. Confidence: {Confidence}, Insights: {InsightCount}",
                confidenceScore, insights.Count);

            return new MEMAnalysisResult
            {
                Success = true,
                Analysis = analysis,
                ConfidenceScore = confidenceScore,
                Insights = insights,
                Recommendations = recommendations
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending backtest results to MEM");
            return new MEMAnalysisResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc/>
    public async Task<MEMStrategyRecommendation> GetStrategyRecommendationAsync(
        BacktestResults results,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting strategy recommendation from MEM for {Symbol}", results.Config.Symbol);

            // Analyze the backtest performance
            var metrics = results.Metrics;
            var isSuccessful = metrics != null &&
                metrics.SharpeRatio > 1.0m &&
                metrics.WinRate > 0.5m &&
                metrics.TotalReturn > 0;

            // Generate reasoning based on results
            var reasoning = new List<string>();

            if (isSuccessful)
            {
                reasoning.Add($"Strong performance with Sharpe ratio of {(double)metrics!.SharpeRatio:F2}");
                reasoning.Add($"Consistent win rate of {(double)metrics.WinRate:P1}");
                reasoning.Add($"Positive total return of {(double)metrics.TotalReturn:P2}");
            }
            else
            {
                reasoning.Add("Performance metrics indicate need for optimization");
                if (metrics != null)
                {
                    if (metrics.SharpeRatio < 1.0m)
                        reasoning.Add($"Low Sharpe ratio ({metrics.SharpeRatio:F2}) - consider improving risk-adjusted returns");
                    if (metrics.WinRate < 0.5m)
                        reasoning.Add($"Win rate below 50% ({metrics.WinRate:P1}) - refine entry/exit signals");
                    if (metrics.TotalReturn < 0)
                        reasoning.Add($"Negative returns ({metrics.TotalReturn:P2}) - strategy needs significant improvement");
                }
            }

            // Load historical parameter performance
            var historicalParams = await LoadHistoricalParameters();

            return new MEMStrategyRecommendation
            {
                StrategyName = isSuccessful ? "Optimized Momentum Strategy" : "Conservative Risk-Managed Strategy",
                ConfidenceScore = CalculateConfidenceScore(results),
                Parameters = GenerateOptimizedParameters(results, historicalParams),
                Reasoning = reasoning,
                RiskLevel = DetermineRiskLevel(results),
                ExpectedReturn = (double)(metrics?.AnnualizedReturn ?? 0),
                ExpectedSharpe = (double)(metrics?.SharpeRatio ?? 0)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting strategy recommendation from MEM");
            return new MEMStrategyRecommendation
            {
                StrategyName = "Error",
                ConfidenceScore = 0,
                Reasoning = new List<string> { $"Error: {ex.Message}" }
            };
        }
    }

    /// <inheritdoc/>
    public async Task<bool> StorInMEMMemoryAsync(
        BacktestResults results,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Storing backtest results in MEM memory: {BacktestId}", results.BacktestId);

            // Create memory entry
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            var memoryEntry = new StringBuilder();

            memoryEntry.AppendLine($"[{timestamp}] Backtest: {results.BacktestId}");
            memoryEntry.AppendLine($"  Symbol: {results.Config.Symbol}");
            memoryEntry.AppendLine($"  Period: {results.Config.StartDate:yyyy-MM-dd} to {results.Config.EndDate:yyyy-MM-dd}");
            memoryEntry.AppendLine($"  Status: {results.Status}");

            if (results.Metrics != null)
            {
                memoryEntry.AppendLine($"  Metrics:");
                memoryEntry.AppendLine($"    - Total Return: {results.Metrics.TotalReturn:P2}");
                memoryEntry.AppendLine($"    - Sharpe Ratio: {results.Metrics.SharpeRatio:F2}");
                memoryEntry.AppendLine($"    - Win Rate: {results.Metrics.WinRate:P1}");
                memoryEntry.AppendLine($"    - Max Drawdown: {results.Metrics.MaxDrawdown:P2}");
                memoryEntry.AppendLine($"    - Total Trades: {results.Metrics.TotalTrades}");
                memoryEntry.AppendLine($"    - Profit Factor: {results.Metrics.ProfitFactor:F2}");
            }

            // Append to core memory file
            await File.AppendAllTextAsync(_coreMemoryPath, memoryEntry.ToString(), cancellationToken);

            // Update parameter history
            await UpdateParameterHistory(results, cancellationToken);

            _logger.LogInformation("Successfully stored backtest results in MEM memory");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing backtest results in MEM memory");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<double> GetMEMConfidenceScoreAsync(
        BacktestConfig config,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Calculating MEM confidence score for {Symbol}", config.Symbol);

            // Load historical performance for similar configurations
            var historicalParams = await LoadHistoricalParameters();

            // Calculate confidence based on historical data
            var baseConfidence = 0.5; // Start with neutral confidence

            // Adjust based on symbol familiarity
            var symbolHistory = historicalParams
                .Where(p => p.Symbol == config.Symbol)
                .ToList();

            if (symbolHistory.Any())
            {
                var avgPerformance = symbolHistory.Average(p => p.Performance);
                baseConfidence += avgPerformance * 0.3; // Add up to 30% based on historical performance
            }

            // Adjust based on timeframe
            var timeframeDays = (config.EndDate - config.StartDate).TotalDays;
            if (timeframeDays >= 365)
                baseConfidence += 0.1; // More confidence with longer backtests
            else if (timeframeDays < 30)
                baseConfidence -= 0.1; // Less confidence with very short backtests

            // Clamp to [0, 1]
            return Math.Max(0, Math.Min(1, baseConfidence));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating MEM confidence score");
            return 0.5; // Return neutral confidence on error
        }
    }

    #region Private Helper Methods

    private string AnalyzeBacktestResults(BacktestResults results)
    {
        if (results.Metrics == null)
            return "Backtest completed but no metrics available.";

        var analysis = new StringBuilder();
        analysis.AppendLine($"Backtest Analysis for {results.Config.Symbol}:");
        analysis.AppendLine($"- Overall Performance: {(results.Metrics.TotalReturn > 0 ? "Profitable" : "Loss-making")}");
        analysis.AppendLine($"- Risk-Adjusted Return: {GetSharpeRating((double)results.Metrics.SharpeRatio)}");
        analysis.AppendLine($"- Consistency: {GetWinRateRating((double)results.Metrics.WinRate)}");
        analysis.AppendLine($"- Drawdown Risk: {GetDrawdownRating((double)results.Metrics.MaxDrawdown)}");

        return analysis.ToString();
    }

    private List<string> GenerateInsights(BacktestResults results)
    {
        var insights = new List<string>();

        if (results.Metrics == null)
            return insights;

        // Sharpe ratio insights
        if (results.Metrics.SharpeRatio > 2.0m)
            insights.Add("Exceptional risk-adjusted returns - strategy shows strong potential");
        else if (results.Metrics.SharpeRatio < 0.5m)
            insights.Add("Poor risk-adjusted returns - consider reducing volatility or improving entry signals");

        // Win rate insights
        if (results.Metrics.WinRate > 0.6m)
            insights.Add("High win rate indicates reliable signal generation");
        else if (results.Metrics.WinRate < 0.4m)
            insights.Add("Low win rate suggests entry signals need refinement");

        // Drawdown insights
        if (results.Metrics.MaxDrawdown < -0.2m)
            insights.Add("Large drawdown detected - implement stronger risk management");
        else if (results.Metrics.MaxDrawdown > -0.05m)
            insights.Add("Low drawdown shows good risk control");

        // Profit factor insights
        if (results.Metrics.ProfitFactor > 2.0m)
            insights.Add("Strong profit factor indicates profitable trading system");
        else if (results.Metrics.ProfitFactor < 1.0m)
            insights.Add("Profit factor below 1.0 - strategy is unprofitable");

        return insights;
    }

    private Dictionary<string, object> GenerateRecommendations(BacktestResults results)
    {
        var recommendations = new Dictionary<string, object>();

        if (results.Metrics == null)
            return recommendations;

        // Position sizing recommendation
        if (results.Metrics.MaxDrawdown < -0.15m)
            recommendations["positionSize"] = "Reduce position size to limit drawdown risk";
        else if (results.Metrics.SharpeRatio > 2.0m)
            recommendations["positionSize"] = "Consider increasing position size given strong risk-adjusted returns";

        // Strategy adjustment recommendations
        if (results.Metrics.WinRate < 0.45m)
            recommendations["entrySignals"] = "Refine entry signals to improve win rate";

        if (results.Metrics.AverageLoss > Math.Abs(results.Metrics.AverageWin) * 2)
            recommendations["stopLoss"] = "Tighten stop losses - average losses are too large";

        // Overall strategy recommendation
        if (results.Metrics.SharpeRatio > 1.5m && results.Metrics.TotalReturn > 0.1m)
            recommendations["overallStrategy"] = "Strategy shows promise - consider live testing with small position sizes";
        else
            recommendations["overallStrategy"] = "Strategy needs optimization before live deployment";

        return recommendations;
    }

    private double CalculateConfidenceScore(BacktestResults results)
    {
        if (results.Metrics == null)
            return 0.0;

        var score = 0.0;

        // Sharpe ratio contribution (max 30 points)
        score += Math.Min(30, (double)results.Metrics.SharpeRatio * 15);

        // Win rate contribution (max 25 points)
        score += (double)results.Metrics.WinRate * 25;

        // Total return contribution (max 20 points)
        score += Math.Min(20, Math.Max(-20, (double)results.Metrics.TotalReturn * 20));

        // Drawdown contribution (max 15 points - less drawdown is better)
        score += Math.Min(15, (1 - Math.Abs((double)results.Metrics.MaxDrawdown)) * 15);

        // Trade count contribution (max 10 points)
        if (results.Metrics.TotalTrades > 30)
            score += 10;
        else if (results.Metrics.TotalTrades > 10)
            score += 5;

        // Normalize to 0-1
        return Math.Max(0, Math.Min(1, score / 100));
    }

    private Dictionary<string, object> GenerateOptimizedParameters(
        BacktestResults results,
        List<ParameterHistory> historicalParams)
    {
        var parameters = new Dictionary<string, object>();

        // Base parameters
        parameters["fastMaPeriod"] = 20;
        parameters["slowMaPeriod"] = 50;
        parameters["rsiPeriod"] = 14;
        parameters["rsiOverbought"] = 70;
        parameters["rsiOversold"] = 30;

        // Adjust based on results
        if (results.Metrics != null)
        {
            // If win rate is low, be more conservative
            if (results.Metrics.WinRate < 0.45m)
            {
                parameters["rsiOverbought"] = 65; // Exit earlier
                parameters["rsiOversold"] = 35; // Enter more conservatively
            }

            // If drawdown is high, reduce position size
            if (results.Metrics.MaxDrawdown < -0.15m)
            {
                parameters["positionSizePercent"] = 50; // Half position
            }
            else
            {
                parameters["positionSizePercent"] = 100; // Full position
            }
        }

        return parameters;
    }

    private string DetermineRiskLevel(BacktestResults results)
    {
        if (results.Metrics == null)
            return "Unknown";

        var drawdown = Math.Abs((double)results.Metrics.MaxDrawdown);

        if (drawdown > 0.3)
            return "High";
        else if (drawdown > 0.15)
            return "Medium";
        else
            return "Low";
    }

    private async Task UpdateParameterHistory(BacktestResults results, CancellationToken cancellationToken)
    {
        try
        {
            var history = await LoadHistoricalParameters();

            var newEntry = new ParameterHistory
            {
                Timestamp = DateTime.UtcNow,
                Symbol = results.Config.Symbol,
                Performance = (double)(results.Metrics?.SharpeRatio ?? 0),
                Parameters = new Dictionary<string, object>
                {
                    { "timeframe", results.Config.Timeframe },
                    { "totalReturn", (double)(results.Metrics?.TotalReturn ?? 0) },
                    { "sharpeRatio", (double)(results.Metrics?.SharpeRatio ?? 0) },
                    { "winRate", (double)(results.Metrics?.WinRate ?? 0) },
                    { "maxDrawdown", (double)(results.Metrics?.MaxDrawdown ?? 0) }
                }
            };

            history.Add(newEntry);

            // Keep only last 1000 entries
            if (history.Count > 1000)
            {
                history = history.OrderByDescending(h => h.Timestamp).Take(1000).ToList();
            }

            var json = JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_parameterUpdatesPath, json, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error updating parameter history");
        }
    }

    private async Task<List<ParameterHistory>> LoadHistoricalParameters()
    {
        try
        {
            if (File.Exists(_parameterUpdatesPath))
            {
                var json = await File.ReadAllTextAsync(_parameterUpdatesPath);
                return JsonSerializer.Deserialize<List<ParameterHistory>>(json) ?? new List<ParameterHistory>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error loading historical parameters");
        }

        return new List<ParameterHistory>();
    }

    private string GetSharpeRating(double sharpeRatio)
    {
        if (sharpeRatio > 2.0) return "Excellent";
        if (sharpeRatio > 1.0) return "Good";
        if (sharpeRatio > 0.5) return "Fair";
        return "Poor";
    }

    private string GetWinRateRating(double winRate)
    {
        if (winRate > 0.6) return "High";
        if (winRate > 0.5) return "Good";
        if (winRate > 0.4) return "Fair";
        return "Low";
    }

    private string GetDrawdownRating(double maxDrawdown)
    {
        var absDrawdown = Math.Abs(maxDrawdown);
        if (absDrawdown < 0.05) return "Very Low";
        if (absDrawdown < 0.10) return "Low";
        if (absDrawdown < 0.20) return "Moderate";
        return "High";
    }

    #endregion
}

/// <summary>
/// Parameter history entry for MEM learning
/// </summary>
public class ParameterHistory
{
    public DateTime Timestamp { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public double Performance { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}
