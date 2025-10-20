using System.IO.Compression;
using System.Text.Json;
using AlgoTrendy.Core.Configuration;
using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace AlgoTrendy.Infrastructure.Services;

/// <summary>
/// Data retention service implementing 7-year retention policy (SEC Rule 17a-3/17a-4)
/// Handles archiving and purging of old data
/// </summary>
public class DataRetentionService
{
    private readonly ILogger<DataRetentionService> _logger;
    private readonly ComplianceSettings _complianceSettings;
    private readonly string _connectionString;

    public DataRetentionService(
        ILogger<DataRetentionService> logger,
        IOptions<ComplianceSettings> complianceSettings,
        IConfiguration configuration)
    {
        _logger = logger;
        _complianceSettings = complianceSettings.Value;
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection string not configured");
    }

    /// <summary>
    /// Execute retention policy for all tables
    /// </summary>
    public async Task ExecuteRetentionPolicyAsync(CancellationToken cancellationToken = default)
    {
        if (!_complianceSettings.DataRetention.Enabled)
        {
            _logger.LogInformation("Data retention policy is disabled");
            return;
        }

        try
        {
            _logger.LogInformation("Starting data retention policy execution");

            var tasks = new List<Task>
            {
                ProcessTableRetentionAsync("orders", "created_at",
                    _complianceSettings.DataRetention.OrderTradeRetentionYears, cancellationToken),
                ProcessTableRetentionAsync("trades", "executed_at",
                    _complianceSettings.DataRetention.OrderTradeRetentionYears, cancellationToken),
                ProcessTableRetentionAsync("compliance_events", "created_at",
                    _complianceSettings.DataRetention.ComplianceEventRetentionYears, cancellationToken),
                ProcessTableRetentionAsync("market_data", "timestamp",
                    _complianceSettings.DataRetention.MarketDataRetentionYears, cancellationToken),
                ProcessTableRetentionAsync("surveillance_alerts", "detection_time",
                    _complianceSettings.DataRetention.ComplianceEventRetentionYears, cancellationToken),
                ProcessTableRetentionAsync("regulatory_reports", "generated_at",
                    _complianceSettings.DataRetention.ComplianceEventRetentionYears, cancellationToken)
            };

            await Task.WhenAll(tasks);

            _logger.LogInformation("Data retention policy execution completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing data retention policy");
            throw;
        }
    }

    /// <summary>
    /// Process retention for a specific table
    /// </summary>
    private async Task ProcessTableRetentionAsync(
        string tableName,
        string timestampColumn,
        int retentionYears,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing retention for table {TableName} ({Years} years)",
                tableName, retentionYears);

            var cutoffDate = DateTime.UtcNow.AddYears(-retentionYears);

            // Count records to be archived
            var recordCount = await CountOldRecordsAsync(tableName, timestampColumn, cutoffDate, cancellationToken);

            if (recordCount == 0)
            {
                _logger.LogInformation("No records to archive in {TableName}", tableName);
                return;
            }

            _logger.LogInformation("Found {Count} records to archive in {TableName}", recordCount, tableName);

            // Send notification if configured
            if (_complianceSettings.DataRetention.NotifyBeforeDeletion)
            {
                _logger.LogWarning("DATA RETENTION: {Count} records in {TableName} will be archived/deleted in {Days} days",
                    recordCount, tableName, _complianceSettings.DataRetention.DeletionNoticeDays);

                // In production, send email/notification to administrators
                await Task.Delay(100, cancellationToken); // Placeholder for notification
            }

            // Archive data if enabled
            string? archivePath = null;
            string? fileHash = null;

            if (_complianceSettings.DataRetention.EnableAutoArchive)
            {
                (archivePath, fileHash) = await ArchiveTableDataAsync(
                    tableName, timestampColumn, cutoffDate, cancellationToken);
            }

            // Delete old records
            var deletedCount = await DeleteOldRecordsAsync(tableName, timestampColumn, cutoffDate, cancellationToken);

            // Log retention operation
            await LogRetentionOperationAsync(
                tableName,
                _complianceSettings.DataRetention.EnableAutoArchive ? "Archive+Delete" : "Delete",
                deletedCount,
                retentionYears,
                archivePath,
                fileHash,
                "Success",
                null,
                cancellationToken);

            _logger.LogInformation("Retention complete for {TableName}: {Count} records processed",
                tableName, deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing retention for table {TableName}", tableName);

            await LogRetentionOperationAsync(
                tableName,
                "Failed",
                0,
                retentionYears,
                null,
                null,
                "Failed",
                ex.Message,
                cancellationToken);
        }
    }

    /// <summary>
    /// Count old records in a table
    /// </summary>
    private async Task<int> CountOldRecordsAsync(
        string tableName,
        string timestampColumn,
        DateTime cutoffDate,
        CancellationToken cancellationToken)
    {
        // Whitelist validation to prevent SQL injection
        ValidateTableAndColumn(tableName, timestampColumn);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = $"SELECT COUNT(*) FROM {tableName} WHERE {timestampColumn} < @cutoffDate";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("cutoffDate", cutoffDate);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result != null ? Convert.ToInt32(result) : 0;
    }

    /// <summary>
    /// Archive table data to file
    /// </summary>
    private async Task<(string archivePath, string fileHash)> ArchiveTableDataAsync(
        string tableName,
        string timestampColumn,
        DateTime cutoffDate,
        CancellationToken cancellationToken)
    {
        // Whitelist validation to prevent SQL injection
        ValidateTableAndColumn(tableName, timestampColumn);

        var archiveDir = _complianceSettings.DataRetention.ArchiveStoragePath;
        Directory.CreateDirectory(archiveDir);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var fileName = $"{tableName}_{timestamp}.json";
        var filePath = Path.Combine(archiveDir, fileName);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = $"SELECT * FROM {tableName} WHERE {timestampColumn} < @cutoffDate";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("cutoffDate", cutoffDate);

        var records = new List<Dictionary<string, object?>>();

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var record = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                record[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }
            records.Add(record);
        }

        // Write to JSON file
        var json = JsonSerializer.Serialize(records, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        await File.WriteAllTextAsync(filePath, json, cancellationToken);

        // Compress if enabled
        if (_complianceSettings.DataRetention.CompressArchives)
        {
            var compressedPath = filePath + ".gz";
            await using var inputStream = File.OpenRead(filePath);
            await using var outputStream = File.Create(compressedPath);
            await using var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal);
            await inputStream.CopyToAsync(gzipStream, cancellationToken);

            File.Delete(filePath);
            filePath = compressedPath;
        }

        // Calculate file hash
        var fileHash = await CalculateFileHashAsync(filePath, cancellationToken);

        _logger.LogInformation("Archived {TableName} data to {FilePath} ({Records} records, hash: {Hash})",
            tableName, filePath, records.Count, fileHash);

        return (filePath, fileHash);
    }

    /// <summary>
    /// Delete old records from a table
    /// </summary>
    private async Task<int> DeleteOldRecordsAsync(
        string tableName,
        string timestampColumn,
        DateTime cutoffDate,
        CancellationToken cancellationToken)
    {
        // Whitelist validation to prevent SQL injection
        ValidateTableAndColumn(tableName, timestampColumn);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = $"DELETE FROM {tableName} WHERE {timestampColumn} < @cutoffDate";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("cutoffDate", cutoffDate);

        var deletedCount = await cmd.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("Deleted {Count} records from {TableName}", deletedCount, tableName);

        return deletedCount;
    }

    /// <summary>
    /// Validates table and column names against whitelist to prevent SQL injection
    /// </summary>
    private void ValidateTableAndColumn(string tableName, string timestampColumn)
    {
        // Whitelist of allowed table names
        var allowedTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "orders",
            "trades",
            "compliance_events",
            "market_data",
            "market_data_1m",
            "surveillance_alerts",
            "regulatory_reports",
            "data_retention_log"
        };

        // Whitelist of allowed timestamp column names
        var allowedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "created_at",
            "executed_at",
            "timestamp",
            "detection_time",
            "generated_at",
            "performed_at"
        };

        if (!allowedTables.Contains(tableName))
        {
            _logger.LogError("Invalid table name rejected: {TableName}", tableName);
            throw new ArgumentException($"Table name '{tableName}' is not allowed. Security validation failed.", nameof(tableName));
        }

        if (!allowedColumns.Contains(timestampColumn))
        {
            _logger.LogError("Invalid column name rejected: {ColumnName}", timestampColumn);
            throw new ArgumentException($"Column name '{timestampColumn}' is not allowed. Security validation failed.", nameof(timestampColumn));
        }

        _logger.LogDebug("Table and column validation passed: {TableName}.{ColumnName}", tableName, timestampColumn);
    }

    /// <summary>
    /// Calculate SHA-256 hash of a file
    /// </summary>
    private async Task<string> CalculateFileHashAsync(string filePath, CancellationToken cancellationToken)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        await using var stream = File.OpenRead(filePath);
        var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// Log retention operation
    /// </summary>
    private async Task LogRetentionOperationAsync(
        string tableName,
        string operation,
        int recordsAffected,
        int retentionPeriodYears,
        string? archivePath,
        string? fileHash,
        string status,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = @"
            INSERT INTO data_retention_log
            (log_id, table_name, operation, records_affected, retention_period_years,
             archive_path, file_hash, performed_at, status, error_message)
            VALUES (@logId, @tableName, @operation, @recordsAffected, @retentionPeriodYears,
                    @archivePath, @fileHash, @performedAt, @status, @errorMessage)";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("logId", Guid.NewGuid());
        cmd.Parameters.AddWithValue("tableName", tableName);
        cmd.Parameters.AddWithValue("operation", operation);
        cmd.Parameters.AddWithValue("recordsAffected", recordsAffected);
        cmd.Parameters.AddWithValue("retentionPeriodYears", retentionPeriodYears);
        cmd.Parameters.AddWithValue("archivePath", (object?)archivePath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("fileHash", (object?)fileHash ?? DBNull.Value);
        cmd.Parameters.AddWithValue("performedAt", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("status", status);
        cmd.Parameters.AddWithValue("errorMessage", (object?)errorMessage ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync(cancellationToken);

        // Also log as compliance event
        await LogComplianceEventAsync(
            ComplianceEventType.DataPurged,
            ComplianceSeverity.Info,
            $"Data retention: {operation} {recordsAffected} records from {tableName}",
            JsonSerializer.Serialize(new
            {
                tableName,
                operation,
                recordsAffected,
                retentionPeriodYears,
                archivePath,
                status
            }),
            cancellationToken);
    }

    /// <summary>
    /// Get retention statistics
    /// </summary>
    public async Task<RetentionStatistics> GetRetentionStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var stats = new RetentionStatistics();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Get statistics for each table
        var tables = new[]
        {
            ("orders", "created_at", _complianceSettings.DataRetention.OrderTradeRetentionYears),
            ("trades", "executed_at", _complianceSettings.DataRetention.OrderTradeRetentionYears),
            ("compliance_events", "created_at", _complianceSettings.DataRetention.ComplianceEventRetentionYears),
            ("market_data", "timestamp", _complianceSettings.DataRetention.MarketDataRetentionYears)
        };

        foreach (var (tableName, timestampColumn, retentionYears) in tables)
        {
            var cutoffDate = DateTime.UtcNow.AddYears(-retentionYears);
            var eligibleCount = await CountOldRecordsAsync(tableName, timestampColumn, cutoffDate, cancellationToken);

            stats.TableStatistics.Add(new TableRetentionInfo
            {
                TableName = tableName,
                RetentionYears = retentionYears,
                CutoffDate = cutoffDate,
                RecordsEligibleForArchive = eligibleCount
            });

            stats.TotalRecordsEligible += eligibleCount;
        }

        return stats;
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
        cmd.Parameters.AddWithValue("source", "DataRetentionService");
        cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}

/// <summary>
/// Retention statistics
/// </summary>
public class RetentionStatistics
{
    public List<TableRetentionInfo> TableStatistics { get; set; } = new();
    public int TotalRecordsEligible { get; set; }
}

/// <summary>
/// Table retention information
/// </summary>
public class TableRetentionInfo
{
    public string TableName { get; set; } = string.Empty;
    public int RetentionYears { get; set; }
    public DateTime CutoffDate { get; set; }
    public int RecordsEligibleForArchive { get; set; }
}
