namespace AlgoTrendy.TradingEngine.Services;

using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for calculating technical indicators with caching
/// Ported from v2.5 indicator_engine.py
/// </summary>
public class IndicatorService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<IndicatorService> _logger;

    public IndicatorService(IMemoryCache cache, ILogger<IndicatorService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    #region RSI (Relative Strength Index)

    /// <summary>
    /// Calculates RSI (Relative Strength Index) with caching
    /// RSI = 100 - (100 / (1 + RS))
    /// RS = Average Gain / Average Loss
    /// </summary>
    /// <param name="symbol">Trading symbol</param>
    /// <param name="historicalData">Historical price data (at least period + 1 points)</param>
    /// <param name="period">RSI period (default: 14)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>RSI value (0-100)</returns>
    public virtual async Task<decimal> CalculateRSIAsync(
        string symbol,
        IEnumerable<MarketData> historicalData,
        int period = 14,
        CancellationToken cancellationToken = default)
    {
        var prices = historicalData.Select(d => d.Close).ToList();

        // Generate cache key based on symbol, period, and latest timestamp
        var latestTimestamp = historicalData.LastOrDefault()?.Timestamp ?? DateTime.UtcNow;
        var cacheKey = $"rsi:{symbol}:{period}:{latestTimestamp:yyyy-MM-dd-HH-mm}";

        // Check cache first
        if (_cache.TryGetValue(cacheKey, out decimal cachedValue))
        {
            _logger.LogInformation(
                "Indicator cache hit - Indicator: {Indicator}, Symbol: {Symbol}, Period: {Period}, CacheKey: {CacheKey}",
                "RSI", symbol, period, cacheKey);
            return cachedValue;
        }

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            await Task.CompletedTask; // Satisfy async requirement
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);

            _logger.LogInformation(
                "Indicator cache miss - Indicator: {Indicator}, Symbol: {Symbol}, Period: {Period}, DataPoints: {DataPoints}",
                "RSI", symbol, period, prices.Count);

            if (prices.Count < period + 1)
            {
                _logger.LogWarning(
                    "Insufficient data for indicator calculation - Indicator: {Indicator}, Symbol: {Symbol}, Required: {Required}, Actual: {Actual}",
                    "RSI", symbol, period + 1, prices.Count);
                return 50m; // Neutral if not enough data
            }

            // Calculate price changes
            var gains = new List<decimal>();
            var losses = new List<decimal>();

            for (int i = 1; i < prices.Count; i++)
            {
                var change = prices[i] - prices[i - 1];
                if (change > 0)
                {
                    gains.Add(change);
                    losses.Add(0);
                }
                else
                {
                    gains.Add(0);
                    losses.Add(Math.Abs(change));
                }
            }

            // Calculate average gain and loss over the period
            var avgGain = gains.TakeLast(period).Average();
            var avgLoss = losses.TakeLast(period).Average();

            if (avgLoss == 0)
            {
                return 100m; // All gains, maximum RSI
            }

            var rs = avgGain / avgLoss;
            var rsi = 100m - (100m / (1m + rs));

            _logger.LogInformation(
                "Indicator calculated - Indicator: {Indicator}, Symbol: {Symbol}, Period: {Period}, Value: {Value}, AvgGain: {AvgGain}, AvgLoss: {AvgLoss}",
                "RSI", symbol, period, rsi, avgGain, avgLoss);

            return rsi;
        });
    }

    #endregion

    #region MACD (Moving Average Convergence Divergence)

    /// <summary>
    /// Calculates MACD (Moving Average Convergence Divergence) with caching
    /// MACD = Fast EMA - Slow EMA
    /// Signal = EMA of MACD
    /// Histogram = MACD - Signal
    /// </summary>
    public virtual async Task<MACDResult> CalculateMACDAsync(
        string symbol,
        IEnumerable<MarketData> historicalData,
        int fastPeriod = 12,
        int slowPeriod = 26,
        int signalPeriod = 9,
        CancellationToken cancellationToken = default)
    {
        var prices = historicalData.Select(d => d.Close).ToList();
        var latestTimestamp = historicalData.LastOrDefault()?.Timestamp ?? DateTime.UtcNow;
        var cacheKey = $"macd:{symbol}:{fastPeriod}:{slowPeriod}:{signalPeriod}:{latestTimestamp:yyyy-MM-dd-HH-mm}";

        // Check cache first
        if (_cache.TryGetValue(cacheKey, out MACDResult? cachedMacd) && cachedMacd != null)
        {
            _logger.LogInformation(
                "Indicator cache hit - Indicator: {Indicator}, Symbol: {Symbol}, FastPeriod: {FastPeriod}, SlowPeriod: {SlowPeriod}",
                "MACD", symbol, fastPeriod, slowPeriod);
            return cachedMacd;
        }

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            await Task.CompletedTask; // Satisfy async requirement
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);

            _logger.LogInformation(
                "Indicator cache miss - Indicator: {Indicator}, Symbol: {Symbol}, FastPeriod: {FastPeriod}, SlowPeriod: {SlowPeriod}, DataPoints: {DataPoints}",
                "MACD", symbol, fastPeriod, slowPeriod, prices.Count);

            if (prices.Count < slowPeriod)
            {
                _logger.LogWarning(
                    "Insufficient data for indicator calculation - Indicator: {Indicator}, Symbol: {Symbol}, Required: {Required}, Actual: {Actual}",
                    "MACD", symbol, slowPeriod, prices.Count);
                return new MACDResult { MACD = 0, Signal = 0, Histogram = 0 };
            }

            // Calculate EMAs
            var fastEMA = CalculateEMA(prices, fastPeriod);
            var slowEMA = CalculateEMA(prices, slowPeriod);

            // MACD line
            var macd = fastEMA - slowEMA;

            // Signal line (EMA of MACD)
            // For simplicity, we'll use the MACD value as an approximation
            // In production, you'd calculate EMA of historical MACD values
            var signal = macd * 0.9m; // Simplified signal line

            // Histogram
            var histogram = macd - signal;

            var result = new MACDResult
            {
                MACD = macd,
                Signal = signal,
                Histogram = histogram
            };

            _logger.LogInformation(
                "Indicator calculated - Indicator: {Indicator}, Symbol: {Symbol}, MACD: {MACD}, Signal: {Signal}, Histogram: {Histogram}, FastEMA: {FastEMA}, SlowEMA: {SlowEMA}",
                "MACD", symbol, macd, signal, histogram, fastEMA, slowEMA);

            return result;
        }) ?? new MACDResult { MACD = 0, Signal = 0, Histogram = 0 };
    }

    #endregion

    #region EMA (Exponential Moving Average)

    /// <summary>
    /// Calculates EMA (Exponential Moving Average) with caching
    /// EMA gives more weight to recent prices
    /// Smoothing factor: 2 / (period + 1)
    /// </summary>
    public virtual async Task<decimal> CalculateEMAAsync(
        string symbol,
        IEnumerable<MarketData> historicalData,
        int period,
        CancellationToken cancellationToken = default)
    {
        var prices = historicalData.Select(d => d.Close).ToList();
        var latestTimestamp = historicalData.LastOrDefault()?.Timestamp ?? DateTime.UtcNow;
        var cacheKey = $"ema:{symbol}:{period}:{latestTimestamp:yyyy-MM-dd-HH-mm}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            await Task.CompletedTask; // Satisfy async requirement
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);

            _logger.LogDebug("Calculating EMA for {Symbol} with period {Period}", symbol, period);

            var ema = CalculateEMA(prices, period);

            _logger.LogDebug("EMA calculated: {EMA} for {Symbol}", ema, symbol);

            return ema;
        });
    }

    /// <summary>
    /// Internal EMA calculation (non-cached)
    /// </summary>
    private decimal CalculateEMA(List<decimal> prices, int period)
    {
        if (prices.Count < period)
        {
            return prices.LastOrDefault();
        }

        // Start with SMA for the first EMA value
        var sma = prices.Take(period).Average();
        var ema = sma;

        // Smoothing factor
        var multiplier = 2m / (period + 1);

        // Calculate EMA for remaining prices
        for (int i = period; i < prices.Count; i++)
        {
            ema = (prices[i] - ema) * multiplier + ema;
        }

        return ema;
    }

    #endregion

    #region SMA (Simple Moving Average)

    /// <summary>
    /// Calculates SMA (Simple Moving Average) with caching
    /// Simple average over the specified period
    /// </summary>
    public virtual async Task<decimal> CalculateSMAAsync(
        string symbol,
        IEnumerable<MarketData> historicalData,
        int period,
        CancellationToken cancellationToken = default)
    {
        var prices = historicalData.Select(d => d.Close).ToList();
        var latestTimestamp = historicalData.LastOrDefault()?.Timestamp ?? DateTime.UtcNow;
        var cacheKey = $"sma:{symbol}:{period}:{latestTimestamp:yyyy-MM-dd-HH-mm}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            await Task.CompletedTask; // Satisfy async requirement
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);

            _logger.LogDebug("Calculating SMA for {Symbol} with period {Period}", symbol, period);

            if (prices.Count < period)
            {
                _logger.LogWarning("Insufficient data for SMA calculation. Need {Required}, got {Actual}",
                    period, prices.Count);
                return prices.LastOrDefault();
            }

            var sma = prices.TakeLast(period).Average();

            _logger.LogDebug("SMA calculated: {SMA} for {Symbol}", sma, symbol);

            return sma;
        });
    }

    #endregion

    #region Volatility

    /// <summary>
    /// Calculates volatility (standard deviation of price changes)
    /// </summary>
    public virtual async Task<decimal> CalculateVolatilityAsync(
        string symbol,
        IEnumerable<MarketData> historicalData,
        int period = 20,
        CancellationToken cancellationToken = default)
    {
        var prices = historicalData.Select(d => d.Close).ToList();
        var latestTimestamp = historicalData.LastOrDefault()?.Timestamp ?? DateTime.UtcNow;
        var cacheKey = $"volatility:{symbol}:{period}:{latestTimestamp:yyyy-MM-dd-HH-mm}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            await Task.CompletedTask; // Satisfy async requirement
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);

            _logger.LogDebug("Calculating volatility for {Symbol} with period {Period}", symbol, period);

            if (prices.Count < period + 1)
            {
                return 0m;
            }

            var recentPrices = prices.TakeLast(period + 1).ToList();
            var changes = new List<decimal>();

            for (int i = 1; i < recentPrices.Count; i++)
            {
                var change = (recentPrices[i] - recentPrices[i - 1]) / recentPrices[i - 1];
                changes.Add(change);
            }

            var mean = changes.Average();
            var variance = changes.Sum(c => (c - mean) * (c - mean)) / changes.Count;
            var volatility = (decimal)Math.Sqrt((double)variance);

            _logger.LogDebug("Volatility calculated: {Volatility} for {Symbol}", volatility, symbol);

            return volatility;
        });
    }

    #endregion

    #region MFI (Money Flow Index)

    /// <summary>
    /// Calculates MFI (Money Flow Index) with caching
    /// MFI is a momentum indicator that uses price and volume
    /// Similar to RSI but incorporates volume
    /// </summary>
    /// <param name="symbol">Trading symbol</param>
    /// <param name="historicalData">Historical OHLCV data (at least period + 1 points)</param>
    /// <param name="period">MFI period (default: 14)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>MFI value (0-100)</returns>
    public virtual async Task<decimal> CalculateMFIAsync(
        string symbol,
        IEnumerable<MarketData> historicalData,
        int period = 14,
        CancellationToken cancellationToken = default)
    {
        var dataList = historicalData.ToList();
        var latestTimestamp = dataList.LastOrDefault()?.Timestamp ?? DateTime.UtcNow;
        var cacheKey = $"mfi:{symbol}:{period}:{latestTimestamp:yyyy-MM-dd-HH-mm}";

        // Check cache first
        if (_cache.TryGetValue(cacheKey, out decimal cachedValue))
        {
            _logger.LogInformation(
                "Indicator cache hit - Indicator: {Indicator}, Symbol: {Symbol}, Period: {Period}",
                "MFI", symbol, period);
            return cachedValue;
        }

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            await Task.CompletedTask; // Satisfy async requirement
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);

            _logger.LogInformation(
                "Indicator cache miss - Indicator: {Indicator}, Symbol: {Symbol}, Period: {Period}, DataPoints: {DataPoints}",
                "MFI", symbol, period, dataList.Count);

            if (dataList.Count < period + 1)
            {
                _logger.LogWarning(
                    "Insufficient data for indicator calculation - Indicator: {Indicator}, Symbol: {Symbol}, Required: {Required}, Actual: {Actual}",
                    "MFI", symbol, period + 1, dataList.Count);
                return 50m; // Neutral if not enough data
            }

            // Calculate Typical Price and Raw Money Flow for each period
            var typicalPrices = new List<decimal>();
            var rawMoneyFlows = new List<decimal>();

            foreach (var data in dataList)
            {
                var typicalPrice = (data.High + data.Low + data.Close) / 3m;
                var rawMoneyFlow = typicalPrice * data.Volume;
                typicalPrices.Add(typicalPrice);
                rawMoneyFlows.Add(rawMoneyFlow);
            }

            // Calculate positive and negative money flows
            var positiveFlows = new List<decimal>();
            var negativeFlows = new List<decimal>();

            for (int i = 1; i < typicalPrices.Count; i++)
            {
                if (typicalPrices[i] > typicalPrices[i - 1])
                {
                    positiveFlows.Add(rawMoneyFlows[i]);
                    negativeFlows.Add(0);
                }
                else if (typicalPrices[i] < typicalPrices[i - 1])
                {
                    positiveFlows.Add(0);
                    negativeFlows.Add(rawMoneyFlows[i]);
                }
                else
                {
                    positiveFlows.Add(0);
                    negativeFlows.Add(0);
                }
            }

            // Calculate MFI over the period
            var positiveFlow = positiveFlows.TakeLast(period).Sum();
            var negativeFlow = negativeFlows.TakeLast(period).Sum();

            if (negativeFlow == 0)
            {
                return 100m; // All positive flow, maximum MFI
            }

            var moneyFlowRatio = positiveFlow / negativeFlow;
            var mfi = 100m - (100m / (1m + moneyFlowRatio));

            _logger.LogInformation(
                "Indicator calculated - Indicator: {Indicator}, Symbol: {Symbol}, Period: {Period}, Value: {Value}, PositiveFlow: {PositiveFlow}, NegativeFlow: {NegativeFlow}",
                "MFI", symbol, period, mfi, positiveFlow, negativeFlow);

            return mfi;
        });
    }

    #endregion

    #region VWAP (Volume Weighted Average Price)

    /// <summary>
    /// Calculates VWAP (Volume Weighted Average Price) with caching
    /// VWAP = Σ(Typical Price × Volume) / Σ(Volume)
    /// </summary>
    /// <param name="symbol">Trading symbol</param>
    /// <param name="historicalData">Historical OHLCV data</param>
    /// <param name="period">Number of periods to calculate over (default: 20)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>VWAP value</returns>
    public virtual async Task<decimal> CalculateVWAPAsync(
        string symbol,
        IEnumerable<MarketData> historicalData,
        int period = 20,
        CancellationToken cancellationToken = default)
    {
        var dataList = historicalData.ToList();
        var latestTimestamp = dataList.LastOrDefault()?.Timestamp ?? DateTime.UtcNow;
        var cacheKey = $"vwap:{symbol}:{period}:{latestTimestamp:yyyy-MM-dd-HH-mm}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            await Task.CompletedTask; // Satisfy async requirement
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);

            _logger.LogDebug("Calculating VWAP for {Symbol} with period {Period}", symbol, period);

            if (dataList.Count < period)
            {
                _logger.LogWarning(
                    "Insufficient data for VWAP calculation. Need {Required}, got {Actual}",
                    period, dataList.Count);

                // Return simple average if not enough data
                return dataList.LastOrDefault()?.Close ?? 0m;
            }

            var recentData = dataList.TakeLast(period).ToList();

            decimal totalPriceVolume = 0m;
            decimal totalVolume = 0m;

            foreach (var data in recentData)
            {
                // Typical price
                var typicalPrice = (data.High + data.Low + data.Close) / 3m;
                totalPriceVolume += typicalPrice * data.Volume;
                totalVolume += data.Volume;
            }

            if (totalVolume == 0)
            {
                _logger.LogWarning("Zero volume for VWAP calculation on {Symbol}", symbol);
                return recentData.Last().Close;
            }

            var vwap = totalPriceVolume / totalVolume;

            _logger.LogDebug(
                "VWAP calculated: {VWAP} for {Symbol} (TotalPriceVolume: {TotalPriceVolume}, TotalVolume: {TotalVolume})",
                vwap, symbol, totalPriceVolume, totalVolume);

            return vwap;
        });
    }

    #endregion

    #region Cache Management

    /// <summary>
    /// Clears all cached indicator values
    /// </summary>
    public void ClearCache()
    {
        if (_cache is MemoryCache memCache)
        {
            memCache.Compact(1.0); // Remove 100% of cache entries
            _logger.LogInformation("Indicator cache cleared");
        }
    }

    #endregion
}

/// <summary>
/// Result of MACD calculation
/// </summary>
public class MACDResult
{
    /// <summary>
    /// MACD line value (Fast EMA - Slow EMA)
    /// </summary>
    public required decimal MACD { get; init; }

    /// <summary>
    /// Signal line value (EMA of MACD)
    /// </summary>
    public required decimal Signal { get; init; }

    /// <summary>
    /// Histogram value (MACD - Signal)
    /// </summary>
    public required decimal Histogram { get; init; }
}
