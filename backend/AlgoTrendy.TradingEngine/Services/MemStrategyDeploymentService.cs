using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace AlgoTrendy.TradingEngine.Services;

/// <summary>
/// MEM Strategy Deployment Service
/// Orchestrates deployment and management of trading strategies (Freqtrade, native, etc.)
/// Integrates with Strategy Registry for dynamic strategy selection
/// </summary>
public class MemStrategyDeploymentService
{
    private readonly ILogger<MemStrategyDeploymentService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Dictionary<string, DeployedStrategy> _deployedStrategies;
    private readonly string _strategyRegistryPath;

    public MemStrategyDeploymentService(
        ILogger<MemStrategyDeploymentService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _deployedStrategies = new Dictionary<string, DeployedStrategy>();
        _strategyRegistryPath = configuration["StrategyRegistry:Path"] ?? "/root/AlgoTrendy_v2.6/data/strategy_registry";
    }

    /// <summary>
    /// Get all available strategies from the registry
    /// </summary>
    public async Task<List<RegistryStrategy>> GetAvailableStrategiesAsync()
    {
        try
        {
            var metadataPath = Path.Combine(_strategyRegistryPath, "metadata");

            if (!Directory.Exists(metadataPath))
            {
                _logger.LogWarning($"Strategy registry metadata directory not found: {metadataPath}");
                return new List<RegistryStrategy>();
            }

            var strategies = new List<RegistryStrategy>();
            var metadataFiles = Directory.GetFiles(metadataPath, "*.json");

            foreach (var file in metadataFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var metadata = JsonSerializer.Deserialize<StrategyMetadata>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (metadata?.Strategy != null)
                    {
                        strategies.Add(new RegistryStrategy
                        {
                            Id = metadata.Strategy.Id,
                            Name = metadata.Strategy.Name,
                            DisplayName = metadata.Strategy.DisplayName,
                            Description = metadata.Strategy.Description,
                            Category = metadata.Strategy.Classification?.Category ?? "unknown",
                            Status = metadata.Strategy.Status,
                            Language = metadata.Strategy.Implementation?.Language ?? "unknown",
                            MemEnabled = metadata.Strategy.MemIntegration?.Enabled ?? false,
                            SharpeRatio = metadata.Strategy.Performance?.Backtest?.SharpeRatio,
                            WinRate = metadata.Strategy.Performance?.Backtest?.WinRate,
                            IsDeployed = _deployedStrategies.ContainsKey(metadata.Strategy.Id)
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error reading strategy metadata from {file}");
                }
            }

            return strategies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading strategies from registry");
            return new List<RegistryStrategy>();
        }
    }

    /// <summary>
    /// Get a specific strategy by ID
    /// </summary>
    public async Task<RegistryStrategy?> GetStrategyAsync(string strategyId)
    {
        var strategies = await GetAvailableStrategiesAsync();
        return strategies.FirstOrDefault(s => s.Id == strategyId);
    }

    /// <summary>
    /// Deploy a strategy (Freqtrade or native)
    /// </summary>
    public async Task<DeploymentResult> DeployStrategyAsync(string strategyId, DeploymentOptions? options = null)
    {
        try
        {
            var strategy = await GetStrategyAsync(strategyId);
            if (strategy == null)
            {
                return DeploymentResult.Failed($"Strategy {strategyId} not found in registry");
            }

            if (_deployedStrategies.ContainsKey(strategyId))
            {
                return DeploymentResult.Failed($"Strategy {strategyId} is already deployed");
            }

            // Load full metadata for deployment details
            var metadataPath = Path.Combine(_strategyRegistryPath, "metadata", $"{strategyId}.json");
            var json = await File.ReadAllTextAsync(metadataPath);
            var metadata = JsonSerializer.Deserialize<StrategyMetadata>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (metadata?.Strategy == null)
            {
                return DeploymentResult.Failed($"Invalid metadata for strategy {strategyId}");
            }

            // Determine deployment type
            var deployment = metadata.Strategy.Deployment;
            DeployedStrategy deployedStrategy;

            if (deployment?.Type == "freqtrade")
            {
                deployedStrategy = await DeployFreqtradeStrategyAsync(strategyId, metadata, deployment, options);
            }
            else if (deployment?.Type == "native")
            {
                deployedStrategy = await DeployNativeStrategyAsync(strategyId, metadata, deployment, options);
            }
            else
            {
                return DeploymentResult.Failed($"Unknown deployment type: {deployment?.Type}");
            }

            _deployedStrategies[strategyId] = deployedStrategy;
            _logger.LogInformation($"Successfully deployed strategy {strategy.DisplayName} ({strategyId})");

            return DeploymentResult.CreateSuccess(deployedStrategy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deploying strategy {strategyId}");
            return DeploymentResult.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Stop a deployed strategy
    /// </summary>
    public async Task<DeploymentResult> StopStrategyAsync(string strategyId)
    {
        try
        {
            if (!_deployedStrategies.TryGetValue(strategyId, out var deployed))
            {
                return DeploymentResult.Failed($"Strategy {strategyId} is not deployed");
            }

            if (deployed.DeploymentType == "freqtrade" && deployed.ProcessId.HasValue)
            {
                try
                {
                    var process = Process.GetProcessById(deployed.ProcessId.Value);
                    process.Kill(true);
                    _logger.LogInformation($"Killed Freqtrade process {deployed.ProcessId} for strategy {strategyId}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Could not kill process {deployed.ProcessId} - may already be stopped");
                }
            }

            _deployedStrategies.Remove(strategyId);
            return DeploymentResult.CreateSuccess(deployed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error stopping strategy {strategyId}");
            return DeploymentResult.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Get all currently deployed strategies
    /// </summary>
    public List<DeployedStrategy> GetDeployedStrategies()
    {
        return _deployedStrategies.Values.ToList();
    }

    /// <summary>
    /// Get MEM's recommended strategies based on current market conditions
    /// </summary>
    public async Task<List<StrategyRecommendation>> GetMemRecommendationsAsync(MarketConditions conditions)
    {
        var strategies = await GetAvailableStrategiesAsync();
        var recommendations = new List<StrategyRecommendation>();

        foreach (var strategy in strategies.Where(s => s.Status == "active" && s.MemEnabled))
        {
            var score = CalculateStrategyScore(strategy, conditions);

            if (score > 0.5m) // Only recommend strategies with >50% confidence
            {
                recommendations.Add(new StrategyRecommendation
                {
                    StrategyId = strategy.Id,
                    StrategyName = strategy.DisplayName,
                    ConfidenceScore = score,
                    Reason = GenerateRecommendationReason(strategy, conditions),
                    EstimatedSharpe = strategy.SharpeRatio,
                    EstimatedWinRate = strategy.WinRate
                });
            }
        }

        return recommendations.OrderByDescending(r => r.ConfidenceScore).ToList();
    }

    #region Private Deployment Methods

    private async Task<DeployedStrategy> DeployFreqtradeStrategyAsync(
        string strategyId,
        StrategyMetadata metadata,
        dynamic deployment,
        DeploymentOptions? options)
    {
        var configFile = deployment.ConfigFile?.ToString() ?? "";
        var port = deployment.Port ?? 8080;
        var dryRun = options?.DryRun ?? deployment.DryRun ?? true;

        // Construct Freqtrade command
        var command = "freqtrade";
        var arguments = $"trade -c {configFile} --port {port}";

        if (dryRun)
        {
            arguments += " --dry-run";
        }

        // Start Freqtrade process
        var processInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        var process = Process.Start(processInfo);

        if (process == null)
        {
            throw new InvalidOperationException("Failed to start Freqtrade process");
        }

        // Wait a bit to ensure it started successfully
        await Task.Delay(2000);

        if (process.HasExited)
        {
            throw new InvalidOperationException($"Freqtrade process exited immediately with code {process.ExitCode}");
        }

        return new DeployedStrategy
        {
            StrategyId = strategyId,
            StrategyName = metadata.Strategy.DisplayName,
            DeploymentType = "freqtrade",
            ProcessId = process.Id,
            Port = port,
            StartedAt = DateTime.UtcNow,
            Status = "running",
            DryRun = dryRun,
            ConfigFile = configFile
        };
    }

    private async Task<DeployedStrategy> DeployNativeStrategyAsync(
        string strategyId,
        StrategyMetadata metadata,
        dynamic deployment,
        DeploymentOptions? options)
    {
        // Native C# strategy deployment
        // This would integrate with the existing AlgoTrendy strategy system
        _logger.LogInformation($"Deploying native strategy {strategyId}");

        return new DeployedStrategy
        {
            StrategyId = strategyId,
            StrategyName = metadata.Strategy.DisplayName,
            DeploymentType = "native",
            StartedAt = DateTime.UtcNow,
            Status = "running",
            DryRun = options?.DryRun ?? true
        };
    }

    private decimal CalculateStrategyScore(RegistryStrategy strategy, MarketConditions conditions)
    {
        // MEM-enhanced strategy scoring algorithm
        decimal score = 0.5m; // Base score

        // Category matching
        if (conditions.Trend == "bullish" && strategy.Category == "momentum")
            score += 0.2m;
        else if (conditions.Trend == "ranging" && strategy.Category == "mean_reversion")
            score += 0.2m;
        else if (conditions.Trend == "bearish" && strategy.Category == "carry")
            score += 0.1m;

        // Performance metrics
        if (strategy.SharpeRatio.HasValue && strategy.SharpeRatio > 1.0m)
            score += 0.15m;

        if (strategy.WinRate.HasValue && strategy.WinRate > 0.55m)
            score += 0.1m;

        // Volatility matching
        if (conditions.Volatility == "high" && strategy.Category == "volatility")
            score += 0.15m;

        return Math.Min(score, 1.0m);
    }

    private string GenerateRecommendationReason(RegistryStrategy strategy, MarketConditions conditions)
    {
        var reasons = new List<string>();

        if (conditions.Trend == "bullish" && strategy.Category == "momentum")
            reasons.Add("momentum strategy suits bullish trend");

        if (conditions.Trend == "ranging" && strategy.Category == "mean_reversion")
            reasons.Add("mean reversion works well in ranging markets");

        if (strategy.SharpeRatio > 1.0m)
            reasons.Add($"strong Sharpe ratio ({strategy.SharpeRatio:F2})");

        if (strategy.WinRate > 0.55m)
            reasons.Add($"high win rate ({strategy.WinRate:P0})");

        return reasons.Any() ? string.Join(", ", reasons) : "general market conditions favorable";
    }

    #endregion

    #region Data Models

    public class RegistryStrategy
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public string Status { get; set; } = "";
        public string Language { get; set; } = "";
        public bool MemEnabled { get; set; }
        public decimal? SharpeRatio { get; set; }
        public decimal? WinRate { get; set; }
        public bool IsDeployed { get; set; }
    }

    public class DeployedStrategy
    {
        public string StrategyId { get; set; } = "";
        public string StrategyName { get; set; } = "";
        public string DeploymentType { get; set; } = "";
        public int? ProcessId { get; set; }
        public int? Port { get; set; }
        public DateTime StartedAt { get; set; }
        public string Status { get; set; } = "";
        public bool DryRun { get; set; }
        public string? ConfigFile { get; set; }
    }

    public class DeploymentOptions
    {
        public bool DryRun { get; set; } = true;
        public Dictionary<string, object>? Parameters { get; set; }
    }

    public class DeploymentResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public DeployedStrategy? Strategy { get; set; }

        public static DeploymentResult CreateSuccess(DeployedStrategy strategy) =>
            new() { Success = true, Strategy = strategy };

        public static DeploymentResult Failed(string message) =>
            new() { Success = false, Message = message };
    }

    public class StrategyRecommendation
    {
        public string StrategyId { get; set; } = "";
        public string StrategyName { get; set; } = "";
        public decimal ConfidenceScore { get; set; }
        public string Reason { get; set; } = "";
        public decimal? EstimatedSharpe { get; set; }
        public decimal? EstimatedWinRate { get; set; }
    }

    public class MarketConditions
    {
        public string Trend { get; set; } = "neutral"; // bullish, bearish, ranging
        public string Volatility { get; set; } = "medium"; // low, medium, high
        public decimal VIX { get; set; }
        public Dictionary<string, decimal> Indicators { get; set; } = new();
    }

    // Strategy metadata structure (matches Python registry)
    private class StrategyMetadata
    {
        public Strategy? Strategy { get; set; }
    }

    private class Strategy
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Description { get; set; } = "";
        public string Status { get; set; } = "";
        public Classification? Classification { get; set; }
        public Implementation? Implementation { get; set; }
        public Performance? Performance { get; set; }
        public MemIntegration? MemIntegration { get; set; }
        public dynamic? Deployment { get; set; }
    }

    private class Classification
    {
        public string Category { get; set; } = "";
    }

    private class Implementation
    {
        public string Language { get; set; } = "";
    }

    private class Performance
    {
        public BacktestPerformance? Backtest { get; set; }
    }

    private class BacktestPerformance
    {
        public decimal? SharpeRatio { get; set; }
        public decimal? WinRate { get; set; }
    }

    private class MemIntegration
    {
        public bool Enabled { get; set; }
    }

    #endregion
}
