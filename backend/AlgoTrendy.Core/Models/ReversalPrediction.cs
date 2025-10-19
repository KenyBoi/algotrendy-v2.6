namespace AlgoTrendy.Core.Models;

/// <summary>
/// Represents a reversal prediction from the ML model
/// </summary>
public class ReversalPrediction
{
    /// <summary>
    /// Confidence level of the prediction (0-1)
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Probability of bullish reversal
    /// </summary>
    public double BullishProbability { get; set; }

    /// <summary>
    /// Probability of bearish reversal
    /// </summary>
    public double BearishProbability { get; set; }

    /// <summary>
    /// Predicted direction (1 = bullish, -1 = bearish, 0 = neutral)
    /// </summary>
    public int Direction { get; set; }

    /// <summary>
    /// Timestamp of prediction
    /// </summary>
    public DateTime Timestamp { get; set; }
}
