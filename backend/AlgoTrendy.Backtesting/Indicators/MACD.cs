namespace AlgoTrendy.Backtesting.Indicators;

/// <summary>
/// Moving Average Convergence Divergence (MACD)
/// </summary>
public static class MACD
{
    public class MACDResult
    {
        public List<decimal?> MACDLine { get; set; } = new();
        public List<decimal?> SignalLine { get; set; } = new();
        public List<decimal?> Histogram { get; set; } = new();
    }

    /// <summary>
    /// Calculate MACD
    /// </summary>
    /// <param name="data">Price data</param>
    /// <param name="fastPeriod">Fast EMA period (typically 12)</param>
    /// <param name="slowPeriod">Slow EMA period (typically 26)</param>
    /// <param name="signalPeriod">Signal line period (typically 9)</param>
    /// <returns>MACD result with MACD line, signal line, and histogram</returns>
    public static MACDResult Calculate(List<decimal> data, int fastPeriod = 12, int slowPeriod = 26, int signalPeriod = 9)
    {
        var emaFast = EMA.Calculate(data, fastPeriod);
        var emaSlow = EMA.Calculate(data, slowPeriod);

        // MACD Line = Fast EMA - Slow EMA
        var macdLine = new List<decimal?>();
        for (int i = 0; i < data.Count; i++)
        {
            if (emaFast[i].HasValue && emaSlow[i].HasValue)
            {
                macdLine.Add(emaFast[i]!.Value - emaSlow[i]!.Value);
            }
            else
            {
                macdLine.Add(null);
            }
        }

        // Signal Line = EMA of MACD Line
        var signalLine = new List<decimal?>();
        var validMacdValues = macdLine.Where(v => v.HasValue).Select(v => v!.Value).ToList();
        var signalEma = EMA.Calculate(validMacdValues, signalPeriod);

        int signalIndex = 0;
        for (int i = 0; i < macdLine.Count; i++)
        {
            if (macdLine[i].HasValue)
            {
                signalLine.Add(signalEma[signalIndex]);
                signalIndex++;
            }
            else
            {
                signalLine.Add(null);
            }
        }

        // Histogram = MACD Line - Signal Line
        var histogram = new List<decimal?>();
        for (int i = 0; i < macdLine.Count; i++)
        {
            if (macdLine[i].HasValue && signalLine[i].HasValue)
            {
                histogram.Add(macdLine[i]!.Value - signalLine[i]!.Value);
            }
            else
            {
                histogram.Add(null);
            }
        }

        return new MACDResult
        {
            MACDLine = macdLine,
            SignalLine = signalLine,
            Histogram = histogram
        };
    }
}
