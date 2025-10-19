using Newtonsoft.Json;

namespace AlgoTrendy.Core.Models;

/// <summary>
/// Represents a machine learning prediction for potential price reversal
/// </summary>
public class ReversalPrediction
{
    /// <summary>
    /// Indicates whether a reversal is predicted
    /// </summary>
    [JsonProperty("is_reversal")]
    public bool IsReversal { get; set; }

    /// <summary>
    /// Confidence score of the prediction (0.0 to 1.0)
    /// </summary>
    [JsonProperty("confidence")]
    public double Confidence { get; set; }

    /// <summary>
    /// Class probabilities for different outcomes
    /// Typically contains keys like "no_reversal", "reversal_up", "reversal_down"
    /// </summary>
    [JsonProperty("probabilities")]
    public Dictionary<string, double> Probabilities { get; set; } = new();

    /// <summary>
    /// Timestamp when the prediction was made
    /// </summary>
    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Optional error message if prediction failed
    /// </summary>
    [JsonProperty("error")]
    public string? Error { get; set; }
}
