using System.ComponentModel.DataAnnotations;

namespace AlgoTrendy.Core.Models;

/// <summary>
/// SEC/FINRA regulatory report tracking
/// </summary>
public class RegulatoryReport
{
    /// <summary>
    /// Unique report identifier
    /// </summary>
    public Guid ReportId { get; set; }

    /// <summary>
    /// Report type (Form PF, 13F, CAT, etc.)
    /// </summary>
    [Required]
    public ReportType ReportType { get; set; }

    /// <summary>
    /// Reporting period start date
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// Reporting period end date
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Report status
    /// </summary>
    public ReportStatus Status { get; set; } = ReportStatus.Draft;

    /// <summary>
    /// Report file path
    /// </summary>
    [StringLength(500)]
    public string? FilePath { get; set; }

    /// <summary>
    /// Report format (XML, JSON, CSV, XBRL)
    /// </summary>
    [StringLength(20)]
    public string Format { get; set; } = "XML";

    /// <summary>
    /// Report content (for small reports) or summary
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Hash/checksum for integrity verification
    /// </summary>
    [StringLength(64)]
    public string? FileHash { get; set; }

    /// <summary>
    /// User who generated the report
    /// </summary>
    public Guid? GeneratedBy { get; set; }

    /// <summary>
    /// Report generation timestamp
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Compliance officer who approved
    /// </summary>
    public Guid? ApprovedBy { get; set; }

    /// <summary>
    /// Approval timestamp
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Submission timestamp
    /// </summary>
    public DateTime? SubmittedAt { get; set; }

    /// <summary>
    /// Regulator confirmation/receipt number
    /// </summary>
    [StringLength(100)]
    public string? ConfirmationNumber { get; set; }

    /// <summary>
    /// Submission response from regulator
    /// </summary>
    public string? SubmissionResponse { get; set; }

    /// <summary>
    /// Error messages if generation/submission failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Metadata (JSON)
    /// </summary>
    public string? Metadata { get; set; }
}

/// <summary>
/// Regulatory report types
/// </summary>
public enum ReportType
{
    /// <summary>
    /// SEC Form PF - Private Fund reporting
    /// </summary>
    FormPF,

    /// <summary>
    /// SEC Form 13F - Institutional investment manager holdings
    /// </summary>
    Form13F,

    /// <summary>
    /// FINRA Consolidated Audit Trail
    /// </summary>
    CAT,

    /// <summary>
    /// FINRA Trade Reporting and Compliance Engine
    /// </summary>
    TRACE,

    /// <summary>
    /// FINRA Order Audit Trail System
    /// </summary>
    OATS,

    /// <summary>
    /// SEC Form 4 - Insider trading
    /// </summary>
    Form4,

    /// <summary>
    /// Large Trader Report
    /// </summary>
    LargeTrader,

    /// <summary>
    /// Suspicious Activity Report
    /// </summary>
    SAR,

    /// <summary>
    /// Custom compliance report
    /// </summary>
    Custom
}

/// <summary>
/// Report status lifecycle
/// </summary>
public enum ReportStatus
{
    Draft,
    Generated,
    UnderReview,
    Approved,
    Rejected,
    Submitted,
    Accepted,
    Failed,
    Archived
}
