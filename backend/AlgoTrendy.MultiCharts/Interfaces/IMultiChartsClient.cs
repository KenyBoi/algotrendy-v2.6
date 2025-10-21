using AlgoTrendy.MultiCharts.Models;

namespace AlgoTrendy.MultiCharts.Interfaces;

/// <summary>
/// Interface for MultiCharts .NET platform integration
/// </summary>
public interface IMultiChartsClient
{
    /// <summary>
    /// Test connection to MultiCharts platform
    /// </summary>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get MultiCharts platform status
    /// </summary>
    Task<MultiChartsPlatformStatus> GetPlatformStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Run a backtest for a strategy
    /// </summary>
    Task<BacktestResult> RunBacktestAsync(BacktestRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run walk-forward optimization
    /// </summary>
    Task<WalkForwardResult> RunWalkForwardOptimizationAsync(WalkForwardRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run Monte Carlo simulation
    /// </summary>
    Task<MonteCarloResult> RunMonteCarloSimulationAsync(MonteCarloRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deploy a strategy to MultiCharts
    /// </summary>
    Task<StrategyDeploymentResult> DeployStrategyAsync(StrategyDeploymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of available strategies
    /// </summary>
    Task<List<StrategyInfo>> GetStrategiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Run market scanner with custom formula
    /// </summary>
    Task<ScanResult> RunMarketScanAsync(ScanRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of available indicators
    /// </summary>
    Task<List<IndicatorInfo>> GetIndicatorsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get historical data from MultiCharts
    /// </summary>
    Task<List<OHLCVData>> GetHistoricalDataAsync(DataRequest request, CancellationToken cancellationToken = default);
}
