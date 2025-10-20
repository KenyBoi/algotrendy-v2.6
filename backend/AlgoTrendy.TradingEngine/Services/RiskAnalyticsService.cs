using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Logging;

namespace AlgoTrendy.TradingEngine.Services;

/// <summary>
/// Advanced risk analytics service implementing VaR, CVaR, and other risk metrics
/// Uses real historical market data for accurate risk assessment
/// </summary>
public class RiskAnalyticsService : IRiskAnalyticsService
{
    private readonly IMarketDataProvider _dataProvider;
    private readonly ILogger<RiskAnalyticsService> _logger;
    private static readonly Random _random = new Random();

    public RiskAnalyticsService(
        IMarketDataProvider dataProvider,
        ILogger<RiskAnalyticsService> logger)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<VaRResult> CalculateVaRAsync(
        VaRRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Calculating VaR using {Method} method at {Confidence:P0} confidence level",
            request.Method, request.ConfidenceLevel);

        // Fetch historical data
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-request.LookbackDays);

        var symbols = request.Symbols ?? request.Positions?.Keys.ToList() ?? new List<string>();
        var historicalData = new Dictionary<string, List<MarketData>>();

        foreach (var symbol in symbols)
        {
            try
            {
                var data = await _dataProvider.FetchHistoricalAsync(
                    symbol, startDate, endDate, "1d", cancellationToken);
                historicalData[symbol] = data.OrderBy(d => d.Timestamp).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch data for {Symbol}", symbol);
            }
        }

        // Calculate returns
        var portfolioReturns = CalculatePortfolioReturns(historicalData, request.Positions);
        var portfolioValue = request.Positions?.Sum(p => p.Value) ?? 10000m;

        // Calculate VaR based on method
        decimal var;
        decimal cvar;

        switch (request.Method)
        {
            case VaRMethod.Historical:
                var = await CalculateHistoricalVaRAsync(
                    portfolioReturns, request.ConfidenceLevel, portfolioValue, request.TimeHorizonDays);
                cvar = await CalculateHistoricalCVaRAsync(
                    portfolioReturns, request.ConfidenceLevel, portfolioValue, request.TimeHorizonDays);
                break;

            case VaRMethod.Parametric:
                var meanReturn = portfolioReturns.Average();
                var volatility = CalculateVolatility(portfolioReturns);
                var = await CalculateParametricVaRAsync(
                    meanReturn, volatility, request.ConfidenceLevel, portfolioValue, request.TimeHorizonDays);
                cvar = await CalculateParametricCVaRAsync(
                    meanReturn, volatility, request.ConfidenceLevel, portfolioValue, request.TimeHorizonDays);
                break;

            case VaRMethod.MonteCarlo:
                meanReturn = portfolioReturns.Average();
                volatility = CalculateVolatility(portfolioReturns);
                var = await CalculateMonteCarloVaRAsync(
                    meanReturn, volatility, request.ConfidenceLevel, portfolioValue,
                    request.TimeHorizonDays, 10000, cancellationToken);
                cvar = await CalculateMonteCarloCVaRAsync(
                    meanReturn, volatility, request.ConfidenceLevel, portfolioValue,
                    request.TimeHorizonDays, 10000, cancellationToken);
                break;

            default:
                throw new ArgumentException($"Unknown VaR method: {request.Method}");
        }

        // Calculate distribution statistics
        var distributionStats = CalculateDistributionStats(portfolioReturns);

        _logger.LogInformation(
            "VaR calculation complete: VaR={VaR:C}, CVaR={CVaR:C}",
            var, cvar);

        return new VaRResult
        {
            VaR = Math.Abs(var),
            CVaR = Math.Abs(cvar),
            VaRPercent = portfolioValue > 0 ? Math.Abs(var) / portfolioValue * 100m : 0,
            CVaRPercent = portfolioValue > 0 ? Math.Abs(cvar) / portfolioValue * 100m : 0,
            ConfidenceLevel = request.ConfidenceLevel,
            TimeHorizonDays = request.TimeHorizonDays,
            Method = request.Method,
            PortfolioValue = portfolioValue,
            PortfolioVolatility = CalculateVolatility(portfolioReturns) * (decimal)Math.Sqrt(252) * 100,
            ReturnsDistribution = distributionStats,
            CalculatedAt = DateTime.UtcNow
        };
    }

    /// <inheritdoc/>
    public async Task<decimal> CalculateCVaRAsync(
        VaRRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await CalculateVaRAsync(request, cancellationToken);
        return result.CVaR;
    }

    /// <inheritdoc/>
    public async Task<decimal> CalculateHistoricalVaRAsync(
        List<decimal> returns,
        decimal confidenceLevel,
        decimal portfolioValue,
        int timeHorizon = 1)
    {
        await Task.CompletedTask;

        if (!returns.Any())
            return 0;

        // Scale returns by time horizon
        var scaledReturns = returns.Select(r => r * (decimal)Math.Sqrt(timeHorizon)).OrderBy(r => r).ToList();

        // Find the percentile
        var percentile = 1 - confidenceLevel;
        var index = (int)(percentile * scaledReturns.Count);
        index = Math.Max(0, Math.Min(scaledReturns.Count - 1, index));

        var varReturn = scaledReturns[index];
        return varReturn * portfolioValue; // Negative value represents loss
    }

    /// <inheritdoc/>
    public async Task<decimal> CalculateParametricVaRAsync(
        decimal meanReturn,
        decimal volatility,
        decimal confidenceLevel,
        decimal portfolioValue,
        int timeHorizon = 1)
    {
        await Task.CompletedTask;

        // Z-score for the confidence level (using normal distribution)
        var zScore = GetZScore(confidenceLevel);

        // Adjust for time horizon
        var adjustedMean = meanReturn * timeHorizon;
        var adjustedVolatility = volatility * (decimal)Math.Sqrt(timeHorizon);

        // VaR = -(μ + σ * z)
        var varReturn = -(adjustedMean + adjustedVolatility * zScore);
        return varReturn * portfolioValue;
    }

    /// <inheritdoc/>
    public async Task<decimal> CalculateMonteCarloVaRAsync(
        decimal meanReturn,
        decimal volatility,
        decimal confidenceLevel,
        decimal portfolioValue,
        int timeHorizon = 1,
        int simulations = 10000,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Running Monte Carlo VaR simulation with {Simulations} iterations",
            simulations);

        var simulatedReturns = new List<decimal>();

        for (int i = 0; i < simulations; i++)
        {
            if (i % 1000 == 0 && cancellationToken.IsCancellationRequested)
                break;

            // Generate random return using Box-Muller transform
            var u1 = (decimal)_random.NextDouble();
            var u2 = (decimal)_random.NextDouble();
            var randomNormal = (decimal)(Math.Sqrt(-2.0 * Math.Log((double)u1)) *
                                        Math.Cos(2.0 * Math.PI * (double)u2));

            var simulatedReturn = meanReturn * timeHorizon +
                                volatility * (decimal)Math.Sqrt(timeHorizon) * randomNormal;
            simulatedReturns.Add(simulatedReturn);
        }

        await Task.CompletedTask;
        return await CalculateHistoricalVaRAsync(simulatedReturns, confidenceLevel, portfolioValue, 1);
    }

    /// <inheritdoc/>
    public async Task<decimal> CalculateBetaAsync(
        List<decimal> portfolioReturns,
        List<decimal> benchmarkReturns)
    {
        await Task.CompletedTask;

        if (portfolioReturns.Count != benchmarkReturns.Count || portfolioReturns.Count < 2)
            return 1.0m; // Default to market beta

        var meanPortfolio = portfolioReturns.Average();
        var meanBenchmark = benchmarkReturns.Average();

        decimal covariance = 0;
        decimal benchmarkVariance = 0;

        for (int i = 0; i < portfolioReturns.Count; i++)
        {
            var portfolioDev = portfolioReturns[i] - meanPortfolio;
            var benchmarkDev = benchmarkReturns[i] - meanBenchmark;

            covariance += portfolioDev * benchmarkDev;
            benchmarkVariance += benchmarkDev * benchmarkDev;
        }

        if (benchmarkVariance == 0)
            return 1.0m;

        return covariance / benchmarkVariance;
    }

    /// <inheritdoc/>
    public async Task<decimal> CalculateMaximumDrawdownAsync(List<decimal> portfolioValues)
    {
        await Task.CompletedTask;

        if (!portfolioValues.Any())
            return 0;

        decimal maxDrawdown = 0;
        decimal peak = portfolioValues[0];

        foreach (var value in portfolioValues)
        {
            if (value > peak)
                peak = value;

            var drawdown = peak > 0 ? (peak - value) / peak : 0;
            if (drawdown > maxDrawdown)
                maxDrawdown = drawdown;
        }

        return maxDrawdown * 100; // Return as percentage
    }

    /// <inheritdoc/>
    public async Task<decimal> CalculateDownsideDeviationAsync(
        List<decimal> returns,
        decimal targetReturn = 0)
    {
        await Task.CompletedTask;

        if (!returns.Any())
            return 0;

        var downsideReturns = returns.Where(r => r < targetReturn).ToList();

        if (!downsideReturns.Any())
            return 0;

        var sumSquaredDeviations = downsideReturns.Sum(r => (r - targetReturn) * (r - targetReturn));
        return (decimal)Math.Sqrt((double)(sumSquaredDeviations / downsideReturns.Count));
    }

    /// <inheritdoc/>
    public async Task<decimal> CalculateSortinoRatioAsync(
        List<decimal> returns,
        decimal riskFreeRate = 0,
        decimal targetReturn = 0)
    {
        var downsideDeviation = await CalculateDownsideDeviationAsync(returns, targetReturn);

        if (downsideDeviation == 0)
            return 0;

        var meanReturn = returns.Average();
        var excessReturn = meanReturn - riskFreeRate;

        return excessReturn / downsideDeviation;
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, decimal>> PerformStressTestAsync(
        Dictionary<string, decimal> positions,
        List<StressTestScenario> scenarios,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, decimal>();

        foreach (var scenario in scenarios)
        {
            decimal scenarioLoss = 0;

            foreach (var (symbol, quantity) in positions)
            {
                if (scenario.PriceShocks.TryGetValue(symbol, out var shock))
                {
                    scenarioLoss += quantity * shock / 100m; // Convert percentage to decimal
                }
            }

            results[scenario.Name] = scenarioLoss;

            _logger.LogInformation(
                "Stress test scenario '{Scenario}': Loss = {Loss:C}",
                scenario.Name, scenarioLoss);
        }

        await Task.CompletedTask;
        return results;
    }

    #region Private Helper Methods

    private List<decimal> CalculatePortfolioReturns(
        Dictionary<string, List<MarketData>> historicalData,
        Dictionary<string, decimal>? positions)
    {
        if (positions == null || !positions.Any())
        {
            // Equal weighted portfolio
            positions = historicalData.Keys.ToDictionary(k => k, k => 1.0m);
        }

        var totalWeight = positions.Values.Sum();
        var weights = positions.ToDictionary(p => p.Key, p => p.Value / totalWeight);

        // Find common dates
        var allDates = historicalData.Values
            .SelectMany(d => d.Select(md => md.Timestamp.Date))
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        var portfolioReturns = new List<decimal>();

        for (int i = 1; i < allDates.Count; i++)
        {
            decimal portfolioReturn = 0;

            foreach (var (symbol, weight) in weights)
            {
                if (historicalData.TryGetValue(symbol, out var data))
                {
                    var prevData = data.FirstOrDefault(d => d.Timestamp.Date == allDates[i - 1]);
                    var currData = data.FirstOrDefault(d => d.Timestamp.Date == allDates[i]);

                    if (prevData != null && currData != null && prevData.Close > 0)
                    {
                        var assetReturn = (currData.Close - prevData.Close) / prevData.Close;
                        portfolioReturn += weight * assetReturn;
                    }
                }
            }

            portfolioReturns.Add(portfolioReturn);
        }

        return portfolioReturns;
    }

    private async Task<decimal> CalculateHistoricalCVaRAsync(
        List<decimal> returns,
        decimal confidenceLevel,
        decimal portfolioValue,
        int timeHorizon)
    {
        await Task.CompletedTask;

        if (!returns.Any())
            return 0;

        var scaledReturns = returns.Select(r => r * (decimal)Math.Sqrt(timeHorizon)).OrderBy(r => r).ToList();

        var percentile = 1 - confidenceLevel;
        var index = (int)(percentile * scaledReturns.Count);

        // CVaR is the average of all returns below VaR threshold
        var tailReturns = scaledReturns.Take(index + 1);
        var cvarReturn = tailReturns.Any() ? tailReturns.Average() : 0;

        return cvarReturn * portfolioValue;
    }

    private async Task<decimal> CalculateParametricCVaRAsync(
        decimal meanReturn,
        decimal volatility,
        decimal confidenceLevel,
        decimal portfolioValue,
        int timeHorizon)
    {
        await Task.CompletedTask;

        var zScore = GetZScore(confidenceLevel);
        var adjustedMean = meanReturn * timeHorizon;
        var adjustedVolatility = volatility * (decimal)Math.Sqrt(timeHorizon);

        // CVaR for normal distribution: μ + σ * φ(z) / (1 - α)
        // Simplified approximation
        var phiZ = (decimal)(Math.Exp(-(double)(zScore * zScore) / 2) / Math.Sqrt(2 * Math.PI));
        var cvarReturn = -(adjustedMean + adjustedVolatility * phiZ / (1 - confidenceLevel));

        return cvarReturn * portfolioValue;
    }

    private async Task<decimal> CalculateMonteCarloCVaRAsync(
        decimal meanReturn,
        decimal volatility,
        decimal confidenceLevel,
        decimal portfolioValue,
        int timeHorizon,
        int simulations,
        CancellationToken cancellationToken)
    {
        var simulatedReturns = new List<decimal>();

        for (int i = 0; i < simulations; i++)
        {
            if (i % 1000 == 0 && cancellationToken.IsCancellationRequested)
                break;

            var u1 = (decimal)_random.NextDouble();
            var u2 = (decimal)_random.NextDouble();
            var randomNormal = (decimal)(Math.Sqrt(-2.0 * Math.Log((double)u1)) *
                                        Math.Cos(2.0 * Math.PI * (double)u2));

            var simulatedReturn = meanReturn * timeHorizon +
                                volatility * (decimal)Math.Sqrt(timeHorizon) * randomNormal;
            simulatedReturns.Add(simulatedReturn);
        }

        await Task.CompletedTask;
        return await CalculateHistoricalCVaRAsync(simulatedReturns, confidenceLevel, portfolioValue, 1);
    }

    private decimal CalculateVolatility(List<decimal> returns)
    {
        if (returns.Count < 2)
            return 0;

        var mean = returns.Average();
        var variance = returns.Sum(r => (r - mean) * (r - mean)) / (returns.Count - 1);
        return (decimal)Math.Sqrt((double)variance);
    }

    private decimal GetZScore(decimal confidenceLevel)
    {
        // Approximate z-scores for common confidence levels
        return confidenceLevel switch
        {
            >= 0.99m => -2.326m,
            >= 0.95m => -1.645m,
            >= 0.90m => -1.282m,
            _ => -1.645m
        };
    }

    private DistributionStats CalculateDistributionStats(List<decimal> returns)
    {
        if (!returns.Any())
        {
            return new DistributionStats
            {
                Mean = 0,
                StandardDeviation = 0,
                Skewness = 0,
                Kurtosis = 0,
                Min = 0,
                Max = 0,
                Percentile5 = 0,
                Percentile95 = 0
            };
        }

        var sorted = returns.OrderBy(r => r).ToList();
        var mean = returns.Average();
        var stdDev = CalculateVolatility(returns);

        // Calculate skewness and kurtosis
        decimal skewness = 0;
        decimal kurtosis = 0;

        if (stdDev > 0)
        {
            foreach (var ret in returns)
            {
                var standardized = (ret - mean) / stdDev;
                skewness += standardized * standardized * standardized;
                kurtosis += standardized * standardized * standardized * standardized;
            }

            skewness /= returns.Count;
            kurtosis = kurtosis / returns.Count - 3; // Excess kurtosis
        }

        return new DistributionStats
        {
            Mean = mean,
            StandardDeviation = stdDev,
            Skewness = skewness,
            Kurtosis = kurtosis,
            Min = sorted.First(),
            Max = sorted.Last(),
            Percentile5 = sorted[(int)(0.05m * sorted.Count)],
            Percentile95 = sorted[(int)(0.95m * sorted.Count)]
        };
    }

    #endregion
}
