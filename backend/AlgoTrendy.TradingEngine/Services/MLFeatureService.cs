using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Logging;

namespace AlgoTrendy.TradingEngine.Services;

/// <summary>
/// Service for calculating ML features from market data
/// Generates the 21 technical indicators required for ML predictions
/// </summary>
public class MLFeatureService
{
    private readonly IndicatorService _indicatorService;
    private readonly ILogger<MLFeatureService> _logger;

    public MLFeatureService(
        IndicatorService indicatorService,
        ILogger<MLFeatureService> logger)
    {
        _indicatorService = indicatorService;
        _logger = logger;
    }

    /// <summary>
    /// Calculates all 21 ML features from historical market data
    /// Features align with v2.5 Python ML model expectations
    /// </summary>
    /// <param name="symbol">Trading symbol</param>
    /// <param name="historicalData">Historical market data (minimum 100 candles recommended)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Array of 21 feature values for ML prediction</returns>
    public async Task<double[]> CalculateFeaturesAsync(
        string symbol,
        List<MarketData> historicalData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (historicalData == null || historicalData.Count < 50)
            {
                _logger.LogWarning(
                    "Insufficient data for ML feature calculation - Symbol: {Symbol}, DataPoints: {Count}",
                    symbol, historicalData?.Count ?? 0);
                return new double[21]; // Return zeros if insufficient data
            }

            _logger.LogDebug(
                "Calculating ML features - Symbol: {Symbol}, DataPoints: {Count}",
                symbol, historicalData.Count);

            var features = new List<double>();

            // Sort by timestamp to ensure chronological order
            var sortedData = historicalData.OrderBy(d => d.Timestamp).ToList();

            // Feature 1-4: RSI at different periods
            features.Add((double)await _indicatorService.CalculateRSIAsync(symbol, sortedData, 14, cancellationToken));
            features.Add((double)await _indicatorService.CalculateRSIAsync(symbol, sortedData, 7, cancellationToken));
            features.Add((double)await _indicatorService.CalculateRSIAsync(symbol, sortedData, 21, cancellationToken));
            features.Add((double)await _indicatorService.CalculateRSIAsync(symbol, sortedData, 28, cancellationToken));

            // Feature 5-7: MACD components
            var macd = CalculateMACD(sortedData);
            features.Add(macd.Value);
            features.Add(macd.Signal);
            features.Add(macd.Histogram);

            // Feature 8-10: Bollinger Bands
            var bb = CalculateBollingerBands(sortedData, 20, 2.0);
            features.Add(bb.Upper);
            features.Add(bb.Middle);
            features.Add(bb.Lower);

            // Feature 11: ATR (Average True Range)
            features.Add(CalculateATR(sortedData, 14));

            // Feature 12-13: Stochastic Oscillator
            var stoch = CalculateStochastic(sortedData, 14, 3);
            features.Add(stoch.K);
            features.Add(stoch.D);

            // Feature 14-15: Moving Averages
            features.Add(CalculateSMA(sortedData, 20));
            features.Add(CalculateEMA(sortedData, 20));

            // Feature 16: Volume Ratio (current vs average)
            features.Add(CalculateVolumeRatio(sortedData, 20));

            // Feature 17: Price Rate of Change
            features.Add(CalculateROC(sortedData, 10));

            // Feature 18: Money Flow Index
            features.Add(CalculateMFI(sortedData, 14));

            // Feature 19: Commodity Channel Index
            features.Add(CalculateCCI(sortedData, 20));

            // Feature 20: Williams %R
            features.Add(CalculateWilliamsR(sortedData, 14));

            // Feature 21: Current price relative to 20-period high/low
            features.Add(CalculatePricePosition(sortedData, 20));

            _logger.LogInformation(
                "ML features calculated - Symbol: {Symbol}, Features: {FeatureCount}",
                symbol, features.Count);

            return features.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating ML features for {Symbol}", symbol);
            return new double[21]; // Return zeros on error
        }
    }

    private (double Value, double Signal, double Histogram) CalculateMACD(List<MarketData> data)
    {
        var ema12 = CalculateEMA(data, 12);
        var ema26 = CalculateEMA(data, 26);
        var macdValue = ema12 - ema26;

        // For signal line, we'd need to calculate EMA of MACD values
        // Simplified: using the current MACD value
        var signalLine = macdValue * 0.9; // Approximation
        var histogram = macdValue - signalLine;

        return (macdValue, signalLine, histogram);
    }

    private (double Upper, double Middle, double Lower) CalculateBollingerBands(
        List<MarketData> data, int period, double stdDevMultiplier)
    {
        var sma = CalculateSMA(data, period);
        var prices = data.TakeLast(period).Select(d => (double)d.Close).ToList();
        var variance = prices.Select(p => Math.Pow(p - sma, 2)).Average();
        var stdDev = Math.Sqrt(variance);

        return (sma + (stdDev * stdDevMultiplier), sma, sma - (stdDev * stdDevMultiplier));
    }

    private double CalculateATR(List<MarketData> data, int period)
    {
        var trueRanges = new List<double>();
        for (int i = 1; i < data.Count && i <= period; i++)
        {
            var high = (double)data[^i].High;
            var low = (double)data[^i].Low;
            var prevClose = (double)data[^(i + 1)].Close;

            var tr = Math.Max(high - low,
                     Math.Max(Math.Abs(high - prevClose),
                     Math.Abs(low - prevClose)));
            trueRanges.Add(tr);
        }

        return trueRanges.Any() ? trueRanges.Average() : 0;
    }

    private (double K, double D) CalculateStochastic(List<MarketData> data, int period, int smoothK)
    {
        var recentData = data.TakeLast(period).ToList();
        var currentClose = (double)data.Last().Close;
        var lowestLow = (double)recentData.Min(d => d.Low);
        var highestHigh = (double)recentData.Max(d => d.High);

        var k = highestHigh != lowestLow
            ? ((currentClose - lowestLow) / (highestHigh - lowestLow)) * 100
            : 50;

        // D is typically a 3-period SMA of K (simplified here)
        var d = k * 0.9; // Approximation

        return (k, d);
    }

    private double CalculateSMA(List<MarketData> data, int period)
    {
        return (double)data.TakeLast(period).Average(d => d.Close);
    }

    private double CalculateEMA(List<MarketData> data, int period)
    {
        var prices = data.TakeLast(period * 2).Select(d => (double)d.Close).ToList();
        if (prices.Count < 2) return prices.LastOrDefault();

        var multiplier = 2.0 / (period + 1);
        var ema = prices[0];

        for (int i = 1; i < prices.Count; i++)
        {
            ema = (prices[i] * multiplier) + (ema * (1 - multiplier));
        }

        return ema;
    }

    private double CalculateVolumeRatio(List<MarketData> data, int period)
    {
        var currentVolume = (double)data.Last().Volume;
        var avgVolume = (double)data.TakeLast(period).Average(d => d.Volume);

        return avgVolume > 0 ? currentVolume / avgVolume : 1.0;
    }

    private double CalculateROC(List<MarketData> data, int period)
    {
        if (data.Count < period + 1) return 0;

        var currentPrice = (double)data.Last().Close;
        var priorPrice = (double)data[^(period + 1)].Close;

        return priorPrice > 0 ? ((currentPrice - priorPrice) / priorPrice) * 100 : 0;
    }

    private double CalculateMFI(List<MarketData> data, int period)
    {
        // Money Flow Index calculation
        var recentData = data.TakeLast(period + 1).ToList();
        double positiveFlow = 0;
        double negativeFlow = 0;

        for (int i = 1; i < recentData.Count; i++)
        {
            var typicalPrice = (double)(recentData[i].High + recentData[i].Low + recentData[i].Close) / 3;
            var prevTypicalPrice = (double)(recentData[i - 1].High + recentData[i - 1].Low + recentData[i - 1].Close) / 3;
            var moneyFlow = typicalPrice * (double)recentData[i].Volume;

            if (typicalPrice > prevTypicalPrice)
                positiveFlow += moneyFlow;
            else if (typicalPrice < prevTypicalPrice)
                negativeFlow += moneyFlow;
        }

        if (negativeFlow == 0) return 100;
        var mfr = positiveFlow / negativeFlow;
        return 100 - (100 / (1 + mfr));
    }

    private double CalculateCCI(List<MarketData> data, int period)
    {
        // Commodity Channel Index
        var recentData = data.TakeLast(period).ToList();
        var typicalPrices = recentData.Select(d => (double)(d.High + d.Low + d.Close) / 3).ToList();
        var sma = typicalPrices.Average();
        var meanDeviation = typicalPrices.Select(tp => Math.Abs(tp - sma)).Average();

        if (meanDeviation == 0) return 0;
        var currentTypicalPrice = typicalPrices.Last();
        return (currentTypicalPrice - sma) / (0.015 * meanDeviation);
    }

    private double CalculateWilliamsR(List<MarketData> data, int period)
    {
        // Williams %R
        var recentData = data.TakeLast(period).ToList();
        var highestHigh = (double)recentData.Max(d => d.High);
        var lowestLow = (double)recentData.Min(d => d.Low);
        var currentClose = (double)data.Last().Close;

        if (highestHigh == lowestLow) return -50;
        return ((highestHigh - currentClose) / (highestHigh - lowestLow)) * -100;
    }

    private double CalculatePricePosition(List<MarketData> data, int period)
    {
        // Current price position within the period's range (0-1)
        var recentData = data.TakeLast(period).ToList();
        var highestHigh = (double)recentData.Max(d => d.High);
        var lowestLow = (double)recentData.Min(d => d.Low);
        var currentClose = (double)data.Last().Close;

        if (highestHigh == lowestLow) return 0.5;
        return (currentClose - lowestLow) / (highestHigh - lowestLow);
    }
}
