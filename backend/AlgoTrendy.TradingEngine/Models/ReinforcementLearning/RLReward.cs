namespace AlgoTrendy.TradingEngine.Models.ReinforcementLearning;

/// <summary>
/// Reinforcement Learning reward
/// Calculates reward signal for RL agent based on P&L and risk penalties
/// </summary>
public class RLReward
{
    /// <summary>
    /// Timestamp when reward was calculated
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// P&L component (profit/loss from trades)
    /// </summary>
    public required decimal PnL { get; init; }

    /// <summary>
    /// Inventory penalty (cost of holding risky positions)
    /// </summary>
    public decimal InventoryPenalty { get; init; } = 0.0m;

    /// <summary>
    /// Spread penalty (cost of wide spreads / missed fills)
    /// </summary>
    public decimal SpreadPenalty { get; init; } = 0.0m;

    /// <summary>
    /// Volatility penalty (cost of high volatility exposure)
    /// </summary>
    public decimal VolatilityPenalty { get; init; } = 0.0m;

    /// <summary>
    /// Additional custom penalties
    /// </summary>
    public decimal CustomPenalty { get; init; } = 0.0m;

    /// <summary>
    /// Total reward (PnL - all penalties)
    /// This is the value used to train the RL agent
    /// </summary>
    public decimal TotalReward => PnL - InventoryPenalty - SpreadPenalty - VolatilityPenalty - CustomPenalty;

    /// <summary>
    /// Normalized reward (scaled to [-1, 1] for stable learning)
    /// </summary>
    public decimal NormalizedReward { get; init; }

    /// <summary>
    /// Is this reward from a terminal state
    /// </summary>
    public bool IsTerminal { get; init; } = false;

    /// <summary>
    /// Calculates reward from P&L and inventory state
    /// </summary>
    /// <param name="pnl">Profit/Loss</param>
    /// <param name="inventory">Current inventory (position)</param>
    /// <param name="maxInventory">Maximum allowed inventory</param>
    /// <param name="inventoryPenaltyWeight">Weight for inventory penalty (default: 0.01)</param>
    /// <returns>RLReward instance</returns>
    public static RLReward Calculate(
        decimal pnl,
        decimal inventory,
        decimal maxInventory,
        decimal inventoryPenaltyWeight = 0.01m)
    {
        // Inventory penalty: quadratic penalty for large positions
        // Penalty = weight * (inventory / maxInventory)^2
        var inventoryRatio = inventory / maxInventory;
        var inventoryPenalty = inventoryPenaltyWeight * inventoryRatio * inventoryRatio;

        var totalReward = pnl - inventoryPenalty;

        // Simple normalization using tanh approximation
        var normalizedReward = totalReward / (1.0m + Math.Abs(totalReward));

        return new RLReward
        {
            Timestamp = DateTime.UtcNow,
            PnL = pnl,
            InventoryPenalty = inventoryPenalty,
            NormalizedReward = normalizedReward,
            IsTerminal = false
        };
    }

    /// <summary>
    /// Calculates reward with full penalty breakdown
    /// </summary>
    /// <param name="pnl">Profit/Loss</param>
    /// <param name="inventory">Current inventory</param>
    /// <param name="maxInventory">Maximum allowed inventory</param>
    /// <param name="spread">Current spread</param>
    /// <param name="volatility">Current volatility</param>
    /// <param name="inventoryPenaltyWeight">Inventory penalty weight</param>
    /// <param name="spreadPenaltyWeight">Spread penalty weight</param>
    /// <param name="volatilityPenaltyWeight">Volatility penalty weight</param>
    /// <returns>RLReward instance</returns>
    public static RLReward CalculateDetailed(
        decimal pnl,
        decimal inventory,
        decimal maxInventory,
        decimal spread,
        decimal volatility,
        decimal inventoryPenaltyWeight = 0.01m,
        decimal spreadPenaltyWeight = 0.005m,
        decimal volatilityPenaltyWeight = 0.002m)
    {
        // Inventory penalty: quadratic
        var inventoryRatio = inventory / maxInventory;
        var inventoryPenalty = inventoryPenaltyWeight * inventoryRatio * inventoryRatio;

        // Spread penalty: linear (wider spreads = higher penalty for missed fills)
        var spreadPenalty = spreadPenaltyWeight * spread;

        // Volatility penalty: linear (higher vol = higher risk)
        var volatilityPenalty = volatilityPenaltyWeight * volatility;

        var totalReward = pnl - inventoryPenalty - spreadPenalty - volatilityPenalty;

        // Normalization
        var normalizedReward = totalReward / (1.0m + Math.Abs(totalReward));

        return new RLReward
        {
            Timestamp = DateTime.UtcNow,
            PnL = pnl,
            InventoryPenalty = inventoryPenalty,
            SpreadPenalty = spreadPenalty,
            VolatilityPenalty = volatilityPenalty,
            NormalizedReward = normalizedReward,
            IsTerminal = false
        };
    }

    /// <summary>
    /// Creates terminal reward (end of episode)
    /// </summary>
    /// <param name="finalPnL">Final P&L for the episode</param>
    /// <param name="finalInventory">Final inventory</param>
    /// <param name="maxInventory">Maximum allowed inventory</param>
    /// <returns>Terminal RLReward instance</returns>
    public static RLReward CreateTerminal(
        decimal finalPnL,
        decimal finalInventory,
        decimal maxInventory)
    {
        // Terminal penalty: heavily penalize non-zero final inventory
        var inventoryRatio = finalInventory / maxInventory;
        var terminalInventoryPenalty = 0.1m * inventoryRatio * inventoryRatio;

        var totalReward = finalPnL - terminalInventoryPenalty;
        var normalizedReward = totalReward / (1.0m + Math.Abs(totalReward));

        return new RLReward
        {
            Timestamp = DateTime.UtcNow,
            PnL = finalPnL,
            InventoryPenalty = terminalInventoryPenalty,
            NormalizedReward = normalizedReward,
            IsTerminal = true
        };
    }

    /// <summary>
    /// Deep clone
    /// </summary>
    public RLReward Clone()
    {
        return new RLReward
        {
            Timestamp = Timestamp,
            PnL = PnL,
            InventoryPenalty = InventoryPenalty,
            SpreadPenalty = SpreadPenalty,
            VolatilityPenalty = VolatilityPenalty,
            CustomPenalty = CustomPenalty,
            NormalizedReward = NormalizedReward,
            IsTerminal = IsTerminal
        };
    }

    public override string ToString()
    {
        var terminalStr = IsTerminal ? " [TERMINAL]" : "";
        return $"RLReward: Total={TotalReward:F4} (PnL={PnL:F4}, " +
               $"InvPen={InventoryPenalty:F4}, Normalized={NormalizedReward:F4}){terminalStr}";
    }
}
