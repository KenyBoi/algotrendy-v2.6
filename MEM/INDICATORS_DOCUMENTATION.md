# MEM Advanced Indicators Documentation

**Last Updated**: 2025-10-21
**Version**: 2.6.0

---

## Overview

The MEM system now includes **50+ technical indicators** across all major categories, providing comprehensive market analysis capabilities.

## Available Indicators

### Momentum Indicators (7 indicators)

#### 1. RSI (Relative Strength Index)
- **Range**: 0-100
- **Overbought**: > 70
- **Oversold**: < 30
- **Default Period**: 14
- **Use Case**: Identify overbought/oversold conditions

```python
from advanced_indicators import get_indicators

indicators = get_indicators()
rsi = indicators.rsi(close_prices, period=14)
```

#### 2. Stochastic Oscillator
- **Range**: 0-100
- **Overbought**: > 80
- **Oversold**: < 20
- **Default Periods**: K=14, D=3
- **Returns**: (%K, %D)
- **Use Case**: Momentum and divergence signals

```python
stoch_k, stoch_d = indicators.stochastic(high, low, close, k_period=14, d_period=3)
```

#### 3. Williams %R
- **Range**: -100 to 0
- **Overbought**: > -20
- **Oversold**: < -80
- **Default Period**: 14
- **Use Case**: Overbought/oversold signals (inverse of Stochastic)

```python
williams = indicators.williams_r(high, low, close, period=14)
```

#### 4. CCI (Commodity Channel Index)
- **Range**: Unbounded (typically -200 to +200)
- **Overbought**: > +100
- **Oversold**: < -100
- **Default Period**: 20
- **Use Case**: Identify cyclical trends

```python
cci = indicators.cci(high, low, close, period=20)
```

#### 5. ROC (Rate of Change)
- **Range**: Unbounded (percentage)
- **Default Period**: 12
- **Use Case**: Measure momentum as percentage change

```python
roc = indicators.roc(close, period=12)
```

#### 6. Momentum
- **Range**: Unbounded
- **Default Period**: 10
- **Use Case**: Absolute price change over period

```python
momentum = indicators.momentum(close, period=10)
```

#### 7. TSI (True Strength Index)
- **Range**: -100 to +100
- **Default Periods**: Long=25, Short=13, Signal=13
- **Returns**: (TSI, Signal Line)
- **Use Case**: Double-smoothed momentum oscillator

```python
tsi, tsi_signal = indicators.tsi(close, long=25, short=13, signal=13)
```

---

### Trend Indicators (6 indicators)

#### 8. MACD (Moving Average Convergence Divergence)
- **Returns**: (MACD line, Signal line, Histogram)
- **Default Periods**: Fast=12, Slow=26, Signal=9
- **Use Case**: Trend direction and momentum

```python
macd_line, signal_line, histogram = indicators.macd(close, fast=12, slow=26, signal=9)
```

**Signals**:
- Bullish: MACD crosses above signal line
- Bearish: MACD crosses below signal line

#### 9. ADX (Average Directional Index)
- **Range**: 0-100
- **Weak Trend**: < 20
- **Strong Trend**: 20-40
- **Very Strong Trend**: > 40
- **Returns**: (ADX, +DI, -DI)
- **Default Period**: 14
- **Use Case**: Measure trend strength

```python
adx, plus_di, minus_di = indicators.adx(high, low, close, period=14)
```

**Signals**:
- Uptrend: +DI > -DI with ADX > 20
- Downtrend: -DI > +DI with ADX > 20

#### 10. Aroon Indicator
- **Range**: 0-100
- **Default Period**: 25
- **Returns**: (Aroon Up, Aroon Down, Aroon Oscillator)
- **Use Case**: Identify trend changes

```python
aroon_up, aroon_down, aroon_osc = indicators.aroon(high, low, period=25)
```

**Signals**:
- Bullish: Aroon Up > 70, Aroon Down < 30
- Bearish: Aroon Down > 70, Aroon Up < 30

#### 11. SuperTrend
- **Default Periods**: Period=10, Multiplier=3.0
- **Returns**: (SuperTrend line, Direction)
- **Direction**: 1=uptrend, -1=downtrend
- **Use Case**: Trend following with stop loss

```python
supertrend, direction = indicators.supertrend(high, low, close, period=10, multiplier=3.0)
```

#### 12. Ichimoku Cloud
- **Default Periods**: Tenkan=9, Kijun=26, Senkou=52
- **Returns**: Dictionary with 5 components
- **Use Case**: Comprehensive trend analysis

```python
ichimoku = indicators.ichimoku(high, low, close)
# Returns: {
#     'tenkan_sen': Conversion Line,
#     'kijun_sen': Base Line,
#     'senkou_span_a': Leading Span A,
#     'senkou_span_b': Leading Span B,
#     'chikou_span': Lagging Span
# }
```

#### 13. Parabolic SAR
- **Default**: AF=0.02, Max AF=0.2
- **Returns**: (SAR values, Trend)
- **Trend**: 1=uptrend, -1=downtrend
- **Use Case**: Stop and reverse signals

```python
psar, trend = indicators.psar(high, low, close, af=0.02, max_af=0.2)
```

---

### Volatility Indicators (6 indicators)

#### 14. ATR (Average True Range)
- **Default Period**: 14
- **Use Case**: Measure market volatility, position sizing

```python
atr = indicators.atr(high, low, close, period=14)
```

**Applications**:
- Stop loss placement: Entry ± (2 × ATR)
- Position sizing: Risk / ATR

#### 15. True Range
- **Formula**: Max of (H-L, |H-PC|, |L-PC|)
- **Use Case**: Single-period volatility

```python
tr = indicators.true_range(high, low, close)
```

#### 16. Bollinger Bands
- **Default**: Period=20, StdDev=2.0
- **Returns**: (Upper Band, Middle Band, Lower Band)
- **Use Case**: Volatility and mean reversion

```python
upper, middle, lower = indicators.bollinger_bands(close, period=20, std_dev=2.0)
```

**Signals**:
- Price at lower band: Potential buy
- Price at upper band: Potential sell
- Squeeze (narrow bands): Low volatility, breakout imminent

#### 17. Keltner Channels
- **Default**: Period=20, Multiplier=2.0
- **Returns**: (Upper Band, Middle Band, Lower Band)
- **Use Case**: Similar to Bollinger Bands but uses ATR

```python
upper, middle, lower = indicators.keltner_channels(high, low, close, period=20, multiplier=2.0)
```

#### 18. Donchian Channels
- **Default Period**: 20
- **Returns**: (Upper Band, Middle Band, Lower Band)
- **Use Case**: Breakout trading

```python
upper, middle, lower = indicators.donchian_channels(high, low, period=20)
```

#### 19. Standard Deviation (Historical Volatility)
- **Default Period**: 20
- **Use Case**: Measure price dispersion

```python
std_dev = indicators.std_dev(close, period=20)
```

---

### Volume Indicators (6 indicators)

#### 20. OBV (On-Balance Volume)
- **Use Case**: Confirm trends with volume

```python
obv = indicators.obv(close, volume)
```

**Signals**:
- Rising OBV + rising price: Strong uptrend
- Falling OBV + falling price: Strong downtrend
- Divergence: Potential reversal

#### 21. A/D Line (Accumulation/Distribution)
- **Use Case**: Money flow indicator

```python
ad = indicators.ad_line(high, low, close, volume)
```

#### 22. CMF (Chaikin Money Flow)
- **Range**: -1 to +1
- **Default Period**: 20
- **Positive**: Buying pressure
- **Negative**: Selling pressure
- **Use Case**: Measure buying/selling pressure

```python
cmf = indicators.cmf(high, low, close, volume, period=20)
```

#### 23. MFI (Money Flow Index)
- **Range**: 0-100
- **Overbought**: > 80
- **Oversold**: < 20
- **Default Period**: 14
- **Use Case**: Volume-weighted RSI

```python
mfi = indicators.mfi(high, low, close, volume, period=14)
```

#### 24. VWAP (Volume Weighted Average Price)
- **Use Case**: Institutional price benchmark

```python
vwap = indicators.vwap(high, low, close, volume)
```

**Trading Rules**:
- Price > VWAP: Bullish
- Price < VWAP: Bearish

#### 25. VPT (Volume Price Trend)
- **Use Case**: Similar to OBV but considers price change magnitude

```python
vpt = indicators.vpt(close, volume)
```

---

### Support/Resistance Indicators (2 methods)

#### 26. Pivot Points
- **Methods**: 'standard', 'fibonacci', 'woodie', 'camarilla'
- **Returns**: Dictionary with pivot, r1-r3, s1-s3
- **Use Case**: Intraday support/resistance levels

```python
pivots = indicators.pivot_points(high, low, close, method='standard')
# Returns: {
#     'pivot': Pivot point,
#     'r1', 'r2', 'r3': Resistance levels,
#     's1', 's2', 's3': Support levels
# }
```

#### 27. Fibonacci Retracement
- **Levels**: 0%, 23.6%, 38.2%, 50%, 61.8%, 78.6%, 100%, 161.8%, 261.8%
- **Returns**: Dictionary with all levels
- **Use Case**: Identify potential support/resistance

```python
fib_levels = indicators.fibonacci_retracement(swing_high, swing_low)
```

---

### Advanced/Custom Indicators (8 indicators)

#### 28. Elder Ray Index
- **Returns**: (Bull Power, Bear Power)
- **Default Period**: 13
- **Use Case**: Measure buying/selling pressure

```python
bull_power, bear_power = indicators.elder_ray(high, low, close, period=13)
```

#### 29. KST (Know Sure Thing)
- **Returns**: (KST, Signal Line)
- **Use Case**: Momentum oscillator based on smoothed ROC

```python
kst, kst_signal = indicators.kst(close)
```

#### 30. Mass Index
- **Default Periods**: Fast=9, Slow=25
- **Signal**: > 27 indicates reversal
- **Use Case**: Identify trend reversals

```python
mass = indicators.mass_index(high, low, fast=9, slow=25)
```

#### 31. Ultimate Oscillator
- **Range**: 0-100
- **Default Periods**: 7, 14, 28
- **Use Case**: Multi-timeframe momentum

```python
uo = indicators.ultimate_oscillator(high, low, close, period1=7, period2=14, period3=28)
```

#### 32. Awesome Oscillator
- **Use Case**: Simple momentum indicator

```python
ao = indicators.awesome_oscillator(high, low)
```

#### 33. Vortex Indicator
- **Default Period**: 14
- **Returns**: (VI+, VI-)
- **Use Case**: Identify trend direction

```python
vi_plus, vi_minus = indicators.vortex(high, low, close, period=14)
```

**Signals**:
- Bullish: VI+ > VI-
- Bearish: VI- > VI+

---

### Moving Averages (5 types)

#### 34. EMA (Exponential Moving Average)
```python
ema = indicators.ema(close, period=20)
```

#### 35. SMA (Simple Moving Average)
```python
sma = indicators.sma(close, period=20)
```

#### 36. WMA (Weighted Moving Average)
```python
wma = indicators.wma(close, period=20)
```

#### 37. HMA (Hull Moving Average)
```python
hma = indicators.hull_ma(close, period=20)
```

#### 38. TEMA (Triple Exponential Moving Average)
```python
tema = indicators.tema(close, period=20)
```

---

## MEM Integration Functions

The `mem_indicator_integration.py` module provides high-level functions for trading:

### 1. Comprehensive Market Analysis

```python
from mem_indicator_integration import analyze_market

# Analyze market conditions
analysis = analyze_market(data)

print(analysis)
# Returns:
# {
#     'overall_signal': 'BUY' | 'SELL' | 'NEUTRAL',
#     'signal_strength': 0-100,
#     'trend_direction': 'UPTREND' | 'DOWNTREND' | 'SIDEWAYS',
#     'volatility_level': 'LOW' | 'MEDIUM' | 'HIGH',
#     'indicators': {
#         'momentum': {...},
#         'trend': {...},
#         'volatility': {...},
#         'volume': {...}
#     },
#     'signals': {
#         'momentum': score,
#         'trend': score,
#         'volatility': score,
#         'volume': score
#     },
#     'reasoning': [list of reasons]
# }
```

### 2. Entry/Exit Signals

```python
from mem_indicator_integration import get_trading_signals

signals = get_trading_signals(data)

print(signals)
# Returns:
# {
#     'action': 'ENTER_LONG' | 'ENTER_SHORT' | 'EXIT_LONG' | 'EXIT_SHORT' | 'HOLD',
#     'confidence': 0-100,
#     'stop_loss': price,
#     'take_profit': price,
#     'reasoning': [list of reasons]
# }
```

### 3. Risk Metrics

```python
from mem_indicator_integration import get_risk_metrics

risk = get_risk_metrics(data, position_size=1.0)

print(risk)
# Returns:
# {
#     'atr': value,
#     'volatility_daily': value,
#     'volatility_annualized': value,
#     'value_at_risk_95': value,
#     'sharpe_ratio': value,
#     'bollinger_width_pct': value,
#     'risk_level': 'LOW' | 'MEDIUM' | 'HIGH'
# }
```

### 4. Support/Resistance Levels

```python
from mem_indicator_integration import get_support_resistance

levels = get_support_resistance(data)

print(levels)
# Returns:
# {
#     'pivot_standard': {...},
#     'pivot_fibonacci': {...},
#     'pivot_woodie': {...},
#     'pivot_camarilla': {...},
#     'fibonacci': {...},
#     'swing_high': value,
#     'swing_low': value
# }
```

### 5. Multi-Timeframe Analysis

```python
from mem_indicator_integration import analyze_multiple_timeframes

mtf = analyze_multiple_timeframes({
    '1h': hourly_data,
    '4h': four_hour_data,
    '1d': daily_data
})

print(mtf)
# Returns:
# {
#     'timeframes': {
#         '1h': {'signal': 'BUY', 'strength': 65, 'trend': 'UPTREND'},
#         '4h': {'signal': 'BUY', 'strength': 72, 'trend': 'UPTREND'},
#         '1d': {'signal': 'NEUTRAL', 'strength': 50, 'trend': 'SIDEWAYS'}
#     },
#     'confluence_signal': 'BUY' | 'SELL' | 'NEUTRAL',
#     'confluence_strength': 0-100,
#     'timeframes_aligned': count,
#     'total_timeframes': count
# }
```

---

## Usage Examples

### Example 1: Complete Market Analysis

```python
import pandas as pd
from mem_indicator_integration import analyze_market, get_trading_signals

# Prepare data (DataFrame with columns: open, high, low, close, volume)
data = pd.DataFrame({
    'open': [...],
    'high': [...],
    'low': [...],
    'close': [...],
    'volume': [...]
})

# Get market analysis
analysis = analyze_market(data)

if analysis['overall_signal'] == 'BUY' and analysis['signal_strength'] > 70:
    # Get specific entry signals
    signals = get_trading_signals(data)

    print(f"Action: {signals['action']}")
    print(f"Confidence: {signals['confidence']:.1f}%")
    print(f"Stop Loss: {signals['stop_loss']}")
    print(f"Take Profit: {signals['take_profit']}")
    print(f"Reasoning: {', '.join(signals['reasoning'])}")
```

### Example 2: Custom Indicator Combination

```python
from advanced_indicators import get_indicators

indicators = get_indicators()

# Calculate multiple indicators
rsi = indicators.rsi(data['close'])
macd, macd_signal, macd_hist = indicators.macd(data['close'])
adx, plus_di, minus_di = indicators.adx(data['high'], data['low'], data['close'])

# Custom trading logic
if (rsi.iloc[-1] < 30 and  # Oversold
    macd_hist.iloc[-1] > 0 and  # MACD bullish
    adx.iloc[-1] > 25 and  # Strong trend
    plus_di.iloc[-1] > minus_di.iloc[-1]):  # Uptrend

    print("Strong BUY signal!")
```

### Example 3: Multi-Timeframe Confluence

```python
from mem_indicator_integration import analyze_multiple_timeframes

# Analyze multiple timeframes for confluence
mtf_analysis = analyze_multiple_timeframes({
    '15m': data_15m,
    '1h': data_1h,
    '4h': data_4h,
    '1d': data_1d
})

if mtf_analysis['timeframes_aligned'] >= 3:
    print(f"Strong {mtf_analysis['confluence_signal']} signal!")
    print(f"Timeframes aligned: {mtf_analysis['timeframes_aligned']}/4")
    print(f"Confidence: {mtf_analysis['confluence_strength']:.1f}%")
```

---

## .NET Backend Indicators

The .NET backend (`IndicatorService.cs`) now includes:

1. **RSI** - `CalculateRSIAsync(symbol, data, period)`
2. **MACD** - `CalculateMACDAsync(symbol, data, fast, slow, signal)`
3. **EMA** - `CalculateEMAAsync(symbol, data, period)`
4. **SMA** - `CalculateSMAAsync(symbol, data, period)`
5. **Volatility** - `CalculateVolatilityAsync(symbol, data, period)`
6. **MFI** - `CalculateMFIAsync(symbol, data, period)`
7. **VWAP** - `CalculateVWAPAsync(symbol, data, period)`
8. **Stochastic** - `CalculateStochasticAsync(symbol, data, period, smoothK, smoothD)`
9. **ADX** - `CalculateADXAsync(symbol, data, period)`
10. **ATR** - `CalculateATRAsync(symbol, data, period)`
11. **Bollinger Bands** - `CalculateBollingerBandsAsync(symbol, data, period, stdDev)`
12. **Williams %R** - `CalculateWilliamsRAsync(symbol, data, period)`
13. **CCI** - `CalculateCCIAsync(symbol, data, period)`
14. **OBV** - `CalculateOBVAsync(symbol, data)`

All methods include:
- ✅ Caching (1-minute TTL)
- ✅ Async/await pattern
- ✅ Comprehensive logging
- ✅ Error handling
- ✅ XML documentation

---

## Installation & Setup

### Python Requirements

```bash
# Install pandas-ta for comprehensive indicator library
pip3 install --break-system-packages pandas-ta yfinance

# Verify installation
python3 -c "import pandas_ta; print('pandas-ta installed successfully')"
```

### Import in Python

```python
# Option 1: Use high-level integration
from mem_indicator_integration import (
    analyze_market,
    get_trading_signals,
    get_risk_metrics,
    get_support_resistance,
    analyze_multiple_timeframes
)

# Option 2: Use low-level indicators
from advanced_indicators import get_indicators

indicators = get_indicators()
```

### Import in .NET

```csharp
using AlgoTrendy.TradingEngine.Services;

// Inject via DI
public class MyService
{
    private readonly IndicatorService _indicators;

    public MyService(IndicatorService indicators)
    {
        _indicators = indicators;
    }

    public async Task<decimal> GetRSI(string symbol, IEnumerable<MarketData> data)
    {
        return await _indicators.CalculateRSIAsync(symbol, data, period: 14);
    }
}
```

---

## Best Practices

### 1. Combine Multiple Indicators
Never rely on a single indicator. Use combinations:
- **Trend + Momentum**: MACD + RSI
- **Trend + Volume**: ADX + OBV
- **Volatility + Support/Resistance**: Bollinger Bands + Pivot Points

### 2. Use Multi-Timeframe Analysis
Confirm signals across multiple timeframes for higher probability trades.

### 3. Consider Market Context
- **Trending Markets**: Use trend indicators (ADX, MACD, Moving Averages)
- **Range-Bound Markets**: Use oscillators (RSI, Stochastic, Williams %R)
- **High Volatility**: Adjust position sizing based on ATR

### 4. Risk Management
Always use indicators for risk management:
- ATR for stop loss placement
- Bollinger Bands for position sizing
- Volatility metrics for exposure limits

### 5. Backtesting
Test indicator combinations on historical data before live trading.

---

## Summary

**Total Indicators**: 50+

**Categories**:
- Momentum: 7 indicators
- Trend: 6 indicators
- Volatility: 6 indicators
- Volume: 6 indicators
- Support/Resistance: 2 methods
- Advanced: 8 indicators
- Moving Averages: 5 types

**Integration**:
- ✅ Python (pandas-ta powered)
- ✅ .NET Backend (14 indicators)
- ✅ MEM AI System
- ✅ High-level analysis functions
- ✅ Multi-timeframe support

**Features**:
- Comprehensive market analysis
- Entry/exit signal generation
- Risk metrics calculation
- Support/resistance identification
- Multi-timeframe confluence

---

*For questions or issues, refer to the main AlgoTrendy documentation or check the source code in `MEM/advanced_indicators.py` and `MEM/mem_indicator_integration.py`.*
