namespace AlgoTrendy.MultiCharts.Configuration;

/// <summary>
/// Configuration options for MultiCharts integration
/// </summary>
public class MultiChartsOptions
{
    public const string SectionName = "MultiCharts";

    /// <summary>
    /// Enable or disable MultiCharts integration
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// MultiCharts API endpoint
    /// </summary>
    public string ApiEndpoint { get; set; } = "http://localhost:8899";

    /// <summary>
    /// MultiCharts data directory path
    /// </summary>
    public string DataPath { get; set; } = "C:\\MultiCharts\\Data";

    /// <summary>
    /// MultiCharts strategies directory path
    /// </summary>
    public string StrategyPath { get; set; } = "C:\\MultiCharts\\Strategies";

    /// <summary>
    /// API timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Maximum backtest duration in minutes
    /// </summary>
    public int MaxBacktestDurationMinutes { get; set; } = 60;

    /// <summary>
    /// Enable automatic retry on failures
    /// </summary>
    public bool EnableRetry { get; set; } = true;

    /// <summary>
    /// Maximum retry attempts
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Retry delay in milliseconds
    /// </summary>
    public int RetryDelayMilliseconds { get; set; } = 1000;

    /// <summary>
    /// Enable request/response logging
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// API key for authentication (if required)
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// API secret for authentication (if required)
    /// </summary>
    public string? ApiSecret { get; set; }
}
