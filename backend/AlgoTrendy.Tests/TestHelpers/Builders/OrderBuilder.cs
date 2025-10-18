using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Models;

namespace AlgoTrendy.Tests.TestHelpers.Builders;

/// <summary>
/// Builder for creating test Order instances with fluent API
/// </summary>
public class OrderBuilder
{
    private string _orderId = Guid.NewGuid().ToString();
    private string? _exchangeOrderId;
    private string _symbol = "BTCUSDT";
    private string _exchange = "binance";
    private OrderSide _side = OrderSide.Buy;
    private OrderType _type = OrderType.Market;
    private OrderStatus _status = OrderStatus.Pending;
    private decimal _quantity = 1.0m;
    private decimal _filledQuantity = 0m;
    private decimal? _price;
    private decimal? _stopPrice;
    private decimal? _averageFillPrice;
    private string? _strategyId;
    private DateTime _createdAt = DateTime.UtcNow;
    private DateTime _updatedAt = DateTime.UtcNow;
    private DateTime? _submittedAt;
    private DateTime? _closedAt;
    private string? _metadata;

    public OrderBuilder WithOrderId(string orderId)
    {
        _orderId = orderId;
        return this;
    }

    public OrderBuilder WithExchangeOrderId(string exchangeOrderId)
    {
        _exchangeOrderId = exchangeOrderId;
        return this;
    }

    public OrderBuilder WithSymbol(string symbol)
    {
        _symbol = symbol;
        return this;
    }

    public OrderBuilder WithExchange(string exchange)
    {
        _exchange = exchange;
        return this;
    }

    public OrderBuilder WithSide(OrderSide side)
    {
        _side = side;
        return this;
    }

    public OrderBuilder WithType(OrderType type)
    {
        _type = type;
        return this;
    }

    public OrderBuilder WithStatus(OrderStatus status)
    {
        _status = status;
        return this;
    }

    public OrderBuilder WithQuantity(decimal quantity)
    {
        _quantity = quantity;
        return this;
    }

    public OrderBuilder WithFilledQuantity(decimal filledQuantity)
    {
        _filledQuantity = filledQuantity;
        return this;
    }

    public OrderBuilder WithPrice(decimal? price)
    {
        _price = price;
        return this;
    }

    public OrderBuilder WithStopPrice(decimal? stopPrice)
    {
        _stopPrice = stopPrice;
        return this;
    }

    public OrderBuilder WithAverageFillPrice(decimal? averageFillPrice)
    {
        _averageFillPrice = averageFillPrice;
        return this;
    }

    public OrderBuilder WithStrategyId(string? strategyId)
    {
        _strategyId = strategyId;
        return this;
    }

    public OrderBuilder WithCreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public OrderBuilder WithUpdatedAt(DateTime updatedAt)
    {
        _updatedAt = updatedAt;
        return this;
    }

    public OrderBuilder WithSubmittedAt(DateTime? submittedAt)
    {
        _submittedAt = submittedAt;
        return this;
    }

    public OrderBuilder WithClosedAt(DateTime? closedAt)
    {
        _closedAt = closedAt;
        return this;
    }

    public OrderBuilder WithMetadata(string? metadata)
    {
        _metadata = metadata;
        return this;
    }

    public Order Build()
    {
        return new Order
        {
            OrderId = _orderId,
            ExchangeOrderId = _exchangeOrderId,
            Symbol = _symbol,
            Exchange = _exchange,
            Side = _side,
            Type = _type,
            Status = _status,
            Quantity = _quantity,
            FilledQuantity = _filledQuantity,
            Price = _price,
            StopPrice = _stopPrice,
            AverageFillPrice = _averageFillPrice,
            StrategyId = _strategyId,
            CreatedAt = _createdAt,
            UpdatedAt = _updatedAt,
            SubmittedAt = _submittedAt,
            ClosedAt = _closedAt,
            Metadata = _metadata
        };
    }
}
