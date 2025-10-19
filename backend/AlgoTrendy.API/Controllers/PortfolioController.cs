using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using AlgoTrendy.Core.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AlgoTrendy.API.Controllers;

/// <summary>
/// Portfolio management controller for debt, margin, and leverage operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PortfolioController : ControllerBase
{
    private readonly IDebtManagementService _debtManagementService;
    private readonly ILogger<PortfolioController> _logger;

    public PortfolioController(
        IDebtManagementService debtManagementService,
        ILogger<PortfolioController> logger)
    {
        _debtManagementService = debtManagementService ?? throw new ArgumentNullException(nameof(debtManagementService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets comprehensive debt and margin summary for the account
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Debt summary with all leveraged positions and exposure</returns>
    /// <response code="200">Debt summary retrieved successfully</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("debt-summary")]
    [ProducesResponseType(typeof(DebtSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DebtSummary>> GetDebtSummaryAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Debt summary requested - OperationType: {OperationType}", "GetDebtSummary");

            var summary = await _debtManagementService.GetDebtSummaryAsync(cancellationToken);

            _logger.LogInformation(
                "Debt summary retrieved - TotalBorrowed: {TotalBorrowed}, TotalCollateral: {TotalCollateral}, " +
                "MarginHealthRatio: {MarginHealthRatio}, ActiveLeveragedPositions: {ActiveLeveragedPositions}, " +
                "TotalLeverageExposure: {TotalLeverageExposure}",
                summary.TotalBorrowedAmount, summary.TotalCollateralAmount, summary.MarginHealthRatio,
                summary.ActiveLeveragedPositions, summary.TotalLeverageExposure);

            if (summary.IsAtLiquidationRisk)
            {
                _logger.LogWarning(
                    "LIQUIDATION RISK DETECTED - MarginHealthRatio: {MarginHealthRatio}, TotalBorrowed: {TotalBorrowed}",
                    summary.MarginHealthRatio, summary.TotalBorrowedAmount);
            }

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve debt summary");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets leverage information for a specific trading symbol
    /// </summary>
    /// <param name="symbol">Trading symbol (e.g., "BTCUSDT")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Leverage info including current leverage, margin type, collateral, and liquidation price</returns>
    /// <response code="200">Leverage info retrieved successfully</response>
    /// <response code="404">Symbol not found or no leverage configured</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("leverage/{symbol}")]
    [ProducesResponseType(typeof(LeverageInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LeverageInfo>> GetLeverageInfoAsync(
        string symbol,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Leverage info requested - Symbol: {Symbol}, OperationType: {OperationType}",
                symbol, "GetLeverageInfo");

            var leverageInfo = await _debtManagementService.GetLeverageInfoAsync(symbol, cancellationToken);

            _logger.LogInformation(
                "Leverage info retrieved - Symbol: {Symbol}, CurrentLeverage: {CurrentLeverage}, " +
                "MarginType: {MarginType}, CollateralAmount: {CollateralAmount}, BorrowedAmount: {BorrowedAmount}, " +
                "LiquidationPrice: {LiquidationPrice}, MarginHealthRatio: {MarginHealthRatio}",
                symbol, leverageInfo.CurrentLeverage, leverageInfo.MarginType, leverageInfo.CollateralAmount,
                leverageInfo.BorrowedAmount, leverageInfo.LiquidationPrice, leverageInfo.MarginHealthRatio);

            if (leverageInfo.IsLiquidationRisk)
            {
                _logger.LogWarning(
                    "LIQUIDATION RISK - Symbol: {Symbol}, MarginHealthRatio: {MarginHealthRatio}, " +
                    "LiquidationPrice: {LiquidationPrice}",
                    symbol, leverageInfo.MarginHealthRatio, leverageInfo.LiquidationPrice);
            }

            return Ok(leverageInfo);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning(ex,
                "Leverage info not found - Symbol: {Symbol}, Reason: {Reason}",
                symbol, "SymbolNotFound");
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to retrieve leverage info - Symbol: {Symbol}",
                symbol);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Sets leverage for a trading symbol
    /// </summary>
    /// <param name="request">Leverage configuration request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated leverage information</returns>
    /// <response code="200">Leverage set successfully</response>
    /// <response code="400">Invalid leverage setting or exceeds limits</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("leverage")]
    [ProducesResponseType(typeof(LeverageInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LeverageInfo>> SetLeverageAsync(
        [FromBody] SetLeverageRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Leverage setting requested - Symbol: {Symbol}, Leverage: {Leverage}, " +
                "MarginType: {MarginType}, OperationType: {OperationType}",
                request.Symbol, request.Leverage, request.MarginType, "SetLeverage");

            // Validate leverage before setting
            var isValid = await _debtManagementService.ValidateLeverageAsync(
                request.Symbol,
                request.Leverage,
                cancellationToken);

            if (!isValid)
            {
                _logger.LogWarning(
                    "Invalid leverage setting - Symbol: {Symbol}, Leverage: {Leverage}, Reason: {Reason}",
                    request.Symbol, request.Leverage, "ExceedsLimits");
                return BadRequest(new { error = $"Leverage {request.Leverage}x exceeds allowed limits or violates risk constraints" });
            }

            var leverageInfo = await _debtManagementService.SetLeverageAsync(
                request.Symbol,
                request.Leverage,
                request.MarginType,
                cancellationToken);

            _logger.LogInformation(
                "Leverage set successfully - Symbol: {Symbol}, CurrentLeverage: {CurrentLeverage}, " +
                "MarginType: {MarginType}, LiquidationPrice: {LiquidationPrice}",
                request.Symbol, leverageInfo.CurrentLeverage, leverageInfo.MarginType, leverageInfo.LiquidationPrice);

            return Ok(leverageInfo);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex,
                "Failed to set leverage - Symbol: {Symbol}, Leverage: {Leverage}, Reason: {Reason}",
                request.Symbol, request.Leverage, ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error setting leverage - Symbol: {Symbol}, Leverage: {Leverage}",
                request.Symbol, request.Leverage);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets the account-wide margin health ratio
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Margin health ratio (0.0 to 1.0 scale, where &lt;0.05 indicates liquidation risk)</returns>
    /// <response code="200">Margin health ratio retrieved successfully</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("margin-health")]
    [ProducesResponseType(typeof(MarginHealthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MarginHealthResponse>> GetMarginHealthAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Margin health ratio requested - OperationType: {OperationType}", "GetMarginHealth");

            var marginHealthRatio = await _debtManagementService.GetMarginHealthRatioAsync(cancellationToken);

            _logger.LogInformation(
                "Margin health ratio retrieved - MarginHealthRatio: {MarginHealthRatio}, Status: {Status}",
                marginHealthRatio,
                marginHealthRatio < 0.05m ? "CRITICAL" : marginHealthRatio < 0.15m ? "WARNING" : "HEALTHY");

            if (marginHealthRatio < 0.05m)
            {
                _logger.LogCritical(
                    "CRITICAL: Account at immediate liquidation risk - MarginHealthRatio: {MarginHealthRatio}",
                    marginHealthRatio);
            }

            return Ok(new MarginHealthResponse
            {
                MarginHealthRatio = marginHealthRatio,
                Status = marginHealthRatio < 0.05m ? "CRITICAL" :
                         marginHealthRatio < 0.15m ? "WARNING" :
                         marginHealthRatio < 0.50m ? "CAUTION" : "HEALTHY",
                IsLiquidationRisk = marginHealthRatio < 0.05m,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve margin health ratio");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets total leverage exposure across all positions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Sum of all position exposure values</returns>
    /// <response code="200">Leverage exposure retrieved successfully</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("leverage-exposure")]
    [ProducesResponseType(typeof(LeverageExposureResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LeverageExposureResponse>> GetLeverageExposureAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Leverage exposure requested - OperationType: {OperationType}", "GetLeverageExposure");

            var totalExposure = await _debtManagementService.GetTotalLeverageExposureAsync(cancellationToken);

            _logger.LogInformation(
                "Leverage exposure retrieved - TotalExposure: {TotalExposure}",
                totalExposure);

            return Ok(new LeverageExposureResponse
            {
                TotalLeverageExposure = totalExposure,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve leverage exposure");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets all positions that are at risk of liquidation
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of positions with margin health ratio below liquidation threshold</returns>
    /// <response code="200">Liquidation risk positions retrieved successfully</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("liquidation-risk-positions")]
    [ProducesResponseType(typeof(IEnumerable<Position>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<Position>>> GetLiquidationRiskPositionsAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Liquidation risk positions requested - OperationType: {OperationType}", "GetLiquidationRiskPositions");

            var riskPositions = await _debtManagementService.GetLiquidationRiskPositionsAsync(cancellationToken);
            var riskPositionsList = riskPositions.ToList();

            _logger.LogInformation(
                "Liquidation risk positions retrieved - Count: {Count}",
                riskPositionsList.Count);

            if (riskPositionsList.Any())
            {
                _logger.LogWarning(
                    "LIQUIDATION RISK DETECTED - Positions at risk: {Count}, Symbols: {Symbols}",
                    riskPositionsList.Count,
                    string.Join(", ", riskPositionsList.Select(p => p.Symbol)));

                foreach (var position in riskPositionsList)
                {
                    _logger.LogWarning(
                        "Risk Position - Symbol: {Symbol}, PositionId: {PositionId}, " +
                        "MarginHealthRatio: {MarginHealthRatio}, LiquidationPrice: {LiquidationPrice}, " +
                        "CurrentPrice: {CurrentPrice}, UnrealizedPnL: {UnrealizedPnL}",
                        position.Symbol, position.PositionId, position.MarginHealthRatio,
                        position.LiquidationPrice, position.CurrentPrice, position.UnrealizedPnL);
                }
            }

            return Ok(riskPositionsList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve liquidation risk positions");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Validates if a leverage setting is allowed
    /// </summary>
    /// <param name="request">Leverage validation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result indicating if leverage is allowed</returns>
    /// <response code="200">Validation completed</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("validate-leverage")]
    [ProducesResponseType(typeof(LeverageValidationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LeverageValidationResponse>> ValidateLeverageAsync(
        [FromBody] ValidateLeverageRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Leverage validation requested - Symbol: {Symbol}, ProposedLeverage: {ProposedLeverage}",
                request.Symbol, request.ProposedLeverage);

            var isValid = await _debtManagementService.ValidateLeverageAsync(
                request.Symbol,
                request.ProposedLeverage,
                cancellationToken);

            _logger.LogInformation(
                "Leverage validation completed - Symbol: {Symbol}, ProposedLeverage: {ProposedLeverage}, IsValid: {IsValid}",
                request.Symbol, request.ProposedLeverage, isValid);

            return Ok(new LeverageValidationResponse
            {
                IsValid = isValid,
                Symbol = request.Symbol,
                ProposedLeverage = request.ProposedLeverage,
                Message = isValid
                    ? $"Leverage {request.ProposedLeverage}x is allowed for {request.Symbol}"
                    : $"Leverage {request.ProposedLeverage}x exceeds limits or violates risk constraints for {request.Symbol}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error validating leverage - Symbol: {Symbol}, ProposedLeverage: {ProposedLeverage}",
                request.Symbol, request.ProposedLeverage);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets margin configuration limits for the broker
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Margin configuration including max leverage, interest rates, and thresholds</returns>
    /// <response code="200">Margin configuration retrieved successfully</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("margin-configuration")]
    [ProducesResponseType(typeof(MarginConfiguration), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MarginConfiguration>> GetMarginConfigurationAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Margin configuration requested - OperationType: {OperationType}", "GetMarginConfiguration");

            var configuration = await _debtManagementService.GetMarginConfigurationAsync(cancellationToken);

            _logger.LogInformation(
                "Margin configuration retrieved - BrokerName: {BrokerName}, MaxLeverage: {MaxLeverage}, " +
                "MaintenanceMarginRatio: {MaintenanceMarginRatio}, InitialMarginRatio: {InitialMarginRatio}",
                configuration.BrokerName, configuration.MaxLeverageAllowed,
                configuration.MaintenanceMarginRatio, configuration.InitialMarginRatio);

            return Ok(configuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve margin configuration");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Closes a leveraged position and settles debt
    /// </summary>
    /// <param name="positionId">Position ID to close</param>
    /// <param name="request">Close position request with reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success indicator</returns>
    /// <response code="200">Position closed successfully</response>
    /// <response code="404">Position not found</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete("positions/{positionId}")]
    [ProducesResponseType(typeof(ClosePositionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ClosePositionResponse>> ClosePositionAsync(
        string positionId,
        [FromBody] ClosePositionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Position closure requested - PositionId: {PositionId}, Reason: {Reason}, OperationType: {OperationType}",
                positionId, request.Reason, "ClosePosition");

            // Note: This is a simplified implementation. In a real system, you'd need to:
            // 1. Fetch the position from a repository
            // 2. Pass it to the debt management service
            // For now, we'll assume the service handles position lookup internally

            // Create a placeholder position object (in real implementation, fetch from repository)
            var position = new Position
            {
                PositionId = positionId,
                Symbol = "PLACEHOLDER", // Would be fetched from repository
                Exchange = "PLACEHOLDER",
                Side = OrderSide.Buy,
                Quantity = 0,
                EntryPrice = 0,
                OpenedAt = DateTime.UtcNow
            };

            var success = await _debtManagementService.CloseLeveragedPositionAsync(
                position,
                request.Reason,
                cancellationToken);

            if (!success)
            {
                _logger.LogWarning(
                    "Failed to close position - PositionId: {PositionId}, Reason: {Reason}",
                    positionId, "ClosureFailed");
                return StatusCode(500, new { error = "Failed to close position" });
            }

            _logger.LogInformation(
                "Position closed successfully - PositionId: {PositionId}, Reason: {Reason}",
                positionId, request.Reason);

            return Ok(new ClosePositionResponse
            {
                Success = true,
                PositionId = positionId,
                Reason = request.Reason,
                ClosedAt = DateTime.UtcNow,
                Message = $"Position {positionId} closed successfully"
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning(ex,
                "Position not found - PositionId: {PositionId}",
                positionId);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error closing position - PositionId: {PositionId}",
                positionId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

/// <summary>
/// Request model for setting leverage on a symbol
/// </summary>
public class SetLeverageRequest
{
    /// <summary>
    /// Trading symbol (e.g., "BTCUSDT")
    /// </summary>
    public required string Symbol { get; set; }

    /// <summary>
    /// Leverage multiplier (e.g., 3.0 for 3x leverage)
    /// </summary>
    public required decimal Leverage { get; set; }

    /// <summary>
    /// Margin type (Cross or Isolated)
    /// </summary>
    public MarginType MarginType { get; set; } = MarginType.Cross;
}

/// <summary>
/// Request model for validating leverage
/// </summary>
public class ValidateLeverageRequest
{
    /// <summary>
    /// Trading symbol (e.g., "BTCUSDT")
    /// </summary>
    public required string Symbol { get; set; }

    /// <summary>
    /// Proposed leverage multiplier to validate
    /// </summary>
    public required decimal ProposedLeverage { get; set; }
}

/// <summary>
/// Response model for leverage validation
/// </summary>
public class LeverageValidationResponse
{
    /// <summary>
    /// Indicates if the proposed leverage is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Trading symbol
    /// </summary>
    public required string Symbol { get; set; }

    /// <summary>
    /// Proposed leverage that was validated
    /// </summary>
    public decimal ProposedLeverage { get; set; }

    /// <summary>
    /// Validation message
    /// </summary>
    public string? Message { get; set; }
}

/// <summary>
/// Response model for margin health status
/// </summary>
public class MarginHealthResponse
{
    /// <summary>
    /// Margin health ratio (0.0 to 1.0)
    /// </summary>
    public decimal MarginHealthRatio { get; set; }

    /// <summary>
    /// Health status ("HEALTHY", "CAUTION", "WARNING", "CRITICAL")
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Whether account is at liquidation risk
    /// </summary>
    public bool IsLiquidationRisk { get; set; }

    /// <summary>
    /// Timestamp when health was calculated
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Response model for leverage exposure
/// </summary>
public class LeverageExposureResponse
{
    /// <summary>
    /// Total leverage exposure across all positions
    /// </summary>
    public decimal TotalLeverageExposure { get; set; }

    /// <summary>
    /// Timestamp when exposure was calculated
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Request model for closing a position
/// </summary>
public class ClosePositionRequest
{
    /// <summary>
    /// Reason for closing position (e.g., "Manual", "Margin Call", "Liquidation")
    /// </summary>
    public required string Reason { get; set; }
}

/// <summary>
/// Response model for position closure
/// </summary>
public class ClosePositionResponse
{
    /// <summary>
    /// Indicates if position was closed successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Position ID that was closed
    /// </summary>
    public required string PositionId { get; set; }

    /// <summary>
    /// Reason for closure
    /// </summary>
    public required string Reason { get; set; }

    /// <summary>
    /// Timestamp when position was closed
    /// </summary>
    public DateTime ClosedAt { get; set; }

    /// <summary>
    /// Closure result message
    /// </summary>
    public string? Message { get; set; }
}
