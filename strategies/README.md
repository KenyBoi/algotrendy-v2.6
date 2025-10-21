# AlgoTrendy Strategies

**Version**: 2.6
**Last Updated**: October 21, 2025

---

## Overview

This directory contains all trading strategies for the AlgoTrendy platform, organized by lifecycle stage following computer science best practices.

---

## Directory Structure

```
strategies/
├── production/          # Live trading strategies (real money)
│   ├── python/
│   └── csharp/
│
├── backtested/          # Validated strategies ready for paper trading
│   └── (pending)
│
├── development/         # Experimental and research strategies
│   └── strategy_research_2025_q4/
│       ├── strategy_1_volatility_managed_momentum/
│       ├── strategy_2_pairs_trading/
│       ├── strategy_3_carry_trade/
│       └── reports/
│
└── archive/             # Deprecated strategies
    └── (empty)
```

---

## Lifecycle Stages

### 1. Development

**Location**: `/strategies/development/`

Experimental strategies under research and development. Includes:
- Initial implementations
- Backtesting with synthetic/historical data
- Parameter optimization
- Documentation and analysis

**Criteria to move forward**: Successful backtest results matching or exceeding published benchmarks.

---

### 2. Backtested

**Location**: `/strategies/backtested/`

Strategies that have passed rigorous backtesting and are ready for paper trading validation.

**Criteria to move forward**: 100+ successful paper trades with performance matching backtests.

---

### 3. Production

**Location**: `/strategies/production/`

Live trading strategies deployed with real capital. Fully validated through:
- Backtesting
- Paper trading
- Risk management review
- MEM integration (where applicable)

**Organized by language**: `python/` and `csharp/` subfolders.

---

### 4. Archive

**Location**: `/strategies/archive/`

Deprecated or replaced strategies kept for reference and analysis.

---

## Current Projects

### Strategy Research 2025 Q4

**Status**: Phase 1 Complete
**Location**: `/development/strategy_research_2025_q4/`

Three academically-validated strategies:
1. ✅ **Volatility-Managed Momentum** - Sharpe 1.20 (exceeds published 0.97)
2. ⏳ **Statistical Arbitrage Pairs Trading** - Needs real data
3. ⏳ **Currency Carry Trade** - Needs real data

See [PROJECT_SUMMARY.md](./development/strategy_research_2025_q4/reports/PROJECT_SUMMARY.md) for full details.

---

## Strategy Registry

All strategies (development, backtested, production) are tracked in the centralized **Strategy Registry**.

**Location**: `/backend/AlgoTrendy.Core/Services/StrategyRegistry/`
**Data**: `/data/strategy_registry/`

### Features

- Unified discovery across all strategies
- Performance tracking per trade
- MEM integration hooks
- Version control
- Tag-based search

### Quick Start

```python
from registry_manager import StrategyRegistryManager, StrategyFilter

manager = StrategyRegistryManager(
    metadata_dir="/root/AlgoTrendy_v2.6/data/strategy_registry/metadata",
    performance_dir="/root/AlgoTrendy_v2.6/data/strategy_registry/performance"
)

# Find high-performing momentum strategies
strategies = manager.get_all_strategies(
    StrategyFilter(category="momentum", min_sharpe=1.0)
)
```

See `/backend/AlgoTrendy.Core/Services/StrategyRegistry/README.md` for full documentation.

---

## Naming Conventions

### Project Folders
Format: `{description}_{year}_q{quarter}`

Examples:
- `strategy_research_2025_q4`
- `ml_enhancement_2025_q1`
- `arbitrage_study_2025_q2`

### Strategy Folders
Format: `strategy_{number}_{descriptive_name}`

Examples:
- `strategy_1_volatility_managed_momentum`
- `strategy_2_pairs_trading`
- `strategy_3_carry_trade`

### Documentation Files
Format: `STRATEGY_{number}_{TYPE}.md`

Examples:
- `STRATEGY_1_FOUNDATIONS.md`
- `STRATEGY_1_BACKTEST_RESULTS.md`
- `PROJECT_SUMMARY.md`

---

## Best Practices

### When Adding New Strategies

1. **Start in development**: Create in `/development/{project_name}/`
2. **Use individual folders**: Each strategy gets its own directory
3. **Document thoroughly**: Include foundations, parameters, and results
4. **Register immediately**: Add to strategy registry
5. **Track performance**: Use registry to log all trades

### Promotion Path

```
development/
  └── my_strategy/
      ├── implementation.py
      ├── backtest.py
      └── results/

      ↓ Backtest successful

backtested/
  └── my_strategy_v1/
      └── (same files)

      ↓ Paper trading validated (100+ trades)

production/
  └── python/
      └── my_strategy_v1.py

      ↓ MEM enhancement

/MEM/enhanced_strategies/
  └── my_strategy_mem_v1.py
```

### Version Control

- **v1, v2, v3**: Major versions with different logic
- **v1.1, v1.2**: Parameter tuning or minor improvements
- **Date suffixes**: `_2025_10` for time-based snapshots

---

## Integration with MEM

MEM-enhanced strategies are stored separately in `/MEM/enhanced_strategies/` but registered in the same unified registry system.

**Link strategies** using the `replacement_strategy_id` field in metadata to track baseline → MEM evolution.

---

## Registry Data Location

- **Metadata**: `/data/strategy_registry/metadata/` (JSON files)
- **Performance**: `/data/strategy_registry/performance/` (JSONL files)

This centralized location ensures:
- All strategies (dev, backtest, prod, MEM) share the same registry
- Easy backup and version control
- Separation of code and data

---

## Quick Links

- [Strategy Research 2025 Q4 Summary](./development/strategy_research_2025_q4/reports/PROJECT_SUMMARY.md)
- [Strategy Registry Documentation](/backend/AlgoTrendy.Core/Services/StrategyRegistry/README.md)
- [Registry Architecture](/strategies/development/strategy_research_2025_q4/reports/STRATEGY_REGISTRY_ARCHITECTURE.md)

---

## Support

For questions about strategy development:
1. Review existing documentation in project `reports/` folders
2. Check Strategy Registry README
3. Refer to academic papers cited in foundations docs

---

**Last Updated**: October 21, 2025
**Maintained by**: AlgoTrendy Development Team
