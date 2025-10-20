using AlgoTrendy.API.DTOs;
using AlgoTrendy.Core.Models;
using AlgoTrendy.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AlgoTrendy.API.Controllers;

/// <summary>
/// Multi-Factor Authentication (MFA) controller
/// Provides endpoints for MFA enrollment, verification, and management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MfaController : ControllerBase
{
    private readonly ILogger<MfaController> _logger;
    private readonly MfaService _mfaService;
    // TODO: Add database context/repository for persisting MFA settings
    // private readonly IUserRepository _userRepository;
    // private readonly IMfaRepository _mfaRepository;

    // Temporary in-memory storage (replace with database)
    private static readonly Dictionary<Guid, UserMfaSettings> _mfaSettings = new();
    private static readonly Dictionary<string, Guid> _emailToUserId = new();

    public MfaController(
        ILogger<MfaController> logger,
        MfaService mfaService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mfaService = mfaService ?? throw new ArgumentNullException(nameof(mfaService));
    }

    /// <summary>
    /// Get MFA status for the authenticated user
    /// </summary>
    /// <returns>MFA status information</returns>
    /// <response code="200">Returns MFA status</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">User not found</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(MfaStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MfaStatusResponse>> GetMfaStatus()
    {
        try
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            _logger.LogInformation("Getting MFA status for user: {Email}", userEmail);

            // TODO: Get user from database
            if (!_emailToUserId.TryGetValue(userEmail, out var userId))
            {
                return Ok(new MfaStatusResponse
                {
                    IsEnabled = false,
                    BackupCodesRemaining = 0
                });
            }

            if (!_mfaSettings.TryGetValue(userId, out var mfaSettings))
            {
                return Ok(new MfaStatusResponse
                {
                    IsEnabled = false,
                    BackupCodesRemaining = 0
                });
            }

            var response = new MfaStatusResponse
            {
                IsEnabled = mfaSettings.IsEnabled,
                MfaMethod = mfaSettings.MfaMethod,
                DeviceName = mfaSettings.DeviceName,
                EnabledAt = mfaSettings.EnabledAt,
                LastUsedAt = mfaSettings.LastUsedAt,
                BackupCodesRemaining = mfaSettings.BackupCodesRemaining
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get MFA status");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Initiate MFA enrollment for a user
    /// Returns QR code and secret for setting up authenticator app
    /// </summary>
    /// <param name="request">Enrollment request containing user email</param>
    /// <returns>QR code and secret for MFA setup</returns>
    /// <response code="200">Returns enrollment information with QR code</response>
    /// <response code="400">Invalid request</response>
    [HttpPost("enroll/initiate")]
    [ProducesResponseType(typeof(MfaEnrollResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MfaEnrollResponse>> InitiateEnrollment(
        [FromBody] MfaEnrollRequest request)
    {
        try
        {
            _logger.LogInformation("Initiating MFA enrollment for: {Email}", request.Email);

            var result = _mfaService.InitiateEnrollment(request.Email);

            var response = new MfaEnrollResponse
            {
                Secret = result.Secret,
                QrCodeUri = result.QrCodeUri,
                QrCodeBase64 = result.QrCodeBase64
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate MFA enrollment");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Complete MFA enrollment by verifying the first TOTP code
    /// Returns backup codes that should be saved securely
    /// </summary>
    /// <param name="request">Request containing secret and verification code</param>
    /// <returns>Backup codes for account recovery</returns>
    /// <response code="200">MFA enabled successfully with backup codes</response>
    /// <response code="400">Invalid verification code or request</response>
    [HttpPost("enroll/complete")]
    [ProducesResponseType(typeof(MfaEnrollCompleteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MfaEnrollCompleteResponse>> CompleteEnrollment(
        [FromBody] MfaEnrollCompleteRequest request)
    {
        try
        {
            _logger.LogInformation("Completing MFA enrollment");

            // TODO: Get authenticated user from context
            // For now, create a demo user ID
            var userId = Guid.NewGuid();
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "demo@algotrendy.com";

            var result = _mfaService.CompleteEnrollment(
                userId,
                request.Secret,
                request.VerificationCode,
                request.DeviceName
            );

            if (!result.Success || result.MfaSettings == null)
            {
                return BadRequest(new { error = result.Message });
            }

            // Store MFA settings (TODO: persist to database)
            _mfaSettings[userId] = result.MfaSettings;
            _emailToUserId[userEmail] = userId;

            var response = new MfaEnrollCompleteResponse
            {
                Success = true,
                BackupCodes = result.BackupCodes,
                Message = result.Message
            };

            _logger.LogInformation("MFA enabled successfully for user: {UserId}", userId);

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("MFA enrollment failed: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete MFA enrollment");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Verify MFA code (TOTP or backup code)
    /// Used during login or for sensitive operations
    /// </summary>
    /// <param name="request">Verification request with email and code</param>
    /// <returns>Verification result</returns>
    /// <response code="200">Verification result (success or failure)</response>
    /// <response code="400">Invalid request</response>
    /// <response code="404">User not found or MFA not enabled</response>
    [HttpPost("verify")]
    [ProducesResponseType(typeof(MfaVerifyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MfaVerifyResponse>> VerifyMfaCode(
        [FromBody] MfaVerifyRequest request)
    {
        try
        {
            _logger.LogInformation("Verifying MFA code for: {Email}", request.Email);

            // TODO: Get user and MFA settings from database
            if (!_emailToUserId.TryGetValue(request.Email, out var userId))
            {
                return NotFound(new { error = "User not found" });
            }

            if (!_mfaSettings.TryGetValue(userId, out var mfaSettings))
            {
                return NotFound(new { error = "MFA not enabled for this user" });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = _mfaService.VerifyMfaCode(mfaSettings, request.Code, ipAddress);

            // TODO: Update MFA settings in database
            _mfaSettings[userId] = mfaSettings;

            var response = new MfaVerifyResponse
            {
                Success = result.Success,
                UsedBackupCode = result.UsedBackupCode,
                BackupCodesRemaining = result.BackupCodesRemaining,
                IsLocked = result.IsLocked,
                Message = result.Message,
                // TODO: Generate JWT token on successful verification
                Token = result.Success ? "jwt-token-here" : null
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify MFA code");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Regenerate backup codes (invalidates old codes)
    /// Requires authentication and current MFA verification
    /// </summary>
    /// <returns>New backup codes</returns>
    /// <response code="200">Returns new backup codes</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">MFA not enabled</response>
    [HttpPost("backup-codes/regenerate")]
    [ProducesResponseType(typeof(MfaBackupCodesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MfaBackupCodesResponse>> RegenerateBackupCodes()
    {
        try
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            _logger.LogInformation("Regenerating backup codes for: {Email}", userEmail);

            // TODO: Get user and MFA settings from database
            if (!_emailToUserId.TryGetValue(userEmail, out var userId))
            {
                return NotFound(new { error = "User not found" });
            }

            if (!_mfaSettings.TryGetValue(userId, out var mfaSettings))
            {
                return NotFound(new { error = "MFA not enabled for this user" });
            }

            var newBackupCodes = _mfaService.RegenerateBackupCodes(mfaSettings);

            // TODO: Update MFA settings in database
            _mfaSettings[userId] = mfaSettings;

            var response = new MfaBackupCodesResponse
            {
                Success = true,
                BackupCodes = newBackupCodes,
                Message = "Backup codes regenerated successfully"
            };

            _logger.LogInformation("Backup codes regenerated for user: {UserId}", userId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to regenerate backup codes");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Disable MFA for the authenticated user
    /// Requires verification code to confirm
    /// </summary>
    /// <param name="request">Disable request with verification code</param>
    /// <returns>Disable result</returns>
    /// <response code="200">MFA disabled successfully</response>
    /// <response code="400">Invalid verification code</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">MFA not enabled</response>
    [HttpPost("disable")]
    [ProducesResponseType(typeof(MfaDisableResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MfaDisableResponse>> DisableMfa(
        [FromBody] MfaDisableRequest request)
    {
        try
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            _logger.LogInformation("Disabling MFA for: {Email}", userEmail);

            // TODO: Get user and MFA settings from database
            if (!_emailToUserId.TryGetValue(userEmail, out var userId))
            {
                return NotFound(new { error = "User not found" });
            }

            if (!_mfaSettings.TryGetValue(userId, out var mfaSettings))
            {
                return NotFound(new { error = "MFA not enabled for this user" });
            }

            var success = _mfaService.DisableMfa(mfaSettings, request.VerificationCode);

            if (!success)
            {
                return BadRequest(new { error = "Invalid verification code" });
            }

            // TODO: Update MFA settings in database
            _mfaSettings[userId] = mfaSettings;

            var response = new MfaDisableResponse
            {
                Success = true,
                Message = "MFA disabled successfully"
            };

            _logger.LogInformation("MFA disabled for user: {UserId}", userId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable MFA");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
