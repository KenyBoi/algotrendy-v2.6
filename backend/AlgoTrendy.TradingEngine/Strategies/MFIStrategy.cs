namespace AlgoTrendy.TradingEngine.Strategies;

using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using AlgoTrendy.TradingEngine.Services;
using Microsoft.Extensions.Logging;

/// <summary>
/// MFI (Money Flow Index) trading strategy
///
/// Strategy Logic:
/// - Buy when MFI < oversold threshold (default: 20) - indicates selling pressure may be exhausted
/// - Sell when MFI > overbought threshold (default: 80) - indicates buying pressure may be exhausted
/// - Hold when MFI is in neutral zone
/// - Confidence based on how extreme the MFI value is
///
/// MFI is similar to RSI but incorporates volume:
/// - Typical Price = (High + Low + Close) / 3
/// - Money Flow = Typical Price × Volume
/// - MFI ranges from 0 to 100
/// - Lower values indicate oversold conditions
/// - Higher values indicate overbought conditions
/// </summary>
public class MFIStrategy : IStrategy
{
    private readonly MFIStrategyConfig _config;
    private readonly IndicatorService _indicatorService;
    private readonly ILogger<MFIStrategy> _logger;

    public string StrategyName => "MFI";

    public MFIStrategy(
        MFIStrategyConfig config,
        IndicatorService indicatorService,
        ILogger<MFIStrategy> logger)
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
            _logger.LogDebug("Analyzing {Symbol} with MFI strategy", currentData.Symbol);

            // Calculate MFI
            var allData = historicalData.Append(currentData).ToList();
            var mfi = await _indicatorService.CalculateMFIAsync(
                currentData.Symbol,
                allData,
                _config.Period,
                cancellationToken);

            var price = currentData.Close;

            // Base signal
            var action = SignalAction.Hold;
            var confidence = 0.4m;
            var reason = $"MFI: {mfi:F1}";

            // MFI-based decision
            if (mfi < _config.OversoldThreshold)
            {
                action = SignalAction.Buy;

                // Higher confidence for deeper oversold
                // Example: MFI=10, threshold=20 -> confidence = (20-10)/20 = 0.50
                confidence = (_config.OversoldThreshold - mfi) / _config.OversoldThreshold;
                confidence = Math.Min(confidence, 0.9m); // Cap at 0.9
                confidence = Math.Max(confidence, 0.5m); // Minimum 0.5

                reason = $"MFI: {mfi:F1} (OVERSOLD - Money flowing out, potential reversal)";
                _logger.LogInformation("BUY signal generated for {Symbol}: {Reason}", currentData.Symbol, reason);
            }
            else if (mfi > _config.OverboughtThreshold)
            {
                action = SignalAction.Sell;

                // Higher confidence for deeper overbought
                // Example: MFI=90, threshold=80 -> confidence = (90-80)/(100-80) = 0.50
                confidence = (mfi - _config.OverboughtThreshold) / (100m - _config.OverboughtThreshold);
                confidence = Math.Min(confidence, 0.9m); // Cap at 0.9
                confidence = Math.Max(confidence, 0.5m); // Minimum 0.5

                reason = $"MFI: {mfi:F1} (OVERBOUGHT - Heavy buying, potential reversal)";
                _logger.LogInformation("SELL signal generated for {Symbol}: {Reason}", currentData.Symbol, reason);
            }
            else
            {
                // MFI in neutral zone
                reason = $"MFI: {mfi:F1} (NEUTRAL - Balanced money flow)";
                _logger.LogDebug("HOLD signal for {Symbol}: MFI in neutral zone", currentData.Symbol);
            }

            // Volume confirmation - MFI already incorporates volume, but we can still check absolute volume
            if (currentData.Volume < _config.MinVolumeThreshold)
            {
                confidence *= 0.8m; // Less penalty than other strategies since MFI already uses volume
                reason += $" [Low Volume: {currentData.Volume:N0}]";
                _logger.LogDebug("Confidence slightly reduced due to low absolute volume for {Symbol}", currentData.Symbol);
            }

            // Divergence detection - check if price is moving opposite to MFI
            // This would require comparing previous MFI values, which we can add later
            // For now, we'll use the basic MFI levels

            // Calculate stop loss and take profit based on action
            decimal? stopLoss = null;
            decimal? takeProfit = null;

            if (action == SignalAction.Buy)
            {
                stopLoss = price * 0.97m;     // 3% stop loss
                takeProfit = price * 1.06m;   // 6% take profit
            }
            else if (action == SignalAction.Sell)
            {
                stopLoss = price * 1.03m;     // 3% stop loss (price going up)
                takeProfit = price * 0.94m;   // 6% take profit (price going down)
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
            _logger.LogError(ex, "Error analyzing {Symbol} with MFI strategy", currentData.Symbol);

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
/// Configuration for MFI strategy
/// </summary>
public class MFIStrategyConfig
{
    /// <summary>
    /// MFI calculation period
    /// Default: 14
    /// </summary>
    public int Period { get; set; } = 14;

    /// <summary>
    /// MFI threshold for oversold condition (BUY signal)
    /// Default: 20 (more extreme than RSI's 30)
    /// </summary>
    public decimal OversoldThreshold { get; set; } = 20m;

    /// <summary>
    /// MFI threshold for overbought condition (SELL signal)
    /// Default: 80 (more extreme than RSI's 70)
    /// </summary>
    public decimal OverboughtThreshold { get; set; } = 80m;

    /// <summary>
    /// Minimum volume required for signal validation
    /// Default: 50,000 (lower than other strategies since MFI already uses volume)
    /// </summary>
    public decimal MinVolumeThreshold { get; set; } = 50000m;
}
