namespace AlgoTrendy.Backtesting.Indicators;

/// <summary>
/// Stochastic Oscillator
/// </summary>
public static class Stochastic
{
    public class StochasticResult
    {
        public List<decimal?> K { get; set; } = new();
        public List<decimal?> D { get; set; } = new();
    }

    /// <summary>
    /// Calculate Stochastic Oscillator
    /// </summary>
    /// <param name="high">High prices</param>
    /// <param name="low">Low prices</param>
    /// <param name="close">Close prices</param>
    /// <param name="kPeriod">%K period (typically 14)</param>
    /// <param name="dPeriod">%D period (typically 3)</param>
    /// <returns>Stochastic result with %K and %D</returns>
    public static StochasticResult Calculate(List<decimal> high, List<decimal> low, List<decimal> close, int kPeriod = 14, int dPeriod = 3)
    {
        if (high.Count != low.Count || high.Count != close.Count)
            throw new ArgumentException("High, low, and close arrays must have the same length");

        var kValues = new List<decimal?>();

        for (int i = 0; i < close.Count; i++)
        {
            if (i < kPeriod - 1)
            {
                kValues.Add(null);
            }
            else
            {
                // Find highest high and lowest low in the period
                var highestHigh = high.Skip(i - kPeriod + 1).Take(kPeriod).Max();
                var lowestLow = low.Skip(i - kPeriod + 1).Take(kPeriod).Min();

                // %K = 100 × (Close - LowestLow) / (HighestHigh - LowestLow)
                var range = highestHigh - lowestLow;
                var k = range == 0 ? 50m : 100m * ((close[i] - lowestLow) / range);
                kValues.Add(k);
            }
        }

        // %D = SMA of %K
        var validKValues = kValues.Where(v => v.HasValue).Select(v => v!.Value).ToList();
        var dSma = SMA.Calculate(validKValues, dPeriod);

        var dValues = new List<decimal?>();
        int dIndex = 0;
        for (int i = 0; i < kValues.Count; i++)
        {
            if (kValues[i].HasValue)
            {
                dValues.Add(dSma[dIndex]);
                dIndex++;
            }
            else
            {
                dValues.Add(null);
            }
        }

        return new StochasticResult
        {
            K = kValues,
            D = dValues
        };
    }
}
