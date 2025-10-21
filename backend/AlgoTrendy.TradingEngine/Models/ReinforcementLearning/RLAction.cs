namespace AlgoTrendy.TradingEngine.Models.ReinforcementLearning;

/// <summary>
/// Reinforcement Learning action
/// Represents the action taken by the RL agent to adjust market making parameters
/// </summary>
public class RLAction
{
    /// <summary>
    /// Action ID (0 to ActionSpaceSize - 1)
    /// Used for discrete action spaces
    /// </summary>
    public required int ActionId { get; init; }

    /// <summary>
    /// Gamma adjustment (risk aversion multiplier)
    /// Range: 0.5 to 2.0 (multiply base gamma by this value)
    /// 1.0 = no change, &lt;1.0 = more aggressive, &gt;1.0 = more conservative
    /// </summary>
    public decimal GammaMultiplier { get; init; } = 1.0m;

    /// <summary>
    /// Spread skew (asymmetric bid/ask adjustment)
    /// Range: -1.0 to +1.0
    /// Negative = widen bid spread (discourage buying)
    /// Positive = widen ask spread (discourage selling)
    /// 0 = symmetric spreads
    /// </summary>
    public decimal SpreadSkew { get; init; } = 0.0m;

    /// <summary>
    /// Inventory target adjustment
    /// Range: -1.0 to +1.0 (as percentage of max inventory)
    /// Allows agent to temporarily shift target inventory
    /// </summary>
    public decimal InventoryTargetAdjustment { get; init; } = 0.0m;

    /// <summary>
    /// Size multiplier for quote quantities
    /// Range: 0.1 to 2.0
    /// 1.0 = normal size, &lt;1.0 = smaller quotes, &gt;1.0 = larger quotes
    /// </summary>
    public decimal SizeMultiplier { get; init; } = 1.0m;

    /// <summary>
    /// Action taken timestamp
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Is this a "do nothing" action
    /// </summary>
    public bool IsNoOp => ActionId == 0 &&
                          GammaMultiplier == 1.0m &&
                          SpreadSkew == 0.0m &&
                          InventoryTargetAdjustment == 0.0m &&
                          SizeMultiplier == 1.0m;

    /// <summary>
    /// Action space size for discrete actions
    /// Common configurations: 5, 9, 13, 21
    /// </summary>
    public const int DefaultActionSpaceSize = 9;

    /// <summary>
    /// Creates a discrete action from action ID
    /// Maps discrete actions to continuous parameter adjustments
    /// </summary>
    /// <param name="actionId">Discrete action ID (0 to 8 for default 9-action space)</param>
    /// <returns>RLAction instance</returns>
    public static RLAction FromActionId(int actionId)
    {
        // 9-action space:
        // 0: No-op (do nothing)
        // 1: Increase gamma (more conservative)
        // 2: Decrease gamma (more aggressive)
        // 3: Skew bid spread wider (reduce buying)
        // 4: Skew ask spread wider (reduce selling)
        // 5: Shift target inventory long (encourage holding longs)
        // 6: Shift target inventory short (encourage holding shorts)
        // 7: Increase quote size
        // 8: Decrease quote size

        return actionId switch
        {
            0 => new RLAction { ActionId = 0 }, // No-op
            1 => new RLAction { ActionId = 1, GammaMultiplier = 1.5m }, // More conservative
            2 => new RLAction { ActionId = 2, GammaMultiplier = 0.7m }, // More aggressive
            3 => new RLAction { ActionId = 3, SpreadSkew = -0.3m }, // Widen bid spread
            4 => new RLAction { ActionId = 4, SpreadSkew = 0.3m }, // Widen ask spread
            5 => new RLAction { ActionId = 5, InventoryTargetAdjustment = 0.2m }, // Target long
            6 => new RLAction { ActionId = 6, InventoryTargetAdjustment = -0.2m }, // Target short
            7 => new RLAction { ActionId = 7, SizeMultiplier = 1.5m }, // Increase size
            8 => new RLAction { ActionId = 8, SizeMultiplier = 0.7m }, // Decrease size
            _ => throw new ArgumentException($"Invalid action ID: {actionId}. Must be 0-8.")
        };
    }

    /// <summary>
    /// Creates a continuous action from parameter values
    /// </summary>
    /// <param name="gammaMultiplier">Gamma multiplier</param>
    /// <param name="spreadSkew">Spread skew</param>
    /// <param name="inventoryTargetAdjustment">Inventory target adjustment</param>
    /// <param name="sizeMultiplier">Size multiplier</param>
    /// <returns>RLAction instance</returns>
    public static RLAction FromContinuous(
        decimal gammaMultiplier = 1.0m,
        decimal spreadSkew = 0.0m,
        decimal inventoryTargetAdjustment = 0.0m,
        decimal sizeMultiplier = 1.0m)
    {
        // Clamp values to valid ranges
        gammaMultiplier = Math.Clamp(gammaMultiplier, 0.1m, 5.0m);
        spreadSkew = Math.Clamp(spreadSkew, -1.0m, 1.0m);
        inventoryTargetAdjustment = Math.Clamp(inventoryTargetAdjustment, -1.0m, 1.0m);
        sizeMultiplier = Math.Clamp(sizeMultiplier, 0.1m, 3.0m);

        return new RLAction
        {
            ActionId = -1, // Continuous action (no discrete ID)
            GammaMultiplier = gammaMultiplier,
            SpreadSkew = spreadSkew,
            InventoryTargetAdjustment = inventoryTargetAdjustment,
            SizeMultiplier = sizeMultiplier
        };
    }

    /// <summary>
    /// Gets action description
    /// </summary>
    public string GetDescription()
    {
        if (IsNoOp)
            return "No-op (maintain current parameters)";

        var parts = new List<string>();

        if (GammaMultiplier != 1.0m)
            parts.Add($"Gamma×{GammaMultiplier:F2}");

        if (SpreadSkew != 0.0m)
            parts.Add($"Skew={SpreadSkew:+0.0;-0.0}");

        if (InventoryTargetAdjustment != 0.0m)
            parts.Add($"InvTarget={InventoryTargetAdjustment:+0.0%;-0.0%}");

        if (SizeMultiplier != 1.0m)
            parts.Add($"Size×{SizeMultiplier:F2}");

        return string.Join(", ", parts);
    }

    /// <summary>
    /// Deep clone
    /// </summary>
    public RLAction Clone()
    {
        return new RLAction
        {
            ActionId = ActionId,
            GammaMultiplier = GammaMultiplier,
            SpreadSkew = SpreadSkew,
            InventoryTargetAdjustment = InventoryTargetAdjustment,
            SizeMultiplier = SizeMultiplier,
            Timestamp = Timestamp
        };
    }

    public override string ToString()
    {
        if (ActionId >= 0)
            return $"RLAction[Discrete] ID={ActionId}: {GetDescription()}";
        else
            return $"RLAction[Continuous]: {GetDescription()}";
    }
}
