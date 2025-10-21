using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AlgoTrendy.API.Middleware;

/// <summary>
/// Middleware for tracking application metrics
/// </summary>
public class MetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MetricsMiddleware> _logger;
    private static readonly Dictionary<string, MetricCounter> _counters = new();
    private static readonly object _lock = new();

    public MetricsMiddleware(RequestDelegate next, ILogger<MetricsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var path = context.Request.Path.Value ?? "/";
        var method = context.Request.Method;

        try
        {
            await _next(context);
            sw.Stop();

            // Record metrics
            RecordMetric("request_total", method, path, context.Response.StatusCode);
            RecordMetric("request_duration_ms", method, path, (int)sw.ElapsedMilliseconds);

            // Log slow requests (> 1 second)
            if (sw.ElapsedMilliseconds > 1000)
            {
                _logger.LogWarning(
                    "Slow request: {Method} {Path} took {Duration}ms",
                    method, path, sw.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            RecordMetric("request_error", method, path, 500);

            _logger.LogError(ex,
                "Request failed: {Method} {Path} after {Duration}ms",
                method, path, sw.ElapsedMilliseconds);

            throw;
        }
    }

    private static void RecordMetric(string metricName, string method, string path, int value)
    {
        var key = $"{metricName}_{method}_{NormalizePath(path)}";

        lock (_lock)
        {
            if (!_counters.ContainsKey(key))
            {
                _counters[key] = new MetricCounter { Name = key };
            }

            _counters[key].Count++;
            _counters[key].TotalValue += value;
            _counters[key].LastValue = value;
            _counters[key].LastUpdated = DateTime.UtcNow;
        }
    }

    private static string NormalizePath(string path)
    {
        // Normalize paths to group similar endpoints
        // e.g., /api/orders/123 -> /api/orders/{id}

        if (path.StartsWith("/api/"))
        {
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

            // Replace numeric/guid segments with placeholders
            for (int i = 0; i < segments.Length; i++)
            {
                if (Guid.TryParse(segments[i], out _))
                {
                    segments[i] = "{id}";
                }
                else if (int.TryParse(segments[i], out _))
                {
                    segments[i] = "{id}";
                }
            }

            return "/" + string.Join("/", segments);
        }

        return path;
    }

    /// <summary>
    /// Gets current metrics snapshot
    /// </summary>
    public static Dictionary<string, MetricCounter> GetMetrics()
    {
        lock (_lock)
        {
            return new Dictionary<string, MetricCounter>(_counters);
        }
    }

    /// <summary>
    /// Resets all metrics
    /// </summary>
    public static void ResetMetrics()
    {
        lock (_lock)
        {
            _counters.Clear();
        }
    }
}

/// <summary>
/// Metric counter for tracking statistics
/// </summary>
public class MetricCounter
{
    public string Name { get; set; } = string.Empty;
    public long Count { get; set; }
    public long TotalValue { get; set; }
    public int LastValue { get; set; }
    public DateTime LastUpdated { get; set; }

    public double AverageValue => Count > 0 ? (double)TotalValue / Count : 0;
}
