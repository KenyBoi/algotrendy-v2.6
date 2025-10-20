namespace AlgoTrendy.TradingEngine.Strategies;

using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using AlgoTrendy.TradingEngine.Services;
using Microsoft.Extensions.Logging;

/// <summary>
/// RSI (Relative Strength Index) trading strategy with ML enhancement
/// Ported from v2.5 strategy_resolver.py RSIStrategy
///
/// Strategy Logic:
/// - Buy if RSI < oversold threshold (default: 30)
/// - Sell if RSI > overbought threshold (default: 70)
/// - Hold if RSI is in neutral zone
/// - Confidence based on how extreme the RSI value is
/// - ML predictions used to enhance confidence and filter signals
/// </summary>
public class RSIStrategy : IStrategy
{
    private readonly RSIStrategyConfig _config;
    private readonly IndicatorService _indicatorService;
    private readonly IMLPredictionService? _mlService;
    private readonly MLFeatureService? _mlFeatureService;
    private readonly ILogger<RSIStrategy> _logger;

    public string StrategyName => "RSI";

    public RSIStrategy(
        RSIStrategyConfig config,
        IndicatorService indicatorService,
        ILogger<RSIStrategy> logger,
        IMLPredictionService? mlService = null,
        MLFeatureService? mlFeatureService = null)
    {
        _config = config;
        _indicatorService = indicatorService;
        _logger = logger;
        _mlService = mlService;
        _mlFeatureService = mlFeatureService;

        if (_mlService != null && _mlFeatureService != null)
        {
            _logger.LogInformation("RSI Strategy initialized with ML enhancement enabled");
        }
        else
        {
            _logger.LogInformation("RSI Strategy initialized without ML enhancement");
        }
    }

    public async Task<TradingSignal> AnalyzeAsync(
        MarketData currentData,
        IEnumerable<MarketData> historicalData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Analyzing {Symbol} with RSI strategy", currentData.Symbol);

            // Calculate RSI
            var allData = historicalData.Append(currentData).ToList();
            var rsi = await _indicatorService.CalculateRSIAsync(
                currentData.Symbol,
                allData,
                _config.Period,
                cancellationToken);

            var price = currentData.Close;

            // Base signal
            var action = SignalAction.Hold;
            var confidence = 0.4m;
            var reason = $"RSI: {rsi:F1}";

            // RSI-based decision
            if (rsi < _config.OversoldThreshold)
            {
                action = SignalAction.Buy;
                // Higher confidence for deeper oversold
                // Example: RSI=20, threshold=30 -> confidence = (30-20)/30 = 0.33
                confidence = (_config.OversoldThreshold - rsi) / _config.OversoldThreshold;
                confidence = Math.Min(confidence, 0.9m); // Cap at 0.9
                reason = $"RSI: {rsi:F1} (OVERSOLD)";

                _logger.LogInformation("BUY signal generated for {Symbol}: {Reason}", currentData.Symbol, reason);
            }
            else if (rsi > _config.OverboughtThreshold)
            {
                action = SignalAction.Sell;
                // Higher confidence for deeper overbought
                // Example: RSI=80, threshold=70 -> confidence = (80-70)/(100-70) = 0.33
                confidence = (rsi - _config.OverboughtThreshold) / (100m - _config.OverboughtThreshold);
                confidence = Math.Min(confidence, 0.9m); // Cap at 0.9
                reason = $"RSI: {rsi:F1} (OVERBOUGHT)";

                _logger.LogInformation("SELL signal generated for {Symbol}: {Reason}", currentData.Symbol, reason);
            }
            else
            {
                // RSI in neutral zone
                reason = $"RSI: {rsi:F1} (NEUTRAL)";
                _logger.LogDebug("HOLD signal for {Symbol}: RSI in neutral zone", currentData.Symbol);
            }

            // ML Enhancement - if ML services are available
            if (_mlService != null && _mlFeatureService != null && _config.UseMLEnhancement)
            {
                try
                {
                    _logger.LogDebug("Calculating ML features for {Symbol}", currentData.Symbol);
                    var features = await _mlFeatureService.CalculateFeaturesAsync(
                        currentData.Symbol,
                        allData,
                        cancellationToken);

                    var mlPrediction = await _mlService.PredictReversalAsync(features, cancellationToken);

                    if (mlPrediction != null && string.IsNullOrEmpty(mlPrediction.Error))
                    {
                        _logger.LogInformation(
                            "ML Prediction for {Symbol}: IsReversal={IsReversal}, Confidence={Confidence:F3}",
                            currentData.Symbol, mlPrediction.IsReversal, mlPrediction.Confidence);

                        // Enhance confidence based on ML prediction
                        if (mlPrediction.IsReversal)
                        {
                            // ML agrees with reversal - boost confidence
                            var mlConfidence = (decimal)mlPrediction.Confidence;
                            confidence = (confidence + mlConfidence) / 2m; // Average of RSI and ML confidence
                            confidence = Math.Min(confidence, 0.95m); // Cap at 0.95
                            reason += $" + ML CONFIRMED ({mlPrediction.Confidence:F2})";
                        }
                        else
                        {
                            // ML disagrees - reduce confidence
                            confidence *= 0.6m; // Reduce confidence by 40%
                            reason += $" - ML UNCERTAIN ({mlPrediction.Confidence:F2})";

                            // If confidence drops too low, change to HOLD
                            if (confidence < 0.3m)
                            {
                                action = SignalAction.Hold;
                                reason = $"RSI: {rsi:F1} - ML OVERRIDE (low confidence)";
                                _logger.LogWarning(
                                    "ML override: Changed signal to HOLD for {Symbol} due to low confidence",
                                    currentData.Symbol);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning(
                            "ML prediction failed for {Symbol}: {Error}",
                            currentData.Symbol, mlPrediction?.Error ?? "Unknown error");
                        reason += " (ML unavailable)";
                    }
                }
                catch (Exception mlEx)
                {
                    _logger.LogError(mlEx, "Error during ML enhancement for {Symbol}", currentData.Symbol);
                    reason += " (ML error)";
                    // Continue with RSI-only signal
                }
            }

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
            _logger.LogError(ex, "Error analyzing {Symbol} with RSI strategy", currentData.Symbol);

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
/// Configuration for RSI strategy
/// </summary>
public class RSIStrategyConfig
{
    /// <summary>
    /// RSI calculation period
    /// Default: 14
    /// </summary>
    public int Period { get; set; } = 14;

    /// <summary>
    /// RSI threshold for oversold condition (BUY signal)
    /// Default: 30
    /// </summary>
    public decimal OversoldThreshold { get; set; } = 30m;

    /// <summary>
    /// RSI threshold for overbought condition (SELL signal)
    /// Default: 70
    /// </summary>
    public decimal OverboughtThreshold { get; set; } = 70m;

    /// <summary>
    /// Enable ML enhancement for signal generation
    /// Default: true
    /// </summary>
    public bool UseMLEnhancement { get; set; } = true;
}
