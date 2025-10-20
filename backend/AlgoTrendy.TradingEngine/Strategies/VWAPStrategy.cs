namespace AlgoTrendy.TradingEngine.Strategies;

using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using AlgoTrendy.TradingEngine.Services;
using Microsoft.Extensions.Logging;

/// <summary>
/// VWAP (Volume Weighted Average Price) trading strategy
///
/// Strategy Logic:
/// - Mean reversion strategy based on price deviation from VWAP
/// - Buy when price is significantly below VWAP (discount to fair value)
/// - Sell when price is significantly above VWAP (premium to fair value)
/// - Hold when price is near VWAP
/// - Confidence based on the percentage distance from VWAP
///
/// VWAP Calculation:
/// - VWAP = Σ(Typical Price × Volume) / Σ(Volume)
/// - Typical Price = (High + Low + Close) / 3
/// - Often used by institutional traders as an execution benchmark
/// - Price below VWAP = potential buy signal
/// - Price above VWAP = potential sell signal
/// </summary>
public class VWAPStrategy : IStrategy
{
    private readonly VWAPStrategyConfig _config;
    private readonly IndicatorService _indicatorService;
    private readonly ILogger<VWAPStrategy> _logger;

    public string StrategyName => "VWAP";

    public VWAPStrategy(
        VWAPStrategyConfig config,
        IndicatorService indicatorService,
        ILogger<VWAPStrategy> logger)
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
            _logger.LogDebug("Analyzing {Symbol} with VWAP strategy", currentData.Symbol);

            // Calculate VWAP
            var allData = historicalData.Append(currentData).ToList();
            var vwap = await _indicatorService.CalculateVWAPAsync(
                currentData.Symbol,
                allData,
                _config.Period,
                cancellationToken);

            var price = currentData.Close;

            // Calculate price deviation from VWAP as percentage
            var deviationPercent = ((price - vwap) / vwap) * 100m;

            // Base signal
            var action = SignalAction.Hold;
            var confidence = 0.4m;
            var reason = $"Price: {price:F2}, VWAP: {vwap:F2}, Deviation: {deviationPercent:+0.00}%";

            // VWAP-based decision (mean reversion)
            if (deviationPercent < _config.BuyDeviationThreshold)
            {
                // Price is significantly below VWAP - potential buy opportunity
                action = SignalAction.Buy;

                // Higher confidence for larger deviation below VWAP
                // Example: -3% deviation, threshold=-2% -> confidence = 3/2 = 1.5 -> cap at 0.9
                var deviationMagnitude = Math.Abs(deviationPercent);
                var thresholdMagnitude = Math.Abs(_config.BuyDeviationThreshold);
                confidence = Math.Min(deviationMagnitude / thresholdMagnitude * 0.6m, 0.9m);
                confidence = Math.Max(confidence, 0.5m); // Minimum 0.5

                reason = $"Price: {price:F2} < VWAP: {vwap:F2} (Deviation: {deviationPercent:+0.00}%, DISCOUNT)";
                _logger.LogInformation("BUY signal generated for {Symbol}: {Reason}", currentData.Symbol, reason);
            }
            else if (deviationPercent > _config.SellDeviationThreshold)
            {
                // Price is significantly above VWAP - potential sell opportunity
                action = SignalAction.Sell;

                // Higher confidence for larger deviation above VWAP
                var deviationMagnitude = Math.Abs(deviationPercent);
                var thresholdMagnitude = Math.Abs(_config.SellDeviationThreshold);
                confidence = Math.Min(deviationMagnitude / thresholdMagnitude * 0.6m, 0.9m);
                confidence = Math.Max(confidence, 0.5m); // Minimum 0.5

                reason = $"Price: {price:F2} > VWAP: {vwap:F2} (Deviation: {deviationPercent:+0.00}%, PREMIUM)";
                _logger.LogInformation("SELL signal generated for {Symbol}: {Reason}", currentData.Symbol, reason);
            }
            else
            {
                // Price is near VWAP - neutral zone
                reason = $"Price: {price:F2} ≈ VWAP: {vwap:F2} (Deviation: {deviationPercent:+0.00}%, FAIR VALUE)";
                _logger.LogDebug("HOLD signal for {Symbol}: Price near VWAP", currentData.Symbol);
            }

            // Volume confirmation - higher volume increases confidence
            // Since VWAP already uses volume, we check if current volume is above average
            var avgVolume = allData.TakeLast(_config.Period).Average(d => d.Volume);
            if (currentData.Volume > avgVolume * 1.2m)
            {
                // High volume confirms the signal
                confidence = Math.Min(confidence * 1.1m, 0.95m);
                reason += " [High Volume Confirmation]";
                _logger.LogDebug("Confidence increased due to high volume for {Symbol}", currentData.Symbol);
            }
            else if (currentData.Volume < avgVolume * 0.5m)
            {
                // Low volume weakens the signal
                confidence *= 0.8m;
                reason += $" [Low Volume: {currentData.Volume:N0}]";
                _logger.LogDebug("Confidence reduced due to low volume for {Symbol}", currentData.Symbol);
            }

            // Calculate stop loss and take profit based on action and VWAP
            decimal? stopLoss = null;
            decimal? takeProfit = null;

            if (action == SignalAction.Buy)
            {
                // Stop loss below entry, take profit at VWAP or slightly above
                stopLoss = price * 0.97m;     // 3% stop loss
                takeProfit = vwap * 1.005m;   // Take profit slightly above VWAP (mean reversion target)
            }
            else if (action == SignalAction.Sell)
            {
                // Stop loss above entry, take profit at VWAP or slightly below
                stopLoss = price * 1.03m;     // 3% stop loss
                takeProfit = vwap * 0.995m;   // Take profit slightly below VWAP (mean reversion target)
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
            _logger.LogError(ex, "Error analyzing {Symbol} with VWAP strategy", currentData.Symbol);

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
/// Configuration for VWAP strategy
/// </summary>
public class VWAPStrategyConfig
{
    /// <summary>
    /// Period for VWAP calculation (number of candles)
    /// Default: 20
    /// </summary>
    public int Period { get; set; } = 20;

    /// <summary>
    /// Percentage deviation below VWAP to trigger BUY signal
    /// Default: -2.0% (price is 2% below VWAP)
    /// </summary>
    public decimal BuyDeviationThreshold { get; set; } = -2.0m;

    /// <summary>
    /// Percentage deviation above VWAP to trigger SELL signal
    /// Default: 2.0% (price is 2% above VWAP)
    /// </summary>
    public decimal SellDeviationThreshold { get; set; } = 2.0m;

    /// <summary>
    /// Use volume confirmation to adjust confidence
    /// Default: true
    /// </summary>
    public bool UseVolumeConfirmation { get; set; } = true;
}
