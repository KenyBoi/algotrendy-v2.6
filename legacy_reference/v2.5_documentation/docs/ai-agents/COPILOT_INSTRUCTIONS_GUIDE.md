# AI Agent Instructions - Complete Guide
## `.github/copilot-instructions.md` Overview

> **Date**: October 16, 2025  
> **Status**: Comprehensive AI Agent Documentation Complete  
> **Total Content**: ~1,350 lines covering all critical patterns

---

## 📋 What's Documented

The `.github/copilot-instructions.md` file now includes comprehensive guidance for AI agents across four major areas:

### **1. 🏗️ Architecture Overview** (Core Knowledge)
- **Trading Engine**: Unified trader with 480+ config variants
- **Broker Abstraction**: Interface-based design for 8 brokers
- **Strategy System**: Dynamic strategy loading and composition
- **Frontend Stack**: Next.js 14 + React 18 + Zustand
- **Backend API**: FastAPI with Pydantic validation

### **2. 🔑 Critical Patterns & Conventions** (Immediate Productivity)
- **Variant-Driven Configuration** ⭐ CORE: Configuration over code, 1 trader → infinite combinations
- **Secure Credentials**: Environment variables + encrypted vault (never hardcode!)
- **Broker Adapter Pattern**: Abstract base + runtime injection
- **Strategy Pattern**: BaseStrategy abstraction with marker-based composition
- **Zustand State Management**: Multi-store architecture (auth, trading, ui)
- **TanStack Query Pattern**: API hooks with caching + refetch logic

### **3. 🛠️ Developer Workflows** (Get Running Fast)
```bash
# Trading Engine
export BYBIT_API_KEY="..." && python -m algotrendy.unified_trader config.json

# Frontend
cd algotrendy-web && npm run dev

# Backend API
cd algotrendy-api && uvicorn app.main:app --reload

# Tests
pytest algotrendy/tests/unit --cov
npm test -- --watch
```

### **4. 🧪 Testing Strategy (State-of-the-Art)** ⭐ NEW
**Python Testing** (Unit → Integration → E2E):
- Test organization structure with 3 tiers
- Pytest conventions: descriptive names, single assertions, fixtures, parametrization
- Running tests: coverage, markers, watch mode, specific test selection
- CI/CD pipeline with GitHub Actions (matrix Python versions, Node.js, type-check)

**Frontend Testing** (Component-driven):
- Test structure: unit, integration, e2e patterns
- Vitest/Jest patterns: store mutations, React Query mocking, component snapshots
- Running tests: watch mode, coverage, specific files, snapshot updates
- Best practices: isolation, predictability, edge cases

**Quality Metrics**:
```bash
# Backend coverage: pytest --cov=algotrendy --cov-report=html
# Frontend coverage: npm test -- --coverage
# Type checking: npm run type-check (strict TypeScript)
```

### **5. 🎨 Frontend Architecture Patterns** ⭐ NEW
**Component Organization**:
- Presentational vs. Container pattern for reusability
- Props-driven design (no internal state in presentational components)

**TypeScript Patterns**:
- Centralized type definitions in `src/types/index.ts`
- Discriminated unions for API responses
- Strict null checks (`strict: true`)

**State Management**:
- Separate Zustand stores by domain (auth, trading, ui)
- Selector pattern to avoid unnecessary re-renders
- Clear separation: UI state vs. server state

**API Integration**:
- Custom hooks for data fetching (`usePositions`, `useStrategy`)
- Mutations with error handling and cache invalidation
- Stale time + garbage collection for optimal performance

**Styling Convention**:
- Semantic Tailwind classnames
- Color system: primary, success, error, neutral, warning

### **6. 🔧 Extensibility & Patterns** ⭐ NEW
**Adding New Brokers** (4-step template):
```python
# Step 1: Create adapter + implement BrokerInterface methods
# Step 2: Register in BrokerFactory.BROKER_CLASSES
# Step 3: Create config variant (broker_config.json)
# Step 4: Test with: python -m algotrendy.unified_trader config.json
```

**Adding New Strategies**:
```python
# Step 1: Extend BaseStrategy + implement analyze()
# Step 2: Register in StrategyResolver.STRATEGIES
# Step 3: Add strategy_params to config
# Step 4: Test signal generation
```

**Adding Indicators**:
- Extend `BaseIndicator` + implement `calculate()`
- Register in `IndicatorEngine.INDICATORS`
- Compose in strategies for signal generation

**Risk Management Rules**:
- `SignalProcessor` for filtering signals
- Daily loss limits, position size limits, correlation risk
- Easy to extend with custom rules

### **7. 🚀 Deployment & DevOps** ⭐ NEW
**Environment Setup**:
```bash
# Python: 3.8+ with venv
python -m venv venv && source venv/bin/activate
pip install -r requirements.txt

# Node.js: 18+ with npm 9+
node --version && npm install

# .env files: DATABASE_URL, JWT_SECRET, CORS_ORIGINS, API_KEY, etc.
```

**Docker Configuration**:
- Backend Dockerfile: Python 3.11 slim with uvicorn
- Frontend Dockerfile: Node 18 alpine multi-stage build
- docker-compose.yml: postgres + redis + backend + frontend
- Health checks, volume persistence, environment injection

**Database Migrations** (SQLAlchemy + Alembic):
```bash
# Generate: alembic revision --autogenerate -m "Create positions table"
# Apply: alembic upgrade head
# Rollback: alembic downgrade -1
```

**Deployment Checklist**:
- ✅ All tests passing with coverage
- ✅ Frontend builds without errors
- ✅ Type checking clean
- ✅ No hardcoded credentials
- ✅ Database migrations tested
- ✅ Environment variables documented

**Production Hardening**:
- AWS Secrets Manager for credentials
- HTTPS/TLS via nginx reverse proxy
- Monitoring with Prometheus/Grafana
- Alerting for crashes
- Regular backups (pg_dump)

---

## 🎯 Key Features for AI Agents

### **1. Configuration-Over-Code Philosophy**
This is THE core principle. AI agents should:
- Never create duplicate trader files
- Always use config variants for broker/strategy/mode combinations
- Treat configuration as the source of truth

### **2. Security-First Credentials**
- **ALWAYS** load from environment variables or secure vault
- **NEVER** hardcode API keys in code or config files
- Use `secure_credentials.py` for credential management with audit logs

### **3. Strict TypeScript + Error Handling**
- `strict: true` in tsconfig.json enforces type safety
- All functions return proper types (no `any` exceptions)
- Error boundaries in frontend, try-catch in backend

### **4. Test-Driven Validation**
- Unit tests for isolated logic (<100ms)
- Integration tests for component interaction (1-10s)
- E2E tests for full workflows (10-60s)
- CI/CD pipeline validates all PRs

### **5. No Code Duplication**
- 30+ duplicate trader files consolidated into 1 unified template
- Variants (broker, strategy, mode, asset_class) control behavior
- Each component has single responsibility

---

## 📚 Quick Navigation

| Section | Lines | Purpose |
|---------|-------|---------|
| Architecture Overview | 1-70 | Understand the system layers |
| Critical Patterns | 71-300 | Learn core design patterns |
| Developer Workflows | 301-400 | Get running in minutes |
| **Testing Strategy** | 401-650 | *NEW* State-of-the-art test practices |
| **Frontend Patterns** | 651-900 | *NEW* Component & state patterns |
| **Extensibility** | 901-1100 | *NEW* How to add brokers/strategies |
| **Deployment** | 1101-1353 | *NEW* Docker, migrations, production |
| Common Pitfalls | 1354+ | What NOT to do |

---

## 🚀 Using This Guide

### **For New Contributors**
1. Read: **Architecture Overview** (understand the big picture)
2. Read: **Critical Patterns** (learn the conventions)
3. Run: **Developer Workflows** (get hands-on)
4. Reference: **Common Pitfalls** (avoid mistakes)

### **For Feature Development**
- **Adding a new broker?** → See "Extensibility & Patterns" → "Adding New Brokers"
- **Adding a new strategy?** → See "Extensibility & Patterns" → "Adding New Strategies"
- **Frontend changes?** → See "Frontend Architecture Patterns"
- **Backend changes?** → See "Deployment & DevOps" → Database Migrations

### **For Testing Changes**
- **Python tests?** → See "Testing Strategy" → "Python Testing Architecture"
- **Frontend tests?** → See "Testing Strategy" → "Frontend Testing"
- **CI/CD setup?** → See "Testing Strategy" → "CI/CD Pipeline"

### **For Deployment**
- **Local development?** → See "Deployment & DevOps" → "Environment Setup"
- **Docker setup?** → See "Deployment & DevOps" → "Docker Configuration"
- **Production release?** → See "Deployment & DevOps" → "Deployment Checklist"

---

## 💡 What This Enables

✅ **AI Agents Can Now**:
- Understand the variant system without reading 30+ trader files
- Add new brokers/strategies following clear patterns
- Write tests following state-of-the-art conventions
- Deploy using Docker + proper migrations
- Type-check all frontend code strictly
- Manage credentials securely with audit logs
- Debug using provided test structure
- Make architectural decisions aligned with the project

✅ **This Prevents**:
- Creating duplicate code (pattern: use variants)
- Hardcoding credentials (pattern: secure vault + env vars)
- Weak typing in TypeScript (pattern: strict mode)
- Untested deployments (pattern: comprehensive test matrix)
- Security vulnerabilities (pattern: code review checklist)

---

## 🔍 Documentation Structure

```
.github/
└── copilot-instructions.md
    ├── 🏗️ Architecture Overview (What the system is)
    ├── 🔑 Critical Patterns (How to solve common problems)
    ├── 🛠️ Developer Workflows (How to get running)
    ├── 🧪 Testing Strategy (How to validate changes)
    ├── 🎨 Frontend Architecture (How to build UI)
    ├── 🔧 Extensibility (How to extend the system)
    ├── 🚀 Deployment (How to deploy)
    ├── 📂 Key Files Reference (What file is for what)
    ├── ⚠️ Common Pitfalls (What NOT to do)
    └── 📐 Architecture Decision Log (Why we chose this way)
```

---

## 📊 Content Coverage Summary

| Area | Coverage | Examples |
|------|----------|----------|
| **Architecture** | ⭐⭐⭐⭐⭐ | 4 layers, 8 components, 480+ configs |
| **Patterns** | ⭐⭐⭐⭐⭐ | 6 core patterns with code examples |
| **Testing** | ⭐⭐⭐⭐⭐ | Unit/Integration/E2E, pytest, Jest, CI/CD |
| **Frontend** | ⭐⭐⭐⭐⭐ | Components, stores, hooks, typing, styling |
| **Extensibility** | ⭐⭐⭐⭐⭐ | Brokers, strategies, indicators, risk rules |
| **Deployment** | ⭐⭐⭐⭐⭐ | Docker, migrations, environment, hardening |

---

## 🎓 Next Steps

1. **Review**: Read `.github/copilot-instructions.md` end-to-end
2. **Reference**: Bookmark key sections for quick lookup
3. **Extend**: Add project-specific sections as needed
4. **Share**: Reference this guide in code reviews
5. **Update**: Keep in sync with architecture changes

---

## ✨ What Makes This Different

| Traditional | This Project |
|-------------|--------------|
| "Read all 30+ trader files to understand" | "Read one config schema" |
| "Find example broker implementation" | "See broker adapter pattern with full code" |
| "How do tests work?" | "See unit/integration/e2e structure + CI/CD" |
| "How do I add a feature?" | "See extensibility patterns with 4-step guides" |
| "What's the tech stack?" | "See every layer with examples" |
| "How do we deploy?" | "See Docker, migrations, hardening checklist" |

---

**For AI Agents**: This guide enables you to be immediately productive in the AlgoTrendy codebase without context switching or file-grepping.

**For Humans**: This guide serves as both onboarding documentation AND a reference for best practices.

---

*Last Updated: October 16, 2025*  
*Covers: AlgoTrendy v2.5 (Testing, Frontend, Extensibility, Deployment)*
