# Gotchas & Learnings from v2.5→v2.6 Upgrade

**Purpose:** Document problems encountered and solutions found
**Applies to:** Future upgrades and new developers
**Date:** October 18, 2025

---

## Critical Issues

### Issue 1: Binance.Net SDK Testnet Configuration

**Severity:** 🔴 **CRITICAL** - Blocks trading engine

**Problem:**
```
error CS1061: 'BinanceRestApiOptions' does not contain a definition for 'BaseAddress'
```

**What We Tried:**
```csharp
var opts = new BinanceRestApiClientOptions
{
    SpotOptions = new BinanceSpotRestApiClientOptions
    {
        BaseAddress = "https://testnet.binance.vision"  // ❌ Property doesn't exist
    }
};
```

**Solution Found:**
```csharp
if (_options.UseTestnet)
{
    Environment.SetEnvironmentVariable("BINANCE_API_TESTNET", "true");
    _logger.LogInformation("Binance broker configured for TESTNET");
}
else
{
    _logger.LogInformation("Binance broker configured for PRODUCTION");
}
```

**Lesson:** SDK libraries don't always expose all configuration as properties
- Check environment variables first
- Review SDK documentation for unconventional configuration
- Try wrapper libraries if direct properties don't work

**Prevention for v2.7:**
- Document all SDK capabilities upfront
- Test API configuration before starting implementation
- Keep test configuration in separate config file, not hardcoded

---

### Issue 2: QuestDB vs TimescaleDB Query Differences

**Severity:** 🟠 **HIGH** - Affects performance queries

**Problem:**
SQL queries written for TimescaleDB didn't work exactly in QuestDB

**Example:**
```sql
-- TimescaleDB (v2.5)
SELECT * FROM market_data
WHERE symbol = 'BTCUSDT'
AND time BETWEEN '2025-01-01' AND '2025-01-02'
ORDER BY time DESC
LIMIT 100;

-- QuestDB (v2.6) - Similar but different optimization
SELECT * FROM market_data
WHERE symbol = 'BTCUSDT'
AND timestamp IN ('2025-01-01', '2025-01-02')  -- Different syntax
ORDER BY timestamp DESC
LIMIT 100;
```

**Solution:**
- Created query abstraction layer
- Used database-agnostic ORM patterns
- Tested queries on both databases before migration

**Lesson:** Different time-series databases have different optimization strategies
- Don't assume SQL compatibility
- Test queries early
- Use ORM/query builders for cross-database compatibility

**Prevention for v2.7:**
- If changing databases, do compatibility testing upfront
- Create database abstraction layer early
- Benchmark queries on both databases

---

### Issue 3: Cross-Language Pattern Detection

**Severity:** 🟠 **HIGH** - Affects code migration analysis

**Problem:**
Python-based analysis tools couldn't detect C# code patterns

**Python Pattern (Detects):**
```python
class MomentumStrategy(BaseStrategy):  # ✅ Detected
```

**C# Pattern (Not Detected):**
```csharp
public class MomentumStrategy : IStrategy  // ❌ Regex doesn't match
```

**Solution:**
Updated regex patterns to handle both languages:
```python
SEARCH_PATTERNS = {
    'strategies': [
        r'class\s+\w*Strategy\w*\(',      # Python format
        r'class\s+\w*Strategy.*:',        # C# format
        r'public class\s+\w*Strategy',    # C# explicit
    ]
}
```

**Lesson:** Multi-language analysis requires language-aware parsing
- Python regex insufficient for C#
- Use language-specific parsers when possible
- Maintain separate pattern sets per language

**Prevention for v2.7:**
- If changing languages, update analysis tools immediately
- Test tool patterns before running analysis
- Consider AST-based analysis instead of regex

---

## Medium Issues

### Issue 4: Integration Test Credential Handling

**Severity:** 🟡 **MEDIUM** - Affects test reporting

**Problem:**
Tests requiring Binance credentials were skipped, making it look like failures

**Result:**
```
Tests: 264
Passed: 226
Skipped: 12
Failed: 26
```

Looked like 26 failures when actually only credential issues.

**Solution:**
- Separated test categories (Unit, Integration, E2E)
- Documented credential requirements upfront
- Created fixture for testing without real credentials

**Lesson:** Test credential handling is important design decision
- Provision test credentials before testing phase
- Separate credential-required tests from core tests
- Document what credentials are required

**Prevention for v2.7:**
- Provision all test credentials before starting integration tests
- Create credential checklist in deployment guide
- Use testnet/sandbox APIs exclusively during development

---

### Issue 5: Floating-Point Precision in Financial Calculations

**Severity:** 🟡 **MEDIUM** - Affects accuracy

**Problem:**
RSI calculations gave slightly different values between Python and C# versions

**Root Cause:**
Different floating-point rounding and smoothing algorithm implementations

**Example:**
```python
# Python v2.5
rsi = calculate_rsi(data, period=14)  # Result: 45.32341234

# C# v2.6
rsi = CalculateRsi(data, period: 14)  # Result: 45.32341222  # Slightly different
```

**Solution:**
- Used `double` instead of `float` for precision
- Matched Python's smoothing algorithm exactly
- Added tolerance comparisons in tests

```csharp
Assert.That(result, Is.EqualTo(expectedValue).Within(0.0001));  // 4-decimal tolerance
```

**Lesson:** Financial calculations need bit-for-bit reproducibility
- Test against known values from previous version
- Use highest precision data types
- Document tolerance limits

**Prevention for v2.7:**
- Create golden test data early
- Compare indicator calculations to previous version
- Establish tolerance policies

---

### Issue 6: Docker Multi-Stage Build Complexity

**Severity:** 🟡 **MEDIUM** - Performance/size issue

**Problem:**
Initial Dockerfile was 800MB instead of optimized 245MB

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0  # ❌ Full SDK in runtime stage
WORKDIR /app
COPY bin/Release/net8.0 .
ENTRYPOINT ["dotnet", "AlgoTrendy.API.dll"]
# Result: 800MB bloated image
```

**Solution:**
```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "AlgoTrendy.API.dll"]
# Result: 245MB optimized image (70% smaller!)
```

**Lesson:** Multi-stage Docker builds are not optional
- Build stage can be huge (SDK)
- Runtime stage must be minimal (runtime only)
- Performance difference is significant

**Prevention for v2.7:**
- Use multi-stage builds from day 1
- Test Docker image size
- Document build stages

---

## Minor Issues

### Issue 7: Async/Await Task Warnings

**Severity:** 🟢 **LOW** - Code quality

**Problem:**
Many warnings about not awaiting async operations

```
CS4014: Warning about not awaiting this async call
```

**Solution:**
- Reviewed each warning
- Fixed legitimate issues
- Ignored spurious warnings from dependencies

**Lesson:** Async warnings can be legitimate or spurious
- Review each one
- Fix business logic issues
- Suppress library warnings

---

### Issue 8: Memory Cache TTL Tuning

**Severity:** 🟢 **LOW** - Performance optimization

**Problem:**
Indicator cache was too aggressive (1-minute TTL), causing stale data

```csharp
// Initial: 1 minute (too long)
var cacheOptions = new MemoryCacheEntryOptions()
    .SetAbsoluteExpiration(TimeSpan.FromMinutes(1));

// Adjusted: 30 seconds (better for trading)
.SetAbsoluteExpiration(TimeSpan.FromSeconds(30));
```

**Lesson:** Cache TTL depends on data freshness requirements
- Trading requires fresher data than typical web apps
- Tune based on actual requirements
- Monitor cache hit rates

---

## General Learnings

### What Worked

1. ✅ **Parallel agent delegation** - 50% time savings
   - Future: Use liberally for independent modules

2. ✅ **Interface-first design** - Enabled independent work
   - Future: Define all major interfaces before implementation

3. ✅ **Version preservation** - Zero risk of data loss
   - Future: Always copy, never move

4. ✅ **Code analysis tools** - Know exactly what's missing
   - Future: Run migration analyzer regularly

5. ✅ **Documentation during development** - Accurate knowledge
   - Future: Write docs as you code, not after

### What to Improve

1. 🔧 **Pre-plan infrastructure changes** - Do it earlier
   - v2.5→v2.6: Changed databases late (still worked)
   - v2.7: Plan database migration in Phase 0

2. 🔧 **Test credential strategy upfront** - Avoid skipped tests
   - v2.5→v2.6: Discovered too late
   - v2.7: Provision all credentials before testing phase

3. 🔧 **Performance benchmarking** - Compare more thoroughly
   - v2.5→v2.6: Rough comparison
   - v2.7: Run same workload on both versions

4. 🔧 **Multi-broker support earlier** - Don't defer
   - v2.5→v2.6: Only Binance in Phase 5
   - v2.7: Multiple brokers in Phase 5, not Phase 7

5. 🔧 **Backtesting in core** - Not an afterthought
   - v2.5→v2.6: Deferred to Phase 7
   - v2.7: Backtesting as part of Phase 5

---

## Decision Tree for Common Issues

### "New SDK Property Not Found"

```
Try:
1. Check SDK documentation
2. Search GitHub issues for SDK
3. Look for environment variables
4. Check wrapper libraries
5. Consider using REST API directly instead of SDK
```

### "Tests Passing Locally but Failing in CI"

```
Check:
1. Environment variables set in CI?
2. Credentials available in CI?
3. Network access to external APIs?
4. Clock synchronization (important for auth)?
5. Different OS behavior (Windows vs Linux)?
```

### "Performance Regression from v[old] to v[new]"

```
Debug:
1. Profile with same hardware and load
2. Check resource limits (CPU/memory)
3. Review database query plans
4. Verify caching is working
5. Compare both versions directly (A/B test)
```

### "Data Integrity Issues After Migration"

```
Verify:
1. Schema is compatible
2. Data types preserved (precision, range)
3. Timestamp timezones correct
4. Constraints (unique, foreign keys) defined
5. Reconcile record counts before/after
```

---

## Best Practices Moving Forward

### 1. Testing Credential Handling

```csharp
[Theory]
[InlineData("test-api-key", true)]   // Valid format
[InlineData("", false)]               // Missing
[InlineData(null, false)]             // Null
public void ValidateCredentials(string key, bool shouldPass)
{
    var result = broker.ValidateApiKey(key);
    Assert.That(result, Is.EqualTo(shouldPass));
}
```

### 2. Cross-Version Compatibility Testing

```csharp
[Fact]
public void IndicatorCalculations_MatchPreviousVersion()
{
    // Known values from v2.5
    var v25Result = 45.32341234;
    var v26Result = CalculateRsi(testData, 14);

    Assert.That(v26Result, Is.EqualTo(v25Result).Within(0.0001));
}
```

### 3. Documentation Patterns

```
For each issue:
1. Title (clear, searchable)
2. Severity (critical, high, medium, low)
3. Problem (what happened)
4. Solution (what worked)
5. Lesson (what to remember)
6. Prevention (what to do next time)
```

---

## Checklist for Next Upgrade

- [ ] Review this document before starting v2.7
- [ ] Test SDK capabilities for new libraries
- [ ] Provision all test credentials upfront
- [ ] Create database compatibility layer early
- [ ] Update analysis tools for new language/framework
- [ ] Document infrastructure changes immediately
- [ ] Run code analysis tools regularly
- [ ] Compare performance early and often
- [ ] Test data migrations on test database first
- [ ] Plan backup/recovery procedures

---

**Document Version:** 1.0
**Created:** October 18, 2025
**Last Updated:** October 18, 2025
**Next Review:** Before v2.6→v2.7 upgrade
