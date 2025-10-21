# Technical Indicators Quick Reference Card

**AlgoTrendy v2.6** | **Total Indicators: 50+** | **Quick Lookup Guide**

---

## Momentum Indicators

| Indicator | Range | Overbought | Oversold | Best For |
|-----------|-------|------------|----------|----------|
| **RSI** | 0-100 | >70 | <30 | Reversals, divergences |
| **Stochastic** | 0-100 | >80 | <20 | Range-bound markets |
| **Williams %R** | -100 to 0 | >-20 | <-80 | Overbought/oversold |
| **CCI** | Unbounded | >+100 | <-100 | Cyclical trends |
| **ROC** | % | Positive | Negative | Momentum confirmation |
| **TSI** | -100 to +100 | - | - | Smoothed momentum |

**Quick Use**:
```python
rsi = indicators.rsi(close, 14)
if rsi < 30: BUY  # Oversold
if rsi > 70: SELL # Overbought
```

---

## Trend Indicators

| Indicator | Signal | Strength | Best For |
|-----------|--------|----------|----------|
| **MACD** | Crossovers | Histogram size | Trend changes |
| **ADX** | +DI/-DI | ADX value | Trend strength |
| **Aroon** | Up/Down lines | Oscillator | Trend identification |
| **SuperTrend** | Price vs line | ATR-based | Trailing stops |
| **Ichimoku** | Cloud position | Cloud thickness | Comprehensive analysis |
| **Parabolic SAR** | Dot position | Acceleration | Stop placement |

**Quick Use**:
```python
adx, plus_di, minus_di = indicators.adx(high, low, close)
if adx > 25 and plus_di > minus_di: STRONG_UPTREND
```

---

## Volatility Indicators

| Indicator | Measurement | Use Case | Typical Values |
|-----------|-------------|----------|----------------|
| **ATR** | Absolute | Stop loss, position sizing | 2-5% of price |
| **Bollinger Bands** | Std dev | Mean reversion | 2σ = 95% of data |
| **Keltner Channels** | ATR-based | Trend trading | Less volatile than BB |
| **Donchian** | High/Low | Breakouts | 20-period standard |
| **Std Dev** | Dispersion | Volatility level | Higher = more volatile |

**Quick Use**:
```python
atr = indicators.atr(high, low, close, 14)
stop_loss = entry_price - (2 * atr)  # 2 ATR stop
```

---

## Volume Indicators

| Indicator | Range | Positive | Negative | Best For |
|-----------|-------|----------|----------|----------|
| **OBV** | Cumulative | Rising | Falling | Trend confirmation |
| **A/D Line** | Cumulative | Rising | Falling | Money flow |
| **CMF** | -1 to +1 | >0.1 | <-0.1 | Buying/selling pressure |
| **MFI** | 0-100 | >80 | <20 | Volume-weighted RSI |
| **VWAP** | Price level | Above | Below | Institutional benchmark |
| **VPT** | Cumulative | Rising | Falling | Price-weighted volume |

**Quick Use**:
```python
mfi = indicators.mfi(high, low, close, volume, 14)
if mfi < 20: OVERSOLD_WITH_VOLUME
if mfi > 80: OVERBOUGHT_WITH_VOLUME
```

---

## Support & Resistance

| Method | Levels | Calculation | Best For |
|--------|--------|-------------|----------|
| **Pivot Points** | 7 (P, R1-R3, S1-S3) | Standard formula | Intraday trading |
| **Fibonacci** | 9 levels | 0%, 23.6%, 38.2%, 50%, 61.8%, 78.6%, 100%, 161.8%, 261.8% | Retracements, targets |

**Quick Use**:
```python
pivots = indicators.pivot_points(high, low, close, 'standard')
if price > pivots['pivot']: BULLISH_BIAS
targets = [pivots['r1'], pivots['r2'], pivots['r3']]
```

---

## Advanced Indicators

| Indicator | Type | Signal | Use Case |
|-----------|------|--------|----------|
| **Elder Ray** | Dual | Bull/Bear power | Pressure measurement |
| **KST** | Oscillator | Crossovers | Multi-timeframe momentum |
| **Mass Index** | Reversal | >27 | Trend reversals |
| **Ultimate Osc** | Oscillator | 0-100 | Multi-period momentum |
| **Awesome Osc** | Momentum | Zero-line cross | Simple momentum |
| **Vortex** | Dual lines | VI+ vs VI- | Trend direction |

---

## Moving Averages

| Type | Characteristics | Lag | Best For |
|------|-----------------|------|----------|
| **SMA** | Equal weights | High | Smooth trends |
| **EMA** | Recent weighted | Medium | Dynamic S/R |
| **WMA** | Linear weights | Medium | Balance |
| **HMA** | Low lag | Low | Fast markets |
| **TEMA** | Triple smoothed | Very Low | Trend trading |

**Common Periods**:
- **Short-term**: 9, 12, 20
- **Medium-term**: 50, 55
- **Long-term**: 100, 200

**Quick Use**:
```python
ema_20 = indicators.ema(close, 20)
ema_50 = indicators.ema(close, 50)

if ema_20 > ema_50: UPTREND  # Golden Cross
if ema_20 < ema_50: DOWNTREND  # Death Cross
```

---

## Python Quick Start

### Basic Indicators
```python
from advanced_indicators import get_indicators

indicators = get_indicators()

# Momentum
rsi = indicators.rsi(close, 14)
stoch_k, stoch_d = indicators.stochastic(high, low, close)

# Trend
macd, signal, hist = indicators.macd(close)
adx, plus_di, minus_di = indicators.adx(high, low, close)

# Volatility
atr = indicators.atr(high, low, close)
bb_u, bb_m, bb_l = indicators.bollinger_bands(close)

# Volume
obv = indicators.obv(close, volume)
mfi = indicators.mfi(high, low, close, volume)
```

### High-Level Analysis
```python
from mem_indicator_integration import (
    analyze_market,
    get_trading_signals,
    get_risk_metrics
)

# Complete analysis
analysis = analyze_market(data)
print(f"Signal: {analysis['overall_signal']}")  # BUY/SELL/NEUTRAL
print(f"Strength: {analysis['signal_strength']:.1f}%")

# Trading signals
signals = get_trading_signals(data)
print(f"Action: {signals['action']}")
print(f"Stop: {signals['stop_loss']}, Target: {signals['take_profit']}")

# Risk metrics
risk = get_risk_metrics(data)
print(f"ATR: {risk['atr']}, Risk Level: {risk['risk_level']}")
```

---

## .NET Quick Start

### Basic Indicators
```csharp
// Inject IndicatorService
private readonly IndicatorService _indicators;

// RSI
var rsi = await _indicators.CalculateRSIAsync(symbol, data, period: 14);

// MACD
var macd = await _indicators.CalculateMACDAsync(symbol, data);

// Stochastic
var stoch = await _indicators.CalculateStochasticAsync(symbol, data);

// ADX
var adx = await _indicators.CalculateADXAsync(symbol, data);

// ATR
var atr = await _indicators.CalculateATRAsync(symbol, data);

// Bollinger Bands
var bb = await _indicators.CalculateBollingerBandsAsync(symbol, data);

// Williams %R
var williams = await _indicators.CalculateWilliamsRAsync(symbol, data);

// CCI
var cci = await _indicators.CalculateCCIAsync(symbol, data);

// OBV
var obv = await _indicators.CalculateOBVAsync(symbol, data);

// Volume
var volatility = await _indicators.CalculateVolatilityAsync(symbol, data);

// MFI
var mfi = await _indicators.CalculateMFIAsync(symbol, data);

// VWAP
var vwap = await _indicators.CalculateVWAPAsync(symbol, data);

// EMA
var ema = await _indicators.CalculateEMAAsync(symbol, data, period: 20);

// SMA
var sma = await _indicators.CalculateSMAAsync(symbol, data, period: 50);
```

---

## Common Strategies

### 1. Trend Following
```python
# Confirm with multiple indicators
ema_20 > ema_50        # Uptrend
ADX > 25               # Strong trend
MACD histogram > 0     # Bullish momentum
→ BUY and hold until signals reverse
```

### 2. Mean Reversion
```python
# Oversold in range
RSI < 30               # Oversold
Price < BB Lower       # Below lower band
Stochastic < 20        # Oversold confirmation
→ BUY for bounce to middle band
```

### 3. Breakout Trading
```python
# Confirmed breakout
Price > Donchian Upper    # Breakout
ATR expanding             # Volatility increase
OBV rising                # Volume confirmation
→ BUY with stop at Donchian Lower
```

### 4. Divergence Trading
```python
# Reversal signal
Price: lower low       # Downtrend
RSI: higher low        # Bullish divergence
MACD: bullish cross    # Momentum confirmation
→ BUY for reversal trade
```

---

## Signal Strength Matrix

| Confluence | Strength | Action |
|------------|----------|--------|
| 1 indicator | Weak | Wait for confirmation |
| 2 indicators | Moderate | Consider entry |
| 3+ indicators | Strong | High-probability setup |
| Multi-timeframe | Very Strong | Best entries |

---

## Risk Management Rules

### Stop Loss Placement
```python
# ATR-based stops
Conservative: 2.0 × ATR
Standard: 1.5 × ATR
Aggressive: 1.0 × ATR

# Example
atr = indicators.atr(high, low, close, 14)
entry = 100.00
stop_loss = entry - (2.0 * atr)  # Conservative
```

### Position Sizing
```python
# Risk-based sizing
account_balance = 10000
risk_per_trade = 0.02  # 2%
risk_amount = account_balance * risk_per_trade

atr = indicators.atr(high, low, close, 14)
stop_distance = 2 * atr

position_size = risk_amount / stop_distance
```

### Take Profit Targets
```python
# Risk-reward ratios
Conservative: 1:2 (risk 1, target 2)
Standard: 1:3
Aggressive: 1:4

# Example
stop_distance = 5.00
take_profit_1 = entry + (stop_distance * 2)  # 1:2
take_profit_2 = entry + (stop_distance * 3)  # 1:3
```

---

## Troubleshooting

### False Signals
**Problem**: Too many false signals
**Solution**:
- Increase ADX threshold (require ADX > 25)
- Wait for multi-indicator confirmation
- Use higher timeframes
- Avoid trading in low ADX (< 20)

### Whipsaws
**Problem**: Getting stopped out frequently
**Solution**:
- Widen stops (use 2.5-3.0 × ATR)
- Use ATR filter (only trade when ATR is elevated)
- Confirm with trend indicators before entry

### Missing Trends
**Problem**: Indicators too slow
**Solution**:
- Use faster periods (RSI-9 instead of RSI-14)
- Add HMA or TEMA for faster signals
- Use smaller timeframes for entries

### Over-trading
**Problem**: Too many signals
**Solution**:
- Increase indicator thresholds
- Require 3+ indicator confirmation
- Only trade with-trend signals
- Use higher timeframes

---

## Performance Benchmarks

### Expected Win Rates by Strategy Type
- **Trend Following**: 40-50% (large winners compensate)
- **Mean Reversion**: 55-65% (smaller wins)
- **Breakout**: 35-45% (explosive winners)
- **Divergence**: 45-55% (medium wins)

### Sharpe Ratio Targets
- **Excellent**: > 2.0
- **Good**: 1.5 - 2.0
- **Acceptable**: 1.0 - 1.5
- **Poor**: < 1.0

### Max Drawdown Limits
- **Conservative**: < 10%
- **Moderate**: < 15%
- **Aggressive**: < 20%
- **Too High**: > 25%

---

## Testing & Validation

### Run Test Suite
```bash
cd /root/AlgoTrendy_v2.6/MEM
python3 test_indicators.py
# Should show: 3/3 tests passed (100%)
```

### Verify Installation
```python
from advanced_indicators import list_all_indicators

all_indicators = list_all_indicators()
total = sum(len(inds) for inds in all_indicators.values())
print(f"Total indicators available: {total}")
# Should show: 38+ methods
```

---

## Resources

- **Full Documentation**: `INDICATOR_LIBRARY_REFERENCE.md`
- **Usage Guide**: `INDICATORS_DOCUMENTATION.md`
- **Installation Summary**: `INDICATOR_INSTALLATION_SUMMARY.md`
- **Source Code**: `advanced_indicators.py`
- **Integration**: `mem_indicator_integration.py`
- **Tests**: `test_indicators.py`

---

## Support

For questions or issues:
1. Check full documentation: `INDICATOR_LIBRARY_REFERENCE.md`
2. Review examples: `test_indicators.py`
3. Read API docs: `INDICATORS_DOCUMENTATION.md`
4. Check AI context: `../AI_CONTEXT.md`

---

**Version**: 2.6.0 | **Last Updated**: 2025-10-21 | **Status**: ✅ Production Ready
