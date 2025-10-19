namespace AlgoTrendy.Backtesting.Indicators;

/// <summary>
/// Relative Strength Index (RSI)
/// </summary>
public static class RSI
{
    /// <summary>
    /// Calculate Relative Strength Index
    /// </summary>
    /// <param name="data">Price data</param>
    /// <param name="period">RSI period (typically 14)</param>
    /// <returns>List of RSI values (0-100)</returns>
    public static List<decimal?> Calculate(List<decimal> data, int period = 14)
    {
        if (period < 1)
            throw new ArgumentException("Period must be at least 1", nameof(period));

        var result = new List<decimal?>();

        if (data.Count < period + 1)
        {
            return Enumerable.Repeat((decimal?)null, data.Count).ToList();
        }

        var gains = new List<decimal>();
        var losses = new List<decimal>();

        // Calculate price changes
        for (int i = 1; i < data.Count; i++)
        {
            var change = data[i] - data[i - 1];
            gains.Add(change > 0 ? change : 0);
            losses.Add(change < 0 ? -change : 0);
        }

        // First value is null (no previous price)
        result.Add(null);

        decimal? avgGain = null;
        decimal? avgLoss = null;

        for (int i = 0; i < gains.Count; i++)
        {
            if (i < period - 1)
            {
                result.Add(null);
            }
            else if (i == period - 1)
            {
                // First average is SMA
                avgGain = gains.Take(period).Average();
                avgLoss = losses.Take(period).Average();

                var rs = avgLoss == 0 ? 100m : avgGain.Value / avgLoss.Value;
                var rsi = 100m - (100m / (1m + rs));
                result.Add(rsi);
            }
            else
            {
                // Subsequent averages use Wilder's smoothing
                avgGain = ((avgGain!.Value * (period - 1)) + gains[i]) / period;
                avgLoss = ((avgLoss!.Value * (period - 1)) + losses[i]) / period;

                var rs = avgLoss == 0 ? 100m : avgGain.Value / avgLoss.Value;
                var rsi = 100m - (100m / (1m + rs));
                result.Add(rsi);
            }
        }

        return result;
    }
}
