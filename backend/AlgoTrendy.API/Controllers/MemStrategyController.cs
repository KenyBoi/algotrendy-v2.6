using AlgoTrendy.TradingEngine.Services;
using Microsoft.AspNetCore.Mvc;
using static AlgoTrendy.TradingEngine.Services.MemStrategyDeploymentService;

namespace AlgoTrendy.API.Controllers;

/// <summary>
/// MEM Strategy Orchestration API
/// Enables MEM to deploy, manage, and recommend trading strategies
/// Integrates with Strategy Registry for dynamic strategy selection
/// </summary>
[ApiController]
[Route("api/mem/strategies")]
[Produces("application/json")]
public class MemStrategyController : ControllerBase
{
    private readonly ILogger<MemStrategyController> _logger;
    private readonly MemStrategyDeploymentService _deploymentService;

    public MemStrategyController(
        ILogger<MemStrategyController> logger,
        MemStrategyDeploymentService deploymentService)
    {
        _logger = logger;
        _deploymentService = deploymentService;
    }

    /// <summary>
    /// Get all available strategies from the registry
    /// </summary>
    /// <returns>List of available strategies with metadata and performance metrics</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<RegistryStrategy>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAvailableStrategies()
    {
        try
        {
            var strategies = await _deploymentService.GetAvailableStrategiesAsync();

            return Ok(new ApiResponse<List<RegistryStrategy>>
            {
                Success = true,
                Data = strategies,
                Message = $"Found {strategies.Count} strategies in registry",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching available strategies");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to fetch strategies",
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Get a specific strategy by ID
    /// </summary>
    /// <param name="id">Strategy ID</param>
    /// <returns>Strategy details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<RegistryStrategy>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStrategy(string id)
    {
        try
        {
            var strategy = await _deploymentService.GetStrategyAsync(id);

            if (strategy == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Strategy {id} not found",
                    Timestamp = DateTime.UtcNow
                });
            }

            return Ok(new ApiResponse<RegistryStrategy>
            {
                Success = true,
                Data = strategy,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching strategy {StrategyId}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to fetch strategy",
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Deploy a strategy (Freqtrade or native)
    /// </summary>
    /// <param name="id">Strategy ID to deploy</param>
    /// <param name="options">Deployment options</param>
    /// <returns>Deployment result with process information</returns>
    [HttpPost("{id}/deploy")]
    [ProducesResponseType(typeof(ApiResponse<DeployedStrategy>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeployStrategy(string id, [FromBody] DeploymentOptions? options = null)
    {
        try
        {
            _logger.LogInformation("Deploying strategy {StrategyId} with options: {@Options}", id, options);

            var result = await _deploymentService.DeployStrategyAsync(id, options);

            if (!result.Success)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = result.Message ?? "Deployment failed",
                    Timestamp = DateTime.UtcNow
                });
            }

            return Ok(new ApiResponse<DeployedStrategy>
            {
                Success = true,
                Data = result.Strategy,
                Message = $"Successfully deployed strategy {id}",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deploying strategy {StrategyId}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to deploy strategy",
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Stop a deployed strategy
    /// </summary>
    /// <param name="id">Strategy ID to stop</param>
    /// <returns>Result of stop operation</returns>
    [HttpPost("{id}/stop")]
    [ProducesResponseType(typeof(ApiResponse<DeployedStrategy>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> StopStrategy(string id)
    {
        try
        {
            _logger.LogInformation("Stopping strategy {StrategyId}", id);

            var result = await _deploymentService.StopStrategyAsync(id);

            if (!result.Success)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = result.Message ?? "Stop failed",
                    Timestamp = DateTime.UtcNow
                });
            }

            return Ok(new ApiResponse<DeployedStrategy>
            {
                Success = true,
                Data = result.Strategy,
                Message = $"Successfully stopped strategy {id}",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping strategy {StrategyId}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to stop strategy",
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Get all currently deployed strategies
    /// </summary>
    /// <returns>List of deployed strategies</returns>
    [HttpGet("deployed")]
    [ProducesResponseType(typeof(ApiResponse<List<DeployedStrategy>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GetDeployedStrategies()
    {
        try
        {
            var deployed = _deploymentService.GetDeployedStrategies();

            return Ok(new ApiResponse<List<DeployedStrategy>>
            {
                Success = true,
                Data = deployed,
                Message = $"{deployed.Count} strategies currently deployed",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching deployed strategies");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to fetch deployed strategies",
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Get MEM's recommended strategies based on current market conditions
    /// </summary>
    /// <param name="request">Market conditions for analysis</param>
    /// <returns>List of recommended strategies with confidence scores</returns>
    [HttpPost("recommendations")]
    [ProducesResponseType(typeof(ApiResponse<List<StrategyRecommendation>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetRecommendations([FromBody] MarketConditions request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Market conditions are required",
                    Timestamp = DateTime.UtcNow
                });
            }

            _logger.LogInformation("Getting MEM recommendations for market conditions: {@Conditions}", request);

            var recommendations = await _deploymentService.GetMemRecommendationsAsync(request);

            return Ok(new ApiResponse<List<StrategyRecommendation>>
            {
                Success = true,
                Data = recommendations,
                Message = $"MEM recommends {recommendations.Count} strategies",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting MEM recommendations");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to get recommendations",
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Get quick market-based recommendations (simplified endpoint)
    /// </summary>
    /// <param name="trend">Market trend: bullish, bearish, ranging</param>
    /// <param name="volatility">Volatility level: low, medium, high</param>
    /// <returns>List of recommended strategies</returns>
    [HttpGet("recommendations/quick")]
    [ProducesResponseType(typeof(ApiResponse<List<StrategyRecommendation>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetQuickRecommendations(
        [FromQuery] string trend = "neutral",
        [FromQuery] string volatility = "medium")
    {
        try
        {
            var conditions = new MarketConditions
            {
                Trend = trend,
                Volatility = volatility
            };

            var recommendations = await _deploymentService.GetMemRecommendationsAsync(conditions);

            return Ok(new ApiResponse<List<StrategyRecommendation>>
            {
                Success = true,
                Data = recommendations,
                Message = $"MEM recommends {recommendations.Count} strategies for {trend} trend with {volatility} volatility",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quick recommendations");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to get recommendations",
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    #region Response Models

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
        public DateTime Timestamp { get; set; }
    }

    #endregion
}
