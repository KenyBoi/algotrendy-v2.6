# AlgoTrendy v2.6 Project Reorganization Plan

**Date:** 2025-10-21
**Goal:** Organize project structure following software engineering best practices

## Best Practices Applied

### 1. Folder Structure Principles
- **Separation of concerns**: Code, docs, tests, config should be separate
- **Shallow nesting**: Avoid deep hierarchies (max 3-4 levels)
- **Logical grouping**: Group by domain/purpose
- **Clear naming**: Use descriptive, consistent names (lowercase, hyphens/underscores)

### 2. Naming Conventions
- Use YYYYMMDD format for dates in filenames
- Use kebab-case or snake_case consistently
- Descriptive names that indicate purpose
- Avoid abbreviations unless standard (API, ML, etc.)

### 3. Documentation Organization
- Root: Only essential docs (README.md, CONTRIBUTING.md, LICENSE)
- docs/: All other documentation organized by category
- Separate user docs from developer docs

## Current Issues Identified

### Root Directory Pollution
- **23+ markdown files** at root level
- **12+ script files** at root level
- **Multiple config variations** (.env.*, nginx.conf.backup)
- **Temporary/report files** (migration_report.json, pattern_analysis_report.json, ssl-setup.log)

### Unclear Directory Purposes
- `filled/` - Old plan documents, should be archived
- `tfvc-projects/` - Empty, legacy from TFVC migration
- `trees/` - Contains only session-state.json
- `troubleshoot/` - Empty
- `prompts/` - Empty
- `ai_context/` - AI session files, development tooling
- `planning/` - Phase-based planning docs, historical

### Disorganized Scripts
- ML scripts scattered at root
- Setup scripts at root
- Mix of Python and shell scripts

## Reorganization Strategy

### Phase 1: Archive Structure Creation
Create organized archive with subdirectories:
```
archive/
├── 2025-10-21_project_cleanup/
│   ├── deprecated_directories/
│   ├── old_documentation/
│   ├── old_scripts/
│   ├── temporary_files/
│   └── legacy_plans/
```

### Phase 2: Documentation Consolidation
Organize into docs/ structure:
```
docs/
├── user/                          # User-facing documentation
├── developer/                     # Developer documentation
├── integration/                   # Integration guides & summaries
│   ├── data-providers/
│   ├── mem/
│   ├── ml/
│   └── backtesting/
├── deployment/                    # Deployment & operations
├── architecture/                  # Architecture & design
└── historical/                    # Historical summaries/reports
```

### Phase 3: Scripts Organization
```
scripts/
├── setup/                         # Setup & installation scripts
├── deployment/                    # Deployment scripts
├── ml/                           # ML-related scripts
├── database/                      # Database scripts
└── utilities/                     # General utilities
```

### Phase 4: Development Tooling
```
.dev/                             # Development tooling (hidden)
├── ai-context/
├── planning/
└── session-notes/
```

## Detailed File Movements

### Documentation Files (Root → docs/)

#### Integration Documentation → docs/integration/
- `DATA_PROVIDERS_INTEGRATION_SUMMARY.md` → `docs/integration/data-providers/summary.md`
- `TIINGO_INTEGRATION.md` → `docs/integration/data-providers/tiingo.md`
- `MEM_FRONTEND_INTEGRATION_COMPLETE.md` → `docs/integration/mem/frontend-integration.md`
- `QUANTCONNECT_MEM_INTEGRATION.md` → `docs/integration/mem/quantconnect-integration.md`
- `ML_TRAINING_WEB_PAGE_INTEGRATION.md` → `docs/integration/ml/training-web-page.md`
- `INTEGRATION_MIGRATION_SUMMARY.md` → `docs/integration/migration-summary.md`

#### ML Documentation → docs/integration/ml/
- `ML_DATA_CONNECTION_POINTS.md` → `docs/integration/ml/data-connection-points.md`
- `ML_MODEL_RETRAINING_GUIDE.md` → `docs/integration/ml/model-retraining-guide.md`
- `ML_RESEARCH_ENHANCEMENTS.md` → `docs/integration/ml/research-enhancements.md`
- `PATTERN_ANALYSIS_INSIGHTS_20251020.md` → `docs/integration/ml/pattern-analysis-insights-20251020.md`
- `PATTERN_ANALYSIS_SUMMARY.md` → `docs/integration/ml/pattern-analysis-summary.md`

#### Historical/Summary Documentation → docs/historical/
- `CLEANUP_SUMMARY.md` → `docs/historical/cleanup-summary.md`
- `DOCUMENTATION_UPDATE_SUMMARY.md` → `docs/historical/documentation-update-summary.md`
- `PHASE_7_ENABLEMENT_SUMMARY.md` → `docs/historical/phase-7-enablement-summary.md`
- `REFACTORING_COMPLETE_SUMMARY.md` → `docs/historical/refactoring-complete-summary.md`
- `PR_DESCRIPTION.md` → `docs/historical/pr-description.md`
- `CUSTOM_ENGINE_DISABLED.md` → `docs/historical/custom-engine-disabled.md`

#### Deployment Documentation → docs/deployment/
- `CREDENTIALS_SETUP_GUIDE.md` → `docs/deployment/credentials-setup-guide.md`
- `SECURITY_UPDATES.md` → `docs/deployment/security-updates.md`

#### Developer Documentation
- `TODO_TREE.md` → `docs/developer/todo-tree.md`
- Keep `CONTRIBUTING.md` at root

### Scripts (Root → scripts/)

#### ML Scripts → scripts/ml/
- `retrain_model.py` → `scripts/ml/retrain_model.py`
- `retrain_model_v2.py` → `scripts/ml/retrain_model_v2.py`
- `run_pattern_analysis.py` → `scripts/ml/run_pattern_analysis.py`
- `ml_api_server.py` → `scripts/ml/ml_api_server.py`
- `memgpt_metrics_dashboard.py` → `scripts/ml/memgpt_metrics_dashboard.py`
- `start_ml_system.sh` → `scripts/ml/start_ml_system.sh`
- `stop_ml_system.sh` → `scripts/ml/stop_ml_system.sh`

#### Setup Scripts → scripts/setup/
- `quick_setup_credentials.sh` → `scripts/setup/quick_setup_credentials.sh`
- `check-dns.sh` → `scripts/setup/check-dns.sh`
- `USE_LOCAL_LEAN.sh` → `scripts/setup/use_local_lean.sh`

#### Testing Scripts
- `test_providers.sh` → `scripts/testing/test_providers.sh`

#### Utilities → scripts/utilities/
- `code_migration_analyzer.py` → `scripts/utilities/code_migration_analyzer.py`

### Temporary/Report Files → archive/
- `migration_report.json` → `archive/2025-10-21_project_cleanup/temporary_files/`
- `pattern_analysis_report.json` → `archive/2025-10-21_project_cleanup/temporary_files/`
- `ssl-setup.log` → `archive/2025-10-21_project_cleanup/temporary_files/`
- `nginx.conf.backup` → `archive/2025-10-21_project_cleanup/temporary_files/`

### Directories to Archive

#### Old/Empty Directories → archive/deprecated_directories/
- `filled/` → `archive/2025-10-21_project_cleanup/deprecated_directories/filled/`
- `tfvc-projects/` → Remove (empty)
- `trees/` → `archive/2025-10-21_project_cleanup/deprecated_directories/trees/`
- `troubleshoot/` → Remove (empty)
- `prompts/` → Remove (empty)

#### Development Context → .dev/
- `ai_context/` → `.dev/ai-context/`
- `planning/` → `.dev/planning/`

### Root Directory After Cleanup

**Files at Root:**
- README.md (main project readme)
- CONTRIBUTING.md (contribution guidelines)
- LICENSE (if exists)
- .gitignore
- .gitattributes
- .editorconfig
- .env.example (template only)
- docker-compose.yml
- docker-compose.prod.yml
- renovate.json
- requirements.txt (if needed at root)

**Directories at Root:**
- backend/
- frontend/
- docs/
- scripts/
- tests/ (if separate from backend/frontend)
- data/
- database/
- infrastructure/
- integrations/
- ml-service/
- backtesting-py-service/
- .dev/ (hidden, development tooling)
- archive/ (historical/deprecated items)

## .gitignore Updates

Add to .gitignore:
```
# Development context (local only)
.dev/

# Temporary files
*.log
*.tmp
*.backup
*_backup.*

# Reports (generated)
*_report.json
pattern_*.json
```

## Success Criteria

1. ✅ Root directory contains <15 files
2. ✅ All documentation organized in docs/ with logical structure
3. ✅ All scripts organized in scripts/ by purpose
4. ✅ No empty directories
5. ✅ Clear separation: code, docs, scripts, config, archive
6. ✅ Consistent naming conventions throughout
7. ✅ Updated .gitignore reflects new structure
8. ✅ All archived items properly organized with timestamps

## Notes

- **DO NOT DELETE** any files, only move/archive
- Maintain git history where possible
- Test key scripts after moving to ensure paths are updated
- Update any hard-coded paths in code/configs
- Document all changes in CLEANUP_REPORT.md
