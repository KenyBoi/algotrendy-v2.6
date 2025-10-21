# Liquidity Indicators & Liquidity Gap Detection
## Reference Guide for High-Frequency Trading Strategies

**Last Updated**: 2025-10-21
**Application**: All HFT strategies (AS Market Making, Pairs Trading, ML-HFT)
**Critical For**: Order execution quality, slippage prevention, entry/exit timing

---

## Table of Contents

1. [Overview](#overview)
2. [Order Book Liquidity Indicators](#order-book-liquidity-indicators)
3. [Volume-Based Liquidity Indicators](#volume-based-liquidity-indicators)
4. [Market Impact Indicators](#market-impact-indicators)
5. [Liquidity Gap Detection](#liquidity-gap-detection)
6. [Time-Based Liquidity Indicators](#time-based-liquidity-indicators)
7. [Implementation Guide](#implementation-guide)
8. [Integration with Strategies](#integration-with-strategies)

---

## Overview

### What is Liquidity?

**Liquidity** is the ability to buy or sell an asset quickly without causing significant price movement. High liquidity = tight spreads, deep order books, minimal slippage.

### Why Liquidity Matters for HFT

1. **Execution Quality**: Deeper liquidity → better fill prices
2. **Slippage Prevention**: Thin liquidity → large slippage on orders
3. **Market Making**: Requires sufficient liquidity to enter/exit positions
4. **Risk Management**: Liquidity gaps can trap positions
5. **Trade Sizing**: Liquidity determines maximum safe order size

### Liquidity Gaps

**Liquidity gaps** are zones in the order book with:
- Few or no limit orders
- Sudden drop in available volume
- Wide price spreads between levels
- Potential for rapid price moves through the gap

**Impact**: Orders can "fall through" gaps, causing severe slippage

---

## Order Book Liquidity Indicators

### 1. Bid-Ask Spread

**Definition**: Difference between best bid and best ask price

```
spread_absolute = best_ask - best_bid
spread_pct = (best_ask - best_bid) / mid_price
```

**Interpretation**:
- Tight spread (< 0.05%): High liquidity
- Medium spread (0.05-0.2%): Moderate liquidity
- Wide spread (> 0.2%): Low liquidity

**C# Implementation**:
```csharp
public class SpreadIndicator
{
    public SpreadMetrics Calculate(OrderBookSnapshot orderBook)
    {
        var spread = orderBook.BestAsk - orderBook.BestBid;
        var midPrice = (orderBook.BestAsk + orderBook.BestBid) / 2;

        return new SpreadMetrics
        {
            AbsoluteSpread = spread,
            PercentageSpread = spread / midPrice,
            MidPrice = midPrice,
            IsLiquid = (spread / midPrice) < 0.002m // < 0.2%
        };
    }
}
```

---

### 2. Order Book Depth

**Definition**: Total volume available at each price level

**Metrics**:
```
depth_at_level_n = sum of volume from best bid/ask to n levels away

total_bid_depth_5 = sum(bid_volume[0:5])
total_ask_depth_5 = sum(ask_volume[0:5])
total_depth_5 = total_bid_depth_5 + total_ask_depth_5
```

**Interpretation**:
- Deep book (> $1M at 5 levels): High liquidity
- Shallow book (< $100K at 5 levels): Low liquidity

**C# Implementation**:
```csharp
public class OrderBookDepthIndicator
{
    public DepthMetrics Calculate(OrderBookSnapshot orderBook, int levels = 5)
    {
        var bidDepth = orderBook.Bids
            .Take(levels)
            .Sum(b => b.Price * b.Quantity); // Dollar value

        var askDepth = orderBook.Asks
            .Take(levels)
            .Sum(a => a.Price * a.Quantity); // Dollar value

        return new DepthMetrics
        {
            BidDepth = bidDepth,
            AskDepth = askDepth,
            TotalDepth = bidDepth + askDepth,
            DepthImbalance = (bidDepth - askDepth) / (bidDepth + askDepth),
            AverageOrderSize = (bidDepth + askDepth) / (levels * 2)
        };
    }
}
```

---

### 3. Order Book Imbalance (OBI)

**Definition**: Asymmetry between bid and ask volumes

```
OBI = (bid_volume - ask_volume) / (bid_volume + ask_volume)
```

**Interpretation**:
- OBI > +0.3: Strong buy pressure (likely price increase)
- OBI < -0.3: Strong sell pressure (likely price decrease)
- OBI ≈ 0: Balanced (neutral)

**Usage**: Predict short-term price direction

**C# Implementation**:
```csharp
public class OrderBookImbalanceIndicator
{
    public OBIMetrics Calculate(OrderBookSnapshot orderBook, int levels = 5)
    {
        var bidVolume = orderBook.Bids.Take(levels).Sum(b => b.Quantity);
        var askVolume = orderBook.Asks.Take(levels).Sum(a => a.Quantity);

        var obi = (bidVolume - askVolume) / (bidVolume + askVolume);

        return new OBIMetrics
        {
            OBI = obi,
            BidVolume = bidVolume,
            AskVolume = askVolume,
            Pressure = obi > 0.3m ? "BUY" :
                       obi < -0.3m ? "SELL" : "NEUTRAL",
            Strength = Math.Abs(obi)
        };
    }
}
```

---

### 4. Cumulative Volume Delta (CVD)

**Definition**: Running cumulative difference between buy and sell volumes

```
CVD[t] = CVD[t-1] + (buy_volume[t] - sell_volume[t])
```

**Interpretation**:
- Rising CVD: Accumulation (bullish)
- Falling CVD: Distribution (bearish)
- Divergence from price: Reversal signal

**C# Implementation**:
```csharp
public class CumulativeVolumeDeltaIndicator
{
    private decimal _cumulativeDelta = 0;

    public CVDMetrics Update(Trade trade)
    {
        var delta = trade.IsBuyerMaker ? -trade.Quantity : trade.Quantity;
        _cumulativeDelta += delta;

        return new CVDMetrics
        {
            CVD = _cumulativeDelta,
            Trend = _cumulativeDelta > 0 ? "ACCUMULATION" : "DISTRIBUTION"
        };
    }
}
```

---

### 5. Microprice

**Definition**: Volume-weighted mid price

```
microprice = (bid_price * ask_volume + ask_price * bid_volume) / (bid_volume + ask_volume)
```

**Why Better Than Mid Price**: Accounts for volume imbalance, predicts next trade price better

**C# Implementation**:
```csharp
public decimal CalculateMicroprice(OrderBookSnapshot orderBook)
{
    var bestBid = orderBook.Bids.First();
    var bestAsk = orderBook.Asks.First();

    return (bestBid.Price * bestAsk.Quantity + bestAsk.Price * bestBid.Quantity) /
           (bestBid.Quantity + bestAsk.Quantity);
}
```

---

## Volume-Based Liquidity Indicators

### 6. Volume-Weighted Average Price (VWAP)

**Definition**: Average price weighted by volume

```
VWAP = sum(price * volume) / sum(volume)
```

**Usage**:
- Benchmark for execution quality
- Support/resistance levels
- Institutional trader reference

**Distance from VWAP**:
```
vwap_distance = (current_price - VWAP) / VWAP
```

**C# Implementation**:
```csharp
public class VWAPIndicator
{
    private List<(decimal price, decimal volume)> _trades = new();

    public VWAPMetrics Update(decimal price, decimal volume)
    {
        _trades.Add((price, volume));

        var totalValue = _trades.Sum(t => t.price * t.volume);
        var totalVolume = _trades.Sum(t => t.volume);
        var vwap = totalValue / totalVolume;

        return new VWAPMetrics
        {
            VWAP = vwap,
            Distance = (price - vwap) / vwap,
            AboveVWAP = price > vwap
        };
    }
}
```

---

### 7. Amihud Illiquidity Ratio

**Definition**: Price impact per unit of volume

```
Amihud = |return| / volume
```

**Interpretation**:
- Low Amihud: High liquidity (small price impact)
- High Amihud: Low liquidity (large price impact)

**C# Implementation**:
```csharp
public class AmihudIlliquidityIndicator
{
    public decimal Calculate(List<Candle> candles)
    {
        var illiquidities = new List<decimal>();

        for (int i = 1; i < candles.Count; i++)
        {
            var ret = Math.Abs((candles[i].Close - candles[i - 1].Close) / candles[i - 1].Close);
            var volume = candles[i].Volume;

            if (volume > 0)
                illiquidities.Add(ret / volume);
        }

        return illiquidities.Average();
    }
}
```

---

### 8. Volume Participation Rate (VPR)

**Definition**: Your order volume as % of market volume

```
VPR = order_volume / total_market_volume_in_period
```

**Safe Limits**:
- VPR < 5%: Minimal market impact
- VPR 5-10%: Moderate impact
- VPR > 10%: Significant impact (avoid)

**C# Implementation**:
```csharp
public class VolumeParticipationIndicator
{
    public VPRMetrics Calculate(decimal orderSize, decimal periodVolume)
    {
        var vpr = orderSize / periodVolume;

        return new VPRMetrics
        {
            VPR = vpr,
            IsAcceptable = vpr < 0.05m,
            ImpactLevel = vpr < 0.05m ? "LOW" :
                          vpr < 0.10m ? "MODERATE" : "HIGH",
            RecommendedMaxSize = periodVolume * 0.05m
        };
    }
}
```

---

## Market Impact Indicators

### 9. Effective Spread

**Definition**: Actual spread paid after execution

```
effective_spread = 2 * |execution_price - mid_price|
```

**Interpretation**: Measures realized cost of trading

**C# Implementation**:
```csharp
public class EffectiveSpreadIndicator
{
    public decimal Calculate(decimal executionPrice, OrderBookSnapshot orderBook)
    {
        var midPrice = (orderBook.BestBid + orderBook.BestAsk) / 2;
        return 2 * Math.Abs(executionPrice - midPrice);
    }
}
```

---

### 10. Market Impact Coefficient

**Definition**: Expected price move for given order size

```
impact = α * (order_size / average_daily_volume)^β

where α and β are estimated from historical data
typical β ≈ 0.5 (square root law)
```

**C# Implementation**:
```csharp
public class MarketImpactIndicator
{
    private readonly decimal _alpha = 0.1m; // Calibrated from data
    private readonly decimal _beta = 0.5m;  // Square root law

    public ImpactMetrics Calculate(decimal orderSize, decimal avgDailyVolume)
    {
        var sizeRatio = orderSize / avgDailyVolume;
        var impact = _alpha * (decimal)Math.Pow((double)sizeRatio, (double)_beta);

        return new ImpactMetrics
        {
            ExpectedImpactPct = impact,
            OrderSizeRatio = sizeRatio,
            IsAcceptable = impact < 0.005m // < 0.5%
        };
    }
}
```

---

## Liquidity Gap Detection

### 11. Gap Size Indicator

**Definition**: Identify zones with insufficient liquidity

**Algorithm**:
```
1. For each price level in order book:
   - Calculate volume at level
   - Calculate price distance from previous level

2. Detect gap if:
   - Volume < threshold (e.g., 10% of average)
   - OR Price jump > threshold (e.g., 2x avg spread)
```

**C# Implementation**:
```csharp
public class LiquidityGapDetector
{
    public List<LiquidityGap> DetectGaps(OrderBookSnapshot orderBook)
    {
        var gaps = new List<LiquidityGap>();

        // Calculate average volume and spread
        var avgBidVolume = orderBook.Bids.Take(10).Average(b => b.Quantity);
        var avgAskVolume = orderBook.Asks.Take(10).Average(a => a.Quantity);
        var avgSpread = CalculateAverageSpread(orderBook);

        // Check bid side
        for (int i = 1; i < orderBook.Bids.Count; i++)
        {
            var currentLevel = orderBook.Bids[i];
            var previousLevel = orderBook.Bids[i - 1];

            var volumeRatio = currentLevel.Quantity / avgBidVolume;
            var priceGap = previousLevel.Price - currentLevel.Price;
            var spreadRatio = priceGap / avgSpread;

            if (volumeRatio < 0.1m || spreadRatio > 2.0m)
            {
                gaps.Add(new LiquidityGap
                {
                    Side = "BID",
                    StartPrice = currentLevel.Price,
                    EndPrice = previousLevel.Price,
                    GapSize = priceGap,
                    Volume = currentLevel.Quantity,
                    Severity = volumeRatio < 0.05m ? "HIGH" :
                              volumeRatio < 0.1m ? "MEDIUM" : "LOW"
                });
            }
        }

        // Check ask side (similar logic)
        for (int i = 1; i < orderBook.Asks.Count; i++)
        {
            var currentLevel = orderBook.Asks[i];
            var previousLevel = orderBook.Asks[i - 1];

            var volumeRatio = currentLevel.Quantity / avgAskVolume;
            var priceGap = currentLevel.Price - previousLevel.Price;
            var spreadRatio = priceGap / avgSpread;

            if (volumeRatio < 0.1m || spreadRatio > 2.0m)
            {
                gaps.Add(new LiquidityGap
                {
                    Side = "ASK",
                    StartPrice = previousLevel.Price,
                    EndPrice = currentLevel.Price,
                    GapSize = priceGap,
                    Volume = currentLevel.Quantity,
                    Severity = volumeRatio < 0.05m ? "HIGH" :
                              volumeRatio < 0.1m ? "MEDIUM" : "LOW"
                });
            }
        }

        return gaps;
    }

    private decimal CalculateAverageSpread(OrderBookSnapshot orderBook)
    {
        var spreads = new List<decimal>();

        for (int i = 1; i < Math.Min(10, orderBook.Bids.Count); i++)
        {
            spreads.Add(orderBook.Bids[i - 1].Price - orderBook.Bids[i].Price);
        }

        for (int i = 1; i < Math.Min(10, orderBook.Asks.Count); i++)
        {
            spreads.Add(orderBook.Asks[i].Price - orderBook.Asks[i - 1].Price);
        }

        return spreads.Average();
    }
}

public class LiquidityGap
{
    public string Side { get; set; }
    public decimal StartPrice { get; set; }
    public decimal EndPrice { get; set; }
    public decimal GapSize { get; set; }
    public decimal Volume { get; set; }
    public string Severity { get; set; }
}
```

---

### 12. Volume Profile

**Definition**: Distribution of volume at each price level

**Usage**: Identify support/resistance and liquidity clusters

**C# Implementation**:
```csharp
public class VolumeProfileIndicator
{
    public VolumeProfile Calculate(List<Trade> trades, int priceBins = 100)
    {
        var minPrice = trades.Min(t => t.Price);
        var maxPrice = trades.Max(t => t.Price);
        var binSize = (maxPrice - minPrice) / priceBins;

        var volumeByPrice = new Dictionary<decimal, decimal>();

        foreach (var trade in trades)
        {
            var bin = Math.Floor((trade.Price - minPrice) / binSize) * binSize + minPrice;

            if (!volumeByPrice.ContainsKey(bin))
                volumeByPrice[bin] = 0;

            volumeByPrice[bin] += trade.Quantity;
        }

        var maxVolumePrice = volumeByPrice.OrderByDescending(kvp => kvp.Value).First().Key;

        return new VolumeProfile
        {
            PointOfControl = maxVolumePrice, // Price with most volume
            ValueArea = CalculateValueArea(volumeByPrice, 0.70m), // 70% of volume
            VolumeClusters = IdentifyVolumeClusters(volumeByPrice)
        };
    }

    private (decimal low, decimal high) CalculateValueArea(
        Dictionary<decimal, decimal> volumeByPrice,
        decimal percentile)
    {
        var totalVolume = volumeByPrice.Values.Sum();
        var targetVolume = totalVolume * percentile;

        var sortedByVolume = volumeByPrice.OrderByDescending(kvp => kvp.Value).ToList();

        decimal cumulativeVolume = 0;
        decimal minPrice = decimal.MaxValue;
        decimal maxPrice = decimal.MinValue;

        foreach (var (price, volume) in sortedByVolume)
        {
            cumulativeVolume += volume;

            if (cumulativeVolume <= targetVolume)
            {
                minPrice = Math.Min(minPrice, price);
                maxPrice = Math.Max(maxPrice, price);
            }
            else
            {
                break;
            }
        }

        return (minPrice, maxPrice);
    }

    private List<VolumeCluster> IdentifyVolumeClusters(
        Dictionary<decimal, decimal> volumeByPrice)
    {
        var avgVolume = volumeByPrice.Values.Average();
        var clusters = new List<VolumeCluster>();

        foreach (var (price, volume) in volumeByPrice.OrderBy(kvp => kvp.Key))
        {
            if (volume > avgVolume * 1.5m) // 50% above average = cluster
            {
                clusters.Add(new VolumeCluster
                {
                    Price = price,
                    Volume = volume,
                    Strength = volume / avgVolume
                });
            }
        }

        return clusters;
    }
}

public class VolumeProfile
{
    public decimal PointOfControl { get; set; }
    public (decimal low, decimal high) ValueArea { get; set; }
    public List<VolumeCluster> VolumeClusters { get; set; }
}

public class VolumeCluster
{
    public decimal Price { get; set; }
    public decimal Volume { get; set; }
    public decimal Strength { get; set; }
}
```

---

## Time-Based Liquidity Indicators

### 13. Liquidity Time Score

**Definition**: Liquidity varies by time of day (sessions, news, etc.)

**High Liquidity Times** (Crypto):
- 08:00-11:00 UTC: Asian session open
- 13:00-17:00 UTC: European session
- 14:00-21:00 UTC: US session overlap (highest liquidity)

**Low Liquidity Times**:
- 22:00-06:00 UTC: Weekend/overnight

**C# Implementation**:
```csharp
public class LiquidityTimeIndicator
{
    public LiquidityTimeScore GetTimeScore(DateTime timestamp)
    {
        var hour = timestamp.Hour; // UTC

        var score = hour switch
        {
            >= 14 and <= 21 => 1.0m, // US/EU overlap - highest liquidity
            >= 13 and <= 17 => 0.9m, // EU session
            >= 8 and <= 11 => 0.8m,  // Asia session
            _ => 0.5m                 // Overnight/low activity
        };

        // Weekend penalty
        if (timestamp.DayOfWeek == DayOfWeek.Saturday ||
            timestamp.DayOfWeek == DayOfWeek.Sunday)
        {
            score *= 0.7m;
        }

        return new LiquidityTimeScore
        {
            Score = score,
            Period = GetSessionName(timestamp),
            Recommendation = score > 0.8m ? "TRADE" : "CAUTION"
        };
    }

    private string GetSessionName(DateTime timestamp)
    {
        var hour = timestamp.Hour;

        return hour switch
        {
            >= 14 and <= 21 => "US/EU Overlap",
            >= 13 and <= 17 => "European",
            >= 8 and <= 11 => "Asian",
            >= 1 and <= 3 => "Asian Pre-Market",
            _ => "Off-Hours"
        };
    }
}
```

---

### 14. Quote Update Frequency

**Definition**: How often order book is updated

**High Frequency** = High liquidity & activity
**Low Frequency** = Low liquidity & activity

**C# Implementation**:
```csharp
public class QuoteUpdateFrequencyIndicator
{
    private readonly Queue<DateTime> _updateTimestamps = new(100);

    public QuoteFrequencyMetrics Update(OrderBookSnapshot orderBook)
    {
        _updateTimestamps.Enqueue(DateTime.UtcNow);

        if (_updateTimestamps.Count > 100)
            _updateTimestamps.Dequeue();

        if (_updateTimestamps.Count < 2)
            return new QuoteFrequencyMetrics { UpdatesPerSecond = 0 };

        var duration = (_updateTimestamps.Last() - _updateTimestamps.First()).TotalSeconds;
        var updatesPerSecond = _updateTimestamps.Count / duration;

        return new QuoteFrequencyMetrics
        {
            UpdatesPerSecond = (decimal)updatesPerSecond,
            LiquidityLevel = updatesPerSecond > 10 ? "HIGH" :
                            updatesPerSecond > 5 ? "MEDIUM" : "LOW"
        };
    }
}
```

---

## Implementation Guide

### Complete Liquidity Service

```csharp
// File: backend/AlgoTrendy.TradingEngine/Services/LiquidityAnalysisService.cs

public class LiquidityAnalysisService
{
    private readonly ILogger<LiquidityAnalysisService> _logger;

    // All indicators
    private readonly SpreadIndicator _spreadIndicator;
    private readonly OrderBookDepthIndicator _depthIndicator;
    private readonly OrderBookImbalanceIndicator _obiIndicator;
    private readonly LiquidityGapDetector _gapDetector;
    private readonly VolumeProfileIndicator _volumeProfile;
    private readonly LiquidityTimeIndicator _timeIndicator;
    private readonly QuoteUpdateFrequencyIndicator _quoteFrequency;

    public CompleteLiquidityReport AnalyzeLiquidity(
        OrderBookSnapshot orderBook,
        List<Trade> recentTrades,
        DateTime timestamp)
    {
        var report = new CompleteLiquidityReport();

        // 1. Spread analysis
        report.Spread = _spreadIndicator.Calculate(orderBook);

        // 2. Depth analysis
        report.Depth = _depthIndicator.Calculate(orderBook, levels: 5);

        // 3. Order book imbalance
        report.OBI = _obiIndicator.Calculate(orderBook, levels: 5);

        // 4. Liquidity gaps
        report.Gaps = _gapDetector.DetectGaps(orderBook);

        // 5. Volume profile
        if (recentTrades != null && recentTrades.Any())
        {
            report.VolumeProfile = _volumeProfile.Calculate(recentTrades);
        }

        // 6. Time-based score
        report.TimeScore = _timeIndicator.GetTimeScore(timestamp);

        // 7. Quote frequency
        report.QuoteFrequency = _quoteFrequency.Update(orderBook);

        // Calculate overall liquidity score
        report.OverallScore = CalculateOverallLiquidityScore(report);

        // Generate recommendations
        report.Recommendations = GenerateRecommendations(report);

        return report;
    }

    private decimal CalculateOverallLiquidityScore(CompleteLiquidityReport report)
    {
        var scores = new List<decimal>();

        // Spread score (30%)
        var spreadScore = report.Spread.IsLiquid ? 1.0m : 0.5m;
        scores.Add(spreadScore * 0.30m);

        // Depth score (25%)
        var depthScore = report.Depth.TotalDepth > 1000000m ? 1.0m :
                        report.Depth.TotalDepth > 100000m ? 0.7m : 0.4m;
        scores.Add(depthScore * 0.25m);

        // OBI score (15%) - neutral is good
        var obiScore = Math.Abs(report.OBI.OBI) < 0.3m ? 1.0m : 0.6m;
        scores.Add(obiScore * 0.15m);

        // Gap score (20%)
        var gapScore = report.Gaps.Any(g => g.Severity == "HIGH") ? 0.3m :
                       report.Gaps.Any(g => g.Severity == "MEDIUM") ? 0.6m : 1.0m;
        scores.Add(gapScore * 0.20m);

        // Time score (10%)
        scores.Add(report.TimeScore.Score * 0.10m);

        return scores.Sum();
    }

    private List<string> GenerateRecommendations(CompleteLiquidityReport report)
    {
        var recommendations = new List<string>();

        // Low liquidity warnings
        if (report.OverallScore < 0.5m)
        {
            recommendations.Add("WARNING: Low overall liquidity - reduce position sizes");
        }

        // Spread warnings
        if (!report.Spread.IsLiquid)
        {
            recommendations.Add($"Wide spread ({report.Spread.PercentageSpread:P2}) - use limit orders");
        }

        // Gap warnings
        if (report.Gaps.Any(g => g.Severity == "HIGH"))
        {
            var highGaps = report.Gaps.Where(g => g.Severity == "HIGH").ToList();
            recommendations.Add($"CRITICAL: {highGaps.Count} high-severity liquidity gaps detected");
        }

        // OBI signals
        if (Math.Abs(report.OBI.OBI) > 0.5m)
        {
            recommendations.Add($"Strong {report.OBI.Pressure} pressure - consider directional bias");
        }

        // Time warnings
        if (report.TimeScore.Recommendation == "CAUTION")
        {
            recommendations.Add($"Low-liquidity time period ({report.TimeScore.Period})");
        }

        return recommendations;
    }
}

public class CompleteLiquidityReport
{
    public SpreadMetrics Spread { get; set; }
    public DepthMetrics Depth { get; set; }
    public OBIMetrics OBI { get; set; }
    public List<LiquidityGap> Gaps { get; set; }
    public VolumeProfile VolumeProfile { get; set; }
    public LiquidityTimeScore TimeScore { get; set; }
    public QuoteFrequencyMetrics QuoteFrequency { get; set; }
    public decimal OverallScore { get; set; }
    public List<string> Recommendations { get; set; }
}
```

---

## Integration with Strategies

### For Avellaneda-Stoikov Market Making

```csharp
// Use liquidity indicators to adjust spreads and order sizes

var liquidity = _liquidityService.AnalyzeLiquidity(orderBook, recentTrades, DateTime.UtcNow);

// Widen spreads in low liquidity
if (liquidity.OverallScore < 0.6m)
{
    spreadMultiplier = 1.5m;
}

// Reduce order size near liquidity gaps
if (liquidity.Gaps.Any(g => g.Severity == "HIGH"))
{
    orderSizeMultiplier = 0.5m;
}

// Don't place orders in gap zones
foreach (var gap in liquidity.Gaps.Where(g => g.Severity == "HIGH"))
{
    // Avoid placing orders between gap.StartPrice and gap.EndPrice
}
```

### For Pairs Trading

```csharp
// Check liquidity before entering pairs position

var liquidity1 = _liquidityService.AnalyzeLiquidity(orderBook1, trades1, DateTime.UtcNow);
var liquidity2 = _liquidityService.AnalyzeLiquidity(orderBook2, trades2, DateTime.UtcNow);

// Only enter if both assets have sufficient liquidity
if (liquidity1.OverallScore > 0.7m && liquidity2.OverallScore > 0.7m)
{
    // Safe to enter position
}
else
{
    // Skip entry due to low liquidity
}
```

### For Yost-Bremm RF HFT

```csharp
// Add liquidity indicators as features

features.SpreadPct = liquidity.Spread.PercentageSpread;
features.OrderBookDepth = liquidity.Depth.TotalDepth;
features.OBI = liquidity.OBI.OBI;
features.HasLiquidityGaps = liquidity.Gaps.Any(g => g.Severity == "HIGH") ? 1 : 0;
features.LiquidityScore = liquidity.OverallScore;
features.QuoteFrequency = liquidity.QuoteFrequency.UpdatesPerSecond;
```

---

## Summary

### Critical Liquidity Indicators for HFT

1. **Bid-Ask Spread** - Must be tight (<0.2%)
2. **Order Book Depth** - Minimum $100K at 5 levels
3. **Liquidity Gaps** - Avoid HIGH severity gaps
4. **Order Book Imbalance** - Directional bias indicator
5. **Time-of-Day Score** - Trade during high-liquidity hours
6. **Volume Participation** - Keep <5% of market volume

### Alert Thresholds

| Indicator | Warning | Critical |
|-----------|---------|----------|
| Spread % | >0.2% | >0.5% |
| Depth 5-levels | <$100K | <$50K |
| Liquidity Gaps | Medium | High |
| OBI Extreme | >0.5 | >0.7 |
| Overall Score | <0.6 | <0.4 |
| VPR | >5% | >10% |

---

**Reference Version**: 1.0
**Date**: 2025-10-21
**Usage**: Critical for all HFT strategy implementations
