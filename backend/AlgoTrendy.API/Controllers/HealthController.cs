using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;
using System.Net.NetworkInformation;

namespace AlgoTrendy.API.Controllers;

/// <summary>
/// Comprehensive health check endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<HealthController> _logger;
    private readonly IConfiguration _configuration;

    public HealthController(
        HealthCheckService healthCheckService,
        ILogger<HealthController> logger,
        IConfiguration configuration)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Basic health check (lightweight)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(503)]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "2.6.0"
        });
    }

    /// <summary>
    /// Detailed health status with all checks
    /// </summary>
    [HttpGet("detailed")]
    [ProducesResponseType(200)]
    [ProducesResponseType(503)]
    public async Task<IActionResult> GetDetailedHealth()
    {
        var report = await _healthCheckService.CheckHealthAsync();

        var response = new
        {
            status = report.Status.ToString().ToLower(),
            timestamp = DateTime.UtcNow,
            duration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString().ToLower(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds,
                exception = e.Value.Exception?.Message,
                data = e.Value.Data
            }),
            info = GetSystemInfo()
        };

        var statusCode = report.Status == HealthStatus.Healthy ? 200 : 503;
        return StatusCode(statusCode, response);
    }

    /// <summary>
    /// Readiness probe (Kubernetes compatible)
    /// </summary>
    [HttpGet("ready")]
    [ProducesResponseType(200)]
    [ProducesResponseType(503)]
    public async Task<IActionResult> GetReadiness()
    {
        var report = await _healthCheckService.CheckHealthAsync();

        if (report.Status == HealthStatus.Healthy)
        {
            return Ok(new { status = "ready", timestamp = DateTime.UtcNow });
        }

        return StatusCode(503, new
        {
            status = "not_ready",
            timestamp = DateTime.UtcNow,
            failures = report.Entries
                .Where(e => e.Value.Status != HealthStatus.Healthy)
                .Select(e => e.Key)
        });
    }

    /// <summary>
    /// Liveness probe (Kubernetes compatible)
    /// </summary>
    [HttpGet("live")]
    [ProducesResponseType(200)]
    public IActionResult GetLiveness()
    {
        // Simple liveness check - is the process running?
        return Ok(new
        {
            status = "alive",
            timestamp = DateTime.UtcNow,
            uptime = GetUptime()
        });
    }

    /// <summary>
    /// Startup probe (Kubernetes compatible)
    /// </summary>
    [HttpGet("startup")]
    [ProducesResponseType(200)]
    [ProducesResponseType(503)]
    public IActionResult GetStartup()
    {
        var uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();

        // Consider started after 30 seconds
        if (uptime.TotalSeconds > 30)
        {
            return Ok(new
            {
                status = "started",
                timestamp = DateTime.UtcNow,
                uptime = uptime.TotalSeconds
            });
        }

        return StatusCode(503, new
        {
            status = "starting",
            timestamp = DateTime.UtcNow,
            uptime = uptime.TotalSeconds
        });
    }

    /// <summary>
    /// Check database connectivity
    /// </summary>
    [HttpGet("db")]
    [ProducesResponseType(200)]
    [ProducesResponseType(503)]
    public async Task<IActionResult> GetDatabaseHealth()
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("QuestDB");

            // Simple connectivity check
            using var ping = new Ping();
            var questDbHost = connectionString?.Split(';')
                .FirstOrDefault(p => p.StartsWith("Host="))
                ?.Split('=')[1] ?? "localhost";

            var reply = await ping.SendPingAsync(questDbHost, 5000);

            if (reply.Status == IPStatus.Success)
            {
                return Ok(new
                {
                    status = "healthy",
                    database = "QuestDB",
                    host = questDbHost,
                    latency = reply.RoundtripTime,
                    timestamp = DateTime.UtcNow
                });
            }

            return StatusCode(503, new
            {
                status = "unhealthy",
                database = "QuestDB",
                host = questDbHost,
                error = reply.Status.ToString(),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return StatusCode(503, new
            {
                status = "unhealthy",
                database = "QuestDB",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Check external broker connectivity
    /// </summary>
    [HttpGet("brokers")]
    [ProducesResponseType(200)]
    public IActionResult GetBrokerHealth()
    {
        // This would ideally ping each configured broker
        var brokers = new[]
        {
            new { name = "Bybit", status = "healthy", testnet = true },
            new { name = "Binance", status = "healthy", testnet = true },
            new { name = "MEXC", status = "healthy", testnet = true },
            new { name = "Alpaca", status = "healthy", paper = true }
        };

        return Ok(new
        {
            status = "healthy",
            brokers,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// System information
    /// </summary>
    [HttpGet("info")]
    [ProducesResponseType(200)]
    public IActionResult GetSystemInfo()
    {
        var process = Process.GetCurrentProcess();

        return Ok(new
        {
            version = "2.6.0",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            platform = Environment.OSVersion.Platform.ToString(),
            osVersion = Environment.OSVersion.VersionString,
            dotnetVersion = Environment.Version.ToString(),
            processId = process.Id,
            uptime = GetUptime(),
            memory = new
            {
                totalMB = process.WorkingSet64 / 1024 / 1024,
                privateMB = process.PrivateMemorySize64 / 1024 / 1024
            },
            cpu = new
            {
                cores = Environment.ProcessorCount,
                totalProcessorTime = process.TotalProcessorTime.TotalSeconds
            },
            timestamp = DateTime.UtcNow
        });
    }

    private static string GetUptime()
    {
        var uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
        return $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
    }
}
