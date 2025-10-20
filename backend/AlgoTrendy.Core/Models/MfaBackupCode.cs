using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlgoTrendy.Core.Models;

/// <summary>
/// Backup code for MFA recovery
/// Allows users to regain access if they lose their authenticator device
/// </summary>
public class MfaBackupCode
{
    /// <summary>
    /// Unique identifier for the backup code
    /// </summary>
    [Key]
    public Guid BackupCodeId { get; set; }

    /// <summary>
    /// MFA settings ID this backup code belongs to
    /// </summary>
    [Required]
    public Guid MfaSettingsId { get; set; }

    /// <summary>
    /// Navigation property to UserMfaSettings
    /// </summary>
    [ForeignKey(nameof(MfaSettingsId))]
    public UserMfaSettings? MfaSettings { get; set; }

    /// <summary>
    /// Hashed backup code (never store plaintext)
    /// Uses bcrypt or similar strong hashing
    /// </summary>
    [Required]
    [StringLength(500)]
    public string CodeHash { get; set; } = string.Empty;

    /// <summary>
    /// Whether this backup code has been used
    /// Once used, it cannot be reused
    /// </summary>
    public bool IsUsed { get; set; } = false;

    /// <summary>
    /// Date when the code was used (if applicable)
    /// </summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// IP address from which the code was used (for audit trail)
    /// </summary>
    [StringLength(45)] // IPv6 max length
    public string? UsedFromIp { get; set; }

    /// <summary>
    /// Code creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional expiration date for backup codes
    /// Recommended to expire after 30-90 days for security
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Check if the backup code is still valid (not used and not expired)
    /// </summary>
    public bool IsValid()
    {
        if (IsUsed) return false;
        if (ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow) return false;
        return true;
    }

    /// <summary>
    /// Mark this backup code as used
    /// </summary>
    public void MarkAsUsed(string? ipAddress = null)
    {
        IsUsed = true;
        UsedAt = DateTime.UtcNow;
        UsedFromIp = ipAddress;
    }
}
