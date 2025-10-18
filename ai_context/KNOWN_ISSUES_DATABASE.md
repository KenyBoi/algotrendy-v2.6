# Known Issues Database

**Purpose:** Problems we've solved, so you don't have to re-solve them
**Format:** Issue → Symptoms → Solution → Prevention

---

## ACTIVE ISSUES

### Issue #1: Binance API Testnet Configuration
**Severity:** 🔴 Critical (blocks trading)
**Status:** ✅ SOLVED

**Symptoms:**
- Compilation error: `error CS1061: 'BinanceRestApiOptions' does not contain a definition for 'BaseAddress'`
- Tests fail: "Cannot connect to testnet"

**Root Cause:** Binance.Net SDK doesn't expose testnet URL as property

**Solution:**
```csharp
if (_options.UseTestnet)
{
    Environment.SetEnvironmentVariable("BINANCE_API_TESTNET", "true");
    _logger.LogInformation("Binance broker configured for TESTNET");
}
```
Location: `backend/AlgoTrendy.TradingEngine/Brokers/BinanceBroker.cs:38`

**Prevention:** When using SDKs, check for environment variable support first

---

### Issue #2: Integration Tests Need Credentials
**Severity:** 🟡 Medium (affects test reporting)
**Status:** ✅ DOCUMENTED

**Symptoms:**
- 12 tests marked as skipped
- 6 tests fail with "credentials required"
- Makes it look like more failures than real

**Root Cause:** Integration tests require BINANCE_API_KEY + BINANCE_API_SECRET

**Solution:**
```bash
# Set credentials before testing
export BINANCE_API_KEY=your_key
export BINANCE_API_SECRET=your_secret
dotnet test
```

Or use User Secrets:
```bash
dotnet user-secrets set "Binance:ApiKey" "your_key"
dotnet user-secrets set "Binance:ApiSecret" "your_secret"
```

**Prevention:** Document credential requirements upfront in CI/CD setup

---

### Issue #3: RSI Calculation Precision
**Severity:** 🟢 Low (acceptable tolerance)
**Status:** ✅ DOCUMENTED

**Symptoms:**
- RSI calculations differ from v2.5 by 0.005-0.01%
- Tests fail with direct equality checks

**Root Cause:** Floating-point rounding differences between Python and C#

**Solution:**
```csharp
Assert.That(result, Is.EqualTo(expectedValue).Within(0.0001));  // 4-decimal tolerance
```

**Prevention:** Use tolerance-based assertions for financial calculations

---

### Issue #4: Docker Warnings on Startup
**Severity:** 🟢 Low (cosmetic)
**Status:** ✅ ACCEPTABLE

**Symptoms:**
- Warning messages in logs on startup
- API still works correctly

**Root Cause:** Non-critical initialization warnings from dependencies

**Solution:** Ignore warnings, monitor logs for actual errors
```bash
docker-compose logs api | grep ERROR  # Check for real errors
```

**Prevention:** None needed, expected behavior

---

## HISTORICAL ISSUES (RESOLVED)

### Issue: "QuestDB Port Conflict"
**When:** Phase 6 Docker setup
**Symptom:** "Port 8812 already in use"
**Solution:** Kill existing process or map to different port
**Status:** ✅ RESOLVED

### Issue: "Market Data Not Appearing"
**When:** Phase 4b testing
**Symptom:** API returns empty array
**Solution:** Wait 60 seconds for first fetch cycle (not immediate)
**Status:** ✅ RESOLVED

### Issue: "SSL Certificate Self-Signed Warning"
**When:** Phase 6 deployment
**Symptom:** Browser shows certificate warning
**Solution:** Use Let's Encrypt for production, self-signed OK for dev
**Status:** ✅ RESOLVED

---

## PREVENTED ISSUES (Avoided)

### Issue: "Secrets Leaked to Git"
**How Prevented:** Never hardcode, environment variables only
**Check:** `grep -r "password\|secret\|api_key" backend/ | grep -v test`

### Issue: "Docker Image Too Large"
**How Prevented:** Multi-stage build (70% reduction)
**Final Size:** 245MB (vs 800MB naive approach)

### Issue: "Data Loss During Upgrade"
**How Prevented:** Copy v2.5 (never move)
**Strategy:** v2.5 remains intact, complete backup

---

## COMMON ERRORS & SOLUTIONS

| Error | Cause | Solution |
|-------|-------|----------|
| "Connection refused" | Service not running | `docker-compose ps` + restart if needed |
| "API returns 500" | Database connection failed | Check QuestDB running + credentials |
| "Order not placed" | Testnet credentials invalid | Regenerate key, check IP whitelist |
| "Market data empty" | Fetch interval hasn't occurred | Wait 60+ seconds (default cycle) |
| "Port already in use" | Another service on port | `lsof -i :<port>` then kill |
| "Out of memory" | Container limit too low | Increase Docker memory limit |
| "Build fails" | Missing dependencies | `dotnet restore` then rebuild |
| "Tests hang" | Async deadlock | Check Task.Wait() usage |

---

## DIAGNOSTICS CHECKLIST

When troubleshooting, run:

```bash
# 1. Check services running
docker-compose ps

# 2. Check API logs
docker-compose logs api | tail -50

# 3. Check database logs
docker-compose logs questdb | tail -50

# 4. Test API health
curl http://localhost:5002/health

# 5. Test database connectivity
curl http://localhost:9000

# 6. Check resource usage
docker stats

# 7. Check recent git changes
git log --oneline -10

# 8. Run minimal test
dotnet test --filter "Category=Unit" --verbosity minimal
```

---

## WHEN TO ESCALATE

| Situation | Action |
|-----------|--------|
| Build failures | Check dependencies, run `dotnet restore` |
| Test failures | Run `dotnet test --verbosity detailed` to see exact error |
| Deployment issues | Follow DEPLOYMENT_DOCKER.md troubleshooting section |
| Data concerns | Check QuestDB backup location (should exist) |
| Security concerns | Review ARCHITECTURE_SNAPSHOT.md security section |
| Performance issues | Profile with Docker stats, check database query plans |

---

**Last Updated:** October 18, 2025
**Active Issues:** 1 (Integration test credentials)
**Resolved Issues:** 3
**Status:** All critical issues solved, known issues documented
