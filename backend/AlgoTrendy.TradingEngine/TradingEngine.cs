using AlgoTrendy.Core.Configuration;
using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace AlgoTrendy.TradingEngine;

/// <summary>
/// Core trading engine for order lifecycle management and position tracking
/// </summary>
public class TradingEngine : ITradingEngine
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMarketDataRepository _marketDataRepository;
    private readonly IBroker _broker;
    private readonly RiskSettings _riskSettings;
    private readonly ILogger<TradingEngine> _logger;

    // In-memory position tracking
    private readonly ConcurrentDictionary<string, Position> _activePositions = new();

    // Events
    public event EventHandler<Order>? OrderStatusChanged;
    public event EventHandler<Position>? PositionOpened;
    public event EventHandler<Position>? PositionClosed;
    public event EventHandler<Position>? PositionUpdated;

    public TradingEngine(
        IOrderRepository orderRepository,
        IMarketDataRepository marketDataRepository,
        IBroker broker,
        IOptions<RiskSettings> riskSettings,
        ILogger<TradingEngine> logger)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _marketDataRepository = marketDataRepository ?? throw new ArgumentNullException(nameof(marketDataRepository));
        _broker = broker ?? throw new ArgumentNullException(nameof(broker));
        _riskSettings = riskSettings?.Value ?? throw new ArgumentNullException(nameof(riskSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Submits a new order to the trading engine
    /// </summary>
    public async Task<Order> SubmitOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Submitting order {OrderId} for {Symbol} - {Side} {Quantity} @ {Type}",
            order.OrderId, order.Symbol, order.Side, order.Quantity, order.Type);

        try
        {
            // Validate order before submission
            var (isValid, errorMessage) = await ValidateOrderAsync(order, cancellationToken);
            if (!isValid)
            {
                _logger.LogWarning("Order validation failed: {ErrorMessage}", errorMessage);
                order.Status = OrderStatus.Rejected;
                order.UpdatedAt = DateTime.UtcNow;
                await _orderRepository.UpdateAsync(order, cancellationToken);
                throw new InvalidOperationException($"Order validation failed: {errorMessage}");
            }

            // Place order with broker
            var request = new OrderRequest
            {
                Symbol = order.Symbol,
                Exchange = order.Exchange,
                Side = order.Side,
                Type = order.Type,
                Quantity = order.Quantity,
                Price = order.Price,
                StopPrice = order.StopPrice,
                StrategyId = order.StrategyId,
                Metadata = order.Metadata
            };

            var placedOrder = await _broker.PlaceOrderAsync(request, cancellationToken);

            // Update order with exchange information
            order.ExchangeOrderId = placedOrder.ExchangeOrderId;
            order.Status = OrderStatus.Open;
            order.SubmittedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            // Persist to repository
            await _orderRepository.UpdateAsync(order, cancellationToken);

            _logger.LogInformation(
                "Order {OrderId} submitted successfully. Exchange order ID: {ExchangeOrderId}",
                order.OrderId, order.ExchangeOrderId);

            // Fire event
            OrderStatusChanged?.Invoke(this, order);

            // If market order, it might be filled immediately - check status
            if (order.Type == OrderType.Market)
            {
                await Task.Delay(500, cancellationToken); // Small delay for exchange processing
                await UpdateOrderStatusAsync(order, cancellationToken);
            }

            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit order {OrderId}", order.OrderId);
            order.Status = OrderStatus.Rejected;
            order.UpdatedAt = DateTime.UtcNow;
            await _orderRepository.UpdateAsync(order, cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Cancels an active order
    /// </summary>
    public async Task<Order> CancelOrderAsync(string orderId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling order {OrderId}", orderId);

        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order == null)
        {
            throw new InvalidOperationException($"Order {orderId} not found");
        }

        if (!order.IsActive)
        {
            _logger.LogWarning("Order {OrderId} is not active (status: {Status})", orderId, order.Status);
            return order;
        }

        try
        {
            if (string.IsNullOrEmpty(order.ExchangeOrderId))
            {
                throw new InvalidOperationException("Order has no exchange order ID");
            }

            // Cancel with broker
            var cancelledOrder = await _broker.CancelOrderAsync(
                order.ExchangeOrderId,
                order.Symbol,
                cancellationToken);

            // Update order status
            order.Status = OrderStatus.Cancelled;
            order.ClosedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            await _orderRepository.UpdateAsync(order, cancellationToken);

            _logger.LogInformation("Order {OrderId} cancelled successfully", orderId);

            // Fire event
            OrderStatusChanged?.Invoke(this, order);

            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel order {OrderId}", orderId);
            throw;
        }
    }

    /// <summary>
    /// Gets the current status of an order from the exchange
    /// </summary>
    public async Task<Order> GetOrderStatusAsync(string orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order == null)
        {
            throw new InvalidOperationException($"Order {orderId} not found");
        }

        if (order.IsTerminal)
        {
            return order; // No need to check exchange for terminal orders
        }

        await UpdateOrderStatusAsync(order, cancellationToken);
        return order;
    }

    /// <summary>
    /// Syncs all active orders with the exchange
    /// </summary>
    public async Task<int> SyncActiveOrdersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Syncing active orders with exchange");

        var activeOrders = await _orderRepository.GetActiveOrdersAsync(cancellationToken);
        var syncedCount = 0;

        foreach (var order in activeOrders)
        {
            try
            {
                await UpdateOrderStatusAsync(order, cancellationToken);
                syncedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync order {OrderId}", order.OrderId);
            }
        }

        _logger.LogInformation("Synced {Count} active orders", syncedCount);
        return syncedCount;
    }

    /// <summary>
    /// Validates an order before submission
    /// </summary>
    public async Task<(bool IsValid, string? ErrorMessage)> ValidateOrderAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        if (!_riskSettings.EnableRiskValidation)
        {
            return (true, null);
        }

        // Check minimum order size
        var orderValue = order.Quantity * (order.Price ?? await _broker.GetCurrentPriceAsync(order.Symbol, cancellationToken));
        if (orderValue < _riskSettings.MinOrderSize)
        {
            return (false, $"Order value {orderValue:F2} is below minimum {_riskSettings.MinOrderSize:F2}");
        }

        // Check maximum order size
        if (_riskSettings.MaxOrderSize.HasValue && orderValue > _riskSettings.MaxOrderSize.Value)
        {
            return (false, $"Order value {orderValue:F2} exceeds maximum {_riskSettings.MaxOrderSize.Value:F2}");
        }

        // Check account balance
        var balance = await GetBalanceAsync(order.Exchange, "USDT", cancellationToken);
        var maxPositionSize = balance * (_riskSettings.MaxPositionSizePercent / 100m);

        if (orderValue > maxPositionSize)
        {
            return (false, $"Order value {orderValue:F2} exceeds max position size {maxPositionSize:F2} ({_riskSettings.MaxPositionSizePercent}% of balance)");
        }

        // Check maximum concurrent positions
        if (_activePositions.Count >= _riskSettings.MaxConcurrentPositions)
        {
            return (false, $"Maximum concurrent positions ({_riskSettings.MaxConcurrentPositions}) reached");
        }

        // Check total exposure
        var totalExposure = _activePositions.Values.Sum(p => p.CurrentValue);
        var maxTotalExposure = balance * (_riskSettings.MaxTotalExposurePercent / 100m);

        if (totalExposure + orderValue > maxTotalExposure)
        {
            return (false, $"Total exposure would exceed limit {maxTotalExposure:F2} ({_riskSettings.MaxTotalExposurePercent}% of balance)");
        }

        return (true, null);
    }

    /// <summary>
    /// Gets account balance for a specific currency
    /// </summary>
    public async Task<decimal> GetBalanceAsync(
        string exchange,
        string currency,
        CancellationToken cancellationToken = default)
    {
        return await _broker.GetBalanceAsync(currency, cancellationToken);
    }

    /// <summary>
    /// Updates order status from exchange and handles fills
    /// </summary>
    private async Task UpdateOrderStatusAsync(Order order, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(order.ExchangeOrderId))
        {
            return;
        }

        try
        {
            var updatedOrder = await _broker.GetOrderStatusAsync(
                order.ExchangeOrderId,
                order.Symbol,
                cancellationToken);

            var statusChanged = order.Status != updatedOrder.Status;
            var fillChanged = order.FilledQuantity != updatedOrder.FilledQuantity;

            if (statusChanged || fillChanged)
            {
                var previousStatus = order.Status;

                // Update order properties
                order.Status = updatedOrder.Status;
                order.FilledQuantity = updatedOrder.FilledQuantity;
                order.AverageFillPrice = updatedOrder.AverageFillPrice;
                order.UpdatedAt = DateTime.UtcNow;

                if (order.IsTerminal && !order.ClosedAt.HasValue)
                {
                    order.ClosedAt = DateTime.UtcNow;
                }

                await _orderRepository.UpdateAsync(order, cancellationToken);

                _logger.LogInformation(
                    "Order {OrderId} status updated: {PreviousStatus} -> {NewStatus}, Filled: {Filled}/{Total}",
                    order.OrderId, previousStatus, order.Status, order.FilledQuantity, order.Quantity);

                // Fire event
                OrderStatusChanged?.Invoke(this, order);

                // Handle position tracking
                if (order.Status == OrderStatus.Filled)
                {
                    await HandleOrderFillAsync(order, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update order status for {OrderId}", order.OrderId);
            throw;
        }
    }

    /// <summary>
    /// Handles order fill and updates position tracking
    /// </summary>
    private async Task HandleOrderFillAsync(Order order, CancellationToken cancellationToken)
    {
        var positionKey = $"{order.Exchange}:{order.Symbol}";

        if (order.Side == OrderSide.Buy)
        {
            // Opening a long position
            var position = new Position
            {
                PositionId = Guid.NewGuid().ToString(),
                Symbol = order.Symbol,
                Exchange = order.Exchange,
                Side = order.Side,
                Quantity = order.FilledQuantity,
                EntryPrice = order.AverageFillPrice ?? order.Price ?? 0,
                CurrentPrice = order.AverageFillPrice ?? order.Price ?? 0,
                StrategyId = order.StrategyId,
                OpenOrderId = order.OrderId,
                OpenedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Apply default stop loss and take profit if configured
            if (_riskSettings.DefaultStopLossPercent > 0)
            {
                position.StopLoss = position.EntryPrice * (1 - _riskSettings.DefaultStopLossPercent / 100m);
            }

            if (_riskSettings.DefaultTakeProfitPercent > 0)
            {
                position.TakeProfit = position.EntryPrice * (1 + _riskSettings.DefaultTakeProfitPercent / 100m);
            }

            _activePositions[positionKey] = position;

            _logger.LogInformation(
                "Position opened: {Symbol} {Side} {Quantity} @ {EntryPrice}",
                position.Symbol, position.Side, position.Quantity, position.EntryPrice);

            PositionOpened?.Invoke(this, position);
        }
        else
        {
            // Closing a long position (or opening short, but we'll focus on closing for now)
            if (_activePositions.TryRemove(positionKey, out var position))
            {
                position.CurrentPrice = order.AverageFillPrice ?? order.Price ?? 0;
                position.UpdatedAt = DateTime.UtcNow;

                var realizedPnL = position.UnrealizedPnL;
                var realizedPnLPercent = position.UnrealizedPnLPercent;

                _logger.LogInformation(
                    "Position closed: {Symbol} PnL: {PnL:F2} ({PnLPercent:F2}%)",
                    position.Symbol, realizedPnL, realizedPnLPercent);

                PositionClosed?.Invoke(this, position);
            }
        }

        // Update current prices for all positions
        await UpdatePositionPricesAsync(cancellationToken);
    }

    /// <summary>
    /// Updates current prices for all active positions
    /// </summary>
    private async Task UpdatePositionPricesAsync(CancellationToken cancellationToken)
    {
        foreach (var position in _activePositions.Values)
        {
            try
            {
                var currentPrice = await _broker.GetCurrentPriceAsync(position.Symbol, cancellationToken);
                position.CurrentPrice = currentPrice;
                position.UpdatedAt = DateTime.UtcNow;

                PositionUpdated?.Invoke(this, position);

                // Check stop loss and take profit
                if (position.IsStopLossHit)
                {
                    _logger.LogWarning(
                        "Stop loss hit for {Symbol}: Current {CurrentPrice} <= Stop {StopLoss}",
                        position.Symbol, position.CurrentPrice, position.StopLoss);
                }

                if (position.IsTakeProfitHit)
                {
                    _logger.LogInformation(
                        "Take profit hit for {Symbol}: Current {CurrentPrice} >= Target {TakeProfit}",
                        position.Symbol, position.CurrentPrice, position.TakeProfit);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update price for position {Symbol}", position.Symbol);
            }
        }
    }

    /// <summary>
    /// Gets all active positions
    /// </summary>
    public IEnumerable<Position> GetActivePositions()
    {
        return _activePositions.Values.ToList();
    }

    /// <summary>
    /// Gets position for a specific symbol
    /// </summary>
    public Position? GetPosition(string exchange, string symbol)
    {
        var positionKey = $"{exchange}:{symbol}";
        _activePositions.TryGetValue(positionKey, out var position);
        return position;
    }
}
