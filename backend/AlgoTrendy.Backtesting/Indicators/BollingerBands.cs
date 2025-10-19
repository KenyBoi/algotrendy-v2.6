namespace AlgoTrendy.Backtesting.Indicators;

/// <summary>
/// Bollinger Bands
/// </summary>
public static class BollingerBands
{
    public class BollingerBandsResult
    {
        public List<decimal?> Upper { get; set; } = new();
        public List<decimal?> Middle { get; set; } = new();
        public List<decimal?> Lower { get; set; } = new();
    }

    /// <summary>
    /// Calculate Bollinger Bands
    /// </summary>
    /// <param name="data">Price data</param>
    /// <param name="period">Period for moving average (typically 20)</param>
    /// <param name="stdDev">Number of standard deviations (typically 2)</param>
    /// <returns>Bollinger Bands result with upper, middle, and lower bands</returns>
    public static BollingerBandsResult Calculate(List<decimal> data, int period = 20, decimal stdDev = 2.0m)
    {
        var middle = SMA.Calculate(data, period);
        var upper = new List<decimal?>();
        var lower = new List<decimal?>();

        for (int i = 0; i < data.Count; i++)
        {
            if (i < period - 1)
            {
                upper.Add(null);
                lower.Add(null);
            }
            else
            {
                // Calculate standard deviation for this window
                var window = new List<decimal>();
                for (int j = 0; j < period; j++)
                {
                    window.Add(data[i - j]);
                }

                var std = CalculateStandardDeviation(window);
                var middleValue = middle[i]!.Value;

                upper.Add(middleValue + (stdDev * std));
                lower.Add(middleValue - (stdDev * std));
            }
        }

        return new BollingerBandsResult
        {
            Upper = upper,
            Middle = middle,
            Lower = lower
        };
    }

    private static decimal CalculateStandardDeviation(List<decimal> values)
    {
        var avg = values.Average();
        var sumOfSquares = values.Sum(v => (v - avg) * (v - avg));
        return (decimal)Math.Sqrt((double)(sumOfSquares / values.Count));
    }
}
