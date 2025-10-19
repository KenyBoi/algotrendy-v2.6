using AlgoTrendy.Backtesting.Models;

namespace AlgoTrendy.Backtesting.Engines;

/// <summary>
/// Interface for backtesting engine implementations
/// </summary>
public interface IBacktestEngine
{
    /// <summary>
    /// Validate the backtest configuration
    /// </summary>
    /// <param name="config">Configuration to validate</param>
    /// <returns>Validation result with error message if invalid</returns>
    (bool IsValid, string? ErrorMessage) ValidateConfig(BacktestConfig config);

    /// <summary>
    /// Run the backtest asynchronously
    /// </summary>
    /// <param name="config">Backtest configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Backtest results</returns>
    Task<BacktestResults> RunAsync(BacktestConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the engine name
    /// </summary>
    string EngineName { get; }

    /// <summary>
    /// Get the engine description
    /// </summary>
    string EngineDescription { get; }
}
