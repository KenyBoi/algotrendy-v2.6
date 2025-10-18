using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Models;
using AlgoTrendy.Tests.TestHelpers.Builders;
using FluentAssertions;
using Xunit;

namespace AlgoTrendy.Tests.Unit.Core;

public class TradeTests
{
    [Fact]
    public void Trade_Creation_WithValidData_Succeeds()
    {
        // Arrange & Act
        var trade = new TradeBuilder()
            .WithSymbol("BTCUSDT")
            .WithSide(OrderSide.Buy)
            .WithPrice(50000m)
            .WithQuantity(1.0m)
            .WithFee(50m)
            .Build();

        // Assert
        trade.Should().NotBeNull();
        trade.Symbol.Should().Be("BTCUSDT");
        trade.Side.Should().Be(OrderSide.Buy);
        trade.Price.Should().Be(50000m);
        trade.Quantity.Should().Be(1.0m);
        trade.Fee.Should().Be(50m);
    }

    [Fact]
    public void NetAmount_ForBuyOrder_IncludesFee()
    {
        // Arrange
        var trade = new TradeBuilder()
            .WithSide(OrderSide.Buy)
            .WithPrice(50000m)
            .WithQuantity(1.0m)
            .WithQuoteQuantity(50000m)
            .WithFee(50m)
            .Build();

        // Act
        var netAmount = trade.NetAmount;

        // Assert
        netAmount.Should().Be(50050m); // 50000 + 50
    }

    [Fact]
    public void NetAmount_ForSellOrder_SubtractsFee()
    {
        // Arrange
        var trade = new TradeBuilder()
            .WithSide(OrderSide.Sell)
            .WithPrice(50000m)
            .WithQuantity(1.0m)
            .WithQuoteQuantity(50000m)
            .WithFee(50m)
            .Build();

        // Act
        var netAmount = trade.NetAmount;

        // Assert
        netAmount.Should().Be(49950m); // 50000 - 50
    }

    [Fact]
    public void NetAmount_WithZeroFee_EqualsQuoteQuantity()
    {
        // Arrange
        var trade = new TradeBuilder()
            .WithSide(OrderSide.Buy)
            .WithQuoteQuantity(50000m)
            .WithFee(0m)
            .Build();

        // Act
        var netAmount = trade.NetAmount;

        // Assert
        netAmount.Should().Be(50000m);
    }

    [Theory]
    [InlineData(OrderSide.Buy, 10000, 100, 10100)]  // Buy: add fee
    [InlineData(OrderSide.Sell, 10000, 100, 9900)]  // Sell: subtract fee
    [InlineData(OrderSide.Buy, 50000, 0, 50000)]    // Buy with no fee
    [InlineData(OrderSide.Sell, 50000, 0, 50000)]   // Sell with no fee
    public void NetAmount_CalculatesCorrectly_ForVariousScenarios(
        OrderSide side, decimal quoteQuantity, decimal fee, decimal expectedNetAmount)
    {
        // Arrange
        var trade = new TradeBuilder()
            .WithSide(side)
            .WithQuoteQuantity(quoteQuantity)
            .WithFee(fee)
            .Build();

        // Act
        var netAmount = trade.NetAmount;

        // Assert
        netAmount.Should().Be(expectedNetAmount);
    }

    [Fact]
    public void Trade_WithOrderId_LinksToOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid().ToString();

        // Act
        var trade = new TradeBuilder()
            .WithOrderId(orderId)
            .Build();

        // Assert
        trade.OrderId.Should().Be(orderId);
    }

    [Fact]
    public void Trade_WithExchangeTradeId_StoresCorrectly()
    {
        // Arrange
        var exchangeTradeId = "12345678";

        // Act
        var trade = new TradeBuilder()
            .WithExchangeTradeId(exchangeTradeId)
            .Build();

        // Assert
        trade.ExchangeTradeId.Should().Be(exchangeTradeId);
    }

    [Fact]
    public void Trade_WithStrategyId_StoresCorrectly()
    {
        // Arrange
        var strategyId = "rsi-strategy-v1";

        // Act
        var trade = new TradeBuilder()
            .WithStrategyId(strategyId)
            .Build();

        // Assert
        trade.StrategyId.Should().Be(strategyId);
    }

    [Fact]
    public void Trade_WithExecutedAt_StoresTimestamp()
    {
        // Arrange
        var executedAt = DateTime.UtcNow.AddMinutes(-5);

        // Act
        var trade = new TradeBuilder()
            .WithExecutedAt(executedAt)
            .Build();

        // Assert
        trade.ExecutedAt.Should().Be(executedAt);
    }

    [Fact]
    public void Trade_WithFeeCurrency_StoresCorrectly()
    {
        // Arrange & Act
        var trade = new TradeBuilder()
            .WithFeeCurrency("BNB")
            .Build();

        // Assert
        trade.FeeCurrency.Should().Be("BNB");
    }

    [Fact]
    public void Trade_QuantityCalculation_AutoCalculatesQuoteQuantity()
    {
        // Arrange & Act
        var trade = new TradeBuilder()
            .WithPrice(50000m)
            .WithQuantity(2.5m)  // Should auto-calculate QuoteQuantity
            .Build();

        // Assert
        trade.Quantity.Should().Be(2.5m);
        trade.QuoteQuantity.Should().Be(125000m); // 50000 * 2.5
    }

    [Theory]
    [InlineData(1000, 0.5, 500)]
    [InlineData(50000, 1.0, 50000)]
    [InlineData(30000, 0.1, 3000)]
    public void Trade_QuoteQuantity_CalculatesFromPriceAndQuantity(
        decimal price, decimal quantity, decimal expectedQuoteQuantity)
    {
        // Arrange & Act
        var trade = new TradeBuilder()
            .WithPrice(price)
            .WithQuantity(quantity)
            .Build();

        // Assert
        trade.QuoteQuantity.Should().Be(expectedQuoteQuantity);
    }

    [Fact]
    public void Trade_WithExchange_StoresExchangeName()
    {
        // Arrange & Act
        var trade = new TradeBuilder()
            .WithExchange("okx")
            .Build();

        // Assert
        trade.Exchange.Should().Be("okx");
    }

    [Fact]
    public void Trade_BuyTrade_CalculatesNetCostCorrectly()
    {
        // Arrange
        var trade = new TradeBuilder()
            .WithSide(OrderSide.Buy)
            .WithPrice(50000m)
            .WithQuantity(2.0m)
            .WithQuoteQuantity(100000m)
            .WithFee(100m)
            .WithFeeCurrency("USDT")
            .Build();

        // Act
        var netAmount = trade.NetAmount;

        // Assert
        // For a buy, total cost = quote quantity + fee
        netAmount.Should().Be(100100m);
    }

    [Fact]
    public void Trade_SellTrade_CalculatesNetProceedsCorrectly()
    {
        // Arrange
        var trade = new TradeBuilder()
            .WithSide(OrderSide.Sell)
            .WithPrice(52000m)
            .WithQuantity(2.0m)
            .WithQuoteQuantity(104000m)
            .WithFee(104m)
            .WithFeeCurrency("USDT")
            .Build();

        // Act
        var netAmount = trade.NetAmount;

        // Assert
        // For a sell, net proceeds = quote quantity - fee
        netAmount.Should().Be(103896m);
    }

    [Fact]
    public void Trade_WithMetadata_StoresJsonString()
    {
        // Arrange & Act
        var metadata = "{\"execution_type\":\"maker\"}";
        var trade = new TradeBuilder()
            .WithMetadata(metadata)
            .Build();

        // Assert
        trade.Metadata.Should().Be(metadata);
    }

    [Fact]
    public void Trade_PartialFill_HasPartialQuantity()
    {
        // Arrange & Act
        var trade = new TradeBuilder()
            .WithQuantity(0.5m)  // Partial fill of larger order
            .WithPrice(50000m)
            .Build();

        // Assert
        trade.Quantity.Should().Be(0.5m);
        trade.QuoteQuantity.Should().Be(25000m);
    }
}
