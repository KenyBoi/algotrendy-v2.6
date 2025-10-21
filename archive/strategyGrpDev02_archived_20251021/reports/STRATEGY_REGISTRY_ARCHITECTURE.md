# Strategy Registry & Library Architecture Design

**Created**: October 21, 2025
**Purpose**: Comprehensive strategy storage, tracking, and MEM integration system
**Status**: Design Complete - Ready for Implementation

---

## Executive Summary

This document designs a **comprehensive strategy registry and library system** that:

✅ **Unifies** C# and Python strategies under one tracking system
✅ **Integrates** seamlessly with MEM for AI enhancement
✅ **Tracks** performance, metadata, and version history
✅ **Enables** rapid strategy discovery and connection
✅ **Supports** learned strategies and auto-generated algorithms
✅ **Provides** audit trail and compliance tracking

---

## Current State Analysis

### Strategy Storage Today (Problems)

**C# Strategies** (Backend):
```
/backend/AlgoTrendy.TradingEngine/Strategies/
├── MomentumStrategy.cs
├── RSIStrategy.cs
├── MACDStrategy.cs
├── MFIStrategy.cs
└── VWAPStrategy.cs
```
❌ **Problems**:
- No centralized registry
- No metadata tracking
- No version control
- Hard to discover what strategies exist
- No performance metrics storage
- No relationship to MEM learned strategies

**Python Strategies** (StrategyGrpDev02):
```
/strategyGrpDev02/implementations/
├── strategy1_vol_managed_momentum.py
├── strategy2_pairs_trading.py
└── strategy3_carry_trade.py
```
❌ **Problems**:
- Completely separate from C# strategies
- No integration with trading engine
- No unified tracking
- Results stored separately

**MEM Learned Strategies**:
```
/MEM/data/mem_knowledge/strategy_modules.py
```
❌ **Problems**:
- Auto-generated but no formal registry
- No versioning or deprecation tracking
- Performance metrics in separate file
- Hard to connect to trading engine

### MEM Integration Today

**Current Flow**:
```
Base Strategy (C#) → ML Prediction → MemGPT Enhancement → Execute
```

**Missing**:
- No formal connection between strategies and MEM
- No strategy metadata for MEM to use
- No tracking of which strategies MEM has enhanced
- No performance comparison (before/after MEM)

---

## Design Principles

1. **Language Agnostic** - Support C#, Python, and future languages
2. **Metadata-Driven** - Rich metadata for discovery and filtering
3. **Version Controlled** - Track strategy evolution over time
4. **Performance-Tracked** - Automatic logging of all results
5. **MEM-Integrated** - Seamless AI enhancement workflow
6. **Discoverable** - Easy to find strategies by type, performance, tags
7. **Audit-Ready** - Complete history for compliance
8. **Extensible** - Easy to add new strategy types

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                  STRATEGY REGISTRY DATABASE                      │
│                     (QuestDB + JSON Files)                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │   Strategies    │  │    Versions     │  │   Performance   │ │
│  │   (Metadata)    │  │   (History)     │  │   (Metrics)     │ │
│  └────────┬────────┘  └────────┬────────┘  └────────┬────────┘ │
│           │                    │                     │          │
│           └────────────────────┼─────────────────────┘          │
│                                │                                │
└────────────────────────────────┼────────────────────────────────┘
                                 │
                ┌────────────────┴────────────────┐
                │                                  │
        ┌───────▼────────┐              ┌────────▼────────┐
        │   C# Registry   │              │ Python Registry  │
        │    Manager      │              │     Manager      │
        └───────┬────────┘              └────────┬────────┘
                │                                  │
        ┌───────▼────────┐              ┌────────▼────────┐
        │  C# Strategies  │              │Python Strategies │
        │  (IStrategy)    │              │  (BaseStrategy)  │
        └───────┬────────┘              └────────┬────────┘
                │                                  │
                └──────────────┬───────────────────┘
                               │
                    ┌──────────▼────────────┐
                    │   MEM Integration     │
                    │   - Signal Enhancement│
                    │   - Learning Loop     │
                    │   - Performance Track │
                    └───────────────────────┘
```

---

## Database Schema Design

### Table 1: `strategies`

**Purpose**: Master registry of all strategies

```sql
CREATE TABLE strategies (
    -- Primary Key
    strategy_id UUID PRIMARY KEY,

    -- Identity
    strategy_name VARCHAR(100) NOT NULL,
    display_name VARCHAR(200) NOT NULL,
    description TEXT,

    -- Classification
    category VARCHAR(50),  -- 'momentum', 'mean_reversion', 'arbitrage', etc.
    sub_category VARCHAR(50),
    complexity_level VARCHAR(20),  -- 'simple', 'moderate', 'complex'

    -- Implementation
    language VARCHAR(20),  -- 'csharp', 'python', 'multi'
    file_path VARCHAR(500),
    class_name VARCHAR(100),
    namespace VARCHAR(200),

    -- Origin & Attribution
    origin VARCHAR(50),  -- 'built_in', 'learned', 'imported', 'research'
    academic_source TEXT,  -- Reference to papers
    learned_from_strategy_id UUID,  -- If this was derived from another
    created_by VARCHAR(100),

    -- Status
    status VARCHAR(20),  -- 'active', 'deprecated', 'experimental', 'archived'
    is_mem_enhanced BOOLEAN DEFAULT FALSE,
    requires_short_selling BOOLEAN DEFAULT FALSE,
    requires_margin BOOLEAN DEFAULT FALSE,

    -- Tags & Search
    tags TEXT[],  -- ['high_frequency', 'crypto', 'forex', etc.]
    search_keywords TEXT,

    -- Versioning
    current_version VARCHAR(20),
    version_count INTEGER DEFAULT 1,

    -- Performance Summary (cached)
    lifetime_sharpe_ratio DECIMAL(10,4),
    lifetime_win_rate DECIMAL(5,4),
    total_trades BIGINT DEFAULT 0,
    total_pnl DECIMAL(20,2),

    -- MEM Integration
    mem_learning_enabled BOOLEAN DEFAULT FALSE,
    mem_learned_patterns_count INTEGER DEFAULT 0,
    last_mem_update TIMESTAMP,

    -- Metadata
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NOT NULL,
    last_used_at TIMESTAMP,
    deprecated_at TIMESTAMP,
    deprecation_reason TEXT
);

-- Indexes
CREATE INDEX idx_strategies_category ON strategies(category, status);
CREATE INDEX idx_strategies_language ON strategies(language);
CREATE INDEX idx_strategies_origin ON strategies(origin);
CREATE INDEX idx_strategies_performance ON strategies(lifetime_sharpe_ratio DESC, lifetime_win_rate DESC);
CREATE INDEX idx_strategies_tags ON strategies USING GIN(tags);
```

### Table 2: `strategy_versions`

**Purpose**: Track all versions of each strategy

```sql
CREATE TABLE strategy_versions (
    -- Primary Key
    version_id UUID PRIMARY KEY,
    strategy_id UUID REFERENCES strategies(strategy_id),

    -- Version Info
    version_number VARCHAR(20) NOT NULL,  -- '1.0.0', '1.1.0', '2.0.0'
    version_type VARCHAR(20),  -- 'major', 'minor', 'patch'

    -- Code
    code_snapshot TEXT,  -- Full code or git hash
    git_commit_hash VARCHAR(40),

    -- Changes
    changelog TEXT,
    breaking_changes TEXT,
    migration_notes TEXT,

    -- Parameters
    parameters_json JSONB,  -- All configurable parameters
    default_parameters_json JSONB,

    -- Testing
    backtest_results_json JSONB,
    validation_status VARCHAR(20),  -- 'passed', 'failed', 'pending'
    test_coverage DECIMAL(5,2),

    -- Performance
    sharpe_ratio DECIMAL(10,4),
    win_rate DECIMAL(5,4),
    max_drawdown DECIMAL(5,4),
    cagr DECIMAL(5,4),

    -- MEM Enhancements
    mem_enhanced_version BOOLEAN DEFAULT FALSE,
    mem_improvement_pct DECIMAL(5,2),  -- % improvement over base

    -- Metadata
    created_by VARCHAR(100),
    created_at TIMESTAMP NOT NULL,
    approved_by VARCHAR(100),
    approved_at TIMESTAMP,

    -- Status
    is_active BOOLEAN DEFAULT TRUE,
    deprecated_at TIMESTAMP,

    UNIQUE(strategy_id, version_number)
);

CREATE INDEX idx_versions_strategy ON strategy_versions(strategy_id, version_number DESC);
CREATE INDEX idx_versions_active ON strategy_versions(is_active, created_at DESC);
```

### Table 3: `strategy_performance`

**Purpose**: Track all trades and performance metrics

```sql
CREATE TABLE strategy_performance (
    -- Primary Key
    performance_id UUID PRIMARY KEY,
    strategy_id UUID REFERENCES strategies(strategy_id),
    version_id UUID REFERENCES strategy_versions(version_id),

    -- Trade Info
    trade_id UUID,  -- Reference to orders table
    symbol VARCHAR(20),
    timeframe VARCHAR(10),

    -- Signal
    signal_action VARCHAR(10),  -- 'buy', 'sell', 'hold'
    signal_confidence DECIMAL(5,4),
    entry_price DECIMAL(20,8),
    exit_price DECIMAL(20,8),

    -- Execution
    entry_timestamp TIMESTAMP,
    exit_timestamp TIMESTAMP,
    duration_seconds INTEGER,

    -- Results
    pnl DECIMAL(20,8),
    pnl_percentage DECIMAL(10,6),
    is_win BOOLEAN,

    -- Risk Metrics
    max_adverse_excursion DECIMAL(10,6),
    max_favorable_excursion DECIMAL(10,6),
    stop_loss DECIMAL(20,8),
    take_profit DECIMAL(20,8),

    -- MEM Enhancement
    was_mem_enhanced BOOLEAN DEFAULT FALSE,
    mem_confidence_boost DECIMAL(5,4),
    mem_reasoning TEXT,
    learned_pattern_applied VARCHAR(100),

    -- Market Conditions
    market_regime VARCHAR(50),
    volatility DECIMAL(10,6),
    volume_ratio DECIMAL(10,4),

    -- Broker
    broker VARCHAR(50),
    slippage DECIMAL(10,6),
    commission DECIMAL(20,8),

    -- Metadata
    recorded_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Indexes for fast queries
CREATE INDEX idx_perf_strategy ON strategy_performance(strategy_id, recorded_at DESC);
CREATE INDEX idx_perf_version ON strategy_performance(version_id, recorded_at DESC);
CREATE INDEX idx_perf_symbol ON strategy_performance(symbol, recorded_at DESC);
CREATE INDEX idx_perf_mem ON strategy_performance(was_mem_enhanced, recorded_at DESC);
```

### Table 4: `strategy_parameters`

**Purpose**: Track parameter changes and A/B tests

```sql
CREATE TABLE strategy_parameters (
    parameter_id UUID PRIMARY KEY,
    strategy_id UUID REFERENCES strategies(strategy_id),
    version_id UUID REFERENCES strategy_versions(version_id),

    -- Parameter
    parameter_name VARCHAR(100) NOT NULL,
    parameter_value TEXT,
    parameter_type VARCHAR(50),  -- 'integer', 'decimal', 'boolean', 'string'

    -- Context
    applied_from TIMESTAMP,
    applied_to TIMESTAMP,

    -- Reason for change
    change_reason TEXT,
    changed_by VARCHAR(100),  -- 'manual', 'mem_learning', 'backtest_optimization'

    -- Performance impact
    before_sharpe DECIMAL(10,4),
    after_sharpe DECIMAL(10,4),
    improvement_pct DECIMAL(5,2),

    -- A/B Testing
    is_ab_test BOOLEAN DEFAULT FALSE,
    ab_test_group VARCHAR(10),  -- 'A', 'B', 'C', etc.

    created_at TIMESTAMP NOT NULL
);

CREATE INDEX idx_params_strategy ON strategy_parameters(strategy_id, applied_from DESC);
```

### Table 5: `strategy_relationships`

**Purpose**: Track how strategies relate to each other

```sql
CREATE TABLE strategy_relationships (
    relationship_id UUID PRIMARY KEY,

    -- Strategies involved
    parent_strategy_id UUID REFERENCES strategies(strategy_id),
    child_strategy_id UUID REFERENCES strategies(strategy_id),

    -- Relationship type
    relationship_type VARCHAR(50),
    -- 'derived_from', 'mem_enhanced_version', 'combined_with',
    -- 'alternative_to', 'replaces', 'portfolio_component'

    -- Details
    description TEXT,
    similarity_score DECIMAL(5,4),  -- How similar are they

    -- Metadata
    created_at TIMESTAMP NOT NULL,

    CHECK (parent_strategy_id != child_strategy_id)
);

CREATE INDEX idx_relationships_parent ON strategy_relationships(parent_strategy_id);
CREATE INDEX idx_relationships_child ON strategy_relationships(child_strategy_id);
CREATE INDEX idx_relationships_type ON strategy_relationships(relationship_type);
```

---

## JSON Metadata File Structure

For rapid loading and caching, each strategy also has a JSON metadata file:

### File Location
```
/data/strategy_registry/metadata/{strategy_id}.json
```

### JSON Schema
```json
{
  "schema_version": "1.0.0",
  "strategy": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "name": "volatility_managed_momentum",
    "display_name": "Volatility-Managed Momentum",
    "version": "1.0.0",
    "status": "active",

    "classification": {
      "category": "momentum",
      "sub_category": "volatility_scaled",
      "complexity": "moderate",
      "tags": ["momentum", "volatility_scaling", "trend_following", "academic"],
      "asset_classes": ["equities", "crypto", "futures"]
    },

    "implementation": {
      "language": "python",
      "file_path": "/strategyGrpDev02/implementations/strategy1_vol_managed_momentum.py",
      "class_name": "VolatilityManagedMomentum",
      "entry_point": "VolatilityManagedMomentum.generate_signal",
      "dependencies": ["numpy", "pandas"],
      "minimum_python_version": "3.10"
    },

    "academic_foundation": {
      "papers": [
        {
          "title": "Momentum Has Its Moments",
          "authors": ["Pedro Barroso", "Pedro Santa-Clara"],
          "journal": "Journal of Financial Economics",
          "year": 2015,
          "doi": "10.1016/j.jfineco.2015.01.002"
        }
      ],
      "published_sharpe": 0.97,
      "published_test_period": "1927-2012"
    },

    "parameters": {
      "momentum_lookback": {
        "value": 252,
        "type": "integer",
        "description": "Days to lookback for momentum calculation",
        "min": 21,
        "max": 504,
        "optimizable": true
      },
      "skip_period": {
        "value": 21,
        "type": "integer",
        "description": "Days to skip to avoid reversal",
        "min": 0,
        "max": 63,
        "optimizable": true
      },
      "volatility_lookback": {
        "value": 126,
        "type": "integer",
        "description": "Days for volatility calculation",
        "min": 21,
        "max": 252,
        "optimizable": true
      },
      "target_volatility": {
        "value": 0.12,
        "type": "float",
        "description": "Target annual volatility",
        "min": 0.05,
        "max": 0.30,
        "optimizable": true
      }
    },

    "performance": {
      "backtest": {
        "start_date": "2015-01-01",
        "end_date": "2024-10-21",
        "total_return": 4.898,
        "cagr": 0.133,
        "sharpe_ratio": 1.197,
        "sortino_ratio": 1.847,
        "max_drawdown": -0.156,
        "win_rate": 0.472,
        "num_trades": 106,
        "avg_trade_duration_days": 30
      },
      "live_trading": {
        "start_date": null,
        "total_trades": 0,
        "live_sharpe": null,
        "live_win_rate": null
      }
    },

    "mem_integration": {
      "enabled": true,
      "enhancement_type": "full",  // 'none', 'partial', 'full'
      "learned_patterns_count": 0,
      "mem_version": "baseline_1.0",
      "estimated_improvement": {
        "sharpe_boost": 0.55,  // Expected +55% improvement
        "sharpe_range": [1.5, 1.8],
        "confidence": "high"
      }
    },

    "requirements": {
      "min_capital": 10000,
      "short_selling": false,
      "margin": false,
      "min_data_points": 252,
      "rebalancing_frequency": "monthly",
      "supported_brokers": ["all"],
      "supported_asset_classes": ["equities", "crypto", "futures"]
    },

    "risk_profile": {
      "risk_level": "medium",
      "volatility_sensitivity": "high",
      "drawdown_risk": "low",
      "tail_risk": "low",
      "liquidity_requirements": "moderate"
    },

    "metadata": {
      "created_by": "research_team",
      "created_at": "2025-10-21T04:18:00Z",
      "last_updated": "2025-10-21T04:25:00Z",
      "last_backtest": "2025-10-21T04:25:17Z",
      "deprecation_date": null,
      "replacement_strategy_id": null
    }
  }
}
```

---

## Strategy Registry Manager API

### C# Implementation

```csharp
// /backend/AlgoTrendy.Core/Services/StrategyRegistryService.cs

public interface IStrategyRegistryService
{
    // Discovery
    Task<IEnumerable<StrategyMetadata>> GetAllStrategiesAsync(StrategyFilter filter = null);
    Task<StrategyMetadata> GetStrategyAsync(Guid strategyId);
    Task<IEnumerable<StrategyMetadata>> SearchStrategiesAsync(string query);
    Task<IEnumerable<StrategyMetadata>> GetStrategiesByTagsAsync(string[] tags);

    // Registration
    Task<Guid> RegisterStrategyAsync(StrategyRegistration registration);
    Task UpdateStrategyAsync(Guid strategyId, StrategyUpdate update);
    Task DeprecateStrategyAsync(Guid strategyId, string reason);

    // Versioning
    Task<Guid> CreateVersionAsync(Guid strategyId, StrategyVersionCreate version);
    Task<IEnumerable<StrategyVersion>> GetVersionHistoryAsync(Guid strategyId);
    Task<StrategyVersion> GetVersionAsync(Guid versionId);

    // Performance
    Task RecordTradeAsync(Guid strategyId, TradePerformance performance);
    Task<PerformanceMetrics> GetPerformanceAsync(Guid strategyId, TimeRange range);
    Task<PerformanceComparison> ComparePerformanceAsync(Guid[] strategyIds);

    // MEM Integration
    Task<bool> IsMEMEnhancedAsync(Guid strategyId);
    Task MarkAsMEMEnhancedAsync(Guid strategyId, MEMEnhancementInfo info);
    Task<MEMLearnedPattern[]> GetLearnedPatternsAsync(Guid strategyId);

    // Parameters
    Task UpdateParameterAsync(Guid strategyId, string paramName, object value, string reason);
    Task<ParameterHistory> GetParameterHistoryAsync(Guid strategyId, string paramName);

    // Relationships
    Task LinkStrategiesAsync(Guid parentId, Guid childId, string relationshipType);
    Task<IEnumerable<StrategyMetadata>> GetRelatedStrategiesAsync(Guid strategyId);
}

// Usage Example
public class TradingEngineService
{
    private readonly IStrategyRegistryService _registry;

    public async Task ExecuteStrategyAsync(string strategyName, string symbol)
    {
        // 1. Find strategy by name
        var strategies = await _registry.SearchStrategiesAsync(strategyName);
        var strategy = strategies.FirstOrDefault(s => s.Status == "active");

        if (strategy == null)
            throw new StrategyNotFoundException(strategyName);

        // 2. Load strategy instance
        var instance = await LoadStrategyInstanceAsync(strategy);

        // 3. Check if MEM enhanced
        var isMEMEnhanced = await _registry.IsMEMEnhancedAsync(strategy.Id);

        // 4. Generate signal
        var signal = await instance.GenerateSignalAsync(symbol);

        // 5. If MEM enhanced, enhance signal
        if (isMEMEnhanced)
        {
            signal = await _memConnector.EnhanceSignalAsync(signal, strategy.Id);
        }

        // 6. Execute trade
        var trade = await ExecuteTradeAsync(signal);

        // 7. Record performance
        await _registry.RecordTradeAsync(strategy.Id, new TradePerformance
        {
            TradeId = trade.Id,
            Signal = signal,
            EntryPrice = trade.EntryPrice,
            ExitPrice = trade.ExitPrice,
            PnL = trade.PnL,
            WasMEMEnhanced = isMEMEnhanced
        });
    }
}
```

### Python Implementation

```python
# /strategyGrpDev02/strategy_registry/registry_manager.py

from typing import List, Dict, Optional
from datetime import datetime
import json

class StrategyRegistryManager:
    """
    Python interface to strategy registry
    Mirrors C# API for cross-language compatibility
    """

    def __init__(self, db_connection_string: str, metadata_dir: str):
        self.db = connect_to_questdb(db_connection_string)
        self.metadata_dir = metadata_dir
        self.cache = {}

    # Discovery
    async def get_all_strategies(self, filter: StrategyFilter = None) -> List[StrategyMetadata]:
        """Get all strategies matching filter"""
        query = "SELECT * FROM strategies WHERE status = 'active'"

        if filter:
            if filter.category:
                query += f" AND category = '{filter.category}'"
            if filter.tags:
                query += f" AND tags @> ARRAY{filter.tags}"
            if filter.min_sharpe:
                query += f" AND lifetime_sharpe_ratio >= {filter.min_sharpe}"

        results = await self.db.execute(query)
        return [StrategyMetadata.from_db(r) for r in results]

    async def get_strategy(self, strategy_id: str) -> StrategyMetadata:
        """Get strategy by ID with caching"""
        if strategy_id in self.cache:
            return self.cache[strategy_id]

        # Load from JSON metadata file
        metadata_path = f"{self.metadata_dir}/{strategy_id}.json"
        with open(metadata_path, 'r') as f:
            data = json.load(f)

        metadata = StrategyMetadata.from_json(data)
        self.cache[strategy_id] = metadata
        return metadata

    async def search_strategies(self, query: str) -> List[StrategyMetadata]:
        """Full-text search across strategies"""
        sql = """
            SELECT * FROM strategies
            WHERE search_keywords ILIKE %s
            OR display_name ILIKE %s
            OR description ILIKE %s
            ORDER BY lifetime_sharpe_ratio DESC
        """
        pattern = f"%{query}%"
        results = await self.db.execute(sql, (pattern, pattern, pattern))
        return [StrategyMetadata.from_db(r) for r in results]

    # Registration
    async def register_strategy(self, registration: StrategyRegistration) -> str:
        """Register new strategy in database and create metadata file"""
        strategy_id = generate_uuid()

        # Insert into database
        await self.db.execute("""
            INSERT INTO strategies (
                strategy_id, strategy_name, display_name, description,
                category, language, file_path, class_name, origin,
                status, created_at, updated_at
            ) VALUES (
                %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, NOW(), NOW()
            )
        """, (
            strategy_id, registration.name, registration.display_name,
            registration.description, registration.category, registration.language,
            registration.file_path, registration.class_name, registration.origin,
            'experimental'
        ))

        # Create JSON metadata file
        metadata = self.build_metadata_json(strategy_id, registration)
        metadata_path = f"{self.metadata_dir}/{strategy_id}.json"
        with open(metadata_path, 'w') as f:
            json.dump(metadata, f, indent=2)

        return strategy_id

    # Performance Tracking
    async def record_trade(self, strategy_id: str, performance: TradePerformance):
        """Record trade performance"""
        await self.db.execute("""
            INSERT INTO strategy_performance (
                performance_id, strategy_id, trade_id, symbol, signal_action,
                signal_confidence, entry_price, exit_price, entry_timestamp,
                exit_timestamp, pnl, pnl_percentage, is_win, was_mem_enhanced,
                mem_confidence_boost, recorded_at
            ) VALUES (
                %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, NOW()
            )
        """, (
            generate_uuid(), strategy_id, performance.trade_id, performance.symbol,
            performance.signal_action, performance.signal_confidence,
            performance.entry_price, performance.exit_price,
            performance.entry_timestamp, performance.exit_timestamp,
            performance.pnl, performance.pnl_pct, performance.is_win,
            performance.was_mem_enhanced, performance.mem_confidence_boost
        ))

        # Update strategy summary stats
        await self.update_strategy_stats(strategy_id)

    async def get_performance(self, strategy_id: str, time_range: TimeRange) -> PerformanceMetrics:
        """Calculate performance metrics for time range"""
        trades = await self.db.execute("""
            SELECT * FROM strategy_performance
            WHERE strategy_id = %s
            AND recorded_at BETWEEN %s AND %s
            ORDER BY recorded_at
        """, (strategy_id, time_range.start, time_range.end))

        return PerformanceMetrics.calculate(trades)

    # MEM Integration
    async def mark_as_mem_enhanced(self, strategy_id: str, info: MEMEnhancementInfo):
        """Mark strategy as MEM-enhanced"""
        await self.db.execute("""
            UPDATE strategies
            SET is_mem_enhanced = TRUE,
                mem_learning_enabled = TRUE,
                last_mem_update = NOW()
            WHERE strategy_id = %s
        """, (strategy_id,))

        # Create relationship to base strategy if derived
        if info.base_strategy_id:
            await self.link_strategies(
                info.base_strategy_id,
                strategy_id,
                'mem_enhanced_version'
            )

    async def get_learned_patterns(self, strategy_id: str) -> List[Dict]:
        """Get MEM learned patterns for strategy"""
        # Query MEM knowledge base
        patterns_file = f"{MEM_DIR}/data/mem_knowledge/learned_patterns_{strategy_id}.json"
        if os.path.exists(patterns_file):
            with open(patterns_file, 'r') as f:
                return json.load(f)
        return []


# Usage Example
async def main():
    registry = StrategyRegistryManager(
        db_connection_string="postgresql://localhost:8812/qdb",
        metadata_dir="/data/strategy_registry/metadata"
    )

    # Register new strategy
    strategy_id = await registry.register_strategy(StrategyRegistration(
        name="volatility_managed_momentum",
        display_name="Volatility-Managed Momentum",
        description="Scales momentum positions by volatility",
        category="momentum",
        language="python",
        file_path="/strategyGrpDev02/implementations/strategy1_vol_managed_momentum.py",
        class_name="VolatilityManagedMomentum",
        origin="research",
        tags=["momentum", "volatility_scaling", "academic"]
    ))

    # Find high-performing strategies
    top_strategies = await registry.get_all_strategies(StrategyFilter(
        min_sharpe=1.0,
        category="momentum",
        tags=["academic"]
    ))

    # Get performance
    performance = await registry.get_performance(
        strategy_id,
        TimeRange(start="2024-01-01", end="2024-10-21")
    )

    print(f"Sharpe: {performance.sharpe_ratio:.2f}")
    print(f"Win Rate: {performance.win_rate:.1%}")
```

---

## MEM Integration Layer

### MEM-Enhanced Strategy Workflow

```python
# /MEM/strategy_enhancement/mem_strategy_connector.py

class MEMStrategyConnector:
    """
    Connects MEM AI system with Strategy Registry
    Automatically enhances signals and tracks learning
    """

    def __init__(self, registry: StrategyRegistryManager, mem_agent: MemGPTAgent):
        self.registry = registry
        self.mem = mem_agent
        self.enhancement_cache = {}

    async def enhance_strategy_signal(
        self,
        strategy_id: str,
        base_signal: Dict,
        market_context: Dict
    ) -> Dict:
        """
        Enhance a strategy signal with MEM intelligence

        Flow:
        1. Load strategy metadata
        2. Check if MEM enhancement enabled
        3. Get ML prediction
        4. Load relevant memories
        5. Apply learned patterns
        6. Boost confidence
        7. Return enhanced signal
        """
        # Load strategy metadata
        strategy = await self.registry.get_strategy(strategy_id)

        if not strategy.mem_integration.enabled:
            return base_signal  # No enhancement

        # Get ML prediction
        ml_prediction = await self.mem.ml_model.predict_reversal(
            symbol=market_context['symbol'],
            candles=market_context['recent_candles']
        )

        # Load relevant memories for this strategy
        memories = await self.mem.recall_strategy_memories(
            strategy_id=strategy_id,
            symbol=market_context['symbol'],
            lookback_trades=50
        )

        # Get learned patterns applicable to current market
        applicable_patterns = await self.get_applicable_patterns(
            strategy_id,
            market_context
        )

        # Combine everything
        enhanced_signal = {
            **base_signal,
            'original_confidence': base_signal['confidence'],
            'ml_prediction': ml_prediction,
            'ml_confidence': ml_prediction['confidence'],
            'applicable_patterns': applicable_patterns,
            'mem_reasoning': self.generate_reasoning(
                base_signal,
                ml_prediction,
                memories,
                applicable_patterns
            )
        }

        # Calculate enhanced confidence
        enhanced_signal['confidence'] = self.calculate_enhanced_confidence(
            base_confidence=base_signal['confidence'],
            ml_confidence=ml_prediction['confidence'],
            pattern_confidence=self.pattern_average_win_rate(applicable_patterns),
            memory_success_rate=self.memory_success_rate(memories)
        )

        # Adjust position sizing based on confidence
        enhanced_signal['position_size'] = self.calculate_position_size(
            base_size=base_signal.get('position_size', 1.0),
            confidence=enhanced_signal['confidence'],
            volatility=market_context.get('volatility', 0.01)
        )

        return enhanced_signal

    async def learn_from_outcome(
        self,
        strategy_id: str,
        signal: Dict,
        outcome: Dict
    ):
        """
        Learn from trade outcome and update MEM knowledge

        Flow:
        1. Record in strategy_performance table
        2. Update MEM memory
        3. Adjust strategy parameters if needed
        4. Discover new patterns if applicable
        5. Update learned strategies
        """
        # Record performance
        await self.registry.record_trade(strategy_id, TradePerformance(
            trade_id=outcome['trade_id'],
            symbol=outcome['symbol'],
            signal_action=signal['action'],
            signal_confidence=signal['confidence'],
            entry_price=outcome['entry_price'],
            exit_price=outcome['exit_price'],
            pnl=outcome['pnl'],
            pnl_pct=outcome['pnl_pct'],
            is_win=outcome['pnl'] > 0,
            was_mem_enhanced=True,
            mem_confidence_boost=signal['confidence'] - signal['original_confidence']
        ))

        # Update MEM memory
        await self.mem.store_decision_outcome(
            strategy_id=strategy_id,
            decision=signal,
            outcome=outcome
        )

        # Check if we should adjust parameters
        recent_performance = await self.registry.get_performance(
            strategy_id,
            TimeRange(days=30)
        )

        if recent_performance.win_rate < 0.45:
            # Performance degraded - adjust parameters
            await self.reduce_risk_parameters(strategy_id)
        elif recent_performance.win_rate > 0.70:
            # Excellent performance - can increase size
            await self.increase_position_sizing(strategy_id)

        # Discover patterns
        if recent_performance.total_trades % 10 == 0:  # Every 10 trades
            await self.discover_new_patterns(strategy_id)

    async def discover_new_patterns(self, strategy_id: str):
        """
        Analyze recent trades to discover new patterns
        """
        # Get last 100 trades
        trades = await self.registry.get_recent_trades(strategy_id, limit=100)

        # Analyze winning trades
        winning_trades = [t for t in trades if t.is_win]

        # Find common characteristics
        patterns = self.pattern_discovery_algorithm(winning_trades)

        # Filter patterns with >70% win rate and >5 occurrences
        significant_patterns = [
            p for p in patterns
            if p.win_rate >= 0.70 and p.trade_count >= 5
        ]

        # Create learned strategies from patterns
        for pattern in significant_patterns:
            await self.create_learned_strategy(strategy_id, pattern)
```

---

## Directory Structure

```
/root/AlgoTrendy_v2.6/
├── data/strategy_registry/
│   ├── metadata/                     # JSON metadata files
│   │   ├── {strategy_id}.json
│   │   └── ...
│   ├── code_snapshots/               # Version snapshots
│   │   ├── {version_id}/
│   │   │   ├── strategy.py or strategy.cs
│   │   │   └── parameters.json
│   └── performance_cache/            # Cached metrics
│       └── {strategy_id}_metrics.json
│
├── backend/AlgoTrendy.Core/
│   └── Services/
│       ├── StrategyRegistryService.cs
│       ├── StrategyVersionService.cs
│       └── StrategyPerformanceService.cs
│
├── strategyGrpDev02/
│   └── strategy_registry/
│       ├── registry_manager.py       # Python registry manager
│       ├── models.py                 # Data models
│       └── queries.py                # Database queries
│
└── MEM/strategy_enhancement/
    ├── mem_strategy_connector.py     # MEM integration
    ├── pattern_discovery.py          # Pattern detection
    └── learned_strategies/           # Auto-generated
        ├── learned_strategy_{id}.py
        └── ...
```

---

## Implementation Roadmap

### Phase 1: Database Setup (Week 1)
- [ ] Create QuestDB tables
- [ ] Set up indexes
- [ ] Create JSON metadata directory structure
- [ ] Migrate existing strategies to registry

### Phase 2: C# Registry Service (Week 2)
- [ ] Implement IStrategyRegistryService
- [ ] Create StrategyMetadata models
- [ ] Add discovery and search APIs
- [ ] Integrate with existing IStrategy implementations

### Phase 3: Python Registry Manager (Week 3)
- [ ] Implement StrategyRegistryManager
- [ ] Create Python models
- [ ] Add performance tracking
- [ ] Register strategyGrpDev02 strategies

### Phase 4: MEM Integration (Week 4)
- [ ] Implement MEMStrategyConnector
- [ ] Add signal enhancement flow
- [ ] Create learning feedback loop
- [ ] Pattern discovery algorithm

### Phase 5: Dashboard & UI (Week 5)
- [ ] Strategy browser UI
- [ ] Performance comparison charts
- [ ] MEM enhancement visualization
- [ ] Parameter tuning interface

---

## Benefits Summary

✅ **Unified Registry** - All strategies (C#, Python, MEM-learned) in one place
✅ **Rapid Discovery** - Find strategies by tags, performance, category
✅ **Version Control** - Track every change with full history
✅ **Performance Tracking** - Automatic metrics for every trade
✅ **MEM Integration** - Seamless AI enhancement workflow
✅ **Learned Strategies** - Auto-generated algorithms from patterns
✅ **Audit Trail** - Complete compliance tracking
✅ **A/B Testing** - Compare strategy versions
✅ **Risk Management** - Auto-adjust based on performance
✅ **Extensible** - Easy to add new strategies and features

---

**Status**: ✅ Design Complete - Ready for Implementation
**Estimated Implementation**: 5 weeks
**Dependencies**: QuestDB, .NET 8, Python 3.10+

---

*This architecture represents a production-grade strategy management system suitable for institutional trading operations.*
