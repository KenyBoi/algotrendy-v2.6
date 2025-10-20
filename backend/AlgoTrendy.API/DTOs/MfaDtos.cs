using System.ComponentModel.DataAnnotations;

namespace AlgoTrendy.API.DTOs;

/// <summary>
/// Request to initiate MFA enrollment
/// </summary>
public class MfaEnrollRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Response containing QR code and secret for MFA enrollment
/// </summary>
public class MfaEnrollResponse
{
    /// <summary>
    /// Base32-encoded secret (for manual entry)
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// OTP Auth URI for QR code
    /// </summary>
    public string QrCodeUri { get; set; } = string.Empty;

    /// <summary>
    /// Base64-encoded PNG QR code image
    /// Can be used in HTML: <img src="data:image/png;base64,{QrCodeBase64}" />
    /// </summary>
    public string QrCodeBase64 { get; set; } = string.Empty;

    /// <summary>
    /// Instructions for the user
    /// </summary>
    public string Instructions { get; set; } = "Scan the QR code with your authenticator app (Google Authenticator, Authy, etc.) or enter the secret manually. Then verify with a code to complete setup.";
}

/// <summary>
/// Request to complete MFA enrollment with verification code
/// </summary>
public class MfaEnrollCompleteRequest
{
    /// <summary>
    /// The secret provided during enrollment initiation
    /// </summary>
    [Required]
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// 6-digit verification code from authenticator app
    /// </summary>
    [Required]
    [StringLength(6, MinimumLength = 6)]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Verification code must be 6 digits")]
    public string VerificationCode { get; set; } = string.Empty;

    /// <summary>
    /// Optional device name for user reference
    /// </summary>
    [StringLength(100)]
    public string? DeviceName { get; set; }
}

/// <summary>
/// Response after completing MFA enrollment
/// </summary>
public class MfaEnrollCompleteResponse
{
    /// <summary>
    /// Whether MFA was successfully enabled
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Backup codes for account recovery (shown only once!)
    /// </summary>
    public List<string> BackupCodes { get; set; } = new();

    /// <summary>
    /// Message to display to user
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Important warning about backup codes
    /// </summary>
    public string Warning { get; set; } = "Save these backup codes in a secure location. Each code can only be used once and will not be shown again.";
}

/// <summary>
/// Request to verify MFA code during login
/// </summary>
public class MfaVerifyRequest
{
    /// <summary>
    /// User email for authentication
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 6-digit TOTP code or 8-character backup code (XXXX-XXXX)
    /// </summary>
    [Required]
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// Response after MFA verification
/// </summary>
public class MfaVerifyResponse
{
    /// <summary>
    /// Whether verification was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Whether a backup code was used
    /// </summary>
    public bool UsedBackupCode { get; set; }

    /// <summary>
    /// Number of backup codes remaining (if backup code was used)
    /// </summary>
    public int? BackupCodesRemaining { get; set; }

    /// <summary>
    /// Whether account is locked due to failed attempts
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// Message to display to user
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// JWT token (if verification successful)
    /// </summary>
    public string? Token { get; set; }
}

/// <summary>
/// Request to disable MFA
/// </summary>
public class MfaDisableRequest
{
    /// <summary>
    /// 6-digit verification code to confirm disable action
    /// </summary>
    [Required]
    [StringLength(6, MinimumLength = 6)]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Verification code must be 6 digits")]
    public string VerificationCode { get; set; } = string.Empty;
}

/// <summary>
/// Response after disabling MFA
/// </summary>
public class MfaDisableResponse
{
    /// <summary>
    /// Whether MFA was successfully disabled
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message to display to user
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response containing MFA status for a user
/// </summary>
public class MfaStatusResponse
{
    /// <summary>
    /// Whether MFA is enabled
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// MFA method (TOTP, SMS, etc.)
    /// </summary>
    public string? MfaMethod { get; set; }

    /// <summary>
    /// Device name (if set)
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// Date MFA was enabled
    /// </summary>
    public DateTime? EnabledAt { get; set; }

    /// <summary>
    /// Date MFA was last used
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Number of backup codes remaining
    /// </summary>
    public int BackupCodesRemaining { get; set; }
}

/// <summary>
/// Response containing regenerated backup codes
/// </summary>
public class MfaBackupCodesResponse
{
    /// <summary>
    /// Whether backup codes were successfully regenerated
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// New backup codes (shown only once!)
    /// </summary>
    public List<string> BackupCodes { get; set; } = new();

    /// <summary>
    /// Message to display to user
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Warning about old codes being invalidated
    /// </summary>
    public string Warning { get; set; } = "All previous backup codes have been invalidated. Save these new codes in a secure location.";
}

/// <summary>
/// Updated login request with optional MFA code
/// </summary>
public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Optional MFA code (if user has MFA enabled)
    /// </summary>
    public string? MfaCode { get; set; }
}

/// <summary>
/// Login response with MFA status
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// Whether login was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Whether MFA is required to complete login
    /// </summary>
    public bool MfaRequired { get; set; }

    /// <summary>
    /// JWT token (only provided after successful authentication including MFA if required)
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// User information (only provided after successful authentication)
    /// </summary>
    public UserDto? User { get; set; }

    /// <summary>
    /// Message to display to user
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Basic user information DTO
/// </summary>
public class UserDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool MfaEnabled { get; set; }
}
