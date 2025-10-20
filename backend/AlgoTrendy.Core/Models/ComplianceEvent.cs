using System.ComponentModel.DataAnnotations;

namespace AlgoTrendy.Core.Models;

/// <summary>
/// Compliance and regulatory event for audit trail
/// </summary>
public class ComplianceEvent
{
    /// <summary>
    /// Unique event identifier
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Type of compliance event
    /// </summary>
    [Required]
    public ComplianceEventType EventType { get; set; }

    /// <summary>
    /// Severity level
    /// </summary>
    public ComplianceSeverity Severity { get; set; } = ComplianceSeverity.Info;

    /// <summary>
    /// User ID associated with this event
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Order ID if related to trading
    /// </summary>
    public Guid? OrderId { get; set; }

    /// <summary>
    /// Trade ID if related to execution
    /// </summary>
    public Guid? TradeId { get; set; }

    /// <summary>
    /// Event title/summary
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description
    /// </summary>
    [StringLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// Event data (JSON)
    /// </summary>
    public string? EventData { get; set; }

    /// <summary>
    /// Source system/service
    /// </summary>
    [StringLength(100)]
    public string? Source { get; set; }

    /// <summary>
    /// Compliance officer who reviewed (if applicable)
    /// </summary>
    public Guid? ReviewedBy { get; set; }

    /// <summary>
    /// Review status
    /// </summary>
    public ReviewStatus ReviewStatus { get; set; } = ReviewStatus.Pending;

    /// <summary>
    /// Review notes
    /// </summary>
    [StringLength(1000)]
    public string? ReviewNotes { get; set; }

    /// <summary>
    /// Date reviewed
    /// </summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// Whether event requires action
    /// </summary>
    public bool RequiresAction { get; set; } = false;

    /// <summary>
    /// Action taken
    /// </summary>
    [StringLength(500)]
    public string? ActionTaken { get; set; }

    /// <summary>
    /// Event timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Correlation ID for distributed tracing
    /// </summary>
    [StringLength(100)]
    public string? CorrelationId { get; set; }
}

/// <summary>
/// Types of compliance events
/// </summary>
public enum ComplianceEventType
{
    // AML Events
    AMLTransactionFlagged,
    AMLHighValueTransaction,
    AMLRapidTransactions,
    AMLSuspiciousPattern,
    AMLAccountBlocked,

    // OFAC Events
    OFACSanctionsMatch,
    OFACScreeningFailed,
    OFACListUpdated,

    // Trade Surveillance
    PumpAndDumpDetected,
    SpoofingDetected,
    WashTradingDetected,
    FrontRunningDetected,
    MarketManipulation,
    UnusualVolumeActivity,
    PriceManipulation,

    // Regulatory Reporting
    FormPFGenerated,
    Form13FGenerated,
    CATReportGenerated,
    ReportSubmissionFailed,
    ReportSubmitted,

    // KYC/Identity
    KYCSubmitted,
    KYCApproved,
    KYCRejected,
    IdentityVerificationFailed,

    // Risk Management
    PositionLimitExceeded,
    ExposureLimitExceeded,
    MarginCallTriggered,
    LiquidationEvent,

    // Data Retention
    DataArchived,
    DataPurged,
    RetentionPolicyViolation,

    // General
    ComplianceViolation,
    PolicyChange,
    AuditLog,
    ManualReview
}

/// <summary>
/// Compliance event severity levels
/// </summary>
public enum ComplianceSeverity
{
    Info,
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Review status for compliance events
/// </summary>
public enum ReviewStatus
{
    Pending,
    UnderReview,
    Approved,
    Rejected,
    Escalated,
    Resolved
}
