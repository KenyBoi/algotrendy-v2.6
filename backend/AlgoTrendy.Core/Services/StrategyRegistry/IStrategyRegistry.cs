using System.Text.Json;

namespace AlgoTrendy.Core.Services.StrategyRegistry;

/// <summary>
/// Interface for interacting with the Strategy Registry
/// Provides C# integration with the Python-based strategy registry system
/// </summary>
public interface IStrategyRegistry
{
    /// <summary>
    /// Get all available strategies from the registry
    /// </summary>
    Task<List<StrategyRegistryEntry>> GetAllStrategiesAsync();

    /// <summary>
    /// Get a specific strategy by ID
    /// </summary>
    Task<StrategyRegistryEntry?> GetStrategyAsync(string strategyId);

    /// <summary>
    /// Register a new strategy in the registry
    /// </summary>
    Task<bool> RegisterStrategyAsync(StrategyRegistryEntry strategy);

    /// <summary>
    /// Update strategy performance metrics
    /// </summary>
    Task<bool> UpdatePerformanceAsync(string strategyId, PerformanceMetrics metrics);

    /// <summary>
    /// Get strategies filtered by category
    /// </summary>
    Task<List<StrategyRegistryEntry>> GetStrategiesByCategoryAsync(string category);

    /// <summary>
    /// Get strategies filtered by status
    /// </summary>
    Task<List<StrategyRegistryEntry>> GetStrategiesByStatusAsync(string status);

    /// <summary>
    /// Check if a strategy exists in the registry
    /// </summary>
    Task<bool> StrategyExistsAsync(string strategyId);
}

/// <summary>
/// Strategy registry entry model
/// Maps to the JSON metadata structure used by the Python registry
/// </summary>
public class StrategyRegistryEntry
{
    public required StrategyInfo Strategy { get; set; }
}

public class StrategyInfo
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string DisplayName { get; set; }
    public required string Description { get; set; }
    public required string Status { get; set; } // active, inactive, deprecated, testing
    public StrategyClassification? Classification { get; set; }
    public StrategyImplementation? Implementation { get; set; }
    public StrategyPerformance? Performance { get; set; }
    public MemIntegrationInfo? MemIntegration { get; set; }
    public JsonElement? Deployment { get; set; } // Dynamic deployment configuration
    public StrategyRiskProfile? RiskProfile { get; set; }
    public List<string>? Tags { get; set; }
    public StrategyMetadata? Metadata { get; set; }
}

public class StrategyClassification
{
    public string? Category { get; set; } // momentum, mean_reversion, volatility, carry, etc.
    public string? Style { get; set; } // trend_following, contrarian, statistical_arbitrage, etc.
    public List<string>? Indicators { get; set; }
    public string? TimeframePreference { get; set; } // scalping, intraday, swing, position
}

public class StrategyImplementation
{
    public required string Language { get; set; } // python, csharp, freqtrade
    public string? Framework { get; set; } // freqtrade, native, custom
    public string? FilePath { get; set; }
    public string? ClassName { get; set; }
    public Dictionary<string, JsonElement>? DefaultParameters { get; set; }
}

public class StrategyPerformance
{
    public BacktestMetrics? Backtest { get; set; }
    public LiveMetrics? Live { get; set; }
    public string? LastUpdated { get; set; }
}

public class BacktestMetrics
{
    public decimal? SharpeRatio { get; set; }
    public decimal? WinRate { get; set; }
    public decimal? MaxDrawdown { get; set; }
    public decimal? TotalReturn { get; set; }
    public decimal? AnnualizedReturn { get; set; }
    public int? TotalTrades { get; set; }
    public decimal? AvgTradeReturn { get; set; }
    public string? Period { get; set; }
}

public class LiveMetrics
{
    public decimal? RealizedPnL { get; set; }
    public decimal? UnrealizedPnL { get; set; }
    public int? TradesExecuted { get; set; }
    public decimal? CurrentWinRate { get; set; }
    public decimal? CurrentDrawdown { get; set; }
    public string? DeployedSince { get; set; }
}

public class MemIntegrationInfo
{
    public bool Enabled { get; set; }
    public decimal? ConfidenceThreshold { get; set; }
    public List<string>? MarketConditions { get; set; }
    public Dictionary<string, decimal>? WeightFactors { get; set; }
}

public class StrategyRiskProfile
{
    public string? RiskLevel { get; set; } // low, medium, high, very_high
    public decimal? MaxPositionSize { get; set; }
    public decimal? MaxLeverage { get; set; }
    public decimal? StopLossPercent { get; set; }
    public bool? RequiresMargin { get; set; }
}

public class StrategyMetadata
{
    public string? Author { get; set; }
    public string? CreatedAt { get; set; }
    public string? UpdatedAt { get; set; }
    public string? Version { get; set; }
    public string? License { get; set; }
    public List<string>? Dependencies { get; set; }
}

public class PerformanceMetrics
{
    public decimal? SharpeRatio { get; set; }
    public decimal? WinRate { get; set; }
    public decimal? MaxDrawdown { get; set; }
    public decimal? TotalReturn { get; set; }
    public int? TotalTrades { get; set; }
    public decimal? RealizedPnL { get; set; }
    public decimal? UnrealizedPnL { get; set; }
    public DateTime Timestamp { get; set; }
}
