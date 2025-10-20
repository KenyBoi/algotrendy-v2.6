namespace AlgoTrendy.TradingEngine.Services;

using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.TradingEngine.Strategies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Factory for creating strategy instances based on configuration
/// Ported from v2.5 strategy_resolver.py StrategyResolver
/// </summary>
public class StrategyFactory
{
    private readonly IConfiguration _configuration;
    private readonly IndicatorService _indicatorService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<StrategyFactory> _logger;

    // Registry of available strategies
    private readonly Dictionary<string, Func<IStrategy>> _strategies;

    public StrategyFactory(
        IConfiguration configuration,
        IndicatorService indicatorService,
        ILoggerFactory loggerFactory)
    {
        _configuration = configuration;
        _indicatorService = indicatorService;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<StrategyFactory>();

        // Initialize strategy registry
        _strategies = new Dictionary<string, Func<IStrategy>>(StringComparer.OrdinalIgnoreCase)
        {
            ["momentum"] = CreateMomentumStrategy,
            ["rsi"] = CreateRSIStrategy,
            ["macd"] = CreateMACDStrategy,
            ["mfi"] = CreateMFIStrategy,
            ["vwap"] = CreateVWAPStrategy
        };
    }

    /// <summary>
    /// Creates a strategy instance by name
    /// </summary>
    /// <param name="strategyName">Name of the strategy (momentum, rsi)</param>
    /// <returns>Strategy instance</returns>
    /// <exception cref="ArgumentException">If strategy name is unknown</exception>
    public IStrategy CreateStrategy(string strategyName)
    {
        if (string.IsNullOrWhiteSpace(strategyName))
        {
            throw new ArgumentException("Strategy name cannot be null or empty", nameof(strategyName));
        }

        if (!_strategies.TryGetValue(strategyName, out var strategyFactory))
        {
            var availableStrategies = string.Join(", ", GetAvailableStrategies());
            throw new ArgumentException(
                $"Unknown strategy: {strategyName}. Available strategies: {availableStrategies}",
                nameof(strategyName));
        }

        var strategy = strategyFactory();
        _logger.LogInformation("Created strategy: {StrategyName}", strategyName);

        return strategy;
    }

    /// <summary>
    /// Gets list of available strategy names
    /// </summary>
    public IEnumerable<string> GetAvailableStrategies()
    {
        return _strategies.Keys;
    }

    /// <summary>
    /// Registers a custom strategy
    /// </summary>
    /// <param name="strategyName">Name to register the strategy under</param>
    /// <param name="strategyFactory">Factory function to create the strategy</param>
    public void RegisterStrategy(string strategyName, Func<IStrategy> strategyFactory)
    {
        if (string.IsNullOrWhiteSpace(strategyName))
        {
            throw new ArgumentException("Strategy name cannot be null or empty", nameof(strategyName));
        }

        if (strategyFactory == null)
        {
            throw new ArgumentNullException(nameof(strategyFactory));
        }

        _strategies[strategyName] = strategyFactory;
        _logger.LogInformation("Registered custom strategy: {StrategyName}", strategyName);
    }

    #region Strategy Factory Methods

    private IStrategy CreateMomentumStrategy()
    {
        var config = _configuration
            .GetSection("TradingStrategies:Momentum")
            .Get<MomentumStrategyConfig>() ?? new MomentumStrategyConfig();

        _logger.LogDebug("Creating Momentum strategy with config: BuyThreshold={BuyThreshold}, SellThreshold={SellThreshold}",
            config.BuyThreshold, config.SellThreshold);

        return new MomentumStrategy(
            config,
            _indicatorService,
            _loggerFactory.CreateLogger<MomentumStrategy>());
    }

    private IStrategy CreateRSIStrategy()
    {
        var config = _configuration
            .GetSection("TradingStrategies:RSI")
            .Get<RSIStrategyConfig>() ?? new RSIStrategyConfig();

        _logger.LogDebug("Creating RSI strategy with config: Period={Period}, OversoldThreshold={OversoldThreshold}, OverboughtThreshold={OverboughtThreshold}",
            config.Period, config.OversoldThreshold, config.OverboughtThreshold);

        return new RSIStrategy(
            config,
            _indicatorService,
            _loggerFactory.CreateLogger<RSIStrategy>());
    }

    private IStrategy CreateMACDStrategy()
    {
        var config = _configuration
            .GetSection("TradingStrategies:MACD")
            .Get<MACDStrategyConfig>() ?? new MACDStrategyConfig();

        _logger.LogDebug("Creating MACD strategy with config: FastPeriod={FastPeriod}, SlowPeriod={SlowPeriod}, SignalPeriod={SignalPeriod}",
            config.FastPeriod, config.SlowPeriod, config.SignalPeriod);

        return new MACDStrategy(
            config,
            _indicatorService,
            _loggerFactory.CreateLogger<MACDStrategy>());
    }

    private IStrategy CreateMFIStrategy()
    {
        var config = _configuration
            .GetSection("TradingStrategies:MFI")
            .Get<MFIStrategyConfig>() ?? new MFIStrategyConfig();

        _logger.LogDebug("Creating MFI strategy with config: Period={Period}, OversoldThreshold={OversoldThreshold}, OverboughtThreshold={OverboughtThreshold}",
            config.Period, config.OversoldThreshold, config.OverboughtThreshold);

        return new MFIStrategy(
            config,
            _indicatorService,
            _loggerFactory.CreateLogger<MFIStrategy>());
    }

    private IStrategy CreateVWAPStrategy()
    {
        var config = _configuration
            .GetSection("TradingStrategies:VWAP")
            .Get<VWAPStrategyConfig>() ?? new VWAPStrategyConfig();

        _logger.LogDebug("Creating VWAP strategy with config: Period={Period}, BuyDeviationThreshold={BuyDeviationThreshold}, SellDeviationThreshold={SellDeviationThreshold}",
            config.Period, config.BuyDeviationThreshold, config.SellDeviationThreshold);

        return new VWAPStrategy(
            config,
            _indicatorService,
            _loggerFactory.CreateLogger<VWAPStrategy>());
    }

    #endregion
}

/// <summary>
/// Extension methods for registering strategy services
/// </summary>
public static class StrategyServiceExtensions
{
    /// <summary>
    /// Registers strategy-related services with the DI container
    /// </summary>
    public static IServiceCollection AddStrategyServices(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<IndicatorService>();
        services.AddSingleton<StrategyFactory>();

        return services;
    }
}
