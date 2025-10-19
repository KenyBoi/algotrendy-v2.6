using AlgoTrendy.Backtesting.Models;

namespace AlgoTrendy.Backtesting.Indicators;

/// <summary>
/// Orchestrates calculation of all technical indicators for a candle dataset
/// </summary>
public class IndicatorCalculator
{
    /// <summary>
    /// Calculate all enabled indicators for the given candles
    /// </summary>
    /// <param name="candles">Historical OHLCV candles</param>
    /// <param name="config">Backtest configuration with enabled indicators</param>
    /// <returns>Dictionary of indicator name to calculated values</returns>
    public Dictionary<string, decimal?[]> CalculateAll(Candle[] candles, BacktestConfig config)
    {
        var results = new Dictionary<string, decimal?[]>();

        if (candles == null || candles.Length == 0)
            return results;

        // Extract price arrays
        var closes = candles.Select(c => c.Close).ToArray();
        var highs = candles.Select(c => c.High).ToArray();
        var lows = candles.Select(c => c.Low).ToArray();
        var volumes = candles.Select(c => c.Volume).ToArray();

        // Calculate indicators based on configuration
        foreach (var indicator in config.Indicators)
        {
            if (!indicator.Value.Enabled)
                continue;

            var name = indicator.Key.ToLower();
            var parameters = indicator.Value.Parameters;

            switch (name)
            {
                case "sma":
                    CalculateSMA(results, closes, parameters);
                    break;

                case "ema":
                    CalculateEMA(results, closes, parameters);
                    break;

                case "rsi":
                    CalculateRSI(results, closes, parameters);
                    break;

                case "macd":
                    CalculateMACD(results, closes, parameters);
                    break;

                case "bollinger":
                case "bollingerbands":
                    CalculateBollingerBands(results, closes, parameters);
                    break;

                case "atr":
                    CalculateATR(results, highs, lows, closes, parameters);
                    break;

                case "stochastic":
                case "stoch":
                    CalculateStochastic(results, highs, lows, closes, parameters);
                    break;

                case "volume":
                    CalculateVolume(results, volumes, parameters);
                    break;

                default:
                    // Skip unknown indicators
                    break;
            }
        }

        return results;
    }

    /// <summary>
    /// Calculate Simple Moving Average with multiple periods
    /// </summary>
    private void CalculateSMA(Dictionary<string, decimal?[]> results, decimal[] closes, Dictionary<string, object> parameters)
    {
        // Default periods: 20, 50, 200
        var periods = GetPeriods(parameters, new[] { 20, 50, 200 });

        foreach (var period in periods)
        {
            var sma = SMA.Calculate(closes.ToList(), period);
            results[$"sma_{period}"] = sma.ToArray();
        }
    }

    /// <summary>
    /// Calculate Exponential Moving Average with multiple periods
    /// </summary>
    private void CalculateEMA(Dictionary<string, decimal?[]> results, decimal[] closes, Dictionary<string, object> parameters)
    {
        // Default periods: 12, 26, 50
        var periods = GetPeriods(parameters, new[] { 12, 26, 50 });

        foreach (var period in periods)
        {
            var ema = EMA.Calculate(closes.ToList(), period);
            results[$"ema_{period}"] = ema.ToArray();
        }
    }

    /// <summary>
    /// Calculate Relative Strength Index
    /// </summary>
    private void CalculateRSI(Dictionary<string, decimal?[]> results, decimal[] closes, Dictionary<string, object> parameters)
    {
        var period = GetPeriod(parameters, "period", 14);
        var rsi = RSI.Calculate(closes.ToList(), period);
        results["rsi"] = rsi.ToArray();
        results[$"rsi_{period}"] = rsi.ToArray();
    }

    /// <summary>
    /// Calculate MACD (Moving Average Convergence Divergence)
    /// </summary>
    private void CalculateMACD(Dictionary<string, decimal?[]> results, decimal[] closes, Dictionary<string, object> parameters)
    {
        var fastPeriod = GetPeriod(parameters, "fast", 12);
        var slowPeriod = GetPeriod(parameters, "slow", 26);
        var signalPeriod = GetPeriod(parameters, "signal", 9);

        var macdResult = MACD.Calculate(closes.ToList(), fastPeriod, slowPeriod, signalPeriod);

        results["macd_line"] = macdResult.MACDLine.ToArray();
        results["macd_signal"] = macdResult.SignalLine.ToArray();
        results["macd_histogram"] = macdResult.Histogram.ToArray();
    }

    /// <summary>
    /// Calculate Bollinger Bands
    /// </summary>
    private void CalculateBollingerBands(Dictionary<string, decimal?[]> results, decimal[] closes, Dictionary<string, object> parameters)
    {
        var period = GetPeriod(parameters, "period", 20);
        var stdDev = GetDecimalParameter(parameters, "stddev", 2.0m);

        var bbResult = BollingerBands.Calculate(closes.ToList(), period, stdDev);

        results["bb_upper"] = bbResult.Upper.ToArray();
        results["bb_middle"] = bbResult.Middle.ToArray();
        results["bb_lower"] = bbResult.Lower.ToArray();
    }

    /// <summary>
    /// Calculate Average True Range
    /// </summary>
    private void CalculateATR(Dictionary<string, decimal?[]> results, decimal[] highs, decimal[] lows, decimal[] closes, Dictionary<string, object> parameters)
    {
        var period = GetPeriod(parameters, "period", 14);
        var atr = ATR.Calculate(highs.ToList(), lows.ToList(), closes.ToList(), period);
        results["atr"] = atr.ToArray();
        results[$"atr_{period}"] = atr.ToArray();
    }

    /// <summary>
    /// Calculate Stochastic Oscillator
    /// </summary>
    private void CalculateStochastic(Dictionary<string, decimal?[]> results, decimal[] highs, decimal[] lows, decimal[] closes, Dictionary<string, object> parameters)
    {
        var kPeriod = GetPeriod(parameters, "k", 14);
        var dPeriod = GetPeriod(parameters, "d", 3);

        var stochResult = Stochastic.Calculate(highs.ToList(), lows.ToList(), closes.ToList(), kPeriod, dPeriod);

        results["stoch_k"] = stochResult.K.ToArray();
        results["stoch_d"] = stochResult.D.ToArray();
    }

    /// <summary>
    /// Calculate Volume indicators
    /// </summary>
    private void CalculateVolume(Dictionary<string, decimal?[]> results, decimal[] volumes, Dictionary<string, object> parameters)
    {
        // Raw volume
        results["volume"] = volumes.Select(v => (decimal?)v).ToArray();

        // Volume moving average
        var maPeriod = GetPeriod(parameters, "ma_period", 20);
        var volumeSMA = SMA.Calculate(volumes.ToList(), maPeriod);
        results["volume_ma"] = volumeSMA.ToArray();
        results[$"volume_ma_{maPeriod}"] = volumeSMA.ToArray();
    }

    /// <summary>
    /// Get indicator period from parameters
    /// </summary>
    private int GetPeriod(Dictionary<string, object> parameters, string key, int defaultValue)
    {
        if (parameters.TryGetValue(key, out var value))
        {
            if (value is int intValue)
                return intValue;
            if (value is string strValue && int.TryParse(strValue, out var parsed))
                return parsed;
        }
        return defaultValue;
    }

    /// <summary>
    /// Get decimal parameter from dictionary
    /// </summary>
    private decimal GetDecimalParameter(Dictionary<string, object> parameters, string key, decimal defaultValue)
    {
        if (parameters.TryGetValue(key, out var value))
        {
            if (value is decimal decValue)
                return decValue;
            if (value is double dblValue)
                return (decimal)dblValue;
            if (value is string strValue && decimal.TryParse(strValue, out var parsed))
                return parsed;
        }
        return defaultValue;
    }

    /// <summary>
    /// Get multiple periods from parameters
    /// </summary>
    private int[] GetPeriods(Dictionary<string, object> parameters, int[] defaultPeriods)
    {
        if (parameters.TryGetValue("periods", out var value))
        {
            if (value is int[] intArray)
                return intArray;
            if (value is List<int> intList)
                return intList.ToArray();
            if (value is string strValue)
            {
                // Parse comma-separated periods: "20,50,200"
                var parts = strValue.Split(',');
                var periods = parts
                    .Select(p => int.TryParse(p.Trim(), out var period) ? period : 0)
                    .Where(p => p > 0)
                    .ToArray();

                if (periods.Length > 0)
                    return periods;
            }
        }

        // Check for individual period parameter
        if (parameters.TryGetValue("period", out var periodValue))
        {
            var period = GetPeriod(parameters, "period", 0);
            if (period > 0)
                return new[] { period };
        }

        return defaultPeriods;
    }

    /// <summary>
    /// Get indicator value at specific index
    /// </summary>
    public decimal? GetIndicatorValue(Dictionary<string, decimal?[]> indicators, string name, int index)
    {
        if (indicators.TryGetValue(name, out var values) && index >= 0 && index < values.Length)
        {
            return values[index];
        }
        return null;
    }

    /// <summary>
    /// Check if all required indicators are ready at given index
    /// </summary>
    public bool AreIndicatorsReady(Dictionary<string, decimal?[]> indicators, string[] requiredIndicators, int index)
    {
        foreach (var name in requiredIndicators)
        {
            var value = GetIndicatorValue(indicators, name, index);
            if (!value.HasValue)
                return false;
        }
        return true;
    }
}
