# Custom Backtest Engine - Currently Disabled

**Status:** ❌ DISABLED
**Date:** 2025-10-20
**Reason:** Pending accuracy verification

---

## Overview

The Custom Backtesting Engine has been disabled to prevent use until accuracy can be verified against known-good implementations.

## Changes Made

### 1. UI Status Updated
- **File:** `backend/AlgoTrendy.Backtesting/Models/BacktestModels.cs:488`
- **Change:** Status set to "disabled" in BacktestingEngines list
- **Effect:** UI will show Custom Engine as disabled

### 2. Validation Block
- **File:** `backend/AlgoTrendy.Backtesting/Engines/CustomBacktestEngine.cs:37-42`
- **Change:** ValidateConfig() now returns error for Custom engine
- **Error Message:** "Custom Engine is currently disabled. Please use QuantConnect engine instead. The Custom engine requires accuracy verification before use."

### 3. Runtime Block
- **File:** `backend/AlgoTrendy.Backtesting/Engines/CustomBacktestEngine.cs:45-59`
- **Change:** RunAsync() immediately returns Failed status
- **Effect:** Even if validation is bypassed, execution will fail safely
- **Original Code:** Commented out (lines 61-158)

---

## Verification Requirements

Before re-enabling the Custom Engine, the following verification must be completed:

### Required Tests (8-12 days estimated)

| Component | Verification Method | Priority |
|-----------|-------------------|----------|
| **Technical Indicators** | Compare SMA, EMA, RSI, MACD, Bollinger Bands, ATR, Stochastic, OBV against TA-Lib or TradingView | HIGH |
| **Performance Metrics** | Validate Sharpe Ratio, Sortino Ratio, Max Drawdown calculations against reference implementations | HIGH |
| **Trade Execution** | Verify entry/exit signals, commission/slippage application, position sizing | HIGH |
| **Strategy Logic** | Confirm SMA crossover signals match expected behavior | HIGH |
| **Integration Tests** | Run parallel backtests (Custom vs QuantConnect) on same data and compare results | CRITICAL |
| **Edge Cases** | Test empty data, single trade, zero volatility, division by zero | MEDIUM |

### Quick Verification (2-3 days)

Run **parallel comparison tests** using both engines:
- Test on 3-5 different symbols (BTC, ETH, AAPL, etc.)
- Same date ranges
- Same parameters
- Compare: Final equity, trade count, Sharpe ratio, max drawdown
- **Success Criteria:** Results match within 1-2%

---

## Components in Custom Engine

### Technical Indicators (Custom Implementations)
- SMA (Simple Moving Average)
- EMA (Exponential Moving Average)
- RSI (Relative Strength Index)
- MACD (Moving Average Convergence Divergence)
- Bollinger Bands
- ATR (Average True Range)
- Stochastic Oscillator
- OBV (On-Balance Volume)

### Performance Metrics
- Total Return & Annualized Return
- Sharpe Ratio (risk-adjusted return)
- Sortino Ratio (downside risk-adjusted)
- Maximum Drawdown
- Win Rate & Profit Factor
- Average Win/Loss
- Largest Win/Loss
- Trade Duration Analysis

### Strategy
- **Type:** SMA Crossover (20/50 period)
- **Entry:** When fast SMA crosses above slow SMA
- **Exit:** When fast SMA crosses below slow SMA
- **Position Size:** 95% of available cash
- **Costs:** Commission + Slippage applied

---

## Re-enabling the Custom Engine

To re-enable after verification:

1. **Complete verification tests** (see above)
2. **Document test results** with evidence
3. **Update this file** with verification status
4. **Restore code:**
   - Set status to "available" in BacktestModels.cs:488
   - Restore original ValidateConfig() logic
   - Uncomment RunAsync() implementation (lines 61-158)
   - Remove blocking code (lines 49-59)

---

## Alternative: Use QuantConnect

The **QuantConnect** integration is fully implemented and production-ready:
- **Status:** ✅ COMPLETE
- **Location:** `backend/AlgoTrendy.Backtesting/Services/QuantConnectApiClient.cs`
- **Requirements:** User ID and API Token from QuantConnect account
- **Benefits:** Industry-standard platform, proven accuracy, extensive features

### Setup QuantConnect
1. Create account at https://www.quantconnect.com
2. Get API credentials from https://www.quantconnect.com/account
3. Configure in `appsettings.json` or user secrets:
   ```json
   "QuantConnect": {
       "UserId": "your-user-id",
       "ApiToken": "your-api-token",
       "BaseUrl": "https://www.quantconnect.com/api/v2",
       "TimeoutSeconds": 300
   }
   ```

---

## Questions?

For questions about this decision or verification process, contact the development team.
