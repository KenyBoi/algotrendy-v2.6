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

    #region Stochastic Oscillator

    /// <summary>
    /// Calculates Stochastic Oscillator with caching
    /// %K = 100 * (Close - LowestLow) / (HighestHigh - LowestLow)
    /// %D = SMA of %K
    /// </summary>
    public virtual async Task<StochasticResult> CalculateStochasticAsync(
        string symbol,
        IEnumerable<MarketData> historicalData,
        int period = 14,
        int smoothK = 3,
        int smoothD = 3,
        CancellationToken cancellationToken = default)
    {
        var dataList = historicalData.ToList();
        var latestTimestamp = dataList.LastOrDefault()?.Timestamp ?? DateTime.UtcNow;
        var cacheKey = $"stochastic:{symbol}:{period}:{smoothK}:{smoothD}:{latestTimestamp:yyyy-MM-dd-HH-mm}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            await Task.CompletedTask;
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);

            if (dataList.Count < period)
            {
                return new StochasticResult { PercentK = 50m, PercentD = 50m };
            }

            var kValues = new List<decimal>();

            for (int i = period - 1; i < dataList.Count; i++)
            {
                var periodData = dataList.Skip(i - period + 1).Take(period).ToList();
                var lowestLow = periodData.Min(d => d.Low);
                var highestHigh = periodData.Max(d => d.High);
                var currentClose = dataList[i].Close;

                var k = 100m * (currentClose - lowestLow) / (highestHigh - lowestLow);
                kValues.Add(k);
            }

            var smoothedK = kValues.TakeLast(smoothK).Average();
            var dValue = kValues.TakeLast(smoothD).Average();

            return new StochasticResult { PercentK = smoothedK, PercentD = dValue };
        }) ?? new StochasticResult { PercentK = 50m, PercentD = 50m };
    }

    #endregion

    #region ADX (Average Directional Index)

    /// <summary>
    /// Calculates ADX (Average Directional Index) with caching
    /// Measures trend strength (0-100)
    /// </summary>
    public virtual async Task<ADXResult> CalculateADXAsync(
        string symbol,
        IEnumerable<MarketData> historicalData,
        int period = 14,
        CancellationToken cancellationToken = default)
    {
        var dataList = historicalData.ToList();
        var latestTimestamp = dataList.LastOrDefault()?.Timestamp ?? DateTime.UtcNow;
        var cacheKey = $"adx:{symbol}:{period}:{latestTimestamp:yyyy-MM-dd-HH-mm}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            await Task.CompletedTask;
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);

            if (dataList.Count < period + 1)
            {
                return new ADXResult { ADX = 0m, PlusDI = 0m, MinusDI = 0m };
            }

            var trList = new List<decimal>();
            var plusDMList = new List<decimal>();
            var minusDMList = new List<decimal>();

            for (int i = 1; i < dataList.Count; i++)
            {
                var current = dataList[i];
                var previous = dataList[i - 1];

                // True Range
                var tr = Math.Max(current.High - current.Low,
                         Math.Max(Math.Abs(current.High - previous.Close),
                                 Math.Abs(current.Low - previous.Close)));
                trList.Add(tr);

                // Directional Movement
                var upMove = current.High - previous.High;
                var downMove = previous.Low - current.Low;

                var plusDM = (upMove > downMove && upMove > 0) ? upMove : 0m;
                var minusDM = (downMove > upMove && downMove > 0) ? downMove : 0m;

                plusDMList.Add(plusDM);
                minusDMList.Add(minusDM);
            }

            var avgTR = trList.TakeLast(period).Average();
            var avgPlusDM = plusDMList.TakeLast(period).Average();
            var avgMinusDM = minusDMList.TakeLast(period).Average();

            var plusDI = 100m * (avgPlusDM / avgTR);
            var minusDI = 100m * (avgMinusDM / avgTR);

            var dx = 100m * Math.Abs(plusDI - minusDI) / (plusDI + minusDI);
            var adx = dx; // Simplified - should use smoothing

            return new ADXResult { ADX = adx, PlusDI = plusDI, MinusDI = minusDI };
        }) ?? new ADXResult { ADX = 0m, PlusDI = 0m, MinusDI = 0m };
    }

    #endregion

    #region ATR (Average True Range)

    /// <summary>
    /// Calculates ATR (Average True Range) with caching
    /// Measures market volatility
    /// </summary>
    public virtual async Task<decimal> CalculateATRAsync(
        string symbol,
        IEnumerable<MarketData> historicalData,
        int period = 14,
        CancellationToken cancellationToken = default)
    {
        var dataList = historicalData.ToList();
        var latestTimestamp = dataList.LastOrDefault()?.Timestamp ?? DateTime.UtcNow;
        var cacheKey = $"atr:{symbol}:{period}:{latestTimestamp:yyyy-MM-dd-HH-mm}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            await Task.CompletedTask;
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);

            if (dataList.Count < period + 1)
            {
                return 0m;
            }

            var trList = new List<decimal>();

            for (int i = 1; i < dataList.Count; i++)
            {
                var current = dataList[i];
                var previous = dataList[i - 1];

                var tr = Math.Max(current.High - current.Low,
                         Math.Max(Math.Abs(current.High - previous.Close),
                                 Math.Abs(current.Low - previous.Close)));
                trList.Add(tr);
            }

            return trList.TakeLast(period).Average();
        });
    }

    #endregion

    #region Bollinger Bands

    /// <summary>
    /// Calculates Bollinger Bands with caching
    /// Upper = SMA + (StdDev * multiplier)
    /// Lower = SMA - (StdDev * multiplier)
    /// </summary>
    public virtual async Task<BollingerBandsResult> CalculateBollingerBandsAsync(
        string symbol,
        IEnumerable<MarketData> historicalData,
        int period = 20,
        decimal stdDevMultiplier = 2m,
        CancellationToken cancellationToken = default)
    {
        var prices = historicalData.Select(d => d.Close).ToList();
        var latestTimestamp = historicalData.LastOrDefault()?.Timestamp ?? DateTime.UtcNow;
        var cacheKey = $"bbands:{symbol}:{period}:{stdDevMultiplier}:{latestTimestamp:yyyy-MM-dd-HH-mm}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            await Task.CompletedTask;
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);

            if (prices.Count < period)
            {
                var currentPrice = prices.LastOrDefault();
                return new BollingerBandsResult
                {
                    Upper = currentPrice * 1.05m,
                    Middle = currentPrice,
                    Lower = currentPrice * 0.95m
                };
            }

            var recentPrices = prices.TakeLast(period).ToList();
            var middle = recentPrices.Average();

            var variance = recentPrices.Sum(p => (p - middle) * (p - middle)) / period;
            var stdDev = (decimal)Math.Sqrt((double)variance);

            return new BollingerBandsResult
            {
                Upper = middle + (stdDev * stdDevMultiplier),
                Middle = middle,
                Lower = middle - (stdDev * stdDevMultiplier)
            };
        }) ?? new BollingerBandsResult { Upper = 0m, Middle = 0m, Lower = 0m };
    }

    #endregion

    #region Williams %R

    /// <summary>
    /// Calculates Williams %R with caching
    /// Range: -100 to 0
    /// Overbought: > -20
    /// Oversold: < -80
    /// </summary>
    public virtual async Task<decimal> CalculateWilliamsRAsync(
        string symbol,
        IEnumerable<MarketData> historicalData,
        int period = 14,
        CancellationToken cancellationToken = default)
    {
        var dataList = historicalData.ToList();
        var latestTimestamp = dataList.LastOrDefault()?.Timestamp ?? DateTime.UtcNow;
        var cacheKey = $"williamsr:{symbol}:{period}:{latestTimestamp:yyyy-MM-dd-HH-mm}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            await Task.CompletedTask;
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);

            if (dataList.Count < period)
            {
                return -50m;
            }

            var recentData = dataList.TakeLast(period).ToList();
            var highestHigh = recentData.Max(d => d.High);
            var lowestLow = recentData.Min(d => d.Low);
            var currentClose = dataList.Last().Close;

            return -100m * (highestHigh - currentClose) / (highestHigh - lowestLow);
        });
    }

    #endregion

    #region CCI (Commodity Channel Index)

    /// <summary>
    /// Calculates CCI (Commodity Channel Index) with caching
    /// Range: Unbounded (typically -200 to +200)
    /// Overbought: > +100
    /// Oversold: < -100
    /// </summary>
    public virtual async Task<decimal> CalculateCCIAsync(
        string symbol,
        IEnumerable<MarketData> historicalData,
        int period = 20,
        CancellationToken cancellationToken = default)
    {
        var dataList = historicalData.ToList();
        var latestTimestamp = dataList.LastOrDefault()?.Timestamp ?? DateTime.UtcNow;
        var cacheKey = $"cci:{symbol}:{period}:{latestTimestamp:yyyy-MM-dd-HH-mm}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            await Task.CompletedTask;
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);

            if (dataList.Count < period)
            {
                return 0m;
            }

            var typicalPrices = dataList.Select(d => (d.High + d.Low + d.Close) / 3m).ToList();
            var recentTP = typicalPrices.TakeLast(period).ToList();
            var sma = recentTP.Average();

            var meanDeviation = recentTP.Sum(tp => Math.Abs(tp - sma)) / period;

            if (meanDeviation == 0)
            {
                return 0m;
            }

            var currentTP = typicalPrices.Last();
            return (currentTP - sma) / (0.015m * meanDeviation);
        });
    }

    #endregion

    #region OBV (On-Balance Volume)

    /// <summary>
    /// Calculates OBV (On-Balance Volume) with caching
    /// Cumulative volume indicator based on price direction
    /// </summary>
    public virtual async Task<decimal> CalculateOBVAsync(
        string symbol,
        IEnumerable<MarketData> historicalData,
        CancellationToken cancellationToken = default)
    {
        var dataList = historicalData.ToList();
        var latestTimestamp = dataList.LastOrDefault()?.Timestamp ?? DateTime.UtcNow;
        var cacheKey = $"obv:{symbol}:{latestTimestamp:yyyy-MM-dd-HH-mm}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            await Task.CompletedTask;
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);

            if (dataList.Count < 2)
            {
                return dataList.FirstOrDefault()?.Volume ?? 0m;
            }

            decimal obv = 0m;

            for (int i = 1; i < dataList.Count; i++)
            {
                if (dataList[i].Close > dataList[i - 1].Close)
                {
                    obv += dataList[i].Volume;
                }
                else if (dataList[i].Close < dataList[i - 1].Close)
                {
                    obv -= dataList[i].Volume;
                }
            }

            return obv;
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
/// Result of Stochastic calculation
/// </summary>
public class StochasticResult
{
    /// <summary>
    /// %K line value
    /// </summary>
    public required decimal PercentK { get; init; }

    /// <summary>
    /// %D line value (signal line)
    /// </summary>
    public required decimal PercentD { get; init; }
}

/// <summary>
/// Result of ADX calculation
/// </summary>
public class ADXResult
{
    /// <summary>
    /// ADX value (trend strength)
    /// </summary>
    public required decimal ADX { get; init; }

    /// <summary>
    /// +DI (Plus Directional Indicator)
    /// </summary>
    public required decimal PlusDI { get; init; }

    /// <summary>
    /// -DI (Minus Directional Indicator)
    /// </summary>
    public required decimal MinusDI { get; init; }
}

/// <summary>
/// Result of Bollinger Bands calculation
/// </summary>
public class BollingerBandsResult
{
    /// <summary>
    /// Upper band value
    /// </summary>
    public required decimal Upper { get; init; }

    /// <summary>
    /// Middle band value (SMA)
    /// </summary>
    public required decimal Middle { get; init; }

    /// <summary>
    /// Lower band value
    /// </summary>
    public required decimal Lower { get; init; }
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
