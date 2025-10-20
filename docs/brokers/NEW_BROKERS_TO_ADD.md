# New Brokers to Add - Expansion Plan

**Date:** October 20, 2025
**Current Count:** 11 brokers
**Target Count:** 19 brokers (+8 new)
**Status:** Planning Complete

---

## Brokers to Add

### ✅ **Already Implemented (Need to Port from v2.5)**

#### 1. **Composer.trade** - DeFi/DEX Aggregator
**File Found:** `/root/algotrendy_v2.5/Brokers/composer_trade_integration.py` (655 lines)
**Status:** ✅ Complete in v2.5, needs C# port
**Type:** DeFi/DEX Trading Platform

**What It Does:**
- Multi-chain DEX aggregator (Ethereum, Polygon, Arbitrum, Optimism, Base, Avalanche)
- Aggregated liquidity (Uniswap, SushiSwap, 1inch, etc.)
- Advanced order types (TWAP, VWAP, DCA)
- Portfolio optimization
- Token swaps across chains

**Asset Classes:**
- ✅ Crypto (via DEX)
- ✅ DeFi tokens
- ✅ Cross-chain swaps

**Port Priority:** HIGH (unique DeFi capability)

---

#### 2. **MEXC** - Crypto Exchange
**File Found:** NOT FOUND in v2.5 (user may have confused with another exchange)
**Status:** ⚠️ NEEDS NEW IMPLEMENTATION
**Type:** Centralized Crypto Exchange

**What It Does:**
- 1,500+ crypto pairs
- Spot, margin, futures trading
- Low fees (0.0% maker on spot)
- New token listings (lots of altcoins)

**Asset Classes:**
- ✅ Crypto Spot (1,500+ pairs)
- ✅ Crypto Futures
- ✅ Crypto Margin

**Priority:** MEDIUM (another crypto exchange, we have 6 already)

---

### 🆕 **New Brokers to Implement**

#### 3. **Optimus Futures** - Futures Broker (FCM)
**Type:** Futures Commission Merchant
**Platform:** CQG, Rithmic, NinjaTrader, Sierra Chart

**What It Does:**
- Low-cost futures trading
- Professional platforms
- Similar to AMP Global

**Asset Classes:**
- ✅ Futures (CME, ICE, Eurex)
- ✅ Options on Futures
- ✅ Micro Futures

**Pricing:**
- Futures: $0.25-0.50/side
- Platforms: CQG ($50-150/month), Rithmic ($0-120/month)

**Implementation:** Very similar to AMPBroker (can reuse 80% of code)
**Priority:** MEDIUM (duplicate of AMP functionality)

---

#### 4. **WeBull** - Commission-Free Trading
**Type:** Retail Broker (Stocks/Options/Crypto)
**API:** WeBull API (REST + WebSocket)

**What It Does:**
- Commission-free stock/options trading
- Cryptocurrency trading
- Extended hours trading
- Paper trading
- Chinese-backed (like Robinhood competitor)

**Asset Classes:**
- ✅ Stocks (US markets)
- ✅ Options (equity options)
- ✅ Crypto (30+ coins)
- ⚠️ Futures (limited - Bitcoin futures)

**Pricing:**
- Stock Commissions: $0 (free)
- Options: $0.65/contract
- Crypto: 0.1% trading fee
- Minimum: $0

**Priority:** HIGH (commission-free, good API)

---

#### 5. **Robinhood** - Commission-Free Pioneer
**Type:** Retail Broker (Stocks/Options/Crypto)
**API:** Robinhood API (unofficial, community-maintained)

**What It Does:**
- Commission-free stock/options/crypto trading
- Fractional shares
- Instant deposits
- IPO access
- Most popular with retail traders

**Asset Classes:**
- ✅ Stocks (US markets)
- ✅ Options (equity options)
- ✅ Crypto (50+ coins via Robinhood Crypto)
- ⚠️ Futures (not available)

**Pricing:**
- Everything: $0 (free)
- Robinhood Gold: $5/month (margin)

**Challenges:**
- ⚠️ NO OFFICIAL API (must use unofficial library)
- Rate limiting strict
- Terms of service may prohibit bots

**Priority:** MEDIUM-HIGH (popular, but API concerns)

---

#### 6. **Tradovate** - Futures Broker
**Type:** Futures-Only Broker
**API:** Tradovate REST API + WebSocket

**What It Does:**
- Modern futures-only broker
- Web-based platform (no desktop required)
- Real-time margin calculator
- Advanced charting

**Asset Classes:**
- ✅ Futures (CME, CBOT, NYMEX, COMEX)
- ✅ Micro Futures
- ❌ Stocks/Options/Crypto

**Pricing:**
- Futures: $0.79-1.49/side (depending on plan)
- Platform: Free
- Data: $0-100/month

**Priority:** MEDIUM (another futures broker, we have 4)

---

#### 7. **Tradier** - Developer-Friendly Broker
**Type:** Stocks/Options Broker
**API:** Tradier Brokerage API (excellent documentation)

**What It Does:**
- Developer-focused broker
- Excellent API (best-in-class documentation)
- Stocks and options trading
- Market data included
- Used by many algo platforms

**Asset Classes:**
- ✅ Stocks (US markets)
- ✅ Options (equity options)
- ❌ Futures
- ❌ Crypto

**Pricing:**
- Equities Plus: $10/month unlimited stock trades
- Options: $0.35/contract + $10 base
- Pro: $99/month (all-inclusive)
- API: Free with account

**Priority:** HIGH (excellent API, algo-friendly)

---

#### 8. **OANDA** - Forex Specialist
**Type:** Forex/CFD Broker
**API:** OANDA v20 REST API

**What It Does:**
- Leading forex broker
- 70+ currency pairs
- CFDs on indices, commodities, metals
- Excellent API documentation
- Used by many algo traders

**Asset Classes:**
- ✅ Forex (70+ pairs)
- ✅ CFDs (indices, commodities, metals)
- ⚠️ Crypto CFDs (limited)
- ❌ Stocks
- ❌ Futures

**Pricing:**
- Spreads: From 0.6 pips (EUR/USD)
- Commissions: $50/million traded (or spread-only)
- Minimum: $0 (but $100 recommended)
- API: Free

**Priority:** HIGH (forex specialist, we only have IB/NT for forex)

---

### ℹ️ **Not a Broker**

#### Koyfin
**Type:** Financial data/analytics platform (like Bloomberg Terminal)
**What It Does:**
- Market data visualization
- Fundamental analysis
- Portfolio tracking
- Screening tools

**Why Not a Broker:**
- READ-ONLY data platform
- Cannot place trades
- No brokerage license
- Basically a Bloomberg/FactSet alternative

**Recommendation:** Use as DATA SOURCE, not broker
**Integration:** Could be added as `IMarketDataProvider` (not `IBroker`)

---

## Implementation Priority

### **Tier 1: HIGH Priority** (Implement First)

1. **Composer.trade** - Unique DeFi capability, already 90% done
2. **Tradier** - Best API, algo-friendly, excellent docs
3. **OANDA** - Forex specialist, fills gap
4. **WeBull** - Commission-free, good API

### **Tier 2: MEDIUM Priority** (Implement After)

5. **Robinhood** - Popular but API concerns
6. **MEXC** - Crypto (we have 6 already)
7. **Tradovate** - Futures (we have 4 already)
8. **Optimus Futures** - Futures (duplicate of AMP)

---

## Asset Class Coverage After Addition

| Asset Class | Current | After | Brokers |
|-------------|---------|-------|---------|
| **Stocks** | 4 | 6 | +WeBull, +Robinhood, +Tradier |
| **Options** | 3 | 5 | +WeBull, +Robinhood, +Tradier |
| **Futures** | 4 | 6 | +Optimus, +Tradovate |
| **Forex** | 3 | 4 | +OANDA |
| **Crypto** | 6 | 8 | +WeBull, +Robinhood, +MEXC |
| **DeFi** | 0 | 1 | +Composer ✨ |

---

## Total After Implementation

- **Current:** 11 brokers (181 KB)
- **After:** 19 brokers (estimated 350 KB)
- **New Capability:** DeFi/DEX trading (Composer)
- **Coverage:** Stocks, Options, Futures, Forex, Crypto, DeFi

---

## Technical Notes

### Easy Implementations (Reuse Existing Code):
- **Optimus Futures:** Copy AMPBroker, change API endpoints
- **Tradovate:** Similar to AMP/NinjaTrader pattern
- **MEXC:** Copy Binance/Bybit pattern

### Moderate Complexity:
- **WeBull:** New API pattern, but straightforward
- **Tradier:** Excellent docs, should be easy
- **OANDA:** Well-documented v20 API

### Complex/Risky:
- **Robinhood:** Unofficial API, TOS concerns
- **Composer:** Port from Python to C# (large file)

---

## Recommended Implementation Order

**Week 1:**
1. Tradier (best API, quick win)
2. OANDA (forex specialist, fills gap)

**Week 2:**
3. WeBull (commission-free stocks/options)
4. Composer (unique DeFi capability)

**Week 3:**
5. MEXC (crypto - if needed)
6. Tradovate (futures - if needed)

**Week 4:**
7. Optimus Futures (if customer requests)
8. Robinhood (if unofficial API is acceptable)

---

## Next Steps

1. ✅ Reviewed existing v2.5 code (found Composer)
2. ⏭️ Start with **Tradier** (easiest, best documented)
3. ⏭️ Implement **OANDA** (forex specialist)
4. ⏭️ Implement **WeBull** (popular, commission-free)
5. ⏭️ Port **Composer** from Python to C# (DeFi capability)
6. ⏭️ Evaluate need for remaining brokers

---

**Ready to proceed?** Should I start implementing in priority order?
