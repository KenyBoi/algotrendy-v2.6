using AspNetCoreRateLimit;
using Microsoft.Extensions.Options;

namespace AlgoTrendy.API.Services;

/// <summary>
/// Custom rate limit policies for different user tiers
/// </summary>
public class RateLimitPolicies
{
    public RateLimitPolicy Free { get; set; } = new();
    public RateLimitPolicy Premium { get; set; } = new();
    public RateLimitPolicy Enterprise { get; set; } = new();
}

/// <summary>
/// Rate limit policy configuration for a user tier
/// </summary>
public class RateLimitPolicy
{
    public int GeneralLimit { get; set; }
    public int TradingLimit { get; set; }
    public int MarketDataLimit { get; set; }
    public int BurstAllowance { get; set; }
}

/// <summary>
/// Service to manage custom rate limiting configuration
/// </summary>
public class RateLimitConfiguration
{
    private readonly RateLimitPolicies _policies;
    private readonly ILogger<RateLimitConfiguration> _logger;

    public RateLimitConfiguration(
        IOptions<RateLimitPolicies> policies,
        ILogger<RateLimitConfiguration> logger)
    {
        _policies = policies.Value;
        _logger = logger;
    }

    /// <summary>
    /// Get rate limit rules for a specific user tier
    /// </summary>
    /// <param name="tier">User tier (Free, Premium, Enterprise)</param>
    /// <returns>List of rate limit rules</returns>
    public List<RateLimitRule> GetRulesForTier(string tier)
    {
        var policy = tier.ToLower() switch
        {
            "premium" => _policies.Premium,
            "enterprise" => _policies.Enterprise,
            _ => _policies.Free
        };

        return new List<RateLimitRule>
        {
            new RateLimitRule
            {
                Endpoint = "*",
                Period = "1m",
                Limit = policy.GeneralLimit
            },
            new RateLimitRule
            {
                Endpoint = "*/api/trading/*",
                Period = "1m",
                Limit = policy.TradingLimit
            },
            new RateLimitRule
            {
                Endpoint = "*/api/marketdata/*",
                Period = "1m",
                Limit = policy.MarketDataLimit
            },
            new RateLimitRule
            {
                Endpoint = "*/api/cryptodata/*",
                Period = "1m",
                Limit = policy.MarketDataLimit
            }
        };
    }

    /// <summary>
    /// Apply custom rate limit rules for a specific client
    /// </summary>
    /// <param name="clientId">Client identifier</param>
    /// <param name="tier">User tier</param>
    public ClientRateLimitPolicy GetClientPolicy(string clientId, string tier)
    {
        var rules = GetRulesForTier(tier);

        _logger.LogInformation(
            "Applied {Tier} tier rate limits for client {ClientId}: General={GeneralLimit}/min, Trading={TradingLimit}/min, MarketData={MarketDataLimit}/min",
            tier,
            clientId,
            rules[0].Limit,
            rules[1].Limit,
            rules[2].Limit);

        return new ClientRateLimitPolicy
        {
            ClientId = clientId,
            Rules = rules
        };
    }

    /// <summary>
    /// Check if a user can burst beyond their normal limits
    /// </summary>
    /// <param name="tier">User tier</param>
    /// <param name="currentCount">Current request count</param>
    /// <param name="normalLimit">Normal rate limit</param>
    /// <returns>True if burst is allowed</returns>
    public bool CanBurst(string tier, int currentCount, int normalLimit)
    {
        var policy = tier.ToLower() switch
        {
            "premium" => _policies.Premium,
            "enterprise" => _policies.Enterprise,
            _ => _policies.Free
        };

        var burstLimit = normalLimit + policy.BurstAllowance;
        return currentCount <= burstLimit;
    }
}

/// <summary>
/// Middleware to add rate limit headers to all responses
/// </summary>
public class RateLimitHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitHeadersMiddleware> _logger;

    public RateLimitHeadersMiddleware(
        RequestDelegate next,
        ILogger<RateLimitHeadersMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IRateLimitCounterStore counterStore, IIpPolicyStore policyStore)
    {
        // Get client IP
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var clientId = context.Request.Headers["X-ClientId"].FirstOrDefault() ?? clientIp;

        // Get rate limit counter for this client
        var endpoint = $"{context.Request.Method}:{context.Request.Path}";

        try
        {
            // Add rate limit information to response headers
            // Note: Actual counter values would come from the rate limiting middleware
            // This is a placeholder implementation
            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey("X-RateLimit-Limit"))
                {
                    // Set default headers (these would be populated by AspNetCoreRateLimit)
                    context.Response.Headers["X-RateLimit-Limit"] = "100";
                    context.Response.Headers["X-RateLimit-Remaining"] = "99";
                    context.Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds().ToString();
                }

                return Task.CompletedTask;
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to add rate limit headers for client {ClientId}", clientId);
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods for rate limiting configuration
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// Add rate limit headers middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseRateLimitHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitHeadersMiddleware>();
    }

    /// <summary>
    /// Configure custom rate limiting services
    /// </summary>
    public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure rate limit policies
        services.Configure<RateLimitPolicies>(configuration.GetSection("RateLimitPolicies"));

        // Register custom configuration service
        services.AddSingleton<RateLimitConfiguration>();

        // Configure IP rate limiting
        services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
        services.Configure<IpRateLimitPolicies>(configuration.GetSection("IpRateLimitPolicies"));

        // Configure Client rate limiting
        services.Configure<ClientRateLimitOptions>(configuration.GetSection("ClientRateLimiting"));
        services.Configure<ClientRateLimitPolicies>(configuration.GetSection("ClientRateLimitPolicies"));

        return services;
    }
}
