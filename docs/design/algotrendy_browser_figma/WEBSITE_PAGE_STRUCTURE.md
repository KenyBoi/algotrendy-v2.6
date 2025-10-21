# AlgoTrendy Website Page Structure

**Date**: October 20, 2025
**Based on**: Industry best practices research + Backend API capabilities
**Purpose**: Complete page map for professional algorithmic trading platform

---

## 📊 Research Summary

Based on 2025 industry best practices for trading platforms:

### Key Principles
- **Simplicity First**: Clean, uncluttered interface - traders need quick access without getting lost
- **Progressive Disclosure**: Don't overwhelm - show what's needed, hide complexity until required
- **AI Integration**: MEM (AI assistant) should be present but not intrusive
- **Data Density**: Professional traders want lots of data, but organized smartly
- **Mobile-First**: Responsive design for trading on the go
- **Dark Mode Native**: Designed for extended sessions, easy on eyes

### Essential Platform Components (Industry Standard)
1. Dashboard (overview, quick actions, real-time data)
2. Trading Interface (place orders, manage positions)
3. Strategy Builder (design algorithms)
4. Backtesting Engine (test strategies)
5. Portfolio Management (positions, P&L, analytics)
6. Market Data (charts, watchlists, analysis)
7. Analytics & Reports (performance, metrics, insights)
8. Settings & Account Management

---

## 🗺️ AlgoTrendy Complete Page Structure

### PRIMARY PAGES (Main Navigation)

#### 1. **Dashboard** `/dashboard` ✅ EXISTS
**Purpose**: Command center - overview of everything
**Priority**: CRITICAL
**Sections**:
- Portfolio Summary (total value, today's P&L, buying power)
- Active Positions (3-5 most important, with live P&L)
- Recent Orders (last 5-10 orders with status)
- Market Overview (watchlist with live prices)
- MEM Activity Feed (what MEM is doing/thinking)
- Quick Actions (place order, close all positions, pause trading)
- Performance Chart (daily P&L last 30 days)
- Alerts & Notifications (margin warnings, fills, MEM insights)

**Backend Needs**:
- `/api/portfolio/debt-summary` ✅
- `/api/positions` (list) ⏳ MISSING
- `/api/orders` ✅ EXISTS (needs implementation)
- `/api/v1/marketdata/.../latest` ✅
- `/api/mem/status` ⏳ FUTURE

**Status**: Page exists, needs API integration

---

#### 2. **Trading** `/trading` ⏳ NEW PAGE NEEDED
**Purpose**: Active trading interface - place orders, monitor execution
**Priority**: CRITICAL
**Sections**:
- **Order Entry Panel** (symbol, side, type, quantity, price)
  - Market, Limit, Stop Market, Stop Limit orders
  - Risk calculator (position size suggestions based on account)
  - MEM validation ("This looks good!" or warnings)
- **Order Book** (live bids/asks for selected symbol)
- **Recent Trades** (time & sales)
- **Open Orders Table** (with cancel all button)
- **Position Summary** (for selected symbol)
- **Price Chart** (TradingView widget or lightweight chart)

**Backend Needs**:
- `POST /api/trading/orders` ✅
- `DELETE /api/trading/orders/{id}` ✅
- `GET /api/orders` ✅ EXISTS
- `/api/v1/marketdata/{symbol}` ✅
- SignalR for live price updates ✅

**Status**: DOESN'T EXIST - PRIORITY 1 TO BUILD

---

#### 3. **Orders** `/orders` ✅ EXISTS
**Purpose**: Order history and management
**Priority**: HIGH
**Sections**:
- **Active Orders Tab**
  - Filterable table (symbol, side, type, quantity, status)
  - Quick cancel buttons
  - Bulk actions (cancel all, cancel by symbol)
- **Order History Tab**
  - Completed orders (last 30 days)
  - Filters (date range, symbol, side, status)
  - Export to CSV
- **Failed Orders Tab**
  - Rejected orders with reasons
  - Retry options

**Backend Needs**:
- `GET /api/orders` ✅ EXISTS
- `DELETE /api/trading/orders/{id}` ✅
- SignalR `OrderUpdate` events ✅

**Status**: Page exists, needs full API integration

---

#### 4. **Positions** `/positions` ✅ EXISTS
**Purpose**: Current open positions and exposure management
**Priority**: HIGH
**Sections**:
- **Open Positions Table**
  - Symbol, side, size, entry price, current price, unrealized P&L
  - Live updates via WebSocket
  - Quick close buttons
  - Leverage indicator for leveraged positions
- **Position Details Modal** (click on position)
  - Entry details (time, price, order ID)
  - Current stats (current price, P&L $, P&L %)
  - Leverage info (if applicable)
  - Liquidation price warning
  - Close position form
- **Exposure Summary**
  - Total exposure by symbol
  - Exposure by side (long vs short)
  - Leverage usage
  - Margin health

**Backend Needs**:
- `GET /api/positions` ⏳ MISSING
- `GET /api/positions/{symbol}` ⏳ MISSING
- `DELETE /api/positions/{symbol}` ⏳ MISSING
- `GET /api/portfolio/leverage/{symbol}` ✅
- SignalR `PositionUpdate` events ✅

**Status**: Page exists, needs backend endpoints + integration

---

#### 5. **Strategies** `/strategies` ✅ EXISTS
**Purpose**: Design, test, and deploy algorithmic trading strategies
**Priority**: MEDIUM-HIGH
**Sections**:
- **My Strategies Tab**
  - List of saved strategies
  - Quick actions (edit, backtest, deploy, delete)
  - Strategy status (idle, backtesting, live, paused)
- **Strategy Builder Tab**
  - Visual strategy designer (drag-and-drop indicators)
  - Code editor option (Python/C# snippets)
  - MEM suggestions ("Try adding RSI filter here")
  - Parameter inputs
  - Save/load strategy
- **Backtest Results Tab**
  - Performance metrics (return, Sharpe, max drawdown, win rate)
  - Equity curve chart
  - Trade list
  - Comparison with other strategies
- **Live Strategies Tab** (Phase 2)
  - Strategies currently running
  - Performance vs backtest
  - Pause/resume/stop controls

**Backend Needs**:
- `POST /api/v1/backtesting/run` ✅
- `GET /api/v1/backtesting/results/{id}` ✅
- `GET /api/v1/backtesting/config` ✅
- `POST /api/strategies` ⏳ MISSING (save strategy)
- `GET /api/strategies` ⏳ MISSING (list strategies)
- `GET /api/strategies/{id}` ⏳ MISSING
- `/api/quantconnect/*` ✅ (advanced backtesting)

**Status**: Page exists with builder UI, needs backend strategy storage

---

#### 6. **Portfolio** `/portfolio` ⏳ NEW PAGE NEEDED
**Purpose**: Complete portfolio analytics and performance tracking
**Priority**: MEDIUM
**Sections**:
- **Overview Tab**
  - Account value chart (last 30/90/365 days)
  - Asset allocation pie chart
  - Performance metrics (total return, annualized return, Sharpe)
  - Comparison vs benchmarks (BTC, SPY)
- **Analytics Tab**
  - Win rate, average win, average loss
  - Profit factor, max consecutive wins/losses
  - Best/worst trades
  - Performance by symbol, by strategy, by time of day
- **Reports Tab**
  - Daily/weekly/monthly P&L reports
  - Tax reports (realized gains)
  - Trade journal
  - Export options
- **Risk Management Tab**
  - Current exposure breakdown
  - Value at Risk (VaR)
  - Margin usage and health
  - Liquidation warnings

**Backend Needs**:
- `GET /api/portfolio/debt-summary` ✅
- `GET /api/portfolio/analytics` ⏳ MISSING
- `GET /api/portfolio/performance` ⏳ MISSING
- `GET /api/portfolio/reports` ⏳ MISSING

**Status**: DOESN'T EXIST - PRIORITY 2 TO BUILD

---

#### 7. **Market** `/market` ⏳ NEW PAGE NEEDED
**Purpose**: Market data, charts, and analysis tools
**Priority**: MEDIUM
**Sections**:
- **Watchlist Tab**
  - Customizable symbol list
  - Live prices with % change
  - Volume, high/low
  - Quick trade button
  - MEM insights ("Bitcoin showing reversal pattern")
- **Charts Tab**
  - TradingView advanced charts
  - Multiple timeframes
  - Technical indicators
  - Drawing tools
  - Save chart layouts
- **Market Overview Tab**
  - Top gainers/losers
  - Most active by volume
  - Market sentiment indicators
  - Crypto fear & greed index
- **Data Explorer Tab** (Advanced)
  - Query historical data
  - Export data
  - Custom indicators

**Backend Needs**:
- `/api/v1/marketdata/{exchange}/latest` ✅
- `/api/v1/marketdata/{exchange}/{symbol}` ✅
- `/api/v1/cryptodata/*` ✅ (additional crypto data)
- SignalR market data hub ✅
- `/api/mem/opportunities` ⏳ FUTURE

**Status**: DOESN'T EXIST - PRIORITY 3 TO BUILD

---

### SECONDARY PAGES (Settings & Utility)

#### 8. **Settings** `/settings` ⏳ NEW PAGE NEEDED
**Purpose**: Account and platform configuration
**Priority**: LOW-MEDIUM
**Sections**:
- **Account Tab**
  - Profile info
  - API keys management
  - Two-factor authentication
  - Change password
- **Trading Tab**
  - Default order type
  - Confirmation dialogs (on/off)
  - Risk settings (max position size, etc.)
  - Preferred exchanges
- **Notifications Tab**
  - Email notifications
  - Push notifications
  - Alert preferences
  - MEM notification settings
- **Appearance Tab**
  - Theme (dark/light)
  - Layout preferences
  - Chart settings
  - Language

**Backend Needs**:
- `/api/account/profile` ⏳ MISSING
- `/api/account/settings` ⏳ MISSING
- `/api/mfa/*` ✅ (MFA endpoints exist)

**Status**: DOESN'T EXIST - LOW PRIORITY

---

#### 9. **Help & Docs** `/help` ⏳ NEW PAGE NEEDED
**Purpose**: Documentation and support
**Priority**: LOW
**Sections**:
- **Getting Started Guide**
- **API Documentation**
- **Trading Guide** (how to use strategies, backtesting)
- **FAQ**
- **Contact Support**
- **MEM Help** ("Ask MEM anything!")

**Backend Needs**: Static content, no special backend

**Status**: DOESN'T EXIST - LOW PRIORITY

---

### AUTHENTICATION PAGES

#### 10. **Login** `/login` ✅ EXISTS
**Purpose**: User authentication
**Sections**:
- Email/username input
- Password input
- Remember me checkbox
- Forgot password link
- Sign up link
- MFA code input (if enabled)

**Status**: EXISTS

---

#### 11. **Register** `/register` ⏳ NEW PAGE (if needed)
**Purpose**: New user sign up
**Sections**:
- Email, username, password
- Terms acceptance
- Email verification

**Status**: DOESN'T EXIST - depends on auth strategy

---

### ERROR PAGES

#### 12. **404 Not Found** `/404` ✅ EXISTS
**Status**: EXISTS

---

#### 13. **500 Server Error** `/500` ⏳ OPTIONAL
**Status**: Can be added

---

## 📱 Page Hierarchy & Navigation

### Top Navigation (Always Visible)
```
┌─────────────────────────────────────────────────────────────┐
│ [AlgoTrendy Logo]  Dashboard  Trading  Orders  Positions   │
│                    Strategies  Portfolio  Market  [MEM]  [⚙] │
└─────────────────────────────────────────────────────────────┘
```

### Sidebar Navigation (Alternative - Mobile Collapsible)
```
┌────────────┐
│ Dashboard  │ ← Always first
│ Trading    │ ← New! Critical
│ Orders     │
│ Positions  │
│ Strategies │
│ Portfolio  │ ← New
│ Market     │ ← New
├────────────┤
│ Settings   │
│ Help       │
└────────────┘
```

### MEM Corner (Always Visible - Bottom Right)
```
┌──────────────────┐
│  MEM    [status] │
│  ────────────    │
│  Today: +$125    │
│  Active: 3       │
│  [Expand Chat]   │
└──────────────────┘
```

---

## 🚦 Implementation Priority

### Phase 1: CRITICAL (Week 1-2)
1. ✅ Dashboard - EXISTS, integrate API
2. ⏳ Trading page - BUILD NEW
3. ✅ Orders - EXISTS, integrate API
4. ✅ Positions - EXISTS, integrate API

### Phase 2: HIGH PRIORITY (Week 3-4)
5. ✅ Strategies - EXISTS, add save/load
6. ⏳ Portfolio - BUILD NEW
7. ⏳ Settings (basic) - BUILD NEW

### Phase 3: MEDIUM PRIORITY (Month 2)
8. ⏳ Market - BUILD NEW
9. ⏳ Advanced analytics
10. ⏳ Reports

### Phase 4: NICE TO HAVE (Future)
11. Help & documentation
12. Advanced MEM features
13. Mobile app

---

## 📋 Summary

### Current Status
- **Existing Pages**: 4 (Dashboard, Orders, Positions, Strategies) + Login + 404
- **Fully Functional**: 0 (all need API integration)
- **Need to Build**: 5-7 new pages

### Recommended Minimal Launch (MVP)
**6 Essential Pages**:
1. Dashboard ✅
2. Trading ⏳
3. Orders ✅
4. Positions ✅
5. Strategies ✅
6. Login ✅

This gives users:
- See their account (Dashboard)
- Place trades (Trading)
- Monitor orders (Orders)
- Manage positions (Positions)
- Test strategies (Strategies)

### Full Professional Platform
**10-13 Pages**:
- All MVP pages
- Portfolio (analytics)
- Market (data explorer)
- Settings
- Help
- Register (if needed)

---

## 🎯 Next Steps

### Option A: Complete Existing Pages First (RECOMMENDED)
1. Integrate Dashboard with real API
2. Integrate Orders with real API
3. Integrate Positions with real API
4. Integrate Strategies with backend storage
5. **THEN** build new Trading page
6. **THEN** build Portfolio page
7. **THEN** build Market page

### Option B: Build Critical New Pages First
1. Build Trading page (most critical missing piece)
2. Then integrate all existing pages
3. Then build Portfolio/Market

### Option C: Hybrid Approach
1. Build Trading page (critical missing)
2. Integrate Orders + Positions (quick wins)
3. Build Portfolio page
4. Integrate Dashboard + Strategies

---

**Last Updated**: October 20, 2025
**Based On**: Industry research + backend API analysis
**Recommendation**: Start with **Option A** - complete what exists, then add new pages
