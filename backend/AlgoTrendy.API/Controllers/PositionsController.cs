using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace AlgoTrendy.API.Controllers;

/// <summary>
/// Positions management controller
/// Provides endpoints for querying and managing active positions
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PositionsController : ControllerBase
{
    private readonly ILogger<PositionsController> _logger;
    private readonly IPositionRepository _positionRepository;

    public PositionsController(
        ILogger<PositionsController> logger,
        IPositionRepository positionRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _positionRepository = positionRepository ?? throw new ArgumentNullException(nameof(positionRepository));
    }

    /// <summary>
    /// Gets all active positions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active positions</returns>
    /// <response code="200">Returns the list of positions</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Position>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<Position>>> GetPositions(
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving all active positions");

            var positions = await _positionRepository.GetAllActiveAsync(cancellationToken);

            _logger.LogInformation("Retrieved {Count} positions", positions.Count());

            return Ok(positions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve positions");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets a position by symbol
    /// </summary>
    /// <param name="symbol">Trading symbol (e.g., BTCUSDT)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Position for the specified symbol</returns>
    /// <response code="200">Returns the position</response>
    /// <response code="404">Position not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{symbol}")]
    [ProducesResponseType(typeof(Position), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Position>> GetPosition(
        string symbol,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving position for symbol: {Symbol}", symbol);

            var position = await _positionRepository.GetBySymbolAsync(symbol, cancellationToken);

            if (position == null)
            {
                _logger.LogWarning("Position not found for symbol: {Symbol}", symbol);
                return NotFound(new { error = $"Position not found for symbol {symbol}" });
            }

            _logger.LogInformation("Retrieved position for symbol: {Symbol}", symbol);

            return Ok(position);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve position for symbol: {Symbol}", symbol);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Closes a position by symbol (deletes it from active positions)
    /// </summary>
    /// <param name="symbol">Trading symbol (e.g., BTCUSDT)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Position closed successfully</response>
    /// <response code="404">Position not found</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete("{symbol}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> ClosePosition(
        string symbol,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Closing position for symbol: {Symbol}", symbol);

            await _positionRepository.DeleteBySymbolAsync(symbol, cancellationToken);

            _logger.LogInformation("Position closed successfully for symbol: {Symbol}", symbol);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Position not found for symbol: {Symbol}", symbol);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to close position for symbol: {Symbol}", symbol);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
