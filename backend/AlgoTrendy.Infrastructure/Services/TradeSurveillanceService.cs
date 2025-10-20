using System.Text.Json;
using AlgoTrendy.Core.Configuration;
using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace AlgoTrendy.Infrastructure.Services;

/// <summary>
/// Trade surveillance service for detecting market manipulation patterns
/// Monitors: Pump & Dump, Spoofing, Wash Trading, Front Running
/// </summary>
public class TradeSurveillanceService
{
    private readonly ILogger<TradeSurveillanceService> _logger;
    private readonly ComplianceSettings _complianceSettings;
    private readonly string _connectionString;

    public TradeSurveillanceService(
        ILogger<TradeSurveillanceService> logger,
        IOptions<ComplianceSettings> complianceSettings,
        IConfiguration configuration)
    {
        _logger = logger;
        _complianceSettings = complianceSettings.Value;
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection string not configured");
    }

    /// <summary>
    /// Perform real-time surveillance on a trade
    /// </summary>
    public async Task MonitorTradeAsync(
        Trade trade,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (!_complianceSettings.TradeSurveillance.Enabled)
        {
            return;
        }

        try
        {
            _logger.LogInformation("Running surveillance on trade {TradeId} for symbol {Symbol}",
                trade.TradeId, trade.Symbol);

            // Run surveillance checks in parallel
            var tasks = new List<Task>();

            if (_complianceSettings.TradeSurveillance.DetectPumpAndDump)
            {
                tasks.Add(DetectPumpAndDumpAsync(trade, userId, cancellationToken));
            }

            if (_complianceSettings.TradeSurveillance.DetectSpoofing)
            {
                tasks.Add(DetectSpoofingAsync(trade, userId, cancellationToken));
            }

            if (_complianceSettings.TradeSurveillance.DetectWashTrading)
            {
                tasks.Add(DetectWashTradingAsync(trade, userId, cancellationToken));
            }

            if (_complianceSettings.TradeSurveillance.DetectFrontRunning)
            {
                tasks.Add(DetectFrontRunningAsync(trade, userId, cancellationToken));
            }

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing surveillance on trade {TradeId}", trade.TradeId);
        }
    }

    /// <summary>
    /// Detect pump and dump pattern
    /// Pattern: Rapid price increase followed by large sell orders
    /// </summary>
    private async Task DetectPumpAndDumpAsync(Trade trade, Guid userId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Get price movement over detection window
        var windowMinutes = _complianceSettings.TradeSurveillance.DetectionWindowMinutes;
        var startTime = DateTime.UtcNow.AddMinutes(-windowMinutes);

        var sql = @"
            SELECT
                MIN(close) as min_price,
                MAX(close) as max_price,
                AVG(close) as avg_price,
                AVG(volume) as avg_volume,
                MAX(volume) as max_volume
            FROM market_data
            WHERE symbol = @symbol
              AND timestamp >= @startTime
              AND timestamp <= @endTime";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("symbol", trade.Symbol);
        cmd.Parameters.AddWithValue("startTime", startTime);
        cmd.Parameters.AddWithValue("endTime", DateTime.UtcNow);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            if (!reader.IsDBNull(0) && !reader.IsDBNull(1))
            {
                var minPrice = reader.GetDecimal(0);
                var maxPrice = reader.GetDecimal(1);
                var avgPrice = reader.GetDecimal(2);
                var avgVolume = reader.GetDecimal(3);
                var maxVolume = reader.GetDecimal(4);

                // Calculate price change percentage
                var priceChange = ((maxPrice - minPrice) / minPrice) * 100;

                // Calculate volume spike
                var volumeSpike = maxVolume / avgVolume;

                // Pump & Dump indicators:
                // 1. Sharp price increase (> threshold)
                // 2. Volume spike (> multiplier)
                // 3. Large sell order near peak price
                if (priceChange > _complianceSettings.TradeSurveillance.PriceDeviationThreshold &&
                    volumeSpike > _complianceSettings.TradeSurveillance.VolumeSpikeMultiplier &&
                    trade.Side == "SELL" &&
                    trade.Price >= avgPrice * 1.05m) // Selling near peak
                {
                    await CreateSurveillanceAlertAsync(
                        "PumpAndDump",
                        ComplianceSeverity.High,
                        trade.Symbol,
                        trade.Exchange,
                        userId,
                        new[] { trade.OrderId },
                        new[] { trade.TradeId },
                        $"Pump & Dump pattern detected: {priceChange:F2}% price increase with {volumeSpike:F2}x volume spike, followed by large sell",
                        new
                        {
                            priceChange,
                            volumeSpike,
                            minPrice,
                            maxPrice,
                            tradePrice = trade.Price,
                            tradeQuantity = trade.Quantity
                        },
                        85, // High confidence
                        cancellationToken);

                    _logger.LogWarning("PUMP & DUMP DETECTED: {Symbol} - Price change: {PriceChange:F2}%, Volume spike: {VolumeSpike:F2}x",
                        trade.Symbol, priceChange, volumeSpike);
                }
            }
        }
    }

    /// <summary>
    /// Detect spoofing/layering pattern
    /// Pattern: Placing large orders without intent to execute (creating false liquidity)
    /// </summary>
    private async Task DetectSpoofingAsync(Trade trade, Guid userId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Look for pattern: Multiple orders placed and quickly cancelled
        var windowMinutes = _complianceSettings.TradeSurveillance.DetectionWindowMinutes;
        var startTime = DateTime.UtcNow.AddMinutes(-windowMinutes);

        var sql = @"
            SELECT COUNT(*) as cancelled_count
            FROM orders
            WHERE symbol = @symbol
              AND user_id = @userId
              AND status = 'Cancelled'
              AND created_at >= @startTime
              AND (closed_at - created_at) < INTERVAL '1 minute'";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("symbol", trade.Symbol);
        cmd.Parameters.AddWithValue("userId", userId);
        cmd.Parameters.AddWithValue("startTime", startTime);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        var cancelledCount = result != null ? Convert.ToInt32(result) : 0;

        // Spoofing indicator: Multiple rapid order cancellations
        if (cancelledCount >= _complianceSettings.TradeSurveillance.SpoofingOrderThreshold)
        {
            await CreateSurveillanceAlertAsync(
                "Spoofing",
                ComplianceSeverity.High,
                trade.Symbol,
                trade.Exchange,
                userId,
                null,
                new[] { trade.TradeId },
                $"Spoofing pattern detected: {cancelledCount} orders cancelled within 1 minute",
                new
                {
                    cancelledOrders = cancelledCount,
                    windowMinutes,
                    symbol = trade.Symbol
                },
                80,
                cancellationToken);

            _logger.LogWarning("SPOOFING DETECTED: User {UserId}, Symbol {Symbol} - {Count} rapid cancellations",
                userId, trade.Symbol, cancelledCount);
        }
    }

    /// <summary>
    /// Detect wash trading pattern
    /// Pattern: Buying and selling same security to create artificial volume
    /// </summary>
    private async Task DetectWashTradingAsync(Trade trade, Guid userId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Look for matching buy/sell pairs at similar prices
        var windowMinutes = _complianceSettings.TradeSurveillance.WashTradeWindowMinutes;
        var startTime = DateTime.UtcNow.AddMinutes(-windowMinutes);

        var sql = @"
            SELECT
                COUNT(*) as match_count,
                SUM(CASE WHEN side = 'BUY' THEN 1 ELSE 0 END) as buy_count,
                SUM(CASE WHEN side = 'SELL' THEN 1 ELSE 0 END) as sell_count
            FROM trades
            WHERE symbol = @symbol
              AND user_id = @userId
              AND executed_at >= @startTime
              AND ABS(price - @tradePrice) / @tradePrice < 0.01"; // Within 1% price

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("symbol", trade.Symbol);
        cmd.Parameters.AddWithValue("userId", userId);
        cmd.Parameters.AddWithValue("startTime", startTime);
        cmd.Parameters.AddWithValue("tradePrice", trade.Price);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            var buyCount = reader.GetInt32(1);
            var sellCount = reader.GetInt32(2);

            // Wash trading indicator: Equal buy/sell volumes at similar prices
            if (buyCount >= 3 && sellCount >= 3 && Math.Abs(buyCount - sellCount) <= 1)
            {
                await CreateSurveillanceAlertAsync(
                    "WashTrading",
                    ComplianceSeverity.Critical,
                    trade.Symbol,
                    trade.Exchange,
                    userId,
                    null,
                    new[] { trade.TradeId },
                    $"Wash trading pattern detected: {buyCount} buys and {sellCount} sells at similar prices",
                    new
                    {
                        buyCount,
                        sellCount,
                        windowMinutes,
                        symbol = trade.Symbol,
                        price = trade.Price
                    },
                    90,
                    cancellationToken);

                _logger.LogWarning("WASH TRADING DETECTED: User {UserId}, Symbol {Symbol} - {BuyCount} buys, {SellCount} sells",
                    userId, trade.Symbol, buyCount, sellCount);
            }
        }
    }

    /// <summary>
    /// Detect front running pattern
    /// Pattern: Trading ahead of large orders with knowledge of upcoming trades
    /// </summary>
    private async Task DetectFrontRunningAsync(Trade trade, Guid userId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Look for pattern: Small trade followed immediately by large opposite trade
        var sql = @"
            SELECT
                t2.trade_id,
                t2.price,
                t2.quantity,
                t2.side,
                t2.executed_at
            FROM trades t1
            JOIN trades t2 ON t1.symbol = t2.symbol
            WHERE t1.trade_id = @tradeId
              AND t2.user_id != @userId
              AND t2.executed_at > t1.executed_at
              AND t2.executed_at <= t1.executed_at + INTERVAL '5 seconds'
              AND t2.side != @tradeSide
              AND t2.quantity > @tradeQuantity * 5
            LIMIT 5";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("tradeId", trade.TradeId);
        cmd.Parameters.AddWithValue("userId", userId);
        cmd.Parameters.AddWithValue("tradeSide", trade.Side);
        cmd.Parameters.AddWithValue("tradeQuantity", trade.Quantity);

        var suspiciousTrades = new List<Guid>();

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            suspiciousTrades.Add(reader.GetGuid(0));
        }

        // Front running indicator: Large opposite trade immediately after
        if (suspiciousTrades.Any())
        {
            await CreateSurveillanceAlertAsync(
                "FrontRunning",
                ComplianceSeverity.High,
                trade.Symbol,
                trade.Exchange,
                userId,
                null,
                suspiciousTrades.Concat(new[] { trade.TradeId }).ToArray(),
                $"Potential front running detected: Large opposite trade immediately following",
                new
                {
                    originalTrade = trade.TradeId,
                    suspiciousTrades = suspiciousTrades,
                    symbol = trade.Symbol
                },
                75,
                cancellationToken);

            _logger.LogWarning("FRONT RUNNING DETECTED: Trade {TradeId}, Symbol {Symbol} - {Count} suspicious following trades",
                trade.TradeId, trade.Symbol, suspiciousTrades.Count);
        }
    }

    /// <summary>
    /// Create surveillance alert
    /// </summary>
    private async Task CreateSurveillanceAlertAsync(
        string alertType,
        ComplianceSeverity severity,
        string symbol,
        string exchange,
        Guid? userId,
        Guid[]? orderIds,
        Guid[]? tradeIds,
        string description,
        object patternData,
        int confidenceScore,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = @"
            INSERT INTO surveillance_alerts
            (alert_id, alert_type, severity, symbol, exchange, user_id, order_ids, trade_ids,
             description, pattern_data, confidence_score, detection_time, status)
            VALUES (@alertId, @alertType, @severity, @symbol, @exchange, @userId, @orderIds::jsonb,
                    @tradeIds::jsonb, @description, @patternData::jsonb, @confidenceScore, @detectionTime, @status)";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("alertId", Guid.NewGuid());
        cmd.Parameters.AddWithValue("alertType", alertType);
        cmd.Parameters.AddWithValue("severity", severity.ToString());
        cmd.Parameters.AddWithValue("symbol", symbol);
        cmd.Parameters.AddWithValue("exchange", exchange);
        cmd.Parameters.AddWithValue("userId", (object?)userId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("orderIds", orderIds != null ? JsonSerializer.Serialize(orderIds) : "[]");
        cmd.Parameters.AddWithValue("tradeIds", tradeIds != null ? JsonSerializer.Serialize(tradeIds) : "[]");
        cmd.Parameters.AddWithValue("description", description);
        cmd.Parameters.AddWithValue("patternData", JsonSerializer.Serialize(patternData));
        cmd.Parameters.AddWithValue("confidenceScore", confidenceScore);
        cmd.Parameters.AddWithValue("detectionTime", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("status", "Active");

        await cmd.ExecuteNonQueryAsync(cancellationToken);

        // Also log as compliance event
        await LogComplianceEventAsync(
            ComplianceEventType.MarketManipulation,
            severity,
            userId,
            $"{alertType}: {description}",
            JsonSerializer.Serialize(patternData),
            cancellationToken);
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
            (event_id, event_type, severity, user_id, title, event_data, source, created_at, requires_action)
            VALUES (@eventId, @eventType, @severity, @userId, @title, @eventData::jsonb, @source, @createdAt, @requiresAction)";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("eventId", Guid.NewGuid());
        cmd.Parameters.AddWithValue("eventType", eventType.ToString());
        cmd.Parameters.AddWithValue("severity", severity.ToString());
        cmd.Parameters.AddWithValue("userId", (object?)userId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("title", title);
        cmd.Parameters.AddWithValue("eventData", (object?)eventData ?? DBNull.Value);
        cmd.Parameters.AddWithValue("source", "TradeSurveillanceService");
        cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("requiresAction", severity >= ComplianceSeverity.High);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Get active surveillance alerts for review
    /// </summary>
    public async Task<List<SurveillanceAlert>> GetActiveAlertsAsync(CancellationToken cancellationToken = default)
    {
        var alerts = new List<SurveillanceAlert>();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = @"
            SELECT alert_id, alert_type, severity, symbol, exchange, user_id,
                   description, confidence_score, detection_time
            FROM surveillance_alerts
            WHERE status = 'Active'
            ORDER BY detection_time DESC
            LIMIT 100";

        await using var cmd = new NpgsqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            alerts.Add(new SurveillanceAlert
            {
                AlertId = reader.GetGuid(0),
                AlertType = reader.GetString(1),
                Severity = reader.GetString(2),
                Symbol = reader.GetString(3),
                Exchange = reader.GetString(4),
                UserId = reader.IsDBNull(5) ? null : reader.GetGuid(5),
                Description = reader.GetString(6),
                ConfidenceScore = reader.GetInt32(7),
                DetectionTime = reader.GetDateTime(8)
            });
        }

        return alerts;
    }
}

/// <summary>
/// Surveillance alert summary
/// </summary>
public class SurveillanceAlert
{
    public Guid AlertId { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int ConfidenceScore { get; set; }
    public DateTime DetectionTime { get; set; }
}
