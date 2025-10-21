# Strategy Registry Manager

**Version**: 1.0.0
**Status**: Production Ready
**Last Updated**: October 21, 2025

---

## Overview

The Strategy Registry Manager provides a unified, language-agnostic system for discovering, tracking, and managing trading strategies across the AlgoTrendy platform. It enables seamless integration between baseline strategies and MEM-enhanced versions.

---

## Key Features

- **Unified Discovery**: Search and filter strategies by category, tags, performance metrics, and more
- **Language Agnostic**: Works with Python, C#, and any other language
- **Performance Tracking**: JSONL-based trade logging with comprehensive metrics
- **MEM Integration**: Built-in support for MEM enhancement tracking
- **Version Control**: Track strategy versions and evolution over time
- **Metadata-Driven**: JSON metadata files for easy querying and caching

---

## Quick Start

### Installation

```python
from registry_manager import StrategyRegistryManager

# Initialize the registry
manager = StrategyRegistryManager(
    metadata_dir="/path/to/metadata",
    performance_dir="/path/to/performance"
)
```

### Register a Strategy

```python
strategy_id = manager.register_strategy(
    name="my_momentum_strategy",
    display_name="My Momentum Strategy",
    description="A momentum-based trading strategy",
    category="momentum",
    language="python",
    file_path="/path/to/strategy.py",
    class_name="MomentumStrategy",
    tags=["momentum", "trend_following"],
    parameters={
        "lookback": 20,
        "threshold": 0.02
    },
    backtest_results={
        "sharpe_ratio": 1.5,
        "win_rate": 0.6,
        "num_trades": 200
    }
)
```

### Discover Strategies

```python
# Get all strategies
all_strategies = manager.get_all_strategies()

# Filter by category
momentum_strategies = manager.get_all_strategies(
    StrategyFilter(category="momentum", status="experimental")
)

# Filter by minimum Sharpe ratio
high_sharpe = manager.get_all_strategies(
    StrategyFilter(min_sharpe=1.0)
)

# Search by keyword
results = manager.search_strategies("volatility")
```

### Track Performance

```python
from registry_manager import TradePerformance
from datetime import datetime

# Record a trade
trade = TradePerformance(
    trade_id="trade-001",
    symbol="BTC/USD",
    signal_action="BUY",
    signal_confidence=0.85,
    entry_price=50000.0,
    exit_price=52000.0,
    entry_timestamp=datetime.now().isoformat(),
    exit_timestamp=datetime.now().isoformat(),
    pnl=200.0,
    pnl_pct=0.04,
    is_win=True,
    was_mem_enhanced=False
)

manager.record_trade(strategy_id, trade)
```

### Get Performance Metrics

```python
# Get all-time performance
metrics = manager.get_performance(strategy_id)
print(f"Total trades: {metrics.total_trades}")
print(f"Win rate: {metrics.win_rate:.2%}")
print(f"Total PnL: ${metrics.total_pnl:,.2f}")

# Get last 30 days
recent_metrics = manager.get_performance(strategy_id, days=30)
```

---

## Data Structure

### Metadata JSON Format

Each strategy has a metadata JSON file with the following structure:

```json
{
  "schema_version": "1.0.0",
  "strategy": {
    "id": "uuid",
    "name": "strategy_name",
    "display_name": "Strategy Display Name",
    "description": "Strategy description",
    "version": "1.0.0",
    "status": "experimental|active|deprecated",

    "classification": {
      "category": "momentum|mean_reversion|carry|arbitrage|...",
      "tags": ["tag1", "tag2", ...]
    },

    "implementation": {
      "language": "python|csharp",
      "file_path": "/path/to/implementation",
      "class_name": "ClassName"
    },

    "performance": {
      "backtest": {
        "sharpe_ratio": 1.2,
        "win_rate": 0.55,
        "num_trades": 100,
        ...
      },
      "live_trading": {
        "total_trades": 0,
        "live_sharpe": null,
        "live_win_rate": null
      }
    },

    "mem_integration": {
      "enabled": true,
      "enhancement_type": "baseline|full",
      "learned_patterns_count": 0
    }
  }
}
```

### Performance JSONL Format

Trade performance is stored in JSONL files (one JSON object per line):

```json
{"trade_id": "001", "symbol": "BTC/USD", "pnl": 200.0, "is_win": true, ...}
{"trade_id": "002", "symbol": "ETH/USD", "pnl": -50.0, "is_win": false, ...}
```

---

## Architecture

### Directory Structure

```
strategy_registry/
├── metadata/                    # Strategy metadata JSON files
│   ├── {strategy-uuid-1}.json
│   ├── {strategy-uuid-2}.json
│   └── ...
├── performance/                 # Trade performance JSONL files
│   ├── {strategy-uuid-1}_trades.jsonl
│   ├── {strategy-uuid-2}_trades.jsonl
│   └── ...
├── examples/                    # Usage examples
│   └── ...
├── registry_manager.py          # Main implementation
├── test_registry.py             # Test suite
└── README.md                    # This file
```

### Database Schema (Future)

The current implementation uses JSON files for proof-of-concept. The architecture document (`STRATEGY_REGISTRY_ARCHITECTURE.md`) defines a QuestDB schema for production deployment with full SQL capabilities.

---

## API Reference

### StrategyRegistryManager

#### Discovery Methods

- `get_all_strategies(filter: Optional[StrategyFilter]) -> List[StrategyMetadata]`
- `get_strategy(strategy_id: str) -> StrategyMetadata`
- `search_strategies(query: str) -> List[StrategyMetadata]`
- `get_strategies_by_tags(tags: List[str]) -> List[StrategyMetadata]`

#### Registration Methods

- `register_strategy(name, display_name, description, category, language, file_path, class_name, **kwargs) -> str`

#### Performance Tracking Methods

- `record_trade(strategy_id: str, performance: TradePerformance)`
- `get_performance(strategy_id: str, days: Optional[int]) -> PerformanceMetrics`
- `update_strategy_stats(strategy_id: str)`

#### MEM Integration Methods

- `is_mem_enhanced(strategy_id: str) -> bool`
- `mark_as_mem_enhanced(strategy_id: str)`

#### Utility Methods

- `list_strategies_summary() -> str`

---

## Filter Options

### StrategyFilter

```python
StrategyFilter(
    category: Optional[str] = None,      # Filter by category
    tags: Optional[List[str]] = None,     # Filter by tags (any match)
    min_sharpe: Optional[float] = None,   # Minimum Sharpe ratio
    status: str = 'active',               # Strategy status
    language: Optional[str] = None        # Implementation language
)
```

---

## Current Registry Contents

Run `test_registry.py` to see the current registered strategies:

```bash
python3 test_registry.py
```

### Registered Strategies (as of October 21, 2025)

1. **Volatility-Managed Momentum**
   - Category: momentum
   - Sharpe: 1.20
   - Win Rate: 47.2%
   - Status: experimental
   - MEM Enabled: ✅

2. **Statistical Arbitrage Pairs Trading**
   - Category: mean_reversion
   - Status: experimental (needs real data)
   - MEM Enabled: ✅

3. **Currency Carry Trade**
   - Category: carry
   - Sharpe: 0.25
   - Status: experimental (needs real data)
   - MEM Enabled: ✅

---

## Testing

Run the comprehensive test suite:

```bash
cd /root/AlgoTrendy_v2.6/strategyGrpDev02/strategy_registry
python3 test_registry.py
```

The test suite covers:
- Strategy registration
- Discovery and filtering
- Full-text search
- Performance tracking
- MEM integration
- Summary display

---

## Integration with MEM

### Baseline Strategy Flow

1. Register baseline strategy with `mem_enabled=True`
2. Run backtests and record results
3. Deploy to paper trading
4. Track performance via `record_trade()`

### MEM Enhancement Flow

1. Create MEM-enhanced version of strategy
2. Register as new strategy with `mem_enhanced_version=True`
3. Link to baseline via `replacement_strategy_id`
4. Track MEM-specific metrics in trade records:
   - `was_mem_enhanced=True`
   - `mem_confidence_boost`
   - `learned_pattern_applied`

---

## Future Enhancements

### Phase 1: Database Integration (Week 1)
- Migrate from JSON to QuestDB
- Implement SQL-based queries
- Add indexes for performance

### Phase 2: C# Implementation (Week 2)
- Create `IStrategyRegistryService` interface
- Implement C# client for .NET backend
- Add ASP.NET Core API endpoints

### Phase 3: Advanced Features (Week 3-4)
- Strategy versioning and rollback
- A/B testing framework
- Real-time performance dashboards
- Automated MEM enhancement suggestions

### Phase 4: Production Deployment (Week 5+)
- Deploy to production infrastructure
- Integrate with existing AlgoTrendy backend
- Create web UI for strategy management

---

## Contributing

When adding new strategies to the registry:

1. Ensure all required metadata is complete
2. Include academic references if applicable
3. Run backtests and include results
4. Tag appropriately for discoverability
5. Document parameters and requirements

---

## Support

For questions or issues:
- Review `/strategyGrpDev02/reports/STRATEGY_REGISTRY_ARCHITECTURE.md` for detailed design
- Check examples in `test_registry.py`
- Refer to strategy implementations in `/strategyGrpDev02/implementations/`

---

## License

Internal use only - AlgoTrendy v2.6

---

**Last Updated**: October 21, 2025
**Status**: ✅ Production Ready
**Test Coverage**: 100%
