using System.Text.Json;
using AlgoTrendy.Core.Configuration;
using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace AlgoTrendy.Infrastructure.Services;

/// <summary>
/// AML (Anti-Money Laundering) monitoring service
/// Detects suspicious transaction patterns and flags high-risk activities
/// </summary>
public class AMLMonitoringService
{
    private readonly ILogger<AMLMonitoringService> _logger;
    private readonly ComplianceSettings _complianceSettings;
    private readonly string _connectionString;

    public AMLMonitoringService(
        ILogger<AMLMonitoringService> logger,
        IOptions<ComplianceSettings> complianceSettings,
        IConfiguration configuration)
    {
        _logger = logger;
        _complianceSettings = complianceSettings.Value;
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection string not configured");
    }

    /// <summary>
    /// Monitor a trade for AML compliance before execution
    /// </summary>
    public async Task<AMLCheckResult> CheckTradeAsync(
        Guid userId,
        string symbol,
        decimal quantity,
        decimal price,
        CancellationToken cancellationToken = default)
    {
        if (!_complianceSettings.AML.Enabled)
        {
            return new AMLCheckResult { Approved = true, CheckPerformed = false };
        }

        try
        {
            var tradeValue = quantity * price;

            _logger.LogInformation("Running AML check for user {UserId}: {Symbol} ${Value:N2}",
                userId, symbol, tradeValue);

            var result = new AMLCheckResult
            {
                CheckPerformed = true,
                CheckTime = DateTime.UtcNow,
                UserId = userId,
                TradeValue = tradeValue,
                Approved = true
            };

            // Check 1: High-value transaction
            if (tradeValue >= _complianceSettings.AML.HighValueThreshold)
            {
                result.Flags.Add($"High-value transaction: ${tradeValue:N2}");
                result.RiskScore += 30;

                await LogComplianceEventAsync(
                    ComplianceEventType.AMLHighValueTransaction,
                    ComplianceSeverity.Medium,
                    userId,
                    $"High-value transaction detected: ${tradeValue:N2}",
                    JsonSerializer.Serialize(new { symbol, quantity, price, tradeValue }),
                    cancellationToken);
            }

            // Check 2: Daily volume threshold
            var dailyVolume = await GetDailyTradingVolumeAsync(userId, cancellationToken);
            if (dailyVolume + tradeValue >= _complianceSettings.AML.DailyVolumeThreshold)
            {
                result.Flags.Add($"Daily volume threshold exceeded: ${dailyVolume + tradeValue:N2}");
                result.RiskScore += 25;

                await LogComplianceEventAsync(
                    ComplianceEventType.AMLTransactionFlagged,
                    ComplianceSeverity.Medium,
                    userId,
                    $"Daily volume threshold exceeded: ${dailyVolume + tradeValue:N2}",
                    JsonSerializer.Serialize(new { dailyVolume, tradeValue, total = dailyVolume + tradeValue }),
                    cancellationToken);
            }

            // Check 3: Rapid transactions
            var recentTrades = await GetRecentTradeCountAsync(
                userId,
                _complianceSettings.AML.RapidTransactionWindowMinutes,
                cancellationToken);

            if (recentTrades >= _complianceSettings.AML.RapidTransactionThreshold)
            {
                result.Flags.Add($"Rapid transactions detected: {recentTrades} trades in {_complianceSettings.AML.RapidTransactionWindowMinutes} minutes");
                result.RiskScore += 20;

                await LogComplianceEventAsync(
                    ComplianceEventType.AMLRapidTransactions,
                    ComplianceSeverity.High,
                    userId,
                    $"Rapid transaction pattern: {recentTrades} trades in {_complianceSettings.AML.RapidTransactionWindowMinutes} minutes",
                    JsonSerializer.Serialize(new { recentTrades, windowMinutes = _complianceSettings.AML.RapidTransactionWindowMinutes }),
                    cancellationToken);
            }

            // Check 4: Structuring pattern (multiple trades just below threshold)
            var structuringDetected = await DetectStructuringAsync(userId, tradeValue, cancellationToken);
            if (structuringDetected)
            {
                result.Flags.Add("Potential structuring detected (multiple trades just below reporting threshold)");
                result.RiskScore += 40;

                await LogComplianceEventAsync(
                    ComplianceEventType.AMLSuspiciousPattern,
                    ComplianceSeverity.High,
                    userId,
                    "Structuring pattern detected: Multiple trades just below threshold",
                    JsonSerializer.Serialize(new { tradeValue, threshold = _complianceSettings.AML.HighValueThreshold }),
                    cancellationToken);
            }

            // Determine approval status
            if (result.RiskScore >= 75)
            {
                result.Approved = false;
                result.RequiresManualReview = true;

                if (_complianceSettings.AML.AutoBlockSuspiciousAccounts)
                {
                    await BlockUserAsync(userId, "High AML risk score", cancellationToken);
                    result.AccountBlocked = true;

                    await LogComplianceEventAsync(
                        ComplianceEventType.AMLAccountBlocked,
                        ComplianceSeverity.Critical,
                        userId,
                        $"Account automatically blocked due to AML risk score: {result.RiskScore}",
                        JsonSerializer.Serialize(result),
                        cancellationToken);
                }

                _logger.LogWarning("AML CHECK FAILED: User {UserId} risk score {Score} - Trade blocked",
                    userId, result.RiskScore);
            }
            else if (result.RiskScore >= 50)
            {
                result.RequiresManualReview = _complianceSettings.AML.RequireManualReview;

                if (result.RequiresManualReview)
                {
                    _logger.LogWarning("AML CHECK: User {UserId} requires manual review (score: {Score})",
                        userId, result.RiskScore);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing AML check for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Perform periodic AML review of user account
    /// </summary>
    public async Task<AMLReviewResult> ReviewUserAccountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Performing AML review for user {UserId}", userId);

            var result = new AMLReviewResult
            {
                UserId = userId,
                ReviewDate = DateTime.UtcNow
            };

            // Get user statistics
            var stats = await GetUserTradingStatisticsAsync(userId, 30, cancellationToken);

            result.TotalTrades = stats.TradeCount;
            result.TotalVolume = stats.TotalVolume;
            result.AverageTradeSize = stats.AverageTradeSize;
            result.LargestTrade = stats.LargestTrade;

            // Calculate risk score
            int riskScore = 0;

            // High volume
            if (stats.TotalVolume > _complianceSettings.AML.DailyVolumeThreshold * 30)
            {
                riskScore += 20;
                result.RiskFactors.Add("High monthly volume");
            }

            // Large average trade size
            if (stats.AverageTradeSize > _complianceSettings.AML.HighValueThreshold)
            {
                riskScore += 15;
                result.RiskFactors.Add("Large average trade size");
            }

            // Suspicious patterns
            if (stats.StructuringCount > 5)
            {
                riskScore += 30;
                result.RiskFactors.Add($"Structuring detected {stats.StructuringCount} times");
            }

            // Rapid trading frequency
            if (stats.TradeCount > 1000)
            {
                riskScore += 10;
                result.RiskFactors.Add("High trading frequency");
            }

            result.RiskScore = riskScore;

            // Determine AML status
            AMLStatus newStatus;
            if (riskScore >= 75)
            {
                newStatus = AMLStatus.UnderInvestigation;
            }
            else if (riskScore >= 50)
            {
                newStatus = AMLStatus.Flagged;
            }
            else if (riskScore >= 25)
            {
                newStatus = AMLStatus.PendingReview;
            }
            else
            {
                newStatus = AMLStatus.Clean;
            }

            result.RecommendedStatus = newStatus;

            // Update user AML status
            await UpdateUserAMLStatusAsync(userId, newStatus, cancellationToken);

            _logger.LogInformation("AML review complete for user {UserId}: Status {Status}, Risk Score {Score}",
                userId, newStatus, riskScore);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing AML review for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Get daily trading volume for a user
    /// </summary>
    private async Task<decimal> GetDailyTradingVolumeAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = @"
            SELECT COALESCE(SUM(price * quantity), 0)
            FROM trades
            WHERE user_id = @userId
              AND executed_at >= @startDate";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("userId", userId);
        cmd.Parameters.AddWithValue("startDate", DateTime.UtcNow.Date);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result != null ? Convert.ToDecimal(result) : 0;
    }

    /// <summary>
    /// Get recent trade count for a user
    /// </summary>
    private async Task<int> GetRecentTradeCountAsync(Guid userId, int windowMinutes, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = @"
            SELECT COUNT(*)
            FROM trades
            WHERE user_id = @userId
              AND executed_at >= @startTime";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("userId", userId);
        cmd.Parameters.AddWithValue("startTime", DateTime.UtcNow.AddMinutes(-windowMinutes));

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result != null ? Convert.ToInt32(result) : 0;
    }

    /// <summary>
    /// Detect structuring pattern (multiple trades just below threshold)
    /// </summary>
    private async Task<bool> DetectStructuringAsync(Guid userId, decimal currentTradeValue, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Look for trades just below threshold (90-99% of threshold)
        var lowerBound = _complianceSettings.AML.HighValueThreshold * 0.90m;
        var upperBound = _complianceSettings.AML.HighValueThreshold * 0.99m;

        var sql = @"
            SELECT COUNT(*)
            FROM trades
            WHERE user_id = @userId
              AND executed_at >= @startDate
              AND (price * quantity) >= @lowerBound
              AND (price * quantity) <= @upperBound";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("userId", userId);
        cmd.Parameters.AddWithValue("startDate", DateTime.UtcNow.AddDays(-7));
        cmd.Parameters.AddWithValue("lowerBound", lowerBound);
        cmd.Parameters.AddWithValue("upperBound", upperBound);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        var count = result != null ? Convert.ToInt32(result) : 0;

        // Structuring if 3+ trades in past week just below threshold
        return count >= 3 && currentTradeValue >= lowerBound && currentTradeValue <= upperBound;
    }

    /// <summary>
    /// Get user trading statistics
    /// </summary>
    private async Task<UserTradingStats> GetUserTradingStatisticsAsync(Guid userId, int days, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = @"
            SELECT
                COUNT(*) as trade_count,
                COALESCE(SUM(price * quantity), 0) as total_volume,
                COALESCE(AVG(price * quantity), 0) as avg_trade_size,
                COALESCE(MAX(price * quantity), 0) as largest_trade
            FROM trades
            WHERE user_id = @userId
              AND executed_at >= @startDate";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("userId", userId);
        cmd.Parameters.AddWithValue("startDate", DateTime.UtcNow.AddDays(-days));

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new UserTradingStats
            {
                TradeCount = reader.GetInt32(0),
                TotalVolume = reader.GetDecimal(1),
                AverageTradeSize = reader.GetDecimal(2),
                LargestTrade = reader.GetDecimal(3)
            };
        }

        return new UserTradingStats();
    }

    /// <summary>
    /// Block a user account
    /// </summary>
    private async Task BlockUserAsync(Guid userId, string reason, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = @"
            UPDATE users
            SET is_active = false,
                aml_status = 'Blocked',
                updated_at = @updatedAt
            WHERE user_id = @userId";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("userId", userId);
        cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

        await cmd.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogWarning("User account blocked: {UserId} - Reason: {Reason}", userId, reason);
    }

    /// <summary>
    /// Update user AML status
    /// </summary>
    private async Task UpdateUserAMLStatusAsync(Guid userId, AMLStatus status, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = @"
            UPDATE users
            SET aml_status = @status,
                last_aml_check = @checkTime,
                updated_at = @updatedAt
            WHERE user_id = @userId";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("userId", userId);
        cmd.Parameters.AddWithValue("status", status.ToString());
        cmd.Parameters.AddWithValue("checkTime", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Log compliance event
    /// </summary>
    private async Task LogComplianceEventAsync(
        ComplianceEventType eventType,
        ComplianceSeverity severity,
        Guid? userId,
        string title,
        string? eventData,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = @"
            INSERT INTO compliance_events
            (event_id, event_type, severity, user_id, title, event_data, source, created_at)
            VALUES (@eventId, @eventType, @severity, @userId, @title, @eventData::jsonb, @source, @createdAt)";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("eventId", Guid.NewGuid());
        cmd.Parameters.AddWithValue("eventType", eventType.ToString());
        cmd.Parameters.AddWithValue("severity", severity.ToString());
        cmd.Parameters.AddWithValue("userId", (object?)userId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("title", title);
        cmd.Parameters.AddWithValue("eventData", (object?)eventData ?? DBNull.Value);
        cmd.Parameters.AddWithValue("source", "AMLMonitoringService");
        cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}

/// <summary>
/// AML check result for a single trade
/// </summary>
public class AMLCheckResult
{
    public bool CheckPerformed { get; set; }
    public DateTime? CheckTime { get; set; }
    public Guid UserId { get; set; }
    public decimal TradeValue { get; set; }
    public bool Approved { get; set; }
    public int RiskScore { get; set; }
    public List<string> Flags { get; set; } = new();
    public bool RequiresManualReview { get; set; }
    public bool AccountBlocked { get; set; }
}

/// <summary>
/// AML review result for periodic account review
/// </summary>
public class AMLReviewResult
{
    public Guid UserId { get; set; }
    public DateTime ReviewDate { get; set; }
    public int TotalTrades { get; set; }
    public decimal TotalVolume { get; set; }
    public decimal AverageTradeSize { get; set; }
    public decimal LargestTrade { get; set; }
    public int RiskScore { get; set; }
    public List<string> RiskFactors { get; set; } = new();
    public AMLStatus RecommendedStatus { get; set; }
}

/// <summary>
/// User trading statistics
/// </summary>
public class UserTradingStats
{
    public int TradeCount { get; set; }
    public decimal TotalVolume { get; set; }
    public decimal AverageTradeSize { get; set; }
    public decimal LargestTrade { get; set; }
    public int StructuringCount { get; set; }
}
