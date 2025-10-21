using AlgoTrendy.TradingEngine.Models.MarketMaking;

namespace AlgoTrendy.TradingEngine.Models.ReinforcementLearning;

/// <summary>
/// Reinforcement Learning state representation
/// Contains all features and context needed by the RL agent to make decisions
/// </summary>
public class RLState
{
    /// <summary>
    /// Symbol being traded
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Timestamp of this state
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Features for RL agent (22 features)
    /// </summary>
    public required ASFeatures Features { get; init; }

    /// <summary>
    /// Current inventory state
    /// </summary>
    public required InventoryState Inventory { get; init; }

    /// <summary>
    /// Current market price (mid price from order book)
    /// </summary>
    public decimal CurrentPrice { get; init; }

    /// <summary>
    /// Time remaining in trading session (0.0 to 1.0)
    /// 1.0 = session start, 0.0 = session end
    /// </summary>
    public decimal TimeRemaining { get; init; } = 1.0m;

    /// <summary>
    /// Episode step number (increments each action)
    /// </summary>
    public int StepNumber { get; init; } = 0;

    /// <summary>
    /// Is this a terminal state (end of episode)
    /// </summary>
    public bool IsTerminal { get; init; } = false;

    /// <summary>
    /// Additional metadata (optional)
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// State dimensionality (should always be 22)
    /// </summary>
    public const int StateDimension = 22;

    /// <summary>
    /// Converts state to array for RL agent input
    /// Uses ASFeatures.ToArray() - returns 22 features
    /// </summary>
    /// <returns>Array of 22 state features</returns>
    public double[] ToArray()
    {
        return Features.ToArray();
    }

    /// <summary>
    /// Gets extended state array including additional context
    /// 22 features + 3 context = 25 dimensions
    /// </summary>
    /// <returns>Extended state array (25 dimensions)</returns>
    public double[] ToExtendedArray()
    {
        var features = Features.ToArray();
        var extended = new double[25];

        // Copy 22 features
        Array.Copy(features, extended, 22);

        // Add context (3 additional dimensions)
        extended[22] = (double)TimeRemaining;
        extended[23] = (double)Inventory.RiskLevel / 100.0; // Normalized 0-1
        extended[24] = (double)StepNumber;

        return extended;
    }

    /// <summary>
    /// Creates RLState from ASFeatures and InventoryState
    /// </summary>
    /// <param name="symbol">Trading symbol</param>
    /// <param name="features">AS features</param>
    /// <param name="inventory">Inventory state</param>
    /// <param name="currentPrice">Current market price</param>
    /// <param name="timeRemaining">Time remaining in session</param>
    /// <param name="stepNumber">Episode step number</param>
    /// <returns>New RLState instance</returns>
    public static RLState Create(
        string symbol,
        ASFeatures features,
        InventoryState inventory,
        decimal currentPrice,
        decimal timeRemaining = 1.0m,
        int stepNumber = 0)
    {
        return new RLState
        {
            Symbol = symbol,
            Timestamp = DateTime.UtcNow,
            Features = features,
            Inventory = inventory,
            CurrentPrice = currentPrice,
            TimeRemaining = timeRemaining,
            StepNumber = stepNumber,
            IsTerminal = false
        };
    }

    /// <summary>
    /// Creates a terminal state (end of episode)
    /// </summary>
    /// <param name="currentState">Current state to mark as terminal</param>
    /// <returns>Terminal state</returns>
    public static RLState CreateTerminal(RLState currentState)
    {
        return new RLState
        {
            Symbol = currentState.Symbol,
            Timestamp = DateTime.UtcNow,
            Features = currentState.Features,
            Inventory = currentState.Inventory,
            CurrentPrice = currentState.CurrentPrice,
            TimeRemaining = 0.0m,
            StepNumber = currentState.StepNumber,
            IsTerminal = true,
            Metadata = currentState.Metadata
        };
    }

    /// <summary>
    /// Deep clone
    /// </summary>
    public RLState Clone()
    {
        return new RLState
        {
            Symbol = Symbol,
            Timestamp = Timestamp,
            Features = Features,
            Inventory = Inventory.Clone(),
            CurrentPrice = CurrentPrice,
            TimeRemaining = TimeRemaining,
            StepNumber = StepNumber,
            IsTerminal = IsTerminal,
            Metadata = Metadata != null ? new Dictionary<string, object>(Metadata) : null
        };
    }

    public override string ToString()
    {
        var terminalStr = IsTerminal ? " [TERMINAL]" : "";
        return $"RLState[{Symbol}] Step={StepNumber}, " +
               $"Price={CurrentPrice:F2}, Inv={Inventory.InventoryPercent:P0}, " +
               $"Risk={Inventory.RiskCategory}, T={TimeRemaining:P0}{terminalStr}";
    }
}
