using System.ComponentModel.DataAnnotations;

namespace AlgoTrendy.Core.Models;

/// <summary>
/// User account with KYC (Know Your Customer) information for compliance
/// </summary>
public class User
{
    /// <summary>
    /// Unique user identifier
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Username for authentication
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Email address
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Full legal name
    /// </summary>
    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Date of birth (for age verification)
    /// </summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// Residential address
    /// </summary>
    [StringLength(200)]
    public string? Address { get; set; }

    /// <summary>
    /// City
    /// </summary>
    [StringLength(100)]
    public string? City { get; set; }

    /// <summary>
    /// State/Province
    /// </summary>
    [StringLength(50)]
    public string? State { get; set; }

    /// <summary>
    /// Postal/ZIP code
    /// </summary>
    [StringLength(20)]
    public string? PostalCode { get; set; }

    /// <summary>
    /// Country (ISO 3166-1 alpha-2)
    /// </summary>
    [StringLength(2)]
    public string? Country { get; set; }

    /// <summary>
    /// Phone number
    /// </summary>
    [Phone]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Tax ID / SSN (encrypted in database)
    /// </summary>
    [StringLength(50)]
    public string? TaxId { get; set; }

    /// <summary>
    /// KYC verification status
    /// </summary>
    public KYCStatus KYCStatus { get; set; } = KYCStatus.Pending;

    /// <summary>
    /// Date KYC was completed
    /// </summary>
    public DateTime? KYCCompletedAt { get; set; }

    /// <summary>
    /// AML (Anti-Money Laundering) status
    /// </summary>
    public AMLStatus AMLStatus { get; set; } = AMLStatus.Clean;

    /// <summary>
    /// Date of last AML check
    /// </summary>
    public DateTime? LastAMLCheck { get; set; }

    /// <summary>
    /// OFAC sanctions screening status
    /// </summary>
    public bool SanctionsScreened { get; set; } = false;

    /// <summary>
    /// Date of last OFAC screening
    /// </summary>
    public DateTime? LastSanctionsCheck { get; set; }

    /// <summary>
    /// Whether user is on any sanctions list
    /// </summary>
    public bool IsSanctioned { get; set; } = false;

    /// <summary>
    /// Risk level for compliance monitoring
    /// </summary>
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;

    /// <summary>
    /// Account enabled status
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Account creation date
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last account update
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional metadata (JSON)
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Multi-factor authentication settings for this user
    /// </summary>
    public UserMfaSettings? MfaSettings { get; set; }
}

/// <summary>
/// KYC verification status
/// </summary>
public enum KYCStatus
{
    Pending,
    Submitted,
    UnderReview,
    Approved,
    Rejected,
    Expired
}

/// <summary>
/// AML status levels
/// </summary>
public enum AMLStatus
{
    Clean,
    PendingReview,
    Flagged,
    UnderInvestigation,
    Blocked
}

/// <summary>
/// User risk levels for compliance monitoring
/// </summary>
public enum RiskLevel
{
    Low,
    Medium,
    High,
    Critical
}
