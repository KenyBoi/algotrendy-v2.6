using AlgoTrendy.Core.Models;

namespace AlgoTrendy.Core.Interfaces;

/// <summary>
/// Advanced risk analytics service for portfolio risk assessment
/// Implements VaR, CVaR, and other risk metrics using real market data
/// </summary>
public interface IRiskAnalyticsService
{
    /// <summary>
    /// Calculates Value at Risk (VaR) for a portfolio
    /// Uses real historical data to estimate maximum expected loss
    /// </summary>
    /// <param name="request">VaR calculation parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>VaR and CVaR results</returns>
    Task<VaRResult> CalculateVaRAsync(
        VaRRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates Conditional Value at Risk (CVaR/Expected Shortfall)
    /// Estimates average loss beyond the VaR threshold
    /// </summary>
    /// <param name="request">VaR calculation parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>CVaR result</returns>
    Task<decimal> CalculateCVaRAsync(
        VaRRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates VaR using historical simulation method
    /// Uses actual historical returns distribution
    /// </summary>
    /// <param name="returns">Historical returns data</param>
    /// <param name="confidenceLevel">Confidence level (e.g., 0.95)</param>
    /// <param name="portfolioValue">Current portfolio value</param>
    /// <param name="timeHorizon">Time horizon in days</param>
    /// <returns>VaR amount</returns>
    Task<decimal> CalculateHistoricalVaRAsync(
        List<decimal> returns,
        decimal confidenceLevel,
        decimal portfolioValue,
        int timeHorizon = 1);

    /// <summary>
    /// Calculates VaR using parametric (variance-covariance) method
    /// Assumes returns follow normal distribution
    /// </summary>
    /// <param name="meanReturn">Expected return</param>
    /// <param name="volatility">Portfolio volatility</param>
    /// <param name="confidenceLevel">Confidence level (e.g., 0.95)</param>
    /// <param name="portfolioValue">Current portfolio value</param>
    /// <param name="timeHorizon">Time horizon in days</param>
    /// <returns>VaR amount</returns>
    Task<decimal> CalculateParametricVaRAsync(
        decimal meanReturn,
        decimal volatility,
        decimal confidenceLevel,
        decimal portfolioValue,
        int timeHorizon = 1);

    /// <summary>
    /// Calculates VaR using Monte Carlo simulation
    /// Simulates thousands of possible price paths
    /// </summary>
    /// <param name="meanReturn">Expected return</param>
    /// <param name="volatility">Portfolio volatility</param>
    /// <param name="confidenceLevel">Confidence level (e.g., 0.95)</param>
    /// <param name="portfolioValue">Current portfolio value</param>
    /// <param name="timeHorizon">Time horizon in days</param>
    /// <param name="simulations">Number of simulations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>VaR amount</returns>
    Task<decimal> CalculateMonteCarloVaRAsync(
        decimal meanReturn,
        decimal volatility,
        decimal confidenceLevel,
        decimal portfolioValue,
        int timeHorizon = 1,
        int simulations = 10000,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates portfolio beta relative to a benchmark
    /// Measures systematic risk exposure
    /// </summary>
    /// <param name="portfolioReturns">Portfolio returns</param>
    /// <param name="benchmarkReturns">Benchmark returns</param>
    /// <returns>Portfolio beta</returns>
    Task<decimal> CalculateBetaAsync(
        List<decimal> portfolioReturns,
        List<decimal> benchmarkReturns);

    /// <summary>
    /// Calculates maximum drawdown from peak
    /// Measures worst historical loss from high water mark
    /// </summary>
    /// <param name="portfolioValues">Historical portfolio values</param>
    /// <returns>Maximum drawdown percentage</returns>
    Task<decimal> CalculateMaximumDrawdownAsync(List<decimal> portfolioValues);

    /// <summary>
    /// Calculates downside deviation
    /// Measures volatility of negative returns only
    /// </summary>
    /// <param name="returns">Historical returns</param>
    /// <param name="targetReturn">Minimum acceptable return</param>
    /// <returns>Downside deviation</returns>
    Task<decimal> CalculateDownsideDeviationAsync(
        List<decimal> returns,
        decimal targetReturn = 0);

    /// <summary>
    /// Calculates Sortino ratio
    /// Risk-adjusted return using downside deviation
    /// </summary>
    /// <param name="returns">Historical returns</param>
    /// <param name="riskFreeRate">Risk-free rate</param>
    /// <param name="targetReturn">Minimum acceptable return</param>
    /// <returns>Sortino ratio</returns>
    Task<decimal> CalculateSortinoRatioAsync(
        List<decimal> returns,
        decimal riskFreeRate = 0,
        decimal targetReturn = 0);

    /// <summary>
    /// Performs stress testing on portfolio
    /// Simulates extreme market scenarios
    /// </summary>
    /// <param name="positions">Current positions</param>
    /// <param name="scenarios">Stress test scenarios</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stress test results</returns>
    Task<Dictionary<string, decimal>> PerformStressTestAsync(
        Dictionary<string, decimal> positions,
        List<StressTestScenario> scenarios,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Stress test scenario definition
/// </summary>
public class StressTestScenario
{
    /// <summary>
    /// Scenario name (e.g., "2008 Financial Crisis", "COVID-19 Crash")
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Price shocks by symbol (percentage change)
    /// </summary>
    public required Dictionary<string, decimal> PriceShocks { get; set; }

    /// <summary>
    /// Volatility multiplier
    /// </summary>
    public decimal VolatilityMultiplier { get; set; } = 1.0m;
}
