using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Models;
using AlgoTrendy.Tests.TestHelpers.Builders;
using FluentAssertions;
using Xunit;

namespace AlgoTrendy.Tests.Unit.Core;

/// <summary>
/// Unit tests for Position model
/// </summary>
public class PositionTests
{
    [Fact]
    public void Position_Creation_WithValidData_Succeeds()
    {
        // Arrange & Act
        var position = new PositionBuilder()
            .WithSymbol("BTCUSDT")
            .WithExchange("binance")
            .WithSide(OrderSide.Buy)
            .WithQuantity(1.0m)
            .WithEntryPrice(50000m)
            .WithCurrentPrice(51000m)
            .Build();

        // Assert
        position.Should().NotBeNull();
        position.Symbol.Should().Be("BTCUSDT");
        position.Exchange.Should().Be("binance");
        position.Side.Should().Be(OrderSide.Buy);
        position.Quantity.Should().Be(1.0m);
        position.EntryPrice.Should().Be(50000m);
        position.CurrentPrice.Should().Be(51000m);
    }

    [Fact]
    public void UnrealizedPnL_ForLongPosition_InProfit_CalculatesCorrectly()
    {
        // Arrange
        var position = new PositionBuilder()
            .WithSide(OrderSide.Buy)
            .WithQuantity(1.0m)
            .WithEntryPrice(50000m)
            .WithCurrentPrice(52000m)
            .Build();

        // Act
        var pnl = position.UnrealizedPnL;

        // Assert
        pnl.Should().Be(2000m); // (52000 - 50000) * 1.0
    }

    [Fact]
    public void UnrealizedPnL_ForLongPosition_InLoss_CalculatesCorrectly()
    {
        // Arrange
        var position = new PositionBuilder()
            .WithSide(OrderSide.Buy)
            .WithQuantity(1.0m)
            .WithEntryPrice(50000m)
            .WithCurrentPrice(48000m)
            .Build();

        // Act
        var pnl = position.UnrealizedPnL;

        // Assert
        pnl.Should().Be(-2000m); // (48000 - 50000) * 1.0
    }

    [Fact]
    public void UnrealizedPnL_ForShortPosition_InProfit_CalculatesCorrectly()
    {
        // Arrange
        var position = new PositionBuilder()
            .WithSide(OrderSide.Sell)
            .WithQuantity(1.0m)
            .WithEntryPrice(50000m)
            .WithCurrentPrice(48000m)
            .Build();

        // Act
        var pnl = position.UnrealizedPnL;

        // Assert
        pnl.Should().Be(2000m); // (50000 - 48000) * 1.0
    }

    [Fact]
    public void UnrealizedPnL_ForShortPosition_InLoss_CalculatesCorrectly()
    {
        // Arrange
        var position = new PositionBuilder()
            .WithSide(OrderSide.Sell)
            .WithQuantity(1.0m)
            .WithEntryPrice(50000m)
            .WithCurrentPrice(52000m)
            .Build();

        // Act
        var pnl = position.UnrealizedPnL;

        // Assert
        pnl.Should().Be(-2000m); // (50000 - 52000) * 1.0
    }

    [Theory]
    [InlineData(OrderSide.Buy, 50000, 51000, 1.0, 1000)]
    [InlineData(OrderSide.Buy, 50000, 49000, 1.0, -1000)]
    [InlineData(OrderSide.Sell, 50000, 49000, 1.0, 1000)]
    [InlineData(OrderSide.Sell, 50000, 51000, 1.0, -1000)]
    [InlineData(OrderSide.Buy, 30000, 33000, 2.5, 7500)]
    [InlineData(OrderSide.Sell, 30000, 27000, 2.5, 7500)]
    public void UnrealizedPnL_CalculatesCorrectly_ForVariousScenarios(
        OrderSide side, decimal entryPrice, decimal currentPrice, decimal quantity, decimal expectedPnL)
    {
        // Arrange
        var position = new PositionBuilder()
            .WithSide(side)
            .WithEntryPrice(entryPrice)
            .WithCurrentPrice(currentPrice)
            .WithQuantity(quantity)
            .Build();

        // Act
        var pnl = position.UnrealizedPnL;

        // Assert
        pnl.Should().Be(expectedPnL);
    }

    [Fact]
    public void UnrealizedPnLPercent_ForLongPosition_InProfit_CalculatesCorrectly()
    {
        // Arrange
        var position = new PositionBuilder()
            .WithSide(OrderSide.Buy)
            .WithEntryPrice(50000m)
            .WithCurrentPrice(55000m)
            .Build();

        // Act
        var pnlPercent = position.UnrealizedPnLPercent;

        // Assert
        pnlPercent.Should().BeApproximately(10.0m, 0.01m); // 10% profit
    }

    [Fact]
    public void UnrealizedPnLPercent_ForLongPosition_InLoss_CalculatesCorrectly()
    {
        // Arrange
        var position = new PositionBuilder()
            .WithSide(OrderSide.Buy)
            .WithEntryPrice(50000m)
            .WithCurrentPrice(45000m)
            .Build();

        // Act
        var pnlPercent = position.UnrealizedPnLPercent;

        // Assert
        pnlPercent.Should().BeApproximately(-10.0m, 0.01m); // 10% loss
    }

    [Fact]
    public void UnrealizedPnLPercent_WithZeroEntryPrice_ReturnsZero()
    {
        // Arrange
        var position = new PositionBuilder()
            .WithEntryPrice(0m)
            .WithCurrentPrice(50000m)
            .Build();

        // Act
        var pnlPercent = position.UnrealizedPnLPercent;

        // Assert
        pnlPercent.Should().Be(0m);
    }

    [Fact]
    public void EntryValue_CalculatesCorrectly()
    {
        // Arrange
        var position = new PositionBuilder()
            .WithQuantity(2.5m)
            .WithEntryPrice(50000m)
            .Build();

        // Act
        var entryValue = position.EntryValue;

        // Assert
        entryValue.Should().Be(125000m); // 2.5 * 50000
    }

    [Fact]
    public void CurrentValue_CalculatesCorrectly()
    {
        // Arrange
        var position = new PositionBuilder()
            .WithQuantity(2.5m)
            .WithCurrentPrice(52000m)
            .Build();

        // Act
        var currentValue = position.CurrentValue;

        // Assert
        currentValue.Should().Be(130000m); // 2.5 * 52000
    }

    [Fact]
    public void IsStopLossHit_ForLongPosition_WhenPriceBelowStopLoss_ReturnsTrue()
    {
        // Arrange
        var position = new PositionBuilder()
            .WithSide(OrderSide.Buy)
            .WithEntryPrice(50000m)
            .WithCurrentPrice(47000m)
            .WithStopLoss(48000m)
            .Build();

        // Act & Assert
        position.IsStopLossHit.Should().BeTrue();
    }

    [Fact]
    public void IsStopLossHit_ForLongPosition_WhenPriceAboveStopLoss_ReturnsFalse()
    {
        // Arrange
        var position = new PositionBuilder()
            .WithSide(OrderSide.Buy)
            .WithEntryPrice(50000m)
            .WithCurrentPrice(49000m)
            .WithStopLoss(48000m)
            .Build();

        // Act & Assert
        position.IsStopLossHit.Should().BeFalse();
    }

    [Fact]
    public void IsStopLossHit_ForShortPosition_WhenPriceAboveStopLoss_ReturnsTrue()
    {
        // Arrange
        var position = new PositionBuilder()
            .WithSide(OrderSide.Sell)
            .WithEntryPrice(50000m)
            .WithCurrentPrice(52000m)
            .WithStopLoss(51000m)
            .Build();

        // Act & Assert
        position.IsStopLossHit.Should().BeTrue();
    }

    [Fact]
    public void IsStopLossHit_WithNoStopLoss_ReturnsFalse()
    {
        // Arrange
        var position = new PositionBuilder()
            .WithStopLoss(null)
            .Build();

        // Act & Assert
        position.IsStopLossHit.Should().BeFalse();
    }

    [Fact]
    public void IsTakeProfitHit_ForLongPosition_WhenPriceAboveTakeProfit_ReturnsTrue()
    {
        // Arrange
        var position = new PositionBuilder()
            .WithSide(OrderSide.Buy)
            .WithEntryPrice(50000m)
            .WithCurrentPrice(56000m)
            .WithTakeProfit(55000m)
            .Build();

        // Act & Assert
        position.IsTakeProfitHit.Should().BeTrue();
    }

    [Fact]
    public void IsTakeProfitHit_ForLongPosition_WhenPriceBelowTakeProfit_ReturnsFalse()
    {
        // Arrange
        var position = new PositionBuilder()
            .WithSide(OrderSide.Buy)
            .WithEntryPrice(50000m)
            .WithCurrentPrice(54000m)
            .WithTakeProfit(55000m)
            .Build();

        // Act & Assert
        position.IsTakeProfitHit.Should().BeFalse();
    }

    [Fact]
    public void IsTakeProfitHit_ForShortPosition_WhenPriceBelowTakeProfit_ReturnsTrue()
    {
        // Arrange
        var position = new PositionBuilder()
            .WithSide(OrderSide.Sell)
            .WithEntryPrice(50000m)
            .WithCurrentPrice(44000m)
            .WithTakeProfit(45000m)
            .Build();

        // Act & Assert
        position.IsTakeProfitHit.Should().BeTrue();
    }

    [Fact]
    public void IsTakeProfitHit_WithNoTakeProfit_ReturnsFalse()
    {
        // Arrange
        var position = new PositionBuilder()
            .WithTakeProfit(null)
            .Build();

        // Act & Assert
        position.IsTakeProfitHit.Should().BeFalse();
    }

    [Fact]
    public void Position_WithStopLossAndTakeProfit_StoresCorrectly()
    {
        // Arrange & Act
        var position = new PositionBuilder()
            .WithEntryPrice(50000m)
            .WithStopLoss(47500m)
            .WithTakeProfit(55000m)
            .Build();

        // Assert
        position.StopLoss.Should().Be(47500m);
        position.TakeProfit.Should().Be(55000m);
    }

    [Fact]
    public void Position_WithStrategyId_StoresCorrectly()
    {
        // Arrange & Act
        var strategyId = "momentum-v1";
        var position = new PositionBuilder()
            .WithStrategyId(strategyId)
            .Build();

        // Assert
        position.StrategyId.Should().Be(strategyId);
    }

    [Fact]
    public void Position_WithOpenOrderId_LinksToOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid().ToString();

        // Act
        var position = new PositionBuilder()
            .WithOpenOrderId(orderId)
            .Build();

        // Assert
        position.OpenOrderId.Should().Be(orderId);
    }

    [Fact]
    public void Position_UpdatedAt_CanBeModified()
    {
        // Arrange
        var openedAt = DateTime.UtcNow.AddHours(-1);
        var updatedAt = DateTime.UtcNow;
        var position = new PositionBuilder()
            .WithOpenedAt(openedAt)
            .WithUpdatedAt(openedAt)
            .Build();

        // Act
        position.UpdatedAt = updatedAt;

        // Assert
        position.UpdatedAt.Should().Be(updatedAt);
        position.UpdatedAt.Should().BeAfter(position.OpenedAt);
    }

    [Fact]
    public void Position_CurrentPrice_CanBeUpdated()
    {
        // Arrange
        var position = new PositionBuilder()
            .WithCurrentPrice(50000m)
            .Build();

        // Act
        position.CurrentPrice = 51000m;

        // Assert
        position.CurrentPrice.Should().Be(51000m);
    }

    [Fact]
    public void Position_LargeQuantity_CalculatesPnLCorrectly()
    {
        // Arrange
        var position = new PositionBuilder()
            .WithSide(OrderSide.Buy)
            .WithQuantity(100m)
            .WithEntryPrice(50000m)
            .WithCurrentPrice(50500m)
            .Build();

        // Act
        var pnl = position.UnrealizedPnL;

        // Assert
        pnl.Should().Be(50000m); // (50500 - 50000) * 100
    }

    [Fact]
    public void Position_SmallPriceMovement_CalculatesPnLCorrectly()
    {
        // Arrange
        var position = new PositionBuilder()
            .WithSide(OrderSide.Buy)
            .WithQuantity(1.0m)
            .WithEntryPrice(50000.00m)
            .WithCurrentPrice(50000.50m)
            .Build();

        // Act
        var pnl = position.UnrealizedPnL;

        // Assert
        pnl.Should().Be(0.50m); // Small movement
    }

    [Theory]
    [InlineData(50000, 55000, 5000)]
    [InlineData(50000, 45000, -5000)]
    [InlineData(50000, 50000, 0)]
    public void Position_ValueChange_TracksCorrectly(decimal entryPrice, decimal currentPrice, decimal expectedPnL)
    {
        // Arrange
        var position = new PositionBuilder()
            .WithSide(OrderSide.Buy)
            .WithQuantity(1.0m)
            .WithEntryPrice(entryPrice)
            .WithCurrentPrice(currentPrice)
            .Build();

        // Act
        var pnl = position.UnrealizedPnL;
        var entryValue = position.EntryValue;
        var currentValue = position.CurrentValue;

        // Assert
        pnl.Should().Be(expectedPnL);
        (currentValue - entryValue).Should().Be(expectedPnL);
    }
}
