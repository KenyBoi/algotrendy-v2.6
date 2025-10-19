namespace AlgoTrendy.Core.Configuration;

/// <summary>
/// Configuration settings for Finnhub API integration
/// </summary>
public class FinnhubSettings
{
    /// <summary>
    /// Finnhub API key (required)
    /// Get your free API key from https://finnhub.io/register
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for Finnhub API
    /// </summary>
    public string BaseUrl { get; set; } = "https://finnhub.io/api/v1";

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Enable request/response logging for debugging
    /// </summary>
    public bool EnableLogging { get; set; } = false;

    /// <summary>
    /// Maximum requests per minute (free tier: 60/min, premium: 300/min)
    /// </summary>
    public int RateLimitPerMinute { get; set; } = 60;

    /// <summary>
    /// Validates that required settings are configured
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(ApiKey) &&
               !string.IsNullOrWhiteSpace(BaseUrl) &&
               TimeoutSeconds > 0 &&
               RateLimitPerMinute > 0;
    }
}
