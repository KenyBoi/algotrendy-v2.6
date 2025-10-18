using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Models;

namespace AlgoTrendy.Tests.TestHelpers.Builders;

/// <summary>
/// Builder for creating test Trade instances with fluent API
/// </summary>
public class TradeBuilder
{
    private string _tradeId = Guid.NewGuid().ToString();
    private string _orderId = Guid.NewGuid().ToString();
    private string? _exchangeTradeId;
    private string _symbol = "BTCUSDT";
    private string _exchange = "binance";
    private OrderSide _side = OrderSide.Buy;
    private decimal _price = 50000m;
    private decimal _quantity = 1.0m;
    private decimal _quoteQuantity = 50000m;
    private decimal _fee = 50m;
    private string _feeCurrency = "USDT";
    private string? _strategyId;
    private DateTime _executedAt = DateTime.UtcNow;
    private string? _metadata;

    public TradeBuilder WithTradeId(string tradeId)
    {
        _tradeId = tradeId;
        return this;
    }

    public TradeBuilder WithOrderId(string orderId)
    {
        _orderId = orderId;
        return this;
    }

    public TradeBuilder WithExchangeTradeId(string? exchangeTradeId)
    {
        _exchangeTradeId = exchangeTradeId;
        return this;
    }

    public TradeBuilder WithSymbol(string symbol)
    {
        _symbol = symbol;
        return this;
    }

    public TradeBuilder WithExchange(string exchange)
    {
        _exchange = exchange;
        return this;
    }

    public TradeBuilder WithSide(OrderSide side)
    {
        _side = side;
        return this;
    }

    public TradeBuilder WithPrice(decimal price)
    {
        _price = price;
        return this;
    }

    public TradeBuilder WithQuantity(decimal quantity)
    {
        _quantity = quantity;
        // Auto-calculate quote quantity
        _quoteQuantity = _price * quantity;
        return this;
    }

    public TradeBuilder WithQuoteQuantity(decimal quoteQuantity)
    {
        _quoteQuantity = quoteQuantity;
        return this;
    }

    public TradeBuilder WithFee(decimal fee)
    {
        _fee = fee;
        return this;
    }

    public TradeBuilder WithFeeCurrency(string feeCurrency)
    {
        _feeCurrency = feeCurrency;
        return this;
    }

    public TradeBuilder WithStrategyId(string? strategyId)
    {
        _strategyId = strategyId;
        return this;
    }

    public TradeBuilder WithExecutedAt(DateTime executedAt)
    {
        _executedAt = executedAt;
        return this;
    }

    public TradeBuilder WithMetadata(string? metadata)
    {
        _metadata = metadata;
        return this;
    }

    public Trade Build()
    {
        return new Trade
        {
            TradeId = _tradeId,
            OrderId = _orderId,
            ExchangeTradeId = _exchangeTradeId,
            Symbol = _symbol,
            Exchange = _exchange,
            Side = _side,
            Price = _price,
            Quantity = _quantity,
            QuoteQuantity = _quoteQuantity,
            Fee = _fee,
            FeeCurrency = _feeCurrency,
            StrategyId = _strategyId,
            ExecutedAt = _executedAt,
            Metadata = _metadata
        };
    }
}
