using AlgoTrendy.Core.Models;

namespace AlgoTrendy.Core.Interfaces;

/// <summary>
/// Portfolio optimization service using modern portfolio theory (MPT)
/// Implements mean-variance optimization and efficient frontier analysis
/// </summary>
public interface IPortfolioOptimizationService
{
    /// <summary>
    /// Optimizes portfolio allocation using mean-variance optimization
    /// Uses real historical market data to calculate returns and covariances
    /// </summary>
    /// <param name="request">Optimization parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Optimized portfolio allocation with expected return and risk</returns>
    Task<PortfolioOptimizationResult> OptimizePortfolioAsync(
        PortfolioOptimizationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the efficient frontier for given assets
    /// Generates multiple portfolio combinations along the risk-return spectrum
    /// </summary>
    /// <param name="symbols">List of asset symbols</param>
    /// <param name="lookbackDays">Historical data period</param>
    /// <param name="points">Number of points on the frontier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Efficient frontier points</returns>
    Task<List<EfficientFrontierPoint>> CalculateEfficientFrontierAsync(
        List<string> symbols,
        int lookbackDays = 252,
        int points = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the maximum Sharpe ratio portfolio
    /// Finds the optimal balance between risk and return
    /// </summary>
    /// <param name="symbols">List of asset symbols</param>
    /// <param name="lookbackDays">Historical data period</param>
    /// <param name="riskFreeRate">Risk-free rate (annualized)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Optimal portfolio allocation</returns>
    Task<PortfolioOptimizationResult> CalculateMaxSharpePortfolioAsync(
        List<string> symbols,
        int lookbackDays = 252,
        decimal riskFreeRate = 0.02m,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the minimum variance portfolio
    /// Finds the allocation with the lowest possible risk
    /// </summary>
    /// <param name="symbols">List of asset symbols</param>
    /// <param name="lookbackDays">Historical data period</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Minimum variance portfolio allocation</returns>
    Task<PortfolioOptimizationResult> CalculateMinimumVariancePortfolioAsync(
        List<string> symbols,
        int lookbackDays = 252,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rebalances current portfolio to match target allocation
    /// Calculates trades needed to adjust current positions
    /// </summary>
    /// <param name="currentPositions">Current portfolio positions</param>
    /// <param name="targetAllocations">Desired allocation percentages</param>
    /// <param name="totalValue">Total portfolio value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Rebalancing trades to execute</returns>
    Task<Dictionary<string, decimal>> CalculateRebalancingTradesAsync(
        Dictionary<string, decimal> currentPositions,
        Dictionary<string, decimal> targetAllocations,
        decimal totalValue,
        CancellationToken cancellationToken = default);
}
