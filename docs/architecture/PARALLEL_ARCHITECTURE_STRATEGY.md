# Parallel Architecture Strategy - v2.6 Monolith + v3.0 Modular

**Goal:** Maintain both monolith and modular architectures in perfect sync without duplicate work

**Date:** 2025-10-21

---

## Strategy Overview

### Current State
- **v2.6 (main branch):** Production monolith - all development happens here
- **v3.0 (modular branch):** Modular architecture - auto-synced from main

### Key Principles
1. **Single source of truth:** All code changes happen in `main` branch
2. **Automated synchronization:** `modular` branch auto-merges from `main`
3. **Different structures, same code:** Only project organization differs
4. **Parallel deployment:** Can deploy either version at any time

---

## Implementation Plan

### Phase 1: Create Modular Branch (1-2 hours)

#### Step 1: Create branch from current main
```bash
git checkout main
git pull origin main
git checkout -b modular
```

#### Step 2: Restructure for modular architecture
```bash
# Move projects to independent services
modular/
├── services/
│   ├── trading-service/        # AlgoTrendy.TradingEngine → standalone
│   ├── data-service/           # AlgoTrendy.DataChannels → standalone
│   ├── backtesting-service/    # AlgoTrendy.Backtesting → standalone
│   └── api-gateway/            # AlgoTrendy.API → gateway
├── shared/
│   └── AlgoTrendy.Core/        # Shared across all services
└── docker-compose.modular.yml  # Multi-service deployment
```

#### Step 3: Update project references
- Convert project references to NuGet packages
- Add service-to-service communication (REST/gRPC)
- Update Docker configurations

#### Step 4: Push modular branch
```bash
git add .
git commit -m "refactor: Create modular architecture for v3.0"
git push origin modular
```

---

### Phase 2: Automated Sync Setup (2-4 hours)

#### Option A: GitHub Actions (Recommended)
```yaml
# .github/workflows/sync-to-modular.yml
name: Sync Main to Modular

on:
  push:
    branches: [main]
  workflow_dispatch:

jobs:
  sync:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
        with:
          ref: main
          fetch-depth: 0

      - name: Sync to modular branch
        run: |
          git config user.name "github-actions[bot]"
          git config user.email "github-actions[bot]@users.noreply.github.com"

          # Fetch modular branch
          git fetch origin modular:modular
          git checkout modular

          # Merge main into modular
          git merge origin/main -m "chore: sync from main branch"

          # Push back
          git push origin modular

      - name: Handle conflicts
        if: failure()
        run: |
          # Create PR for manual conflict resolution
          gh pr create --base modular --head main \
            --title "Sync conflicts from main" \
            --body "Automated sync encountered conflicts"
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

#### Option B: Local Sync Script
```bash
#!/bin/bash
# scripts/sync-to-modular.sh

echo "Syncing main → modular branch..."

# Save current branch
CURRENT_BRANCH=$(git branch --show-current)

# Checkout modular
git checkout modular

# Merge from main
git merge main -m "chore: sync from main"

# If merge succeeds, push
if [ $? -eq 0 ]; then
  git push origin modular
  echo "✅ Sync successful"
else
  echo "⚠️  Conflicts detected - resolve manually"
  git merge --abort
fi

# Return to original branch
git checkout $CURRENT_BRANCH
```

---

### Phase 3: Development Workflow

#### Daily Development on Main Branch
```bash
# Developer works on v2.6 (monolith)
git checkout main
# ... make changes ...
git add .
git commit -m "feat: add new trading strategy"
git push origin main

# Automated sync triggers (GitHub Actions)
# OR run manually: ./scripts/sync-to-modular.sh
```

#### Testing Modular Version
```bash
# Switch to modular branch
git checkout modular

# Build and test modular architecture
docker-compose -f docker-compose.modular.yml up

# If issues found, fix in main branch, then re-sync
```

---

### Phase 4: Parallel CI/CD

#### Build Both Versions
```yaml
# .github/workflows/build.yml
name: Build Both Architectures

on: [push, pull_request]

jobs:
  build-monolith:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
        with:
          ref: main
      - name: Build v2.6 Monolith
        run: dotnet build
      - name: Run tests
        run: dotnet test

  build-modular:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
        with:
          ref: modular
      - name: Build v3.0 Modular
        run: docker-compose -f docker-compose.modular.yml build
      - name: Run integration tests
        run: docker-compose -f docker-compose.modular.yml up --abort-on-container-exit
```

---

## File Mapping: Monolith vs Modular

### Monolith (main branch)
```
backend/
├── AlgoTrendy.API/              # All-in-one API
├── AlgoTrendy.Core/
├── AlgoTrendy.TradingEngine/
├── AlgoTrendy.DataChannels/
└── AlgoTrendy.Backtesting/
```

### Modular (modular branch)
```
services/
├── trading-service/
│   ├── src/
│   │   └── AlgoTrendy.TradingEngine/  # SAME code from main
│   ├── Dockerfile
│   └── appsettings.json
├── data-service/
│   ├── src/
│   │   └── AlgoTrendy.DataChannels/   # SAME code from main
│   ├── Dockerfile
│   └── appsettings.json
├── backtesting-service/
│   ├── src/
│   │   └── AlgoTrendy.Backtesting/    # SAME code from main
│   ├── Dockerfile
│   └── appsettings.json
└── api-gateway/
    ├── src/
    │   └── AlgoTrendy.API/             # SAME code from main
    ├── Dockerfile
    └── appsettings.json

shared/
└── AlgoTrendy.Core/                    # SAME code from main
```

**Key Point:** The actual `.cs` files are identical - only the organization differs!

---

## Conflict Resolution Strategy

### Common Conflicts
1. **Project structure changes** - Manual resolution needed
2. **New dependencies** - Auto-merge usually works
3. **Code changes** - Auto-merge (no conflicts)

### Resolution Process
```bash
# If automated sync fails
git checkout modular
git merge main

# Resolve conflicts manually
git status  # See conflicted files
# Edit files to resolve
git add .
git commit -m "chore: resolve sync conflicts"
git push origin modular
```

---

## Deployment Options

### Deploy Monolith (v2.6)
```bash
git checkout main
docker-compose up -d
# Single container, all-in-one
```

### Deploy Modular (v3.0)
```bash
git checkout modular
docker-compose -f docker-compose.modular.yml up -d
# Multiple containers, microservices
```

### A/B Testing
```bash
# Run both simultaneously on different ports
docker-compose up -d                              # Monolith on 5000
docker-compose -f docker-compose.modular.yml up -d  # Modular on 5001

# Compare performance, test compatibility
```

---

## Version Release Strategy

### v2.6.x Releases (Monolith)
- Continue as normal
- Fast iterations
- Single deployment
- Production-proven

### v3.0.x Releases (Modular)
- Beta testing
- Performance testing
- Gradual rollout
- Canary deployments

### v4.0 (Future)
- Retire monolith
- Full modular only
- Based on v3.0 learnings

---

## Benefits of This Approach

1. **No duplicate work** - Code changes once, auto-syncs
2. **No timeline delay** - v2.6 development continues full speed
3. **Parallel testing** - Test both architectures
4. **Risk mitigation** - Fallback to monolith if needed
5. **Gradual migration** - Move services one at a time
6. **Customer choice** - Deploy what works best for them

---

## Timeline Estimate

| Phase | Time | Impact |
|-------|------|--------|
| Create modular branch | 2-4 hours | Low |
| Setup auto-sync | 2-4 hours | Low |
| Test sync process | 1 hour | Low |
| Setup parallel CI/CD | 4-6 hours | Medium |
| **Total** | **9-15 hours** | **Low-Medium** |

---

## Success Criteria

- ✅ Both branches build successfully
- ✅ Auto-sync runs on every main push
- ✅ Conflicts are rare (<5%)
- ✅ CI/CD tests both versions
- ✅ Documentation updated
- ✅ Team understands workflow

---

## Next Steps

1. **Create modular branch** from current main
2. **Restructure for services** (keep code identical)
3. **Setup GitHub Actions** for auto-sync
4. **Test sync workflow** with sample changes
5. **Update CI/CD** to build both
6. **Document workflow** for team

Would you like me to start with Phase 1?
