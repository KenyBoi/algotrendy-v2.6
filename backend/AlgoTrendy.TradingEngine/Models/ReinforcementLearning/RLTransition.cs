namespace AlgoTrendy.TradingEngine.Models.ReinforcementLearning;

/// <summary>
/// Reinforcement Learning transition
/// Represents a single experience tuple (State, Action, Reward, Next State, Done)
/// Used for training RL agents and storing in replay buffer
/// </summary>
public class RLTransition
{
    /// <summary>
    /// Current state (before action)
    /// </summary>
    public required RLState State { get; init; }

    /// <summary>
    /// Action taken in current state
    /// </summary>
    public required RLAction Action { get; init; }

    /// <summary>
    /// Reward received after taking action
    /// </summary>
    public required RLReward Reward { get; init; }

    /// <summary>
    /// Next state (after action)
    /// </summary>
    public required RLState NextState { get; init; }

    /// <summary>
    /// Is next state terminal (episode ended)
    /// </summary>
    public required bool Done { get; init; }

    /// <summary>
    /// Timestamp when transition occurred
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Priority for prioritized experience replay (optional)
    /// Higher priority = more important transition
    /// </summary>
    public decimal? Priority { get; init; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Creates a transition from components
    /// </summary>
    /// <param name="state">Current state</param>
    /// <param name="action">Action taken</param>
    /// <param name="reward">Reward received</param>
    /// <param name="nextState">Next state</param>
    /// <param name="done">Is episode done</param>
    /// <returns>RLTransition instance</returns>
    public static RLTransition Create(
        RLState state,
        RLAction action,
        RLReward reward,
        RLState nextState,
        bool done)
    {
        return new RLTransition
        {
            State = state,
            Action = action,
            Reward = reward,
            NextState = nextState,
            Done = done,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a transition with priority (for prioritized replay)
    /// </summary>
    /// <param name="state">Current state</param>
    /// <param name="action">Action taken</param>
    /// <param name="reward">Reward received</param>
    /// <param name="nextState">Next state</param>
    /// <param name="done">Is episode done</param>
    /// <param name="priority">Priority value</param>
    /// <returns>RLTransition instance</returns>
    public static RLTransition CreateWithPriority(
        RLState state,
        RLAction action,
        RLReward reward,
        RLState nextState,
        bool done,
        decimal priority)
    {
        return new RLTransition
        {
            State = state,
            Action = action,
            Reward = reward,
            NextState = nextState,
            Done = done,
            Timestamp = DateTime.UtcNow,
            Priority = priority
        };
    }

    /// <summary>
    /// Calculates TD error (Temporal Difference error) for prioritization
    /// TD Error = |reward + gamma * max_Q(next_state) - Q(state, action)|
    /// Simplified version just uses reward magnitude
    /// </summary>
    /// <returns>TD error estimate</returns>
    public decimal CalculateTDError()
    {
        // Simplified: use absolute reward as proxy for TD error
        // Real implementation would require Q-network values
        return Math.Abs(Reward.TotalReward);
    }

    /// <summary>
    /// Gets transition as arrays for neural network training
    /// </summary>
    /// <returns>Tuple of (state_array, action_id, reward, next_state_array, done)</returns>
    public (double[] state, int actionId, double reward, double[] nextState, bool done) ToArrays()
    {
        return (
            State.ToArray(),
            Action.ActionId,
            (double)Reward.NormalizedReward,
            NextState.ToArray(),
            Done
        );
    }

    /// <summary>
    /// Gets extended transition arrays (with context)
    /// </summary>
    /// <returns>Tuple of extended arrays</returns>
    public (double[] state, int actionId, double reward, double[] nextState, bool done) ToExtendedArrays()
    {
        return (
            State.ToExtendedArray(),
            Action.ActionId,
            (double)Reward.NormalizedReward,
            NextState.ToExtendedArray(),
            Done
        );
    }

    /// <summary>
    /// Deep clone
    /// </summary>
    public RLTransition Clone()
    {
        return new RLTransition
        {
            State = State.Clone(),
            Action = Action.Clone(),
            Reward = Reward.Clone(),
            NextState = NextState.Clone(),
            Done = Done,
            Timestamp = Timestamp,
            Priority = Priority,
            Metadata = Metadata != null ? new Dictionary<string, object>(Metadata) : null
        };
    }

    public override string ToString()
    {
        var doneStr = Done ? " [DONE]" : "";
        var priorityStr = Priority.HasValue ? $", Pri={Priority.Value:F2}" : "";

        return $"RLTransition: " +
               $"S[Step={State.StepNumber}] -> " +
               $"A[{Action.ActionId}] -> " +
               $"R[{Reward.TotalReward:F4}] -> " +
               $"S'[Step={NextState.StepNumber}]{doneStr}{priorityStr}";
    }
}
