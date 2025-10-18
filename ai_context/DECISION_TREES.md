# Decision Trees for AlgoTrendy

**Purpose:** Help any AI quickly make the right decisions
**Format:** If/Then decision trees for common scenarios

---

## Decision 1: "Should we deploy to production NOW?"

```
Are all tests passing (226+)?
├─ YES → Next question
└─ NO → Fix tests first, then ask again

Is Docker image built and tested locally?
├─ YES → Next question
└─ NO → Build and test: docker build -f backend/Dockerfile -t algotrendy:v2.6

Is DEPLOYMENT_CHECKLIST completed?
├─ YES → Next question
└─ NO → Complete checklist (100+ items)

Do you have Binance API credentials?
├─ YES (testnet) → Staging environment first (recommended)
├─ YES (production) → Staging environment FIRST, then production
└─ NO → Cannot proceed yet, get credentials

Have you tested in staging for 24 hours?
├─ YES → ✅ DEPLOY TO PRODUCTION (proceed with docker-compose.prod.yml)
└─ NO → Set up staging environment, test 24 hours, then deploy
```

---

## Decision 2: "Should I add a new feature?"

```
Is v2.6 MVP stable (226/264 tests passing)?
├─ YES → Consider Phase 7 features
└─ NO → Fix tests first

What type of feature?

New Strategy:
├─ Read: ARCHITECTURE_SNAPSHOT.md (IStrategy pattern)
├─ Create: New file in backend/AlgoTrendy.Strategies/
├─ Estimate: 2-4 hours (including tests)
└─ Priority: Medium (core strategies working)

New Broker Integration:
├─ Is data channel already available?
│  ├─ YES → Just implement IBroker (trading wrapper)
│  └─ NO → First implement IDataChannel
├─ Estimate: 2-3 hours per broker
└─ Priority: Medium (Binance done, others deferred)

New Indicator:
├─ Add to IndicatorService
├─ Estimate: 1-2 hours
└─ Priority: Medium (5 indicators available)

Backtesting Engine:
├─ Estimate: 20-30 hours (significant work)
├─ Priority: HIGH (important for safety)
└─ Status: Not started

Dashboard UI:
├─ Estimate: 30+ hours
├─ Priority: LOW (API exists, UI nice-to-have)
└─ Status: Not started
```

---

## Decision 3: "We found a bug. What do we do?"

```
Is it a core logic bug (affects orders/positions)?
├─ YES → CRITICAL: Fix immediately, don't deploy until fixed
└─ NO → Continue

Is it a test bug (test framework issue)?
├─ YES → Fix test setup, doesn't affect production
└─ NO → Continue

Is it a performance regression?
├─ YES → Investigate and profile before deploying
└─ NO → Continue

Is it already in v2.5?
├─ YES → Known issue, document it (lower priority)
└─ NO → New regression, prioritize fix

Should we hotfix production?
├─ CRITICAL + PRODUCTION DOWN → YES, hotfix
├─ CRITICAL + DATA LOSS RISK → YES, hotfix
├─ MEDIUM + WORKAROUND EXISTS → Deploy in next release
└─ LOW + NON-BLOCKING → Include in next version
```

---

## Decision 4: "How many tests should pass?"

```
Target: 85%+ (226+/264)

Are integration tests skipped?
├─ YES, because BINANCE_API_KEY missing → This is OK, normal
└─ YES, for other reasons → Fix test setup

Are core module tests passing (Order, Position, Strategy)?
├─ YES (95%+) → Good, can deploy
└─ NO (<90%) → Fix before deploying

Are all build errors gone?
├─ YES (0 errors) → Good
└─ NO → Fix all compilation errors before testing

Are warnings mostly from dependencies?
├─ YES → OK, ignore
└─ NO → Fix async/await warnings in our code

Result: 226/264 (85.6%) = ✅ PRODUCTION READY
```

---

## Decision 5: "Should we change the database?"

```
Are there performance problems?
├─ YES (>50ms query time) → Investigate QuestDB tuning first
└─ NO → Don't change, current is fine

Is QuestDB the bottleneck?
├─ YES (confirmed via profiling) → Consider alternatives
└─ NO (API/logic is slow) → Optimize code instead

Do we need full SQL compliance?
├─ YES → Keep QuestDB (PostgreSQL protocol)
└─ NO → Consider specialized databases

Recommendation: ✅ KEEP QuestDB (working well, no reason to change)
```

---

## Decision 6: "Binance API key fails. What happened?"

```
Error type?

"Invalid API key":
├─ Check: Key format is 32+ alphanumeric characters?
├─ Check: Copy/paste errors?
├─ Check: API key has trading permissions enabled?
└─ Fix: Regenerate key from Binance account

"API key signature invalid":
├─ Check: Secret key is correct?
├─ Check: No extra spaces in key?
├─ Check: System clock synchronized?
└─ Fix: Regenerate both key + secret

"IP not allowed":
├─ Check: Binance IP whitelist includes your VPS IP?
├─ Fix: Add VPS IP to whitelist in Binance API management
└─ Workaround: Use BINANCE_USE_TESTNET=true (no IP restriction)

"Testnet endpoint failing":
├─ Check: BINANCE_USE_TESTNET=true set?
├─ Check: Connection to testnet.binance.vision?
└─ Fix: Manually test: curl https://testnet.binance.vision/fapi/v1/ping

"Too many requests (rate limit)":
├─ Check: Rate limiting configured correctly?
├─ Check: Not making 1200+ requests/minute?
└─ Fix: Space out requests, implement backoff
```

---

## Decision 7: "QuestDB won't start. What happened?"

```
Error message type?

"Port 8812 already in use":
├─ Check: Other QuestDB instance running?
├─ Check: Another service on port 8812?
└─ Fix: Kill process: lsof -i :8812, kill <PID>

"Permission denied on data directory":
├─ Check: Docker container user permissions?
├─ Check: Volume mount permissions?
└─ Fix: chmod 755 on volume directory

"Out of memory":
├─ Check: Docker memory limit set?
├─ Check: QuestDB too much data?
└─ Fix: Increase Docker memory limit or prune old data

"Connection refused":
├─ Check: QuestDB fully started (wait 10 seconds)?
├─ Check: Container logs: docker-compose logs questdb
└─ Fix: Restart container: docker-compose restart questdb
```

---

## Decision 8: "Should we scale horizontally?"

```
Current load: 100 req/sec?
├─ YES → Horizontal scaling worth considering
└─ NO (under 50 req/sec) → Vertical scaling sufficient

Is API the bottleneck?
├─ YES → Scale API (multiple instances + load balancer)
└─ NO → Continue

Is database the bottleneck?
├─ YES → Database scaling needed (read replicas)
└─ NO → Continue

Recommendation for now: ✅ NOT YET (single instance sufficient)

When to scale:
- API: >200 req/sec sustained
- Database: >100K records/day ingestion
- Memory: >1GB sustained usage
- CPU: >80% sustained
```

---

## Decision 9: "New AI session starting. What should I do?"

```
Read in order (27 minutes total):

1. THIS DIRECTORY (ai_context/)
   ├─ README.md (5 min)
   ├─ PROJECT_SNAPSHOT.md (2 min)
   ├─ CURRENT_STATE.md (2 min)
   ├─ ARCHITECTURE_SNAPSHOT.md (3 min)
   ├─ DECISION_TREES.md (2 min) [you are here]
   └─ VERSION_HISTORY.md (10 min)

2. KNOWN_ISSUES_DATABASE.md (2 min)

3. Then proceed with task

Result: ✅ Full context in 27 minutes
```

---

## Decision 10: "How do I continue from where the last AI left off?"

```
Step 1: Check CURRENT_STATE.md
├─ What was the last status?
├─ What phase are we in?
└─ What was completed?

Step 2: Read VERSION_HISTORY.md
├─ What was accomplished this session?
├─ What was attempted but deferred?
└─ What's next?

Step 3: Check Git Log
└─ Last 5 commits: git log --oneline -5

Step 4: Read KNOWN_ISSUES_DATABASE.md
└─ What problems were encountered?
└─ What solutions were found?

Step 5: Proceed with task
└─ Reference DECISION_TREES for how to proceed
```

---

**Status:** Decision trees active
**Last Updated:** October 18, 2025
**Usage:** Reference as needed during development
