using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using AlgoTrendy.Core.Configuration;
using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace AlgoTrendy.Infrastructure.Services;

/// <summary>
/// SEC/FINRA regulatory reporting service
/// Generates Form PF, Form 13F, CAT reports
/// </summary>
public class RegulatoryReportingService
{
    private readonly ILogger<RegulatoryReportingService> _logger;
    private readonly ComplianceSettings _complianceSettings;
    private readonly string _connectionString;

    public RegulatoryReportingService(
        ILogger<RegulatoryReportingService> logger,
        IOptions<ComplianceSettings> complianceSettings,
        IConfiguration configuration)
    {
        _logger = logger;
        _complianceSettings = complianceSettings.Value;
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection string not configured");
    }

    /// <summary>
    /// Generate SEC Form PF (Private Fund reporting)
    /// Required for hedge funds with $150M+ AUM
    /// </summary>
    public async Task<RegulatoryReport> GenerateFormPFAsync(
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default)
    {
        if (!_complianceSettings.RegulatoryReporting.GenerateFormPF)
        {
            throw new InvalidOperationException("Form PF generation is disabled");
        }

        try
        {
            _logger.LogInformation("Generating Form PF report for period {Start} to {End}",
                periodStart, periodEnd);

            var report = new RegulatoryReport
            {
                ReportId = Guid.NewGuid(),
                ReportType = ReportType.FormPF,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                Status = ReportStatus.Draft,
                Format = _complianceSettings.RegulatoryReporting.ReportFormat,
                GeneratedAt = DateTime.UtcNow
            };

            // Gather data
            var fundData = await GetFundDataAsync(periodStart, periodEnd, cancellationToken);

            // Generate report content based on format
            report.Content = _complianceSettings.RegulatoryReporting.ReportFormat.ToUpper() switch
            {
                "XML" => GenerateFormPFXML(fundData, periodStart, periodEnd),
                "JSON" => GenerateFormPFJSON(fundData, periodStart, periodEnd),
                _ => throw new NotSupportedException($"Format {_complianceSettings.RegulatoryReporting.ReportFormat} not supported")
            };

            // Save report to file
            if (!string.IsNullOrEmpty(_complianceSettings.RegulatoryReporting.ReportExportDirectory))
            {
                report.FilePath = await SaveReportToFileAsync(
                    report.ReportType,
                    report.Format,
                    report.Content,
                    periodStart,
                    periodEnd,
                    cancellationToken);

                report.FileHash = CalculateHash(report.Content);
            }

            // Save to database
            await SaveReportAsync(report, cancellationToken);

            report.Status = ReportStatus.Generated;

            _logger.LogInformation("Form PF report generated: {ReportId}", report.ReportId);

            await LogComplianceEventAsync(
                ComplianceEventType.FormPFGenerated,
                ComplianceSeverity.Info,
                $"Form PF generated for period {periodStart:yyyy-MM-dd} to {periodEnd:yyyy-MM-dd}",
                JsonSerializer.Serialize(new { report.ReportId, report.FilePath }),
                cancellationToken);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Form PF report");
            throw;
        }
    }

    /// <summary>
    /// Generate SEC Form 13F (Institutional Investment Manager holdings)
    /// Required for managers with $100M+ in qualifying assets
    /// </summary>
    public async Task<RegulatoryReport> GenerateForm13FAsync(
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default)
    {
        if (!_complianceSettings.RegulatoryReporting.GenerateForm13F)
        {
            throw new InvalidOperationException("Form 13F generation is disabled");
        }

        try
        {
            _logger.LogInformation("Generating Form 13F report for period {Start} to {End}",
                periodStart, periodEnd);

            var report = new RegulatoryReport
            {
                ReportId = Guid.NewGuid(),
                ReportType = ReportType.Form13F,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                Status = ReportStatus.Draft,
                Format = _complianceSettings.RegulatoryReporting.ReportFormat,
                GeneratedAt = DateTime.UtcNow
            };

            // Gather holdings data
            var holdings = await GetHoldingsDataAsync(periodEnd, cancellationToken);

            // Generate report content
            report.Content = _complianceSettings.RegulatoryReporting.ReportFormat.ToUpper() switch
            {
                "XML" => GenerateForm13FXML(holdings, periodEnd),
                "JSON" => GenerateForm13FJSON(holdings, periodEnd),
                _ => throw new NotSupportedException($"Format {_complianceSettings.RegulatoryReporting.ReportFormat} not supported")
            };

            // Save report
            if (!string.IsNullOrEmpty(_complianceSettings.RegulatoryReporting.ReportExportDirectory))
            {
                report.FilePath = await SaveReportToFileAsync(
                    report.ReportType,
                    report.Format,
                    report.Content,
                    periodStart,
                    periodEnd,
                    cancellationToken);

                report.FileHash = CalculateHash(report.Content);
            }

            await SaveReportAsync(report, cancellationToken);

            report.Status = ReportStatus.Generated;

            _logger.LogInformation("Form 13F report generated: {ReportId}", report.ReportId);

            await LogComplianceEventAsync(
                ComplianceEventType.Form13FGenerated,
                ComplianceSeverity.Info,
                $"Form 13F generated for period ending {periodEnd:yyyy-MM-dd}",
                JsonSerializer.Serialize(new { report.ReportId, report.FilePath }),
                cancellationToken);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Form 13F report");
            throw;
        }
    }

    /// <summary>
    /// Generate FINRA CAT (Consolidated Audit Trail) report
    /// Real-time order and trade reporting
    /// </summary>
    public async Task<RegulatoryReport> GenerateCATReportAsync(
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default)
    {
        if (!_complianceSettings.RegulatoryReporting.GenerateCATReports)
        {
            throw new InvalidOperationException("CAT report generation is disabled");
        }

        try
        {
            _logger.LogInformation("Generating CAT report for period {Start} to {End}",
                periodStart, periodEnd);

            var report = new RegulatoryReport
            {
                ReportId = Guid.NewGuid(),
                ReportType = ReportType.CAT,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                Status = ReportStatus.Draft,
                Format = "JSON", // CAT requires JSON
                GeneratedAt = DateTime.UtcNow
            };

            // Get order and trade data
            var catData = await GetCATDataAsync(periodStart, periodEnd, cancellationToken);

            // Generate CAT JSON format
            report.Content = GenerateCATJSON(catData);

            // Save report
            if (!string.IsNullOrEmpty(_complianceSettings.RegulatoryReporting.ReportExportDirectory))
            {
                report.FilePath = await SaveReportToFileAsync(
                    report.ReportType,
                    report.Format,
                    report.Content,
                    periodStart,
                    periodEnd,
                    cancellationToken);

                report.FileHash = CalculateHash(report.Content);
            }

            await SaveReportAsync(report, cancellationToken);

            report.Status = ReportStatus.Generated;

            _logger.LogInformation("CAT report generated: {ReportId}", report.ReportId);

            await LogComplianceEventAsync(
                ComplianceEventType.CATReportGenerated,
                ComplianceSeverity.Info,
                $"CAT report generated for period {periodStart:yyyy-MM-dd} to {periodEnd:yyyy-MM-dd}",
                JsonSerializer.Serialize(new { report.ReportId, report.FilePath }),
                cancellationToken);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating CAT report");
            throw;
        }
    }

    /// <summary>
    /// Get fund data for Form PF
    /// </summary>
    private async Task<FundData> GetFundDataAsync(DateTime start, DateTime end, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Get AUM (Assets Under Management)
        var aumSql = @"
            SELECT COALESCE(SUM(total_value), 0)
            FROM portfolio_snapshots
            WHERE snapshot_time >= @start AND snapshot_time <= @end
            ORDER BY snapshot_time DESC
            LIMIT 1";

        decimal aum = 0;
        await using (var cmd = new NpgsqlCommand(aumSql, connection))
        {
            cmd.Parameters.AddWithValue("start", start);
            cmd.Parameters.AddWithValue("end", end);
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            aum = result != null ? Convert.ToDecimal(result) : 0;
        }

        // Get leverage data
        var leverageSql = @"
            SELECT COALESCE(AVG(CAST(metadata->>'leverage' AS NUMERIC)), 1)
            FROM positions
            WHERE opened_at >= @start AND opened_at <= @end";

        decimal leverage = 1;
        await using (var cmd = new NpgsqlCommand(leverageSql, connection))
        {
            cmd.Parameters.AddWithValue("start", start);
            cmd.Parameters.AddWithValue("end", end);
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            leverage = result != null ? Convert.ToDecimal(result) : 1;
        }

        return new FundData
        {
            AUM = aum,
            AverageLeverage = leverage,
            ReportingPeriodStart = start,
            ReportingPeriodEnd = end
        };
    }

    /// <summary>
    /// Get holdings data for Form 13F
    /// </summary>
    private async Task<List<HoldingData>> GetHoldingsDataAsync(DateTime asOfDate, CancellationToken cancellationToken)
    {
        var holdings = new List<HoldingData>();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = @"
            SELECT symbol, exchange, quantity, entry_price, current_price,
                   (quantity * current_price) as market_value
            FROM positions
            WHERE status = 'Open'
              AND opened_at <= @asOfDate
            ORDER BY market_value DESC";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("asOfDate", asOfDate);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            holdings.Add(new HoldingData
            {
                Symbol = reader.GetString(0),
                Exchange = reader.GetString(1),
                Quantity = reader.GetDecimal(2),
                CostBasis = reader.GetDecimal(3),
                MarketPrice = reader.GetDecimal(4),
                MarketValue = reader.GetDecimal(5)
            });
        }

        return holdings;
    }

    /// <summary>
    /// Get CAT data (orders and trades)
    /// </summary>
    private async Task<CATData> GetCATDataAsync(DateTime start, DateTime end, CancellationToken cancellationToken)
    {
        var catData = new CATData();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Get orders
        var orderSql = @"
            SELECT order_id, client_order_id, exchange_order_id, symbol, exchange,
                   side, type, quantity, price, status, created_at, submitted_at
            FROM orders
            WHERE created_at >= @start AND created_at <= @end";

        await using var cmd = new NpgsqlCommand(orderSql, connection);
        cmd.Parameters.AddWithValue("start", start);
        cmd.Parameters.AddWithValue("end", end);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            catData.Orders.Add(new CATOrder
            {
                OrderId = reader.GetGuid(0).ToString(),
                ClientOrderId = reader.IsDBNull(1) ? null : reader.GetString(1),
                ExchangeOrderId = reader.IsDBNull(2) ? null : reader.GetString(2),
                Symbol = reader.GetString(3),
                Exchange = reader.GetString(4),
                Side = reader.GetString(5),
                OrderType = reader.GetString(6),
                Quantity = reader.GetDecimal(7),
                Price = reader.IsDBNull(8) ? null : reader.GetDecimal(8),
                Status = reader.GetString(9),
                Timestamp = reader.GetDateTime(10)
            });
        }

        return catData;
    }

    /// <summary>
    /// Generate Form PF in XML format
    /// </summary>
    private string GenerateFormPFXML(FundData data, DateTime start, DateTime end)
    {
        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement("FormPF",
                new XElement("ReportingPeriod",
                    new XElement("StartDate", start.ToString("yyyy-MM-dd")),
                    new XElement("EndDate", end.ToString("yyyy-MM-dd"))
                ),
                new XElement("FundInformation",
                    new XElement("AUM", data.AUM),
                    new XElement("AverageLeverage", data.AverageLeverage)
                ),
                new XElement("GeneratedAt", DateTime.UtcNow.ToString("O")),
                new XElement("GeneratedBy", "AlgoTrendy")
            )
        );

        return doc.ToString();
    }

    /// <summary>
    /// Generate Form PF in JSON format
    /// </summary>
    private string GenerateFormPFJSON(FundData data, DateTime start, DateTime end)
    {
        var reportData = new
        {
            formType = "PF",
            reportingPeriod = new { start, end },
            fundInformation = new
            {
                aum = data.AUM,
                averageLeverage = data.AverageLeverage
            },
            generatedAt = DateTime.UtcNow,
            generatedBy = "AlgoTrendy"
        };

        return JsonSerializer.Serialize(reportData, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Generate Form 13F in XML format
    /// </summary>
    private string GenerateForm13FXML(List<HoldingData> holdings, DateTime asOfDate)
    {
        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement("Form13F",
                new XElement("CoverPage",
                    new XElement("ReportCalendarOrQuarter", asOfDate.ToString("yyyy-MM-dd")),
                    new XElement("FilerCIK", _complianceSettings.RegulatoryReporting.EDGARFilerCIK ?? "N/A")
                ),
                new XElement("InformationTable",
                    holdings.Select(h => new XElement("InfoTableEntry",
                        new XElement("IssuerName", h.Symbol),
                        new XElement("Class", "Common Stock"),
                        new XElement("Quantity", h.Quantity),
                        new XElement("Value", h.MarketValue)
                    ))
                ),
                new XElement("GeneratedAt", DateTime.UtcNow.ToString("O"))
            )
        );

        return doc.ToString();
    }

    /// <summary>
    /// Generate Form 13F in JSON format
    /// </summary>
    private string GenerateForm13FJSON(List<HoldingData> holdings, DateTime asOfDate)
    {
        var reportData = new
        {
            formType = "13F",
            reportCalendarOrQuarter = asOfDate,
            filerCIK = _complianceSettings.RegulatoryReporting.EDGARFilerCIK ?? "N/A",
            holdings = holdings.Select(h => new
            {
                symbol = h.Symbol,
                quantity = h.Quantity,
                marketValue = h.MarketValue
            }),
            generatedAt = DateTime.UtcNow
        };

        return JsonSerializer.Serialize(reportData, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Generate CAT report in JSON format
    /// </summary>
    private string GenerateCATJSON(CATData data)
    {
        var reportData = new
        {
            reportType = "CAT",
            firmDesignatedID = _complianceSettings.RegulatoryReporting.FINRAFirmID ?? "N/A",
            orders = data.Orders,
            generatedAt = DateTime.UtcNow
        };

        return JsonSerializer.Serialize(reportData, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Save report to file
    /// </summary>
    private async Task<string> SaveReportToFileAsync(
        ReportType reportType,
        string format,
        string content,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken)
    {
        var directory = _complianceSettings.RegulatoryReporting.ReportExportDirectory;
        Directory.CreateDirectory(directory);

        var extension = format.ToLower();
        var fileName = $"{reportType}_{start:yyyyMMdd}_{end:yyyyMMdd}_{DateTime.UtcNow:yyyyMMddHHmmss}.{extension}";
        var filePath = Path.Combine(directory, fileName);

        await File.WriteAllTextAsync(filePath, content, cancellationToken);

        _logger.LogInformation("Report saved to {FilePath}", filePath);

        return filePath;
    }

    /// <summary>
    /// Save report to database
    /// </summary>
    private async Task SaveReportAsync(RegulatoryReport report, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = @"
            INSERT INTO regulatory_reports
            (report_id, report_type, period_start, period_end, status, file_path, format, content, file_hash, generated_at)
            VALUES (@reportId, @reportType, @periodStart, @periodEnd, @status, @filePath, @format, @content, @fileHash, @generatedAt)";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("reportId", report.ReportId);
        cmd.Parameters.AddWithValue("reportType", report.ReportType.ToString());
        cmd.Parameters.AddWithValue("periodStart", report.PeriodStart);
        cmd.Parameters.AddWithValue("periodEnd", report.PeriodEnd);
        cmd.Parameters.AddWithValue("status", report.Status.ToString());
        cmd.Parameters.AddWithValue("filePath", (object?)report.FilePath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("format", report.Format);
        cmd.Parameters.AddWithValue("content", (object?)report.Content ?? DBNull.Value);
        cmd.Parameters.AddWithValue("fileHash", (object?)report.FileHash ?? DBNull.Value);
        cmd.Parameters.AddWithValue("generatedAt", report.GeneratedAt);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Calculate SHA-256 hash
    /// </summary>
    private string CalculateHash(string content)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// Log compliance event
    /// </summary>
    private async Task LogComplianceEventAsync(
        ComplianceEventType eventType,
        ComplianceSeverity severity,
        string title,
        string? eventData,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = @"
            INSERT INTO compliance_events
            (event_id, event_type, severity, title, event_data, source, created_at)
            VALUES (@eventId, @eventType, @severity, @title, @eventData::jsonb, @source, @createdAt)";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("eventId", Guid.NewGuid());
        cmd.Parameters.AddWithValue("eventType", eventType.ToString());
        cmd.Parameters.AddWithValue("severity", severity.ToString());
        cmd.Parameters.AddWithValue("title", title);
        cmd.Parameters.AddWithValue("eventData", (object?)eventData ?? DBNull.Value);
        cmd.Parameters.AddWithValue("source", "RegulatoryReportingService");
        cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}

// Data models for reporting
public class FundData
{
    public decimal AUM { get; set; }
    public decimal AverageLeverage { get; set; }
    public DateTime ReportingPeriodStart { get; set; }
    public DateTime ReportingPeriodEnd { get; set; }
}

public class HoldingData
{
    public string Symbol { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal CostBasis { get; set; }
    public decimal MarketPrice { get; set; }
    public decimal MarketValue { get; set; }
}

public class CATData
{
    public List<CATOrder> Orders { get; set; } = new();
}

public class CATOrder
{
    public string OrderId { get; set; } = string.Empty;
    public string? ClientOrderId { get; set; }
    public string? ExchangeOrderId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
    public string Side { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal? Price { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
