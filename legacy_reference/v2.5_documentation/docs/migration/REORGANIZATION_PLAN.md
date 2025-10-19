# 📁 V2.5 Organization & Standardization Plan

> **Date**: October 16, 2025  
> **Status**: REORGANIZATION REQUIRED  
> **Goal**: Standardize all items to follow documented structure

---

## Current State vs. Intended State

### Current Directory Structure
```
algotrendy_v2.5/
├── .github/
├── .qodo/
├── Brokers/                    ❌ Needs organization
├── MEM/                        ❌ Partially organized
├── algotrendy/                 ✅ OK
├── algotrendy-api/             ✅ OK
├── algotrendy-web/             ✅ OK
├── config/                     ⚠️  Needs categorization
├── docs/                       ✅ OK (ai-agents added)
├── scripts/                    ✅ OK
└── utils/                      ⚠️  Needs cleanup
```

### Intended Standardized Structure
```
algotrendy_v2.5/
├── .github/                    # GitHub configuration
├── algotrendy/                 # Trading engine
├── algotrendy-api/             # FastAPI backend
├── algotrendy-web/             # Next.js frontend
├── integrations/               # 🆕 All integrations
│   ├── tradingview/
│   ├── freqtrade/
│   └── composer/
├── brokers/                    # 🆕 Broker adapters
│   ├── bybit/
│   ├── alpaca/
│   └── ...
├── data/                       # 🆕 Historical & training data
│   ├── knowledge/
│   ├── real_market_data/
│   └── mem_knowledge/
├── docs/                       # All documentation
├── config/                     # Configuration files
├── scripts/                    # Utility scripts
├── utils/                      # Utility modules
└── .qodo/                      # QoDo configuration
```

---

## Migration Actions Required

### 1. **Brokers/ Directory** → `brokers/` (Lowercase)
**Status**: ⚠️ NEEDS ORGANIZATION

Current:
```
Brokers/
└── composer_trade_integration.py
```

Action:
```bash
# Move to standardized location
mkdir -p brokers/composer
mv Brokers/composer_trade_integration.py brokers/composer/integration.py
rmdir Brokers
```

**Rationale**: 
- Lowercase for consistency
- Organize by broker name in subdirectories
- Each broker gets its own folder for scalability

---

### 2. **MEM/ Directory** → `integrations/mem/` or `systems/mem/`
**Status**: ⚠️ NEEDS REORGANIZATION

Current:
```
MEM/
└── MEM_Modules_toolbox/
    └── [Python modules]
```

Action:
```bash
# Create standardized structure
mkdir -p integrations/mem
mv MEM/MEM_Modules_toolbox integrations/mem/modules
rmdir MEM/MEM
```

**Rationale**:
- MEM is an external integration/system
- Should be in `integrations/` directory
- Modules clearly labeled

---

### 3. **config/ Directory** → Consolidate
**Status**: ⚠️ NEEDS ORGANIZATION

Current:
```
config/
└── composer_config.json
```

Action:
```bash
# Organize by integration type
mkdir -p config/{integrations,trading,system}
mv config/composer_config.json config/integrations/composer.json
```

**Rationale**:
- Group configs by purpose
- Easier to find and manage
- Scales for future configs

---

### 4. **utils/ Directory** → Categorize
**Status**: ⚠️ NEEDS CLEANUP

Current:
```
utils/
└── sqlite_manager.py
```

Action:
```bash
# Create utility subcategories
mkdir -p utils/{database,helpers,monitoring}
mv utils/sqlite_manager.py utils/database/sqlite_manager.py
```

**Rationale**:
- Group utilities by function
- Clearer purpose and organization
- Easier to discover tools

---

### 5. **New: Create data/ Directory** 
**Status**: ⏳ TO BE CREATED

Action:
```bash
mkdir -p data/{knowledge,real_market_data,mem_knowledge,backtest_results}
# Copy from v2.4 (Phase 1 migration)
cp -r /root/algotrendy_v2.4/knowledge/* data/knowledge/
cp -r /root/algotrendy_v2.4/real_market_data/* data/real_market_data/
cp -r /root/algotrendy_v2.4/mem_knowledge/* data/mem_knowledge/
```

**Rationale**:
- Centralized data directory
- Separates code from data
- Easier backups and data management

---

### 6. **New: Create integrations/ Directory**
**Status**: ⏳ TO BE CREATED

Action:
```bash
mkdir -p integrations/{tradingview,composer,freqtrade,mem}
# TradingView was already copied (needs organizing)
# MEM will move here
# Other integrations can go here
```

**Rationale**:
- All external integrations in one place
- Clear separation from core system
- Easy to add new integrations

---

## Step-by-Step Reorganization Plan

### Phase 1: Directory Creation (5 minutes)
```bash
cd /root/algotrendy_v2.5

# Create standardized directories
mkdir -p brokers/{bybit,alpaca,binance,okx,kraken}
mkdir -p integrations/{tradingview,composer,freqtrade,mem}
mkdir -p data/{knowledge,real_market_data,mem_knowledge,backtest_results}
mkdir -p config/{integrations,trading,system}
mkdir -p utils/{database,helpers,monitoring}
```

### Phase 2: Move Brokers (2 minutes)
```bash
# Move broker files
mkdir -p brokers/composer
mv Brokers/composer_trade_integration.py brokers/composer/integration.py
rmdir Brokers
```

### Phase 3: Move MEM (3 minutes)
```bash
# Move MEM integration
mkdir -p integrations/mem
mv MEM/MEM_Modules_toolbox integrations/mem/modules
rmdir MEM/MEM 2>/dev/null
rmdir MEM
```

### Phase 4: Organize Config (2 minutes)
```bash
# Organize configs
mkdir -p config/{integrations,trading,system}
mv config/composer_config.json config/integrations/composer.json
```

### Phase 5: Organize Utils (2 minutes)
```bash
# Organize utilities
mkdir -p utils/{database,helpers,monitoring}
mv utils/sqlite_manager.py utils/database/sqlite_manager.py
```

### Phase 6: Create Archive (Optional, 5 minutes)
```bash
# Archive historical/deprecated items
mkdir -p archive/{v2.4_reference,deprecated,legacy}
```

---

## Complete Standardized Structure

After reorganization:

```
algotrendy_v2.5/
├── .github/
│   └── copilot-instructions.md                 # AI agent instructions
│
├── algotrendy/                                 # Core trading engine
│   ├── unified_trader.py
│   ├── strategy_resolver.py
│   ├── broker_abstraction.py
│   ├── secure_credentials.py
│   └── configs/
│
├── algotrendy-api/                             # FastAPI backend
│   ├── app/
│   └── requirements.txt
│
├── algotrendy-web/                             # Next.js frontend
│   ├── src/
│   └── package.json
│
├── brokers/                                    # Broker adapters
│   ├── bybit/
│   ├── alpaca/
│   ├── binance/
│   ├── okx/
│   ├── kraken/
│   ├── composer/
│   │   └── integration.py
│   └── README.md
│
├── integrations/                               # External integrations
│   ├── tradingview/
│   │   ├── scripts/
│   │   └── README.md
│   ├── freqtrade/
│   ├── composer/
│   ├── mem/
│   │   ├── modules/
│   │   └── README.md
│   └── README.md
│
├── data/                                       # Historical & training data
│   ├── knowledge/
│   │   ├── articles/
│   │   ├── indicators/
│   │   ├── market_insights/
│   │   └── strategies/
│   ├── real_market_data/
│   │   ├── *.csv (OHLCV data)
│   │   └── *.json (charts)
│   ├── mem_knowledge/
│   ├── backtest_results/
│   └── README.md
│
├── config/                                     # Configuration files
│   ├── integrations/
│   │   ├── composer.json
│   │   ├── tradingview.json
│   │   └── README.md
│   ├── trading/
│   │   └── README.md
│   └── system/
│       └── README.md
│
├── docs/                                       # All documentation
│   ├── ai-agents/
│   │   ├── .github/copilot-instructions.md (symlink/copy)
│   │   └── [supporting docs]
│   ├── architecture/
│   ├── deployment/
│   ├── integration/
│   ├── progress/
│   ├── reference/
│   ├── setup/
│   ├── migration/
│   │   └── V2_4_MIGRATION_AUDIT_REPORT.md
│   └── README.md
│
├── scripts/                                    # Utility scripts
│   ├── setup/
│   ├── deployment/
│   ├── test/
│   └── README.md
│
├── utils/                                      # Utility modules
│   ├── database/
│   │   └── sqlite_manager.py
│   ├── helpers/
│   └── monitoring/
│
├── archive/                                    # Deprecated items
│   ├── v2.4_reference/
│   ├── deprecated/
│   └── legacy/
│
├── README.md
├── STRUCTURE.md
└── .qodo/
```

---

## File Relocation Summary

| Current Path | New Path | Action | Priority |
|---|---|---|---|
| `Brokers/` | `brokers/composer/` | Move | 🔴 HIGH |
| `MEM/MEM_Modules_toolbox/` | `integrations/mem/modules/` | Move | 🔴 HIGH |
| `config/composer_config.json` | `config/integrations/composer.json` | Move | 🟡 MEDIUM |
| `utils/sqlite_manager.py` | `utils/database/sqlite_manager.py` | Move | 🟡 MEDIUM |
| (create) | `data/` | Create | 🔴 HIGH |
| (create) | `integrations/` | Create | 🔴 HIGH |

---

## Reorganization Checklist

### Pre-Reorganization
- [ ] Backup current v2.5 structure
- [ ] Verify all files are accounted for
- [ ] Document current locations
- [ ] Test backup integrity

### Reorganization Phase 1
- [ ] Create new directory structure
- [ ] Move Brokers → brokers/
- [ ] Move MEM → integrations/mem/
- [ ] Move configs appropriately
- [ ] Move utils appropriately

### Reorganization Phase 2
- [ ] Copy historical data to data/
- [ ] Copy knowledge base
- [ ] Copy market data
- [ ] Copy MemGPT knowledge

### Post-Reorganization
- [ ] Verify all files moved successfully
- [ ] Update all imports in code
- [ ] Update documentation
- [ ] Update STRUCTURE.md
- [ ] Create README files for each section
- [ ] Test that everything still works
- [ ] Update `.gitignore` if needed

---

## Impact Analysis

### What Changes
```
✅ Directory structure more organized
✅ Better separation of concerns
✅ Easier to find things
✅ Scales better for future additions
✅ Follows documentation standards
```

### What Stays the Same
```
✅ Code functionality (no changes)
✅ API compatibility
✅ Data integrity
✅ Configuration values
```

### What Needs Updating
```
❌ Import statements in Python files
❌ Path references in scripts
❌ Documentation path references
❌ CI/CD pipeline paths (if any)
```

---

## Testing After Reorganization

```bash
# 1. Verify directory structure
find . -maxdepth 2 -type d | sort

# 2. Verify file counts
find brokers -type f | wc -l
find integrations -type f | wc -l
find data -type f | wc -l

# 3. Test imports (for each module)
python -c "import algotrendy"
python -c "from brokers.composer import integration"

# 4. Verify configuration loading
python -c "import json; json.load(open('config/integrations/composer.json'))"

# 5. Check file integrity
find . -type f -exec file {} \; | grep -i error
```

---

## Updated Documentation

After reorganization, update:

1. **STRUCTURE.md** - Update all paths
2. **README.md** - Update directory guide
3. **docs/ai-agents/** - Update path references
4. **docs/reference/** - Update file index
5. **Each directory** - Add README.md explaining contents

---

## Timeline

| Phase | Tasks | Time | Status |
|-------|-------|------|--------|
| 1 | Create directories | 5 min | ⏳ READY |
| 2 | Move files | 10 min | ⏳ READY |
| 3 | Copy data | 15 min | ⏳ READY |
| 4 | Update imports | 30 min | ⏳ READY |
| 5 | Update docs | 20 min | ⏳ READY |
| 6 | Test & verify | 20 min | ⏳ READY |

**Total**: ~1 hour for complete reorganization

---

## Rollback Plan

If anything goes wrong:

```bash
# All reorganization can be undone via git
git status                    # See what changed
git diff                      # See changes
git checkout -- .             # Restore original state

# Or restore from backup
cp -r /backup/algotrendy_v2.5_backup/* .
```

---

**Status**: Ready for execution  
**Approval Required**: Before proceeding with Phase 1

