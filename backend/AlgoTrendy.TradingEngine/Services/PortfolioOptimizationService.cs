using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Logging;

namespace AlgoTrendy.TradingEngine.Services;

/// <summary>
/// Portfolio optimization service using modern portfolio theory
/// Implements mean-variance optimization with real market data
/// </summary>
public class PortfolioOptimizationService : IPortfolioOptimizationService
{
    private readonly IMarketDataProvider _dataProvider;
    private readonly ILogger<PortfolioOptimizationService> _logger;

    public PortfolioOptimizationService(
        IMarketDataProvider dataProvider,
        ILogger<PortfolioOptimizationService> logger)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<PortfolioOptimizationResult> OptimizePortfolioAsync(
        PortfolioOptimizationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting portfolio optimization for {Count} symbols with risk tolerance {RiskTolerance}",
            request.Symbols.Count, request.RiskTolerance);

        // Fetch historical data for all symbols
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-request.LookbackDays);

        var historicalData = await FetchHistoricalDataAsync(
            request.Symbols, startDate, endDate, cancellationToken);

        // Calculate returns and statistics
        var returns = CalculateReturns(historicalData);
        var expectedReturns = CalculateExpectedReturns(returns);
        var covarianceMatrix = CalculateCovarianceMatrix(returns);
        var volatilities = CalculateVolatilities(returns);

        // Perform mean-variance optimization
        var allocations = OptimizeMeanVariance(
            expectedReturns,
            covarianceMatrix,
            request.RiskTolerance,
            request.MinAllocationPercent,
            request.MaxAllocationPercent);

        // Calculate portfolio metrics
        var portfolioReturn = CalculatePortfolioReturn(allocations, expectedReturns);
        var portfolioRisk = CalculatePortfolioRisk(allocations, covarianceMatrix);
        var sharpeRatio = portfolioRisk > 0 ? portfolioReturn / portfolioRisk : 0;

        // Annualize metrics (assuming daily data)
        var annualizedReturn = portfolioReturn * 252m;
        var annualizedRisk = portfolioRisk * (decimal)Math.Sqrt(252);
        var annualizedSharpe = annualizedRisk > 0 ? annualizedReturn / annualizedRisk : 0;

        _logger.LogInformation(
            "Portfolio optimization complete: Return={Return:P2}, Risk={Risk:P2}, Sharpe={Sharpe:F2}",
            annualizedReturn, annualizedRisk, annualizedSharpe);

        return new PortfolioOptimizationResult
        {
            Allocations = allocations,
            ExpectedReturn = annualizedReturn * 100, // Convert to percentage
            PortfolioRisk = annualizedRisk * 100, // Convert to percentage
            SharpeRatio = annualizedSharpe,
            CovarianceMatrix = ConvertCovarianceToDict(covarianceMatrix, request.Symbols),
            AssetReturns = expectedReturns.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value * 252m * 100), // Annualized percentage
            AssetVolatilities = volatilities.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value * (decimal)Math.Sqrt(252) * 100), // Annualized percentage
            OptimizedAt = DateTime.UtcNow
        };
    }

    /// <inheritdoc/>
    public async Task<List<EfficientFrontierPoint>> CalculateEfficientFrontierAsync(
        List<string> symbols,
        int lookbackDays = 252,
        int points = 100,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Calculating efficient frontier for {Count} symbols with {Points} points",
            symbols.Count, points);

        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-lookbackDays);

        var historicalData = await FetchHistoricalDataAsync(symbols, startDate, endDate, cancellationToken);
        var returns = CalculateReturns(historicalData);
        var expectedReturns = CalculateExpectedReturns(returns);
        var covarianceMatrix = CalculateCovarianceMatrix(returns);

        var frontier = new List<EfficientFrontierPoint>();

        // Generate frontier by varying risk tolerance from 0 to 1
        for (int i = 0; i < points; i++)
        {
            var riskTolerance = (decimal)i / (points - 1);
            var allocations = OptimizeMeanVariance(expectedReturns, covarianceMatrix, riskTolerance);

            var portfolioReturn = CalculatePortfolioReturn(allocations, expectedReturns) * 252m * 100;
            var portfolioRisk = CalculatePortfolioRisk(allocations, covarianceMatrix) * (decimal)Math.Sqrt(252) * 100;
            var sharpe = portfolioRisk > 0 ? portfolioReturn / portfolioRisk : 0;

            frontier.Add(new EfficientFrontierPoint
            {
                Risk = portfolioRisk,
                Return = portfolioReturn,
                SharpeRatio = sharpe,
                Allocations = allocations
            });
        }

        _logger.LogInformation("Efficient frontier calculation complete with {Count} points", frontier.Count);

        return frontier;
    }

    /// <inheritdoc/>
    public async Task<PortfolioOptimizationResult> CalculateMaxSharpePortfolioAsync(
        List<string> symbols,
        int lookbackDays = 252,
        decimal riskFreeRate = 0.02m,
        CancellationToken cancellationToken = default)
    {
        var request = new PortfolioOptimizationRequest
        {
            Symbols = symbols,
            LookbackDays = lookbackDays,
            RiskTolerance = 1.0m, // Maximum return optimization
            TotalCapital = 10000m
        };

        return await OptimizePortfolioAsync(request, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<PortfolioOptimizationResult> CalculateMinimumVariancePortfolioAsync(
        List<string> symbols,
        int lookbackDays = 252,
        CancellationToken cancellationToken = default)
    {
        var request = new PortfolioOptimizationRequest
        {
            Symbols = symbols,
            LookbackDays = lookbackDays,
            RiskTolerance = 0.0m, // Minimum risk optimization
            TotalCapital = 10000m
        };

        return await OptimizePortfolioAsync(request, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, decimal>> CalculateRebalancingTradesAsync(
        Dictionary<string, decimal> currentPositions,
        Dictionary<string, decimal> targetAllocations,
        decimal totalValue,
        CancellationToken cancellationToken = default)
    {
        var trades = new Dictionary<string, decimal>();

        foreach (var symbol in targetAllocations.Keys.Union(currentPositions.Keys))
        {
            var targetValue = targetAllocations.GetValueOrDefault(symbol, 0) / 100m * totalValue;
            var currentValue = currentPositions.GetValueOrDefault(symbol, 0);
            var tradeValue = targetValue - currentValue;

            if (Math.Abs(tradeValue) > 0.01m) // Ignore tiny trades
            {
                trades[symbol] = tradeValue;
            }
        }

        await Task.CompletedTask; // For async compliance
        return trades;
    }

    #region Private Helper Methods

    private async Task<Dictionary<string, List<MarketData>>> FetchHistoricalDataAsync(
        List<string> symbols,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, List<MarketData>>();

        foreach (var symbol in symbols)
        {
            try
            {
                var data = await _dataProvider.FetchHistoricalAsync(
                    symbol, startDate, endDate, "1d", cancellationToken);

                result[symbol] = data.OrderBy(d => d.Timestamp).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch historical data for {Symbol}", symbol);
                result[symbol] = new List<MarketData>();
            }
        }

        return result;
    }

    private Dictionary<string, List<decimal>> CalculateReturns(
        Dictionary<string, List<MarketData>> historicalData)
    {
        var returns = new Dictionary<string, List<decimal>>();

        foreach (var (symbol, data) in historicalData)
        {
            var symbolReturns = new List<decimal>();

            for (int i = 1; i < data.Count; i++)
            {
                if (data[i - 1].Close > 0)
                {
                    var dailyReturn = (data[i].Close - data[i - 1].Close) / data[i - 1].Close;
                    symbolReturns.Add(dailyReturn);
                }
            }

            returns[symbol] = symbolReturns;
        }

        return returns;
    }

    private Dictionary<string, decimal> CalculateExpectedReturns(
        Dictionary<string, List<decimal>> returns)
    {
        var expectedReturns = new Dictionary<string, decimal>();

        foreach (var (symbol, returnList) in returns)
        {
            expectedReturns[symbol] = returnList.Any() ? returnList.Average() : 0;
        }

        return expectedReturns;
    }

    private Dictionary<string, decimal> CalculateVolatilities(
        Dictionary<string, List<decimal>> returns)
    {
        var volatilities = new Dictionary<string, decimal>();

        foreach (var (symbol, returnList) in returns)
        {
            if (returnList.Count > 1)
            {
                var mean = returnList.Average();
                var variance = returnList.Sum(r => (r - mean) * (r - mean)) / (returnList.Count - 1);
                volatilities[symbol] = (decimal)Math.Sqrt((double)variance);
            }
            else
            {
                volatilities[symbol] = 0;
            }
        }

        return volatilities;
    }

    private decimal[,] CalculateCovarianceMatrix(Dictionary<string, List<decimal>> returns)
    {
        var symbols = returns.Keys.ToList();
        var n = symbols.Count;
        var matrix = new decimal[n, n];

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                var returnsI = returns[symbols[i]];
                var returnsJ = returns[symbols[j]];

                if (returnsI.Count > 0 && returnsJ.Count > 0)
                {
                    var minLength = Math.Min(returnsI.Count, returnsJ.Count);
                    var meanI = returnsI.Take(minLength).Average();
                    var meanJ = returnsJ.Take(minLength).Average();

                    decimal covariance = 0;
                    for (int k = 0; k < minLength; k++)
                    {
                        covariance += (returnsI[k] - meanI) * (returnsJ[k] - meanJ);
                    }

                    matrix[i, j] = covariance / (minLength - 1);
                }
            }
        }

        return matrix;
    }

    private Dictionary<string, decimal> OptimizeMeanVariance(
        Dictionary<string, decimal> expectedReturns,
        decimal[,] covarianceMatrix,
        decimal riskTolerance,
        decimal minAllocation = 0,
        decimal maxAllocation = 100)
    {
        // Simplified mean-variance optimization using risk tolerance
        // Risk tolerance: 0 = minimize risk, 1 = maximize return

        var symbols = expectedReturns.Keys.ToList();
        var n = symbols.Count;
        var weights = new decimal[n];

        if (riskTolerance >= 1.0m)
        {
            // Maximum return: allocate to highest return asset
            var maxReturnIdx = 0;
            var maxReturn = decimal.MinValue;

            for (int i = 0; i < n; i++)
            {
                if (expectedReturns[symbols[i]] > maxReturn)
                {
                    maxReturn = expectedReturns[symbols[i]];
                    maxReturnIdx = i;
                }
            }

            weights[maxReturnIdx] = 1.0m;
        }
        else if (riskTolerance <= 0.0m)
        {
            // Minimum variance: equal weight as approximation
            for (int i = 0; i < n; i++)
            {
                weights[i] = 1.0m / n;
            }
        }
        else
        {
            // Blend between min variance and max return
            for (int i = 0; i < n; i++)
            {
                var normalizedReturn = expectedReturns[symbols[i]];
                var variance = covarianceMatrix[i, i];

                // Weight based on risk-adjusted return
                weights[i] = variance > 0
                    ? riskTolerance * normalizedReturn / variance + (1 - riskTolerance) / n
                    : 1.0m / n;
            }

            // Normalize weights to sum to 1
            var totalWeight = weights.Sum();
            if (totalWeight > 0)
            {
                for (int i = 0; i < n; i++)
                {
                    weights[i] /= totalWeight;
                }
            }
        }

        // Apply allocation constraints
        for (int i = 0; i < n; i++)
        {
            weights[i] = Math.Max(minAllocation / 100m, Math.Min(maxAllocation / 100m, weights[i]));
        }

        // Re-normalize after constraints
        var constrainedTotal = weights.Sum();
        if (constrainedTotal > 0)
        {
            for (int i = 0; i < n; i++)
            {
                weights[i] /= constrainedTotal;
            }
        }

        // Convert to dictionary with percentages
        var allocations = new Dictionary<string, decimal>();
        for (int i = 0; i < n; i++)
        {
            allocations[symbols[i]] = weights[i] * 100m; // Convert to percentage
        }

        return allocations;
    }

    private decimal CalculatePortfolioReturn(
        Dictionary<string, decimal> allocations,
        Dictionary<string, decimal> expectedReturns)
    {
        return allocations.Sum(kvp => (kvp.Value / 100m) * expectedReturns.GetValueOrDefault(kvp.Key, 0));
    }

    private decimal CalculatePortfolioRisk(
        Dictionary<string, decimal> allocations,
        decimal[,] covarianceMatrix)
    {
        var symbols = allocations.Keys.ToList();
        var n = symbols.Count;
        decimal variance = 0;

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                var weightI = allocations[symbols[i]] / 100m;
                var weightJ = allocations[symbols[j]] / 100m;
                variance += weightI * weightJ * covarianceMatrix[i, j];
            }
        }

        return (decimal)Math.Sqrt((double)Math.Max(0, variance));
    }

    private Dictionary<string, Dictionary<string, decimal>> ConvertCovarianceToDict(
        decimal[,] matrix,
        List<string> symbols)
    {
        var result = new Dictionary<string, Dictionary<string, decimal>>();
        var n = symbols.Count;

        for (int i = 0; i < n; i++)
        {
            result[symbols[i]] = new Dictionary<string, decimal>();
            for (int j = 0; j < n; j++)
            {
                result[symbols[i]][symbols[j]] = matrix[i, j];
            }
        }

        return result;
    }

    #endregion
}
