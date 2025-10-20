using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlgoTrendy.Backtesting.Engines;

/// <summary>
/// Engine selection mode
/// </summary>
public enum BacktestEngineType
{
    /// <summary>
    /// Use QuantConnect Cloud API
    /// </summary>
    Cloud,

    /// <summary>
    /// Use local LEAN Docker engine
    /// </summary>
    Local,

    /// <summary>
    /// Use custom AlgoTrendy engine
    /// </summary>
    Custom,

    /// <summary>
    /// Auto-select: prefer local if available, fallback to cloud
    /// </summary>
    Auto
}

/// <summary>
/// Configuration for backtest engine selection
/// </summary>
public class BacktestEngineConfig
{
    /// <summary>
    /// Default engine type
    /// </summary>
    public BacktestEngineType DefaultEngine { get; set; } = BacktestEngineType.Auto;

    /// <summary>
    /// Allow fallback to cloud if local fails
    /// </summary>
    public bool AllowCloudFallback { get; set; } = true;
}

/// <summary>
/// Factory for creating and selecting backtest engines
/// </summary>
public class BacktestEngineFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly BacktestEngineConfig _config;
    private readonly ILogger<BacktestEngineFactory> _logger;

    public BacktestEngineFactory(
        IServiceProvider serviceProvider,
        IOptions<BacktestEngineConfig> config,
        ILogger<BacktestEngineFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _config = config.Value;
        _logger = logger;
    }

    /// <summary>
    /// Get backtest engine based on configuration or explicit selection
    /// </summary>
    /// <param name="engineType">Specific engine type, or null to use default</param>
    /// <returns>Selected backtest engine</returns>
    public IBacktestEngine GetEngine(BacktestEngineType? engineType = null)
    {
        var selectedType = engineType ?? _config.DefaultEngine;

        _logger.LogInformation("Selecting backtest engine: {EngineType}", selectedType);

        return selectedType switch
        {
            BacktestEngineType.Cloud => GetCloudEngine(),
            BacktestEngineType.Local => GetLocalEngine(),
            BacktestEngineType.Custom => GetCustomEngine(),
            BacktestEngineType.Auto => GetAutoEngine(),
            _ => throw new ArgumentException($"Unknown engine type: {selectedType}")
        };
    }

    /// <summary>
    /// Get QuantConnect Cloud engine
    /// </summary>
    private IBacktestEngine GetCloudEngine()
    {
        var engine = _serviceProvider.GetService(typeof(QuantConnectBacktestEngine)) as IBacktestEngine;
        if (engine == null)
        {
            throw new InvalidOperationException("QuantConnect Cloud engine is not registered");
        }

        _logger.LogInformation("Using QuantConnect Cloud engine");
        return engine;
    }

    /// <summary>
    /// Get local LEAN Docker engine
    /// </summary>
    private IBacktestEngine GetLocalEngine()
    {
        var engine = _serviceProvider.GetService(typeof(LocalLeanBacktestEngine)) as IBacktestEngine;
        if (engine == null)
        {
            _logger.LogWarning("Local LEAN engine is not available");

            if (_config.AllowCloudFallback)
            {
                _logger.LogInformation("Falling back to QuantConnect Cloud engine");
                return GetCloudEngine();
            }

            throw new InvalidOperationException("Local LEAN engine is not registered and fallback is disabled");
        }

        _logger.LogInformation("Using local LEAN Docker engine");
        return engine;
    }

    /// <summary>
    /// Get custom AlgoTrendy engine
    /// </summary>
    private IBacktestEngine GetCustomEngine()
    {
        var engine = _serviceProvider.GetService(typeof(CustomBacktestEngine)) as IBacktestEngine;
        if (engine == null)
        {
            throw new InvalidOperationException("Custom backtest engine is not registered");
        }

        _logger.LogInformation("Using custom AlgoTrendy engine");
        return engine;
    }

    /// <summary>
    /// Auto-select engine: prefer local, fallback to cloud
    /// </summary>
    private IBacktestEngine GetAutoEngine()
    {
        _logger.LogInformation("Auto-selecting backtest engine");

        // Try local first
        var localEngine = _serviceProvider.GetService(typeof(LocalLeanBacktestEngine)) as IBacktestEngine;
        if (localEngine != null)
        {
            // Check if Docker is available
            if (IsDockerAvailable())
            {
                _logger.LogInformation("Auto-selected: Local LEAN engine (Docker available)");
                return localEngine;
            }
        }

        // Try custom engine
        var customEngine = _serviceProvider.GetService(typeof(CustomBacktestEngine)) as IBacktestEngine;
        if (customEngine != null)
        {
            _logger.LogInformation("Auto-selected: Custom AlgoTrendy engine");
            return customEngine;
        }

        // Fallback to cloud
        _logger.LogInformation("Auto-selected: QuantConnect Cloud engine (local not available)");
        return GetCloudEngine();
    }

    /// <summary>
    /// Check if Docker is available
    /// </summary>
    private bool IsDockerAvailable()
    {
        try
        {
            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get all available engines
    /// </summary>
    public List<(string Name, string Description, bool Available)> GetAvailableEngines()
    {
        var engines = new List<(string Name, string Description, bool Available)>();

        // Check Cloud
        var cloudEngine = _serviceProvider.GetService(typeof(QuantConnectBacktestEngine)) as IBacktestEngine;
        if (cloudEngine != null)
        {
            engines.Add((cloudEngine.EngineName, cloudEngine.EngineDescription, true));
        }

        // Check Local
        var localEngine = _serviceProvider.GetService(typeof(LocalLeanBacktestEngine)) as IBacktestEngine;
        if (localEngine != null)
        {
            var dockerAvailable = IsDockerAvailable();
            engines.Add((localEngine.EngineName, localEngine.EngineDescription, dockerAvailable));
        }

        // Check Custom
        var customEngine = _serviceProvider.GetService(typeof(CustomBacktestEngine)) as IBacktestEngine;
        if (customEngine != null)
        {
            engines.Add((customEngine.EngineName, customEngine.EngineDescription, true));
        }

        return engines;
    }
}
