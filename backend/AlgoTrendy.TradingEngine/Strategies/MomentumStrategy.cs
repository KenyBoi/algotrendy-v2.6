namespace AlgoTrendy.TradingEngine.Strategies;

using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using AlgoTrendy.TradingEngine.Services;
using Microsoft.Extensions.Logging;

/// <summary>
/// Momentum-based trading strategy
/// Ported from v2.5 strategy_resolver.py MomentumStrategy
///
/// Strategy Logic:
/// - Buy if price change > buy threshold and volatility is acceptable
/// - Sell if price change < sell threshold and volatility is acceptable
/// - Hold otherwise
/// - Confidence based on momentum strength
/// </summary>
public class MomentumStrategy : IStrategy
{
    private readonly MomentumStrategyConfig _config;
    private readonly IndicatorService _indicatorService;
    private readonly ILogger<MomentumStrategy> _logger;

    public string StrategyName => "Momentum";

    public MomentumStrategy(
        MomentumStrategyConfig config,
        IndicatorService indicatorService,
        ILogger<MomentumStrategy> logger)
    {
        _config = config;
        _indicatorService = indicatorService;
        _logger = logger;
    }

    public async Task<TradingSignal> AnalyzeAsync(
        MarketData currentData,
        IEnumerable<MarketData> historicalData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Analyzing {Symbol} with Momentum strategy", currentData.Symbol);

            // Calculate price change percentage
            var priceChange = currentData.ChangePercent;
            var price = currentData.Close;
            var volume = currentData.Volume;

            // Calculate volatility
            var allData = historicalData.Append(currentData).ToList();
            var volatility = await _indicatorService.CalculateVolatilityAsync(
                currentData.Symbol,
                allData,
                20,
                cancellationToken);

            // Base signal
            var action = SignalAction.Hold;
            var confidence = 0.3m;
            var reason = $"Momentum: {priceChange:+0.00}% change";

            // Momentum-based decision
            if (priceChange > _config.BuyThreshold && volatility < _config.VolatilityFilter)
            {
                action = SignalAction.Buy;
                // Normalize confidence to 0.95 max (stronger momentum = higher confidence)
                confidence = Math.Min(Math.Abs(priceChange) / 5.0m, 0.95m);
                reason = $"Momentum: {priceChange:+0.00}% change (STRONG UPWARD), Volatility: {volatility:F4}";

                _logger.LogInformation("BUY signal generated for {Symbol}: {Reason}", currentData.Symbol, reason);
            }
            else if (priceChange < _config.SellThreshold && volatility < _config.VolatilityFilter)
            {
                action = SignalAction.Sell;
                confidence = Math.Min(Math.Abs(priceChange) / 5.0m, 0.95m);
                reason = $"Momentum: {priceChange:+0.00}% change (STRONG DOWNWARD), Volatility: {volatility:F4}";

                _logger.LogInformation("SELL signal generated for {Symbol}: {Reason}", currentData.Symbol, reason);
            }
            else if (volatility >= _config.VolatilityFilter)
            {
                reason = $"Momentum: {priceChange:+0.00}% change, High Volatility: {volatility:F4} (FILTERED)";
                _logger.LogDebug("Signal filtered due to high volatility for {Symbol}", currentData.Symbol);
            }

            // Volume confirmation - reduce confidence for low volume
            if (volume < _config.MinVolumeThreshold)
            {
                confidence *= 0.7m;
                reason += $" [Low Volume: {volume:N0}]";
                _logger.LogDebug("Confidence reduced due to low volume for {Symbol}", currentData.Symbol);
            }

            // Calculate stop loss and take profit based on action
            decimal? stopLoss = null;
            decimal? takeProfit = null;

            if (action == SignalAction.Buy)
            {
                stopLoss = price * 0.98m;    // 2% stop loss
                takeProfit = price * 1.05m;   // 5% take profit
            }
            else if (action == SignalAction.Sell)
            {
                stopLoss = price * 1.02m;     // 2% stop loss (price going up)
                takeProfit = price * 0.95m;   // 5% take profit (price going down)
            }

            return new TradingSignal
            {
                Action = action,
                Confidence = confidence,
                EntryPrice = price,
                StopLoss = stopLoss,
                TakeProfit = takeProfit,
                Reason = reason,
                Symbol = currentData.Symbol,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing {Symbol} with Momentum strategy", currentData.Symbol);

            return new TradingSignal
            {
                Action = SignalAction.Hold,
                Confidence = 0.0m,
                Reason = $"Error: {ex.Message}",
                Symbol = currentData.Symbol,
                Timestamp = DateTime.UtcNow
            };
        }
    }
}

/// <summary>
/// Configuration for Momentum strategy
/// </summary>
public class MomentumStrategyConfig
{
    /// <summary>
    /// Percentage change threshold to trigger a BUY signal
    /// Default: 2.0% upward movement
    /// </summary>
    public decimal BuyThreshold { get; set; } = 2.0m;

    /// <summary>
    /// Percentage change threshold to trigger a SELL signal
    /// Default: -2.0% downward movement
    /// </summary>
    public decimal SellThreshold { get; set; } = -2.0m;

    /// <summary>
    /// Maximum volatility allowed for signals (to avoid whipsaws)
    /// Default: 0.15 (15%)
    /// </summary>
    public decimal VolatilityFilter { get; set; } = 0.15m;

    /// <summary>
    /// Minimum volume required for high confidence
    /// Default: 100,000
    /// </summary>
    public decimal MinVolumeThreshold { get; set; } = 100000m;
}
