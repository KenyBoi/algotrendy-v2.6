using AlgoTrendy.MultiCharts.Interfaces;
using AlgoTrendy.MultiCharts.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AlgoTrendy.MultiCharts.Controllers;

/// <summary>
/// API controller for MultiCharts integration
/// </summary>
[ApiController]
[Route("api/multicharts")]
[Produces("application/json")]
public class MultiChartsController : ControllerBase
{
    private readonly IMultiChartsClient _multiChartsClient;
    private readonly ILogger<MultiChartsController> _logger;

    public MultiChartsController(
        IMultiChartsClient multiChartsClient,
        ILogger<MultiChartsController> logger)
    {
        _multiChartsClient = multiChartsClient ?? throw new ArgumentNullException(nameof(multiChartsClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Test connection to MultiCharts platform
    /// </summary>
    /// <returns>Connection status</returns>
    [HttpGet("health")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> TestConnection()
    {
        try
        {
            var isConnected = await _multiChartsClient.TestConnectionAsync();

            return Ok(new
            {
                connected = isConnected,
                timestamp = DateTime.UtcNow,
                message = isConnected ? "MultiCharts is connected" : "MultiCharts is not available"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get MultiCharts platform status
    /// </summary>
    /// <returns>Platform status information</returns>
    [HttpGet("status")]
    [ProducesResponseType(typeof(MultiChartsPlatformStatus), 200)]
    public async Task<IActionResult> GetPlatformStatus()
    {
        try
        {
            var status = await _multiChartsClient.GetPlatformStatusAsync();
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get platform status");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Run a backtest for a strategy
    /// </summary>
    /// <param name="request">Backtest parameters</param>
    /// <returns>Backtest results</returns>
    [HttpPost("backtest")]
    [ProducesResponseType(typeof(BacktestResult), 200)]
    public async Task<IActionResult> RunBacktest([FromBody] BacktestRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.StrategyName))
                return BadRequest(new { error = "Strategy name is required" });

            if (string.IsNullOrEmpty(request.Symbol))
                return BadRequest(new { error = "Symbol is required" });

            var result = await _multiChartsClient.RunBacktestAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backtest failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Run walk-forward optimization
    /// </summary>
    /// <param name="request">Walk-forward parameters</param>
    /// <returns>Optimization results</returns>
    [HttpPost("optimization/walk-forward")]
    [ProducesResponseType(typeof(WalkForwardResult), 200)]
    public async Task<IActionResult> RunWalkForwardOptimization([FromBody] WalkForwardRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.StrategyName))
                return BadRequest(new { error = "Strategy name is required" });

            if (string.IsNullOrEmpty(request.Symbol))
                return BadRequest(new { error = "Symbol is required" });

            var result = await _multiChartsClient.RunWalkForwardOptimizationAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Walk-forward optimization failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Run Monte Carlo simulation
    /// </summary>
    /// <param name="request">Monte Carlo parameters</param>
    /// <returns>Simulation results</returns>
    [HttpPost("simulation/monte-carlo")]
    [ProducesResponseType(typeof(MonteCarloResult), 200)]
    public async Task<IActionResult> RunMonteCarloSimulation([FromBody] MonteCarloRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.StrategyName))
                return BadRequest(new { error = "Strategy name is required" });

            if (string.IsNullOrEmpty(request.Symbol))
                return BadRequest(new { error = "Symbol is required" });

            if (request.NumberOfRuns < 100 || request.NumberOfRuns > 10000)
                return BadRequest(new { error = "Number of runs must be between 100 and 10000" });

            var result = await _multiChartsClient.RunMonteCarloSimulationAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Monte Carlo simulation failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Deploy a strategy to MultiCharts
    /// </summary>
    /// <param name="request">Strategy deployment parameters</param>
    /// <returns>Deployment result</returns>
    [HttpPost("strategy/deploy")]
    [ProducesResponseType(typeof(StrategyDeploymentResult), 200)]
    public async Task<IActionResult> DeployStrategy([FromBody] StrategyDeploymentRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.StrategyName))
                return BadRequest(new { error = "Strategy name is required" });

            if (string.IsNullOrEmpty(request.StrategyCode))
                return BadRequest(new { error = "Strategy code is required" });

            var result = await _multiChartsClient.DeployStrategyAsync(request);

            if (result.Success)
                return Ok(result);
            else
                return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Strategy deployment failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get list of available strategies
    /// </summary>
    /// <returns>List of strategies</returns>
    [HttpGet("strategy/list")]
    [ProducesResponseType(typeof(List<StrategyInfo>), 200)]
    public async Task<IActionResult> GetStrategies()
    {
        try
        {
            var strategies = await _multiChartsClient.GetStrategiesAsync();
            return Ok(strategies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get strategies list");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Run market scanner
    /// </summary>
    /// <param name="request">Scan parameters</param>
    /// <returns>Scan results</returns>
    [HttpPost("scanner/run")]
    [ProducesResponseType(typeof(ScanResult), 200)]
    public async Task<IActionResult> RunMarketScan([FromBody] ScanRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ScanName))
                return BadRequest(new { error = "Scan name is required" });

            if (request.Symbols == null || request.Symbols.Count == 0)
                return BadRequest(new { error = "Symbols list is required" });

            if (string.IsNullOrEmpty(request.ScanFormula))
                return BadRequest(new { error = "Scan formula is required" });

            var result = await _multiChartsClient.RunMarketScanAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Market scan failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get list of available indicators
    /// </summary>
    /// <returns>List of indicators</returns>
    [HttpGet("indicator/list")]
    [ProducesResponseType(typeof(List<IndicatorInfo>), 200)]
    public async Task<IActionResult> GetIndicators()
    {
        try
        {
            var indicators = await _multiChartsClient.GetIndicatorsAsync();
            return Ok(indicators);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get indicators list");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get historical data
    /// </summary>
    /// <param name="request">Data request parameters</param>
    /// <returns>Historical OHLCV data</returns>
    [HttpPost("data/historical")]
    [ProducesResponseType(typeof(List<OHLCVData>), 200)]
    public async Task<IActionResult> GetHistoricalData([FromBody] DataRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Symbol))
                return BadRequest(new { error = "Symbol is required" });

            if (request.FromDate >= request.ToDate)
                return BadRequest(new { error = "FromDate must be before ToDate" });

            var data = await _multiChartsClient.GetHistoricalDataAsync(request);
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get historical data");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
