# Phase 7B: Backtesting Engine - Final Build Plan

**Created:** October 19, 2025, 6:45 PM
**Status:** 🚀 Ready to Execute
**Estimated Time:** 16-18 hours remaining
**Target Completion:** October 21-22, 2025

---

## 📋 Remaining Work Breakdown

### ✅ Already Complete (~70%)
- [x] All 8 technical indicators
- [x] PerformanceCalculator with 14 metrics
- [x] IndicatorCalculator service
- [x] CustomBacktestEngine refactored
- [x] Models extended (BacktestMetrics, EquityPoint)
- [x] API endpoints (6/6)
- [x] Clean build verified

---

## 🔨 BUILD SEQUENCE (No Overlaps)

### **PHASE 1: Data Persistence Layer** (2-3 hours)

#### Task 1.1: Create IBacktestRepository Interface (20 min)
**File:** `/backend/AlgoTrendy.Core/Interfaces/IBacktestRepository.cs`
- Define interface with CRUD operations
- Methods: SaveAsync, GetByIdAsync, GetRecentAsync, DeleteAsync
- Support for filtering and pagination

#### Task 1.2: Create Database Migration Script (30 min)
**File:** `/database/migrations/002_create_backtests_table.sql`
- Create backtests table schema
- Columns: backtest_id, symbol, config_json, results_json, metrics, timestamps
- Add indexes for performance (symbol, created_at)
- Add unique constraints
- Include rollback script

#### Task 1.3: Implement BacktestRepository (1.5 hours)
**File:** `/backend/AlgoTrendy.Infrastructure/Repositories/BacktestRepository.cs`
- Implement IBacktestRepository
- Use Dapper for PostgreSQL access
- JSON serialization for complex objects
- Error handling and logging
- Connection pooling

#### Task 1.4: Test Repository with Sample Data (30 min)
- Create sample backtest result
- Test SaveAsync
- Test GetByIdAsync
- Test GetRecentAsync
- Verify JSON serialization/deserialization

---

### **PHASE 2: Service Layer Integration** (1-2 hours)

#### Task 2.1: Review BacktestService (30 min)
**File:** `/backend/AlgoTrendy.Backtesting/Services/BacktestService.cs`
- Verify it uses PerformanceCalculator
- Check repository integration
- Validate error handling
- Update if needed

#### Task 2.2: Implement IBacktestService Interface (30 min)
**File:** `/backend/AlgoTrendy.Backtesting/Services/IBacktestService.cs`
- Define service contract
- Methods: RunBacktestAsync, GetResultsAsync, GetHistoryAsync, DeleteAsync
- Async/await throughout

#### Task 2.3: Register Services in DI Container (30 min)
**File:** `/backend/AlgoTrendy.API/Program.cs`
- Register IBacktestEngine → CustomBacktestEngine
- Register IBacktestService → BacktestService
- Register IBacktestRepository → BacktestRepository
- Register IndicatorCalculator (singleton)
- Configure database connection

---

### **PHASE 3: Unit Testing** (6-7 hours)

#### Task 3.1: PerformanceCalculator Tests (2 hours)
**File:** `/backend/AlgoTrendy.Tests/Unit/Backtesting/PerformanceCalculatorTests.cs`
- Test all 14 metrics calculations
- Test edge cases (no trades, all wins, all losses)
- Test Sharpe ratio calculation
- Test Sortino ratio calculation
- Test max drawdown calculation
- Target: 15+ tests

#### Task 3.2: IndicatorCalculator Tests (1.5 hours)
**File:** `/backend/AlgoTrendy.Tests/Unit/Backtesting/IndicatorCalculatorTests.cs`
- Test SMA calculation (multiple periods)
- Test EMA calculation
- Test RSI, MACD, Bollinger, ATR, Stochastic
- Test parameter parsing
- Test configuration-based selection
- Target: 12+ tests

#### Task 3.3: CustomBacktestEngine Tests (2 hours)
**File:** `/backend/AlgoTrendy.Tests/Unit/Backtesting/CustomBacktestEngineTests.cs`
- Test SMA crossover strategy logic
- Test trade execution (entry/exit)
- Test commission/slippage handling
- Test equity curve generation
- Test peak/drawdown tracking
- Mock data generation testing
- Target: 15+ tests

#### Task 3.4: BacktestService Tests (1 hour)
**File:** `/backend/AlgoTrendy.Tests/Unit/Backtesting/BacktestServiceTests.cs`
- Test orchestration flow
- Test error handling
- Test repository integration (mocked)
- Target: 8+ tests

---

### **PHASE 4: Integration Testing** (3-4 hours)

#### Task 4.1: Repository Integration Tests (1.5 hours)
**File:** `/backend/AlgoTrendy.Tests/Integration/Backtesting/BacktestRepositoryTests.cs`
- Test with real PostgreSQL database
- Test SaveAsync with full backtest result
- Test GetByIdAsync retrieval
- Test GetRecentAsync with pagination
- Test DeleteAsync
- Test concurrent operations
- Target: 8+ tests

#### Task 4.2: API Integration Tests (1.5 hours)
**File:** `/backend/AlgoTrendy.Tests/Integration/Backtesting/BacktestApiTests.cs`
- Test POST /api/v1/backtesting/run
- Test GET /api/v1/backtesting/results/{id}
- Test GET /api/v1/backtesting/history
- Test GET /api/v1/backtesting/config
- Test DELETE /api/v1/backtesting/{id}
- Test error responses (400, 404, 500)
- Target: 10+ tests

#### Task 4.3: End-to-End Workflow Tests (1 hour)
**File:** `/backend/AlgoTrendy.Tests/E2E/BacktestE2ETests.cs`
- Test complete workflow: API → Engine → Repository → API
- Test with real indicators and metrics
- Performance benchmark (<5s for 1-year backtest)
- Stress test (5 concurrent backtests)
- Target: 4+ tests

---

### **PHASE 5: Documentation** (2-3 hours)

#### Task 5.1: API Documentation (45 min)
**File:** `/backend/AlgoTrendy.Backtesting/README.md`
- Quick start guide
- API endpoint reference
- Configuration examples
- Sample requests/responses

#### Task 5.2: Developer Guide (1 hour)
**File:** `/docs/BACKTESTING_DEVELOPER_GUIDE.md`
- Architecture overview
- How to add new indicators
- How to add new strategies
- Testing guidelines
- Performance tips

#### Task 5.3: Usage Examples (45 min)
**File:** `/docs/BACKTESTING_EXAMPLES.md`
- 5 complete examples:
  1. Simple SMA crossover
  2. Multi-indicator strategy
  3. Parameter optimization
  4. Walk-forward analysis
  5. Portfolio backtesting

#### Task 5.4: Migration Notes (30 min)
**File:** `/docs/BACKTESTING_MIGRATION.md`
- Python → C# conversion guide
- API differences
- Performance comparisons

---

### **PHASE 6: Final Integration & Deployment** (1-2 hours)

#### Task 6.1: Build Entire Solution (15 min)
- Run `dotnet build` on entire backend
- Fix any compilation errors
- Verify 0 warnings

#### Task 6.2: Run All Tests (30 min)
- Execute full test suite (93+ tests)
- Verify >85% code coverage
- Fix any failing tests
- Generate coverage report

#### Task 6.3: Database Setup (30 min)
- Run migration scripts
- Seed test data
- Verify schema

#### Task 6.4: End-to-End Validation (30 min)
- Start API server
- Run sample backtest via Postman
- Verify results in database
- Check logs for errors
- Performance validation

---

## 📊 Task Checklist (Sequential, No Duplicates)

### PHASE 1: Data Persistence (2-3h)
- [ ] 1.1: Create IBacktestRepository interface
- [ ] 1.2: Create database migration script
- [ ] 1.3: Implement BacktestRepository
- [ ] 1.4: Test repository with sample data

### PHASE 2: Service Integration (1-2h)
- [ ] 2.1: Review BacktestService
- [ ] 2.2: Implement IBacktestService interface
- [ ] 2.3: Register services in DI container

### PHASE 3: Unit Testing (6-7h)
- [ ] 3.1: Write PerformanceCalculator tests (15 tests)
- [ ] 3.2: Write IndicatorCalculator tests (12 tests)
- [ ] 3.3: Write CustomBacktestEngine tests (15 tests)
- [ ] 3.4: Write BacktestService tests (8 tests)

### PHASE 4: Integration Testing (3-4h)
- [ ] 4.1: Write Repository integration tests (8 tests)
- [ ] 4.2: Write API integration tests (10 tests)
- [ ] 4.3: Write E2E workflow tests (4 tests)

### PHASE 5: Documentation (2-3h)
- [ ] 5.1: Write API documentation
- [ ] 5.2: Write Developer guide
- [ ] 5.3: Write usage examples
- [ ] 5.4: Write migration notes

### PHASE 6: Final Integration (1-2h)
- [ ] 6.1: Build entire solution
- [ ] 6.2: Run all tests
- [ ] 6.3: Database setup
- [ ] 6.4: End-to-end validation

---

## ⏱️ Time Estimates

| Phase | Tasks | Estimated | Priority |
|-------|-------|-----------|----------|
| Phase 1: Data Persistence | 4 | 2-3h | 🔴 Critical |
| Phase 2: Service Integration | 3 | 1-2h | 🔴 Critical |
| Phase 3: Unit Testing | 4 | 6-7h | 🟡 High |
| Phase 4: Integration Testing | 3 | 3-4h | 🟡 High |
| Phase 5: Documentation | 4 | 2-3h | 🟢 Medium |
| Phase 6: Final Integration | 4 | 1-2h | 🔴 Critical |
| **TOTAL** | **22** | **16-21h** | - |

---

## 🎯 Execution Strategy

### Option A: Sequential (Recommended)
Execute phases 1→2→3→4→5→6 in order
- **Pros:** Logical flow, dependencies handled naturally
- **Cons:** Can't parallelize
- **Time:** 16-21 hours over 2-3 days

### Option B: Parallel (If Multiple Devs)
- Dev 1: Phase 1 + Phase 2 (3-5h)
- Dev 2: Phase 3.1 + Phase 3.2 (3.5h)
- Dev 3: Phase 3.3 + Phase 3.4 (3h)
- Then: Phase 4 (everyone), Phase 5 (documentation person), Phase 6 (lead)
- **Time:** 10-12 hours over 1.5 days

### Option C: MVP Focus (Fastest)
Execute only critical tasks:
- Phase 1: All tasks (2-3h)
- Phase 2: All tasks (1-2h)
- Phase 3: Tasks 3.1 + 3.3 only (4h)
- Phase 4: Task 4.3 only (1h)
- Phase 5: Task 5.1 only (45min)
- Phase 6: All tasks (1-2h)
- **Time:** 10-13 hours over 1.5 days

---

## 🚦 Dependency Graph

```
Phase 1 (Repository)
    ↓
Phase 2 (Service + DI)
    ↓
Phase 3 (Unit Tests) ← Can start earlier if mocking
    ↓
Phase 4 (Integration Tests) ← Needs Phase 1 & 2 complete
    ↓
Phase 5 (Documentation) ← Can be parallel
    ↓
Phase 6 (Final Integration) ← Needs everything
```

---

## ✅ Definition of Done

### Per Phase
- [ ] All tasks in phase completed
- [ ] Code builds without errors
- [ ] Tests passing (if applicable)
- [ ] Code reviewed
- [ ] Documentation updated

### Overall (Phase 7B Complete)
- [ ] All 22 tasks completed
- [ ] 93+ tests passing
- [ ] >85% code coverage
- [ ] Clean build (0 warnings)
- [ ] API functional end-to-end
- [ ] Database migrations applied
- [ ] Documentation complete
- [ ] Performance benchmarks met (<5s backtest)

---

## 🎊 Success Criteria

1. ✅ Can run backtest via API
2. ✅ Results persisted to database
3. ✅ All 14 metrics calculated correctly
4. ✅ All 8 indicators working
5. ✅ Tests provide confidence
6. ✅ Documentation enables new developers
7. ✅ Performance meets targets

---

## 📝 Next Action

**START HERE:** Phase 1, Task 1.1
→ Create IBacktestRepository interface

**Estimated Time:** 20 minutes
**File:** `/backend/AlgoTrendy.Core/Interfaces/IBacktestRepository.cs`

---

**Build Plan Created:** October 19, 2025
**Estimated Completion:** October 21-22, 2025
**Total Remaining:** 16-21 hours (22 tasks)
**Current Progress:** 70% complete

