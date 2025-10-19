namespace AlgoTrendy.Backtesting.Indicators;

/// <summary>
/// OHLCV (Open, High, Low, Close, Volume) candle data
/// </summary>
public class Candle
{
    /// <summary>Timestamp (UTC)</summary>
    public required DateTime Timestamp { get; set; }

    /// <summary>Opening price</summary>
    public required decimal Open { get; set; }

    /// <summary>High price</summary>
    public required decimal High { get; set; }

    /// <summary>Low price</summary>
    public required decimal Low { get; set; }

    /// <summary>Closing price</summary>
    public required decimal Close { get; set; }

    /// <summary>Volume</summary>
    public decimal Volume { get; set; }
}

/// <summary>
/// Technical indicators calculations for backtesting
/// </summary>
public static class TechnicalIndicators
{
    /// <summary>
    /// Calculate Simple Moving Average (SMA)
    /// </summary>
    /// <param name="values">Series of values (typically closing prices)</param>
    /// <param name="period">Number of periods for the average</param>
    /// <returns>Array of SMA values, early values are NaN</returns>
    public static decimal?[] CalculateSMA(decimal[] values, int period)
    {
        var result = new decimal?[values.Length];

        for (int i = 0; i < values.Length; i++)
        {
            if (i < period - 1)
            {
                result[i] = null;
                continue;
            }

            decimal sum = 0;
            for (int j = i - period + 1; j <= i; j++)
            {
                sum += values[j];
            }

            result[i] = sum / period;
        }

        return result;
    }

    /// <summary>
    /// Calculate Exponential Moving Average (EMA)
    /// </summary>
    /// <param name="values">Series of values</param>
    /// <param name="period">Number of periods</param>
    /// <returns>Array of EMA values</returns>
    public static decimal?[] CalculateEMA(decimal[] values, int period)
    {
        var result = new decimal?[values.Length];
        decimal multiplier = 2m / (period + 1);

        for (int i = 0; i < values.Length; i++)
        {
            if (i < period - 1)
            {
                result[i] = null;
                continue;
            }

            if (i == period - 1)
            {
                // First EMA is SMA
                decimal sum = 0;
                for (int j = 0; j < period; j++)
                {
                    sum += values[j];
                }
                result[i] = sum / period;
            }
            else
            {
                // Subsequent EMAs use the multiplier
                result[i] = (values[i] - result[i - 1]) * multiplier + result[i - 1];
            }
        }

        return result;
    }

    /// <summary>
    /// Calculate Relative Strength Index (RSI)
    /// </summary>
    /// <param name="values">Series of closing prices</param>
    /// <param name="period">RSI period (typically 14)</param>
    /// <returns>Array of RSI values (0-100)</returns>
    public static decimal?[] CalculateRSI(decimal[] values, int period = 14)
    {
        var result = new decimal?[values.Length];

        if (values.Length < period + 1)
            return result;

        decimal avgGain = 0;
        decimal avgLoss = 0;

        // Calculate initial average gains and losses
        for (int i = 1; i <= period; i++)
        {
            decimal change = values[i] - values[i - 1];
            if (change > 0)
                avgGain += change;
            else
                avgLoss += -change;
        }

        avgGain /= period;
        avgLoss /= period;

        for (int i = period; i < values.Length; i++)
        {
            if (i == period)
            {
                result[i] = 100 - (100 / (1 + (avgGain / avgLoss)));
            }
            else
            {
                decimal change = values[i] - values[i - 1];
                decimal gain = change > 0 ? change : 0;
                decimal loss = change < 0 ? -change : 0;

                avgGain = (avgGain * (period - 1) + gain) / period;
                avgLoss = (avgLoss * (period - 1) + loss) / period;

                if (avgLoss == 0)
                {
                    result[i] = avgGain == 0 ? 50 : 100;
                }
                else
                {
                    decimal rs = avgGain / avgLoss;
                    result[i] = 100 - (100 / (1 + rs));
                }
            }

            if (i < period)
                result[i] = null;
        }

        return result;
    }

    /// <summary>
    /// Calculate MACD (Moving Average Convergence Divergence)
    /// </summary>
    /// <param name="values">Series of closing prices</param>
    /// <param name="fastPeriod">Fast EMA period (typically 12)</param>
    /// <param name="slowPeriod">Slow EMA period (typically 26)</param>
    /// <param name="signalPeriod">Signal line period (typically 9)</param>
    /// <returns>Tuple of (MACD line, Signal line, Histogram)</returns>
    public static (decimal?[] MACDLine, decimal?[] SignalLine, decimal?[] Histogram) CalculateMACD(
        decimal[] values,
        int fastPeriod = 12,
        int slowPeriod = 26,
        int signalPeriod = 9)
    {
        var fastEMA = CalculateEMA(values, fastPeriod);
        var slowEMA = CalculateEMA(values, slowPeriod);

        var macdLine = new decimal?[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            if (fastEMA[i].HasValue && slowEMA[i].HasValue)
                macdLine[i] = fastEMA[i] - slowEMA[i];
        }

        var signalLine = CalculateEMA(macdLine.Where(x => x.HasValue).Select(x => x!.Value).ToArray(), signalPeriod);

        var histogram = new decimal?[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            if (macdLine[i].HasValue && signalLine[i].HasValue)
                histogram[i] = macdLine[i] - signalLine[i];
        }

        return (macdLine, signalLine, histogram);
    }

    /// <summary>
    /// Calculate Bollinger Bands
    /// </summary>
    /// <param name="values">Series of closing prices</param>
    /// <param name="period">Number of periods (typically 20)</param>
    /// <param name="standardDeviations">Number of standard deviations (typically 2)</param>
    /// <returns>Tuple of (Upper Band, Middle Band/SMA, Lower Band)</returns>
    public static (decimal?[] UpperBand, decimal?[] MiddleBand, decimal?[] LowerBand) CalculateBollingerBands(
        decimal[] values,
        int period = 20,
        decimal standardDeviations = 2)
    {
        var middleBand = CalculateSMA(values, period);
        var upperBand = new decimal?[values.Length];
        var lowerBand = new decimal?[values.Length];

        for (int i = period - 1; i < values.Length; i++)
        {
            // Calculate standard deviation
            decimal sum = 0;
            for (int j = i - period + 1; j <= i; j++)
            {
                decimal diff = values[j] - middleBand[i]!.Value;
                sum += diff * diff;
            }

            decimal stdDev = (decimal)Math.Sqrt((double)(sum / period));
            upperBand[i] = middleBand[i] + (stdDev * standardDeviations);
            lowerBand[i] = middleBand[i] - (stdDev * standardDeviations);
        }

        return (upperBand, middleBand, lowerBand);
    }

    /// <summary>
    /// Calculate Average True Range (ATR)
    /// </summary>
    /// <param name="candles">OHLCV candle data</param>
    /// <param name="period">ATR period (typically 14)</param>
    /// <returns>Array of ATR values</returns>
    public static decimal?[] CalculateATR(Candle[] candles, int period = 14)
    {
        var result = new decimal?[candles.Length];

        if (candles.Length < 2)
            return result;

        var trueRanges = new decimal[candles.Length];

        for (int i = 1; i < candles.Length; i++)
        {
            decimal highLow = candles[i].High - candles[i].Low;
            decimal highClose = Math.Abs(candles[i].High - candles[i - 1].Close);
            decimal lowClose = Math.Abs(candles[i].Low - candles[i - 1].Close);

            trueRanges[i] = Math.Max(highLow, Math.Max(highClose, lowClose));
        }

        // Calculate ATR using EMA
        var atr = CalculateEMA(trueRanges, period);
        return atr;
    }

    /// <summary>
    /// Calculate Stochastic Oscillator
    /// </summary>
    /// <param name="candles">OHLCV candle data</param>
    /// <param name="kPeriod">%K period (typically 14)</param>
    /// <param name="dPeriod">%D period (typically 3)</param>
    /// <returns>Tuple of (%K line, %D line)</returns>
    public static (decimal?[] KLine, decimal?[] DLine) CalculateStochastic(
        Candle[] candles,
        int kPeriod = 14,
        int dPeriod = 3)
    {
        var kLine = new decimal?[candles.Length];
        var closes = candles.Select(c => c.Close).ToArray();

        for (int i = kPeriod - 1; i < candles.Length; i++)
        {
            decimal highest = candles.Skip(i - kPeriod + 1).Take(kPeriod).Max(c => c.High);
            decimal lowest = candles.Skip(i - kPeriod + 1).Take(kPeriod).Min(c => c.Low);

            if (highest - lowest == 0)
            {
                kLine[i] = 50;
            }
            else
            {
                kLine[i] = ((closes[i] - lowest) / (highest - lowest)) * 100;
            }
        }

        // Calculate %D as SMA of %K
        var kValues = kLine.Where(x => x.HasValue).Select(x => x!.Value).ToArray();
        var dSMA = CalculateSMA(kValues, dPeriod);

        var dLine = new decimal?[candles.Length];
        int offset = 0;
        for (int i = 0; i < candles.Length; i++)
        {
            if (kLine[i].HasValue)
            {
                if (offset < dSMA.Length)
                    dLine[i] = dSMA[offset];
                offset++;
            }
        }

        return (kLine, dLine);
    }

    /// <summary>
    /// Calculate On-Balance Volume (OBV)
    /// </summary>
    /// <param name="candles">OHLCV candle data</param>
    /// <returns>Array of OBV values</returns>
    public static decimal[] CalculateOBV(Candle[] candles)
    {
        var obv = new decimal[candles.Length];

        if (candles.Length == 0)
            return obv;

        obv[0] = candles[0].Volume;

        for (int i = 1; i < candles.Length; i++)
        {
            if (candles[i].Close > candles[i - 1].Close)
                obv[i] = obv[i - 1] + candles[i].Volume;
            else if (candles[i].Close < candles[i - 1].Close)
                obv[i] = obv[i - 1] - candles[i].Volume;
            else
                obv[i] = obv[i - 1];
        }

        return obv;
    }
}

/// <summary>
/// Indicator metadata and configuration
/// </summary>
public static class IndicatorMetadata
{
    /// <summary>
    /// Get available indicators and their metadata
    /// </summary>
    public static Dictionary<string, Dictionary<string, object>> GetAvailableIndicators() => new()
    {
        {
            "sma",
            new()
            {
                { "name", "Simple Moving Average" },
                { "description", "Average price over N periods" },
                { "category", "trend" },
                {
                    "params",
                    new Dictionary<string, object>
                    {
                        {
                            "period",
                            new { type = "int", @default = 20, min = 2, max = 200, description = "Number of periods" }
                        }
                    }
                }
            }
        },
        {
            "ema",
            new()
            {
                { "name", "Exponential Moving Average" },
                { "description", "Exponentially weighted moving average" },
                { "category", "trend" },
                {
                    "params",
                    new Dictionary<string, object>
                    {
                        {
                            "period",
                            new { type = "int", @default = 12, min = 2, max = 200, description = "Number of periods" }
                        }
                    }
                }
            }
        },
        {
            "rsi",
            new()
            {
                { "name", "Relative Strength Index" },
                { "description", "Momentum oscillator (0-100)" },
                { "category", "momentum" },
                {
                    "params",
                    new Dictionary<string, object>
                    {
                        {
                            "period",
                            new { type = "int", @default = 14, min = 2, max = 50, description = "RSI period" }
                        }
                    }
                }
            }
        },
        {
            "macd",
            new()
            {
                { "name", "MACD" },
                { "description", "Moving Average Convergence Divergence" },
                { "category", "momentum" }
            }
        },
        {
            "bollinger",
            new()
            {
                { "name", "Bollinger Bands" },
                { "description", "Volatility bands around moving average" },
                { "category", "volatility" }
            }
        },
        {
            "atr",
            new()
            {
                { "name", "Average True Range" },
                { "description", "Volatility indicator" },
                { "category", "volatility" }
            }
        },
        {
            "stochastic",
            new()
            {
                { "name", "Stochastic Oscillator" },
                { "description", "Momentum indicator (0-100)" },
                { "category", "momentum" }
            }
        },
        {
            "obv",
            new()
            {
                { "name", "On-Balance Volume" },
                { "description", "Volume-based momentum indicator" },
                { "category", "volume" }
            }
        }
    };
}
