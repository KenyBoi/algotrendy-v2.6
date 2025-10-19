# AlgoTrendy v2.5 - AI Agent Quick Reference Card

**File**: `.github/copilot-instructions.md` | **Lines**: ~1,350 | **Updated**: October 16, 2025

---

## 🎯 Core Principle
**Configuration Over Code** → One unified trader + JSON variants = 480+ unique configurations

---

## 🔑 5-Minute Architecture

```
┌─────────────────────────────────────────────────────┐
│         AlgoTrendy v2.5 - 3 Main Components         │
├─────────────────────────────────────────────────────┤
│                                                     │
│  🐍 Python Backend (Trading Engine)                │
│  ├─ unified_trader.py (main logic)                 │
│  ├─ broker_abstraction.py (8 brokers)              │
│  ├─ strategy_resolver.py (5 strategies)            │
│  ├─ indicator_engine.py (RSI, MACD, etc.)          │
│  └─ secure_credentials.py (vault + audit)          │
│                                                     │
│  🎨 Next.js Frontend                               │
│  ├─ src/pages/ (route pages)                       │
│  ├─ src/store/ (Zustand state)                     │
│  ├─ src/components/ (UI components)                │
│  ├─ src/services/api.ts (API client)               │
│  └─ src/types/ (TypeScript definitions)            │
│                                                     │
│  ⚡ FastAPI Backend API                            │
│  ├─ app/main.py (routes)                           │
│  ├─ app/schemas.py (Pydantic models)               │
│  └─ database (PostgreSQL + SQLAlchemy)             │
│                                                     │
└─────────────────────────────────────────────────────┘
```

---

## 🔄 Variant Dimensions (480 = 8 × 5 × 3 × 4)

```json
{
  "variants": {
    "broker": "bybit",        // 8: bybit, alpaca, binance, okx, kraken, deribit, ftx, gemini
    "strategy": "momentum",   // 5: momentum, rsi, macd, mfi, vwap
    "mode": "live",           // 3: live, paper, backtest
    "asset_class": "crypto"   // 4: crypto, stocks, futures, etf
  }
}
```

---

## ⚡ Quick Commands

```bash
# 🚀 Start Trading (Python)
export BYBIT_API_KEY="xxx" && export BYBIT_API_SECRET="yyy"
python -m algotrendy.unified_trader algotrendy/configs/bybit_crypto_momentum_live.json

# 🎨 Start Frontend (Next.js)
cd algotrendy-web && npm run dev

# ⚙️ Start Backend API (FastAPI)
cd algotrendy-api && uvicorn app.main:app --reload

# 🧪 Run Tests
pytest algotrendy/tests/unit --cov
npm test -- --watch
cd algotrendy-web && npm run type-check

# 🐳 Docker Everything
docker-compose up --build
```

---

## 📋 Pattern Cheat Sheet

| Problem | Pattern | File | Example |
|---------|---------|------|---------|
| Add new broker | BrokerAdapter + register in BrokerFactory | `broker_abstraction.py` | NewBrokerAdapter class |
| Add new strategy | BaseStrategy + register in STRATEGIES dict | `strategy_resolver.py` | BollingerBandsStrategy |
| Add new indicator | BaseIndicator + register in INDICATORS dict | `indicator_engine.py` | ADXIndicator |
| Frontend state | Zustand store (separate by domain) | `store/authStore.ts` | useAuthStore |
| API calls | useQuery hooks with TanStack | `services/api.ts` | usePositions hook |
| Components | Presentational ≠ Container | `components/` | PositionsTable (pure) |
| Config variant | JSON in `configs/` | `configs/broker_strategy_mode.json` | bybit_crypto_momentum_live.json |
| Security | Env vars + secure vault | `secure_credentials.py` | get_credential_manager() |

---

## ✅ Testing Levels

```
Unit Tests
  └─ <100ms per test
  └─ pytest with fixtures + parametrization
  └─ Mocked dependencies (no DB, no API)

Integration Tests
  └─ 1-10s per test
  └─ Component interaction
  └─ Mocked external services

E2E Tests
  └─ 10-60s per test
  └─ Full trading workflows
  └─ Real or paper trading accounts

CI/CD
  └─ Matrix: Python 3.8-3.10, Node 18
  └─ Coverage reports
  └─ Type checking
```

---

## 🚫 Common Pitfalls

| ❌ Wrong | ✅ Right |
|---------|----------|
| `memgpt_new_broker_trader.py` | Add broker adapter + config variant |
| `BYBIT_API_KEY = "xxx"` in code | `export BYBIT_API_KEY=xxx` in env |
| Different logic per broker | Signal logic in strategy, broker handles API |
| Direct axios in components | Use useQuery hooks for caching |
| Multiple assertions per test | One assertion = one failure point |
| TypeScript with `any` type | `strict: true` everywhere |
| Hardcoded URLs | `process.env.NEXT_PUBLIC_API_URL` |
| Test database in tests | Use fixtures + mocks |

---

## 🏗️ Frontend Architecture

```typescript
// Type-safe types
export interface Position {
  id: string;
  symbol: string;
  side: 'long' | 'short';
  // ...
}

// Zustand stores (domain-separated)
export const useTradingStore = create((set) => ({
  positions: [],
  setPositions: (p) => set({ positions: p }),
}));

// API hooks
export function usePositions() {
  return useQuery({
    queryKey: ['positions'],
    queryFn: tradingApi.getPositions,
  });
}

// Presentational component
export const PositionsTable = ({ positions, onClose }) => (
  <table>
    {positions.map(p => (
      <tr key={p.id}>...</tr>
    ))}
  </table>
);

// Container component
export default function Dashboard() {
  const { data: positions } = usePositions();
  return <PositionsTable positions={positions} onClose={...} />;
}
```

---

## 🔐 Credential Security

```python
# ✅ CORRECT: Encrypted vault + audit log
from secure_credentials import get_credential_manager

cred_manager = get_credential_manager()
credentials = cred_manager.get_broker_credentials("bybit")
# Automatically encrypted, logged, and rotatable

# ❌ WRONG: Hardcoded anywhere
config = {"api_key": "xxx"}  # SECURITY RISK
```

---

## 🐳 Docker Quick Start

```bash
# Build all services
docker-compose up --build

# Run migrations
docker-compose exec backend alembic upgrade head

# Check health
curl http://localhost:8000/health
curl http://localhost:3000/

# View logs
docker-compose logs -f backend
```

---

## 📊 Testing Command Reference

```bash
# All tests with coverage
pytest --cov=algotrendy --cov-report=html

# Only unit tests (fast)
pytest algotrendy/tests/unit -v

# Specific test
pytest algotrendy/tests/unit/test_strategy_resolver.py::test_momentum_strategy -vv

# With markers
pytest -m "broker_bybit"
pytest -m "not slow"

# Frontend tests
npm test -- --watch
npm test -- --coverage
npm test -- PositionsTable
```

---

## 📁 Key File Map

```
algotrendy/
├── unified_trader.py           ← Main trader template
├── strategy_resolver.py        ← Strategy registry
├── broker_abstraction.py       ← Broker interface
├── secure_credentials.py       ← Credential vault
├── configs/                    ← Trading variants (JSON)
└── tests/                      ← Unit/integration/e2e tests

algotrendy-api/
├── app/main.py                 ← API routes
├── app/schemas.py              ← Pydantic models
└── requirements.txt            ← Dependencies

algotrendy-web/
├── src/pages/                  ← Route pages
├── src/store/                  ← Zustand stores
├── src/components/             ← UI components
├── src/services/api.ts         ← API client
├── src/types/index.ts          ← TypeScript definitions
└── package.json                ← Dependencies
```

---

## 🎓 Learning Path

1. **Start**: Read `Architecture Overview` (understand system)
2. **Learn**: Read `Critical Patterns` (learn conventions)
3. **Build**: Try `Developer Workflows` (get running)
4. **Test**: Follow `Testing Strategy` (validate changes)
5. **Extend**: Use `Extensibility Patterns` (add features)
6. **Deploy**: Follow `Deployment & DevOps` (go live)

---

## 💡 Remember

- **Config > Code**: Use variants, not code duplication
- **Vault > Hardcode**: Never put secrets in files
- **Type > Any**: Use strict TypeScript everywhere
- **Test > Hope**: Unit + Integration + E2E coverage
- **Compose > Copy**: Reuse components via props
- **Abstract > Concrete**: Use interfaces for flexibility

---

**Document**: `.github/copilot-instructions.md` (1,350 lines)  
**Summary**: `COPILOT_INSTRUCTIONS_GUIDE.md` (this file)  
**Quick Ref**: `COPILOT_INSTRUCTIONS_QUICKREF.md` (this file)  
**Updated**: October 16, 2025
