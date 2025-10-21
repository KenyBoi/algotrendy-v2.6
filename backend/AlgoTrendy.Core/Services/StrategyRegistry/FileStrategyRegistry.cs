using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AlgoTrendy.Core.Services.StrategyRegistry;

/// <summary>
/// File-based implementation of IStrategyRegistry
/// Reads from the Python-based Strategy Registry metadata files
/// </summary>
public class FileStrategyRegistry : IStrategyRegistry
{
    private readonly ILogger<FileStrategyRegistry> _logger;
    private readonly string _registryPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public FileStrategyRegistry(
        ILogger<FileStrategyRegistry> logger,
        string? registryPath = null)
    {
        _logger = logger;
        _registryPath = registryPath ?? "/root/AlgoTrendy_v2.6/data/strategy_registry";
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
    }

    public async Task<List<StrategyRegistryEntry>> GetAllStrategiesAsync()
    {
        try
        {
            var metadataPath = Path.Combine(_registryPath, "metadata");

            if (!Directory.Exists(metadataPath))
            {
                _logger.LogWarning("Strategy registry metadata directory not found: {Path}", metadataPath);
                return new List<StrategyRegistryEntry>();
            }

            var strategies = new List<StrategyRegistryEntry>();
            var metadataFiles = Directory.GetFiles(metadataPath, "*.json");

            foreach (var file in metadataFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var entry = JsonSerializer.Deserialize<StrategyRegistryEntry>(json, _jsonOptions);

                    if (entry?.Strategy != null)
                    {
                        strategies.Add(entry);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error reading strategy metadata from {File}", file);
                }
            }

            return strategies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading strategies from registry");
            return new List<StrategyRegistryEntry>();
        }
    }

    public async Task<StrategyRegistryEntry?> GetStrategyAsync(string strategyId)
    {
        try
        {
            var metadataPath = Path.Combine(_registryPath, "metadata", $"{strategyId}.json");

            if (!File.Exists(metadataPath))
            {
                _logger.LogWarning("Strategy metadata not found for ID: {StrategyId}", strategyId);
                return null;
            }

            var json = await File.ReadAllTextAsync(metadataPath);
            var entry = JsonSerializer.Deserialize<StrategyRegistryEntry>(json, _jsonOptions);

            return entry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading strategy {StrategyId}", strategyId);
            return null;
        }
    }

    public async Task<bool> RegisterStrategyAsync(StrategyRegistryEntry strategy)
    {
        try
        {
            var metadataPath = Path.Combine(_registryPath, "metadata");

            if (!Directory.Exists(metadataPath))
            {
                Directory.CreateDirectory(metadataPath);
            }

            var filePath = Path.Combine(metadataPath, $"{strategy.Strategy.Id}.json");
            var json = JsonSerializer.Serialize(strategy, _jsonOptions);

            await File.WriteAllTextAsync(filePath, json);

            _logger.LogInformation("Registered strategy {StrategyId} in registry", strategy.Strategy.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering strategy {StrategyId}", strategy.Strategy.Id);
            return false;
        }
    }

    public async Task<bool> UpdatePerformanceAsync(string strategyId, PerformanceMetrics metrics)
    {
        try
        {
            var strategy = await GetStrategyAsync(strategyId);
            if (strategy == null)
            {
                _logger.LogWarning("Cannot update performance - strategy {StrategyId} not found", strategyId);
                return false;
            }

            // Update performance metrics
            if (strategy.Strategy.Performance == null)
            {
                strategy.Strategy.Performance = new StrategyPerformance();
            }

            if (strategy.Strategy.Performance.Live == null)
            {
                strategy.Strategy.Performance.Live = new LiveMetrics();
            }

            strategy.Strategy.Performance.Live.RealizedPnL = metrics.RealizedPnL;
            strategy.Strategy.Performance.Live.UnrealizedPnL = metrics.UnrealizedPnL;
            strategy.Strategy.Performance.Live.TradesExecuted = metrics.TotalTrades;
            strategy.Strategy.Performance.Live.CurrentWinRate = metrics.WinRate;
            strategy.Strategy.Performance.Live.CurrentDrawdown = metrics.MaxDrawdown;
            strategy.Strategy.Performance.LastUpdated = DateTime.UtcNow.ToString("O");

            // Save updated strategy
            await RegisterStrategyAsync(strategy);

            // Append to performance log
            await AppendPerformanceLogAsync(strategyId, metrics);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating performance for strategy {StrategyId}", strategyId);
            return false;
        }
    }

    public async Task<List<StrategyRegistryEntry>> GetStrategiesByCategoryAsync(string category)
    {
        var allStrategies = await GetAllStrategiesAsync();
        return allStrategies
            .Where(s => s.Strategy.Classification?.Category?.Equals(category, StringComparison.OrdinalIgnoreCase) == true)
            .ToList();
    }

    public async Task<List<StrategyRegistryEntry>> GetStrategiesByStatusAsync(string status)
    {
        var allStrategies = await GetAllStrategiesAsync();
        return allStrategies
            .Where(s => s.Strategy.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<bool> StrategyExistsAsync(string strategyId)
    {
        var metadataPath = Path.Combine(_registryPath, "metadata", $"{strategyId}.json");
        return await Task.FromResult(File.Exists(metadataPath));
    }

    private async Task AppendPerformanceLogAsync(string strategyId, PerformanceMetrics metrics)
    {
        try
        {
            var performancePath = Path.Combine(_registryPath, "performance");

            if (!Directory.Exists(performancePath))
            {
                Directory.CreateDirectory(performancePath);
            }

            var logFile = Path.Combine(performancePath, $"{strategyId}.jsonl");

            var logEntry = new
            {
                timestamp = metrics.Timestamp.ToString("O"),
                sharpe_ratio = metrics.SharpeRatio,
                win_rate = metrics.WinRate,
                max_drawdown = metrics.MaxDrawdown,
                total_return = metrics.TotalReturn,
                total_trades = metrics.TotalTrades,
                realized_pnl = metrics.RealizedPnL,
                unrealized_pnl = metrics.UnrealizedPnL
            };

            var json = JsonSerializer.Serialize(logEntry, _jsonOptions);
            await File.AppendAllTextAsync(logFile, json + Environment.NewLine);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error appending performance log for strategy {StrategyId}", strategyId);
        }
    }
}
