using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AlgoTrendy.API.Controllers;

/// <summary>
/// Portfolio optimization and advanced risk analytics controller
/// Provides mean-variance optimization, VaR, CVaR, and other risk metrics
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PortfolioAnalyticsController : ControllerBase
{
    private readonly IPortfolioOptimizationService _optimizationService;
    private readonly IRiskAnalyticsService _riskAnalyticsService;
    private readonly ILogger<PortfolioAnalyticsController> _logger;

    public PortfolioAnalyticsController(
        IPortfolioOptimizationService optimizationService,
        IRiskAnalyticsService riskAnalyticsService,
        ILogger<PortfolioAnalyticsController> logger)
    {
        _optimizationService = optimizationService ?? throw new ArgumentNullException(nameof(optimizationService));
        _riskAnalyticsService = riskAnalyticsService ?? throw new ArgumentNullException(nameof(riskAnalyticsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Optimizes portfolio allocation using mean-variance optimization
    /// Uses real historical market data for accurate optimization
    /// </summary>
    /// <param name="request">Portfolio optimization request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Optimized portfolio allocation with expected return and risk</returns>
    /// <response code="200">Portfolio optimized successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("optimize")]
    [ProducesResponseType(typeof(PortfolioOptimizationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PortfolioOptimizationResult>> OptimizePortfolioAsync(
        [FromBody] PortfolioOptimizationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Portfolio optimization requested for {Count} symbols with risk tolerance {RiskTolerance}",
                request.Symbols.Count, request.RiskTolerance);

            var result = await _optimizationService.OptimizePortfolioAsync(request, cancellationToken);

            _logger.LogInformation(
                "Portfolio optimization complete: Return={Return:F2}%, Risk={Risk:F2}%, Sharpe={Sharpe:F2}",
                result.ExpectedReturn, result.PortfolioRisk, result.SharpeRatio);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid portfolio optimization request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing portfolio");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Calculates the efficient frontier for portfolio optimization
    /// Shows the risk-return tradeoff across different allocations
    /// </summary>
    /// <param name="request">Efficient frontier request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of efficient frontier points</returns>
    /// <response code="200">Efficient frontier calculated successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("efficient-frontier")]
    [ProducesResponseType(typeof(List<EfficientFrontierPoint>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<EfficientFrontierPoint>>> GetEfficientFrontierAsync(
        [FromBody] EfficientFrontierRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Efficient frontier requested for {Count} symbols with {Points} points",
                request.Symbols.Count, request.Points);

            var result = await _optimizationService.CalculateEfficientFrontierAsync(
                request.Symbols,
                request.LookbackDays,
                request.Points,
                cancellationToken);

            _logger.LogInformation(
                "Efficient frontier calculated with {Count} points",
                result.Count);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid efficient frontier request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating efficient frontier");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Calculates Value at Risk (VaR) and Conditional Value at Risk (CVaR)
    /// Uses real historical data to estimate maximum expected loss
    /// </summary>
    /// <param name="request">VaR calculation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>VaR and CVaR results with detailed risk metrics</returns>
    /// <response code="200">VaR calculated successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("var")]
    [ProducesResponseType(typeof(VaRResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VaRResult>> CalculateVaRAsync(
        [FromBody] VaRRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "VaR calculation requested using {Method} method at {Confidence:P0} confidence",
                request.Method, request.ConfidenceLevel);

            var result = await _riskAnalyticsService.CalculateVaRAsync(request, cancellationToken);

            _logger.LogInformation(
                "VaR calculated: VaR={VaR:C} ({VaRPercent:F2}%), CVaR={CVaR:C} ({CVaRPercent:F2}%)",
                result.VaR, result.VaRPercent, result.CVaR, result.CVaRPercent);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid VaR calculation request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating VaR");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Calculates maximum Sharpe ratio portfolio
    /// Finds the optimal balance between risk and return
    /// </summary>
    /// <param name="request">Max Sharpe request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Optimal portfolio allocation</returns>
    /// <response code="200">Max Sharpe portfolio calculated successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("max-sharpe")]
    [ProducesResponseType(typeof(PortfolioOptimizationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PortfolioOptimizationResult>> GetMaxSharpePortfolioAsync(
        [FromBody] MaxSharpeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Max Sharpe portfolio requested for {Count} symbols",
                request.Symbols.Count);

            var result = await _optimizationService.CalculateMaxSharpePortfolioAsync(
                request.Symbols,
                request.LookbackDays,
                request.RiskFreeRate,
                cancellationToken);

            _logger.LogInformation(
                "Max Sharpe portfolio calculated: Sharpe={Sharpe:F2}",
                result.SharpeRatio);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid max Sharpe request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating max Sharpe portfolio");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Calculates minimum variance portfolio
    /// Finds the allocation with the lowest possible risk
    /// </summary>
    /// <param name="request">Min variance request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Minimum variance portfolio allocation</returns>
    /// <response code="200">Min variance portfolio calculated successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("min-variance")]
    [ProducesResponseType(typeof(PortfolioOptimizationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PortfolioOptimizationResult>> GetMinVariancePortfolioAsync(
        [FromBody] MinVarianceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Min variance portfolio requested for {Count} symbols",
                request.Symbols.Count);

            var result = await _optimizationService.CalculateMinimumVariancePortfolioAsync(
                request.Symbols,
                request.LookbackDays,
                cancellationToken);

            _logger.LogInformation(
                "Min variance portfolio calculated: Risk={Risk:F2}%",
                result.PortfolioRisk);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid min variance request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating min variance portfolio");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Performs stress testing on portfolio
    /// Simulates extreme market scenarios to assess resilience
    /// </summary>
    /// <param name="request">Stress test request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stress test results for each scenario</returns>
    /// <response code="200">Stress test completed successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("stress-test")]
    [ProducesResponseType(typeof(Dictionary<string, decimal>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Dictionary<string, decimal>>> PerformStressTestAsync(
        [FromBody] StressTestRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Stress test requested with {Count} scenarios",
                request.Scenarios.Count);

            var result = await _riskAnalyticsService.PerformStressTestAsync(
                request.Positions,
                request.Scenarios,
                cancellationToken);

            _logger.LogInformation(
                "Stress test completed for {Count} scenarios",
                result.Count);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid stress test request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing stress test");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Calculates portfolio beta relative to benchmark
    /// Measures systematic risk exposure
    /// </summary>
    /// <param name="request">Beta calculation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Portfolio beta</returns>
    /// <response code="200">Beta calculated successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("beta")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<decimal>> CalculateBetaAsync(
        [FromBody] BetaRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Beta calculation requested");

            var beta = await _riskAnalyticsService.CalculateBetaAsync(
                request.PortfolioReturns,
                request.BenchmarkReturns);

            _logger.LogInformation("Beta calculated: {Beta:F2}", beta);

            return Ok(beta);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid beta calculation request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating beta");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

#region Request Models

/// <summary>
/// Efficient frontier request
/// </summary>
public class EfficientFrontierRequest
{
    /// <summary>
    /// List of symbols to analyze
    /// </summary>
    [Required]
    [MinLength(2, ErrorMessage = "At least 2 symbols required")]
    public required List<string> Symbols { get; set; }

    /// <summary>
    /// Historical data lookback period in days
    /// </summary>
    [Range(30, 1000, ErrorMessage = "Lookback days must be between 30 and 1000")]
    public int LookbackDays { get; set; } = 252;

    /// <summary>
    /// Number of points on the frontier
    /// </summary>
    [Range(10, 200, ErrorMessage = "Points must be between 10 and 200")]
    public int Points { get; set; } = 100;
}

/// <summary>
/// Maximum Sharpe ratio request
/// </summary>
public class MaxSharpeRequest
{
    /// <summary>
    /// List of symbols
    /// </summary>
    [Required]
    [MinLength(2)]
    public required List<string> Symbols { get; set; }

    /// <summary>
    /// Lookback period in days
    /// </summary>
    [Range(30, 1000)]
    public int LookbackDays { get; set; } = 252;

    /// <summary>
    /// Risk-free rate (annualized)
    /// </summary>
    [Range(0, 0.1)]
    public decimal RiskFreeRate { get; set; } = 0.02m;
}

/// <summary>
/// Minimum variance request
/// </summary>
public class MinVarianceRequest
{
    /// <summary>
    /// List of symbols
    /// </summary>
    [Required]
    [MinLength(2)]
    public required List<string> Symbols { get; set; }

    /// <summary>
    /// Lookback period in days
    /// </summary>
    [Range(30, 1000)]
    public int LookbackDays { get; set; } = 252;
}

/// <summary>
/// Stress test request
/// </summary>
public class StressTestRequest
{
    /// <summary>
    /// Current positions
    /// </summary>
    [Required]
    public required Dictionary<string, decimal> Positions { get; set; }

    /// <summary>
    /// Stress test scenarios
    /// </summary>
    [Required]
    [MinLength(1)]
    public required List<StressTestScenario> Scenarios { get; set; }
}

/// <summary>
/// Beta calculation request
/// </summary>
public class BetaRequest
{
    /// <summary>
    /// Portfolio returns
    /// </summary>
    [Required]
    [MinLength(20)]
    public required List<decimal> PortfolioReturns { get; set; }

    /// <summary>
    /// Benchmark returns
    /// </summary>
    [Required]
    [MinLength(20)]
    public required List<decimal> BenchmarkReturns { get; set; }
}

#endregion
