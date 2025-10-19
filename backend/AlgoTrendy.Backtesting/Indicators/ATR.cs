namespace AlgoTrendy.Backtesting.Indicators;

/// <summary>
/// Average True Range (ATR)
/// </summary>
public static class ATR
{
    /// <summary>
    /// Calculate Average True Range
    /// </summary>
    /// <param name="high">High prices</param>
    /// <param name="low">Low prices</param>
    /// <param name="close">Close prices</param>
    /// <param name="period">ATR period (typically 14)</param>
    /// <returns>List of ATR values</returns>
    public static List<decimal?> Calculate(List<decimal> high, List<decimal> low, List<decimal> close, int period = 14)
    {
        if (high.Count != low.Count || high.Count != close.Count)
            throw new ArgumentException("High, low, and close arrays must have the same length");

        var result = new List<decimal?>();
        var trueRanges = new List<decimal>();

        // First value has no previous close
        result.Add(null);

        // Calculate True Range for each period
        for (int i = 1; i < high.Count; i++)
        {
            var highLow = high[i] - low[i];
            var highClose = Math.Abs(high[i] - close[i - 1]);
            var lowClose = Math.Abs(low[i] - close[i - 1]);

            var tr = Math.Max(highLow, Math.Max(highClose, lowClose));
            trueRanges.Add(tr);

            if (i < period)
            {
                result.Add(null);
            }
            else if (i == period)
            {
                // First ATR is SMA of True Range
                var atr = trueRanges.Take(period).Average();
                result.Add(atr);
            }
            else
            {
                // Subsequent ATR values use Wilder's smoothing
                var prevATR = result[i - 1]!.Value;
                var atr = ((prevATR * (period - 1)) + tr) / period;
                result.Add(atr);
            }
        }

        return result;
    }
}
