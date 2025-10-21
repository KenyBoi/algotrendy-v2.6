using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AlgoTrendy.API.Controllers;

/// <summary>
/// System health metrics and scaling decision support
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SystemMetricsController : ControllerBase
{
    private readonly ILogger<SystemMetricsController> _logger;
    private const double CPU_WARNING_THRESHOLD = 70.0;
    private const double CPU_CRITICAL_THRESHOLD = 85.0;
    private const double MEMORY_WARNING_THRESHOLD = 80.0;
    private const double MEMORY_CRITICAL_THRESHOLD = 90.0;
    private const double LATENCY_WARNING_THRESHOLD = 200.0;
    private const double LATENCY_CRITICAL_THRESHOLD = 500.0;
    private const int REQUEST_RATE_MICROSERVICES_THRESHOLD = 10000; // req/min

    public SystemMetricsController(ILogger<SystemMetricsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get current system metrics
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(SystemMetricsResponse), 200)]
    public IActionResult GetSystemMetrics()
    {
        var cpuUsage = GetCpuUsage();
        var memoryUsage = GetMemoryUsage();
        var (avgLatency, p95Latency) = GetLatencyMetrics();
        var requestsPerMinute = GetRequestsPerMinute();
        var errorRate = GetErrorRate();

        var response = new SystemMetricsResponse
        {
            Timestamp = DateTime.UtcNow,
            Cpu = new MetricInfo
            {
                Current = Math.Round(cpuUsage, 2),
                WarningThreshold = CPU_WARNING_THRESHOLD,
                CriticalThreshold = CPU_CRITICAL_THRESHOLD,
                Status = GetStatus(cpuUsage, CPU_WARNING_THRESHOLD, CPU_CRITICAL_THRESHOLD),
                Unit = "%"
            },
            Memory = new MetricInfo
            {
                Current = Math.Round(memoryUsage, 2),
                WarningThreshold = MEMORY_WARNING_THRESHOLD,
                CriticalThreshold = MEMORY_CRITICAL_THRESHOLD,
                Status = GetStatus(memoryUsage, MEMORY_WARNING_THRESHOLD, MEMORY_CRITICAL_THRESHOLD),
                Unit = "%"
            },
            Latency = new LatencyMetricInfo
            {
                Average = Math.Round(avgLatency, 2),
                P95 = Math.Round(p95Latency, 2),
                WarningThreshold = LATENCY_WARNING_THRESHOLD,
                CriticalThreshold = LATENCY_CRITICAL_THRESHOLD,
                Status = GetStatus(p95Latency, LATENCY_WARNING_THRESHOLD, LATENCY_CRITICAL_THRESHOLD),
                Unit = "ms"
            },
            RequestsPerMinute = Math.Round(requestsPerMinute, 0),
            ErrorRate = Math.Round(errorRate, 2),
            Uptime = GetUptime()
        };

        return Ok(response);
    }

    /// <summary>
    /// Get overall health score (0-100)
    /// </summary>
    [HttpGet("health-score")]
    [ProducesResponseType(typeof(HealthScoreResponse), 200)]
    public IActionResult GetHealthScore()
    {
        var cpuUsage = GetCpuUsage();
        var memoryUsage = GetMemoryUsage();
        var (_, p95Latency) = GetLatencyMetrics();
        var errorRate = GetErrorRate();

        // Calculate health score (0-100, higher is better)
        var cpuScore = CalculateComponentScore(cpuUsage, CPU_CRITICAL_THRESHOLD);
        var memoryScore = CalculateComponentScore(memoryUsage, MEMORY_CRITICAL_THRESHOLD);
        var latencyScore = CalculateComponentScore(p95Latency, LATENCY_CRITICAL_THRESHOLD);
        var errorScore = 100 - (errorRate * 10); // 10% error rate = 0 score

        var overallScore = (cpuScore * 0.3 + memoryScore * 0.3 + latencyScore * 0.25 + errorScore * 0.15);

        var response = new HealthScoreResponse
        {
            OverallScore = Math.Round(overallScore, 0),
            ComponentScores = new Dictionary<string, double>
            {
                { "CPU", Math.Round(cpuScore, 0) },
                { "Memory", Math.Round(memoryScore, 0) },
                { "Latency", Math.Round(latencyScore, 0) },
                { "ErrorRate", Math.Round(errorScore, 0) }
            },
            Status = overallScore >= 80 ? "Excellent" :
                     overallScore >= 60 ? "Good" :
                     overallScore >= 40 ? "Fair" : "Poor",
            Recommendations = GetHealthRecommendations(cpuUsage, memoryUsage, p95Latency, errorRate)
        };

        return Ok(response);
    }

    /// <summary>
    /// Get scaling decision recommendation
    /// </summary>
    [HttpGet("scaling-decision")]
    [ProducesResponseType(typeof(ScalingDecisionResponse), 200)]
    public IActionResult GetScalingDecision()
    {
        var cpuUsage = GetCpuUsage();
        var memoryUsage = GetMemoryUsage();
        var (_, p95Latency) = GetLatencyMetrics();
        var requestsPerMinute = GetRequestsPerMinute();
        var errorRate = GetErrorRate();

        var (recommendation, confidence, reasons, estimatedCostImpact) = DetermineScalingRecommendation(
            cpuUsage, memoryUsage, p95Latency, requestsPerMinute, errorRate);

        var response = new ScalingDecisionResponse
        {
            CurrentArchitecture = "Monolith",
            Recommendation = recommendation,
            Confidence = confidence,
            Reasons = reasons,
            EstimatedCostImpact = estimatedCostImpact,
            EstimatedPerformanceGain = CalculatePerformanceGain(recommendation),
            NextReviewIn = recommendation == "Monolith" ? "24 hours" : "Immediate action required",
            ActionRequired = recommendation != "Monolith",
            MigrationEstimatedTime = GetMigrationTime(recommendation)
        };

        return Ok(response);
    }

    /// <summary>
    /// Get metrics trend over time (last 24 hours)
    /// </summary>
    [HttpGet("trend")]
    [ProducesResponseType(typeof(MetricsTrendResponse), 200)]
    public IActionResult GetTrend()
    {
        // This would ideally pull from a time-series database
        // For now, return current snapshot with historical simulation
        var response = new MetricsTrendResponse
        {
            Period = "24h",
            DataPoints = 24, // One per hour
            Trend = "Stable", // Would calculate from historical data
            Message = "Metrics trend analysis requires time-series data storage. " +
                      "Consider enabling Prometheus for detailed trending."
        };

        return Ok(response);
    }

    // Helper methods

    private double GetCpuUsage()
    {
        try
        {
            var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpuCounter.NextValue(); // First call returns 0
            System.Threading.Thread.Sleep(100);
            return cpuCounter.NextValue();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get CPU usage, returning simulated value");
            // Fallback: Use Process CPU time as approximation
            var process = Process.GetCurrentProcess();
            var totalTime = (DateTime.Now - process.StartTime).TotalMilliseconds;
            var cpuTime = process.TotalProcessorTime.TotalMilliseconds;
            return Math.Min((cpuTime / totalTime / Environment.ProcessorCount) * 100, 100);
        }
    }

    private double GetMemoryUsage()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var usedMemory = process.WorkingSet64;

            // On Linux, read from /proc/meminfo
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var memInfo = System.IO.File.ReadAllText("/proc/meminfo");
                var totalMatch = System.Text.RegularExpressions.Regex.Match(memInfo, @"MemTotal:\s+(\d+)");
                var availMatch = System.Text.RegularExpressions.Regex.Match(memInfo, @"MemAvailable:\s+(\d+)");

                if (totalMatch.Success && availMatch.Success)
                {
                    var totalKb = long.Parse(totalMatch.Groups[1].Value);
                    var availKb = long.Parse(availMatch.Groups[1].Value);
                    return ((totalKb - availKb) / (double)totalKb) * 100;
                }
            }

            // Fallback: Use GC memory
            var gcMemory = GC.GetTotalMemory(false);
            return (gcMemory / (double)Environment.WorkingSet) * 100;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get memory usage");
            return 50.0; // Default assumption
        }
    }

    private (double avgLatency, double p95Latency) GetLatencyMetrics()
    {
        var metrics = Middleware.MetricsMiddleware.GetMetrics();
        var durationMetrics = metrics.Where(m => m.Key.StartsWith("request_duration_ms_")).ToList();

        if (!durationMetrics.Any())
            return (0, 0);

        var allValues = durationMetrics.Select(m => m.Value.AverageValue).OrderBy(v => v).ToList();
        var avgLatency = allValues.Average();
        var p95Index = (int)(allValues.Count * 0.95);
        var p95Latency = allValues[Math.Min(p95Index, allValues.Count - 1)];

        return (avgLatency, p95Latency);
    }

    private double GetRequestsPerMinute()
    {
        var metrics = Middleware.MetricsMiddleware.GetMetrics();
        var totalRequests = metrics.Where(m => m.Key.StartsWith("request_total_"))
                                   .Sum(m => m.Value.Count);

        var uptime = (DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalMinutes;
        return uptime > 0 ? totalRequests / uptime : 0;
    }

    private double GetErrorRate()
    {
        var metrics = Middleware.MetricsMiddleware.GetMetrics();
        var totalRequests = metrics.Where(m => m.Key.StartsWith("request_total_")).Sum(m => m.Value.Count);
        var totalErrors = metrics.Where(m => m.Key.StartsWith("request_error_")).Sum(m => m.Value.Count);

        return totalRequests > 0 ? (totalErrors / (double)totalRequests) * 100 : 0;
    }

    private string GetUptime()
    {
        var uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
        return $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m";
    }

    private string GetStatus(double current, double warning, double critical)
    {
        if (current >= critical) return "Critical";
        if (current >= warning) return "Warning";
        return "OK";
    }

    private double CalculateComponentScore(double current, double threshold)
    {
        // Score decreases as current approaches threshold
        var ratio = current / threshold;
        if (ratio >= 1.0) return 0; // At or above threshold = 0 score
        return Math.Max(0, 100 * (1 - ratio));
    }

    private List<string> GetHealthRecommendations(double cpu, double memory, double latency, double errorRate)
    {
        var recommendations = new List<string>();

        if (cpu >= CPU_CRITICAL_THRESHOLD)
            recommendations.Add("🔴 CRITICAL: CPU usage extremely high. Consider immediate scaling.");
        else if (cpu >= CPU_WARNING_THRESHOLD)
            recommendations.Add("🟡 WARNING: CPU usage elevated. Monitor closely and prepare for scaling.");

        if (memory >= MEMORY_CRITICAL_THRESHOLD)
            recommendations.Add("🔴 CRITICAL: Memory usage extremely high. Risk of out-of-memory errors.");
        else if (memory >= MEMORY_WARNING_THRESHOLD)
            recommendations.Add("🟡 WARNING: Memory usage elevated. Consider increasing memory allocation.");

        if (latency >= LATENCY_CRITICAL_THRESHOLD)
            recommendations.Add("🔴 CRITICAL: API latency unacceptable. Immediate optimization required.");
        else if (latency >= LATENCY_WARNING_THRESHOLD)
            recommendations.Add("🟡 WARNING: API latency degraded. Review slow endpoints.");

        if (errorRate >= 5.0)
            recommendations.Add("🔴 CRITICAL: High error rate. Investigate application errors.");
        else if (errorRate >= 1.0)
            recommendations.Add("🟡 WARNING: Elevated error rate. Review error logs.");

        if (recommendations.Count == 0)
            recommendations.Add("✅ All metrics healthy. System operating normally.");

        return recommendations;
    }

    private (string recommendation, int confidence, List<string> reasons, string costImpact) DetermineScalingRecommendation(
        double cpu, double memory, double latency, double rpm, double errorRate)
    {
        var reasons = new List<string>();
        var criticalCount = 0;
        var warningCount = 0;

        // Analyze each metric
        if (cpu >= CPU_CRITICAL_THRESHOLD)
        {
            criticalCount++;
            reasons.Add($"CPU at {cpu:F1}% (critical threshold: {CPU_CRITICAL_THRESHOLD}%)");
        }
        else if (cpu >= CPU_WARNING_THRESHOLD)
        {
            warningCount++;
            reasons.Add($"CPU at {cpu:F1}% (approaching {CPU_WARNING_THRESHOLD}% warning threshold)");
        }

        if (memory >= MEMORY_CRITICAL_THRESHOLD)
        {
            criticalCount++;
            reasons.Add($"Memory at {memory:F1}% (critical threshold: {MEMORY_CRITICAL_THRESHOLD}%)");
        }
        else if (memory >= MEMORY_WARNING_THRESHOLD)
        {
            warningCount++;
            reasons.Add($"Memory at {memory:F1}% (approaching {MEMORY_WARNING_THRESHOLD}% warning threshold)");
        }

        if (latency >= LATENCY_CRITICAL_THRESHOLD)
        {
            criticalCount++;
            reasons.Add($"Latency at {latency:F1}ms (critical threshold: {LATENCY_CRITICAL_THRESHOLD}ms)");
        }
        else if (latency >= LATENCY_WARNING_THRESHOLD)
        {
            warningCount++;
            reasons.Add($"Latency at {latency:F1}ms (approaching {LATENCY_WARNING_THRESHOLD}ms warning threshold)");
        }

        if (rpm >= REQUEST_RATE_MICROSERVICES_THRESHOLD)
        {
            warningCount++;
            reasons.Add($"Request rate at {rpm:F0} req/min (microservices recommended above {REQUEST_RATE_MICROSERVICES_THRESHOLD})");
        }

        // Decision logic
        if (criticalCount >= 2)
        {
            // Multiple critical metrics - recommend full microservices
            return ("Microservices", 95,
                reasons.Any() ? reasons : new List<string> { "Multiple critical resource constraints detected" },
                "+$150-300/month");
        }
        else if (criticalCount == 1 || warningCount >= 2)
        {
            // One critical or multiple warnings - recommend hybrid
            return ("Hybrid", 85,
                reasons.Any() ? reasons : new List<string> { "Resource constraints detected in specific components" },
                "+$50-100/month (extract bottleneck service only)");
        }
        else if (warningCount == 1)
        {
            // Single warning - monitor closely but stay monolith
            return ("Monolith", 75,
                new List<string> { "Minor resource concerns, continue monitoring", reasons.FirstOrDefault() ?? "Some metrics approaching thresholds" },
                "$0 (no change needed yet)");
        }
        else
        {
            // All metrics healthy
            return ("Monolith", 95,
                new List<string> { "All metrics within healthy ranges", "Current architecture sufficient for load" },
                "$0 (no change needed)");
        }
    }

    private string CalculatePerformanceGain(string recommendation)
    {
        return recommendation switch
        {
            "Microservices" => "40-60% improvement (independent scaling, multi-region deployment)",
            "Hybrid" => "20-40% improvement (targeted service optimization)",
            _ => "N/A (no architecture change)"
        };
    }

    private string GetMigrationTime(string recommendation)
    {
        return recommendation switch
        {
            "Microservices" => "4-8 hours (full migration)",
            "Hybrid" => "2-4 hours (extract single service)",
            _ => "N/A"
        };
    }
}

// Response models
public record SystemMetricsResponse
{
    public DateTime Timestamp { get; init; }
    public MetricInfo Cpu { get; init; } = new();
    public MetricInfo Memory { get; init; } = new();
    public LatencyMetricInfo Latency { get; init; } = new();
    public double RequestsPerMinute { get; init; }
    public double ErrorRate { get; init; }
    public string Uptime { get; init; } = string.Empty;
}

public record MetricInfo
{
    public double Current { get; init; }
    public double WarningThreshold { get; init; }
    public double CriticalThreshold { get; init; }
    public string Status { get; init; } = "OK";
    public string Unit { get; init; } = string.Empty;
}

public record LatencyMetricInfo : MetricInfo
{
    public double Average { get; init; }
    public double P95 { get; init; }
}

public record HealthScoreResponse
{
    public double OverallScore { get; init; }
    public Dictionary<string, double> ComponentScores { get; init; } = new();
    public string Status { get; init; } = string.Empty;
    public List<string> Recommendations { get; init; } = new();
}

public record ScalingDecisionResponse
{
    public string CurrentArchitecture { get; init; } = string.Empty;
    public string Recommendation { get; init; } = string.Empty;
    public int Confidence { get; init; }
    public List<string> Reasons { get; init; } = new();
    public string EstimatedCostImpact { get; init; } = string.Empty;
    public string EstimatedPerformanceGain { get; init; } = string.Empty;
    public string NextReviewIn { get; init; } = string.Empty;
    public bool ActionRequired { get; init; }
    public string MigrationEstimatedTime { get; init; } = string.Empty;
}

public record MetricsTrendResponse
{
    public string Period { get; init; } = string.Empty;
    public int DataPoints { get; init; }
    public string Trend { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
