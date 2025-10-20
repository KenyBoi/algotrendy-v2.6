namespace AlgoTrendy.TradingEngine.Strategies;

using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using AlgoTrendy.TradingEngine.Services;
using Microsoft.Extensions.Logging;

/// <summary>
/// MACD (Moving Average Convergence Divergence) trading strategy
///
/// Strategy Logic:
/// - Buy when MACD line crosses above signal line (bullish crossover)
/// - Sell when MACD line crosses below signal line (bearish crossover)
/// - Hold otherwise
/// - Confidence based on histogram magnitude (stronger divergence = higher confidence)
///
/// MACD Components:
/// - MACD Line = 12-period EMA - 26-period EMA
/// - Signal Line = 9-period EMA of MACD Line
/// - Histogram = MACD Line - Signal Line
/// </summary>
public class MACDStrategy : IStrategy
{
    private readonly MACDStrategyConfig _config;
    private readonly IndicatorService _indicatorService;
    private readonly ILogger<MACDStrategy> _logger;

    public string StrategyName => "MACD";

    public MACDStrategy(
        MACDStrategyConfig config,
        IndicatorService indicatorService,
        ILogger<MACDStrategy> logger)
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
            _logger.LogDebug("Analyzing {Symbol} with MACD strategy", currentData.Symbol);

            // Calculate MACD
            var allData = historicalData.Append(currentData).ToList();
            var macdResult = await _indicatorService.CalculateMACDAsync(
                currentData.Symbol,
                allData,
                _config.FastPeriod,
                _config.SlowPeriod,
                _config.SignalPeriod,
                cancellationToken);

            var price = currentData.Close;
            var macd = macdResult.MACD;
            var signal = macdResult.Signal;
            var histogram = macdResult.Histogram;

            // Base signal
            var action = SignalAction.Hold;
            var confidence = 0.4m;
            var reason = $"MACD: {macd:F4}, Signal: {signal:F4}, Histogram: {histogram:F4}";

            // MACD-based decision
            // Bullish: MACD > Signal (histogram > 0)
            // Bearish: MACD < Signal (histogram < 0)

            if (histogram > _config.BuyThreshold)
            {
                action = SignalAction.Buy;

                // Higher confidence for stronger bullish divergence
                // Normalize histogram to confidence (cap at 0.95)
                confidence = Math.Min(Math.Abs(histogram) / (price * 0.01m), 0.95m);
                confidence = Math.Max(confidence, 0.5m); // Minimum 0.5 confidence

                reason = $"MACD: {macd:F4} > Signal: {signal:F4}, Histogram: {histogram:F4} (BULLISH CROSSOVER)";
                _logger.LogInformation("BUY signal generated for {Symbol}: {Reason}", currentData.Symbol, reason);
            }
            else if (histogram < _config.SellThreshold)
            {
                action = SignalAction.Sell;

                // Higher confidence for stronger bearish divergence
                confidence = Math.Min(Math.Abs(histogram) / (price * 0.01m), 0.95m);
                confidence = Math.Max(confidence, 0.5m); // Minimum 0.5 confidence

                reason = $"MACD: {macd:F4} < Signal: {signal:F4}, Histogram: {histogram:F4} (BEARISH CROSSOVER)";
                _logger.LogInformation("SELL signal generated for {Symbol}: {Reason}", currentData.Symbol, reason);
            }
            else
            {
                // MACD in neutral zone or weak signal
                reason = $"MACD: {macd:F4}, Signal: {signal:F4}, Histogram: {histogram:F4} (NEUTRAL)";
                _logger.LogDebug("HOLD signal for {Symbol}: No clear crossover", currentData.Symbol);
            }

            // Volume confirmation - reduce confidence for low volume
            if (currentData.Volume < _config.MinVolumeThreshold)
            {
                confidence *= 0.7m;
                reason += $" [Low Volume: {currentData.Volume:N0}]";
                _logger.LogDebug("Confidence reduced due to low volume for {Symbol}", currentData.Symbol);
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
            _logger.LogError(ex, "Error analyzing {Symbol} with MACD strategy", currentData.Symbol);

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
/// Configuration for MACD strategy
/// </summary>
public class MACDStrategyConfig
{
    /// <summary>
    /// Fast EMA period for MACD calculation
    /// Default: 12
    /// </summary>
    public int FastPeriod { get; set; } = 12;

    /// <summary>
    /// Slow EMA period for MACD calculation
    /// Default: 26
    /// </summary>
    public int SlowPeriod { get; set; } = 26;

    /// <summary>
    /// Signal line period (EMA of MACD)
    /// Default: 9
    /// </summary>
    public int SignalPeriod { get; set; } = 9;

    /// <summary>
    /// Histogram threshold for BUY signal (MACD > Signal)
    /// Default: 0.0001 (slightly positive)
    /// </summary>
    public decimal BuyThreshold { get; set; } = 0.0001m;

    /// <summary>
    /// Histogram threshold for SELL signal (MACD < Signal)
    /// Default: -0.0001 (slightly negative)
    /// </summary>
    public decimal SellThreshold { get; set; } = -0.0001m;

    /// <summary>
    /// Minimum volume required for high confidence
    /// Default: 100,000
    /// </summary>
    public decimal MinVolumeThreshold { get; set; } = 100000m;
}
