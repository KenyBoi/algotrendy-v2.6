namespace AlgoTrendy.Core.Configuration;

/// <summary>
/// Compliance and regulatory settings for SEC, FINRA, AML, and OFAC requirements
/// </summary>
public class ComplianceSettings
{
    /// <summary>
    /// Master switch for all compliance features
    /// </summary>
    public bool EnableComplianceFeatures { get; set; } = true;

    /// <summary>
    /// AML (Anti-Money Laundering) settings
    /// </summary>
    public AMLSettings AML { get; set; } = new();

    /// <summary>
    /// OFAC (Office of Foreign Assets Control) sanctions screening settings
    /// </summary>
    public OFACSettings OFAC { get; set; } = new();

    /// <summary>
    /// Trade surveillance settings for market manipulation detection
    /// </summary>
    public TradeSurveillanceSettings TradeSurveillance { get; set; } = new();

    /// <summary>
    /// Regulatory reporting settings (SEC, FINRA)
    /// </summary>
    public RegulatoryReportingSettings RegulatoryReporting { get; set; } = new();

    /// <summary>
    /// Data retention policy settings
    /// </summary>
    public DataRetentionSettings DataRetention { get; set; } = new();
}

public class AMLSettings
{
    /// <summary>
    /// Enable AML transaction monitoring
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Transaction amount threshold for automatic flagging (USD)
    /// </summary>
    public decimal HighValueThreshold { get; set; } = 10000m;

    /// <summary>
    /// Daily transaction volume threshold per user (USD)
    /// </summary>
    public decimal DailyVolumeThreshold { get; set; } = 50000m;

    /// <summary>
    /// Number of transactions in short period to flag as suspicious
    /// </summary>
    public int RapidTransactionThreshold { get; set; } = 10;

    /// <summary>
    /// Time window for rapid transaction detection (minutes)
    /// </summary>
    public int RapidTransactionWindowMinutes { get; set; } = 5;

    /// <summary>
    /// Enable automatic blocking of flagged accounts
    /// </summary>
    public bool AutoBlockSuspiciousAccounts { get; set; } = false;

    /// <summary>
    /// Require manual review for high-risk transactions
    /// </summary>
    public bool RequireManualReview { get; set; } = true;
}

public class OFACSettings
{
    /// <summary>
    /// Enable OFAC sanctions screening
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// OFAC SDN (Specially Designated Nationals) list API endpoint
    /// </summary>
    public string SDNListUrl { get; set; } = "https://www.treasury.gov/ofac/downloads/sdn.csv";

    /// <summary>
    /// How often to refresh OFAC lists (hours)
    /// </summary>
    public int RefreshIntervalHours { get; set; } = 24;

    /// <summary>
    /// Minimum match score to flag (0-100)
    /// </summary>
    public int MinimumMatchScore { get; set; } = 85;

    /// <summary>
    /// Block orders from sanctioned entities automatically
    /// </summary>
    public bool BlockSanctionedOrders { get; set; } = true;

    /// <summary>
    /// Screen all trades (not just new accounts)
    /// </summary>
    public bool ScreenAllTrades { get; set; } = true;

    /// <summary>
    /// Cache OFAC data locally for faster checks
    /// </summary>
    public bool UseLocalCache { get; set; } = true;
}

public class TradeSurveillanceSettings
{
    /// <summary>
    /// Enable trade surveillance monitoring
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Enable pump and dump detection
    /// </summary>
    public bool DetectPumpAndDump { get; set; } = true;

    /// <summary>
    /// Enable spoofing/layering detection
    /// </summary>
    public bool DetectSpoofing { get; set; } = true;

    /// <summary>
    /// Enable wash trading detection
    /// </summary>
    public bool DetectWashTrading { get; set; } = true;

    /// <summary>
    /// Enable front running detection
    /// </summary>
    public bool DetectFrontRunning { get; set; } = true;

    /// <summary>
    /// Price deviation threshold for manipulation alerts (%)
    /// </summary>
    public decimal PriceDeviationThreshold { get; set; } = 5.0m;

    /// <summary>
    /// Volume spike multiplier for alerts
    /// </summary>
    public decimal VolumeSpikeMultiplier { get; set; } = 3.0m;

    /// <summary>
    /// Time window for pattern detection (minutes)
    /// </summary>
    public int DetectionWindowMinutes { get; set; } = 60;

    /// <summary>
    /// Minimum number of orders to constitute spoofing
    /// </summary>
    public int SpoofingOrderThreshold { get; set; } = 5;

    /// <summary>
    /// Wash trade detection window (minutes)
    /// </summary>
    public int WashTradeWindowMinutes { get; set; } = 30;

    /// <summary>
    /// Alert level for surveillance events
    /// </summary>
    public string AlertLevel { get; set; } = "WARNING"; // INFO, WARNING, CRITICAL
}

public class RegulatoryReportingSettings
{
    /// <summary>
    /// Enable SEC/FINRA regulatory reporting
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Generate Form PF reports (hedge fund reporting)
    /// </summary>
    public bool GenerateFormPF { get; set; } = true;

    /// <summary>
    /// Generate Form 13F reports (institutional investment managers)
    /// </summary>
    public bool GenerateForm13F { get; set; } = true;

    /// <summary>
    /// Generate FINRA CAT (Consolidated Audit Trail) reports
    /// </summary>
    public bool GenerateCATReports { get; set; } = true;

    /// <summary>
    /// Export directory for regulatory reports
    /// </summary>
    public string ReportExportDirectory { get; set; } = "/var/compliance/reports";

    /// <summary>
    /// Report format (JSON, XML, CSV, XBRL)
    /// </summary>
    public string ReportFormat { get; set; } = "XML";

    /// <summary>
    /// Automatically submit reports to regulators
    /// </summary>
    public bool AutoSubmitReports { get; set; } = false;

    /// <summary>
    /// Require compliance officer approval before submission
    /// </summary>
    public bool RequireApproval { get; set; } = true;

    /// <summary>
    /// Reporting frequency (DAILY, WEEKLY, MONTHLY, QUARTERLY)
    /// </summary>
    public string ReportingFrequency { get; set; } = "DAILY";

    /// <summary>
    /// SEC EDGAR filing credentials
    /// </summary>
    public string? EDGARFilerCIK { get; set; }

    /// <summary>
    /// FINRA reporting firm ID
    /// </summary>
    public string? FINRAFirmID { get; set; }
}

public class DataRetentionSettings
{
    /// <summary>
    /// Enable automated data retention policies
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Retention period for orders and trades (years) - SEC Rule 17a-3
    /// </summary>
    public int OrderTradeRetentionYears { get; set; } = 7;

    /// <summary>
    /// Retention period for audit logs (years)
    /// </summary>
    public int AuditLogRetentionYears { get; set; } = 6;

    /// <summary>
    /// Retention period for market data (years)
    /// </summary>
    public int MarketDataRetentionYears { get; set; } = 1;

    /// <summary>
    /// Retention period for user KYC data (years) - GDPR compliance
    /// </summary>
    public int UserKYCRetentionYears { get; set; } = 7;

    /// <summary>
    /// Retention period for compliance events (years)
    /// </summary>
    public int ComplianceEventRetentionYears { get; set; } = 7;

    /// <summary>
    /// Enable automatic archival of old data
    /// </summary>
    public bool EnableAutoArchive { get; set; } = true;

    /// <summary>
    /// Archive storage path
    /// </summary>
    public string ArchiveStoragePath { get; set; } = "/var/compliance/archive";

    /// <summary>
    /// Compress archived data
    /// </summary>
    public bool CompressArchives { get; set; } = true;

    /// <summary>
    /// Run data retention cleanup (cron expression)
    /// </summary>
    public string CleanupSchedule { get; set; } = "0 2 * * 0"; // Every Sunday at 2 AM

    /// <summary>
    /// Notify administrators before deletion
    /// </summary>
    public bool NotifyBeforeDeletion { get; set; } = true;

    /// <summary>
    /// Days of notice before deletion
    /// </summary>
    public int DeletionNoticeDays { get; set; } = 30;
}
