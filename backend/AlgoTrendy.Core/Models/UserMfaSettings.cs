using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlgoTrendy.Core.Models;

/// <summary>
/// Multi-factor authentication settings for a user
/// Stores TOTP configuration and backup codes
/// </summary>
public class UserMfaSettings
{
    /// <summary>
    /// Unique identifier for MFA settings
    /// </summary>
    [Key]
    public Guid MfaSettingsId { get; set; }

    /// <summary>
    /// User ID this MFA configuration belongs to
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to User
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>
    /// Whether MFA is enabled for this user
    /// </summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// Encrypted TOTP secret key (base32 encoded)
    /// Used to generate time-based one-time passwords
    /// </summary>
    [Required]
    [StringLength(500)]
    public string TotpSecretEncrypted { get; set; } = string.Empty;

    /// <summary>
    /// Date when MFA was first enabled
    /// </summary>
    public DateTime? EnabledAt { get; set; }

    /// <summary>
    /// Date when MFA was last used successfully
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Number of backup codes remaining
    /// </summary>
    public int BackupCodesRemaining { get; set; } = 0;

    /// <summary>
    /// Collection of backup codes for account recovery
    /// </summary>
    public ICollection<MfaBackupCode> BackupCodes { get; set; } = new List<MfaBackupCode>();

    /// <summary>
    /// Number of consecutive failed MFA attempts
    /// Used for security monitoring and account lockout
    /// </summary>
    public int FailedAttempts { get; set; } = 0;

    /// <summary>
    /// Date when account was locked due to failed MFA attempts
    /// </summary>
    public DateTime? LockedUntil { get; set; }

    /// <summary>
    /// MFA method type (TOTP, SMS, Email, etc.)
    /// Currently supporting TOTP only
    /// </summary>
    [StringLength(20)]
    public string MfaMethod { get; set; } = "TOTP";

    /// <summary>
    /// Device/app name used for MFA (for user reference)
    /// e.g., "Google Authenticator on iPhone"
    /// </summary>
    [StringLength(100)]
    public string? DeviceName { get; set; }

    /// <summary>
    /// Record creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Check if account is currently locked due to failed attempts
    /// </summary>
    public bool IsLocked()
    {
        return LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;
    }

    /// <summary>
    /// Reset failed attempts counter
    /// </summary>
    public void ResetFailedAttempts()
    {
        FailedAttempts = 0;
        LockedUntil = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Increment failed attempts and apply lockout if threshold exceeded
    /// </summary>
    public void IncrementFailedAttempts(int lockoutThreshold = 5, int lockoutMinutes = 15)
    {
        FailedAttempts++;
        UpdatedAt = DateTime.UtcNow;

        if (FailedAttempts >= lockoutThreshold)
        {
            LockedUntil = DateTime.UtcNow.AddMinutes(lockoutMinutes);
        }
    }
}
