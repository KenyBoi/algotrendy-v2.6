using AlgoTrendy.Backtesting.Engines;
using AlgoTrendy.Backtesting.Indicators;
using AlgoTrendy.Backtesting.Models;
using AlgoTrendy.Backtesting.Services;
using Microsoft.AspNetCore.Mvc;

namespace AlgoTrendy.API.Controllers;

/// <summary>
/// API controller for backtesting operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class BacktestingController : ControllerBase
{
    private readonly IBacktestService _backtestService;
    private readonly BacktestEngineFactory? _engineFactory;
    private readonly ILogger<BacktestingController> _logger;

    public BacktestingController(
        IBacktestService backtestService,
        ILogger<BacktestingController> logger,
        BacktestEngineFactory? engineFactory = null)
    {
        _backtestService = backtestService ?? throw new ArgumentNullException(nameof(backtestService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _engineFactory = engineFactory; // Optional - may be null if not configured
    }

    /// <summary>
    /// Get backtesting configuration options for UI
    /// </summary>
    /// <returns>Configuration options including asset classes, timeframes, and indicators</returns>
    [HttpGet("config")]
    [ProducesResponseType(typeof(BacktestConfigOptions), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BacktestConfigOptions>> GetConfigAsync()
    {
        try
        {
            _logger.LogInformation("Fetching backtest configuration options");
            var config = await _backtestService.GetConfigOptionsAsync();
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching configuration options");
            return StatusCode(500, "An error occurred while fetching configuration options");
        }
    }

    /// <summary>
    /// Run a backtest with the given configuration
    /// </summary>
    /// <param name="config">Backtest configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Backtest results with metrics and trades</returns>
    [HttpPost("run")]
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
                return BadRequest("Backtest configuration is required");

            _logger.LogInformation(
                "Running backtest for {Symbol} ({AssetClass}) from {StartDate} to {EndDate}",
                config.Symbol, config.AssetClass, config.StartDate, config.EndDate);

            var results = await _backtestService.RunBacktestAsync(config, cancellationToken);

            _logger.LogInformation("Backtest completed: {BacktestId}", results.BacktestId);

            return Ok(results);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Backtest was cancelled");
            return StatusCode(499, "Backtest was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running backtest");
            return StatusCode(500, "An error occurred while running the backtest");
        }
    }

    /// <summary>
    /// Get backtest results by ID
    /// </summary>
    /// <param name="id">Backtest ID</param>
    /// <returns>Backtest results if found</returns>
    [HttpGet("results/{id}")]
    [ProducesResponseType(typeof(BacktestResults), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BacktestResults>> GetResultsAsync(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("Backtest ID is required");

            _logger.LogInformation("Fetching backtest results for {BacktestId}", id);

            var results = await _backtestService.GetBacktestResultsAsync(id);

            if (results == null)
                return NotFound($"Backtest with ID {id} not found");

            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching backtest results for {BacktestId}", id);
            return StatusCode(500, "An error occurred while fetching backtest results");
        }
    }

    /// <summary>
    /// Get backtest history (list of recent backtests)
    /// </summary>
    /// <param name="limit">Maximum number of results (default 50)</param>
    /// <returns>List of backtest history items</returns>
    [HttpGet("history")]
    [ProducesResponseType(typeof(List<BacktestHistoryItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<BacktestHistoryItem>>> GetHistoryAsync([FromQuery] int limit = 50)
    {
        try
        {
            if (limit < 1 || limit > 500)
                return BadRequest("Limit must be between 1 and 500");

            _logger.LogInformation("Fetching backtest history with limit {Limit}", limit);

            var history = await _backtestService.GetBacktestHistoryAsync(limit);

            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching backtest history");
            return StatusCode(500, "An error occurred while fetching backtest history");
        }
    }

    /// <summary>
    /// Get available technical indicators and their metadata
    /// </summary>
    /// <returns>Dictionary of available indicators with configuration options</returns>
    [HttpGet("indicators")]
    [ProducesResponseType(typeof(Dictionary<string, Dictionary<string, object>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<Dictionary<string, Dictionary<string, object>>> GetIndicatorsAsync()
    {
        try
        {
            _logger.LogInformation("Fetching available indicators");

            var indicators = IndicatorMetadata.GetAvailableIndicators();

            return Ok(indicators);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching indicators");
            return StatusCode(500, "An error occurred while fetching indicators");
        }
    }

    /// <summary>
    /// Delete a backtest by ID
    /// </summary>
    /// <param name="id">Backtest ID to delete</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteAsync(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("Backtest ID is required");

            _logger.LogInformation("Deleting backtest {BacktestId}", id);

            var deleted = await _backtestService.DeleteBacktestAsync(id);

            if (!deleted)
                return NotFound($"Backtest with ID {id} not found");

            _logger.LogInformation("Backtest {BacktestId} deleted successfully", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting backtest {BacktestId}", id);
            return StatusCode(500, "An error occurred while deleting the backtest");
        }
    }

    /// <summary>
    /// Get available backtest engines (Cloud, Local LEAN, Custom)
    /// </summary>
    /// <returns>List of available engines with their availability status</returns>
    [HttpGet("engines")]
    [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult GetAvailableEngines()
    {
        try
        {
            _logger.LogInformation("Fetching available backtest engines");

            if (_engineFactory == null)
            {
                return Ok(new List<object>
                {
                    new { name = "Custom", description = "AlgoTrendy built-in engine", available = true }
                });
            }

            var engines = _engineFactory.GetAvailableEngines()
                .Select(e => new
                {
                    name = e.Name,
                    description = e.Description,
                    available = e.Available
                })
                .ToList();

            return Ok(engines);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching available engines");
            return StatusCode(500, "An error occurred while fetching engines");
        }
    }

    /// <summary>
    /// Run backtest with specific engine selection
    /// </summary>
    /// <param name="request">Backtest request with engine type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Backtest results</returns>
    [HttpPost("run/with-engine")]
    [ProducesResponseType(typeof(BacktestResults), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BacktestResults>> RunWithEngineAsync(
        [FromBody] BacktestWithEngineRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request?.Config == null)
                return BadRequest("Backtest configuration is required");

            if (_engineFactory == null)
                return BadRequest("Engine selection is not available - using default engine via /run endpoint");

            _logger.LogInformation(
                "Running backtest for {Symbol} with engine: {Engine}",
                request.Config.Symbol, request.EngineType);

            var engine = _engineFactory.GetEngine(request.EngineType);
            var results = await engine.RunAsync(request.Config, cancellationToken);

            _logger.LogInformation("Backtest completed: {BacktestId} using {Engine}",
                results.BacktestId, engine.EngineName);

            return Ok(results);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Backtest was cancelled");
            return StatusCode(499, "Backtest was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running backtest with engine");
            return StatusCode(500, $"An error occurred while running the backtest: {ex.Message}");
        }
    }
}

/// <summary>
/// Request model for running backtest with specific engine
/// </summary>
public class BacktestWithEngineRequest
{
    /// <summary>
    /// Backtest configuration
    /// </summary>
    public required BacktestConfig Config { get; set; }

    /// <summary>
    /// Engine type to use (Cloud, Local, Custom, Auto)
    /// </summary>
    public BacktestEngineType EngineType { get; set; } = BacktestEngineType.Auto;
}
