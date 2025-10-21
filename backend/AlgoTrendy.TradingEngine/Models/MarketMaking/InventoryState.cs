namespace AlgoTrendy.TradingEngine.Models.MarketMaking;

/// <summary>
/// Current inventory state for market making strategy
/// Tracks position, risk level, and inventory metrics
/// </summary>
public class InventoryState
{
    /// <summary>
    /// Symbol being tracked
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Timestamp of this state
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Current inventory (position) in base currency
    /// Positive = long, Negative = short, 0 = neutral
    /// </summary>
    public required decimal CurrentInventory { get; init; }

    /// <summary>
    /// Target inventory (usually 0 for market neutral)
    /// </summary>
    public decimal TargetInventory { get; init; } = 0.0m;

    /// <summary>
    /// Maximum allowed inventory (position limit)
    /// </summary>
    public required decimal MaxInventory { get; init; }

    /// <summary>
    /// Current market price (for P&L calculation)
    /// </summary>
    public decimal? CurrentPrice { get; init; }

    /// <summary>
    /// Average entry price for current position
    /// </summary>
    public decimal? AverageEntryPrice { get; init; }

    /// <summary>
    /// Realized P&L (from closed positions)
    /// </summary>
    public decimal RealizedPnL { get; init; } = 0.0m;

    // Computed properties

    /// <summary>
    /// Inventory as percentage of max position (-100% to +100%)
    /// </summary>
    public decimal InventoryPercent => CurrentInventory / MaxInventory;

    /// <summary>
    /// Distance from target inventory
    /// </summary>
    public decimal DistanceFromTarget => CurrentInventory - TargetInventory;

    /// <summary>
    /// Absolute inventory level (always positive)
    /// </summary>
    public decimal AbsoluteInventory => Math.Abs(CurrentInventory);

    /// <summary>
    /// Inventory direction: 1 (long), -1 (short), 0 (neutral)
    /// </summary>
    public int Direction => CurrentInventory > 0 ? 1 : (CurrentInventory < 0 ? -1 : 0);

    /// <summary>
    /// Is inventory at or near limit (>90% of max)
    /// </summary>
    public bool IsNearLimit => Math.Abs(InventoryPercent) >= 0.9m;

    /// <summary>
    /// Is inventory at or exceeding limit
    /// </summary>
    public bool IsAtLimit => Math.Abs(CurrentInventory) >= MaxInventory;

    /// <summary>
    /// Is position market neutral (within 5% of target)
    /// </summary>
    public bool IsNeutral => Math.Abs(InventoryPercent) <= 0.05m;

    /// <summary>
    /// Available capacity to increase position (in base currency)
    /// </summary>
    public decimal AvailableCapacity => MaxInventory - AbsoluteInventory;

    /// <summary>
    /// Unrealized P&L (mark-to-market)
    /// </summary>
    public decimal? UnrealizedPnL
    {
        get
        {
            if (CurrentPrice == null || AverageEntryPrice == null)
                return null;

            return CurrentInventory * (CurrentPrice.Value - AverageEntryPrice.Value);
        }
    }

    /// <summary>
    /// Total P&L (realized + unrealized)
    /// </summary>
    public decimal? TotalPnL => UnrealizedPnL != null ? RealizedPnL + UnrealizedPnL : null;

    /// <summary>
    /// Risk level (0-100): Higher = more risky
    /// Based on inventory utilization and distance from target
    /// </summary>
    public int RiskLevel
    {
        get
        {
            var utilization = Math.Abs(InventoryPercent);
            var distancePct = Math.Abs(DistanceFromTarget / MaxInventory);

            // Risk increases exponentially near limits
            var risk = (utilization * 0.7m + distancePct * 0.3m) * 100.0m;

            return (int)Math.Min(100, Math.Max(0, risk));
        }
    }

    /// <summary>
    /// Risk category: Low, Medium, High, Critical
    /// </summary>
    public string RiskCategory
    {
        get
        {
            return RiskLevel switch
            {
                < 30 => "Low",
                < 60 => "Medium",
                < 85 => "High",
                _ => "Critical"
            };
        }
    }

    /// <summary>
    /// Can increase position (buy more)
    /// </summary>
    /// <param name="quantity">Quantity to buy</param>
    /// <returns>True if position can be increased</returns>
    public bool CanIncreaseLong(decimal quantity)
    {
        if (quantity <= 0) return false;

        var newInventory = CurrentInventory + quantity;
        return newInventory <= MaxInventory;
    }

    /// <summary>
    /// Can decrease position (sell/short)
    /// </summary>
    /// <param name="quantity">Quantity to sell</param>
    /// <returns>True if position can be decreased</returns>
    public bool CanIncreaseShort(decimal quantity)
    {
        if (quantity <= 0) return false;

        var newInventory = CurrentInventory - quantity;
        return Math.Abs(newInventory) <= MaxInventory;
    }

    /// <summary>
    /// Should reduce position (risk management)
    /// True if inventory is >70% of max or >50% away from target
    /// </summary>
    public bool ShouldReducePosition
    {
        get
        {
            var utilizationPct = Math.Abs(InventoryPercent);
            var targetDistancePct = Math.Abs(DistanceFromTarget / MaxInventory);

            return utilizationPct > 0.7m || targetDistancePct > 0.5m;
        }
    }

    /// <summary>
    /// Deep clone
    /// </summary>
    public InventoryState Clone()
    {
        return new InventoryState
        {
            Symbol = Symbol,
            Timestamp = Timestamp,
            CurrentInventory = CurrentInventory,
            TargetInventory = TargetInventory,
            MaxInventory = MaxInventory,
            CurrentPrice = CurrentPrice,
            AverageEntryPrice = AverageEntryPrice,
            RealizedPnL = RealizedPnL
        };
    }

    public override string ToString()
    {
        var pnlStr = TotalPnL.HasValue ? $", PnL={TotalPnL.Value:F2}" : "";

        return $"InventoryState[{Symbol}]: " +
               $"Pos={CurrentInventory:F4} ({InventoryPercent:P0}), " +
               $"Risk={RiskCategory} ({RiskLevel}){pnlStr}";
    }
}
