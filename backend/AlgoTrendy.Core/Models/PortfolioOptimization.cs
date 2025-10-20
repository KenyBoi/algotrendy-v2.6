namespace AlgoTrendy.Core.Models;

/// <summary>
/// Portfolio optimization request for mean-variance optimization
/// </summary>
public class PortfolioOptimizationRequest
{
    /// <summary>
    /// List of symbols to optimize
    /// </summary>
    public required List<string> Symbols { get; set; }

    /// <summary>
    /// Total capital to allocate
    /// </summary>
    public decimal TotalCapital { get; set; }

    /// <summary>
    /// Risk tolerance level (0.0 = minimum risk, 1.0 = maximum return)
    /// </summary>
    public decimal RiskTolerance { get; set; } = 0.5m;

    /// <summary>
    /// Historical data lookback period in days
    /// </summary>
    public int LookbackDays { get; set; } = 252; // 1 trading year

    /// <summary>
    /// Minimum allocation percentage per asset (0-100)
    /// </summary>
    public decimal MinAllocationPercent { get; set; } = 0m;

    /// <summary>
    /// Maximum allocation percentage per asset (0-100)
    /// </summary>
    public decimal MaxAllocationPercent { get; set; } = 100m;

    /// <summary>
    /// Whether to allow short positions
    /// </summary>
    public bool AllowShort { get; set; } = false;
}

/// <summary>
/// Portfolio optimization result from mean-variance optimization
/// </summary>
public class PortfolioOptimizationResult
{
    /// <summary>
    /// Optimized asset allocations
    /// </summary>
    public required Dictionary<string, decimal> Allocations { get; set; }

    /// <summary>
    /// Expected portfolio return (annualized percentage)
    /// </summary>
    public decimal ExpectedReturn { get; set; }

    /// <summary>
    /// Portfolio volatility/risk (annualized standard deviation)
    /// </summary>
    public decimal PortfolioRisk { get; set; }

    /// <summary>
    /// Sharpe ratio (risk-adjusted return)
    /// </summary>
    public decimal SharpeRatio { get; set; }

    /// <summary>
    /// Covariance matrix of returns
    /// </summary>
    public Dictionary<string, Dictionary<string, decimal>>? CovarianceMatrix { get; set; }

    /// <summary>
    /// Individual asset expected returns
    /// </summary>
    public Dictionary<string, decimal>? AssetReturns { get; set; }

    /// <summary>
    /// Individual asset volatilities
    /// </summary>
    public Dictionary<string, decimal>? AssetVolatilities { get; set; }

    /// <summary>
    /// Optimization timestamp
    /// </summary>
    public DateTime OptimizedAt { get; set; }

    /// <summary>
    /// Efficient frontier points (risk vs return)
    /// </summary>
    public List<EfficientFrontierPoint>? EfficientFrontier { get; set; }
}

/// <summary>
/// Point on the efficient frontier
/// </summary>
public class EfficientFrontierPoint
{
    /// <summary>
    /// Portfolio risk (volatility)
    /// </summary>
    public decimal Risk { get; set; }

    /// <summary>
    /// Expected return
    /// </summary>
    public decimal Return { get; set; }

    /// <summary>
    /// Sharpe ratio
    /// </summary>
    public decimal SharpeRatio { get; set; }

    /// <summary>
    /// Asset allocations for this point
    /// </summary>
    public Dictionary<string, decimal>? Allocations { get; set; }
}

/// <summary>
/// VaR (Value at Risk) calculation request
/// </summary>
public class VaRRequest
{
    /// <summary>
    /// Portfolio positions or symbols to analyze
    /// </summary>
    public List<string>? Symbols { get; set; }

    /// <summary>
    /// Position quantities for each symbol
    /// </summary>
    public Dictionary<string, decimal>? Positions { get; set; }

    /// <summary>
    /// Confidence level (e.g., 0.95 for 95%, 0.99 for 99%)
    /// </summary>
    public decimal ConfidenceLevel { get; set; } = 0.95m;

    /// <summary>
    /// Time horizon in days
    /// </summary>
    public int TimeHorizonDays { get; set; } = 1;

    /// <summary>
    /// Historical data lookback period in days
    /// </summary>
    public int LookbackDays { get; set; } = 252;

    /// <summary>
    /// VaR calculation method
    /// </summary>
    public VaRMethod Method { get; set; } = VaRMethod.Historical;
}

/// <summary>
/// VaR calculation method
/// </summary>
public enum VaRMethod
{
    /// <summary>
    /// Historical simulation method
    /// </summary>
    Historical,

    /// <summary>
    /// Parametric (variance-covariance) method
    /// </summary>
    Parametric,

    /// <summary>
    /// Monte Carlo simulation method
    /// </summary>
    MonteCarlo
}

/// <summary>
/// VaR and CVaR calculation result
/// </summary>
public class VaRResult
{
    /// <summary>
    /// Value at Risk amount
    /// </summary>
    public decimal VaR { get; set; }

    /// <summary>
    /// Conditional Value at Risk (CVaR/ES) amount
    /// </summary>
    public decimal CVaR { get; set; }

    /// <summary>
    /// VaR as percentage of portfolio value
    /// </summary>
    public decimal VaRPercent { get; set; }

    /// <summary>
    /// CVaR as percentage of portfolio value
    /// </summary>
    public decimal CVaRPercent { get; set; }

    /// <summary>
    /// Confidence level used
    /// </summary>
    public decimal ConfidenceLevel { get; set; }

    /// <summary>
    /// Time horizon in days
    /// </summary>
    public int TimeHorizonDays { get; set; }

    /// <summary>
    /// Calculation method used
    /// </summary>
    public VaRMethod Method { get; set; }

    /// <summary>
    /// Total portfolio value
    /// </summary>
    public decimal PortfolioValue { get; set; }

    /// <summary>
    /// Portfolio volatility (annualized)
    /// </summary>
    public decimal PortfolioVolatility { get; set; }

    /// <summary>
    /// Individual asset VaRs
    /// </summary>
    public Dictionary<string, decimal>? ComponentVaRs { get; set; }

    /// <summary>
    /// Historical returns distribution stats
    /// </summary>
    public DistributionStats? ReturnsDistribution { get; set; }

    /// <summary>
    /// Calculation timestamp
    /// </summary>
    public DateTime CalculatedAt { get; set; }
}

/// <summary>
/// Statistical distribution information
/// </summary>
public class DistributionStats
{
    /// <summary>
    /// Mean return
    /// </summary>
    public decimal Mean { get; set; }

    /// <summary>
    /// Standard deviation
    /// </summary>
    public decimal StandardDeviation { get; set; }

    /// <summary>
    /// Skewness
    /// </summary>
    public decimal Skewness { get; set; }

    /// <summary>
    /// Kurtosis
    /// </summary>
    public decimal Kurtosis { get; set; }

    /// <summary>
    /// Minimum value
    /// </summary>
    public decimal Min { get; set; }

    /// <summary>
    /// Maximum value
    /// </summary>
    public decimal Max { get; set; }

    /// <summary>
    /// 5th percentile
    /// </summary>
    public decimal Percentile5 { get; set; }

    /// <summary>
    /// 95th percentile
    /// </summary>
    public decimal Percentile95 { get; set; }
}
