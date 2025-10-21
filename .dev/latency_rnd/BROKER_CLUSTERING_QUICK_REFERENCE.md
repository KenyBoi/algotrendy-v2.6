# Broker Clustering - Quick Reference Guide

**Purpose:** Show which brokers/providers should be deployed to which VPS locations for optimal latency

---

## Geographic Broker Clusters

### 🇺🇸 NEW JERSEY VPS - US Trading Hub
**Priority: IMMEDIATE** | **Cost: $50-80/month** | **ROI: 1-2 weeks**

```
BROKERS (6):
├── Alpaca           → US Stocks, ETFs (NYSE colocation)
├── Coinbase         → US Crypto (AWS Virginia)
├── Binance.US       → US Crypto (AWS US East)
├── Interactive Brokers → US Stocks, Options (US East presence)
├── TradeStation     → US Stocks, Options, Futures (Florida)
└── Polygon.io       → US Stock Data (Primary US provider)

DATA PROVIDERS (7):
├── Polygon.io       → US Stock Market Data
├── Alpaca Data API  → US Stock Data
├── Tiingo           → US Stocks + Crypto Data
├── EODHD            → Global Stocks (US East servers)
├── Alpha Vantage    → Stocks, Forex (AWS US East)
├── Finnhub          → Global Stocks (US East)
└── Financial Modeling Prep → US Stock Fundamentals

ASSET CLASSES:
• US Stocks (NYSE, NASDAQ)
• US ETFs
• US-regulated Crypto
• Stock Options

EXPECTED LATENCY:
• Current (from CDMX): 200-350ms
• After deployment: 5-20ms
• Improvement: 85-95%

TRADING HOURS:
• Pre-market: 4:00 AM - 9:30 AM ET
• Market: 9:30 AM - 4:00 PM ET
• After-hours: 4:00 PM - 8:00 PM ET
```

---

### 🇸🇬 SINGAPORE VPS - Crypto Trading Hub
**Priority: IMMEDIATE** | **Cost: $50-80/month** | **ROI: 1-2 weeks**

```
BROKERS (5):
├── Binance          → Global Crypto (Primary: Tokyo/Singapore)
├── Bybit            → Crypto Derivatives (Primary: Hong Kong)
├── MEXC             → Crypto (Primary: Singapore)
├── Interactive Brokers → Asian Stocks (Singapore/Hong Kong presence)
└── Kraken (future)  → Crypto (planned integration)

DATA PROVIDERS (2):
├── CoinGecko        → Crypto Market Data (Primary: Singapore)
└── Binance Market Data → Real-time Crypto Data

ASSET CLASSES:
• Global Cryptocurrency (Spot)
• Crypto Futures/Perpetuals
• Crypto Options
• Asian Stocks (via IB)

EXPECTED LATENCY:
• Current (from CDMX): 237-492ms (Bybit worst!)
• After deployment: 5-20ms
• Improvement: 95-97%

TRADING HOURS:
• 24/7 for crypto
• Asian market hours: 9 AM - 3 PM local

KEY ADVANTAGE:
★ Bybit latency: 492ms → 15ms (97% IMPROVEMENT!)
★ Singapore is the global crypto capital
★ Closest to Binance, Bybit, MEXC infrastructure
```

---

### 🇬🇧 LONDON VPS - European Trading Hub
**Priority: MEDIUM** | **Cost: $40-60/month** | **ROI: If expanding to EU**

```
BROKERS (4):
├── Interactive Brokers → European Stocks (London presence)
├── Kraken           → Crypto (Frankfurt datacenter nearby)
├── Binance          → EU Crypto (Ireland datacenter)
└── Bybit            → Crypto (Amsterdam datacenter)

DATA PROVIDERS (3):
├── European Stock Exchanges (via IB)
├── EODHD            → Global Stocks
└── Twelve Data      → Global Data (CDN)

ASSET CLASSES:
• European Stocks (LSE, Euronext, DAX)
• European Forex
• European Crypto

EXPECTED LATENCY:
• Unknown (not tested yet)
• Expected: 10-30ms to EU exchanges
• Covers European trading hours

TRADING HOURS:
• LSE: 8:00 AM - 4:30 PM GMT
• Xetra: 9:00 AM - 5:30 PM CET
• Crypto: 24/7

KEY ADVANTAGE:
★ Covers entire European time zone
★ Close to Binance EU (Ireland)
★ Close to Bybit (Amsterdam)
★ Essential for EU market expansion
```

---

### 🌆 CHICAGO VPS - Futures Specialization
**Priority: LOW (Unless doing HFT futures)** | **Cost: $80-120/month** | **ROI: Only for HFT**

```
BROKERS (3):
├── NinjaTrader      → Futures, Forex (CME proximity)
├── Interactive Brokers → Futures (Chicago presence)
└── TradeStation     → Futures Trading

DATA PROVIDERS (2):
├── CME Group        → Futures Market Data
└── CBOE             → Options Market Data

ASSET CLASSES:
• Futures (Commodities, Indices, Bonds)
• Options (Equities, Indices)
• Forex

EXPECTED LATENCY:
• Unknown (not tested yet)
• Expected: 1-10ms to CME/CBOE
• Ultra-low latency for HFT

TRADING HOURS:
• Futures: 24/5 (6 PM Sun - 5 PM Fri ET)
• Options: 9:30 AM - 4:00 PM ET

KEY ADVANTAGE:
★ Sub-10ms to CME Group
★ Essential for high-frequency futures trading
★ Premium pricing for colocation-quality latency

WHEN TO USE:
✓ High-frequency futures trading
✓ Latency arbitrage strategies
✓ Sub-second execution critical
✗ Skip if only doing swing/position trading
```

---

### 🇲🇽 CDMX VPS - Central Orchestration (Current)
**Priority: KEEP** | **Cost: Current** | **Role: Hub + ML/Backtesting**

```
SERVICES (5):
├── API Gateway      → Routes requests to regional VPS
├── ML Service       → Model training (not latency-sensitive)
├── Backtesting      → Strategy backtesting (not latency-sensitive)
├── QuestDB Master   → Primary database (write operations)
└── Frontend/UI      → User-facing dashboard

DATA PROVIDERS (2):
├── Twelve Data      → CDN-based (already fast)
└── CoinGecko        → Already good latency (30ms)

ASSET CLASSES:
• Orchestration only
• No direct trading

KEY ROLE:
★ Central hub for user interface
★ Routes requests to optimal region
★ Handles non-latency-sensitive workloads
★ Master database (all regions replicate here)
★ Monitoring and alerting central point

WHY KEEP:
✓ Already set up
✓ Good geographic midpoint
✓ Perfect for ML/backtesting (GPU-intensive, not latency-sensitive)
✓ User interface can be anywhere
```

---

## Priority Matrix

| VPS Location | Priority | Monthly Cost | Impact | When to Deploy |
|--------------|----------|--------------|--------|----------------|
| **New Jersey** | 🔴 CRITICAL | $50-80 | 95% latency ↓ | IMMEDIATE |
| **Singapore** | 🔴 CRITICAL | $50-80 | 97% latency ↓ | IMMEDIATE |
| **London** | 🟡 MEDIUM | $40-60 | EU expansion | When trading EU |
| **Chicago** | 🟢 LOW | $80-120 | HFT futures | Only for HFT |
| **CDMX** | ✅ KEEP | Current | Orchestration | Already deployed |

---

## Deployment Phases

### Phase 1: Critical Latency Fix (Week 1)
```bash
ACTION: Deploy New Jersey VPS
COST: $50-80/month
SERVICES: Trading (US stocks/crypto), Data (US markets)
BROKERS: Alpaca, Coinbase, Binance.US, Interactive Brokers, TradeStation
PROVIDERS: Polygon, Alpaca Data, Tiingo, EODHD, Alpha Vantage
IMPACT: 95% latency reduction for US trading
ROI: 1-2 weeks
```

### Phase 2: Crypto Optimization (Week 2)
```bash
ACTION: Deploy Singapore VPS
COST: $50-80/month
SERVICES: Trading (crypto), Data (crypto)
BROKERS: Binance, Bybit, MEXC
PROVIDERS: CoinGecko, Binance Data
IMPACT: 97% latency reduction for crypto (Bybit: 492ms → 15ms!)
ROI: 1-2 weeks
```

### Phase 3: EU Expansion (Month 2-3)
```bash
ACTION: Deploy London VPS (if needed)
COST: $40-60/month
SERVICES: Trading (EU), Data (EU)
BROKERS: Interactive Brokers (EU), Kraken, Binance EU
PROVIDERS: European stock data
IMPACT: Enables European market trading
ROI: If expanding to EU markets
```

### Phase 4: Futures HFT (Optional)
```bash
ACTION: Deploy Chicago VPS (if doing HFT)
COST: $80-120/month
SERVICES: Trading (futures), Data (CME/CBOE)
BROKERS: NinjaTrader, Interactive Brokers (futures)
IMPACT: Sub-10ms to CME, enables HFT strategies
ROI: Only if high-frequency futures trading
```

---

## Broker Count by Region

| Region | Brokers | Data Providers | Total APIs | Priority |
|--------|---------|----------------|------------|----------|
| **New Jersey** | 6 | 7 | 13 | 🔴 CRITICAL |
| **Singapore** | 5 | 2 | 7 | 🔴 CRITICAL |
| **London** | 4 | 3 | 7 | 🟡 MEDIUM |
| **Chicago** | 3 | 2 | 5 | 🟢 LOW |
| **CDMX** | 0 | 2 | 2 | ✅ KEEP |

**Total:** 10 unique brokers, 10 unique data providers

---

## Asset Class Distribution

### US Stocks
**Primary VPS:** New Jersey
**Brokers:** Alpaca, Interactive Brokers, TradeStation
**Data:** Polygon, Alpaca Data, Tiingo, EODHD, Alpha Vantage, Finnhub

### Cryptocurrency (Global)
**Primary VPS:** Singapore
**Secondary VPS:** New Jersey (US-regulated)
**Brokers:** Binance, Bybit, MEXC (Singapore); Binance.US, Coinbase (New Jersey)
**Data:** CoinGecko, Binance Data

### Stock Options
**Primary VPS:** New Jersey
**Secondary VPS:** Chicago (for options on futures)
**Brokers:** Interactive Brokers, TradeStation
**Data:** CBOE, Polygon

### Futures
**Primary VPS:** Chicago
**Brokers:** NinjaTrader, Interactive Brokers, TradeStation
**Data:** CME Group, CBOE

### European Stocks
**Primary VPS:** London
**Brokers:** Interactive Brokers
**Data:** European exchanges via IB

### Forex
**Primary VPS:** Any (24/5 global market)
**Brokers:** Interactive Brokers, NinjaTrader
**Data:** Alpha Vantage, Twelve Data

---

## Cost Summary

### Minimal Deployment (Recommended Start)
```
CDMX (current):        $30/month
New Jersey:            $60/month
Singapore:             $60/month
------------------------
TOTAL:                $150/month ($1,800/year)

Coverage: US stocks, global crypto, 95% of trading needs
```

### Full Deployment (Complete Global Coverage)
```
CDMX (current):        $30/month
New Jersey:            $60/month
Singapore:             $60/month
London:                $50/month
Chicago:               $100/month
------------------------
TOTAL:                $300/month ($3,600/year)

Coverage: 100% global, all asset classes, 24/7, HFT-ready
```

---

## Quick Decision Tree

```
Are you trading US stocks?
├─ YES → Deploy New Jersey VPS immediately
└─ NO  → Skip for now

Are you trading cryptocurrency?
├─ YES → Deploy Singapore VPS immediately
└─ NO  → Skip for now

Are you trading European stocks?
├─ YES → Deploy London VPS
└─ NO  → Skip for now

Are you doing high-frequency futures?
├─ YES → Deploy Chicago VPS
└─ NO  → Skip Chicago (not worth $100/month)

Are you just backtesting/ML?
└─ Stay on CDMX only (latency doesn't matter)
```

---

## Expected Results After Deployment

### Before (CDMX Only)
```
Average Broker Latency: 228.4ms
Worst Case (Bybit):    491.5ms
Best Case (CoinGecko):  29.8ms

Trading Viability:
✗ High-frequency trading
✗ Scalping strategies
✗ Latency arbitrage
✓ Swing trading (barely)
✓ Position trading
```

### After (New Jersey + Singapore)
```
Average Broker Latency: 12-15ms
Worst Case:            20ms
Best Case:             5ms

Trading Viability:
✓ High-frequency trading (enabled!)
✓ Scalping strategies
✓ Latency arbitrage
✓ Swing trading
✓ Position trading
✓ Day trading
✓ Market making (possible)

Improvement: 93-95% latency reduction
```

---

## Recommended Action (Summary)

**IMMEDIATE:**
1. Provision **New Jersey VPS** - Vultr or DigitalOcean ($60/month)
2. Provision **Singapore VPS** - Vultr or DigitalOcean ($60/month)
3. Deploy trading/data services per clustering above
4. Re-run latency tests
5. Measure improvement

**TOTAL COST:** $120/month ($1,440/year)
**EXPECTED IMPROVEMENT:** 95% latency reduction
**ROI:** Pays for itself in 1-2 weeks of active trading

**DEFER:**
- London VPS (until EU expansion)
- Chicago VPS (unless doing HFT futures)

**Status:** Ready to execute Phase 1 & 2

---

**Last Updated:** October 21, 2025
