# AI Context Checklist

**Purpose:** Verify you have full context before starting work
**For:** Any new Claude instance starting a session
**Time:** 1 minute to complete
**Status:** Use this to validate understanding

---

## Quick Context Check

Before you start ANY task on AlgoTrendy, verify you know:

### Project Basics (5 questions)

- [ ] **What is AlgoTrendy?**
  Answer: Automated cryptocurrency trading platform
  Source: PROJECT_SNAPSHOT.md

- [ ] **What version are we on?**
  Answer: v2.6 (C# .NET 8)
  Source: CURRENT_STATE.md

- [ ] **Is it production-ready?**
  Answer: Yes (226/264 tests passing, Docker ready)
  Source: CURRENT_STATE.md

- [ ] **Where is the code?**
  Answer: /root/AlgoTrendy_v2.6/backend/
  Source: README.md (this directory)

- [ ] **Is v2.5 (Python) still there?**
  Answer: Yes, at /root/algotrendy_v2.5 (DO NOT MODIFY)
  Source: CURRENT_STATE.md

### Architecture (5 questions)

- [ ] **What are the 4 main components?**
  Answer: API, Trading Engine, Data Channels, Strategies
  Source: ARCHITECTURE_SNAPSHOT.md

- [ ] **How does market data flow?**
  Answer: Exchange APIs → Channels → QuestDB → API → Clients
  Source: ARCHITECTURE_SNAPSHOT.md

- [ ] **How many exchanges are integrated?**
  Answer: 4 (Binance, OKX, Coinbase, Kraken) for data; Binance for trading
  Source: CURRENT_STATE.md

- [ ] **What database is used?**
  Answer: QuestDB (time-series, replaced TimescaleDB)
  Source: ARCHITECTURE_SNAPSHOT.md

- [ ] **How are orders executed?**
  Answer: Signal → RiskCheck → Broker (Binance only currently) → Exchange
  Source: ARCHITECTURE_SNAPSHOT.md

### Current State (5 questions)

- [ ] **What's the current deployment status?**
  Answer: Production-ready, tested, awaiting deployment
  Source: CURRENT_STATE.md

- [ ] **How many tests are passing?**
  Answer: 226/264 (85.6%)
  Source: CURRENT_STATE.md

- [ ] **What's NOT yet built?**
  Answer: Backtesting, additional brokers, more strategies, dashboard UI
  Source: CURRENT_STATE.md

- [ ] **What was just completed?**
  Answer: Version upgrade framework, AI context repository (this directory)
  Source: VERSION_HISTORY.md

- [ ] **What's blocking deployment?**
  Answer: Nothing - ready to deploy now
  Source: CURRENT_STATE.md

### Issues & Solutions (3 questions)

- [ ] **Do you know about Binance testnet configuration issue?**
  Answer: Yes, use environment variable BINANCE_API_TESTNET=true
  Source: KNOWN_ISSUES_DATABASE.md

- [ ] **Do you know about integration test credentials?**
  Answer: Yes, 12 tests skipped when BINANCE_API_KEY missing (normal)
  Source: KNOWN_ISSUES_DATABASE.md

- [ ] **Do you know where to find known issues?**
  Answer: KNOWN_ISSUES_DATABASE.md (this directory)
  Source: README.md

### Decision Making (3 questions)

- [ ] **Where do you look for decisions to make?**
  Answer: DECISION_TREES.md (this directory)
  Source: README.md

- [ ] **Where do you find what was accomplished?**
  Answer: VERSION_HISTORY.md and CURRENT_STATE.md
  Source: README.md

- [ ] **Where is the deployment guide?**
  Answer: /DEPLOYMENT_DOCKER.md (21KB, comprehensive)
  Source: CURRENT_STATE.md

---

## File Reading Order Verification

Verify you've read these files:

```
ai_context/
├─ [ ] README.md (2 min)                    - Framework overview
├─ [ ] PROJECT_SNAPSHOT.md (2 min)          - What is AlgoTrendy
├─ [ ] CURRENT_STATE.md (2 min)             - Where we are now
├─ [ ] ARCHITECTURE_SNAPSHOT.md (3 min)     - How it's built
├─ [ ] DECISION_TREES.md (2 min)            - How to decide
├─ [ ] KNOWN_ISSUES_DATABASE.md (2 min)     - Problems & solutions
├─ [ ] VERSION_HISTORY.md (10 min)          - What was built
└─ [ ] AI_CONTEXT_CHECKLIST.md (1 min)      - This file
```

**Total Reading Time:** 24-27 minutes
**Status:** Complete before starting any task

---

## Confidence Check

Rate your confidence in each area (1-5):

| Area | Confidence | If Not 5, Read... |
|------|-----------|-------------------|
| What AlgoTrendy is | ☐ | PROJECT_SNAPSHOT.md |
| Current status | ☐ | CURRENT_STATE.md |
| How to make decisions | ☐ | DECISION_TREES.md |
| System architecture | ☐ | ARCHITECTURE_SNAPSHOT.md |
| Known issues | ☐ | KNOWN_ISSUES_DATABASE.md |
| Version history | ☐ | VERSION_HISTORY.md |
| Next steps | ☐ | CURRENT_STATE.md |

**Status:** Ready if all ≥ 4

---

## Common Tasks Quick Guide

**"I'm starting fresh on this project"**
→ Read all files in order above (27 minutes)
→ Then check this checklist
→ Then proceed with task

**"I'm continuing from previous work"**
→ Read: CURRENT_STATE.md (2 min)
→ Skim: VERSION_HISTORY.md (5 min)
→ Check: DECISION_TREES.md for your task
→ Proceed

**"I hit an error"**
→ Search: KNOWN_ISSUES_DATABASE.md
→ If not found: Check git logs (`git log --oneline -10`)
→ If still confused: Read ARCHITECTURE_SNAPSHOT.md

**"I don't know what to do next"**
→ Read: CURRENT_STATE.md (section: What to Do Next)
→ Review: DECISION_TREES.md (10 decision scenarios)
→ Ask: Follow Decision Tree closest to your situation

**"I want to understand something specific"**
→ Use this quick index:

| Question | Answer File |
|----------|-------------|
| What is AlgoTrendy? | PROJECT_SNAPSHOT.md |
| How do I deploy? | /DEPLOYMENT_DOCKER.md |
| How is it built? | ARCHITECTURE_SNAPSHOT.md |
| What are known issues? | KNOWN_ISSUES_DATABASE.md |
| What was completed? | VERSION_HISTORY.md |
| What's next? | CURRENT_STATE.md |
| How do I decide? | DECISION_TREES.md |
| What's the current status? | CURRENT_STATE.md |

---

## Readiness Assessment

**Are you ready to work on AlgoTrendy?**

```
ALL of these must be true:

✅ You've read ai_context/README.md
✅ You understand what AlgoTrendy is
✅ You know we're on v2.6 (C# .NET 8)
✅ You know 226/264 tests passing
✅ You know deployment is ready
✅ You know the 4 main components
✅ You know Binance is only broker (currently)
✅ You know Phase 7 (backtesting, more brokers) is next
✅ You know where known issues are documented
✅ You know how to make decisions (DECISION_TREES.md)

If any above is FALSE:
→ Read the corresponding file from the list above
→ Come back to this checklist
→ Re-check
```

---

## Session Startup Procedure

**Every time a new Claude instance starts:**

1. **Read this file:** (1 min) - You are validating context
2. **If missing context:** Read files from list above (27 min)
3. **Verify checklist:** Answer all questions above
4. **Confirm readiness:** All should be ✅
5. **Review current task:** What am I working on today?
6. **Reference DECISION_TREES.md:** Find matching scenario
7. **Proceed with work:** Confident and fully contextual

---

## What NOT To Do

❌ **Do NOT modify v2.5**
→ It's at /root/algotrendy_v2.5
→ Read-only reference backup
→ DO NOT TOUCH

❌ **Do NOT hardcode secrets**
→ Use environment variables
→ Or User Secrets for development
→ Never in code

❌ **Do NOT skip Docker multi-stage build**
→ It's critical for 70% image size reduction
→ Final image: 245MB (vs 800MB without)

❌ **Do NOT deploy without testing**
→ Always test locally first
→ Recommend: Test in staging 24 hours

❌ **Do NOT ignore test failures**
→ 226/264 is good (85.6%)
→ But fix any new failures before deploying

---

## Quick Reference Commands

```bash
# Build
dotnet build

# Test
dotnet test

# Docker build
docker build -f backend/Dockerfile -t algotrendy:v2.6

# Docker dev environment
docker-compose up

# Docker production
docker-compose -f docker-compose.prod.yml up -d

# Check status
docker-compose ps

# View logs
docker-compose logs -f api

# Test API
curl http://localhost:5002/health

# Git status
git status

# Last commits
git log --oneline -10
```

---

## Session Transition Protocol

**When this session ends and next Claude starts:**

1. Save all work (commit to git)
2. Update CURRENT_STATE.md with new status
3. Update VERSION_HISTORY.md with what was done
4. If found new issues: Add to KNOWN_ISSUES_DATABASE.md
5. Commit with semantic message: `feat: X` or `fix: X`

**Next Claude will:**
1. Read CURRENT_STATE.md first (immediate context)
2. Skim VERSION_HISTORY.md (what happened)
3. Review DECISION_TREES.md (how to proceed)
4. Continue smoothly from where you left off

---

## Validation

✅ **This checklist helps ensure:**
- No lost context between sessions
- Every AI understands the project
- Common mistakes prevented
- Quick decisions possible
- Smooth session transitions

---

**Checklist Status:** Active & Maintained
**Last Updated:** October 18, 2025
**Version:** 1.0 (First AI context repository)
**Review:** Before every work session

---

## Final Question

**Are you ready to work on AlgoTrendy with full context?**

☐ YES - I've read all files and understand the project
☐ NO - I need to review: _____________ (which file?)

If YES: Proceed with confidence!
If NO: Read the missing file and come back.

---

**Welcome to AlgoTrendy!** 🚀
