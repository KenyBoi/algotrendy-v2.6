using AlgoTrendy.Backtesting.Engines;
using AlgoTrendy.Backtesting.Models;
using AlgoTrendy.Backtesting.Models.QuantConnect;
using AlgoTrendy.Backtesting.Services;
using Microsoft.AspNetCore.Mvc;

namespace AlgoTrendy.API.Controllers;

/// <summary>
/// API controller for QuantConnect cloud backtesting operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class QuantConnectController : ControllerBase
{
    private readonly IQuantConnectApiClient _qcClient;
    private readonly QuantConnectBacktestEngine _qcEngine;
    private readonly IMEMIntegrationService _memService;
    private readonly ILogger<QuantConnectController> _logger;

    public QuantConnectController(
        IQuantConnectApiClient qcClient,
        QuantConnectBacktestEngine qcEngine,
        IMEMIntegrationService memService,
        ILogger<QuantConnectController> logger)
    {
        _qcClient = qcClient;
        _qcEngine = qcEngine;
        _memService = memService;
        _logger = logger;
    }

    /// <summary>
    /// Test QuantConnect API authentication
    /// </summary>
    /// <returns>Authentication status</returns>
    [HttpGet("auth/test")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> TestAuthenticationAsync()
    {
        try
        {
            _logger.LogInformation("Testing QuantConnect authentication");

            var authenticated = await _qcClient.AuthenticateAsync();

            if (authenticated)
            {
                return Ok(new { success = true, message = "Successfully authenticated with QuantConnect API" });
            }

            return Unauthorized(new { success = false, message = "Failed to authenticate with QuantConnect API" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing QuantConnect authentication");
            return StatusCode(500, new { success = false, message = "Error testing authentication", error = ex.Message });
        }
    }

    /// <summary>
    /// Run a backtest on QuantConnect cloud platform
    /// </summary>
    /// <param name="config">Backtest configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Backtest results with metrics and trades</returns>
    [HttpPost("backtest")]
    [ProducesResponseType(typeof(BacktestResults), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BacktestResults>> RunBacktestAsync(
        [FromBody] BacktestConfig config,
        CancellationToken cancellationToken)
    {
        try
        {
            if (config == null)
                return BadRequest(new { message = "Backtest configuration is required" });

            _logger.LogInformation(
                "Running QuantConnect backtest for {Symbol} from {StartDate} to {EndDate}",
                config.Symbol, config.StartDate, config.EndDate);

            // Run backtest on QuantConnect
            var results = await _qcEngine.RunAsync(config, cancellationToken);

            // Send results to MEM for analysis and learning
            if (results.Status == BacktestStatus.Completed)
            {
                _logger.LogInformation("Sending backtest results to MEM for analysis");
                _ = _memService.SendBacktestResultsToMEMAsync(results, cancellationToken);
            }

            return Ok(results);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("QuantConnect backtest was cancelled");
            return StatusCode(499, new { message = "Backtest was cancelled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running QuantConnect backtest");
            return StatusCode(500, new { message = "Error running backtest", error = ex.Message });
        }
    }

    /// <summary>
    /// Run backtest and get MEM analysis
    /// </summary>
    /// <param name="config">Backtest configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Backtest results with MEM analysis</returns>
    [HttpPost("backtest/with-analysis")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> RunBacktestWithAnalysisAsync(
        [FromBody] BacktestConfig config,
        CancellationToken cancellationToken)
    {
        try
        {
            if (config == null)
                return BadRequest(new { message = "Backtest configuration is required" });

            _logger.LogInformation("Running QuantConnect backtest with MEM analysis for {Symbol}", config.Symbol);

            // Run backtest
            var results = await _qcEngine.RunAsync(config, cancellationToken);

            if (results.Status != BacktestStatus.Completed)
            {
                return Ok(new
                {
                    backtest = results,
                    analysis = (MEMAnalysisResult?)null,
                    recommendation = (MEMStrategyRecommendation?)null
                });
            }

            // Get MEM analysis
            var analysis = await _memService.SendBacktestResultsToMEMAsync(results, cancellationToken);
            var recommendation = await _memService.GetStrategyRecommendationAsync(results, cancellationToken);

            return Ok(new
            {
                backtest = results,
                analysis = analysis,
                recommendation = recommendation
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running QuantConnect backtest with analysis");
            return StatusCode(500, new { message = "Error running backtest with analysis", error = ex.Message });
        }
    }

    /// <summary>
    /// Get MEM's confidence score for a strategy
    /// </summary>
    /// <param name="config">Backtest configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Confidence score (0-1)</returns>
    [HttpPost("strategy/confidence")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetStrategyConfidenceAsync(
        [FromBody] BacktestConfig config,
        CancellationToken cancellationToken)
    {
        try
        {
            if (config == null)
                return BadRequest(new { message = "Configuration is required" });

            _logger.LogInformation("Getting MEM confidence score for {Symbol}", config.Symbol);

            var confidenceScore = await _memService.GetMEMConfidenceScoreAsync(config, cancellationToken);

            return Ok(new
            {
                symbol = config.Symbol,
                confidenceScore = confidenceScore,
                rating = GetConfidenceRating(confidenceScore)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting strategy confidence");
            return StatusCode(500, new { message = "Error getting confidence score", error = ex.Message });
        }
    }

    /// <summary>
    /// List all QuantConnect projects
    /// </summary>
    /// <returns>List of projects</returns>
    [HttpGet("projects")]
    [ProducesResponseType(typeof(QCProjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<QCProjectResponse>> ListProjectsAsync()
    {
        try
        {
            _logger.LogInformation("Listing QuantConnect projects");

            // Note: This would require implementing a list projects method in IQuantConnectApiClient
            // For now, return a placeholder response
            return Ok(new QCProjectResponse
            {
                Success = true,
                Projects = new List<QCProject>()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing QuantConnect projects");
            return StatusCode(500, new { message = "Error listing projects", error = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific QuantConnect project
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Project details</returns>
    [HttpGet("projects/{projectId}")]
    [ProducesResponseType(typeof(QCProjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<QCProjectResponse>> GetProjectAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting QuantConnect project {ProjectId}", projectId);

            var response = await _qcClient.ReadProjectAsync(projectId, cancellationToken);

            if (!response.Success)
            {
                return NotFound(new { message = $"Project {projectId} not found" });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting QuantConnect project {ProjectId}", projectId);
            return StatusCode(500, new { message = "Error getting project", error = ex.Message });
        }
    }

    /// <summary>
    /// List backtests for a project
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of backtests</returns>
    [HttpGet("projects/{projectId}/backtests")]
    [ProducesResponseType(typeof(QCBacktestListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<QCBacktestListResponse>> ListBacktestsAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Listing backtests for project {ProjectId}", projectId);

            var response = await _qcClient.ListBacktestsAsync(projectId, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing backtests for project {ProjectId}", projectId);
            return StatusCode(500, new { message = "Error listing backtests", error = ex.Message });
        }
    }

    /// <summary>
    /// Get specific backtest results from QuantConnect
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="backtestId">Backtest ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Backtest results</returns>
    [HttpGet("projects/{projectId}/backtests/{backtestId}")]
    [ProducesResponseType(typeof(QCBacktestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<QCBacktestResponse>> GetBacktestAsync(
        int projectId,
        string backtestId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting backtest {BacktestId} for project {ProjectId}", backtestId, projectId);

            var response = await _qcClient.ReadBacktestAsync(projectId, backtestId, cancellationToken);

            if (!response.Success)
            {
                return NotFound(new { message = $"Backtest {backtestId} not found" });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting backtest {BacktestId}", backtestId);
            return StatusCode(500, new { message = "Error getting backtest", error = ex.Message });
        }
    }

    /// <summary>
    /// Export strategy to QuantConnect project
    /// </summary>
    /// <param name="request">Strategy export request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Export result with project details</returns>
    [HttpPost("strategy/export")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> ExportStrategyAsync(
        [FromBody] StrategyExportRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.StrategyCode))
                return BadRequest(new { message = "Strategy code is required" });

            _logger.LogInformation("Exporting strategy to QuantConnect");

            // Create project
            var projectName = request.ProjectName ?? $"AlgoTrendy_Strategy_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            var projectResponse = await _qcClient.CreateProjectAsync(projectName, "CSharp", cancellationToken);

            if (!projectResponse.Success || projectResponse.Projects.Count == 0)
            {
                var error = string.Join(", ", projectResponse.Errors);
                return StatusCode(500, new { message = $"Failed to create project: {error}" });
            }

            var projectId = projectResponse.Projects[0].ProjectId;

            // Upload strategy code
            var fileCreated = await _qcClient.CreateOrUpdateFileAsync(
                projectId,
                "Main.cs",
                request.StrategyCode,
                cancellationToken);

            if (!fileCreated)
            {
                return StatusCode(500, new { message = "Failed to upload strategy code" });
            }

            _logger.LogInformation("Strategy exported successfully to project {ProjectId}", projectId);

            return Ok(new
            {
                success = true,
                projectId = projectId,
                projectName = projectName,
                message = "Strategy exported successfully to QuantConnect"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting strategy to QuantConnect");
            return StatusCode(500, new { message = "Error exporting strategy", error = ex.Message });
        }
    }

    private string GetConfidenceRating(double score)
    {
        if (score >= 0.8) return "Very High";
        if (score >= 0.6) return "High";
        if (score >= 0.4) return "Medium";
        if (score >= 0.2) return "Low";
        return "Very Low";
    }
}

/// <summary>
/// Strategy export request
/// </summary>
public class StrategyExportRequest
{
    public string StrategyCode { get; set; } = string.Empty;
    public string? ProjectName { get; set; }
}
