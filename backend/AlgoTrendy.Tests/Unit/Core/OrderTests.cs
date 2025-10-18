using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Models;
using AlgoTrendy.Tests.TestHelpers.Builders;
using FluentAssertions;
using Xunit;

namespace AlgoTrendy.Tests.Unit.Core;

public class OrderTests
{
    [Fact]
    public void Order_Creation_WithValidData_Succeeds()
    {
        // Arrange & Act
        var order = new OrderBuilder()
            .WithSymbol("BTCUSDT")
            .WithExchange("binance")
            .WithSide(OrderSide.Buy)
            .WithType(OrderType.Limit)
            .WithQuantity(1.5m)
            .WithPrice(50000m)
            .Build();

        // Assert
        order.Should().NotBeNull();
        order.Symbol.Should().Be("BTCUSDT");
        order.Exchange.Should().Be("binance");
        order.Side.Should().Be(OrderSide.Buy);
        order.Type.Should().Be(OrderType.Limit);
        order.Quantity.Should().Be(1.5m);
        order.Price.Should().Be(50000m);
    }

    [Fact]
    public void RemainingQuantity_WithNoFills_ReturnsFullQuantity()
    {
        // Arrange
        var order = new OrderBuilder()
            .WithQuantity(10m)
            .WithFilledQuantity(0m)
            .Build();

        // Act
        var remaining = order.RemainingQuantity;

        // Assert
        remaining.Should().Be(10m);
    }

    [Fact]
    public void RemainingQuantity_WithPartialFills_ReturnsCorrectAmount()
    {
        // Arrange
        var order = new OrderBuilder()
            .WithQuantity(10m)
            .WithFilledQuantity(3.5m)
            .Build();

        // Act
        var remaining = order.RemainingQuantity;

        // Assert
        remaining.Should().Be(6.5m);
    }

    [Fact]
    public void RemainingQuantity_WithFullFill_ReturnsZero()
    {
        // Arrange
        var order = new OrderBuilder()
            .WithQuantity(10m)
            .WithFilledQuantity(10m)
            .Build();

        // Act
        var remaining = order.RemainingQuantity;

        // Assert
        remaining.Should().Be(0m);
    }

    [Theory]
    [InlineData(OrderStatus.Filled, true)]
    [InlineData(OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Rejected, true)]
    [InlineData(OrderStatus.Expired, true)]
    [InlineData(OrderStatus.Pending, false)]
    [InlineData(OrderStatus.Open, false)]
    [InlineData(OrderStatus.PartiallyFilled, false)]
    public void IsTerminal_ReturnsCorrectValue_ForOrderStatus(OrderStatus status, bool expectedIsTerminal)
    {
        // Arrange
        var order = new OrderBuilder()
            .WithStatus(status)
            .Build();

        // Act
        var isTerminal = order.IsTerminal;

        // Assert
        isTerminal.Should().Be(expectedIsTerminal);
    }

    [Theory]
    [InlineData(OrderStatus.Open, true)]
    [InlineData(OrderStatus.PartiallyFilled, true)]
    [InlineData(OrderStatus.Pending, false)]
    [InlineData(OrderStatus.Filled, false)]
    [InlineData(OrderStatus.Cancelled, false)]
    [InlineData(OrderStatus.Rejected, false)]
    [InlineData(OrderStatus.Expired, false)]
    public void IsActive_ReturnsCorrectValue_ForOrderStatus(OrderStatus status, bool expectedIsActive)
    {
        // Arrange
        var order = new OrderBuilder()
            .WithStatus(status)
            .Build();

        // Act
        var isActive = order.IsActive;

        // Assert
        isActive.Should().Be(expectedIsActive);
    }

    [Fact]
    public void Order_WithStopPrice_CreatesStopOrder()
    {
        // Arrange & Act
        var order = new OrderBuilder()
            .WithType(OrderType.StopLoss)
            .WithStopPrice(48000m)
            .WithPrice(47500m)
            .Build();

        // Assert
        order.Type.Should().Be(OrderType.StopLoss);
        order.StopPrice.Should().Be(48000m);
        order.Price.Should().Be(47500m);
    }

    [Fact]
    public void Order_MarketOrder_HasNullPrice()
    {
        // Arrange & Act
        var order = new OrderBuilder()
            .WithType(OrderType.Market)
            .WithPrice(null)
            .Build();

        // Assert
        order.Type.Should().Be(OrderType.Market);
        order.Price.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10.5)]
    public void Order_WithZeroOrNegativeQuantity_StillCreates(decimal quantity)
    {
        // Note: Validation should be done at service level, not model level
        // Arrange & Act
        var order = new OrderBuilder()
            .WithQuantity(quantity)
            .Build();

        // Assert
        order.Quantity.Should().Be(quantity);
    }

    [Fact]
    public void Order_UpdatedAt_CanBeModified()
    {
        // Arrange
        var initialTime = DateTime.UtcNow.AddHours(-1);
        var updatedTime = DateTime.UtcNow;
        var order = new OrderBuilder()
            .WithCreatedAt(initialTime)
            .WithUpdatedAt(initialTime)
            .Build();

        // Act
        order.UpdatedAt = updatedTime;

        // Assert
        order.UpdatedAt.Should().Be(updatedTime);
        order.UpdatedAt.Should().BeAfter(order.CreatedAt);
    }

    [Fact]
    public void Order_WithStrategy_StoresStrategyId()
    {
        // Arrange & Act
        var strategyId = "momentum-v1";
        var order = new OrderBuilder()
            .WithStrategyId(strategyId)
            .Build();

        // Assert
        order.StrategyId.Should().Be(strategyId);
    }

    [Fact]
    public void Order_BuyOrder_CreatesCorrectly()
    {
        // Arrange & Act
        var order = new OrderBuilder()
            .WithSide(OrderSide.Buy)
            .WithQuantity(2.5m)
            .WithPrice(50000m)
            .Build();

        // Assert
        order.Side.Should().Be(OrderSide.Buy);
        order.Quantity.Should().Be(2.5m);
    }

    [Fact]
    public void Order_SellOrder_CreatesCorrectly()
    {
        // Arrange & Act
        var order = new OrderBuilder()
            .WithSide(OrderSide.Sell)
            .WithQuantity(1.8m)
            .WithPrice(52000m)
            .Build();

        // Assert
        order.Side.Should().Be(OrderSide.Sell);
        order.Quantity.Should().Be(1.8m);
    }

    [Fact]
    public void Order_WithMetadata_StoresJsonString()
    {
        // Arrange & Act
        var metadata = "{\"note\":\"test order\"}";
        var order = new OrderBuilder()
            .WithMetadata(metadata)
            .Build();

        // Assert
        order.Metadata.Should().Be(metadata);
    }
}
