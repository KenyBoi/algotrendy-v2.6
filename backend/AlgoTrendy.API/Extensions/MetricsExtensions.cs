using AlgoTrendy.API.Middleware;
using Microsoft.AspNetCore.Builder;

namespace AlgoTrendy.API.Extensions;

/// <summary>
/// Extension methods for metrics configuration
/// </summary>
public static class MetricsExtensions
{
    /// <summary>
    /// Add metrics tracking middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseMetrics(this IApplicationBuilder app)
    {
        return app.UseMiddleware<MetricsMiddleware>();
    }
}
