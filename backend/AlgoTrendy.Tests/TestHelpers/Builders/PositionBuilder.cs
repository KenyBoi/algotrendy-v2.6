using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Models;

namespace AlgoTrendy.Tests.TestHelpers.Builders;

/// <summary>
/// Builder for creating test Position instances with fluent API
/// </summary>
public class PositionBuilder
{
    private string _positionId = Guid.NewGuid().ToString();
    private string _symbol = "BTCUSDT";
    private string _exchange = "binance";
    private OrderSide _side = OrderSide.Buy;
    private decimal _quantity = 1.0m;
    private decimal _entryPrice = 50000m;
    private decimal _currentPrice = 50000m;
    private decimal? _stopLoss;
    private decimal? _takeProfit;
    private string? _strategyId;
    private string? _openOrderId;
    private DateTime _openedAt = DateTime.UtcNow;
    private DateTime _updatedAt = DateTime.UtcNow;

    public PositionBuilder WithPositionId(string positionId)
    {
        _positionId = positionId;
        return this;
    }

    public PositionBuilder WithSymbol(string symbol)
    {
        _symbol = symbol;
        return this;
    }

    public PositionBuilder WithExchange(string exchange)
    {
        _exchange = exchange;
        return this;
    }

    public PositionBuilder WithSide(OrderSide side)
    {
        _side = side;
        return this;
    }

    public PositionBuilder WithQuantity(decimal quantity)
    {
        _quantity = quantity;
        return this;
    }

    public PositionBuilder WithEntryPrice(decimal entryPrice)
    {
        _entryPrice = entryPrice;
        return this;
    }

    public PositionBuilder WithCurrentPrice(decimal currentPrice)
    {
        _currentPrice = currentPrice;
        return this;
    }

    public PositionBuilder WithStopLoss(decimal? stopLoss)
    {
        _stopLoss = stopLoss;
        return this;
    }

    public PositionBuilder WithTakeProfit(decimal? takeProfit)
    {
        _takeProfit = takeProfit;
        return this;
    }

    public PositionBuilder WithStrategyId(string? strategyId)
    {
        _strategyId = strategyId;
        return this;
    }

    public PositionBuilder WithOpenOrderId(string? openOrderId)
    {
        _openOrderId = openOrderId;
        return this;
    }

    public PositionBuilder WithOpenedAt(DateTime openedAt)
    {
        _openedAt = openedAt;
        return this;
    }

    public PositionBuilder WithUpdatedAt(DateTime updatedAt)
    {
        _updatedAt = updatedAt;
        return this;
    }

    /// <summary>
    /// Creates a profitable long position
    /// </summary>
    public PositionBuilder InProfit(decimal profitPercent = 10m)
    {
        _side = OrderSide.Buy;
        _entryPrice = 50000m;
        _currentPrice = _entryPrice * (1 + profitPercent / 100m);
        return this;
    }

    /// <summary>
    /// Creates a losing long position
    /// </summary>
    public PositionBuilder InLoss(decimal lossPercent = 5m)
    {
        _side = OrderSide.Buy;
        _entryPrice = 50000m;
        _currentPrice = _entryPrice * (1 - lossPercent / 100m);
        return this;
    }

    /// <summary>
    /// Creates a position at break-even
    /// </summary>
    public PositionBuilder AtBreakEven()
    {
        _entryPrice = 50000m;
        _currentPrice = 50000m;
        return this;
    }

    public Position Build()
    {
        return new Position
        {
            PositionId = _positionId,
            Symbol = _symbol,
            Exchange = _exchange,
            Side = _side,
            Quantity = _quantity,
            EntryPrice = _entryPrice,
            CurrentPrice = _currentPrice,
            StopLoss = _stopLoss,
            TakeProfit = _takeProfit,
            StrategyId = _strategyId,
            OpenOrderId = _openOrderId,
            OpenedAt = _openedAt,
            UpdatedAt = _updatedAt
        };
    }
}
