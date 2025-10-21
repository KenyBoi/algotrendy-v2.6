# Strategy Group Development 02 - Foundation Report

**Created**: October 21, 2025
**Project**: AlgoTrendy v2.6 - MEM Enhanced Trading Strategies
**Purpose**: Foundation document for implementing and testing 3 academically-validated strategies
**Status**: Forward Testing Phase

---

## Executive Summary

This document serves as the foundation for implementing and testing three trading strategies identified through comprehensive academic research. Each strategy has:

✅ **Published academic validation** (15+ years of backtests)
✅ **Open-source implementations** available
✅ **No AI/ML for execution** (manual/rule-based only)
✅ **High potential** for MEM enhancement (50-100% improvement expected)

---

## Strategy Portfolio Overview

| # | Strategy Name | Base Sharpe | MEM Est. Sharpe | Complexity | Priority |
|---|---------------|-------------|-----------------|------------|----------|
| 1 | Volatility-Managed Momentum | 0.97 | 1.5-1.8 | Medium | ⭐ High |
| 2 | Statistical Arbitrage Pairs | 1.5-2.5 | 3.0-4.5 | High | ⭐⭐⭐ Highest |
| 3 | Currency Carry Trade | 1.07 | 1.8-2.4 | Medium | ⭐⭐ Med |

**Expected Portfolio Performance** (conservative):
- Combined Sharpe Ratio: 2.0-2.5
- CAGR: 18-25%
- Maximum Drawdown: -15% to -20%
- Win Rate: 65-72%

---

## Strategy #1: Volatility-Managed Momentum

### Academic Foundation

**Paper**: "Momentum Has Its Moments"
**Authors**: Pedro Barroso & Pedro Santa-Clara
**Journal**: Journal of Financial Economics, Vol 116, Issue 1 (2015)
**Citations**: 1,000+
**Test Period**: 1927-2012 (85 years)

### Core Methodology

**Principle**: Scale momentum portfolio positions inversely to realized volatility to maintain constant risk exposure

**Formula**:
```
Position_Size = (Target_Vol / Realized_Vol) × Base_Position × Momentum_Signal

Where:
- Target_Vol = 12% annualized (typical)
- Realized_Vol = std(returns[-126:]) × sqrt(252)  # 6-month lookback
- Base_Position = 1.0 (100% capital)
- Momentum_Signal = sign(returns[-252:-21].sum())  # 12m momentum, skip 1m
```

### Published Performance Metrics

| Metric | Raw Momentum | Vol-Managed | Improvement |
|--------|-------------|-------------|-------------|
| Sharpe Ratio | 0.53 | 0.97 | +83% |
| CAGR | ~12% | ~17% | +42% |
| Max Drawdown | -96.69% | -45.20% | -53% |
| Min Monthly Return | -78.96% | -28.40% | -64% |
| Excess Kurtosis | 18.24 | 2.68 | -85% |

**Key Insight**: Volatility scaling virtually eliminates momentum crashes while nearly doubling Sharpe ratio

### Implementation Specifications

**Assets**: SPY, QQQ, or crypto equivalents (BTC, ETH)
**Rebalancing**: Monthly (end of month)
**Lookback Periods**:
- Momentum: 252 trading days (12 months)
- Skip period: 21 days (1 month) - avoids reversal
- Volatility: 126 trading days (6 months)

**Position Sizing Logic**:
```python
if momentum_12m > 0:
    direction = 1  # Long
elif momentum_12m < 0:
    direction = -1  # Short or cash
else:
    direction = 0  # Neutral

volatility_scalar = target_vol / realized_vol
position_size = direction * volatility_scalar * base_allocation
```

### MEM Enhancement Plan

**Layer 1 - Real-Time Regime Detection**:
- Monitor volatility intraday (vs. monthly rebalance)
- Classify market regime: calm/volatile/crash/recovery
- Adjust position sizing based on regime

**Layer 2 - Adaptive Lookback**:
- Learn optimal volatility lookback per regime
- Shorter lookback (20-60 days) in volatile markets
- Longer lookback (126+ days) in calm markets

**Layer 3 - ML Crash Prediction**:
- Use MEM's 78% accuracy reversal model
- Reduce exposure when crash probability > 65%
- Pre-emptive exit vs. reactive monthly rebalance

**Expected MEM Enhancement**:
- Sharpe: 0.97 → 1.5-1.8 (+55-85%)
- Max DD: -45.20% → -25% to -30% (-44%)
- Crash avoidance: +70% faster exits

---

## Strategy #2: Statistical Arbitrage Pairs Trading

### Academic Foundation

**Papers**:
1. "Statistical Arbitrage in the US Equities Market" (foundational)
2. "An Application of the Ornstein-Uhlenbeck Process to Pairs Trading" (arXiv:2412.12458, 2024)
3. Multiple open-source implementations on GitHub

**Real-World Usage**: Morgan Stanley proprietary trading (1980s-present)

### Core Methodology

**Principle**: Trade mean-reverting spreads between cointegrated asset pairs

**Two-Stage Process**:

**Stage 1 - Pair Selection**:
```python
# Test cointegration between asset pairs
for (asset_a, asset_b) in all_pairs:
    # Engle-Granger cointegration test
    residuals = asset_a - beta * asset_b
    adf_test = augmented_dickey_fuller(residuals)

    if adf_test.p_value < 0.05:
        # Pair is cointegrated
        valid_pairs.append((asset_a, asset_b))
```

**Stage 2 - Trading Logic**:
```python
# Calculate spread
spread = log(price_a / price_b)

# Calculate z-score
z_score = (spread - spread.mean()) / spread.std()

# Entry/Exit rules
if z_score > 2.0:
    # Spread too high - short A, long B
    return OPEN_SHORT_A_LONG_B
elif z_score < -2.0:
    # Spread too low - long A, short B
    return OPEN_LONG_A_SHORT_B
elif abs(z_score) < 0.5:
    # Mean reversion complete
    return CLOSE_POSITION
```

### Published Performance Metrics

**High-Frequency** (Morgan Stanley):
| Metric | Value |
|--------|-------|
| Annual Return | 50.5% |
| Sharpe Ratio | 8.14 |
| Max Drawdown | -8.3% |
| Win Rate | 65% |
| Trade Duration | 2-5 days |

**Daily Rebalancing**:
| Metric | Value |
|--------|-------|
| Annual Return | 15-20% |
| Sharpe Ratio | 1.5-2.5 |
| Win Rate | 55-65% |
| Max Drawdown | -20% to -30% |

**Crypto Implementation** (open-source):
| Metric | Value |
|--------|-------|
| Total Return | 96.64% |
| Sharpe Ratio | 1.42 |
| Max Drawdown | -31.98% |

### Implementation Specifications

**Assets**: Crypto pairs (BTC/ETH, ETH/BNB, etc.) or equity pairs
**Rebalancing**: Daily (end-of-day)
**Lookback Period**: 60 days for spread statistics

**Entry Thresholds**:
- Z-score > 2.0: Enter short spread
- Z-score < -2.0: Enter long spread

**Exit Thresholds**:
- abs(z-score) < 0.5: Close position (mean reversion)
- abs(z-score) > 4.0: Stop loss (cointegration breakdown)

**Position Sizing**:
```python
# Equal dollar allocation to each leg
position_a = capital * 0.5 / price_a
position_b = capital * 0.5 / price_b
```

### MEM Enhancement Plan

**Layer 1 - Dynamic Pair Discovery**:
- Continuously scan universe for new cointegrated pairs
- Backtest each pair before activation
- Auto-retire pairs when cointegration weakens

**Layer 2 - Adaptive Thresholds**:
- Learn optimal z-score thresholds per pair
- Wider thresholds in volatile regimes (2.5 vs 2.0)
- Tighter exit thresholds for high-confidence pairs

**Layer 3 - Real-Time Cointegration Monitoring**:
- Check cointegration every hour (vs daily)
- Exit immediately when p-value > 0.10
- ML predicts cointegration stability

**Layer 4 - Portfolio Optimization**:
- Dynamic capital allocation across pairs
- More capital to high-performing pairs
- Correlation-aware position sizing

**Expected MEM Enhancement**:
- Sharpe: 1.5-2.5 → 3.0-4.5 (+100%)
- Annual Return: 15-20% → 30-40% (+100%)
- Win Rate: 55-65% → 70-80% (+23%)
- Cointegration breakdown avoidance: 70-80%

---

## Strategy #3: Currency Carry Trade

### Academic Foundation

**Primary Papers**:
1. "The Carry Trade: Risks and Drawdowns" - Daniel, Hodrick & Lu (2017)
2. "Risk-Adjusted Return Managed Carry Trade" - Journal of Banking & Finance (2021)
3. "Carry Trade and Momentum in Currency Markets" - Burnside et al. (2011)

**Test Period**: 1976-2018 (40+ years)
**Markets**: 34-40 currency pairs

### Core Methodology

**Principle**: Borrow in low-interest-rate currencies, invest in high-interest-rate currencies

**Formula**:
```
Interest_Differential = Interest_Rate[currency] - Interest_Rate[base]

Ranking:
1. Calculate differential for all currencies vs. USD
2. Rank currencies by differential (high to low)
3. Long top 3 currencies (highest differentials)
4. Short bottom 3 currencies (lowest differentials)
5. Equal weight (1/3 each)
```

### Published Performance Metrics

**Baseline Carry Trade (HML-FX)**:
| Metric | Value |
|--------|-------|
| Sharpe Ratio | 0.76-0.78 |
| Annual Return | 4.6-6.0% |
| Test Period | 1976-2018 |

**Volatility-Adjusted**:
| Metric | Value |
|--------|-------|
| Sharpe Ratio | 0.84-0.99 |
| Improvement | +11-30% |

**Combined Signals (Forward Discount + Vol)**:
| Metric | Value |
|--------|-------|
| Sharpe Ratio | 1.07 |
| Improvement | +41% vs baseline |

**Dollar Carry (with predictability)**:
| Metric | Unconditional | With Prediction | Improvement |
|--------|--------------|----------------|-------------|
| Sharpe Ratio | 0.44 | 1.37 | +211% |

### Implementation Specifications

**Assets**: 10-20 currency pairs (majors: EUR, JPY, GBP, AUD, CAD, etc.)
**Rebalancing**: Monthly
**Position Sizing**: Equal-weighted (1/N)

**Entry Logic**:
```python
# Calculate interest rate differentials
differentials = {}
for currency in universe:
    diff = interest_rates[currency] - interest_rates['USD']
    differentials[currency] = diff

# Rank and select
sorted_currencies = sorted(differentials.items(), key=lambda x: x[1], reverse=True)

# Long top 3, short bottom 3
long_positions = sorted_currencies[:3]   # 33.3% each
short_positions = sorted_currencies[-3:]  # -33.3% each
```

**Exit Logic**:
- Monthly rebalancing only
- No intra-month stops
- Rerank and reposition monthly

### MEM Enhancement Plan

**Layer 1 - Real-Time Rate Monitoring**:
- Monitor central bank communications 24/7
- Predict rate changes before announcements
- Pre-position ahead of expected changes

**Layer 2 - Crash Risk Detection**:
- Monitor VIX, credit spreads, safe-haven flows
- ML predicts carry crash probability
- Reduce exposure when crash_prob > 65%

**Layer 3 - Dynamic Position Sizing**:
- Volatility-adjusted sizing per currency
- Higher allocation to stable high-yield currencies
- Lower allocation to volatile pairs

**Layer 4 - Multi-Factor Enhancement**:
- Combine carry + momentum + value signals
- Learn optimal factor weights per regime
- MEM adapts weights based on performance

**Expected MEM Enhancement**:
- Sharpe: 1.07 → 1.8-2.4 (+68-75%)
- Annual Return: 6-8% → 12-16% (+100%)
- Crash avoidance: 60-70%
- Max DD: -25-35% → -10-15% (-57%)

---

## Forward Testing Plan

### Testing Phases

**Phase 1: Baseline Implementation** (Week 1-2)
- Implement all 3 strategies WITHOUT MEM enhancements
- Use published parameters exactly as in research papers
- Run historical backtests to validate we can replicate published results
- **Success Criteria**: Achieve within 10% of published Sharpe ratios

**Phase 2: MEM Enhancement** (Week 3-4)
- Add MEM enhancements to each strategy
- Test MEM vs. baseline on same historical data
- **Success Criteria**: MEM Sharpe > Baseline Sharpe + 30%

**Phase 3: Paper Trading** (Week 5-10)
- Deploy all 6 versions (3 baseline + 3 MEM) to paper accounts
- Track real-time performance
- **Success Criteria**: 100+ trades, MEM wins on risk-adjusted returns

**Phase 4: Small Capital Live** (Week 11-16)
- Deploy $10K-$25K per strategy
- Monitor for slippage, execution issues
- **Success Criteria**: Maintain paper trading performance

### Testing Metrics

**Primary Metrics**:
1. Sharpe Ratio (risk-adjusted return)
2. CAGR (compound annual growth rate)
3. Maximum Drawdown
4. Win Rate

**Secondary Metrics**:
1. Sortino Ratio (downside deviation)
2. Calmar Ratio (CAGR / Max DD)
3. Profit Factor (gross profit / gross loss)
4. Average Win / Average Loss

**MEM-Specific Metrics**:
1. Regime classification accuracy
2. Crash prediction accuracy
3. Parameter adaptation frequency
4. Trade count (MEM vs baseline)

### Data Requirements

**Strategy #1 (Vol-Managed Momentum)**:
- Historical price data: 10+ years
- Frequency: Daily close prices
- Assets: SPY, QQQ, or BTC, ETH
- Minimum: 2,520 data points (10 years daily)

**Strategy #2 (Pairs Trading)**:
- Historical price data: 5+ years
- Frequency: Daily close prices
- Assets: 20-50 candidate pairs
- Minimum: 1,260 data points (5 years daily)

**Strategy #3 (Carry Trade)**:
- Historical FX rates: 10+ years
- Interest rate data: Central bank rates
- Frequency: Daily
- Assets: 10-20 currency pairs

### Risk Parameters

**Per-Strategy Limits**:
- Max position size: 100% of allocated capital
- Max leverage: 2x (pairs trading only)
- Max drawdown stop: -25% (halt trading)
- Daily loss limit: -5%

**Portfolio Limits**:
- Total capital allocation: 33.3% per strategy
- Correlation limit: <0.5 between strategies
- Portfolio max drawdown: -20%

---

## Implementation Architecture

### File Structure

```
/strategyGrpDev02/
├── reports/
│   ├── STRATEGY_FOUNDATIONS.md (this file)
│   ├── BACKTEST_RESULTS.md
│   └── PERFORMANCE_ANALYSIS.md
├── implementations/
│   ├── strategy1_vol_managed_momentum.py
│   ├── strategy2_pairs_trading.py
│   ├── strategy3_carry_trade.py
│   ├── strategy1_mem_enhanced.py
│   ├── strategy2_mem_enhanced.py
│   └── strategy3_mem_enhanced.py
├── backtests/
│   ├── run_backtest.py
│   ├── backtest_config.json
│   └── data/
│       ├── spy_historical.csv
│       ├── btc_eth_historical.csv
│       └── fx_rates_historical.csv
└── results/
    ├── strategy1_baseline_results.json
    ├── strategy1_mem_results.json
    ├── strategy2_baseline_results.json
    ├── strategy2_mem_results.json
    ├── strategy3_baseline_results.json
    └── strategy3_mem_results.json
```

### Technology Stack

**Language**: Python 3.10+
**Backtesting**: Custom framework + MEM integration
**Data**: yfinance, Alpha Vantage, QuestDB
**ML Models**: MEM's existing models (reversal_model.joblib)
**Visualization**: matplotlib, plotly

---

## Success Criteria Summary

### Baseline Validation (Week 1-2)
- [ ] Strategy #1 Sharpe within 10% of 0.97
- [ ] Strategy #2 Sharpe within 10% of 1.5-2.5
- [ ] Strategy #3 Sharpe within 10% of 1.07

### MEM Enhancement Validation (Week 3-4)
- [ ] Strategy #1 MEM Sharpe > 1.3 (baseline 0.97 + 30%)
- [ ] Strategy #2 MEM Sharpe > 2.0 (baseline 1.5 + 30%)
- [ ] Strategy #3 MEM Sharpe > 1.4 (baseline 1.07 + 30%)

### Paper Trading (Week 5-10)
- [ ] 100+ trades per strategy
- [ ] MEM wins on Sharpe in all 3 strategies
- [ ] Max drawdown < -25% for any strategy

### Live Deployment (Week 11-16)
- [ ] Slippage < 0.1% per trade
- [ ] Execution quality > 95%
- [ ] Performance matches paper trading (within 15%)

---

## Risk Warnings

### Strategy-Specific Risks

**Volatility-Managed Momentum**:
- ⚠️ Momentum crashes still possible (albeit reduced)
- ⚠️ Whipsaws in choppy markets
- ⚠️ Transaction costs can erode returns

**Pairs Trading**:
- ⚠️ Cointegration can break permanently
- ⚠️ Requires short-selling capability
- ⚠️ Capital-intensive (need margin)

**Carry Trade**:
- ⚠️ Susceptible to "risk-off" market crashes
- ⚠️ Interest rate changes can reverse quickly
- ⚠️ Currency crises can cause large losses

### General Risks

1. **Overfitting**: MEM may overfit to historical data
2. **Model Drift**: Market regimes can change
3. **Black Swans**: Unprecedented events not in training data
4. **Execution Risk**: Slippage, failed orders
5. **Liquidity Risk**: Inability to exit positions

---

## Next Steps

### Immediate (This Week)
1. ✅ Create directory structure
2. ✅ Document strategy foundations
3. ⏳ Implement baseline strategies
4. ⏳ Collect historical data

### Short-Term (Weeks 2-4)
1. Run baseline backtests
2. Implement MEM enhancements
3. Compare MEM vs baseline
4. Generate performance reports

### Medium-Term (Weeks 5-10)
1. Deploy to paper trading
2. Monitor real-time performance
3. Refine MEM parameters
4. Prepare for live deployment

---

## References

### Academic Papers
1. Barroso, P., & Santa-Clara, P. (2015). Journal of Financial Economics, 116(1), 111-120.
2. Daniel, K., Hodrick, R. J., & Lu, Z. (2017). Critical Finance Review, 6(2), 211-262.
3. Various statistical arbitrage papers (see main research report)

### Data Sources
- SSRN (academic papers)
- arXiv (quantitative finance)
- GitHub (open-source implementations)
- QuantPedia (strategy database)

---

**Document Version**: 1.0
**Last Updated**: October 21, 2025
**Status**: ✅ Foundation Complete - Ready for Implementation
**Next Document**: Implementation files and backtest results

---

*This document serves as the authoritative reference for Strategy Group Development 02. All implementations must adhere to the specifications outlined here.*
