# AlgoTrendy v2.5 → v2.6: Complete File Inventory & Migration Map

**Last Updated:** October 18, 2025
**Purpose:** Comprehensive file-by-file migration instructions
**Status:** Planning Phase - No migration work has begun

---

## LEGEND

**Migration Categories:**
- `KEEP` = Copy as-is with minimal/no changes
- `MODIFY` = Copy and make specific fixes/improvements
- `REWRITE-CS` = Rewrite in C#/.NET
- `REWRITE-TS` = Rewrite in TypeScript/Next.js
- `NEW` = Create from scratch (doesn't exist in v2.5)
- `DEPRECATE` = Do not migrate, will be replaced

**Priority Levels:**
- 🔴 P0 = Critical (Phase 1)
- 🟡 P1 = High (Phase 2-3)
- 🟢 P2 = Medium (Phase 4-5)
- ⚪ P3 = Low (Phase 6)

---

## SECTION 1: CORE TRADING ENGINE

### 1.1 Main Trader Components

| # | Source File | v2.5 LOC | Category | Priority | v2.6 Destination | Notes |
|---|-------------|----------|----------|----------|------------------|-------|
| 1 | `algotrendy/unified_trader.py` | 650 | REWRITE-CS | 🔴 P0 | `backend/dotnet/TradingEngine.Core/UnifiedTrader.cs` | Port to C# with idempotency, rate limiting, lock-free structures |
| 2 | `algotrendy/base_trader.py` | 400 | REWRITE-CS | 🔴 P0 | `backend/dotnet/TradingEngine.Core/BaseTrader.cs` | Abstract base class for all traders |
| 3 | `algotrendy/broker_abstraction.py` | 400 | REWRITE-CS | 🔴 P0 | `backend/dotnet/TradingEngine.Brokers/IBroker.cs` + implementations | Fix: Add idempotency keys, implement all 6 brokers |
| 4 | `algotrendy/strategy_resolver.py` | 300 | REWRITE-CS | 🟡 P1 | `backend/dotnet/TradingEngine.Core/StrategyResolver.cs` | Load strategies dynamically from config |
| 5 | `algotrendy/indicator_engine.py` | 500 | KEEP | 🟡 P1 | `backend/python/analytics/indicator_engine.py` | Keep in Python (NumPy/Pandas optimized) |
| 6 | `algotrendy/signal_processor.py` | 200 | REWRITE-CS | 🟡 P1 | `backend/dotnet/TradingEngine.Core/SignalProcessor.cs` | Port signal generation logic |

**Migration Steps for #1 (unified_trader.py → UnifiedTrader.cs):**

1. Read Python implementation thoroughly
2. Document all methods and their purposes
3. Create C# interface `ITrader`
4. Implement base functionality
5. Add critical security fixes:
   - Idempotent order placement
   - Lock-free position tracking (ConcurrentDictionary)
   - Rate limiting per broker
   - Retry logic with exponential backoff
6. Unit test each method
7. Integration test with testnet

**Security Fixes Required:**
- ✅ Add `Guid` idempotency keys to all order methods
- ✅ Replace dict with `ConcurrentDictionary` for positions
- ✅ Add `asyncio.Lock` equivalent (`SemaphoreSlim` in C#)
- ✅ Implement token bucket rate limiter

---

### 1.2 Configuration & Security

| # | Source File | v2.5 LOC | Category | Priority | v2.6 Destination | Notes |
|---|-------------|----------|----------|----------|------------------|-------|
| 7 | `algotrendy/config_manager.py` | 200 | MODIFY | 🔴 P0 | `backend/python/config/config_manager.py` | Fix: Add range validation, remove magic numbers |
| 8 | `algotrendy/secure_credentials.py` | 350 | DEPRECATE | 🔴 P0 | (Replaced by Azure Key Vault/AWS Secrets Manager) | Custom vault replaced by cloud secrets |
| 9 | `algotrendy/configs/*.json` | - | MODIFY | 🔴 P0 | `backend/python/configs/*.json` | **CRITICAL**: Remove all hardcoded API keys/secrets, replace with `${ENV_VAR}` |

**Migration Steps for #9 (Config JSON files):**

```bash
# Step 1: Copy all config files
cp -r /root/algotrendy_v2.5/algotrendy/configs/* \
      /root/AlgoTrendy_v2.6/backend/python/configs/

# Step 2: Scan for secrets
grep -r "api_key\|secret\|password" /root/AlgoTrendy_v2.6/backend/python/configs/

# Step 3: Manual replacement in each file
# BEFORE:
{
  "broker": "bybit",
  "api_key": "ACTUAL_KEY_12345",
  "api_secret": "ACTUAL_SECRET_67890"
}

# AFTER:
{
  "broker": "bybit",
  "api_key": "${BYBIT_API_KEY}",
  "api_secret": "${BYBIT_API_SECRET}"
}

# Step 4: Create .env.example template
cat > /root/AlgoTrendy_v2.6/.env.example << EOF
# Bybit
BYBIT_API_KEY=your_bybit_key_here
BYBIT_API_SECRET=your_bybit_secret_here

# Binance
BINANCE_API_KEY=your_binance_key_here
BINANCE_API_SECRET=your_binance_secret_here
EOF
```

---

### 1.3 Task Queue & Scheduling

| # | Source File | v2.5 LOC | Category | Priority | v2.6 Destination | Notes |
|---|-------------|----------|----------|----------|------------------|-------|
| 10 | `algotrendy/celery_app.py` | 150 | KEEP | 🟡 P1 | `backend/python/celery_app.py` | Working config, just update Redis connection |
| 11 | `algotrendy/tasks.py` | 400 | MODIFY | 🔴 P0 | `backend/python/tasks.py` | **CRITICAL FIX**: SQL injection at line 363-366 |

**Critical Fix for #11 (tasks.py SQL injection):**

```python
# BEFORE (v2.5) - LINE 363-366 - SQL INJECTION VULNERABILITY
result = db.execute(text(f"""
    SELECT compress_chunk(i)
    FROM show_chunks('{table_name}', older_than => INTERVAL '7 days') i
"""))

# AFTER (v2.6) - PARAMETERIZED QUERY
from sqlalchemy import text

result = db.execute(
    text("""
        SELECT compress_chunk(i)
        FROM show_chunks(:table_name, older_than => INTERVAL '7 days') i
    """),
    {"table_name": table_name}
)
```

**Other Fixes for #11:**
- Replace `asyncio.run()` in Celery tasks (lines 49, 71, 102, 132, 162, 192)
- Use native async Celery tasks instead

---

## SECTION 2: DATA CHANNELS

### 2.1 Base Channel Framework

| # | Source File | v2.5 LOC | Category | Priority | v2.6 Destination | Notes |
|---|-------------|----------|----------|----------|------------------|-------|
| 12 | `algotrendy/data_channels/base.py` | 350 | MODIFY | 🔴 P0 | `backend/python/data_channels/base.py` | Fix SQL injection in `_log_ingestion` method |
| 13 | `algotrendy/data_channels/manager.py` | 200 | KEEP | 🟡 P1 | `backend/python/data_channels/manager.py` | Good orchestration logic |
| 14 | `algotrendy/data_channels/exceptions.py` | 50 | KEEP | 🟡 P1 | `backend/python/data_channels/exceptions.py` | Custom exception classes |

**Critical Fix for #12 (base.py SQL injection):**

See similar fix pattern as tasks.py above. Also update database writes to use QuestDB for time-series data.

---

### 2.2 Market Data Channels (WORKING)

| # | Source File | v2.5 LOC | Category | Priority | v2.6 Destination | Notes |
|---|-------------|----------|----------|----------|------------------|-------|
| 15 | `data_channels/market_data/binance.py` | 200 | MODIFY | 🟡 P1 | `backend/python/data_channels/market_data/binance.py` | Change TimescaleDB → QuestDB |
| 16 | `data_channels/market_data/okx.py` | 150 | MODIFY | 🟡 P1 | `backend/python/data_channels/market_data/okx.py` | Change TimescaleDB → QuestDB |
| 17 | `data_channels/market_data/coinbase.py` | 150 | MODIFY | 🟡 P1 | `backend/python/data_channels/market_data/coinbase.py` | Change TimescaleDB → QuestDB |
| 18 | `data_channels/market_data/kraken.py` | 150 | MODIFY | 🟡 P1 | `backend/python/data_channels/market_data/kraken.py` | Change TimescaleDB → QuestDB |

**QuestDB Integration Pattern (apply to all 4 channels):**

```python
# BEFORE (v2.5) - TimescaleDB via asyncpg
async def write_to_db(self, candles: List[Dict]):
    async with self.db_pool.acquire() as conn:
        await conn.executemany("""
            INSERT INTO market_data (symbol, open, high, low, close, volume, timestamp)
            VALUES ($1, $2, $3, $4, $5, $6, $7)
        """, [(c['symbol'], c['open'], c['high'], c['low'], c['close'], c['volume'], c['timestamp'])
              for c in candles])

# AFTER (v2.6) - QuestDB via InfluxDB Line Protocol
from questdb.ingress import Sender

async def write_to_questdb(self, candles: List[Dict]):
    with Sender('localhost', 9009) as sender:
        for candle in candles:
            sender.row(
                'ohlcv',  # table name
                symbols={'symbol': candle['symbol'], 'exchange': 'binance', 'timeframe': '5m'},
                columns={
                    'open': candle['open'],
                    'high': candle['high'],
                    'low': candle['low'],
                    'close': candle['close'],
                    'volume': candle['volume']
                },
                at=candle['timestamp']  # timestamp
            )
        sender.flush()
```

---

### 2.3 News Channels (MOSTLY WORKING)

| # | Source File | v2.5 LOC | Category | Priority | v2.6 Destination | Notes |
|---|-------------|----------|----------|----------|------------------|-------|
| 19 | `data_channels/news/polygon.py` | 180 | MODIFY | 🟡 P1 | `backend/python/data_channels/news/polygon.py` | Update API version, add error handling |
| 20 | `data_channels/news/cryptopanic.py` | 170 | KEEP | 🟡 P1 | `backend/python/data_channels/news/cryptopanic.py` | Good implementation |
| 21 | `data_channels/news/fmp.py` | 160 | MODIFY | 🔴 P0 | `backend/python/data_channels/news/fmp.py` | **FIX**: Bare except at line 108-109 |
| 22 | `data_channels/news/yahoo.py` | 140 | KEEP | 🟡 P1 | `backend/python/data_channels/news/yahoo.py` | RSS parser, works fine |

**Critical Fix for #21 (fmp.py bare except):**

```python
# BEFORE (v2.5) - LINE 108-109
try:
    timestamp = datetime.fromisoformat(article['publishedDate'])
except:  # BAD - catches everything including KeyboardInterrupt
    timestamp = datetime.utcnow()

# AFTER (v2.6) - SPECIFIC EXCEPTION
try:
    timestamp = datetime.fromisoformat(article['publishedDate'])
except (ValueError, KeyError, TypeError) as e:
    logger.warning(f"Invalid date format for article: {e}")
    timestamp = datetime.utcnow()
```

---

### 2.4 Sentiment Channels (EMPTY - CREATE NEW)

| # | Source File | v2.5 LOC | Category | Priority | v2.6 Destination | Notes |
|---|-------------|----------|----------|----------|------------------|-------|
| 23 | `data_channels/sentiment/` (empty) | 0 | NEW | 🟢 P2 | `backend/python/data_channels/sentiment/reddit.py` | Create Reddit channel using PRAW |
| 24 | - | 0 | NEW | 🟢 P2 | `backend/python/data_channels/sentiment/twitter.py` | Create Twitter/X channel |
| 25 | - | 0 | NEW | 🟢 P2 | `backend/python/data_channels/sentiment/lunarcrush.py` | Create LunarCrush API integration |

**Template for #23 (Reddit Sentiment Channel):**

```python
import praw
from textblob import TextBlob
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
        self.subreddits = ['cryptocurrency', 'bitcoin', 'ethtrader', 'cryptomarkets']

    async def fetch_data(self) -> List[Dict]:
        posts = []
        for subreddit_name in self.subreddits:
            subreddit = self.reddit.subreddit(subreddit_name)
            for submission in subreddit.hot(limit=25):
                text = f"{submission.title} {submission.selftext}"
                sentiment = TextBlob(text).sentiment.polarity

                posts.append({
                    'source': f'reddit_{subreddit_name}',
                    'title': submission.title,
                    'url': submission.url,
                    'score': submission.score,
                    'num_comments': submission.num_comments,
                    'sentiment': sentiment,  # -1 to +1
                    'sentiment_label': self._categorize_sentiment(sentiment),
                    'timestamp': datetime.fromtimestamp(submission.created_utc),
                    'symbols': self._extract_symbols(text)
                })
        return posts

    def _categorize_sentiment(self, score: float) -> str:
        if score > 0.1:
            return 'positive'
        elif score < -0.1:
            return 'negative'
        else:
            return 'neutral'

    def _extract_symbols(self, text: str) -> List[str]:
        # Extract $BTC, $ETH, etc.
        import re
        return re.findall(r'\$([A-Z]{2,5})\b', text.upper())
```

---

### 2.5 On-Chain Channels (EMPTY - CREATE NEW)

| # | Source File | v2.5 LOC | Category | Priority | v2.6 Destination | Notes |
|---|-------------|----------|----------|----------|------------------|-------|
| 26 | `data_channels/onchain/` (empty) | 0 | NEW | 🟢 P2 | `backend/python/data_channels/onchain/glassnode.py` | Glassnode API integration |
| 27 | - | 0 | NEW | 🟢 P2 | `backend/python/data_channels/onchain/intotheblock.py` | IntoTheBlock API |
| 28 | - | 0 | NEW | 🟢 P2 | `backend/python/data_channels/onchain/whale_alert.py` | Whale Alert API |

---

### 2.6 Alternative Data Channels (EMPTY - CREATE NEW)

| # | Source File | v2.5 LOC | Category | Priority | v2.6 Destination | Notes |
|---|-------------|----------|----------|----------|------------------|-------|
| 29 | `data_channels/alt_data/` (empty) | 0 | NEW | 🟢 P2 | `backend/python/data_channels/alt_data/defillama.py` | DeFi TVL metrics |
| 30 | - | 0 | NEW | 🟢 P2 | `backend/python/data_channels/alt_data/coingecko.py` | CoinGecko aggregator |
| 31 | - | 0 | NEW | 🟢 P2 | `backend/python/data_channels/alt_data/fear_greed.py` | Fear & Greed Index |

---

## SECTION 3: API LAYER (FastAPI)

### 3.1 Main API Application

| # | Source File | v2.5 LOC | Category | Priority | v2.6 Destination | Notes |
|---|-------------|----------|----------|----------|------------------|-------|
| 32 | `algotrendy-api/app/main.py` | 800 | REWRITE-CS | 🔴 P0 | `backend/dotnet/TradingEngine.API/Program.cs` + Controllers | Rewrite in ASP.NET Core Minimal APIs |
| 33 | `algotrendy-api/app/core/config.py` | 150 | MODIFY | 🔴 P0 | `backend/dotnet/TradingEngine.API/appsettings.json` | **FIX**: Hardcoded SECRET_KEY at line 27 |
| 34 | `algotrendy-api/app/auth.py` | 100 | DEPRECATE | 🔴 P0 | (Replaced by ASP.NET Identity + JWT) | Demo users replaced with real auth |
| 35 | `algotrendy-api/app/cache.py` | 80 | DEPRECATE | 🔴 P0 | (Replaced by .NET IMemoryCache) | Disabled module, replace with .NET caching |
| 36 | `algotrendy-api/app/monitoring.py` | 120 | DEPRECATE | 🔴 P0 | (Replaced by .NET OpenTelemetry) | Disabled module, use .NET monitoring |
| 37 | `algotrendy-api/app/db_pool.py` | 100 | DEPRECATE | 🔴 P0 | (Replaced by EF Core connection pooling) | .NET handles pooling automatically |

**ASP.NET Core Program.cs Template (for #32):**

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using TradingEngine.Core;
using TradingEngine.Brokers;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddSignalR();  // For real-time WebSocket
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// Add authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

// Add trading services
builder.Services.AddSingleton<IBrokerFactory, BrokerFactory>();
builder.Services.AddScoped<ITrader, UnifiedTrader>();
builder.Services.AddScoped<IRateLimiter, TokenBucketRateLimiter>();

var app = builder.Build();

// Configure middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<TradingHub>("/hubs/trading");  // SignalR hub

app.Run();
```

---

### 3.2 API Endpoints (Rewrite in .NET Controllers)

| # | Source File | v2.5 LOC | Category | Priority | v2.6 Destination | Notes |
|---|-------------|----------|----------|----------|------------------|-------|
| 38 | N/A (main.py endpoints) | - | REWRITE-CS | 🔴 P0 | `backend/dotnet/TradingEngine.API/Controllers/PortfolioController.cs` | GET /api/portfolio, /api/portfolio/positions |
| 39 | N/A (main.py endpoints) | - | REWRITE-CS | 🔴 P0 | `backend/dotnet/TradingEngine.API/Controllers/OrdersController.cs` | POST /api/orders (with idempotency) |
| 40 | N/A (main.py endpoints) | - | REWRITE-CS | 🔴 P0 | `backend/dotnet/TradingEngine.API/Controllers/MarketsController.cs` | GET /api/market/data, /api/market/ohlcv/{symbol} |
| 41 | N/A (main.py endpoints) | - | REWRITE-CS | 🟡 P1 | `backend/dotnet/TradingEngine.API/Controllers/StrategiesController.cs` | GET /api/strategies |
| 42 | N/A (main.py endpoints) | - | NEW | 🟡 P1 | `backend/dotnet/TradingEngine.API/Hubs/TradingHub.cs` | SignalR hub for real-time streaming |

**Example Controller (for #38 - Portfolio):**

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]  // Requires JWT
public class PortfolioController : ControllerBase
{
    private readonly ITrader _trader;
    private readonly IMemoryCache _cache;

    public PortfolioController(ITrader trader, IMemoryCache cache)
    {
        _trader = trader;
        _cache = cache;
    }

    [HttpGet]
    public async Task<ActionResult<PortfolioSummary>> GetPortfolio()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var cacheKey = $"portfolio:{userId}";

        if (!_cache.TryGetValue(cacheKey, out PortfolioSummary? summary))
        {
            summary = await _trader.GetPortfolioSummaryAsync();
            _cache.Set(cacheKey, summary, TimeSpan.FromSeconds(5));  // Cache for 5s
        }

        return Ok(summary);
    }

    [HttpGet("positions")]
    public async Task<ActionResult<List<Position>>> GetPositions()
    {
        var positions = await _trader.GetActivePositionsAsync();
        return Ok(positions);
    }
}
```

---

### 3.3 Backtesting Module (Keep in Python)

| # | Source File | v2.5 LOC | Category | Priority | v2.6 Destination | Notes |
|---|-------------|----------|----------|----------|------------------|-------|
| 43 | `algotrendy-api/app/backtesting/engines.py` | 800 | MODIFY | 🟡 P1 | `backend/python/backtesting/engines.py` | Fix: Connect to real historical data (not mock) |
| 44 | `algotrendy-api/app/backtesting/indicators.py` | 300 | KEEP | 🟡 P1 | `backend/python/backtesting/indicators.py` | Reuse indicator engine |
| 45 | `algotrendy-api/app/backtesting/models.py` | 200 | KEEP | 🟡 P1 | `backend/python/backtesting/models.py` | Pydantic models |

**Fix for #43 (engines.py mock data):**

```python
# BEFORE (v2.5) - LINE 127
async def fetch_historical_data(self, symbol: str, start: datetime, end: datetime):
    # TODO: Replace with actual data fetching from database or API
    return self._generate_mock_data(symbol, start, end)  # FAKE DATA

# AFTER (v2.6) - FETCH FROM QUESTDB
async def fetch_historical_data(self, symbol: str, start: datetime, end: datetime):
    from questdb.ingress import Sender
    import psycopg2

    conn = psycopg2.connect(
        host='localhost',
        port=8812,  # QuestDB PostgreSQL wire protocol
        database='qdb',
        user='admin',
        password='quest'
    )

    cursor = conn.cursor()
    cursor.execute("""
        SELECT timestamp, open, high, low, close, volume
        FROM ohlcv
        WHERE symbol = %s
          AND timestamp BETWEEN %s AND %s
        ORDER BY timestamp ASC
    """, (symbol, start, end))

    rows = cursor.fetchall()
    cursor.close()
    conn.close()

    return [
        {
            'timestamp': row[0],
            'open': row[1],
            'high': row[2],
            'low': row[3],
            'close': row[4],
            'volume': row[5]
        }
        for row in rows
    ]
```

---

## SECTION 4: DATABASE

### 4.1 Schema Files

| # | Source File | v2.5 LOC | Category | Priority | v2.6 Destination | Notes |
|---|-------------|----------|----------|----------|------------------|-------|
| 46 | `database/schema.sql` | 500 | MODIFY | 🔴 P0 | Split into 2 files:<br>`database/postgresql/schemas/relational.sql`<br>`database/questdb/schemas/timeseries.sql` | Separate relational from time-series |

**Migration Steps for #46:**

1. Copy original schema.sql
2. Identify time-series tables (market_data, tick_data, bar_data, signals, trades)
3. Remove hypertable declarations
4. Create separate QuestDB schema with PARTITION BY clauses
5. Keep user, portfolio, audit_logs in PostgreSQL

---

### 4.2 Migrations

| # | Source File | v2.5 LOC | Category | Priority | v2.6 Destination | Notes |
|---|-------------|----------|----------|----------|------------------|-------|
| 47 | `database/migrations/*.sql` (Alembic) | - | REWRITE-CS | 🟡 P1 | `backend/dotnet/TradingEngine.API/Migrations/` | Convert to EF Core migrations |

**Convert to EF Core:**

```bash
cd /root/AlgoTrendy_v2.6/backend/dotnet/TradingEngine.API

# Create initial migration
dotnet ef migrations add InitialCreate

# This generates C# migration files instead of SQL
```

---

## SECTION 5: FRONTEND (Complete Rewrite)

### 5.1 Next.js Frontend (v2.5 has old Next.js/React)

| # | Source File | v2.5 LOC | Category | Priority | v2.6 Destination | Notes |
|---|-------------|----------|----------|----------|------------------|-------|
| 48 | `algotrendy-web/*` | ~5000 | REWRITE-TS | 🟢 P2 | `frontend/nextjs-app/` | **COMPLETE REWRITE** in Next.js 15 + React 19 |
| 49 | `desktop_app/*` | ~3000 | REWRITE-TS | ⚪ P3 | `frontend/electron-app/` | Rebuild with Electron + React 19 |

**DO NOT copy any v2.5 frontend code directly**

Instead:
1. Document existing pages and their purposes
2. Document API calls they make
3. Redesign UI/UX from scratch
4. Build with Next.js 15 App Router + React Server Components

---

## SECTION 6: TESTING

### 6.1 Unit Tests

| # | Source File | v2.5 LOC | Category | Priority | v2.6 Destination | Notes |
|---|-------------|----------|----------|----------|------------------|-------|
| 50 | `algotrendy/tests/unit/test_strategy_resolver.py` | 150 | KEEP | 🟡 P1 | `backend/python/tests/unit/test_strategy_resolver.py` | Good tests, keep |
| 51 | `algotrendy/tests/unit/test_indicator_engine.py` | 200 | KEEP | 🟡 P1 | `backend/python/tests/unit/test_indicator_engine.py` | Good tests, keep |
| 52 | `algotrendy/tests/unit/test_config_manager.py` | 100 | KEEP | 🟡 P1 | `backend/python/tests/unit/test_config_manager.py` | Good tests, keep |

**Create NEW tests for .NET:**
- `backend/dotnet/TradingEngine.Tests/UnifiedTraderTests.cs`
- `backend/dotnet/TradingEngine.Tests/BrokerTests.cs`
- `backend/dotnet/TradingEngine.Tests/RateLimiterTests.cs`

---

## SECTION 7: SCRIPTS & UTILITIES

### 7.1 Deployment Scripts

| # | Source File | v2.5 LOC | Category | Priority | v2.6 Destination | Notes |
|---|-------------|----------|----------|----------|------------------|-------|
| 53 | `scripts/deployment/*.py` | Various | DEPRECATE | ⚪ P3 | N/A | TradingView scripts - assess if still needed |
| 54 | `scripts/setup/setup_credentials.py` | 200 | DEPRECATE | 🔴 P0 | (Replaced by Azure Key Vault setup scripts) | Use cloud secrets manager |
| 55 | `scripts/test/*.py` | 300 | KEEP | 🟡 P1 | `scripts/test/` | Test scripts, update paths |

---

## SECTION 8: DOCUMENTATION

### 8.1 Markdown Files

| # | Source File | v2.5 LOC | Category | Priority | v2.6 Destination | Notes |
|---|-------------|----------|----------|----------|------------------|-------|
| 56 | `TODO_TRACKING.md` | - | DEPRECATE | ⚪ P3 | N/A | Old TODOs, use v2.6 tracker instead |
| 57 | `DATA_ARCHITECTURE_ANALYSIS.md` | - | KEEP | 🟢 P2 | `docs/v2.5_legacy/DATA_ARCHITECTURE_ANALYSIS.md` | Archive for reference |
| 58 | `BACKTESTING_MODULE_COMPLETE.md` | - | KEEP | 🟢 P2 | `docs/v2.5_legacy/BACKTESTING_MODULE_COMPLETE.md` | Archive for reference |

---

## SUMMARY STATISTICS

### By Migration Category

| Category | Count | Estimated Hours |
|----------|-------|----------------|
| KEEP AS-IS | 12 files | 20-30 hours (copy + test) |
| MODIFY | 15 files | 80-120 hours (fix + test) |
| REWRITE-CS | 20 files | 400-500 hours (rewrite in C#) |
| REWRITE-TS | 2 sections | 240-300 hours (Next.js rebuild) |
| NEW | 11 files | 180-240 hours (create from scratch) |
| DEPRECATE | 8 files | 0 hours (do not migrate) |
| **TOTAL** | **68 items** | **920-1,190 hours** |

### By Priority

| Priority | Count | Est. Hours | Phase |
|----------|-------|-----------|-------|
| 🔴 P0 (Critical) | 15 | 200-250 | Phase 1 (Week 1-4) |
| 🟡 P1 (High) | 24 | 350-450 | Phase 2-3 (Week 5-12) |
| 🟢 P2 (Medium) | 20 | 280-350 | Phase 4-5 (Week 13-24) |
| ⚪ P3 (Low) | 9 | 90-140 | Phase 6 (Week 25-28) |

---

## PHASE-ALIGNED MIGRATION ORDER

### Phase 1: Foundation (Week 1-4) - Files #7-11, #32-36, #46

1. Migrate & sanitize config JSONs (#9)
2. Fix SQL injection in tasks.py (#11)
3. Fix SQL injection in base.py (#12)
4. Split database schemas (#46)
5. Set up .NET solution (#32)
6. Implement secrets management (replace #8)

### Phase 2: Real-Time Infrastructure (Week 5-8) - Files #1-6, #15-18, #42

1. Port Bybit broker to C# (#3)
2. Port unified trader to C# (#1)
3. Migrate market data channels (#15-18)
4. Implement SignalR hub (#42)
5. Connect QuestDB

### Phase 3: AI Agents (Week 9-12) - NEW files for agents

1. Create LangGraph workflows (NEW)
2. Integrate MemGPT (NEW)
3. Build 5 agent nodes (NEW)

### Phase 4: Data Expansion (Week 13-16) - Files #23-31

1. Create sentiment channels (#23-25)
2. Create on-chain channels (#26-28)
3. Create alt data channels (#29-31)

### Phase 5: Frontend (Week 17-24) - File #48

1. Build Next.js 15 app from scratch (#48)
2. Integrate SignalR client
3. Build all pages

### Phase 6: Testing & Deploy (Week 25-28) - Files #50-52 + NEW

1. Port existing tests (#50-52)
2. Create new .NET tests
3. Load testing
4. Production deployment

---

## FILE MIGRATION CHECKLIST TEMPLATE

For each file, complete this checklist:

```markdown
## File: algotrendy/unified_trader.py → TradingEngine.Core/UnifiedTrader.cs

- [ ] Read and understand v2.5 implementation
- [ ] Document all methods and their purposes
- [ ] Identify dependencies
- [ ] List security issues to fix
- [ ] Create v2.6 file structure
- [ ] Implement/port functionality
- [ ] Add security improvements
- [ ] Write unit tests
- [ ] Integration test
- [ ] Code review
- [ ] Update migration tracker
- [ ] Mark as complete ✅
```

---

**Document Status:** Planning Complete - Ready for Implementation
**Next Step:** Review with stakeholders, begin Phase 1 migration
**Total Files to Migrate:** 68
**Estimated Total Effort:** 920-1,190 hours
