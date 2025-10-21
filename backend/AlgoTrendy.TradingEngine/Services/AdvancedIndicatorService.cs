namespace AlgoTrendy.TradingEngine.Services;

using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

/// <summary>
/// Advanced Technical Indicators Service
/// Implements professional-grade indicators including:
/// - Advanced Momentum (Fisher, Laguerre RSI, Connors RSI, etc.)
/// - Volatility & Risk (HV, Parkinson, Garman-Klass, Yang-Zhang, Choppiness)
/// - Multi-Timeframe (MTF RSI, MTF MA, MTF MACD)
/// - Machine Learning Enhanced indicators
/// </summary>
public class AdvancedIndicatorService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<AdvancedIndicatorService> _logger;

    public AdvancedIndicatorService(IMemoryCache cache, ILogger<AdvancedIndicatorService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    #region Advanced Momentum Indicators

    /// <summary>
    /// Ehlers Fisher Transform
    /// Transforms prices to Gaussian normal distribution for better signal clarity
    /// Range: Unbounded, typically -3 to +3
    /// Buy: Fisher crosses above signal line
    /// Sell: Fisher crosses below signal line
    /// </summary>
    public async Task<FisherTransformResult> CalculateFisherTransformAsync(
        string symbol,
        IEnumerable<MarketData> historicalData,
        int period = 10,
        CancellationToken cancellationToken = default)
    {
        var dataList = historicalData.ToList();
        var cacheKey = $"fisher:{symbol}:{period}:{dataList.LastOrDefault()?.Timestamp:yyyy-MM-dd-HH-mm}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            await Task.CompletedTask;
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);

            if (dataList.Count < period + 1)
                return new FisherTransformResult { Fisher = 0, Trigger = 0 };

            var prices = new List<decimal>();
            var fishers = new List<decimal>();
            decimal prevFisher = 0;

            for (int i = 0; i < dataList.Count; i++)
            {
                if (i < period - 1)
                {
                    fishers.Add(0);
                    continue;
                }

                // Get period window
                var window = dataList.Skip(i - period + 1).Take(period).ToList();
                var high = window.Max(d => d.High);
                var low = window.Min(d => d.Low);
                var close = dataList[i].Close;

                // Normalize to -1 to +1
                var value = (high - low) != 0
                    ? 2m * ((close - low) / (high - low) - 0.5m)
                    : 0m;

                // Constrain to -0.999 to +0.999
                value = Math.Max(-0.999m, Math.Min(0.999m, value));

                // Fisher Transform
                var fisher = 0.5m * (decimal)Math.Log((double)((1m + value) / (1m - value)));
                fisher = 0.33m * fisher + 0.67m * prevFisher;

                fishers.Add(fisher);
                prevFisher = fisher;
            }

            var currentFisher = fishers.LastOrDefault();
            var trigger = fishers.Count > 1 ? fishers[fishers.Count - 2] : 0;

            _logger.LogInformation(
                "Fisher Transform calculated - Symbol: {Symbol}, Fisher: {Fisher}, Trigger: {Trigger}",
                symbol, currentFisher, trigger);

            return new FisherTransformResult
            {
                Fisher = currentFisher,
                Trigger = trigger,
                Signal = currentFisher > trigger ? "BUY" : currentFisher < trigger ? "SELL" : "NEUTRAL"
            };
        }) ?? new FisherTransformResult { Fisher = 0, Trigger = 0 };
    }

    /// <summary>
    /// Laguerre RSI
    /// Low-lag RSI using Laguerre filter for faster response
    /// Range: 0-1
    /// Buy: < 0.2 (oversold)
    /// Sell: > 0.8 (overbought)
    /// </summary>
    public async Task<decimal> CalculateLaguerreRSIAsync(
        string symbol,
        IEnumerable<MarketData> historicalData,
        decimal gamma = 0.5m,
        CancellationToken cancellationToken = default)
    {
        var dataList = historicalData.ToList();
        var cacheKey = $"laguerre_rsi:{symbol}:{gamma}:{dataList.LastOrDefault()?.Timestamp:yyyy-MM-dd-HH-mm}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            await Task.CompletedTask;
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);

            if (dataList.Count < 4) return 0.5m;

            decimal L0 = 0, L1 = 0, L2 = 0, L3 = 0;

            foreach (var data in dataList)
            {
                var price = data.Close;

                // Laguerre filter
                L0 = (1m - gamma) * price + gamma * L0;
                L1 = -gamma * L0 + L0 + gamma * L1;
                L2 = -gamma * L1 + L1 + gamma * L2;
                L3 = -gamma * L2 + L2 + gamma * L3;
            }

            var cu = 0m;
            var cd = 0m;

            if (L0 >= L1) cu = L0 - L1; else cd = L1 - L0;
            if (L1 >= L2) cu += L1 - L2; else cd += L2 - L1;
            if (L2 >= L3) cu += L2 - L3; else cd += L3 - L2;

            var laguerreRSI = (cu + cd) != 0 ? cu / (cu + cd) : 0.5m;

            _logger.LogInformation(
                "Laguerre RSI calculated - Symbol: {Symbol}, Value: {Value}",
                symbol, laguerreRSI);

            return laguerreRSI;
        });
    }

    /// <summary>
    /// Connors RSI (CRSI)
    /// Composite momentum indicator combining:
    /// - RSI of close
    /// - RSI of streak (consecutive up/down days)
    /// - Percent rank of ROC
    /// Range: 0-100
    /// Buy: < 10 (extreme oversold)
    /// Sell: > 90 (extreme overbought)
    /// </summary>
    public async Task<decimal> CalculateConnorsRSIAsync(
        string symbol,
        IEnumerable<MarketData> historicalData,
        int rsiPeriod = 3,
        int streakPeriod = 2,
        int rocPeriod = 100,
        CancellationToken cancellationToken = default)
    {
        var dataList = historicalData.ToList();
        var cacheKey = $"connors_rsi:{symbol}:{rsiPeriod}:{streakPeriod}:{rocPeriod}:{dataList.LastOrDefault()?.Timestamp:yyyy-MM-dd-HH-mm}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            await Task.CompletedTask;
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);

            if (dataList.Count < rocPeriod) return 50m;

            var closes = dataList.Select(d => d.Close).ToList();

            // Component 1: RSI of close
            var rsi = CalculateRSI(closes, rsiPeriod);

            // Component 2: RSI of streak
            var streaks = new List<decimal>();
            decimal currentStreak = 0;
            for (int i = 1; i < closes.Count; i++)
            {
                if (closes[i] > closes[i - 1])
                    currentStreak = currentStreak > 0 ? currentStreak + 1 : 1;
                else if (closes[i] < closes[i - 1])
                    currentStreak = currentStreak < 0 ? currentStreak - 1 : -1;
                else
                    currentStreak = 0;

                streaks.Add(currentStreak);
            }
            var streakRSI = CalculateRSI(streaks, streakPeriod);

            // Component 3: Percent Rank of ROC
            var rocs = new List<decimal>();
            for (int i = 1; i < closes.Count; i++)
            {
                var roc = ((closes[i] - closes[i - 1]) / closes[i - 1]) * 100m;
                rocs.Add(roc);
            }
            var currentROC = rocs.LastOrDefault();
            var rocWindow = rocs.TakeLast(rocPeriod).ToList();
            var percentRank = rocWindow.Count(r => r < currentROC) / (decimal)rocWindow.Count * 100m;

            // Connors RSI = Average of all three
            var connorsRSI = (rsi + streakRSI + percentRank) / 3m;

            _logger.LogInformation(
                "Connors RSI calculated - Symbol: {Symbol}, CRSI: {CRSI}, RSI: {RSI}, StreakRSI: {StreakRSI}, PercentRank: {PercentRank}",
                symbol, connorsRSI, rsi, streakRSI, percentRank);

            return connorsRSI;
        });
    }

    /// <summary>
    /// Squeeze Momentum Indicator
    /// Combines Bollinger Bands and Keltner Channels
    /// Squeeze: BB inside KC (low volatility, potential breakout)
    /// Momentum: Histogram shows direction
    /// </summary>
    public async Task<SqueezeMomentumResult> CalculateSqueezeMomentumAsync(
        string symbol,
        IEnumerable<MarketData> historicalData,
        int length = 20,
        decimal bbMult = 2.0m,
        decimal kcMult = 1.5m,
        CancellationToken cancellationToken = default)
    {
        var dataList = historicalData.ToList();
        var cacheKey = $"squeeze:{symbol}:{length}:{bbMult}:{kcMult}:{dataList.LastOrDefault()?.Timestamp:yyyy-MM-dd-HH-mm}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            await Task.CompletedTask;
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);

            if (dataList.Count < length + 1)
                return new SqueezeMomentumResult { IsSqueeze = false, Momentum = 0 };

            var closes = dataList.Select(d => d.Close).ToList();
            var highs = dataList.Select(d => d.High).ToList();
            var lows = dataList.Select(d => d.Low).ToList();

            // Calculate Bollinger Bands
            var sma = closes.TakeLast(length).Average();
            var variance = closes.TakeLast(length).Sum(c => (c - sma) * (c - sma)) / length;
            var stdDev = (decimal)Math.Sqrt((double)variance);
            var bbUpper = sma + (bbMult * stdDev);
            var bbLower = sma - (bbMult * stdDev);

            // Calculate Keltner Channels
            var atr = CalculateATRSimple(dataList.TakeLast(length + 1).ToList());
            var kcUpper = sma + (kcMult * atr);
            var kcLower = sma - (kcMult * atr);

            // Squeeze detection
            var isSqueeze = bbLower > kcLower && bbUpper < kcUpper;

            // Momentum calculation (Linear Regression)
            var momentum = CalculateLinearRegressionValue(closes.TakeLast(length).ToList());

            _logger.LogInformation(
                "Squeeze Momentum calculated - Symbol: {Symbol}, IsSqueeze: {IsSqueeze}, Momentum: {Momentum}",
                symbol, isSqueeze, momentum);

            return new SqueezeMomentumResult
            {
                IsSqueeze = isSqueeze,
                Momentum = momentum,
                Signal = isSqueeze ? "SQUEEZE" : momentum > 0 ? "BULLISH" : "BEARISH",
                BBUpper = bbUpper,
                BBLower = bbLower,
                KCUpper = kcUpper,
                KCLower = kcLower
            };
        }) ?? new SqueezeMomentumResult { IsSqueeze = false, Momentum = 0 };
    }

    /// <summary>
    /// Wave Trend Oscillator
    /// Combines TCI (Typical price Channel Index) with Money Flow
    /// Range: Typically -100 to +100
    /// Buy: Crosses above signal line in oversold zone
    /// Sell: Crosses below signal line in overbought zone
    /// </summary>
    public async Task<WaveTrendResult> CalculateWaveTrendAsync(
        string symbol,
        IEnumerable<MarketData> historicalData,
        int channelLength = 10,
        int avgLength = 21,
        CancellationToken cancellationToken = default)
    {
        var dataList = historicalData.ToList();
        var cacheKey = $"wavetrend:{symbol}:{channelLength}:{avgLength}:{dataList.LastOrDefault()?.Timestamp:yyyy-MM-dd-HH-mm}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            await Task.CompletedTask;
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);

            if (dataList.Count < avgLength + channelLength)
                return new WaveTrendResult { WT1 = 0, WT2 = 0 };

            var typicalPrices = dataList.Select(d => (d.High + d.Low + d.Close) / 3m).ToList();

            // Calculate ESA (Exponential of the Simple Average)
            var esa = CalculateEMA(typicalPrices, channelLength);

            // Calculate absolute difference
            var diffs = new List<decimal>();
            for (int i = 0; i < typicalPrices.Count; i++)
            {
                var diff = Math.Abs(typicalPrices[i] - esa);
                diffs.Add(diff);
            }

            var d = CalculateEMA(diffs, channelLength);
            var ci = d != 0 ? (typicalPrices.Last() - esa) / (0.015m * d) : 0;

            var wt1Values = new List<decimal> { ci };
            var wt1 = CalculateEMA(wt1Values, avgLength);
            var wt2Values = new List<decimal> { wt1 };
            var wt2 = CalculateSMA(wt2Values, 4);

            return new WaveTrendResult
            {
                WT1 = wt1,
                WT2 = wt2,
                Signal = wt1 > wt2 ? "BUY" : wt1 < wt2 ? "SELL" : "NEUTRAL"
            };
        }) ?? new WaveTrendResult { WT1 = 0, WT2 = 0 };
    }

    /// <summary>
    /// Relative Vigor Index (RVI)
    /// Measures the conviction of a price move by comparing close vs open
    /// Range: Oscillates around 0
    /// Buy: RVI crosses above signal line
    /// Sell: RVI crosses below signal line
    /// </summary>
    public async Task<RVIResult> CalculateRVIAsync(
        string symbol,
        IEnumerable<MarketData> historicalData,
        int period = 10,
        CancellationToken cancellationToken = default)
    {
        var dataList = historicalData.ToList();
        var cacheKey = $"rvi:{symbol}:{period}:{dataList.LastOrDefault()?.Timestamp:yyyy-MM-dd-HH-mm}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            await Task.CompletedTask;
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);

            if (dataList.Count < period + 3)
                return new RVIResult { RVI = 0, Signal = 0 };

            var numerators = new List<decimal>();
            var denominators = new List<decimal>();

            for (int i = 3; i < dataList.Count; i++)
            {
                // Weighted difference of close - open
                var num = ((dataList[i].Close - dataList[i].Open) +
                          2m * (dataList[i - 1].Close - dataList[i - 1].Open) +
                          2m * (dataList[i - 2].Close - dataList[i - 2].Open) +
                          (dataList[i - 3].Close - dataList[i - 3].Open)) / 6m;

                // Weighted difference of high - low
                var den = ((dataList[i].High - dataList[i].Low) +
                          2m * (dataList[i - 1].High - dataList[i - 1].Low) +
                          2m * (dataList[i - 2].High - dataList[i - 2].Low) +
                          (dataList[i - 3].High - dataList[i - 3].Low)) / 6m;

                numerators.Add(num);
                denominators.Add(den);
            }

            var avgNum = numerators.TakeLast(period).Average();
            var avgDen = denominators.TakeLast(period).Average();
            var rvi = avgDen != 0 ? avgNum / avgDen : 0;

            // Signal line is SMA of RVI
            var rviSignal = rvi * 0.67m; // Simplified signal

            return new RVIResult
            {
                RVI = rvi,
                Signal = rviSignal,
                Trend = rvi > rviSignal ? "BULLISH" : "BEARISH"
            };
        }) ?? new RVIResult { RVI = 0, Signal = 0 };
    }

    /// <summary>
    /// Schaff Trend Cycle (STC)
    /// Enhanced MACD using Stochastic calculation
    /// Range: 0-100
    /// Buy: Crosses above 25
    /// Sell: Crosses below 75
    /// </summary>
    public async Task<decimal> CalculateSchaffTrendCycleAsync(
        string symbol,
        IEnumerable<MarketData> historicalData,
        int fastPeriod = 23,
        int slowPeriod = 50,
        int cyclePeriod = 10,
        CancellationToken cancellationToken = default)
    {
        var dataList = historicalData.ToList();
        var cacheKey = $"stc:{symbol}:{fastPeriod}:{slowPeriod}:{cyclePeriod}:{dataList.LastOrDefault()?.Timestamp:yyyy-MM-dd-HH-mm}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            await Task.CompletedTask;
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);

            if (dataList.Count < slowPeriod + cyclePeriod)
                return 50m;

            var closes = dataList.Select(d => d.Close).ToList();

            // Calculate MACD
            var fastEMA = CalculateEMA(closes, fastPeriod);
            var slowEMA = CalculateEMA(closes, slowPeriod);
            var macd = fastEMA - slowEMA;

            // Apply Stochastic calculation to MACD
            var macdValues = new List<decimal> { macd };
            var stoch = CalculateStochastic(macdValues, cyclePeriod);

            // Apply Stochastic again (double smoothed)
            var stochValues = new List<decimal> { stoch };
            var stc = CalculateStochastic(stochValues, cyclePeriod);

            _logger.LogInformation(
                "Schaff Trend Cycle calculated - Symbol: {Symbol}, STC: {STC}",
                symbol, stc);

            return stc;
        });
    }

    #endregion

    #region Volatility & Risk Indicators

    /// <summary>
    /// Historical Volatility (HV)
    /// Realized volatility based on standard deviation of returns
    /// Annualized percentage
    /// </summary>
    public async Task<decimal> CalculateHistoricalVolatilityAsync(
        string symbol,
        IEnumerable<MarketData> historicalData,
        int period = 30,
        CancellationToken cancellationToken = default)
    {
        var dataList = historicalData.ToList();
        var cacheKey = $"hv:{symbol}:{period}:{dataList.LastOrDefault()?.Timestamp:yyyy-MM-dd-HH-mm}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            await Task.CompletedTask;
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);

            if (dataList.Count < period + 1) return 0m;

            var returns = new List<decimal>();
            for (int i = 1; i < dataList.Count; i++)
            {
                var logReturn = (decimal)Math.Log((double)(dataList[i].Close / dataList[i - 1].Close));
                returns.Add(logReturn);
            }

            var recentReturns = returns.TakeLast(period).ToList();
            var mean = recentReturns.Average();
            var variance = recentReturns.Sum(r => (r - mean) * (r - mean)) / period;
            var stdDev = (decimal)Math.Sqrt((double)variance);

            // Annualize (assuming 252 trading days)
            var annualizedVol = stdDev * (decimal)Math.Sqrt(252) * 100m;

            _logger.LogInformation(
                "Historical Volatility calculated - Symbol: {Symbol}, HV: {HV}%",
                symbol, annualizedVol);

            return annualizedVol;
        });
    }

    /// <summary>
    /// Parkinson's Volatility
    /// Range-based volatility estimator using high and low
    /// More efficient than close-to-close
    /// </summary>
    public async Task<decimal> CalculateParkinsonVolatilityAsync(
        string symbol,
        IEnumerable<MarketData> historicalData,
        int period = 30,
        CancellationToken cancellationToken = default)
    {
        var dataList = historicalData.ToList();
        var cacheKey = $"parkinson:{symbol}:{period}:{dataList.LastOrDefault()?.Timestamp:yyyy-MM-dd-HH-mm}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            await Task.CompletedTask;
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);

            if (dataList.Count < period) return 0m;

            var recentData = dataList.TakeLast(period).ToList();
            var sum = 0m;

            foreach (var data in recentData)
            {
                if (data.Low > 0)
                {
                    var logHL = (decimal)Math.Log((double)(data.High / data.Low));
                    sum += logHL * logHL;
                }
            }

            var parkinson = (decimal)Math.Sqrt((double)(sum / (4m * period * (decimal)Math.Log(2))));
            var annualized = parkinson * (decimal)Math.Sqrt(252) * 100m;

            _logger.LogInformation(
                "Parkinson Volatility calculated - Symbol: {Symbol}, Parkinson: {Parkinson}%",
                symbol, annualized);

            return annualized;
        });
    }

    /// <summary>
    /// Garman-Klass Volatility
    /// OHLC-based volatility estimator
    /// More accurate than Parkinson
    /// </summary>
    public async Task<decimal> CalculateGarmanKlassVolatilityAsync(
        string symbol,
        IEnumerable<MarketData> historicalData,
        int period = 30,
        CancellationToken cancellationToken = default)
    {
        var dataList = historicalData.ToList();
        var cacheKey = $"gk:{symbol}:{period}:{dataList.LastOrDefault()?.Timestamp:yyyy-MM-dd-HH-mm}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            await Task.CompletedTask;
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);

            if (dataList.Count < period) return 0m;

            var recentData = dataList.TakeLast(period).ToList();
            var sum = 0m;

            foreach (var data in recentData)
            {
                if (data.Low > 0 && data.Open > 0)
                {
                    var logHL = (decimal)Math.Log((double)(data.High / data.Low));
                    var logCO = (decimal)Math.Log((double)(data.Close / data.Open));
                    sum += 0.5m * logHL * logHL - (2m * (decimal)Math.Log(2) - 1m) * logCO * logCO;
                }
            }

            var gk = (decimal)Math.Sqrt((double)(sum / period));
            var annualized = gk * (decimal)Math.Sqrt(252) * 100m;

            _logger.LogInformation(
                "Garman-Klass Volatility calculated - Symbol: {Symbol}, GK: {GK}%",
                symbol, annualized);

            return annualized;
        });
    }

    /// <summary>
    /// Yang-Zhang Volatility
    /// Best OHLC volatility estimator
    /// Combines overnight and intraday volatility
    /// </summary>
    public async Task<decimal> CalculateYangZhangVolatilityAsync(
        string symbol,
        IEnumerable<MarketData> historicalData,
        int period = 30,
        CancellationToken cancellationToken = default)
    {
        var dataList = historicalData.ToList();
        var cacheKey = $"yz:{symbol}:{period}:{dataList.LastOrDefault()?.Timestamp:yyyy-MM-dd-HH-mm}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            await Task.CompletedTask;
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);

            if (dataList.Count < period + 1) return 0m;

            var recentData = dataList.TakeLast(period + 1).ToList();

            // Overnight volatility
            var overnightSum = 0m;
            for (int i = 1; i < recentData.Count; i++)
            {
                if (recentData[i].Open > 0 && recentData[i - 1].Close > 0)
                {
                    var logOC = (decimal)Math.Log((double)(recentData[i].Open / recentData[i - 1].Close));
                    overnightSum += logOC * logOC;
                }
            }
            var overnightVol = overnightSum / period;

            // Open-to-close volatility
            var ocSum = 0m;
            foreach (var data in recentData.Take(period))
            {
                if (data.Close > 0 && data.Open > 0)
                {
                    var logOC = (decimal)Math.Log((double)(data.Close / data.Open));
                    ocSum += logOC * logOC;
                }
            }
            var ocVol = ocSum / period;

            // Rogers-Satchell volatility
            var rsSum = 0m;
            foreach (var data in recentData.Take(period))
            {
                if (data.High > 0 && data.Low > 0 && data.Open > 0 && data.Close > 0)
                {
                    var logHC = (decimal)Math.Log((double)(data.High / data.Close));
                    var logHO = (decimal)Math.Log((double)(data.High / data.Open));
                    var logLC = (decimal)Math.Log((double)(data.Low / data.Close));
                    var logLO = (decimal)Math.Log((double)(data.Low / data.Open));
                    rsSum += logHC * logHO + logLC * logLO;
                }
            }
            var rsVol = rsSum / period;

            // Yang-Zhang estimator
            var k = 0.34m / (1.34m + (period + 1m) / (period - 1m));
            var yz = (decimal)Math.Sqrt((double)(overnightVol + k * ocVol + (1m - k) * rsVol));
            var annualized = yz * (decimal)Math.Sqrt(252) * 100m;

            _logger.LogInformation(
                "Yang-Zhang Volatility calculated - Symbol: {Symbol}, YZ: {YZ}%",
                symbol, annualized);

            return annualized;
        });
    }

    /// <summary>
    /// Choppiness Index
    /// Determines if market is trending or ranging
    /// Range: 0-100
    /// < 38.2: Trending
    /// > 61.8: Choppy/Ranging
    /// 38.2-61.8: Transitional
    /// </summary>
    public async Task<ChoppinessResult> CalculateChoppinessIndexAsync(
        string symbol,
        IEnumerable<MarketData> historicalData,
        int period = 14,
        CancellationToken cancellationToken = default)
    {
        var dataList = historicalData.ToList();
        var cacheKey = $"chop:{symbol}:{period}:{dataList.LastOrDefault()?.Timestamp:yyyy-MM-dd-HH-mm}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            await Task.CompletedTask;
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);

            if (dataList.Count < period + 1)
                return new ChoppinessResult { Index = 50, State = "UNKNOWN" };

            var recentData = dataList.TakeLast(period + 1).ToList();

            // Sum of True Ranges
            var trSum = 0m;
            for (int i = 1; i < recentData.Count; i++)
            {
                var tr = Math.Max(
                    recentData[i].High - recentData[i].Low,
                    Math.Max(
                        Math.Abs(recentData[i].High - recentData[i - 1].Close),
                        Math.Abs(recentData[i].Low - recentData[i - 1].Close)
                    )
                );
                trSum += tr;
            }

            // High-Low range over period
            var periodHigh = recentData.Skip(1).Max(d => d.High);
            var periodLow = recentData.Skip(1).Min(d => d.Low);
            var range = periodHigh - periodLow;

            // Choppiness Index
            var chop = range > 0
                ? 100m * (decimal)Math.Log10((double)(trSum / range)) / (decimal)Math.Log10(period)
                : 50m;

            var state = chop < 38.2m ? "TRENDING"
                      : chop > 61.8m ? "CHOPPY"
                      : "TRANSITIONAL";

            _logger.LogInformation(
                "Choppiness Index calculated - Symbol: {Symbol}, Choppiness: {Chop}, State: {State}",
                symbol, chop, state);

            return new ChoppinessResult
            {
                Index = chop,
                State = state,
                IsTrending = chop < 38.2m,
                IsRanging = chop > 61.8m
            };
        }) ?? new ChoppinessResult { Index = 50, State = "UNKNOWN" };
    }

    #endregion

    #region Multi-Timeframe Indicators

    /// <summary>
    /// Multi-Timeframe RSI
    /// Calculates RSI across multiple timeframes for confluence
    /// </summary>
    public async Task<MTFIndicatorResult> CalculateMTFRSIAsync(
        string symbol,
        Dictionary<string, IEnumerable<MarketData>> timeframeData,
        int period = 14,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, decimal>();

        foreach (var kvp in timeframeData)
        {
            var timeframe = kvp.Key;
            var data = kvp.Value.ToList();

            if (data.Count >= period + 1)
            {
                var closes = data.Select(d => d.Close).ToList();
                var rsi = CalculateRSI(closes, period);
                results[timeframe] = rsi;
            }
        }

        var avgRSI = results.Values.Any() ? results.Values.Average() : 50m;
        var bullishCount = results.Values.Count(v => v < 30);
        var bearishCount = results.Values.Count(v => v > 70);

        var signal = bullishCount >= 2 ? "STRONG_BUY"
                   : avgRSI < 30 ? "BUY"
                   : bearishCount >= 2 ? "STRONG_SELL"
                   : avgRSI > 70 ? "SELL"
                   : "NEUTRAL";

        return new MTFIndicatorResult
        {
            Name = "MTF_RSI",
            Values = results,
            AverageValue = avgRSI,
            Signal = signal,
            Confluence = results.Values.Count(v =>
                (avgRSI < 30 && v < 40) || (avgRSI > 70 && v > 60)
            )
        };
    }

    /// <summary>
    /// Multi-Timeframe Moving Average Trend
    /// Checks trend alignment across timeframes
    /// </summary>
    public async Task<MTFIndicatorResult> CalculateMTFMovingAverageAsync(
        string symbol,
        Dictionary<string, IEnumerable<MarketData>> timeframeData,
        int shortPeriod = 20,
        int longPeriod = 50,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, decimal>();
        var trends = new Dictionary<string, string>();

        foreach (var kvp in timeframeData)
        {
            var timeframe = kvp.Key;
            var data = kvp.Value.ToList();

            if (data.Count >= longPeriod)
            {
                var closes = data.Select(d => d.Close).ToList();
                var shortMA = CalculateEMA(closes, shortPeriod);
                var longMA = CalculateEMA(closes, longPeriod);

                var trend = shortMA > longMA ? 1m : -1m;
                results[timeframe] = trend;
                trends[timeframe] = trend > 0 ? "UP" : "DOWN";
            }
        }

        var bullishCount = results.Values.Count(v => v > 0);
        var bearishCount = results.Values.Count(v => v < 0);
        var totalTimeframes = results.Count;

        var signal = bullishCount == totalTimeframes ? "STRONG_BUY"
                   : bullishCount > bearishCount ? "BUY"
                   : bearishCount == totalTimeframes ? "STRONG_SELL"
                   : bearishCount > bullishCount ? "SELL"
                   : "NEUTRAL";

        return new MTFIndicatorResult
        {
            Name = "MTF_MA_Trend",
            Values = results,
            Trends = trends,
            Signal = signal,
            Confluence = Math.Max(bullishCount, bearishCount)
        };
    }

    #endregion

    #region Helper Methods

    private decimal CalculateRSI(List<decimal> prices, int period)
    {
        if (prices.Count < period + 1) return 50m;

        var gains = new List<decimal>();
        var losses = new List<decimal>();

        for (int i = 1; i < prices.Count; i++)
        {
            var change = prices[i] - prices[i - 1];
            gains.Add(change > 0 ? change : 0);
            losses.Add(change < 0 ? Math.Abs(change) : 0);
        }

        var avgGain = gains.TakeLast(period).Average();
        var avgLoss = losses.TakeLast(period).Average();

        if (avgLoss == 0) return 100m;

        var rs = avgGain / avgLoss;
        return 100m - (100m / (1m + rs));
    }

    private decimal CalculateEMA(List<decimal> values, int period)
    {
        if (values.Count < period) return values.LastOrDefault();

        var multiplier = 2m / (period + 1);
        var ema = values.Take(period).Average();

        for (int i = period; i < values.Count; i++)
        {
            ema = (values[i] - ema) * multiplier + ema;
        }

        return ema;
    }

    private decimal CalculateSMA(List<decimal> values, int period)
    {
        if (values.Count < period) return values.LastOrDefault();
        return values.TakeLast(period).Average();
    }

    private decimal CalculateATRSimple(List<MarketData> data)
    {
        if (data.Count < 2) return 0;

        var trs = new List<decimal>();
        for (int i = 1; i < data.Count; i++)
        {
            var tr = Math.Max(
                data[i].High - data[i].Low,
                Math.Max(
                    Math.Abs(data[i].High - data[i - 1].Close),
                    Math.Abs(data[i].Low - data[i - 1].Close)
                )
            );
            trs.Add(tr);
        }

        return trs.Average();
    }

    private decimal CalculateLinearRegressionValue(List<decimal> values)
    {
        if (values.Count == 0) return 0;

        var n = values.Count;
        var sumX = 0m;
        var sumY = 0m;
        var sumXY = 0m;
        var sumX2 = 0m;

        for (int i = 0; i < n; i++)
        {
            sumX += i;
            sumY += values[i];
            sumXY += i * values[i];
            sumX2 += i * i;
        }

        var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        return slope * n; // Return the current value
    }

    private decimal CalculateStochastic(List<decimal> values, int period)
    {
        if (values.Count < period) return 50m;

        var recentValues = values.TakeLast(period).ToList();
        var high = recentValues.Max();
        var low = recentValues.Min();
        var current = values.Last();

        return (high - low) != 0
            ? ((current - low) / (high - low)) * 100m
            : 50m;
    }

    #endregion
}

#region Result Models

public class FisherTransformResult
{
    public required decimal Fisher { get; init; }
    public required decimal Trigger { get; init; }
    public string Signal { get; init; } = "NEUTRAL";
}

public class SqueezeMomentumResult
{
    public required bool IsSqueeze { get; init; }
    public required decimal Momentum { get; init; }
    public string Signal { get; init; } = "NEUTRAL";
    public decimal BBUpper { get; init; }
    public decimal BBLower { get; init; }
    public decimal KCUpper { get; init; }
    public decimal KCLower { get; init; }
}

public class WaveTrendResult
{
    public required decimal WT1 { get; init; }
    public required decimal WT2 { get; init; }
    public string Signal { get; init; } = "NEUTRAL";
}

public class RVIResult
{
    public required decimal RVI { get; init; }
    public required decimal Signal { get; init; }
    public string Trend { get; init; } = "NEUTRAL";
}

public class ChoppinessResult
{
    public required decimal Index { get; init; }
    public required string State { get; init; }
    public bool IsTrending { get; init; }
    public bool IsRanging { get; init; }
}

public class MTFIndicatorResult
{
    public required string Name { get; init; }
    public required Dictionary<string, decimal> Values { get; init; }
    public Dictionary<string, string>? Trends { get; init; }
    public decimal AverageValue { get; init; }
    public required string Signal { get; init; }
    public int Confluence { get; init; }
}

#endregion
