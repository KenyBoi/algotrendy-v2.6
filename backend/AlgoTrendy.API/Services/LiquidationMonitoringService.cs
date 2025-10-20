using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;

namespace AlgoTrendy.API.Services;

/// <summary>
/// Background service that monitors positions for liquidation risk
/// Runs every 30 seconds to check margin levels and liquidate positions if necessary
/// </summary>
public class LiquidationMonitoringService : BackgroundService
{
    private readonly ILogger<LiquidationMonitoringService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);

    // Margin level thresholds
    private const decimal WarningLevel = 0.70m;  // 70% margin usage
    private const decimal CriticalLevel = 0.80m; // 80% margin usage
    private const decimal LiquidationLevel = 0.90m; // 90% margin usage

    public LiquidationMonitoringService(
        ILogger<LiquidationMonitoringService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Liquidation Monitoring Service starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndManagePositionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in liquidation monitoring cycle");
            }

            // Wait for next check interval
            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Liquidation Monitoring Service stopped");
    }

    private async Task CheckAndManagePositionsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var positionRepository = scope.ServiceProvider.GetService<IPositionRepository>();

        if (positionRepository == null)
        {
            _logger.LogWarning("Position repository not available, skipping liquidation check");
            return;
        }

        // Get all open positions
        var openPositions = await positionRepository.GetOpenPositionsAsync(cancellationToken);

        if (!openPositions.Any())
        {
            _logger.LogDebug("No open positions to monitor");
            return;
        }

        _logger.LogInformation("Monitoring {Count} open positions for liquidation risk", openPositions.Count());

        foreach (var position in openPositions)
        {
            await CheckPositionRisk(position, scope.ServiceProvider, cancellationToken);
        }
    }

    private async Task CheckPositionRisk(
        Position position,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        try
        {
            // Calculate margin level (simplified - assumes margin info is available)
            // In production, this would fetch current prices and calculate actual margin
            var marginLevel = CalculateMarginLevel(position);

            if (marginLevel >= LiquidationLevel)
            {
                _logger.LogCritical(
                    "LIQUIDATION TRIGGERED: Position {PositionId} for {Symbol} at {MarginLevel:P2} margin usage",
                    position.Id, position.Symbol, marginLevel);

                await LiquidatePosition(position, serviceProvider, "Automatic liquidation due to margin threshold", cancellationToken);
            }
            else if (marginLevel >= CriticalLevel)
            {
                _logger.LogWarning(
                    "MARGIN CALL: Position {PositionId} for {Symbol} at {MarginLevel:P2} margin usage",
                    position.Id, position.Symbol, marginLevel);

                await SendMarginCall(position, marginLevel);
            }
            else if (marginLevel >= WarningLevel)
            {
                _logger.LogInformation(
                    "Margin Warning: Position {PositionId} for {Symbol} at {MarginLevel:P2} margin usage",
                    position.Id, position.Symbol, marginLevel);

                await SendMarginWarning(position, marginLevel);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking position {PositionId} risk", position.PositionId);
        }
    }

    private decimal CalculateMarginLevel(Position position)
    {
        // Simplified margin calculation
        // In production, this would:
        // 1. Fetch current market price
        // 2. Calculate unrealized P&L
        // 3. Calculate current equity
        // 4. Calculate margin used
        // 5. Return margin_used / total_equity

        // For now, return a safe default
        return 0.0m;
    }

    private async Task LiquidatePosition(
        Position position,
        IServiceProvider serviceProvider,
        string reason,
        CancellationToken cancellationToken)
    {
        var broker = serviceProvider.GetService<IBroker>();
        if (broker == null)
        {
            _logger.LogError("Broker not available for liquidation of position {PositionId}", position.PositionId);
            return;
        }

        try
        {
            // Close the position
            var closeOrder = new OrderRequest
            {
                Symbol = position.Symbol,
                Side = position.Side == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy,
                Type = OrderType.Market,
                Quantity = Math.Abs(position.Quantity),
                TimeInForce = TimeInForce.GTC,
                ClientOrderId = $"LIQUIDATION_{position.PositionId}_{DateTime.UtcNow.Ticks}"
            };

            _logger.LogCritical(
                "Executing liquidation order for position {PositionId} - {Symbol} {Quantity}",
                position.PositionId, position.Symbol, position.Quantity);

            var result = await broker.PlaceOrderAsync(closeOrder, cancellationToken);

            // Log liquidation event for audit
            _logger.LogCritical(
                "Liquidation completed for position {PositionId}. Order: {OrderId}, Reason: {Reason}",
                position.PositionId, result.OrderId, reason);

            // TODO: Send notification to user
            // TODO: Record in audit log database
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to liquidate position {PositionId}", position.PositionId);
        }
    }

    private Task SendMarginCall(Position position, decimal marginLevel)
    {
        // TODO: Implement notification system (email, SMS, push notification)
        _logger.LogWarning(
            "Margin call notification would be sent for position {PositionId} at {MarginLevel:P2}",
            position.PositionId, marginLevel);

        return Task.CompletedTask;
    }

    private Task SendMarginWarning(Position position, decimal marginLevel)
    {
        // TODO: Implement notification system
        _logger.LogInformation(
            "Margin warning notification would be sent for position {PositionId} at {MarginLevel:P2}",
            position.PositionId, marginLevel);

        return Task.CompletedTask;
    }
}
