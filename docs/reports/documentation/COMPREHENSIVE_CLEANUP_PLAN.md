# AlgoTrendy v2.6 - Comprehensive Cleanup & Organization Plan

**Created**: October 21, 2025
**Status**: Planning Phase
**Goal**: Organize entire codebase according to computer science best practices

---

## Current State Analysis

### Issues Identified

1. ✅ **43 top-level MD files** - Should be organized into docs/
2. ✅ **strategyGrpDev02/** - Legacy folder, already migrated
3. ✅ **Duplicate/scattered documentation** - planning/, reports/, docs/, root
4. ⚠️ **Unclear service organization** - ml-service, backtesting-py-service, services/
5. ⚠️ **Mixed planning files** - Some in /planning, some at root

---

## Proposed Best-Practice Structure

```
/root/AlgoTrendy_v2.6/
│
├── .github/              # CI/CD workflows
├── .vscode/              # IDE config
├── .claude/              # Claude Code config
├── .qodo/                # Qodo config
│
├── backend/              # ✅ C# Backend (already organized)
├── frontend/             # ✅ React Frontend (already organized)
├── strategies/           # ✅ Trading Strategies (just organized)
├── MEM/                  # ✅ MEM AI System
│
├── services/             # ⚙️ Microservices (REORGANIZE)
│   ├── ml-service/
│   ├── backtesting/
│   ├── risk-management/
│   └── README.md
│
├── infrastructure/       # ✅ Infrastructure as Code
│   ├── ansible/
│   ├── docker/
│   ├── certbot/
│   └── ssl/
│
├── data/                 # ✅ Centralized Data
│   ├── strategy_registry/
│   ├── questdb-data/
│   └── ml_models/
│
├── docs/                 # 📚 ALL DOCUMENTATION (REORGANIZE)
│   ├── README.md
│   ├── architecture/
│   ├── api/
│   ├── deployment/
│   ├── developer/
│   ├── planning/
│   │   ├── research/
│   │   ├── roadmaps/
│   │   └── decisions/
│   ├── reports/
│   │   ├── validation/
│   │   ├── sessions/
│   │   └── analysis/
│   ├── security/
│   ├── testing/
│   └── user-guides/
│
├── scripts/              # ✅ Utility Scripts
├── devtools/             # ✅ Development Tools
├── integrations/         # ✅ Third-party Integrations
├── benchmarks/           # ✅ Performance Benchmarks
│
├── archive/              # 📦 Archived/Legacy Code
│   ├── strategyGrpDev02_archived_20251021/
│   ├── legacy_reference/
│   └── tfvc-projects/
│
├── .dev/                 # Development temp files
├── logs/                 # Runtime logs
├── backups/              # Backups
│
├── docker-compose*.yml   # Docker configs
├── package.json          # Node deps
├── requirements.txt      # Python deps
├── .env*                 # Environment configs
├── .gitignore
├── README.md             # ✨ Main project README
└── CHANGELOG.md
```

---

## Cleanup Tasks

### Phase 1: Archive Legacy Folders ✅

**Action**: Move old/replaced folders to /archive/

```bash
# Archive strategyGrpDev02
mv /root/AlgoTrendy_v2.6/strategyGrpDev02 \
   /root/AlgoTrendy_v2.6/archive/strategyGrpDev02_archived_20251021

# Archive legacy_reference (already there)
# Archive tfvc-projects
mv /root/AlgoTrendy_v2.6/tfvc-projects \
   /root/AlgoTrendy_v2.6/archive/tfvc-projects

# Archive file_mgmt_code (unclear purpose)
mv /root/AlgoTrendy_v2.6/file_mgmt_code \
   /root/AlgoTrendy_v2.6/archive/file_mgmt_code_archived_20251021
```

---

### Phase 2: Organize Documentation 📚

#### 2A: Move Root-Level MD Files to docs/

**Session Summaries** → `docs/reports/sessions/`
```
SESSION_*.md → docs/reports/sessions/
```

**Architecture Docs** → `docs/architecture/`
```
ARCHITECTURE_*.md → docs/architecture/
AI_CONTEXT.md → docs/architecture/
PARALLEL_ARCHITECTURE_STRATEGY.md → docs/architecture/
```

**Planning Docs** → `docs/planning/decisions/`
```
MULTI_AI_BUILD_PLAN.md → docs/planning/decisions/
REORGANIZATION_PLAN.md → docs/planning/decisions/
V2.6_SAFETY_PLAN.md → docs/planning/decisions/
```

**Feature/Competitive Docs** → `docs/analysis/`
```
COMPETITIVE_*.md → docs/analysis/
ACTUAL_MISSING_FEATURES.md → docs/analysis/
```

**Documentation Summaries** → `docs/reports/documentation/`
```
DOCUMENTATION_*.md → docs/reports/documentation/
CLEANUP_REPORT.md → docs/reports/documentation/
```

**Frontend Docs** → `docs/frontend/`
```
FRONTEND_*.md → docs/frontend/
```

**Quick Guides** → `docs/user-guides/`
```
QUICK_START_GUIDE.md → docs/user-guides/
DEVELOPER_ONBOARDING.md → docs/developer/
```

**Security Docs** → `docs/security/`
```
SECURITY*.md → docs/security/
```

**Version/Release Docs** → `docs/deployment/`
```
VERSION_MANAGEMENT_TOOLING.md → docs/deployment/
```

#### 2B: Consolidate /planning/ folder

Move `/planning/` research files to `docs/planning/research/`:
```
planning/MEM_ENHANCED_STRATEGIES_RESEARCH.md → docs/planning/research/
planning/TOP_5_TRADING_STRATEGIES_FOR_MEM.md → docs/planning/research/
planning/DAY_TRADING_TIMEFRAMES_RESEARCH.md → docs/planning/research/
planning/HIGH_FREQUENCY_ALGO_RESEARCH.md → docs/planning/research/
planning/TICK_AND_RANGE_IMPLEMENTATION_PLAN.md → docs/planning/research/
planning/STOCKS_FUTURES_INTEGRATION_PLAN.md → docs/planning/research/
```

Move phase docs to `docs/planning/roadmaps/`:
```
planning/phase*/→ docs/planning/roadmaps/phases/
planning/*_roadmap.md → docs/planning/roadmaps/
planning/migration_plan.md → docs/planning/roadmaps/
```

Keep strategic planning at `/planning/` (optional) or move everything to docs.

#### 2C: Consolidate /reports/ folder

Move to `docs/reports/validation/`:
```
reports/* → docs/reports/validation/
```

---

### Phase 3: Organize Services 🛠️

**Consolidate under /services/**:

```bash
# Move backtesting-py-service
mv /root/AlgoTrendy_v2.6/backtesting-py-service \
   /root/AlgoTrendy_v2.6/services/backtesting

# Check if risk_management should be here
# If it's a service, move it:
mv /root/AlgoTrendy_v2.6/risk_management \
   /root/AlgoTrendy_v2.6/services/risk-management

# ml-service already exists - verify structure
```

Create `/services/README.md` with service catalog.

---

### Phase 4: Organize Infrastructure 🏗️

**Consolidate under /infrastructure/**:

```bash
# Move ansible
mv /root/AlgoTrendy_v2.6/ansible \
   /root/AlgoTrendy_v2.6/infrastructure/ansible

# Move docker
mv /root/AlgoTrendy_v2.6/docker \
   /root/AlgoTrendy_v2.6/infrastructure/docker

# Move certbot
mv /root/AlgoTrendy_v2.6/certbot \
   /root/AlgoTrendy_v2.6/infrastructure/certbot

# Move ssl
mv /root/AlgoTrendy_v2.6/ssl \
   /root/AlgoTrendy_v2.6/infrastructure/ssl
```

---

### Phase 5: Organize Data 💾

**Consolidate under /data/**:

```bash
# Move questdb-data
mv /root/AlgoTrendy_v2.6/questdb-data \
   /root/AlgoTrendy_v2.6/data/questdb

# Move ml_models
mv /root/AlgoTrendy_v2.6/ml_models \
   /root/AlgoTrendy_v2.6/data/ml_models

# Move database (if data, not code)
# Check database/ folder first
```

---

### Phase 6: Organize Scripts & Tools 🔧

**Consolidate version tools**:

```bash
# Move version upgrade tools
mv /root/AlgoTrendy_v2.6/version_upgrade_tools\&doc \
   /root/AlgoTrendy_v2.6/devtools/version-upgrade
```

**Organize prompts**:

```bash
# Move prompts to docs
mv /root/AlgoTrendy_v2.6/prompts \
   /root/AlgoTrendy_v2.6/docs/prompts
```

**Organize troubleshoot**:

```bash
# Merge with docs/guides
mv /root/AlgoTrendy_v2.6/troubleshoot/* \
   /root/AlgoTrendy_v2.6/docs/guides/troubleshooting/
```

---

### Phase 7: Clean Root Directory 🧹

**After all moves, root should contain ONLY**:

#### Config Files (Keep)
- .env*
- .gitignore, .gitattributes
- .editorconfig
- .clauderc
- .markdownlint.json
- .releaserc.json
- GitVersion.yml
- renovate.json

#### Docker Files (Keep)
- docker-compose.yml
- docker-compose.*.yml
- nginx.conf

#### Package Files (Keep)
- package.json
- requirements.txt

#### Essential Docs (Keep)
- README.md
- CHANGELOG.md
- CONTRIBUTING.md
- SECURITY.md

#### API Collections (Keep or move to docs/)
- AlgoTrendy_API.postman_collection.json → docs/api/

#### Data Files (Move to data/)
- broker_api_latency_results.json → data/benchmarks/

#### Git Commit Messages (Archive or delete)
- frontend_enhancement_git_commit.txt → archive/ or delete

#### Nginx Backup (Delete)
- nginx.conf.backup → delete

---

## Detailed Action Plan

### Week 1: Documentation Cleanup

**Day 1: Archive Legacy**
- [ ] Archive strategyGrpDev02
- [ ] Archive file_mgmt_code
- [ ] Archive tfvc-projects

**Day 2: Session Summaries**
- [ ] Create docs/reports/sessions/
- [ ] Move all SESSION_*.md files
- [ ] Create index in sessions/ folder

**Day 3: Architecture Docs**
- [ ] Organize architecture docs to docs/architecture/
- [ ] Create architecture index

**Day 4: Planning & Research**
- [ ] Organize planning/ folder
- [ ] Move research docs to docs/planning/research/
- [ ] Move roadmaps to docs/planning/roadmaps/

**Day 5: Reports & Analysis**
- [ ] Move reports/ to docs/reports/validation/
- [ ] Move competitive analysis to docs/analysis/

---

### Week 2: Services & Infrastructure

**Day 1: Services**
- [ ] Consolidate services under /services/
- [ ] Create services catalog README

**Day 2: Infrastructure**
- [ ] Consolidate infrastructure files
- [ ] Create infrastructure README

**Day 3: Data**
- [ ] Consolidate data directories
- [ ] Create data README

**Day 4-5: Verification**
- [ ] Test all moved files
- [ ] Update references in code
- [ ] Run builds to verify

---

### Week 3: Documentation & Testing

**Day 1-2: Create Navigation**
- [ ] Create master DOCUMENTATION_INDEX.md
- [ ] Update all README files
- [ ] Create folder-level READMEs

**Day 3-4: Update References**
- [ ] Search for file path references in code
- [ ] Update import statements
- [ ] Update docker-compose volume mounts

**Day 5: Final Cleanup**
- [ ] Delete temporary files
- [ ] Clean root directory
- [ ] Final verification

---

## Benefits

### Improved Organization
✅ Clear separation of concerns
✅ Easy to navigate for new developers
✅ Follows industry standards

### Better Maintenance
✅ Easier to find files
✅ Reduced duplication
✅ Clear ownership

### Scalability
✅ Structure supports growth
✅ Easy to add new modules
✅ Clean for CI/CD

---

## Risk Mitigation

### Before Moving Files
1. ✅ Create backups
2. ✅ Document all moves
3. ✅ Test after each phase

### During Cleanup
1. ✅ Move, don't delete (until verified)
2. ✅ Keep git history
3. ✅ Update documentation

### After Cleanup
1. ✅ Run full test suite
2. ✅ Verify builds
3. ✅ Update team

---

## Success Criteria

✅ **Root directory has <20 files**
✅ **All docs in /docs/ with clear structure**
✅ **Services consolidated under /services/**
✅ **Infrastructure under /infrastructure/**
✅ **All legacy code in /archive/**
✅ **No duplicate documentation**
✅ **All paths updated and working**
✅ **Comprehensive navigation docs**

---

**Status**: Plan Ready for Execution
**Next Step**: Begin Phase 1 - Archive Legacy Folders
