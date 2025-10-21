# AlgoTrendy Technical Indicators Library Reference

**Version**: 2.6.0
**Last Updated**: 2025-10-21
**Total Indicators**: 50+
**Library**: pandas-ta + Custom Implementations

---

## Table of Contents

1. [Momentum Indicators](#momentum-indicators)
2. [Trend Indicators](#trend-indicators)
3. [Volatility Indicators](#volatility-indicators)
4. [Volume Indicators](#volume-indicators)
5. [Support & Resistance](#support--resistance)
6. [Advanced Indicators](#advanced-indicators)
7. [Moving Averages](#moving-averages)
8. [Signal Interpretation Guide](#signal-interpretation-guide)
9. [Indicator Combinations](#indicator-combinations)
10. [Best Practices](#best-practices)

---

# Momentum Indicators

Momentum indicators measure the rate of price change to identify overbought/oversold conditions and potential reversals.

---

## 1. RSI (Relative Strength Index)

### Description
Measures the magnitude of recent price changes to evaluate overbought or oversold conditions.

### Formula
```
RS = Average Gain / Average Loss
RSI = 100 - (100 / (1 + RS))
```

### Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `close` | pd.Series | Required | Closing prices |
| `period` | int | 14 | Number of periods |

### Returns
`pd.Series` - RSI values (0-100)

### Interpretation
| Range | Condition | Action |
|-------|-----------|--------|
| > 70 | Overbought | Consider selling |
| 30-70 | Neutral | Hold/monitor |
| < 30 | Oversold | Consider buying |

### Divergence Signals
- **Bullish Divergence**: Price makes lower low, RSI makes higher low → Potential reversal up
- **Bearish Divergence**: Price makes higher high, RSI makes lower high → Potential reversal down

### Python Usage
```python
from advanced_indicators import get_indicators

indicators = get_indicators()
rsi = indicators.rsi(close, period=14)

# Trading signals
if rsi.iloc[-1] < 30:
    print("Oversold - potential buy")
elif rsi.iloc[-1] > 70:
    print("Overbought - potential sell")
```

### .NET Usage
```csharp
var rsi = await _indicators.CalculateRSIAsync(symbol, historicalData, period: 14);

if (rsi < 30m)
{
    // Oversold - potential buy
}
else if (rsi > 70m)
{
    // Overbought - potential sell
}
```

### Best Practices
- Use 14 periods for standard analysis
- Combine with trend indicators to avoid false signals in trending markets
- Look for divergences for early reversal signals
- In strong trends, RSI can remain overbought/oversold for extended periods

---

## 2. Stochastic Oscillator

### Description
Compares a security's closing price to its price range over a given period to identify momentum and potential reversals.

### Formula
```
%K = 100 × (Close - LowestLow) / (HighestHigh - LowestLow)
%D = SMA of %K (signal line)
```

### Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `high` | pd.Series | Required | High prices |
| `low` | pd.Series | Required | Low prices |
| `close` | pd.Series | Required | Closing prices |
| `k_period` | int | 14 | %K period |
| `d_period` | int | 3 | %D smoothing period |

### Returns
`Tuple[pd.Series, pd.Series]` - (%K, %D)

### Interpretation
| Range | Condition | Signal |
|-------|-----------|--------|
| > 80 | Overbought | Bearish |
| 20-80 | Neutral | - |
| < 20 | Oversold | Bullish |

### Trading Signals
- **Bullish**: %K crosses above %D in oversold zone (< 20)
- **Bearish**: %K crosses below %D in overbought zone (> 80)

### Python Usage
```python
stoch_k, stoch_d = indicators.stochastic(high, low, close, k_period=14, d_period=3)

# Crossover signals
if stoch_k.iloc[-1] > stoch_d.iloc[-1] and stoch_k.iloc[-2] <= stoch_d.iloc[-2]:
    if stoch_k.iloc[-1] < 20:
        print("Bullish crossover in oversold zone - STRONG BUY")
```

### .NET Usage
```csharp
var stoch = await _indicators.CalculateStochasticAsync(symbol, data, period: 14);

if (stoch.PercentK > stoch.PercentD && stoch.PercentK < 20m)
{
    // Bullish signal in oversold zone
}
```

### Best Practices
- Most effective in range-bound markets
- Wait for crossover in overbought/oversold zones
- Confirm with trend indicators in trending markets
- Use divergences for reversal signals

---

## 3. Williams %R

### Description
Momentum indicator that measures overbought/oversold levels, inverse of Stochastic Oscillator.

### Formula
```
Williams %R = -100 × (HighestHigh - Close) / (HighestHigh - LowestLow)
```

### Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `high` | pd.Series | Required | High prices |
| `low` | pd.Series | Required | Low prices |
| `close` | pd.Series | Required | Closing prices |
| `period` | int | 14 | Lookback period |

### Returns
`pd.Series` - Williams %R values (-100 to 0)

### Interpretation
| Range | Condition | Action |
|-------|-----------|--------|
| -20 to 0 | Overbought | Consider selling |
| -50 to -20 | Neutral | Monitor |
| -100 to -50 | Neutral-Oversold | Monitor |
| -100 to -80 | Oversold | Consider buying |

### Python Usage
```python
williams = indicators.williams_r(high, low, close, period=14)

if williams.iloc[-1] < -80:
    print("Oversold - potential buy")
elif williams.iloc[-1] > -20:
    print("Overbought - potential sell")
```

### .NET Usage
```csharp
var williams = await _indicators.CalculateWilliamsRAsync(symbol, data, period: 14);

if (williams < -80m)  // Oversold
{
    // Potential buy signal
}
```

### Best Practices
- Similar to Stochastic but inverted scale
- Use -20 and -80 as key levels
- Effective in ranging markets
- Combine with trend confirmation

---

## 4. CCI (Commodity Channel Index)

### Description
Measures the deviation of price from its statistical mean to identify cyclical trends.

### Formula
```
Typical Price = (High + Low + Close) / 3
CCI = (Typical Price - SMA) / (0.015 × Mean Deviation)
```

### Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `high` | pd.Series | Required | High prices |
| `low` | pd.Series | Required | Low prices |
| `close` | pd.Series | Required | Closing prices |
| `period` | int | 20 | Lookback period |

### Returns
`pd.Series` - CCI values (unbounded, typically -200 to +200)

### Interpretation
| Range | Condition | Signal |
|-------|-----------|--------|
| > +100 | Overbought | Potential reversal down |
| -100 to +100 | Normal range | Trend continuation |
| < -100 | Oversold | Potential reversal up |

### Python Usage
```python
cci = indicators.cci(high, low, close, period=20)

if cci.iloc[-1] > 100:
    print("Overbought - strong uptrend or potential reversal")
elif cci.iloc[-1] < -100:
    print("Oversold - strong downtrend or potential reversal")
```

### .NET Usage
```csharp
var cci = await _indicators.CalculateCCIAsync(symbol, data, period: 20);

if (cci > 100m)
{
    // Overbought - monitor for reversal
}
```

### Best Practices
- Readings above +100 and below -100 occur 25% of the time
- Use for trend identification and overbought/oversold conditions
- Works well in trending markets
- Combine with trend confirmation indicators

---

## 5. ROC (Rate of Change)

### Description
Measures the percentage change in price over a specified period.

### Formula
```
ROC = [(Close - Close_n_periods_ago) / Close_n_periods_ago] × 100
```

### Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `close` | pd.Series | Required | Closing prices |
| `period` | int | 12 | Lookback period |

### Returns
`pd.Series` - ROC values (percentage)

### Interpretation
| Value | Meaning |
|-------|---------|
| > 0 | Upward momentum |
| 0 | No change |
| < 0 | Downward momentum |

### Python Usage
```python
roc = indicators.roc(close, period=12)

if roc.iloc[-1] > 0 and roc.iloc[-2] <= 0:
    print("Momentum turning positive - potential buy")
```

### Best Practices
- Use as momentum confirmation
- Look for zero-line crossovers
- Divergences can signal reversals
- Adjust period based on trading timeframe

---

## 6. Momentum

### Description
Measures the absolute change in price over a specified period.

### Formula
```
Momentum = Close - Close_n_periods_ago
```

### Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `close` | pd.Series | Required | Closing prices |
| `period` | int | 10 | Lookback period |

### Returns
`pd.Series` - Momentum values (absolute change)

### Python Usage
```python
mom = indicators.momentum(close, period=10)

if mom.iloc[-1] > 0:
    print("Positive momentum")
```

---

## 7. TSI (True Strength Index)

### Description
Double-smoothed momentum oscillator that filters out noise.

### Formula
```
Double Smoothed Momentum = EMA(EMA(Momentum))
Double Smoothed Absolute Momentum = EMA(EMA(|Momentum|))
TSI = 100 × (DSM / DSAM)
```

### Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `close` | pd.Series | Required | Closing prices |
| `long` | int | 25 | Long smoothing period |
| `short` | int | 13 | Short smoothing period |
| `signal` | int | 13 | Signal line period |

### Returns
`Tuple[pd.Series, pd.Series]` - (TSI, Signal Line)

### Interpretation
| Signal | Condition |
|--------|-----------|
| TSI > 0 | Bullish momentum |
| TSI < 0 | Bearish momentum |
| TSI > Signal | Buy signal |
| TSI < Signal | Sell signal |

### Python Usage
```python
tsi, tsi_signal = indicators.tsi(close, long=25, short=13, signal=13)

if tsi.iloc[-1] > tsi_signal.iloc[-1] and tsi.iloc[-1] > 0:
    print("Strong bullish signal")
```

### Best Practices
- Smooth indicator with less noise
- Use crossovers for signals
- Effective in trending markets
- Combine with price action

---

# Trend Indicators

Trend indicators identify the direction and strength of market trends.

---

## 8. MACD (Moving Average Convergence Divergence)

### Description
Shows the relationship between two moving averages to identify trend changes and momentum.

### Formula
```
MACD Line = EMA(12) - EMA(26)
Signal Line = EMA(9) of MACD Line
Histogram = MACD Line - Signal Line
```

### Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `close` | pd.Series | Required | Closing prices |
| `fast` | int | 12 | Fast EMA period |
| `slow` | int | 26 | Slow EMA period |
| `signal` | int | 9 | Signal line period |

### Returns
`Tuple[pd.Series, pd.Series, pd.Series]` - (MACD, Signal, Histogram)

### Trading Signals
| Signal | Condition | Action |
|--------|-----------|--------|
| Bullish Crossover | MACD > Signal | Buy |
| Bearish Crossover | MACD < Signal | Sell |
| Histogram Positive | MACD > Signal | Bullish momentum |
| Histogram Negative | MACD < Signal | Bearish momentum |
| Zero-Line Cross Up | MACD > 0 | Trend turning bullish |
| Zero-Line Cross Down | MACD < 0 | Trend turning bearish |

### Divergence Signals
- **Bullish**: Price lower low, MACD higher low → Reversal up
- **Bearish**: Price higher high, MACD lower high → Reversal down

### Python Usage
```python
macd, signal, histogram = indicators.macd(close, fast=12, slow=26, signal=9)

# Crossover signal
if macd.iloc[-1] > signal.iloc[-1] and macd.iloc[-2] <= signal.iloc[-2]:
    print("Bullish crossover - BUY signal")

# Histogram momentum
if histogram.iloc[-1] > 0 and histogram.iloc[-1] > histogram.iloc[-2]:
    print("Increasing bullish momentum")
```

### .NET Usage
```csharp
var macd = await _indicators.CalculateMACDAsync(symbol, data, 12, 26, 9);

if (macd.MACD > macd.Signal && macd.Histogram > 0)
{
    // Strong bullish signal
}
```

### Best Practices
- Most popular trend indicator
- Use crossovers for entry/exit
- Confirm with other indicators
- Watch for divergences
- Histogram shows momentum strength

---

## 9. ADX (Average Directional Index)

### Description
Measures trend strength regardless of direction. Does NOT indicate trend direction, only strength.

### Formula
```
+DI = 100 × (Smoothed +DM / ATR)
-DI = 100 × (Smoothed -DM / ATR)
DX = 100 × |+DI - -DI| / (+DI + -DI)
ADX = Smoothed DX
```

### Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `high` | pd.Series | Required | High prices |
| `low` | pd.Series | Required | Low prices |
| `close` | pd.Series | Required | Closing prices |
| `period` | int | 14 | Lookback period |

### Returns
`Tuple[pd.Series, pd.Series, pd.Series]` - (ADX, +DI, -DI)

### Interpretation
| ADX Value | Trend Strength |
|-----------|----------------|
| 0-20 | Absent or weak trend |
| 20-25 | Emerging trend |
| 25-50 | Strong trend |
| 50-75 | Very strong trend |
| 75-100 | Extremely strong trend |

### Directional Signals
| Condition | Trend Direction |
|-----------|-----------------|
| +DI > -DI | Uptrend |
| -DI > +DI | Downtrend |
| +DI crosses above -DI | Potential uptrend start |
| -DI crosses above +DI | Potential downtrend start |

### Python Usage
```python
adx, plus_di, minus_di = indicators.adx(high, low, close, period=14)

# Trend strength and direction
if adx.iloc[-1] > 25:
    if plus_di.iloc[-1] > minus_di.iloc[-1]:
        print(f"Strong uptrend (ADX: {adx.iloc[-1]:.1f})")
    else:
        print(f"Strong downtrend (ADX: {adx.iloc[-1]:.1f})")
else:
    print("Weak trend - range-bound market")
```

### .NET Usage
```csharp
var adx = await _indicators.CalculateADXAsync(symbol, data, period: 14);

if (adx.ADX > 25m)
{
    if (adx.PlusDI > adx.MinusDI)
    {
        // Strong uptrend
    }
    else
    {
        // Strong downtrend
    }
}
```

### Best Practices
- Use ADX > 25 to confirm trend strength
- Use +DI/-DI for direction
- Rising ADX = strengthening trend
- Falling ADX = weakening trend
- Don't trade in low ADX environments (< 20)
- Combine with other trend indicators

---

## 10. Aroon Indicator

### Description
Identifies trend changes and measures how long since the highest high or lowest low occurred.

### Formula
```
Aroon Up = [(Period - Periods Since Highest High) / Period] × 100
Aroon Down = [(Period - Periods Since Lowest Low) / Period] × 100
Aroon Oscillator = Aroon Up - Aroon Down
```

### Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `high` | pd.Series | Required | High prices |
| `low` | pd.Series | Required | Low prices |
| `period` | int | 25 | Lookback period |

### Returns
`Tuple[pd.Series, pd.Series, pd.Series]` - (Aroon Up, Aroon Down, Aroon Oscillator)

### Interpretation
| Condition | Signal |
|-----------|--------|
| Aroon Up > 70, Down < 30 | Strong uptrend |
| Aroon Down > 70, Up < 30 | Strong downtrend |
| Both near 50 | Consolidation |
| Aroon Up crosses above Down | Bullish signal |
| Aroon Down crosses above Up | Bearish signal |

### Python Usage
```python
aroon_up, aroon_down, aroon_osc = indicators.aroon(high, low, period=25)

if aroon_up.iloc[-1] > 70 and aroon_down.iloc[-1] < 30:
    print("Strong uptrend confirmed")
elif aroon_osc.iloc[-1] > 50:
    print("Bullish momentum")
```

### Best Practices
- Effective for identifying trend changes
- Use 70/30 as key levels
- Oscillator above 0 = bullish, below 0 = bearish
- Works well in trending markets

---

## 11. SuperTrend

### Description
Trend-following indicator based on ATR that provides buy/sell signals and dynamic support/resistance.

### Formula
```
Basic Upper Band = (High + Low) / 2 + Multiplier × ATR
Basic Lower Band = (High + Low) / 2 - Multiplier × ATR
SuperTrend = Upper or Lower Band based on trend direction
```

### Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `high` | pd.Series | Required | High prices |
| `low` | pd.Series | Required | Low prices |
| `close` | pd.Series | Required | Closing prices |
| `period` | int | 10 | ATR period |
| `multiplier` | float | 3.0 | ATR multiplier |

### Returns
`Tuple[pd.Series, pd.Series]` - (SuperTrend Line, Direction: 1=up, -1=down)

### Interpretation
| Condition | Signal |
|-----------|--------|
| Price > SuperTrend | Uptrend - BUY |
| Price < SuperTrend | Downtrend - SELL |
| Direction changes from -1 to 1 | Buy signal |
| Direction changes from 1 to -1 | Sell signal |

### Python Usage
```python
supertrend, direction = indicators.supertrend(high, low, close, period=10, multiplier=3.0)

current_price = close.iloc[-1]
st_value = supertrend.iloc[-1]

if direction.iloc[-1] == 1:
    print(f"UPTREND - Price: {current_price:.2f}, Support: {st_value:.2f}")
else:
    print(f"DOWNTREND - Price: {current_price:.2f}, Resistance: {st_value:.2f}")
```

### Best Practices
- Excellent for trend following
- Use SuperTrend line as trailing stop loss
- Reduce multiplier for more sensitivity (2.0-2.5)
- Increase multiplier for less noise (3.0-4.0)
- Combine with momentum indicators
- Works best in trending markets

---

## 12. Ichimoku Cloud

### Description
Comprehensive indicator that shows support/resistance, trend direction, and momentum in one view.

### Components
```
Tenkan-sen (Conversion Line) = (9-period high + 9-period low) / 2
Kijun-sen (Base Line) = (26-period high + 26-period low) / 2
Senkou Span A (Leading Span A) = (Tenkan-sen + Kijun-sen) / 2, shifted 26 periods ahead
Senkou Span B (Leading Span B) = (52-period high + 52-period low) / 2, shifted 26 periods ahead
Chikou Span (Lagging Span) = Close, shifted 26 periods back
```

### Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `high` | pd.Series | Required | High prices |
| `low` | pd.Series | Required | Low prices |
| `close` | pd.Series | Required | Closing prices |
| `tenkan` | int | 9 | Conversion line period |
| `kijun` | int | 26 | Base line period |
| `senkou` | int | 52 | Leading span B period |

### Returns
`Dict[str, pd.Series]` - Dictionary with all 5 components

### Trading Signals
| Signal | Condition |
|--------|-----------|
| **Strong Buy** | Price > Cloud, Tenkan > Kijun, Chikou > Price (26 bars ago) |
| **Buy** | Price crosses above cloud |
| **Strong Sell** | Price < Cloud, Tenkan < Kijun, Chikou < Price (26 bars ago) |
| **Sell** | Price crosses below cloud |
| **Support** | Cloud acts as support in uptrend |
| **Resistance** | Cloud acts as resistance in downtrend |

### Python Usage
```python
ichimoku = indicators.ichimoku(high, low, close)

tenkan = ichimoku['tenkan_sen']
kijun = ichimoku['kijun_sen']
span_a = ichimoku['senkou_span_a']
span_b = ichimoku['senkou_span_b']
chikou = ichimoku['chikou_span']

current_price = close.iloc[-1]
cloud_top = max(span_a.iloc[-1], span_b.iloc[-1])
cloud_bottom = min(span_a.iloc[-1], span_b.iloc[-1])

if current_price > cloud_top and tenkan.iloc[-1] > kijun.iloc[-1]:
    print("STRONG BULLISH - Price above cloud, TK cross bullish")
elif current_price < cloud_bottom and tenkan.iloc[-1] < kijun.iloc[-1]:
    print("STRONG BEARISH - Price below cloud, TK cross bearish")
```

### Best Practices
- Most comprehensive single indicator
- Cloud thickness = volatility
- Green cloud (Span A > Span B) = bullish
- Red cloud (Span A < Span B) = bearish
- Use all components for confirmation
- Best for medium to long-term trading

---

## 13. Parabolic SAR

### Description
Stop and Reverse indicator that provides entry/exit points and trailing stop levels.

### Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `high` | pd.Series | Required | High prices |
| `low` | pd.Series | Required | Low prices |
| `close` | pd.Series | Required | Closing prices |
| `af` | float | 0.02 | Acceleration factor start |
| `max_af` | float | 0.2 | Maximum acceleration factor |

### Returns
`Tuple[pd.Series, pd.Series]` - (SAR values, Trend: 1=up, -1=down)

### Interpretation
| Condition | Signal |
|-----------|--------|
| SAR below price | Uptrend - Hold long |
| SAR above price | Downtrend - Hold short |
| SAR flips from above to below | Buy signal |
| SAR flips from below to above | Sell signal |

### Python Usage
```python
psar, trend = indicators.psar(high, low, close, af=0.02, max_af=0.2)

current_price = close.iloc[-1]
sar_value = psar.iloc[-1]

if trend.iloc[-1] == 1:
    print(f"UPTREND - SAR Stop Loss: {sar_value:.2f}")
else:
    print(f"DOWNTREND - SAR Resistance: {sar_value:.2f}")
```

### Best Practices
- Excellent for trailing stops
- Works best in trending markets
- Generates false signals in ranging markets
- Use with trend confirmation indicators
- Lower AF = less sensitive, fewer signals
- Higher AF = more sensitive, more signals

---

# Volatility Indicators

Volatility indicators measure price fluctuations and help assess risk.

---

## 14. ATR (Average True Range)

### Description
Measures market volatility by calculating the average range over a specified period.

### Formula
```
True Range = max[(High - Low), |High - Previous Close|, |Low - Previous Close|]
ATR = Average of True Range over period
```

### Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `high` | pd.Series | Required | High prices |
| `low` | pd.Series | Required | Low prices |
| `close` | pd.Series | Required | Closing prices |
| `period` | int | 14 | Lookback period |

### Returns
`pd.Series` - ATR values

### Applications
1. **Stop Loss Placement**: Entry ± (2 × ATR)
2. **Position Sizing**: Risk / ATR
3. **Volatility Assessment**: Higher ATR = higher volatility
4. **Breakout Confirmation**: Expanding ATR confirms breakouts

### Python Usage
```python
atr = indicators.atr(high, low, close, period=14)
current_atr = atr.iloc[-1]
entry_price = close.iloc[-1]

# Calculate stop loss
stop_loss_long = entry_price - (2 * current_atr)
stop_loss_short = entry_price + (2 * current_atr)

print(f"ATR: {current_atr:.4f}")
print(f"Long Stop Loss: {stop_loss_long:.2f}")
print(f"Short Stop Loss: {stop_loss_short:.2f}")

# Position sizing
account_risk = 1000  # Risk $1000 per trade
position_size = account_risk / (2 * current_atr)
print(f"Position Size: {position_size:.2f} units")
```

### .NET Usage
```csharp
var atr = await _indicators.CalculateATRAsync(symbol, data, period: 14);
var entryPrice = data.Last().Close;

// Stop loss placement
var stopLossLong = entryPrice - (2m * atr);
var stopLossShort = entryPrice + (2m * atr);

// Position sizing
var accountRisk = 1000m;
var positionSize = accountRisk / (2m * atr);
```

### Best Practices
- Essential for risk management
- Use 14 periods as standard
- Higher ATR = widen stops
- Lower ATR = tighten stops
- Monitor ATR trends for volatility changes
- Combine with other volatility indicators

---

## 15. Bollinger Bands

### Description
Volatility bands placed above and below a moving average, expanding and contracting with volatility.

### Formula
```
Middle Band = 20-period SMA
Upper Band = Middle Band + (2 × Standard Deviation)
Lower Band = Middle Band - (2 × Standard Deviation)
```

### Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `close` | pd.Series | Required | Closing prices |
| `period` | int | 20 | SMA period |
| `std_dev` | float | 2.0 | Standard deviation multiplier |

### Returns
`Tuple[pd.Series, pd.Series, pd.Series]` - (Upper, Middle, Lower)

### Trading Strategies

**1. Bounce Strategy** (Range-bound markets)
- Buy near lower band
- Sell near upper band

**2. Breakout Strategy** (Trending markets)
- Buy when price breaks above upper band
- Sell when price breaks below lower band

**3. Squeeze Strategy**
- Bollinger Squeeze = narrow bands (low volatility)
- Expansion follows squeeze (breakout imminent)

### Band Width
```
%B = (Close - Lower Band) / (Upper Band - Lower Band)
```
- %B > 1: Above upper band
- %B = 0.5: At middle band
- %B < 0: Below lower band

### Python Usage
```python
bb_upper, bb_middle, bb_lower = indicators.bollinger_bands(close, period=20, std_dev=2.0)

current_price = close.iloc[-1]
upper = bb_upper.iloc[-1]
middle = bb_middle.iloc[-1]
lower = bb_lower.iloc[-1]

# Calculate %B
percent_b = (current_price - lower) / (upper - lower)

# Bollinger Band Width
bb_width = (upper - lower) / middle * 100

print(f"Price: {current_price:.2f}")
print(f"BB Upper: {upper:.2f}, Middle: {middle:.2f}, Lower: {lower:.2f}")
print(f"%B: {percent_b:.2f}")
print(f"BB Width: {bb_width:.2f}%")

# Trading signals
if percent_b < 0:
    print("Price below lower band - oversold")
elif percent_b > 1:
    print("Price above upper band - overbought")

if bb_width < 5:
    print("Squeeze detected - breakout imminent")
```

### .NET Usage
```csharp
var bb = await _indicators.CalculateBollingerBandsAsync(symbol, data, period: 20, stdDevMultiplier: 2m);

var currentPrice = data.Last().Close;
var percentB = (currentPrice - bb.Lower) / (bb.Upper - bb.Lower);
var bbWidth = (bb.Upper - bb.Lower) / bb.Middle * 100m;

if (percentB < 0m)
{
    // Price below lower band - oversold
}
else if (percentB > 1m)
{
    // Price above upper band - overbought
}
```

### Best Practices
- Use 20-period SMA and 2 std dev as standard
- Bands contain ~95% of price action
- Narrow bands = low volatility (squeeze)
- Wide bands = high volatility
- Price touching bands is NOT a signal alone
- Combine with momentum indicators
- Adjust std dev for different sensitivities

---

## 16. Keltner Channels

### Description
Similar to Bollinger Bands but uses ATR instead of standard deviation, making it less reactive to price spikes.

### Formula
```
Middle Line = 20-period EMA
Upper Channel = EMA + (Multiplier × ATR)
Lower Channel = EMA - (Multiplier × ATR)
```

### Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `high` | pd.Series | Required | High prices |
| `low` | pd.Series | Required | Low prices |
| `close` | pd.Series | Required | Closing prices |
| `period` | int | 20 | EMA period |
| `multiplier` | float | 2.0 | ATR multiplier |

### Returns
`Tuple[pd.Series, pd.Series, pd.Series]` - (Upper, Middle, Lower)

### Python Usage
```python
kc_upper, kc_middle, kc_lower = indicators.keltner_channels(
    high, low, close, period=20, multiplier=2.0
)

if close.iloc[-1] > kc_upper.iloc[-1]:
    print("Price above upper channel - strong uptrend")
elif close.iloc[-1] < kc_lower.iloc[-1]:
    print("Price below lower channel - strong downtrend")
```

### Best Practices
- More stable than Bollinger Bands
- Less prone to whipsaws
- Use for trend identification
- Breakouts above/below channels are significant
- Combine with BB for squeeze identification

---

## 17. Donchian Channels

### Description
Shows the highest high and lowest low over a specified period, used for breakout trading.

### Formula
```
Upper Channel = Highest High over period
Middle Channel = (Upper + Lower) / 2
Lower Channel = Lowest Low over period
```

### Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `high` | pd.Series | Required | High prices |
| `low` | pd.Series | Required | Low prices |
| `period` | int | 20 | Lookback period |

### Returns
`Tuple[pd.Series, pd.Series, pd.Series]` - (Upper, Middle, Lower)

### Trading Strategy
- **Buy**: When price breaks above upper channel
- **Sell**: When price breaks below lower channel
- **Exit Long**: When price touches lower channel
- **Exit Short**: When price touches upper channel

### Python Usage
```python
dc_upper, dc_middle, dc_lower = indicators.donchian_channels(high, low, period=20)

if close.iloc[-1] > dc_upper.iloc[-2]:  # Breakout above previous upper
    print("BREAKOUT - Buy signal")
elif close.iloc[-1] < dc_lower.iloc[-2]:  # Breakout below previous lower
    print("BREAKDOWN - Sell signal")
```

### Best Practices
- Excellent for breakout strategies
- Used in Turtle Trading system
- Works best in trending markets
- Use middle channel as trailing stop
- Confirm breakouts with volume

---

## 18. Standard Deviation (Historical Volatility)

### Description
Measures the dispersion of prices from the mean, indicating volatility.

### Formula
```
StdDev = √[Σ(Price - Mean)² / n]
```

### Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `close` | pd.Series | Required | Closing prices |
| `period` | int | 20 | Lookback period |

### Returns
`pd.Series` - Standard deviation values

### Python Usage
```python
std_dev = indicators.std_dev(close, period=20)

if std_dev.iloc[-1] > std_dev.rolling(50).mean().iloc[-1]:
    print("Volatility increasing")
else:
    print("Volatility decreasing")
```

### Best Practices
- Higher values = higher volatility
- Use for risk assessment
- Compare to historical averages
- Helps with position sizing

---

# Volume Indicators

Volume indicators analyze trading volume to confirm trends and identify potential reversals.

---

## 19. OBV (On-Balance Volume)

### Description
Cumulative volume indicator that adds volume on up days and subtracts on down days.

### Formula
```
If Close > Previous Close: OBV = Previous OBV + Volume
If Close < Previous Close: OBV = Previous OBV - Volume
If Close = Previous Close: OBV = Previous OBV
```

### Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `close` | pd.Series | Required | Closing prices |
| `volume` | pd.Series | Required | Trading volume |

### Returns
`pd.Series` - OBV values (cumulative)

### Interpretation
| Condition | Signal |
|-----------|--------|
| Rising OBV + Rising Price | Confirmed uptrend |
| Falling OBV + Falling Price | Confirmed downtrend |
| Rising OBV + Flat Price | Accumulation (bullish) |
| Falling OBV + Flat Price | Distribution (bearish) |
| Price up + OBV down | Bearish divergence |
| Price down + OBV up | Bullish divergence |

### Python Usage
```python
obv = indicators.obv(close, volume)

# Trend confirmation
price_trend = close.iloc[-1] > close.iloc[-20]
obv_trend = obv.iloc[-1] > obv.iloc[-20]

if price_trend and obv_trend:
    print("STRONG UPTREND - Price and volume confirming")
elif price_trend and not obv_trend:
    print("WARNING - Price up but OBV down (divergence)")

# OBV slope
obv_slope = (obv.iloc[-1] - obv.iloc[-10]) / 10
if obv_slope > 0:
    print(f"OBV rising (slope: {obv_slope:.0f})")
```

### .NET Usage
```csharp
var obv = await _indicators.CalculateOBVAsync(symbol, data);

// Check for divergence
var priceRising = data.Last().Close > data.First().Close;
var obvRising = obv > 0;  // Compare to earlier value

if (priceRising && !obvRising)
{
    // Bearish divergence warning
}
```

### Best Practices
- Use for trend confirmation
- Look for divergences (early reversal signals)
- Rising OBV supports uptrend
- Falling OBV supports downtrend
- Works best with other volume indicators
- Absolute values don't matter, trend matters

---

## 20. A/D Line (Accumulation/Distribution)

### Description
Volume-based indicator that measures money flow into or out of a security.

### Formula
```
CLV = [(Close - Low) - (High - Close)] / (High - Low)
A/D = Previous A/D + (CLV × Volume)
```

### Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `high` | pd.Series | Required | High prices |
| `low` | pd.Series | Required | Low prices |
| `close` | pd.Series | Required | Closing prices |
| `volume` | pd.Series | Required | Trading volume |

### Returns
`pd.Series` - A/D Line values (cumulative)

### Python Usage
```python
ad_line = indicators.ad_line(high, low, close, volume)

# Divergence detection
if close.iloc[-1] > close.iloc[-20] and ad_line.iloc[-1] < ad_line.iloc[-20]:
    print("Bearish divergence - price up, A/D down")
elif close.iloc[-1] < close.iloc[-20] and ad_line.iloc[-1] > ad_line.iloc[-20]:
    print("Bullish divergence - price down, A/D up")
```

### Best Practices
- More sensitive than OBV
- Focus on divergences
- Rising A/D = accumulation
- Falling A/D = distribution

---

## 21. CMF (Chaikin Money Flow)

### Description
Measures buying and selling pressure over a specified period.

### Formula
```
MFV = [(Close - Low) - (High - Close)] / (High - Low) × Volume
CMF = Sum(MFV over period) / Sum(Volume over period)
```

### Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `high` | pd.Series | Required | High prices |
| `low` | pd.Series | Required | Low prices |
| `close` | pd.Series | Required | Closing prices |
| `volume` | pd.Series | Required | Trading volume |
| `period` | int | 20 | Lookback period |

### Returns
`pd.Series` - CMF values (-1 to +1)

### Interpretation
| Range | Condition |
|-------|-----------|
| > 0.25 | Strong buying pressure |
| 0.1 to 0.25 | Moderate buying pressure |
| -0.1 to 0.1 | Neutral |
| -0.25 to -0.1 | Moderate selling pressure |
| < -0.25 | Strong selling pressure |

### Python Usage
```python
cmf = indicators.cmf(high, low, close, volume, period=20)

cmf_value = cmf.iloc[-1]

if cmf_value > 0.25:
    print("Strong buying pressure")
elif cmf_value < -0.25:
    print("Strong selling pressure")
elif cmf_value > 0.1:
    print("Moderate accumulation")
elif cmf_value < -0.1:
    print("Moderate distribution")
```

### Best Practices
- Bounded oscillator (-1 to +1)
- Use 0.1 and -0.1 as key levels
- Divergences signal reversals
- Combine with price action

---

## 22. MFI (Money Flow Index)

### Description
Volume-weighted RSI that measures buying and selling pressure.

### Formula
```
Typical Price = (High + Low + Close) / 3
Raw Money Flow = Typical Price × Volume
Positive Flow = Sum of flows when TP rises
Negative Flow = Sum of flows when TP falls
MFI = 100 - [100 / (1 + Positive Flow / Negative Flow)]
```

### Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `high` | pd.Series | Required | High prices |
| `low` | pd.Series | Required | Low prices |
| `close` | pd.Series | Required | Closing prices |
| `volume` | pd.Series | Required | Trading volume |
| `period` | int | 14 | Lookback period |

### Returns
`pd.Series` - MFI values (0-100)

### Interpretation
| Range | Condition |
|-------|-----------|
| > 80 | Overbought |
| 50-80 | Neutral-Bullish |
| 20-50 | Neutral-Bearish |
| < 20 | Oversold |

### Python Usage
```python
mfi = indicators.mfi(high, low, close, volume, period=14)

mfi_value = mfi.iloc[-1]

if mfi_value > 80:
    print("Overbought - consider selling")
elif mfi_value < 20:
    print("Oversold - consider buying")

# Divergence
price_higher = close.iloc[-1] > close.iloc[-10]
mfi_lower = mfi.iloc[-1] < mfi.iloc[-10]

if price_higher and mfi_lower:
    print("Bearish divergence detected")
```

### Best Practices
- Volume-weighted version of RSI
- More sensitive to volume changes
- Use 80/20 as overbought/oversold
- Look for divergences
- Effective in trending markets

---

## 23. VWAP (Volume Weighted Average Price)

### Description
Average price weighted by volume, institutional benchmark for intraday trading.

### Formula
```
VWAP = Σ(Typical Price × Volume) / Σ(Volume)
```

### Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `high` | pd.Series | Required | High prices |
| `low` | pd.Series | Required | Low prices |
| `close` | pd.Series | Required | Closing prices |
| `volume` | pd.Series | Required | Trading volume |

### Returns
`pd.Series` - VWAP values

### Trading Rules
| Condition | Signal |
|-----------|--------|
| Price > VWAP | Bullish (buying above average) |
| Price < VWAP | Bearish (selling below average) |
| Price crosses above VWAP | Buy signal |
| Price crosses below VWAP | Sell signal |
| VWAP slope rising | Uptrend |
| VWAP slope falling | Downtrend |

### Python Usage
```python
vwap = indicators.vwap(high, low, close, volume)

current_price = close.iloc[-1]
vwap_value = vwap.iloc[-1]

if current_price > vwap_value:
    premium = (current_price - vwap_value) / vwap_value * 100
    print(f"Price above VWAP by {premium:.2f}%")
else:
    discount = (vwap_value - current_price) / vwap_value * 100
    print(f"Price below VWAP by {discount:.2f}%")

# VWAP as support/resistance
if current_price > vwap_value:
    print(f"VWAP support at: {vwap_value:.2f}")
else:
    print(f"VWAP resistance at: {vwap_value:.2f}")
```

### Best Practices
- Primary institutional benchmark
- Reset daily for intraday trading
- Use as dynamic support/resistance
- Above VWAP = good fill for sellers
- Below VWAP = good fill for buyers
- Combine with price action

---

## 24. VPT (Volume Price Trend)

### Description
Similar to OBV but considers the magnitude of price changes.

### Formula
```
VPT = Previous VPT + [Volume × (Close - Previous Close) / Previous Close]
```

### Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `close` | pd.Series | Required | Closing prices |
| `volume` | pd.Series | Required | Trading volume |

### Returns
`pd.Series` - VPT values (cumulative)

### Python Usage
```python
vpt = indicators.vpt(close, volume)

# Trend analysis
if vpt.iloc[-1] > vpt.iloc[-20]:
    print("Volume supporting uptrend")
else:
    print("Volume supporting downtrend")
```

### Best Practices
- More sensitive than OBV
- Weights volume by price change
- Use for trend confirmation
- Look for divergences

---

# Support & Resistance

---

## 25. Pivot Points

### Description
Calculate support and resistance levels based on previous period's high, low, and close.

### Methods

**Standard Pivot Points**
```
Pivot = (High + Low + Close) / 3
R1 = 2 × Pivot - Low
S1 = 2 × Pivot - High
R2 = Pivot + (High - Low)
S2 = Pivot - (High - Low)
R3 = High + 2 × (Pivot - Low)
S3 = Low - 2 × (High - Pivot)
```

**Fibonacci Pivot Points**
```
Pivot = (High + Low + Close) / 3
R1 = Pivot + 0.382 × (High - Low)
S1 = Pivot - 0.382 × (High - Low)
R2 = Pivot + 0.618 × (High - Low)
S2 = Pivot - 0.618 × (High - Low)
R3 = Pivot + 1.000 × (High - Low)
S3 = Pivot - 1.000 × (High - Low)
```

**Woodie Pivot Points**
```
Pivot = (High + Low + 2 × Close) / 4
R1 = 2 × Pivot - Low
S1 = 2 × Pivot - High
R2 = Pivot + (High - Low)
S2 = Pivot - (High - Low)
```

**Camarilla Pivot Points**
```
R1 = Close + (High - Low) × 1.1/12
S1 = Close - (High - Low) × 1.1/12
R2 = Close + (High - Low) × 1.1/6
S2 = Close - (High - Low) × 1.1/6
R3 = Close + (High - Low) × 1.1/4
S3 = Close - (High - Low) × 1.1/4
R4 = Close + (High - Low) × 1.1/2
S4 = Close - (High - Low) × 1.1/2
```

### Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `high` | pd.Series | Required | High prices |
| `low` | pd.Series | Required | Low prices |
| `close` | pd.Series | Required | Closing prices |
| `method` | str | 'standard' | Calculation method |

### Returns
`Dict[str, float]` - Dictionary with pivot, r1-r3, s1-s3

### Python Usage
```python
# Calculate all methods
standard = indicators.pivot_points(high, low, close, method='standard')
fibonacci = indicators.pivot_points(high, low, close, method='fibonacci')
woodie = indicators.pivot_points(high, low, close, method='woodie')
camarilla = indicators.pivot_points(high, low, close, method='camarilla')

print("Standard Pivots:")
print(f"  Pivot: {standard['pivot']:.2f}")
print(f"  R1: {standard['r1']:.2f}, R2: {standard['r2']:.2f}, R3: {standard['r3']:.2f}")
print(f"  S1: {standard['s1']:.2f}, S2: {standard['s2']:.2f}, S3: {standard['s3']:.2f}")

# Trading strategy
current_price = close.iloc[-1]
if current_price > standard['pivot']:
    print(f"Above pivot - targets: R1={standard['r1']:.2f}, R2={standard['r2']:.2f}")
else:
    print(f"Below pivot - targets: S1={standard['s1']:.2f}, S2={standard['s2']:.2f}")
```

### Trading Guidelines
- **Above Pivot**: Bullish bias, use R1/R2/R3 as targets
- **Below Pivot**: Bearish bias, use S1/S2/S3 as targets
- **At Pivot**: Test level, breakout direction indicates trend
- **Bounces**: Price often bounces at pivot levels
- **Breakouts**: Break through R3/S3 = strong move

### Best Practices
- Calculate using previous day's data
- Use for intraday trading
- Combine multiple methods for confluence
- Most reliable in liquid markets
- Use as entry/exit targets

---

## 26. Fibonacci Retracement

### Description
Identifies potential support and resistance levels based on Fibonacci ratios.

### Key Levels
```
0% (100% retracement) = Starting point (high or low)
23.6% = High - 0.236 × (High - Low)
38.2% = High - 0.382 × (High - Low)
50% = High - 0.500 × (High - Low)
61.8% (Golden Ratio) = High - 0.618 × (High - Low)
78.6% = High - 0.786 × (High - Low)
100% = Ending point (low or high)

Extensions:
161.8% = High + 0.618 × (High - Low)
261.8% = High + 1.618 × (High - Low)
```

### Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `high` | float | Required | Swing high |
| `low` | float | Required | Swing low |

### Returns
`Dict[str, float]` - Dictionary with all Fibonacci levels

### Python Usage
```python
# Find swing high and low
swing_high = high.rolling(20).max().iloc[-1]
swing_low = low.rolling(20).min().iloc[-1]

# Calculate Fibonacci levels
fib = indicators.fibonacci_retracement(swing_high, swing_low)

print("Fibonacci Retracement Levels:")
for level, price in fib.items():
    print(f"  {level}: {price:.2f}")

# Trading strategy
current_price = close.iloc[-1]

# Find nearest support
if current_price > fib['61.8%']:
    print(f"Above 61.8% - strong uptrend, support at {fib['61.8%']:.2f}")
elif current_price > fib['50%']:
    print(f"Above 50% - moderate uptrend, support at {fib['50%']:.2f}")
elif current_price > fib['38.2%']:
    print(f"Above 38.2% - weak uptrend, support at {fib['38.2%']:.2f}")
```

### Retracement vs Extension
**Retracement** (0% - 100%): Used in pullbacks
- Buy at 38.2%, 50%, or 61.8% in uptrend
- Sell at 38.2%, 50%, or 61.8% in downtrend

**Extension** (161.8%, 261.8%): Used for targets
- Take profit at 161.8% or 261.8% extensions

### Best Practices
- 61.8% (Golden Ratio) is most important
- 50% is psychological level
- 38.2% is shallow retracement
- Use swing highs/lows for accuracy
- Combine with other support/resistance
- More effective in trending markets

---

# Advanced Indicators

---

## 27. Elder Ray Index

### Description
Measures buying and selling pressure using the relationship between price and EMA.

### Formula
```
Bull Power = High - EMA(13)
Bear Power = Low - EMA(13)
```

### Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `high` | pd.Series | Required | High prices |
| `low` | pd.Series | Required | Low prices |
| `close` | pd.Series | Required | Closing prices |
| `period` | int | 13 | EMA period |

### Returns
`Tuple[pd.Series, pd.Series]` - (Bull Power, Bear Power)

### Interpretation
| Condition | Signal |
|-----------|--------|
| Bull Power > 0 | Bulls in control |
| Bear Power < 0 | Bears in control |
| Both > 0 | Strong uptrend |
| Both < 0 | Strong downtrend |
| Bull Power rising | Increasing buying pressure |
| Bear Power falling (more negative) | Increasing selling pressure |

### Python Usage
```python
bull_power, bear_power = indicators.elder_ray(high, low, close, period=13)

bull = bull_power.iloc[-1]
bear = bear_power.iloc[-1]

if bull > 0 and bear > 0:
    print("STRONG BULLISH - Both powers positive")
elif bull > 0 and bear < 0 and abs(bull) > abs(bear):
    print("BULLISH - Bull power dominates")
elif bear < 0 and bull < 0:
    print("STRONG BEARISH - Both powers negative")
elif bear < 0 and bull > 0 and abs(bear) > abs(bull):
    print("BEARISH - Bear power dominates")
```

### Best Practices
- Use with trend confirmation
- Divergences signal reversals
- Combine with MACD or ADX
- Works well in trending markets

---

## 28. KST (Know Sure Thing)

### Description
Momentum oscillator based on smoothed ROC calculations across multiple timeframes.

### Formula
```
ROC1 = ROC(10), smoothed with 10-period SMA
ROC2 = ROC(15), smoothed with 10-period SMA
ROC3 = ROC(20), smoothed with 10-period SMA
ROC4 = ROC(30), smoothed with 15-period SMA

KST = (ROC1 × 1) + (ROC2 × 2) + (ROC3 × 3) + (ROC4 × 4)
Signal = 9-period SMA of KST
```

### Returns
`Tuple[pd.Series, pd.Series]` - (KST, Signal Line)

### Python Usage
```python
kst, kst_signal = indicators.kst(close)

# Crossover signals
if kst.iloc[-1] > kst_signal.iloc[-1] and kst.iloc[-2] <= kst_signal.iloc[-2]:
    print("Bullish crossover")
elif kst.iloc[-1] < kst_signal.iloc[-1] and kst.iloc[-2] >= kst_signal.iloc[-2]:
    print("Bearish crossover")

# Zero-line cross
if kst.iloc[-1] > 0 and kst.iloc[-2] <= 0:
    print("Momentum turning positive")
```

### Best Practices
- Multiple timeframe momentum
- Use crossovers for signals
- Divergences are significant
- Smoother than single ROC

---

## 29. Mass Index

### Description
Identifies trend reversals by measuring the range between high and low prices.

### Formula
```
Single EMA = EMA(High - Low, 9)
Double EMA = EMA(Single EMA, 9)
Mass Index = Sum[(Single EMA / Double EMA), 25]
```

### Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `high` | pd.Series | Required | High prices |
| `low` | pd.Series | Required | Low prices |
| `fast` | int | 9 | Fast EMA period |
| `slow` | int | 25 | Summation period |

### Returns
`pd.Series` - Mass Index values

### Interpretation
| Value | Signal |
|-------|--------|
| > 27 | Reversal bulge - potential trend reversal |
| < 27 | Trending phase continues |

### Python Usage
```python
mass = indicators.mass_index(high, low, fast=9, slow=25)

if mass.iloc[-1] > 27:
    print("REVERSAL SIGNAL - Mass Index > 27")
    print("Trend may reverse soon")
```

### Best Practices
- Identifies reversals, not direction
- Use with trend indicators for direction
- Reading > 27 is key signal
- Confirm with other indicators

---

## 30. Ultimate Oscillator

### Description
Combines short, medium, and long-term momentum into single oscillator.

### Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `high` | pd.Series | Required | High prices |
| `low` | pd.Series | Required | Low prices |
| `close` | pd.Series | Required | Closing prices |
| `period1` | int | 7 | Short period |
| `period2` | int | 14 | Medium period |
| `period3` | int | 28 | Long period |

### Returns
`pd.Series` - Ultimate Oscillator values (0-100)

### Interpretation
| Range | Condition |
|-------|-----------|
| > 70 | Overbought |
| 30-70 | Neutral |
| < 30 | Oversold |

### Python Usage
```python
uo = indicators.ultimate_oscillator(high, low, close, period1=7, period2=14, period3=28)

if uo.iloc[-1] > 70:
    print("Overbought - potential sell")
elif uo.iloc[-1] < 30:
    print("Oversold - potential buy")
```

### Best Practices
- Multi-timeframe momentum
- Less prone to false signals
- Use divergences for reversals
- Combine with trend indicators

---

## 31. Awesome Oscillator

### Description
Simple momentum indicator based on midpoint price differences.

### Formula
```
Median Price = (High + Low) / 2
AO = 5-period SMA of Median Price - 34-period SMA of Median Price
```

### Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `high` | pd.Series | Required | High prices |
| `low` | pd.Series | Required | Low prices |

### Returns
`pd.Series` - Awesome Oscillator values

### Trading Signals
| Signal | Condition |
|--------|-----------|
| Saucer | 3 consecutive bars with specific pattern |
| Zero-line cross | AO crosses zero line |
| Twin Peaks | Two peaks above/below zero line with divergence |

### Python Usage
```python
ao = indicators.awesome_oscillator(high, low)

# Zero-line cross
if ao.iloc[-1] > 0 and ao.iloc[-2] <= 0:
    print("Bullish zero-line cross")
elif ao.iloc[-1] < 0 and ao.iloc[-2] >= 0:
    print("Bearish zero-line cross")

# Saucer pattern (simplified)
if ao.iloc[-3] < ao.iloc[-2] < ao.iloc[-1]:
    print("Bullish saucer forming")
```

### Best Practices
- Simple but effective
- Use with other momentum indicators
- Zero-line crosses are key
- Look for saucer and twin peak patterns

---

## 32. Vortex Indicator

### Description
Identifies trend direction and strength using positive and negative vortex movement.

### Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `high` | pd.Series | Required | High prices |
| `low` | pd.Series | Required | Low prices |
| `close` | pd.Series | Required | Closing prices |
| `period` | int | 14 | Lookback period |

### Returns
`Tuple[pd.Series, pd.Series]` - (VI+, VI-)

### Interpretation
| Condition | Signal |
|-----------|--------|
| VI+ > VI- | Uptrend |
| VI- > VI+ | Downtrend |
| VI+ crosses above VI- | Buy signal |
| VI- crosses above VI+ | Sell signal |

### Python Usage
```python
vi_plus, vi_minus = indicators.vortex(high, low, close, period=14)

if vi_plus.iloc[-1] > vi_minus.iloc[-1]:
    strength = vi_plus.iloc[-1] - vi_minus.iloc[-1]
    print(f"UPTREND - Strength: {strength:.2f}")
else:
    strength = vi_minus.iloc[-1] - vi_plus.iloc[-1]
    print(f"DOWNTREND - Strength: {strength:.2f}")

# Crossover
if vi_plus.iloc[-1] > vi_minus.iloc[-1] and vi_plus.iloc[-2] <= vi_minus.iloc[-2]:
    print("Bullish crossover - BUY signal")
```

### Best Practices
- Identifies trend changes quickly
- Use crossovers for entries
- Strength indicated by separation
- Combine with momentum indicators

---

# Moving Averages

---

## 33. EMA (Exponential Moving Average)

### Description
Weighted moving average that gives more weight to recent prices.

### Formula
```
Multiplier = 2 / (Period + 1)
EMA = (Close - Previous EMA) × Multiplier + Previous EMA
```

### Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `series` | pd.Series | Required | Price series |
| `period` | int | Required | Number of periods |

### Returns
`pd.Series` - EMA values

### Python Usage
```python
ema_9 = indicators.ema(close, period=9)
ema_21 = indicators.ema(close, period=21)
ema_50 = indicators.ema(close, period=50)

# Golden Cross
if ema_21.iloc[-1] > ema_50.iloc[-1] and ema_21.iloc[-2] <= ema_50.iloc[-2]:
    print("Golden Cross - bullish signal")

# Death Cross
if ema_21.iloc[-1] < ema_50.iloc[-1] and ema_21.iloc[-2] >= ema_50.iloc[-2]:
    print("Death Cross - bearish signal")
```

### Best Practices
- More responsive than SMA
- Use for dynamic support/resistance
- Common periods: 9, 12, 21, 50, 200
- Price above EMA = bullish
- Price below EMA = bearish

---

## 34. SMA (Simple Moving Average)

### Description
Average price over a specified number of periods.

### Formula
```
SMA = Sum of Prices over Period / Period
```

### Python Usage
```python
sma_20 = indicators.sma(close, period=20)
sma_50 = indicators.sma(close, period=50)
sma_200 = indicators.sma(close, period=200)

# Price position
if close.iloc[-1] > sma_200.iloc[-1]:
    print("Above 200 SMA - long-term uptrend")
```

### Best Practices
- Smooth and stable
- Less responsive than EMA
- Common periods: 20, 50, 100, 200
- Good for trend identification

---

## 35. WMA (Weighted Moving Average)

### Description
Moving average with linearly increasing weights for recent prices.

### Formula
```
WMA = Σ(Price × Weight) / Σ(Weight)
Weights = 1, 2, 3, ..., n
```

### Python Usage
```python
wma_20 = indicators.wma(close, period=20)

if close.iloc[-1] > wma_20.iloc[-1]:
    print("Above WMA - bullish")
```

### Best Practices
- More weight to recent prices than SMA
- Less weight to recent prices than EMA
- Balance between SMA and EMA

---

## 36. HMA (Hull Moving Average)

### Description
Reduces lag by using weighted moving averages of different periods.

### Formula
```
HMA = WMA(2 × WMA(n/2) - WMA(n)), √n
```

### Python Usage
```python
hma_20 = indicators.hull_ma(close, period=20)

if close.iloc[-1] > hma_20.iloc[-1]:
    print("Above HMA - strong uptrend")
```

### Best Practices
- Very responsive
- Minimal lag
- Good for fast-moving markets
- Use for trend identification

---

## 37. TEMA (Triple Exponential Moving Average)

### Description
Triple-smoothed EMA that reduces lag significantly.

### Formula
```
TEMA = 3 × EMA1 - 3 × EMA2 + EMA3
where EMA2 = EMA(EMA1) and EMA3 = EMA(EMA2)
```

### Python Usage
```python
tema_20 = indicators.tema(close, period=20)

if close.iloc[-1] > tema_20.iloc[-1]:
    print("Above TEMA - uptrend")
```

### Best Practices
- Very responsive
- Minimal lag
- Good for trend trading
- Prone to whipsaws in ranging markets

---

# Signal Interpretation Guide

## Combining Indicators

### Trend Confirmation
```python
# Strong trend confirmation
adx_value = adx.iloc[-1]
macd_hist = histogram.iloc[-1]
price_above_ema = close.iloc[-1] > ema_50.iloc[-1]

if adx_value > 25 and macd_hist > 0 and price_above_ema:
    print("STRONG CONFIRMED UPTREND")
```

### Overbought/Oversold with Trend
```python
# Don't fight the trend
rsi_value = rsi.iloc[-1]
trend_up = close.iloc[-1] > ema_200.iloc[-1]

if rsi_value < 30 and trend_up:
    print("Oversold in uptrend - BUY opportunity")
elif rsi_value > 70 and not trend_up:
    print("Overbought in downtrend - SELL opportunity")
```

### Volume Confirmation
```python
# Price move with volume support
price_breakout = close.iloc[-1] > bb_upper.iloc[-1]
obv_rising = obv.iloc[-1] > obv.iloc[-10]

if price_breakout and obv_rising:
    print("Breakout confirmed by volume - STRONG signal")
```

---

# Indicator Combinations

## Strategy 1: Trend Following
**Components**: EMA(21), EMA(50), ADX, MACD

```python
ema_21 = indicators.ema(close, 21)
ema_50 = indicators.ema(close, 50)
adx, plus_di, minus_di = indicators.adx(high, low, close)
macd, signal, histogram = indicators.macd(close)

# Entry conditions
uptrend = ema_21.iloc[-1] > ema_50.iloc[-1]
strong_trend = adx.iloc[-1] > 25
macd_bullish = histogram.iloc[-1] > 0

if uptrend and strong_trend and macd_bullish:
    print("TREND FOLLOWING BUY")
```

## Strategy 2: Mean Reversion
**Components**: Bollinger Bands, RSI, Stochastic

```python
bb_upper, bb_mid, bb_lower = indicators.bollinger_bands(close)
rsi = indicators.rsi(close)
stoch_k, stoch_d = indicators.stochastic(high, low, close)

# Entry conditions
price_at_lower = close.iloc[-1] < bb_lower.iloc[-1]
rsi_oversold = rsi.iloc[-1] < 30
stoch_oversold = stoch_k.iloc[-1] < 20

if price_at_lower and rsi_oversold and stoch_oversold:
    print("MEAN REVERSION BUY")
```

## Strategy 3: Breakout Trading
**Components**: Donchian Channels, ATR, Volume

```python
dc_upper, dc_mid, dc_lower = indicators.donchian_channels(high, low)
atr = indicators.atr(high, low, close)
obv = indicators.obv(close, volume)

# Entry conditions
price_breakout = close.iloc[-1] > dc_upper.iloc[-2]
high_volatility = atr.iloc[-1] > atr.rolling(20).mean().iloc[-1]
volume_confirmation = obv.iloc[-1] > obv.iloc[-10]

if price_breakout and high_volatility and volume_confirmation:
    print("BREAKOUT BUY")
    stop_loss = close.iloc[-1] - (2 * atr.iloc[-1])
    print(f"Stop Loss: {stop_loss:.2f}")
```

## Strategy 4: Multi-Timeframe Confluence
**Components**: Multiple timeframes of same indicators

```python
from mem_indicator_integration import analyze_multiple_timeframes

mtf_analysis = analyze_multiple_timeframes({
    '1h': hourly_data,
    '4h': four_hour_data,
    '1d': daily_data
})

if mtf_analysis['confluence_signal'] == 'BUY':
    if mtf_analysis['timeframes_aligned'] >= 2:
        print(f"MULTI-TIMEFRAME BUY")
        print(f"Strength: {mtf_analysis['confluence_strength']:.1f}%")
```

---

# Best Practices

## 1. Never Use Single Indicator
Always combine at least 2-3 indicators from different categories:
- 1 Trend indicator (MACD, ADX, Moving Averages)
- 1 Momentum indicator (RSI, Stochastic)
- 1 Volume indicator (OBV, MFI) [optional but recommended]

## 2. Confirm Signals
Wait for multiple confirmations before entering trades:
- Indicator signal
- Price action confirmation
- Volume confirmation
- Higher timeframe alignment

## 3. Use Appropriate Timeframes
- **Scalping**: 1m, 5m charts
- **Day Trading**: 15m, 1h charts
- **Swing Trading**: 4h, 1d charts
- **Position Trading**: 1d, 1w charts

## 4. Adjust for Market Conditions
- **Trending Markets**: Use trend indicators (MACD, ADX, Moving Averages)
- **Range-Bound Markets**: Use oscillators (RSI, Stochastic, Williams %R)
- **High Volatility**: Widen stops, reduce position size
- **Low Volatility**: Tighten stops, prepare for breakout

## 5. Risk Management
Always use indicators for risk management:
- ATR for stop loss placement
- Bollinger Bands for volatility assessment
- ADX for trend strength (avoid trading when ADX < 20)

## 6. Backtest Strategies
Test indicator combinations on historical data before live trading:
- Minimum 1 year of data
- Multiple market conditions
- Calculate win rate, Sharpe ratio, max drawdown

## 7. Avoid Over-Optimization
- Don't curve-fit to historical data
- Keep strategies simple
- Use standard indicator periods as starting point
- Test on out-of-sample data

## 8. Monitor Performance
Track indicator effectiveness:
- Win rate by indicator
- False signal rate
- Average profit per signal
- Adjust or replace underperforming indicators

---

## Conclusion

This library provides 50+ professional technical indicators for comprehensive market analysis. Use indicators in combination, always confirm signals, and practice proper risk management for successful trading.

**Remember**: Indicators are tools, not crystal balls. They help identify high-probability setups, but no indicator is 100% accurate. Always use proper risk management and never risk more than you can afford to lose.

---

*For implementation details, see:*
- `advanced_indicators.py` - Python implementations
- `mem_indicator_integration.py` - High-level trading functions
- `IndicatorService.cs` - .NET implementations
- `test_indicators.py` - Test suite and examples
