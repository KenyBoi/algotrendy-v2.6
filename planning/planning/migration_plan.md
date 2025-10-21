# AlgoTrendy v2.5 → v2.6 Methodical Migration Plan

**Date:** October 18, 2025
**Purpose:** Step-by-step file migration from v2.5 to v2.6
**Approach:** Section-by-section, phase-by-phase (NOT all at once)
**Goals:**
1. ✅ Preserve all existing work
2. ✅ Avoid redundant tasks
3. ✅ Optimize each section's approach
4. ✅ Maintain working system during migration

---

## MIGRATION PRINCIPLES

### Do NOT Do This ❌
```bash
# WRONG - Copying everything at once
cp -r /root/algotrendy_v2.5/* /root/AlgoTrendy_v2.6/
```

**Why This Is Bad:**
- Brings over all problems from v2.5
- No opportunity to fix issues during migration
- Mixes old and new architecture
- Loses opportunity for optimization

### Do This Instead ✅
1. **Analyze each file** before copying
2. **Categorize**: Keep as-is, Modify, Rewrite, Deprecate
3. **Copy section-by-section** aligned with implementation phases
4. **Test after each section** migration
5. **Document changes** made during migration

---

## FILE CATEGORIZATION MATRIX

### Category 1: KEEP AS-IS (Copy directly)
Files that are good quality and don't need changes.

**Criteria:**
- No security issues
- No performance problems
- Well-tested
- No hardcoded values

**Examples:**
- Database schema definitions
- Configuration JSON files (after removing secrets)
- Utility functions
- Well-written data channel implementations

### Category 2: MODIFY (Copy + Edit)
Files that need minor fixes before use.

**Criteria:**
- Good structure but has hardcoded values
- Contains TODOs that can be addressed
- Has minor security issues (easy fixes)
- Needs environment variable injection

**Examples:**
- Config files with hardcoded secrets
- Database connection modules
- Indicator calculations with magic numbers

### Category 3: REWRITE (.NET/TypeScript)
Files that must be rewritten in new technology.

**Criteria:**
- Python execution code → .NET C#
- React frontend → Next.js 15
- Performance-critical paths
- Heavy refactoring needed

**Examples:**
- Trading engine core
- Broker abstraction layer
- API endpoints (FastAPI → ASP.NET)
- Frontend components

### Category 4: DEPRECATE (Do not migrate)
Files that will not be used in v2.6.

**Criteria:**
- Replaced by better technology
- Security nightmares
- Disabled/broken code
- Old frontend code

**Examples:**
- Disabled auth modules
- Old Next.js frontend
- Demo user code
- Commented-out production modules

---

## PHASE-BY-PHASE MIGRATION PLAN

## PHASE 1: FOUNDATION SETUP (Week 1-4)

### Week 1: Project Structure & Configuration

#### Task 1.1: Create v2.6 Directory Structure
```bash
# Create all necessary directories
mkdir -p /root/AlgoTrendy_v2.6/{
backend/{
    dotnet/{
        TradingEngine,
        TradingEngine.API,
        TradingEngine.Core,
        TradingEngine.Brokers,
        TradingEngine.Tests
    },
    python/{
        analytics,
        backtesting,
        data_channels,
        ml_models,
        agents
    }
},
database/{
    questdb/schemas,
    postgresql/schemas,
    postgresql/migrations
},
frontend/nextjs-app,
infrastructure/{
    docker,
    kubernetes,
    terraform
},
docs,
scripts,
tests
}
```

#### Task 1.2: Migrate Configuration Files (MODIFY Category)

**Source:** `/root/algotrendy_v2.5/algotrendy/configs/`

**Actions:**
1. Copy all JSON config files
2. **BEFORE USE:** Scan for hardcoded secrets
3. Replace secrets with environment variable placeholders
4. Validate JSON structure

**Script:**
```bash
# Copy configs
cp -r /root/algotrendy_v2.5/algotrendy/configs/ \
      /root/AlgoTrendy_v2.6/backend/python/configs/

# Scan for secrets (manual review required)
grep -r "api_key\|secret\|password" \
     /root/AlgoTrendy_v2.6/backend/python/configs/
```

**Manual Steps:**
```json
// BEFORE (v2.5):
{
  "broker": "bybit",
  "api_key": "ACTUAL_KEY_HERE",
  "api_secret": "ACTUAL_SECRET_HERE"
}

// AFTER (v2.6):
{
  "broker": "bybit",
  "api_key": "${BYBIT_API_KEY}",
  "api_secret": "${BYBIT_API_SECRET}"
}
```

**Checklist:**
- [ ] All JSON files copied
- [ ] All secrets removed and replaced with `${ENV_VAR}`
- [ ] Validated JSON syntax
- [ ] Created `.env.example` template

#### Task 1.3: Migrate Database Schema (MODIFY Category)

**Source:** `/root/algotrendy_v2.5/database/`

**Decisions:**
1. **PostgreSQL tables** → Keep for user data, configs, audit logs
2. **TimescaleDB hypertables** → Migrate to QuestDB schemas

**PostgreSQL Migration:**
```bash
# Copy schema
cp /root/algotrendy_v2.5/database/schema.sql \
   /root/AlgoTrendy_v2.6/database/postgresql/schemas/v2.6_initial.sql

# Review and edit:
# - Remove TimescaleDB-specific tables (market_data, tick_data, etc.)
# - Keep users, audit_logs, portfolio, positions, orders, trades
# - Add new tables: agent_memory, agent_logs, strategy_versions
```

**QuestDB Schema Creation:**
Create new file: `/root/AlgoTrendy_v2.6/database/questdb/schemas/time_series.sql`

```sql
-- Tick data (every trade)
CREATE TABLE ticks (
    symbol SYMBOL,
    exchange SYMBOL,
    price DOUBLE,
    volume DOUBLE,
    side SYMBOL,
    timestamp TIMESTAMP
) TIMESTAMP(timestamp) PARTITION BY DAY;

-- OHLCV bars
CREATE TABLE ohlcv (
    symbol SYMBOL,
    exchange SYMBOL,
    timeframe SYMBOL,
    open DOUBLE,
    high DOUBLE,
    low DOUBLE,
    close DOUBLE,
    volume DOUBLE,
    timestamp TIMESTAMP
) TIMESTAMP(timestamp) PARTITION BY DAY;

-- Order book snapshots
CREATE TABLE order_book (
    symbol SYMBOL,
    exchange SYMBOL,
    bids STRING,
    asks STRING,
    timestamp TIMESTAMP
) TIMESTAMP(timestamp) PARTITION BY HOUR;

-- Trading signals
CREATE TABLE signals (
    symbol SYMBOL,
    strategy SYMBOL,
    signal SYMBOL,  -- buy, sell, hold
    confidence DOUBLE,
    price DOUBLE,
    metadata STRING,
    timestamp TIMESTAMP
) TIMESTAMP(timestamp) PARTITION BY DAY;

-- Executed trades
CREATE TABLE trades (
    trade_id SYMBOL,
    symbol SYMBOL,
    exchange SYMBOL,
    side SYMBOL,
    quantity DOUBLE,
    price DOUBLE,
    fee DOUBLE,
    order_id SYMBOL,
    timestamp TIMESTAMP
) TIMESTAMP(timestamp) PARTITION BY DAY;
```

**Checklist:**
- [ ] PostgreSQL schema cleaned (no time-series tables)
- [ ] QuestDB schemas created
- [ ] Migration plan documented
- [ ] Test data prepared for validation

#### Task 1.4: Set Up Secrets Management (NEW)

**Create:** `/root/AlgoTrendy_v2.6/backend/dotnet/TradingEngine.Core/SecretManager.cs`

```csharp
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

public class SecretManager
{
    private readonly SecretClient _client;

    public SecretManager(string keyVaultUrl)
    {
        _client = new SecretClient(
            new Uri(keyVaultUrl),
            new DefaultAzureCredential()
        );
    }

    public async Task<string> GetSecretAsync(string secretName)
    {
        KeyVaultSecret secret = await _client.GetSecretAsync(secretName);
        return secret.Value;
    }
}
```

**Checklist:**
- [ ] Azure Key Vault created (or AWS Secrets Manager)
- [ ] SecretManager class implemented
- [ ] All secrets migrated to vault
- [ ] Access policies configured
- [ ] Integration tested

---

### Week 2: Core Trading Engine Migration (Part 1)

#### Task 2.1: Analyze & Inventory v2.5 Trading Engine

**Source Files:**
```
/root/algotrendy_v2.5/algotrendy/
├── unified_trader.py (650 lines) - REWRITE in C#
├── broker_abstraction.py (400 lines) - REWRITE in C#
├── strategy_resolver.py (300 lines) - REWRITE in C#
├── indicator_engine.py (500 lines) - KEEP in Python (data science)
└── signal_processor.py (200 lines) - REWRITE in C#
```

**Analysis Checklist:**
- [ ] Read unified_trader.py - understand core logic
- [ ] Document all public methods and their purposes
- [ ] Identify dependencies (what it calls)
- [ ] List all configuration requirements
- [ ] Note all TODO/FIXME comments

**Create Analysis Document:**
`/root/AlgoTrendy_v2.6/docs/v2.5_trading_engine_analysis.md`

#### Task 2.2: Migrate Indicator Engine (KEEP AS-IS)

**Why Keep in Python:**
- NumPy/Pandas optimized for array operations
- Existing TA-Lib, Pandas-TA integrations
- Data science ecosystem
- No latency requirements (calculated before trading)

**Action:**
```bash
# Copy indicator engine
cp /root/algotrendy_v2.5/algotrendy/indicator_engine.py \
   /root/AlgoTrendy_v2.6/backend/python/analytics/indicator_engine.py

# Copy tests if they exist
cp /root/algotrendy_v2.5/algotrendy/tests/test_indicators.py \
   /root/AlgoTrendy_v2.6/backend/python/tests/ 2>/dev/null || true
```

**Modifications:**
1. Update import paths
2. Connect to QuestDB instead of TimescaleDB
3. Add type hints (Python 3.11+)

**Checklist:**
- [ ] File copied
- [ ] Imports fixed
- [ ] Database connection updated
- [ ] Tests run successfully
- [ ] Documented in migration log

#### Task 2.3: Create .NET Trading Engine Structure (REWRITE)

**Create .NET Solution:**
```bash
cd /root/AlgoTrendy_v2.6/backend/dotnet

# Create solution
dotnet new sln -n TradingEngine

# Create projects
dotnet new classlib -n TradingEngine.Core
dotnet new classlib -n TradingEngine.Brokers
dotnet new webapi -n TradingEngine.API
dotnet new xunit -n TradingEngine.Tests

# Add projects to solution
dotnet sln add TradingEngine.Core/TradingEngine.Core.csproj
dotnet sln add TradingEngine.Brokers/TradingEngine.Brokers.csproj
dotnet sln add TradingEngine.API/TradingEngine.API.csproj
dotnet sln add TradingEngine.Tests/TradingEngine.Tests.csproj

# Add project references
cd TradingEngine.Brokers
dotnet add reference ../TradingEngine.Core/TradingEngine.Core.csproj

cd ../TradingEngine.API
dotnet add reference ../TradingEngine.Core/TradingEngine.Core.csproj
dotnet add reference ../TradingEngine.Brokers/TradingEngine.Brokers.csproj

cd ../TradingEngine.Tests
dotnet add reference ../TradingEngine.Core/TradingEngine.Core.csproj
dotnet add reference ../TradingEngine.Brokers/TradingEngine.Brokers.csproj
```

**Checklist:**
- [ ] .NET solution created
- [ ] All projects build successfully
- [ ] Project references configured
- [ ] NuGet packages installed

#### Task 2.4: Translate Broker Abstraction to C# (REWRITE)

**Study v2.5 Implementation:**
`/root/algotrendy_v2.5/algotrendy/broker_abstraction.py`

**Key Classes:**
- `BrokerInterface` (abstract base class)
- `BybitBroker` (only complete implementation)
- `BinanceBroker`, `OKXBroker`, etc. (stubs)

**Create C# Equivalent:**
`/root/AlgoTrendy_v2.6/backend/dotnet/TradingEngine.Core/Interfaces/IBroker.cs`

```csharp
public interface IBroker
{
    Task<decimal> GetBalanceAsync(CancellationToken ct = default);
    Task<List<Position>> GetPositionsAsync(CancellationToken ct = default);
    Task<OrderResult> PlaceOrderAsync(
        OrderRequest request,
        CancellationToken ct = default
    );
    Task<bool> CancelOrderAsync(
        string orderId,
        CancellationToken ct = default
    );
    Task SetLeverageAsync(
        string symbol,
        int leverage,
        CancellationToken ct = default
    );
}

public record OrderRequest(
    string Symbol,
    OrderSide Side,
    decimal Quantity,
    OrderType Type,
    decimal? Price = null,
    Guid? IdempotencyKey = null
);

public record OrderResult(
    bool Success,
    string? OrderId = null,
    string? Error = null,
    OrderStatus? Status = null
);
```

**Port Bybit Implementation:**
`/root/AlgoTrendy_v2.6/backend/dotnet/TradingEngine.Brokers/BybitBroker.cs`

**Translation Guide:**
```python
# Python v2.5
async def get_balance(self) -> float:
    result = self.client.get_wallet_balance(accountType="UNIFIED")
    return float(result['result']['list'][0]['totalWalletBalance'])
```

```csharp
// C# v2.6
public async Task<decimal> GetBalanceAsync(CancellationToken ct = default)
{
    var result = await _client.GetWalletBalanceAsync("UNIFIED", ct);
    return result.Result.List[0].TotalWalletBalance;
}
```

**Security Improvements During Port:**
1. Add idempotency keys to all order methods
2. Implement rate limiting (token bucket)
3. Add retry logic with exponential backoff
4. Validate all inputs
5. Use CancellationToken for timeouts

**Checklist:**
- [ ] `IBroker` interface created
- [ ] `BybitBroker` fully ported with improvements
- [ ] Unit tests written
- [ ] Rate limiting implemented
- [ ] Idempotency working
- [ ] Testnet testing completed

---

### Week 3-4: Complete Phase 1 Tasks

**(Continued migration of foundation components)**

---

## PHASE 2: DATA CHANNELS MIGRATION (Week 5-8)

### Week 5: Market Data Channels

#### Task 5.1: Categorize Data Channel Files

**Source:** `/root/algotrendy_v2.5/algotrendy/data_channels/`

**Decision Matrix:**

| File | Category | Action | Reason |
|------|----------|--------|--------|
| `base.py` | MODIFY | Copy + Edit | Good base class, fix SQL injection |
| `manager.py` | KEEP AS-IS | Copy directly | Clean orchestration logic |
| `market_data/binance.py` | KEEP AS-IS | Copy | Working implementation |
| `market_data/okx.py` | KEEP AS-IS | Copy | Working implementation |
| `market_data/coinbase.py` | KEEP AS-IS | Copy | Working implementation |
| `market_data/kraken.py` | KEEP AS-IS | Copy | Working implementation |
| `news/polygon.py` | MODIFY | Copy + Update API | Add error handling |
| `news/cryptopanic.py` | KEEP AS-IS | Copy | Good implementation |
| `news/fmp.py` | MODIFY | Copy + Fix bare except | Security issue |
| `news/yahoo.py` | KEEP AS-IS | Copy | Simple RSS parser |
| `sentiment/` | NEW | Create from scratch | Empty in v2.5 |
| `onchain/` | NEW | Create from scratch | Empty in v2.5 |
| `alt_data/` | NEW | Create from scratch | Empty in v2.5 |

#### Task 5.2: Migrate Base Data Channel (MODIFY)

**Source:** `/root/algotrendy_v2.5/algotrendy/data_channels/base.py`

**Migration Steps:**
```bash
# Copy file
cp /root/algotrendy_v2.5/algotrendy/data_channels/base.py \
   /root/AlgoTrendy_v2.6/backend/python/data_channels/base.py
```

**Critical Fix - SQL Injection:**
```python
# BEFORE (v2.5) - SQL INJECTION VULNERABILITY
async def _log_ingestion(self, db: Session, stats: Dict[str, Any]):
    table_name = "ingestion_logs"
    query = f"""
        INSERT INTO {table_name} (source_id, records, timestamp)
        VALUES ('{stats["source_id"]}', {stats["records"]}, NOW())
    """
    db.execute(text(query))

# AFTER (v2.6) - FIXED
async def _log_ingestion(self, db: Session, stats: Dict[str, Any]):
    query = text("""
        INSERT INTO ingestion_logs (source_id, records, timestamp)
        VALUES (:source_id, :records, NOW())
    """)
    db.execute(query, {
        "source_id": stats["source_id"],
        "records": stats["records"]
    })
```

**Other Improvements:**
1. Add comprehensive error logging
2. Add retry logic
3. Update to use QuestDB for time-series inserts
4. Add type hints

**Checklist:**
- [ ] File copied
- [ ] SQL injection fixed
- [ ] QuestDB integration added
- [ ] Type hints added
- [ ] Error handling improved
- [ ] Tests written

#### Task 5.3: Migrate Market Data Channels (KEEP AS-IS)

**Channels to Migrate:**
- Binance
- OKX
- Coinbase
- Kraken

**Process for Each:**
```bash
# Example: Binance
cp /root/algotrendy_v2.5/algotrendy/data_channels/market_data/binance.py \
   /root/AlgoTrendy_v2.6/backend/python/data_channels/market_data/binance.py

# Minimal changes:
# 1. Update imports (base.py path)
# 2. Change database writes to QuestDB
# 3. Verify no hardcoded secrets
```

**QuestDB Integration Example:**
```python
# BEFORE (TimescaleDB)
await db.execute("""
    INSERT INTO market_data (symbol, open, high, low, close, volume, timestamp)
    VALUES (%s, %s, %s, %s, %s, %s, %s)
""", (symbol, open, high, low, close, volume, timestamp))

# AFTER (QuestDB via InfluxDB Line Protocol)
from questdb.ingress import Sender

async def write_to_questdb(self, candles: List[Dict]):
    with Sender('localhost', 9009) as sender:
        for candle in candles:
            sender.row(
                'ohlcv',
                symbols={'symbol': candle['symbol'], 'exchange': 'binance'},
                columns={
                    'open': candle['open'],
                    'high': candle['high'],
                    'low': candle['low'],
                    'close': candle['close'],
                    'volume': candle['volume']
                },
                at=candle['timestamp']
            )
        sender.flush()
```

**Checklist (per channel):**
- [ ] File copied
- [ ] Imports updated
- [ ] QuestDB integration complete
- [ ] No secrets in code
- [ ] Tested with live data
- [ ] Logging verified

---

### Week 6: News & Sentiment Channels

#### Task 6.1: Migrate News Channels (MODIFY)

**Files:**
- `news/polygon.py` - Update API version
- `news/cryptopanic.py` - Keep as-is
- `news/fmp.py` - Fix bare except
- `news/yahoo.py` - Keep as-is

**FMP Fix Example:**
```python
# BEFORE (v2.5) - BARE EXCEPT
try:
    timestamp = datetime.fromisoformat(article['publishedDate'])
except:  # BAD - catches everything
    timestamp = datetime.utcnow()

# AFTER (v2.6) - SPECIFIC EXCEPTION
try:
    timestamp = datetime.fromisoformat(article['publishedDate'])
except (ValueError, KeyError) as e:
    logger.warning(f"Invalid date format: {e}")
    timestamp = datetime.utcnow()
```

#### Task 6.2: Create Sentiment Channels (NEW)

**Files to Create:**
- `sentiment/reddit.py` - Reddit sentiment via PRAW
- `sentiment/twitter.py` - Twitter/X sentiment
- `sentiment/lunarcrush.py` - LunarCrush API integration

**Reddit Channel Template:**
```python
import praw
from .base import DataChannel

class RedditSentimentChannel(DataChannel):
    def __init__(self):
        super().__init__(
            name="Reddit Sentiment",
            source_id=10,
            update_interval=300  # 5 minutes
        )
        self.reddit = praw.Reddit(
            client_id=os.getenv('REDDIT_CLIENT_ID'),
            client_secret=os.getenv('REDDIT_CLIENT_SECRET'),
            user_agent='AlgoTrendy/2.6'
        )

    async def fetch_data(self) -> List[Dict]:
        posts = []
        for submission in self.reddit.subreddit('cryptocurrency').hot(limit=100):
            sentiment = self._analyze_sentiment(submission.title + ' ' + submission.selftext)
            posts.append({
                'symbol': self._extract_symbol(submission.title),
                'title': submission.title,
                'score': submission.score,
                'sentiment': sentiment,
                'url': submission.url,
                'timestamp': datetime.fromtimestamp(submission.created_utc)
            })
        return posts

    def _analyze_sentiment(self, text: str) -> float:
        # Use TextBlob or VADER for sentiment analysis
        from textblob import TextBlob
        return TextBlob(text).sentiment.polarity
```

**Checklist:**
- [ ] Reddit channel created
- [ ] Twitter channel created
- [ ] LunarCrush channel created
- [ ] All use environment variables for secrets
- [ ] Sentiment analysis implemented
- [ ] QuestDB integration complete

---

### Week 7-8: On-Chain & Alternative Data

**(Similar pattern - create new channels)**

---

## PHASE 3: AI AGENT INTEGRATION (Week 9-12)

### Week 9: LangGraph Setup

#### Task 9.1: Create Agent Directory Structure

```bash
mkdir -p /root/AlgoTrendy_v2.6/backend/python/agents/{
    workflows,
    tools,
    memory,
    prompts,
    config
}
```

#### Task 9.2: Set Up LangGraph Environment

**Create:** `/root/AlgoTrendy_v2.6/backend/python/agents/requirements.txt`

```
langgraph>=0.2.0
langchain>=0.3.0
letta>=0.4.0  # MemGPT
pinecone-client>=3.0.0
openai>=1.0.0
anthropic>=0.20.0
```

#### Task 9.3: Create Base Agent Workflow

**No v2.5 code to migrate** - This is entirely NEW

**Create:** `/root/AlgoTrendy_v2.6/backend/python/agents/workflows/trading_workflow.py`

```python
from langgraph.graph import StateGraph, END
from typing import TypedDict, List

class TradingState(TypedDict):
    market_data: dict
    news: List[dict]
    signals: List[dict]
    risk_assessment: dict
    orders: List[dict]
    memory_context: str

# Define workflow
workflow = StateGraph(TradingState)

# Add nodes (agents)
workflow.add_node("market_analyzer", analyze_market)
workflow.add_node("signal_generator", generate_signals)
workflow.add_node("risk_manager", assess_risk)
workflow.add_node("executor", execute_orders)
workflow.add_node("memory_updater", update_memory)

# Define edges
workflow.set_entry_point("market_analyzer")
workflow.add_edge("market_analyzer", "signal_generator")
workflow.add_edge("signal_generator", "risk_manager")
workflow.add_conditional_edges(
    "risk_manager",
    should_execute,
    {
        "execute": "executor",
        "reject": "memory_updater"
    }
)
workflow.add_edge("executor", "memory_updater")
workflow.add_edge("memory_updater", END)

# Compile
trading_agent = workflow.compile()
```

**Checklist:**
- [ ] LangGraph installed
- [ ] Base workflow created
- [ ] 5 agent nodes defined
- [ ] Conditional logic working
- [ ] Tested with mock data

---

## PHASE 4: FRONTEND MIGRATION (Week 17-24)

### Week 17: Next.js 15 Setup

#### Task 17.1: Create Next.js Project (REWRITE)

**DO NOT copy anything from v2.5 frontend** - Complete rewrite

```bash
cd /root/AlgoTrendy_v2.6/frontend

# Create Next.js 15 app
npx create-next-app@latest nextjs-app \
  --typescript \
  --tailwind \
  --app \
  --src-dir \
  --import-alias "@/*"

cd nextjs-app

# Install dependencies
npm install @microsoft/signalr  # SignalR client
npm install zustand  # State management
npm install @tanstack/react-query  # Server state
npm install recharts  # Charts
npm install plotly.js react-plotly.js  # ML visualizations
npm install @monaco-editor/react  # Code editor
npm install @radix-ui/react-* # UI primitives
npm install lucide-react  # Icons
npm install zod react-hook-form  # Forms
```

#### Task 17.2: Identify Reusable Business Logic from v2.5

**Study v2.5 frontend (if it exists):**
```bash
# Analyze existing frontend
ls -la /root/algotrendy_v2.5/algotrendy-web/
```

**Extract Reusable Concepts (NOT code):**
- API endpoint URLs (document them)
- Data structures (convert to TypeScript interfaces)
- Business logic (rewrite in TypeScript)
- UI component hierarchy (document for redesign)

**Create Migration Guide:**
`/root/AlgoTrendy_v2.6/docs/frontend_v2.5_to_v2.6_guide.md`

**Document:**
- Which v2.5 pages exist
- What data they fetch
- What actions they perform
- Which v2.6 pages will replace them

**DO NOT copy v2.5 React code directly** - Use as reference only

---

## MIGRATION TRACKING SYSTEM

### Create Migration Tracker

**File:** `/root/AlgoTrendy_v2.6/planning/migration_tracker.md`

```markdown
# Migration Tracker

## Phase 1: Foundation (Week 1-4)

| Task | Source File | Destination | Category | Status | Notes |
|------|-------------|-------------|----------|--------|-------|
| Config files | `configs/*.json` | `backend/python/configs/` | MODIFY | ✅ Done | Removed secrets |
| DB Schema | `database/schema.sql` | `database/postgresql/schemas/` | MODIFY | ✅ Done | Split PG/QuestDB |
| Indicator Engine | `indicator_engine.py` | `backend/python/analytics/` | KEEP AS-IS | ✅ Done | No changes |
| Broker Interface | `broker_abstraction.py` | `backend/dotnet/TradingEngine.Core/` | REWRITE | 🟡 In Progress | Porting to C# |
| ... | ... | ... | ... | ... | ... |

## Phase 2: Data Channels (Week 5-8)

| Task | Source File | Destination | Category | Status | Notes |
|------|-------------|-------------|----------|--------|-------|
| Base Channel | `data_channels/base.py` | `backend/python/data_channels/` | MODIFY | ⬜ Not Started | Fix SQL injection |
| Binance Channel | `market_data/binance.py` | `backend/python/data_channels/market_data/` | KEEP AS-IS | ⬜ Not Started | Update QuestDB |
| ... | ... | ... | ... | ... | ... |

**Status Legend:**
- ✅ Done
- 🟡 In Progress
- ⬜ Not Started
- ❌ Blocked
- 🔄 Needs Review
```

### Daily Migration Log

**File:** `/root/AlgoTrendy_v2.6/planning/daily_log.md`

```markdown
# Daily Migration Log

## 2025-10-18

### Files Migrated
- `algotrendy/configs/bybit_crypto_momentum_live.json` → Modified, removed API keys
- `database/schema.sql` → Split into PostgreSQL and QuestDB schemas

### Issues Found
- SQL injection in `data_channels/base.py:264`
- Hardcoded secret in `core/config.py:27`

### Decisions Made
- Use Azure Key Vault for secrets management
- QuestDB for all time-series data
- Keep indicator_engine.py in Python (no port to C#)

### Next Steps
- [ ] Complete Bybit broker port to C#
- [ ] Set up Azure Key Vault
- [ ] Test QuestDB ingestion

---

## 2025-10-19

...
```

---

## TESTING STRATEGY DURING MIGRATION

### After Each Section Migration

**Run These Tests:**

1. **Unit Tests** (if exist for that section)
   ```bash
   # Python
   pytest /root/AlgoTrendy_v2.6/backend/python/tests/test_<section>.py

   # .NET
   dotnet test /root/AlgoTrendy_v2.6/backend/dotnet/TradingEngine.Tests/
   ```

2. **Integration Test** (section with dependencies)
   ```bash
   # Example: Test data channel writes to QuestDB
   python -m pytest tests/integration/test_binance_to_questdb.py
   ```

3. **Security Scan** (after MODIFY category changes)
   ```bash
   # Check for secrets
   gitleaks detect --source=/root/AlgoTrendy_v2.6/backend/ --no-git

   # Check for SQL injection patterns
   grep -r "text(f\"" /root/AlgoTrendy_v2.6/backend/
   ```

4. **Code Quality Check**
   ```bash
   # Python
   pylint /root/AlgoTrendy_v2.6/backend/python/<migrated_file>.py

   # .NET
   dotnet format /root/AlgoTrendy_v2.6/backend/dotnet/TradingEngine.sln --verify-no-changes
   ```

---

## ROLLBACK PLAN

### If Migration Section Fails

1. **Don't panic** - v2.5 is still intact
2. **Document the issue** in migration tracker
3. **Mark task as ❌ Blocked**
4. **Analyze root cause**
5. **Create fix plan**
6. **Test fix in isolation**
7. **Retry migration**

### Git Workflow (Recommended)

```bash
# Create v2.6 repository
cd /root/AlgoTrendy_v2.6
git init
git add .
git commit -m "Initial v2.6 structure"

# Create branch for each phase
git checkout -b phase-1-foundation
# ... do Phase 1 work ...
git add .
git commit -m "Complete Phase 1: Foundation"

# If something breaks, rollback
git reset --hard HEAD~1  # Back to previous commit

# When phase is solid, merge to main
git checkout main
git merge phase-1-foundation
git branch -d phase-1-foundation
```

---

## MIGRATION CHECKLIST MASTER

### Pre-Migration
- [ ] v2.6 directory structure created
- [ ] Migration tracker template created
- [ ] Daily log started
- [ ] Git repository initialized
- [ ] Secrets manager set up (Azure/AWS)
- [ ] QuestDB instance running
- [ ] PostgreSQL instance running
- [ ] .NET 8 SDK installed
- [ ] Python 3.11+ installed
- [ ] Node.js 20+ installed

### Phase 1 (Foundation)
- [ ] Config files migrated and sanitized
- [ ] Database schemas split (PG vs QuestDB)
- [ ] Secrets management implemented
- [ ] .NET solution structure created
- [ ] Broker abstraction ported to C#
- [ ] Bybit broker fully functional in C#
- [ ] All Phase 1 tests passing

### Phase 2 (Data Channels)
- [ ] Base channel migrated with SQL injection fix
- [ ] All 4 market data channels working with QuestDB
- [ ] All 4 news channels migrated
- [ ] 3 new sentiment channels created
- [ ] All channels tested with live data
- [ ] QuestDB ingestion verified

### Phase 3 (AI Agents)
- [ ] LangGraph environment set up
- [ ] MemGPT/Letta integrated
- [ ] Pinecone vector DB configured
- [ ] 5 agent nodes implemented
- [ ] Workflow tested end-to-end
- [ ] Agent control API created

### Phase 4 (Data Expansion)
- [ ] On-chain channels created
- [ ] DeFi channels created
- [ ] Whale Alert integrated
- [ ] All 16 total channels operational

### Phase 5 (Frontend)
- [ ] Next.js 15 project created
- [ ] SignalR client integrated
- [ ] All pages implemented
- [ ] Real-time updates working
- [ ] ML visualizations rendering
- [ ] Algorithm IDE functional

### Phase 6 (Testing & Deploy)
- [ ] Unit tests > 80% coverage
- [ ] Integration tests passing
- [ ] Load tests completed
- [ ] Security audit passed
- [ ] Production deployment successful
- [ ] Monitoring dashboards active

---

## DECISION LOG

### Key Architectural Decisions

| Date | Decision | Rationale | Alternatives Considered |
|------|----------|-----------|------------------------|
| 2025-10-18 | Use QuestDB instead of TimescaleDB | 3.5x faster per 2025 benchmarks, industry adoption | ClickHouse (more complex), InfluxDB (less SQL) |
| 2025-10-18 | Rewrite trading engine in .NET 8 | 10-100x faster than Python per QuantConnect data | Keep Python (too slow), Use C++ (harder to hire) |
| 2025-10-18 | Use LangGraph for AI agents | #1 framework for financial agents per AWS 2025 | AutoGPT (less controllable), CrewAI (smaller community) |
| 2025-10-18 | Next.js 15 for frontend | Best ML viz ecosystem, RSC performance | SvelteKit (smaller ecosystem), Angular (too heavy) |
| 2025-10-18 | Keep indicator engine in Python | NumPy/Pandas optimized, no latency requirement | Port to C# (unnecessary complexity) |

---

## SUMMARY

This migration plan ensures:

✅ **No Work Lost** - v2.5 remains intact, sections migrated methodically
✅ **No Redundancy** - Each file categorized (KEEP/MODIFY/REWRITE/DEPRECATE)
✅ **Optimal Approach** - Technology choices backed by 2025 industry research
✅ **Organized Process** - Phase-by-phase with tracking and testing

**Total Timeline:** 28 weeks
**Current Status:** Planning phase - No work has begun
**Next Step:** Review and approve this plan before starting Phase 1

---

**Document Version:** 1.0
**Last Updated:** October 18, 2025
**Status:** Ready for Review
