using AlgoTrendy.Core.Models;

namespace AlgoTrendy.Core.Interfaces;

/// <summary>
/// Interface for ML prediction service
/// Provides machine learning predictions for trading signals and reversals
/// </summary>
public interface IMLPredictionService
{
    /// <summary>
    /// Predicts potential reversal based on feature array
    /// </summary>
    /// <param name="features">Array of technical indicators and features</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Reversal prediction with confidence and probabilities</returns>
    Task<ReversalPrediction?> PredictReversalAsync(
        double[] features,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the ML prediction service is healthy and available
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if service is healthy, false otherwise</returns>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
