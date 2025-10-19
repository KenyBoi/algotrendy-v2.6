# Debt Management Module - Build & Integration Plan

## Executive Summary
This document outlines the build plan for integrating the Debt Management Module into AlgoTrendy v2.6. The module is designed as a self-contained, version-independent component to enable easy software upgrades.

---

## Module Overview

**Module Name:** `debt_mgmt_module`
**Version:** 1.0.0
**Source:** Extracted from AlgoTrendy v2.5
**Target:** AlgoTrendy v2.6
**Purpose:** Margin, leverage, and debt tracking for cryptocurrency trading

---

## Directory Structure

```
AlgoTrendy_v2.6/debt_mgmt_module/
├── core/                           # Core module code
│   ├── __init__.py                # Module initialization
│   ├── broker_abstraction.py      # Multi-broker leverage management
│   └── fund_manager.py            # Margin calculation & fund tracking
├── database/                       # Database schemas
│   ├── schema.sql                 # Full schema with positions table
│   └── add_ingestion_config.sql   # Configuration migration
├── tests/                          # Test suite
│   └── test_margin_scenarios.py   # Margin calculation tests
├── config/                         # Configuration files
│   ├── broker_config.json         # Broker settings & risk parameters
│   └── module_config.yaml         # Module-specific settings
├── api/                            # API integration layer
│   ├── endpoints.py               # Portfolio & margin endpoints
│   └── queries.py                 # Database queries
├── docs/                           # Documentation
│   ├── BUILD_PLAN.md              # This file
│   ├── API_REFERENCE.md           # API documentation
│   └── SECURITY_RECOMMENDATIONS.md # Security best practices
└── README.md                       # Module overview
```

---

## Phase 1: Module Preparation ✅ COMPLETED

### 1.1 File Migration
- [✅] Create module directory structure
- [✅] Copy broker_abstraction.py to core/
- [✅] Copy fund_manager.py to core/
- [✅] Copy schema.sql to database/
- [✅] Copy add_ingestion_config.sql to database/
- [✅] Copy test_margin_scenarios.py to tests/
- [✅] Copy broker_config.json to config/

### 1.2 Files Migrated
| Source File | Destination | Status |
|-------------|-------------|--------|
| `/algotrendy/broker_abstraction.py` | `/debt_mgmt_module/core/` | ✅ |
| `/integrations/.../fund_manager.py` | `/debt_mgmt_module/core/` | ✅ |
| `/database/schema.sql` | `/debt_mgmt_module/database/` | ✅ |
| `/database/migrations/add_ingestion_config.sql` | `/debt_mgmt_module/database/` | ✅ |
| `/integrations/.../test_margin_scenarios.py` | `/debt_mgmt_module/tests/` | ✅ |
| `/broker_config.json` | `/debt_mgmt_module/config/` | ✅ |

---

## Phase 2: Module Configuration (IN PROGRESS)

### 2.1 Create Module Initialization Files
- [ ] Create `core/__init__.py` - Export key classes
- [ ] Create `api/__init__.py` - Export API components
- [ ] Create root `__init__.py` - Module entry point

### 2.2 Create Configuration Management
- [ ] Create `config/module_config.yaml` - Module settings
- [ ] Create `config/defaults.yaml` - Default values
- [ ] Add environment variable support

### 2.3 Update Import Paths
- [ ] Update imports in broker_abstraction.py
- [ ] Update imports in fund_manager.py
- [ ] Ensure relative imports work correctly

---

## Phase 3: API Integration Layer

### 3.1 Create API Endpoints
Create `/debt_mgmt_module/api/endpoints.py`:
```python
# Portfolio endpoints
GET /api/debt_mgmt/portfolio
GET /api/debt_mgmt/positions
GET /api/debt_mgmt/margin/status
GET /api/debt_mgmt/leverage/{symbol}

# Management endpoints
POST /api/debt_mgmt/leverage/set
POST /api/debt_mgmt/funds/reset
GET /api/debt_mgmt/funds/history
```

### 3.2 Create Database Query Layer
Create `/debt_mgmt_module/api/queries.py`:
- Extract PositionQueries from v2.5
- Add margin-specific queries
- Add leverage tracking queries

### 3.3 Tasks
- [ ] Create endpoints.py with FastAPI routes
- [ ] Create queries.py with database queries
- [ ] Add request/response models (Pydantic)
- [ ] Add error handling middleware

---

## Phase 4: Database Integration

### 4.1 Schema Integration Options

**Option A: Separate Database Schema** (Recommended)
- Create `debt_mgmt` schema in PostgreSQL
- Full isolation from main application
- Easiest to upgrade/rollback
- Cross-schema queries for reporting

**Option B: Main Database with Prefixes**
- Add tables to main database with `dm_` prefix
- Shared connection pool
- Simpler deployment
- Less isolation

**Option C: Separate Database**
- Completely separate PostgreSQL database
- Maximum isolation
- Requires microservice architecture
- More complex deployment

**Recommendation:** Use Option A (Separate Schema)

### 4.2 Migration Strategy
```sql
-- Create dedicated schema
CREATE SCHEMA IF NOT EXISTS debt_mgmt;

-- Run module schema in dedicated namespace
\c your_database
SET search_path TO debt_mgmt, public;
\i database/schema.sql
\i database/add_ingestion_config.sql
```

### 4.3 Tasks
- [ ] Choose integration option
- [ ] Create schema migration script
- [ ] Update connection strings in module
- [ ] Add database configuration to module_config.yaml
- [ ] Test migrations on dev environment

---

## Phase 5: Dependency Management

### 5.1 Module Dependencies
Create `/debt_mgmt_module/requirements.txt`:
```
# Core dependencies
asyncio>=3.4.3
pybit>=5.6.0           # Bybit API
python-binance>=1.0.19  # Binance API
ccxt>=4.0.0            # Multi-exchange support

# Database
psycopg2-binary>=2.9.9
SQLAlchemy>=2.0.23

# API
fastapi>=0.104.1
pydantic>=2.5.0

# Testing
pytest>=7.4.3
pytest-asyncio>=0.21.1

# Utilities
python-decimal>=1.0
pyyaml>=6.0.1
```

### 5.2 Tasks
- [ ] Create requirements.txt
- [ ] Verify no conflicting dependencies with v2.6
- [ ] Add to main project requirements or keep separate
- [ ] Document installation process

---

## Phase 6: Security Hardening

### 6.1 Critical Security Fixes Required

**Priority 1: Leverage Limits**
```yaml
# config/module_config.yaml
risk_settings:
  default_leverage: 2.0    # CHANGED from 75x
  max_leverage: 5.0        # NEW: Hard limit
  min_position_size: 50
  max_position_size: 750
  liquidation_threshold: 0.8  # NEW: 80% margin used
```

**Priority 2: Credential Security**
- [ ] Move API keys to environment variables
- [ ] Add encryption for stored credentials
- [ ] Implement credential rotation
- [ ] Add secrets management (e.g., HashiCorp Vault)

**Priority 3: Margin Safety**
- [ ] Add pre-trade margin validation
- [ ] Implement automatic liquidation at 80% threshold
- [ ] Add margin call warnings at 70%
- [ ] Implement circuit breakers

**Priority 4: Audit Logging**
- [ ] Log all leverage changes
- [ ] Log all margin threshold breaches
- [ ] Log all position liquidations
- [ ] Add audit trail to database

### 6.2 Tasks
- [ ] Update broker_config.json with safe defaults
- [ ] Add environment variable support
- [ ] Implement liquidation engine
- [ ] Add audit logging system
- [ ] Create security documentation

---

## Phase 7: Testing & Validation

### 7.1 Unit Tests
- [ ] Test margin calculations (existing test_margin_scenarios.py)
- [ ] Test leverage setting across all brokers
- [ ] Test fund manager reset logic
- [ ] Test API endpoints

### 7.2 Integration Tests
- [ ] Test with v2.6 backend
- [ ] Test database schema compatibility
- [ ] Test API integration with frontend
- [ ] Test broker connections

### 7.3 Load Testing
- [ ] Test concurrent position tracking
- [ ] Test margin calculations under load
- [ ] Test database query performance

### 7.4 Security Testing
- [ ] Penetration testing for API endpoints
- [ ] Credential leak detection
- [ ] SQL injection testing
- [ ] Authorization/authentication testing

---

## Phase 8: Documentation

### 8.1 Technical Documentation
- [✅] BUILD_PLAN.md (this file)
- [ ] API_REFERENCE.md - API endpoint documentation
- [ ] SECURITY_RECOMMENDATIONS.md - Security guidelines
- [ ] DATABASE_SCHEMA.md - Schema documentation

### 8.2 Integration Guide
- [ ] How to integrate with v2.6 backend
- [ ] How to configure brokers
- [ ] How to set up database
- [ ] Environment variables reference

### 8.3 User Documentation
- [ ] Margin trading guide
- [ ] Leverage usage guide
- [ ] Risk management best practices
- [ ] Troubleshooting guide

---

## Phase 9: Integration with v2.6

### 9.1 Backend Integration

**Step 1: Import Module**
```python
# In backend/app/main.py
from debt_mgmt_module import DebtMgmtModule
from debt_mgmt_module.api import debt_mgmt_router

# Initialize module
debt_mgmt = DebtMgmtModule(config_path="debt_mgmt_module/config/module_config.yaml")

# Register API routes
app.include_router(
    debt_mgmt_router,
    prefix="/api/debt_mgmt",
    tags=["debt_management"]
)
```

**Step 2: Database Setup**
```bash
# Run migrations
cd AlgoTrendy_v2.6
psql -U postgres -d algotrendy -f debt_mgmt_module/database/schema.sql
psql -U postgres -d algotrendy -f debt_mgmt_module/database/add_ingestion_config.sql
```

**Step 3: Configure Environment**
```bash
# Add to .env
DEBT_MGMT_DB_SCHEMA=debt_mgmt
DEBT_MGMT_DEFAULT_LEVERAGE=2.0
DEBT_MGMT_MAX_LEVERAGE=5.0
DEBT_MGMT_LIQUIDATION_THRESHOLD=0.8

# Broker credentials (encrypted)
BYBIT_API_KEY=${BYBIT_API_KEY}
BYBIT_API_SECRET=${BYBIT_API_SECRET}
```

### 9.2 Frontend Integration
```typescript
// Frontend API client
import { DebtMgmtClient } from '@/api/debt-mgmt';

const client = new DebtMgmtClient();

// Fetch portfolio with margin data
const portfolio = await client.getPortfolio();
// {
//   total_value: 125000.50,
//   buying_power: 50000.00,
//   margin_used: 30000.00,
//   margin_available: 20000.00,
//   ...
// }
```

### 9.3 Tasks
- [ ] Add module to backend imports
- [ ] Register API routes
- [ ] Configure environment variables
- [ ] Run database migrations
- [ ] Update frontend to call new endpoints
- [ ] Test end-to-end flow

---

## Phase 10: Deployment

### 10.1 Deployment Checklist
- [ ] All tests passing
- [ ] Security audit completed
- [ ] Documentation complete
- [ ] Database migrations tested
- [ ] Rollback plan documented
- [ ] Monitoring configured

### 10.2 Deployment Steps
1. Backup current database
2. Deploy database migrations
3. Deploy backend with module
4. Deploy frontend updates
5. Verify all endpoints working
6. Monitor for errors
7. Enable for production traffic

### 10.3 Rollback Plan
1. Revert backend deployment
2. Rollback database migrations
3. Restore frontend
4. Restore from backup if needed

---

## Phase 11: Monitoring & Maintenance

### 11.1 Monitoring Metrics
- Margin utilization per user
- Leverage distribution
- Liquidation events
- API endpoint response times
- Database query performance
- Error rates

### 11.2 Alerts
- Margin utilization > 70% (warning)
- Margin utilization > 80% (critical)
- Liquidation events
- API errors > 5%
- Database connection failures

### 11.3 Maintenance Tasks
- Weekly: Review liquidation events
- Monthly: Security audit
- Quarterly: Performance optimization
- Yearly: Full code review

---

## Success Criteria

✅ Module is fully self-contained
✅ Zero dependencies on v2.5 code
✅ All security vulnerabilities fixed
✅ 100% test coverage for core functions
✅ API documentation complete
✅ Successfully integrated with v2.6
✅ Passes security audit
✅ Production-ready monitoring

---

## Timeline Estimate

| Phase | Duration | Status |
|-------|----------|--------|
| Phase 1: Preparation | 1 day | ✅ DONE |
| Phase 2: Configuration | 0.5 days | 🔄 IN PROGRESS |
| Phase 3: API Layer | 1 day | ⏳ PENDING |
| Phase 4: Database | 1 day | ⏳ PENDING |
| Phase 5: Dependencies | 0.5 days | ⏳ PENDING |
| Phase 6: Security | 2 days | ⏳ PENDING |
| Phase 7: Testing | 2 days | ⏳ PENDING |
| Phase 8: Documentation | 1 day | 🔄 IN PROGRESS |
| Phase 9: Integration | 1 day | ⏳ PENDING |
| Phase 10: Deployment | 1 day | ⏳ PENDING |
| **Total** | **11 days** | **9% Complete** |

---

## Risk Assessment

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Breaking v2.6 compatibility | High | Low | Thorough testing, rollback plan |
| Security vulnerabilities | Critical | Medium | Security audit, hardening |
| Performance degradation | Medium | Low | Load testing, optimization |
| Data loss during migration | Critical | Very Low | Backup strategy, staging tests |
| Broker API changes | Medium | Medium | Abstraction layer, versioning |

---

## Next Steps (Immediate)

1. ✅ Complete Phase 1 (File Migration)
2. 🔄 Complete Phase 2 (Configuration)
3. ⏳ Create API integration files
4. ⏳ Fix security vulnerabilities
5. ⏳ Write comprehensive tests

---

## Notes

- Module designed for **zero breaking changes** to v2.6
- All configuration is **externalized** for easy updates
- **Security-first** approach with safe defaults
- **Fully tested** before production deployment
- **Monitoring** built-in from day one

---

**Document Version:** 1.0
**Last Updated:** 2025-10-18
**Author:** AlgoTrendy Engineering Team
**Status:** IN PROGRESS - Phase 2
