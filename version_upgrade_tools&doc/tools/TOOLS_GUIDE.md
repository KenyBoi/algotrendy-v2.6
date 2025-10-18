# Version Upgrade Tools Guide

**Purpose:** How to use each analysis tool for upgrades
**Tools Available:** 4 comprehensive analysis utilities
**Date:** October 18, 2025

---

## Quick Reference

| Tool | Purpose | Time | Output |
|------|---------|------|--------|
| `code_migration_analyzer.py` | Find missing code between versions | 2-3 min | JSON + Text |
| `duplication_checker.py` | Find duplicate code patterns | 3-5 min | JSON + HTML + Text |
| `optimization_analyzer.py` | Find optimization opportunities | 1-2 min | Text report |
| `project_maintenance.py` | General project health | 2-3 min | Text report |

---

## Tool 1: code_migration_analyzer.py

**Purpose:** Identify what valuable code from old version is missing in new version

### Quick Start

```bash
cd /root/AlgoTrendy_v[new]/version_upgrade_tools\&doc/tools

python3 code_migration_analyzer.py \
  /root/algotrendy_v2.5 \
  /root/AlgoTrendy_v2.6 \
  --json
```

### Output

**Console:**
```
Migration Completeness: 2.9%
  v2.5 patterns found: 2408
  v2.6 patterns found: 69
  Potentially missing files: 411

Category: STRATEGIES
   v2.5: 33 patterns in 16 files
   v2.6: 0 patterns in 0 files
   Coverage: 0.0% ⚠️
   Missing files:
      - data/knowledge/strategies/smart_money_strategy.py
      - algotrendy/strategy_resolver.py
      ...
```

**File:** `migration_report.json`
```json
{
  "timestamp": "2025-10-18",
  "v25_total_files": 1226,
  "v26_total_files": 120,
  "categories": {
    "strategies": {
      "v25_count": 33,
      "v26_count": 0,
      "coverage": "0%",
      "missing_files": [...]
    },
    ...
  }
}
```

### What It Checks

Searches for these code patterns:
- **Strategies** - `class.*Strategy`, `generate_signal`, `strategy_config`
- **Data Channels** - `class.*Channel`, `async def fetch`, `rate_limit`
- **Brokers** - `class.*Broker`, `place_order`, `cancel_order`
- **Backtesting** - `class Backtest`, `calculate_pnl`, `performance_report`
- **Indicators** - `calculate_rsi`, `calculate_macd`, `calculate_ema`
- **Database** - `Repository`, `save`, `query`, SQL patterns
- **Risk Management** - `RiskManager`, `validate_order`, `max_position`
- **API** - API routes, endpoint definitions

### Use Cases

**Before Starting Upgrade:**
```bash
# Analyze what's in old version
python3 code_migration_analyzer.py \
  /root/algotrendy_v2.5 \
  /root/AlgoTrendy_v2.6 \
  --json

# Review migration_report.json
# Identify what features are must-have vs nice-to-have
```

**After Core Features Complete (Phase 4-5):**
```bash
# Check what still needs to be ported
python3 code_migration_analyzer.py \
  /root/algotrendy_v2.5 \
  /root/AlgoTrendy_v2.6 \
  --json

# Read report, identify gaps
# Plan Phase 7 enhancements based on coverage
```

---

## Tool 2: duplication_checker.py

**Purpose:** Find duplicate code patterns (refactoring opportunities)

### Quick Start

```bash
cd /root/AlgoTrendy_v[new]/version_upgrade_tools\&doc/tools

# Find strict duplicates (70%+ similar)
python3 duplication_checker.py \
  /root/algotrendy_v2.5 \
  --threshold 70 \
  --text-report

# Generate all report formats
python3 duplication_checker.py \
  /root/algotrendy_v2.5 \
  --all-reports
```

### Output

**Text Report:**
```
CODE DUPLICATION ANALYSIS REPORT
================================================================================
Files Analyzed: 1249
Total Lines: 198262
Duplicates Found: 22015
Threshold: 70%
Min Block Size: 3 lines

[Block 1] - Similarity: 100.0%
Occurrences: 84
  - ./integrations/strategies/file1.py:45
  - ./integrations/strategies/file2.py:89
  - ./integrations/strategies/file3.py:45
Code:
  | def place_order(order):
  | if order.size > MAX_SIZE:
  | return False
  | ...
```

**JSON Report:** `duplication_report.json`
```json
{
  "files_analyzed": 1249,
  "total_lines": 198262,
  "duplicates_found": 22015,
  "duplicate_blocks": [
    {
      "hash": "12345...",
      "block_size": 5,
      "occurrences": [
        {"file": "file1.py", "line": 45},
        {"file": "file2.py", "line": 89}
      ],
      "similarity_score": 100.0
    }
  ]
}
```

**HTML Report:** `duplication_report.html`
- Interactive visualization
- Click to expand code blocks
- Color-coded by severity

### Use Cases

**Find Refactoring Opportunities:**
```bash
# Before starting v[new], analyze v[old]
python3 duplication_checker.py /root/algotrendy_v2.5 --threshold 80

# Review report
# Identify patterns to consolidate in v[new]
# Example: 84 copies of same config pattern → consolidate into 1
```

**Quality Check After Upgrade:**
```bash
# Check if v[new] has new duplicates introduced
python3 duplication_checker.py /root/AlgoTrendy_v2.6

# Should have fewer duplicates than v2.5 (cleaner code)
# If more duplicates, refactor before production
```

---

## Tool 3: optimization_analyzer.py

**Purpose:** Find optimization opportunities and technical debt

### Quick Start

```bash
cd /root/AlgoTrendy_v[new]/version_upgrade_tools\&doc/tools

python3 optimization_analyzer.py /root/algotrendy_v2.5
```

### Output

```
OPTIMIZATION ANALYSIS REPORT
================================================================================

📊 Summary:
Files Analyzed: 350
Total Lines: 45000
Issues Found: 127

Severity Breakdown:
Critical: 3
High: 15
Medium: 42
Low: 67

============================================================================
CRITICAL ISSUES (Must Fix)

[CRITICAL] Cyclomatic Complexity
File: algotrendy/unified_trader.py:156
Function: place_order (complexity: 28)
Issue: Function is too complex
Suggestion: Break into smaller functions

[CRITICAL] Performance Bottleneck
File: algotrendy/data_channels/manager.py:78
Issue: Nested loops without optimization
Suggestion: Use list comprehension or generator
```

### What It Checks

- **Cyclomatic Complexity** - Functions with >10 branches (too complex)
- **Performance Bottlenecks** - Nested loops, repeated calculations
- **Security Issues** - Hardcoded secrets, SQL injection risks
- **Dead Code** - Unused imports, unreachable code
- **Memory Leaks** - Resource handles not closed

### Use Cases

**Before Upgrade (Understand Technical Debt):**
```bash
python3 optimization_analyzer.py /root/algotrendy_v2.5

# Review critical issues
# Decide: Port exactly (risks) vs refactor (opportunities)
```

**After Upgrade (Avoid Repeating Mistakes):**
```bash
python3 optimization_analyzer.py /root/AlgoTrendy_v2.6

# Should have fewer issues than v2.5
# If more issues introduced, fix before production
```

---

## Tool 4: project_maintenance.py

**Purpose:** General project health analysis

### Quick Start

```bash
cd /root/AlgoTrendy_v[new]/version_upgrade_tools\&doc/tools

# Run full analysis
python3 project_maintenance.py /root/algotrendy_v2.5

# Check duplicates only
python3 project_maintenance.py /root/algotrendy_v2.5 --check-duplicates

# Analyze file organization only
python3 project_maintenance.py /root/algotrendy_v2.5 --analyze-files
```

### Output

```
PROJECT MAINTENANCE REPORT
================================================================================

🔍 Code Quality Analysis
Total Python Files: 350
Average Lines per File: 128
Files >500 lines: 12 (too large, refactor)
Files <10 lines: 8 (too small, consolidate)

🔄 Duplication Analysis
Duplicate Blocks Found: 127
Most Duplicated: place_order logic (84 copies)
Refactoring Opportunity: High

📁 File Organization
Well-Organized Directories: 8
Disorganized: 3
Recommended Structure: [suggestions]

📊 Testing Coverage
Test Files: 45
Tests per Module: Average 8
Untested Modules: 5 [list]
```

### Use Cases

**Pre-Upgrade Assessment:**
```bash
python3 project_maintenance.py /root/algotrendy_v2.5

# Understand:
# - How well-organized is the old codebase?
# - What are the biggest duplication issues?
# - Which modules lack tests?
```

---

## Running All Tools (Comprehensive Analysis)

### Setup Script

```bash
#!/bin/bash
# run_all_upgrade_tools.sh

OLD_VERSION=$1
NEW_VERSION=$2
TOOLS_DIR="$(dirname "$0")"

echo "🔍 Running comprehensive upgrade analysis..."
echo "Old version: $OLD_VERSION"
echo "New version: $NEW_VERSION"
echo ""

# 1. Migration analysis
echo "1️⃣  Analyzing migration gaps..."
python3 $TOOLS_DIR/code_migration_analyzer.py $OLD_VERSION $NEW_VERSION --json

# 2. Duplication check
echo "2️⃣  Checking for code duplication..."
python3 $TOOLS_DIR/duplication_checker.py $OLD_VERSION --all-reports

# 3. Optimization opportunities
echo "3️⃣  Finding optimization opportunities..."
python3 $TOOLS_DIR/optimization_analyzer.py $OLD_VERSION > optimization_report.txt

# 4. Project health
echo "4️⃣  Analyzing project health..."
python3 $TOOLS_DIR/project_maintenance.py $OLD_VERSION > maintenance_report.txt

echo ""
echo "✅ Analysis complete!"
echo ""
echo "Generated reports:"
echo "- migration_report.json"
echo "- duplication_report.json + duplication_report.html"
echo "- optimization_report.txt"
echo "- maintenance_report.txt"
```

### Run It

```bash
bash run_all_upgrade_tools.sh /root/algotrendy_v2.5 /root/AlgoTrendy_v2.6

# Wait 10-15 minutes for all analyses to complete
# Review all reports
# Take action on findings
```

---

## Interpreting Results

### Migration Report (code_migration_analyzer.py)

**High Coverage (80%+):** ✅ Feature parity achieved
```json
"database": {
  "v25_count": 57,
  "v26_count": 23,
  "coverage": "40%"  // Still low, but acceptable for phase
}
```

**Low Coverage (<30%):** ⚠️ Significant work remains
```json
"strategies": {
  "v25_count": 33,
  "v26_count": 0,
  "coverage": "0%"  // Critical gap, needs attention
}
```

**Action:** Prioritize low-coverage areas for Phase 7

---

### Duplication Report (duplication_checker.py)

**Good (v2.6 < v2.5):** ✅ Code cleaner after upgrade
```
v2.5: 22,015 duplicate blocks
v2.6: 5,000 duplicate blocks
Improvement: 77% reduction in duplication ✅
```

**Bad (v2.6 > v2.5):** ⚠️ Introduced technical debt
```
v2.5: 10,000 duplicate blocks
v2.6: 12,000 duplicate blocks
Regression: 20% increase in duplication ⚠️
Action: Refactor before production
```

---

### Optimization Report (optimization_analyzer.py)

**Critical Issues:** 🔴 Fix before production
```
Critical: 0 issues ✅
High: 2 issues ⚠️ (review carefully)
```

**Regression:** 🟡 Monitor and plan
```
v2.5 Critical: 5 issues
v2.6 Critical: 2 issues
Improvement: Reduced by 60% ✅
```

---

## Schedule for Running Tools

### Phase 0: Planning
- [ ] Run all tools on v[old]
- [ ] Review reports
- [ ] Document findings

### Phase 4-5: Development
- [ ] Run migration analyzer weekly
- [ ] Track coverage growth
- [ ] Identify remaining gaps

### Phase 6: Quality
- [ ] Run all tools on v[new]
- [ ] Compare to v[old]
- [ ] Fix regressions

### Phase 7+: Enhancements
- [ ] Use reports to prioritize work
- [ ] Track improvements over time

---

## Troubleshooting

### Tool Errors

**Error: "ModuleNotFoundError: No module named 'pathlib'"**
```bash
# Update Python (need 3.8+)
python3 --version
```

**Error: "Permission denied"**
```bash
# Make executable
chmod +x code_migration_analyzer.py
```

**Error: "No such file or directory"**
```bash
# Check paths exist
ls /root/algotrendy_v2.5
ls /root/AlgoTrendy_v2.6
```

### Slow Performance

**Taking >10 minutes:**
```bash
# Use smaller scope
python3 code_migration_analyzer.py ./src ./src-new  # Local dirs only
```

**Out of memory:**
```bash
# Run on server with more RAM
# Or analyze one module at a time
```

---

## Adding New Tools

When developing new analysis tools:

1. Place in `tools/` directory
2. Add to this guide with:
   - Purpose (one sentence)
   - Quick start (copy-paste ready)
   - Output description
   - Use cases (3-4 examples)
3. Update main README.md

---

**Document Version:** 1.0
**Created:** October 18, 2025
**Tools Version:** Stable
