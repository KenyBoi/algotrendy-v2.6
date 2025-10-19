namespace AlgoTrendy.Backtesting.Indicators;

/// <summary>
/// Exponential Moving Average (EMA)
/// </summary>
public static class EMA
{
    /// <summary>
    /// Calculate Exponential Moving Average
    /// </summary>
    /// <param name="data">Price data</param>
    /// <param name="period">Number of periods</param>
    /// <returns>List of EMA values</returns>
    public static List<decimal?> Calculate(List<decimal> data, int period)
    {
        if (period < 1)
            throw new ArgumentException("Period must be at least 1", nameof(period));

        var result = new List<decimal?>();
        var multiplier = 2m / (period + 1);
        decimal? ema = null;

        for (int i = 0; i < data.Count; i++)
        {
            if (i < period - 1)
            {
                result.Add(null);
            }
            else if (i == period - 1)
            {
                // First EMA is SMA
                var sum = 0m;
                for (int j = 0; j < period; j++)
                {
                    sum += data[i - j];
                }
                ema = sum / period;
                result.Add(ema);
            }
            else
            {
                // EMA = (Close - EMA_prev) × multiplier + EMA_prev
                ema = (data[i] - ema!.Value) * multiplier + ema.Value;
                result.Add(ema);
            }
        }

        return result;
    }
}
