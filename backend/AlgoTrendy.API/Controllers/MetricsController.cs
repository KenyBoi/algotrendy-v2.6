using AlgoTrendy.API.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace AlgoTrendy.API.Controllers;

/// <summary>
/// Metrics and application health monitoring
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MetricsController : ControllerBase
{
    private readonly ILogger<MetricsController> _logger;

    public MetricsController(ILogger<MetricsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get application metrics
    /// </summary>
    /// <returns>Current metrics snapshot</returns>
    [HttpGet]
    [ProducesResponseType(typeof(Dictionary<string, MetricCounter>), 200)]
    public IActionResult GetMetrics()
    {
        var metrics = MetricsMiddleware.GetMetrics();

        return Ok(new
        {
            timestamp = DateTime.UtcNow,
            metrics = metrics.Select(m => new
            {
                name = m.Key,
                count = m.Value.Count,
                totalValue = m.Value.TotalValue,
                averageValue = m.Value.AverageValue,
                lastValue = m.Value.LastValue,
                lastUpdated = m.Value.LastUpdated
            }).OrderByDescending(m => m.count).ToList()
        });
    }

    /// <summary>
    /// Get metrics summary
    /// </summary>
    /// <returns>Aggregated metrics summary</returns>
    [HttpGet("summary")]
    public IActionResult GetSummary()
    {
        var metrics = MetricsMiddleware.GetMetrics();

        var requestMetrics = metrics.Where(m => m.Key.StartsWith("request_total_")).ToList();
        var durationMetrics = metrics.Where(m => m.Key.StartsWith("request_duration_ms_")).ToList();
        var errorMetrics = metrics.Where(m => m.Key.StartsWith("request_error_")).ToList();

        var totalRequests = requestMetrics.Sum(m => m.Value.Count);
        var totalErrors = errorMetrics.Sum(m => m.Value.Count);
        var errorRate = totalRequests > 0 ? (double)totalErrors / totalRequests * 100 : 0;

        var avgDuration = durationMetrics.Count > 0
            ? durationMetrics.Average(m => m.Value.AverageValue)
            : 0;

        return Ok(new
        {
            timestamp = DateTime.UtcNow,
            summary = new
            {
                totalRequests,
                totalErrors,
                errorRate = Math.Round(errorRate, 2),
                averageDurationMs = Math.Round(avgDuration, 2),
                uptime = GetUptime(),
                topEndpoints = requestMetrics
                    .OrderByDescending(m => m.Value.Count)
                    .Take(10)
                    .Select(m => new
                    {
                        endpoint = m.Key.Replace("request_total_", ""),
                        requests = m.Value.Count,
                        avgDurationMs = durationMetrics
                            .FirstOrDefault(d => d.Key.Replace("request_duration_ms_", "") == m.Key.Replace("request_total_", ""))
                            ?.Value.AverageValue ?? 0
                    })
                    .ToList(),
                slowestEndpoints = durationMetrics
                    .OrderByDescending(m => m.Value.AverageValue)
                    .Take(10)
                    .Select(m => new
                    {
                        endpoint = m.Key.Replace("request_duration_ms_", ""),
                        avgDurationMs = Math.Round(m.Value.AverageValue, 2),
                        requestCount = m.Value.Count
                    })
                    .ToList()
            }
        });
    }

    /// <summary>
    /// Reset all metrics
    /// </summary>
    [HttpPost("reset")]
    public IActionResult ResetMetrics()
    {
        MetricsMiddleware.ResetMetrics();
        _logger.LogInformation("Metrics reset by {User}", User.Identity?.Name ?? "anonymous");

        return Ok(new
        {
            message = "Metrics reset successfully",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get Prometheus-compatible metrics
    /// </summary>
    [HttpGet("prometheus")]
    [Produces("text/plain")]
    public IActionResult GetPrometheusMetrics()
    {
        var metrics = MetricsMiddleware.GetMetrics();
        var lines = new List<string>();

        // Help and type headers
        lines.Add("# HELP http_requests_total Total number of HTTP requests");
        lines.Add("# TYPE http_requests_total counter");

        foreach (var metric in metrics.Where(m => m.Key.StartsWith("request_total_")))
        {
            var parts = metric.Key.Replace("request_total_", "").Split('_');
            var method = parts.Length > 0 ? parts[0] : "UNKNOWN";
            var path = parts.Length > 1 ? string.Join("_", parts.Skip(1)) : "unknown";

            lines.Add($"http_requests_total{{method=\"{method}\",path=\"{path}\"}} {metric.Value.Count}");
        }

        lines.Add("");
        lines.Add("# HELP http_request_duration_milliseconds HTTP request duration in milliseconds");
        lines.Add("# TYPE http_request_duration_milliseconds histogram");

        foreach (var metric in metrics.Where(m => m.Key.StartsWith("request_duration_ms_")))
        {
            var parts = metric.Key.Replace("request_duration_ms_", "").Split('_');
            var method = parts.Length > 0 ? parts[0] : "UNKNOWN";
            var path = parts.Length > 1 ? string.Join("_", parts.Skip(1)) : "unknown";

            lines.Add($"http_request_duration_milliseconds_sum{{method=\"{method}\",path=\"{path}\"}} {metric.Value.TotalValue}");
            lines.Add($"http_request_duration_milliseconds_count{{method=\"{method}\",path=\"{path}\"}} {metric.Value.Count}");
        }

        return Content(string.Join("\n", lines), "text/plain");
    }

    private static string GetUptime()
    {
        var uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
        return $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m";
    }
}
