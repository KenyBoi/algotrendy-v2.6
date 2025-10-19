namespace AlgoTrendy.Backtesting.Indicators;

/// <summary>
/// Simple Moving Average (SMA)
/// </summary>
public static class SMA
{
    /// <summary>
    /// Calculate Simple Moving Average
    /// </summary>
    /// <param name="data">Price data</param>
    /// <param name="period">Number of periods</param>
    /// <returns>List of SMA values (null for insufficient data)</returns>
    public static List<decimal?> Calculate(List<decimal> data, int period)
    {
        if (period < 1)
            throw new ArgumentException("Period must be at least 1", nameof(period));

        var result = new List<decimal?>();

        for (int i = 0; i < data.Count; i++)
        {
            if (i < period - 1)
            {
                result.Add(null);
            }
            else
            {
                var sum = 0m;
                for (int j = 0; j < period; j++)
                {
                    sum += data[i - j];
                }
                result.Add(sum / period);
            }
        }

        return result;
    }
}
