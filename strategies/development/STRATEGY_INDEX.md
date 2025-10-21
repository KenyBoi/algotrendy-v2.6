# AlgoTrendy Strategy Index
## Complete Catalog of All Trading Strategies

**Last Updated**: 2025-10-21
**Status**: Comprehensive index of all strategies across AlgoTrendy v2.6 and v2.5

---

## Table of Contents

1. [High-Frequency Trading Strategies (NEW - 10+ trades/day)](#high-frequency-trading-strategies)
2. [C# .NET Strategies (Backend)](#c-net-strategies)
3. [Python Research Strategies (Academic)](#python-research-strategies)
4. [Legacy v2.5 Strategies](#legacy-v25-strategies)
5. [External Integration Strategies](#external-integration-strategies)
6. [Strategy Migration Plan](#strategy-migration-plan)

---

## High-Frequency Trading Strategies

**Location**: `/root/AlgoTrendy_v2.6/strategies/development/`
**Status**: ✅ NEWLY RESEARCHED (2025-10-21)
**Implementation**: Full guides with code examples

### 1. Avellaneda-Stoikov Market Making + RL ⭐⭐⭐⭐⭐

**File**: `01_AVELLANEDA_STOIKOV_IMPLEMENTATION.md`
- **Type**: Market Making with Reinforcement Learning
- **Trades/Day**: 17,280+ (5-second action cycles)
- **Research**: PLOS ONE 2022 (peer-reviewed)
- **Complexity**: High
- **Timeline**: 4-6 weeks implementation
- **MEM Compatible**: ✅ Yes (inventory risk monitoring)
- **Status**: Ready for implementation
- **Key Features**:
  - Level 2 order book data
  - Double Deep Q-Network (RL)
  - 22 ML features
  - Superior Sharpe/Sortino ratios
- **Use Case**: Institutional-grade market making

### 2. Mean Reversion Pairs Trading ⭐⭐⭐⭐

**File**: `02_MEAN_REVERSION_PAIRS_TRADING.md`
- **Type**: Statistical Arbitrage
- **Trades/Day**: 10-50 (across 5-10 pairs)
- **Research**: Multiple academic papers (2015-2024)
- **Complexity**: Low-Medium
- **Timeline**: 2-3 weeks implementation
- **MEM Compatible**: ✅ Yes (cointegration monitoring)
- **Status**: Ready for implementation (RECOMMENDED FOR QUICK START)
- **Key Features**:
  - Cointegration-based pair selection
  - Z-score mean reversion
  - Market neutral (hedged)
  - 10x more trades vs single-coin
  - Max drawdown -29% vs -83% for BTC
- **Use Case**: Low-risk, market-neutral arbitrage

### 3. Yost-Bremm Random Forest HFT ⭐⭐⭐⭐

**File**: `03_YOST_BREMM_RANDOM_FOREST_HFT.md`
- **Type**: Machine Learning HFT
- **Trades/Day**: 20-96 (15-min to 1-min timeframes)
- **Research**: Journal of Computer Information Systems 2018
- **Complexity**: Medium-High
- **Timeline**: 6-8 weeks implementation
- **MEM Compatible**: ✅ Yes (accuracy monitoring)
- **Status**: Ready for implementation
- **Key Features**:
  - Random Forest classifier
  - Multi-exchange data (6+ exchanges)
  - 97% historical accuracy (degraded over time)
  - Sharpe ratio 8.22 (exceptional)
  - 40+ technical features
- **Use Case**: ML-based directional prediction

### Supporting Documentation

**File**: `04_LIQUIDITY_INDICATORS.md`
- **14 Liquidity Indicators**:
  1. Bid-Ask Spread
  2. Order Book Depth
  3. Order Book Imbalance (OBI)
  4. Cumulative Volume Delta (CVD)
  5. Microprice
  6. VWAP
  7. Amihud Illiquidity Ratio
  8. Volume Participation Rate
  9. Effective Spread
  10. Market Impact Coefficient
  11. Liquidity Gap Detection
  12. Volume Profile
  13. Liquidity Time Score
  14. Quote Update Frequency
- **Complete C# implementations**
- **Integration examples for all HFT strategies**

**File**: `00_RESEARCH_REPORT.md`
- Complete research findings
- Performance comparisons
- References to papers and repos

**File**: `README.md`
- Quick navigation
- Implementation roadmap
- Strategy comparison matrix

---

## C# .NET Strategies

**Location**: `/root/AlgoTrendy_v2.6/backend/AlgoTrendy.TradingEngine/Strategies/`
**Status**: ✅ PRODUCTION CODE (Active in v2.6)
**Language**: C#
**Framework**: .NET 8.0

### 4. RSI Strategy

**File**: `RSIStrategy.cs`
- **Type**: Mean Reversion / Oversold-Overbought
- **Indicator**: Relative Strength Index (14-period)
- **Entry**: RSI < 30 (oversold) or RSI > 70 (overbought)
- **Exit**: RSI crosses back to neutral zone
- **Status**: ✅ Active
- **Tests**: Unit tests in AlgoTrendy.Tests
- **MEM Compatible**: Yes
- **Key Features**:
  - Configurable RSI period
  - Multiple timeframe support
  - Async/await implementation
  - XML documentation

### 5. MACD Strategy

**File**: `MACDStrategy.cs`
- **Type**: Trend Following / Momentum
- **Indicator**: Moving Average Convergence Divergence
- **Entry**: MACD line crosses signal line
- **Exit**: Opposite crossover
- **Status**: ✅ Active
- **Tests**: Unit tests in AlgoTrendy.Tests
- **MEM Compatible**: Yes
- **Key Features**:
  - Standard MACD (12, 26, 9)
  - Histogram analysis
  - Divergence detection
  - Signal strength calculation

### 6. MFI Strategy

**File**: `MFIStrategy.cs`
- **Type**: Volume-Weighted RSI
- **Indicator**: Money Flow Index (14-period)
- **Entry**: MFI < 20 (oversold) or MFI > 80 (overbought)
- **Exit**: MFI returns to neutral
- **Status**: ✅ Active
- **Tests**: Unit tests in AlgoTrendy.Tests
- **MEM Compatible**: Yes
- **Key Features**:
  - Volume consideration
  - Divergence detection
  - Multiple timeframes

### 7. VWAP Strategy

**File**: `VWAPStrategy.cs`
- **Type**: Mean Reversion / Institutional
- **Indicator**: Volume-Weighted Average Price
- **Entry**: Price deviates from VWAP by threshold
- **Exit**: Price returns to VWAP
- **Status**: ✅ Active
- **Tests**: Unit tests in AlgoTrendy.Tests
- **MEM Compatible**: Yes
- **Key Features**:
  - Intraday reset (daily VWAP)
  - Distance-based signals
  - Volume-weighted execution

### 8. Momentum Strategy

**File**: `MomentumStrategy.cs`
- **Type**: Trend Following
- **Indicator**: Rate of Change (ROC)
- **Entry**: Strong momentum in direction
- **Exit**: Momentum weakens
- **Status**: ✅ Active
- **Tests**: Unit tests in AlgoTrendy.Tests
- **MEM Compatible**: Yes
- **Key Features**:
  - Multiple ROC periods
  - Momentum strength scoring
  - Trend confirmation

**Factory**: `Services/StrategyFactory.cs` - Instantiates all C# strategies
**Registry**: `Core/Services/StrategyRegistry` - Strategy registration system

---

## Python Research Strategies

**Location**: `/root/AlgoTrendy_v2.6/strategyGrpDev02/implementations/`
**Status**: ✅ ACADEMIC BASELINE IMPLEMENTATIONS
**Language**: Python 3.13
**Purpose**: Research and baseline testing

### 9. Volatility-Managed Momentum

**File**: `strategy1_vol_managed_momentum.py`
- **Type**: Academic Research Strategy
- **Paper**: "Momentum Has Its Moments" by Barroso & Santa-Clara (2015)
- **Entry**: 12-month momentum, skip last month
- **Position Sizing**: Inverse volatility weighting
- **Volatility Target**: 12% annualized
- **Rebalancing**: Monthly (end of month)
- **Status**: ✅ Baseline implemented (no MEM enhancements)
- **Tests**: Results in `strategyGrpDev02/results/strategy_1_baseline_results.json`
- **Key Parameters**:
  - Momentum lookback: 252 days
  - Skip period: 21 days
  - Volatility lookback: 126 days
  - Target volatility: 12%
- **Performance**: Documented in results file
- **MEM Enhancement Potential**: High (adaptive volatility targeting)

### 10. Statistical Arbitrage Pairs Trading

**File**: `strategy2_pairs_trading.py`
- **Type**: Academic Research Strategy
- **Paper**: Based on O-U process and cointegration research
- **Entry**: Z-score > ±2.0 (mean reversion)
- **Exit**: Z-score < 0.5 (return to mean)
- **Stop Loss**: Z-score > ±4.0
- **Rebalancing**: Daily
- **Status**: ✅ Baseline implemented (no MEM enhancements)
- **Key Parameters**:
  - Lookback: 60 days
  - Entry threshold: 2.0 σ
  - Exit threshold: 0.5 σ
  - Stop loss: 4.0 σ
  - Cointegration p-value: 0.05
- **Tests**: Uses Augmented Dickey-Fuller (ADF) test
- **MEM Enhancement Potential**: High (adaptive thresholds, pair discovery)
- **Note**: Similar to HFT strategy #2 but without MEM oversight

### 11. Currency Carry Trade

**File**: `strategy3_carry_trade.py`
- **Type**: Academic Research Strategy
- **Paper**: "The Carry Trade: Risks and Drawdowns" by Daniel, Hodrick & Lu (2017)
- **Entry**: Long top 3 currencies by interest rate, Short bottom 3
- **Position Sizing**: Equal-weighted
- **Rebalancing**: Monthly
- **Status**: ✅ Baseline implemented (no MEM enhancements)
- **Tests**: Results in `strategyGrpDev02/results/strategy_3_baseline_results.json`
- **Key Parameters**:
  - Num long: 3 currencies
  - Num short: 3 currencies
  - Rebalance: Monthly
- **Performance**: Documented in results file
- **MEM Enhancement Potential**: Medium (interest rate forecasting)
- **Note**: Requires forex/crypto pairs with interest rate differentials

**Archive**: Copies also in `/root/AlgoTrendy_v2.6/strategies/development/strategy_research_2025_q4/`

---

## Legacy v2.5 Strategies

**Location**: `/root/algotrendy_v2.5/algotrendy/strategy_resolver.py`
**Status**: ⚠️ LEGACY (migrated to v2.6)
**Language**: Python

### 12. Momentum Strategy (v2.5 Legacy)

**File**: `strategy_resolver.py` (class MomentumStrategy)
- **Type**: Basic Momentum
- **Entry**: Price change > threshold_buy (default 2%)
- **Exit**: Price change < threshold_sell (default -2%)
- **Volatility Filter**: < 15% volatility
- **Status**: ⚠️ Legacy (superseded by C# MomentumStrategy and HFT strategies)
- **Migration Status**: Functionality covered by v2.6 strategies
- **Recommendation**: Use C# MomentumStrategy.cs instead

**Note**: v2.5 strategy_resolver.py provides BaseStrategy abstract class framework that could be useful for future Python strategy development.

---

## External Integration Strategies

**Location**: `/root/AlgoTrendy_v2.6/integrations/strategies_external/`
**Status**: 🔌 EXTERNAL INTEGRATIONS
**Purpose**: Third-party strategy platforms

### 13. OpenAlgo Strategies

**Location**: `external_strategies/openalgo/strategies/examples/`
- **File**: `simple_ema_strategy.py`
- **Type**: Example EMA crossover
- **Status**: 🔌 External integration template
- **Purpose**: Demo for OpenAlgo platform integration

### 14. Plutus DeepMM Strategy

**Location**: `strategies_plutus/plutus_strategies/deepmm/src/strategy/asmodel.py`
- **Type**: Deep learning market making
- **Status**: 🔌 External integration (Plutus platform)
- **Note**: Advanced market making with neural networks

### 15. TradeMaster Strategies

**Location**: `external_strategies/TradeMaster/configs/finagent/exp/`
- **Files**: Multiple stock-specific strategies (AAPL, GOOGL, TSLA, AMZN)
- **Type**: FinAgent trading strategies
- **Status**: 🔌 External integration
- **Purpose**: Research and comparison

---

## Strategy Migration Plan

### Already Migrated ✅

1. **RSI, MACD, MFI, VWAP, Momentum** → C# strategies in v2.6 backend
2. **Volatility-Managed Momentum** → strategyGrpDev02 (baseline)
3. **Pairs Trading** → strategyGrpDev02 (baseline)
4. **Carry Trade** → strategyGrpDev02 (baseline)

### New HFT Strategies (Ready for Implementation) 🆕

1. **Avellaneda-Stoikov + RL** → Full implementation guide ready
2. **Mean Reversion Pairs Trading** → Enhanced version with MEM oversight
3. **Yost-Bremm RF HFT** → Full implementation guide ready
4. **Liquidity Indicators Suite** → Complete reference guide

### No Migration Needed ⚠️

- **v2.5 MomentumStrategy** → Superseded by v2.6 C# MomentumStrategy
- **External integrations** → Remain in integrations folder

---

## Strategy Locations Summary

### Active Strategies (Use These)

```
/root/AlgoTrendy_v2.6/
├── backend/AlgoTrendy.TradingEngine/Strategies/     # Production C# strategies
│   ├── RSIStrategy.cs                               # ✅ Active
│   ├── MACDStrategy.cs                              # ✅ Active
│   ├── MFIStrategy.cs                               # ✅ Active
│   ├── VWAPStrategy.cs                              # ✅ Active
│   └── MomentumStrategy.cs                          # ✅ Active
│
├── strategies/development/                          # HFT strategy guides
│   ├── 00_RESEARCH_REPORT.md                        # 🆕 Research findings
│   ├── 01_AVELLANEDA_STOIKOV_IMPLEMENTATION.md      # 🆕 Market making guide
│   ├── 02_MEAN_REVERSION_PAIRS_TRADING.md           # 🆕 Pairs trading guide
│   ├── 03_YOST_BREMM_RANDOM_FOREST_HFT.md           # 🆕 ML HFT guide
│   ├── 04_LIQUIDITY_INDICATORS.md                   # 🆕 Liquidity reference
│   ├── README.md                                    # 🆕 Navigation & roadmap
│   └── STRATEGY_INDEX.md                            # 📍 This file
│
└── strategyGrpDev02/implementations/                # Research baselines
    ├── strategy1_vol_managed_momentum.py            # ✅ Academic baseline
    ├── strategy2_pairs_trading.py                   # ✅ Academic baseline
    └── strategy3_carry_trade.py                     # ✅ Academic baseline
```

### Archive/Legacy (Reference Only)

```
/root/algotrendy_v2.5/
└── algotrendy/strategy_resolver.py                  # ⚠️ Legacy v2.5

/root/AlgoTrendy_v2.6/
├── archive/legacy_reference/v2.5_strategies/        # ⚠️ Archived
└── integrations/strategies_external/                # 🔌 External platforms
```

---

## Strategy Comparison Matrix

| Strategy | Type | Language | Trades/Day | Complexity | Status | MEM Ready |
|----------|------|----------|-----------|-----------|--------|-----------|
| **AS Market Making + RL** | Market Making | C# + Python | 17,280+ | High | 🆕 Ready | ✅ Yes |
| **Mean Reversion Pairs** | Stat Arb | C# + Python | 10-50 | Low-Med | 🆕 Ready | ✅ Yes |
| **Yost-Bremm RF HFT** | ML HFT | C# + Python | 20-96 | Med-High | 🆕 Ready | ✅ Yes |
| **RSI Strategy** | Mean Rev | C# | 1-5 | Low | ✅ Active | ✅ Yes |
| **MACD Strategy** | Trend | C# | 1-5 | Low | ✅ Active | ✅ Yes |
| **MFI Strategy** | Volume | C# | 1-5 | Low | ✅ Active | ✅ Yes |
| **VWAP Strategy** | Inst | C# | 1-10 | Low | ✅ Active | ✅ Yes |
| **Momentum Strategy** | Trend | C# | 1-5 | Low | ✅ Active | ✅ Yes |
| **Vol-Managed Momentum** | Academic | Python | Monthly | Medium | ✅ Baseline | 🔄 Enhanceable |
| **Pairs Trading (baseline)** | Academic | Python | Daily | Medium | ✅ Baseline | 🔄 Enhanceable |
| **Carry Trade** | Academic | Python | Monthly | Medium | ✅ Baseline | 🔄 Enhanceable |

---

## Recommended Strategy Portfolio

### For High-Frequency Trading (10+ trades/day)

**Recommended Combination**:
1. **Mean Reversion Pairs Trading** (Quick start, 2-3 weeks)
   - Low risk, market neutral
   - 10-20 trades/day initially
   - Scale to 5-10 pairs for 50+ trades/day

2. **Avellaneda-Stoikov Market Making** (After pairs proven)
   - Institutional-grade
   - 50-100+ trades/day
   - Continuous income from spreads

3. **Yost-Bremm RF HFT** (Optional enhancement)
   - ML-based directional prediction
   - 20-40 trades/day
   - Exceptional Sharpe potential

### For Traditional Trading (Intraday/Swing)

**Recommended Combination**:
1. **RSI Strategy** - Oversold/overbought entries
2. **MACD Strategy** - Trend confirmation
3. **VWAP Strategy** - Institutional price levels
4. **Momentum Strategy** - Trend following

### For Research & Backtesting

**Baseline Strategies**:
1. **Volatility-Managed Momentum** - Academic baseline
2. **Pairs Trading (baseline)** - Statistical arbitrage baseline
3. **Carry Trade** - Interest rate differential baseline

---

## Development Priorities

### Phase 1: Immediate (Weeks 1-3)
✅ HFT research complete
✅ Implementation guides complete
⬜ Start Mean Reversion Pairs Trading implementation

### Phase 2: Short-term (Weeks 4-9)
⬜ Implement Avellaneda-Stoikov Market Making
⬜ Integrate liquidity indicators
⬜ Deploy MEM oversight for both strategies

### Phase 3: Medium-term (Weeks 10-17)
⬜ Implement Yost-Bremm RF HFT
⬜ Enhance academic baseline strategies with MEM
⬜ Portfolio optimization across all strategies

---

## Strategy Development Guidelines

### When to Create New Strategy

1. **Research-backed**: Published paper or documented backtests
2. **Performance target**: Sharpe > 1.5, Max DD < -20%
3. **MEM compatible**: Can be monitored/enhanced by AI
4. **Unique value**: Not duplicating existing strategies
5. **Implementation feasible**: Data available, complexity manageable

### Strategy File Organization

```
strategies/development/
├── [NUMBER]_[STRATEGY_NAME].md           # Implementation guide
├── [STRATEGY_NAME]_baseline.py           # Academic baseline (Python)
├── [STRATEGY_NAME]_enhanced.py           # MEM-enhanced version (Python)
└── backend/.../Strategies/
    └── [StrategyName].cs                  # Production C# implementation
```

### Required Documentation

Each strategy must have:
1. Research background (papers, sources)
2. Performance metrics (backtested or published)
3. Implementation code (C# and/or Python)
4. MEM integration plan
5. Risk management rules
6. Test cases

---

## Quick Reference

### Finding a Strategy

**By Purpose**:
- **High-frequency trading** → strategies/development/ (guides 01-03)
- **Intraday/swing trading** → backend/.../Strategies/ (C# strategies)
- **Academic research** → strategyGrpDev02/implementations/
- **Liquidity analysis** → strategies/development/04_LIQUIDITY_INDICATORS.md

**By Programming Language**:
- **C# strategies** → backend/AlgoTrendy.TradingEngine/Strategies/
- **Python strategies** → strategyGrpDev02/implementations/
- **Python+C# HFT** → strategies/development/ (implementation guides)

**By Complexity**:
- **Beginner** → RSI, MACD, MFI strategies
- **Intermediate** → VWAP, Momentum, Mean Reversion Pairs
- **Advanced** → Avellaneda-Stoikov, Yost-Bremm RF
- **Expert** → Vol-Managed Momentum, Carry Trade

---

## Support & Resources

### Strategy Development Help

- **Implementation Guides**: `/root/AlgoTrendy_v2.6/strategies/development/`
- **Code Examples**: Backend C# strategies + Python baselines
- **Research Papers**: Referenced in 00_RESEARCH_REPORT.md
- **MEM Integration**: Each HFT guide includes MEM oversight examples

### Testing & Validation

- **Unit Tests**: `backend/AlgoTrendy.Tests/Unit/Strategies/`
- **Backtest Results**: `strategyGrpDev02/results/`
- **Backtest Engines**: `backend/AlgoTrendy.Backtesting/Engines/`

### Additional Documentation

- **Main README**: `/root/AlgoTrendy_v2.6/README.md`
- **AI Context**: `/root/AlgoTrendy_v2.6/AI_CONTEXT.md`
- **Architecture**: `/root/AlgoTrendy_v2.6/ARCHITECTURE_MAP.md`

---

**Index Version**: 1.0
**Last Updated**: 2025-10-21
**Total Strategies Cataloged**: 15 (5 C#, 3 Python baseline, 3 HFT guides, 4 external)
**Status**: Complete and comprehensive
