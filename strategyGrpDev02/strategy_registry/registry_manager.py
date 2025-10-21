"""
Strategy Registry Manager - Python Implementation
Provides unified interface for strategy discovery, tracking, and MEM integration
"""

import json
import os
import uuid
from datetime import datetime, timedelta
from typing import List, Dict, Optional, Any
from dataclasses import dataclass, asdict
from pathlib import Path

# Note: In production, would use actual database connection
# For now, using JSON files as proof-of-concept


@dataclass
class StrategyFilter:
    """Filter criteria for strategy queries"""
    category: Optional[str] = None
    tags: Optional[List[str]] = None
    min_sharpe: Optional[float] = None
    status: str = 'active'
    language: Optional[str] = None


@dataclass
class StrategyMetadata:
    """Complete strategy metadata"""
    id: str
    name: str
    display_name: str
    description: str
    category: str
    language: str
    file_path: str
    class_name: str
    origin: str
    status: str
    tags: List[str]
    created_at: str
    updated_at: str

    # Performance
    lifetime_sharpe_ratio: Optional[float] = None
    lifetime_win_rate: Optional[float] = None
    total_trades: int = 0
    total_pnl: float = 0.0

    # MEM Integration
    mem_enabled: bool = False
    mem_enhanced_version: bool = False
    mem_learned_patterns_count: int = 0

    @classmethod
    def from_json(cls, data: Dict) -> 'StrategyMetadata':
        """Create from JSON data"""
        strategy = data.get('strategy', {})
        return cls(
            id=strategy.get('id', ''),
            name=strategy.get('name', ''),
            display_name=strategy.get('display_name', ''),
            description=strategy.get('description', ''),
            category=strategy['classification'].get('category', ''),
            language=strategy['implementation'].get('language', ''),
            file_path=strategy['implementation'].get('file_path', ''),
            class_name=strategy['implementation'].get('class_name', ''),
            origin=strategy.get('origin', 'unknown'),
            status=strategy.get('status', 'experimental'),
            tags=strategy['classification'].get('tags', []),
            created_at=strategy['metadata'].get('created_at', ''),
            updated_at=strategy['metadata'].get('last_updated', ''),
            lifetime_sharpe_ratio=strategy['performance']['backtest'].get('sharpe_ratio'),
            lifetime_win_rate=strategy['performance']['backtest'].get('win_rate'),
            total_trades=strategy['performance']['backtest'].get('num_trades', 0),
            mem_enabled=strategy['mem_integration'].get('enabled', False),
            mem_enhanced_version=strategy['mem_integration'].get('enhancement_type') == 'full'
        )


@dataclass
class TradePerformance:
    """Performance record for a single trade"""
    trade_id: str
    symbol: str
    signal_action: str
    signal_confidence: float
    entry_price: float
    exit_price: float
    entry_timestamp: str
    exit_timestamp: str
    pnl: float
    pnl_pct: float
    is_win: bool
    was_mem_enhanced: bool = False
    mem_confidence_boost: float = 0.0
    learned_pattern_applied: Optional[str] = None


@dataclass
class PerformanceMetrics:
    """Calculated performance metrics"""
    total_trades: int
    win_rate: float
    sharpe_ratio: float
    sortino_ratio: float
    max_drawdown: float
    total_pnl: float
    avg_win: float
    avg_loss: float
    profit_factor: float

    @classmethod
    def calculate(cls, trades: List[TradePerformance]) -> 'PerformanceMetrics':
        """Calculate metrics from trade list"""
        if not trades:
            return cls(0, 0, 0, 0, 0, 0, 0, 0, 0)

        wins = [t for t in trades if t.is_win]
        losses = [t for t in trades if not t.is_win]

        win_rate = len(wins) / len(trades) if trades else 0
        total_pnl = sum(t.pnl for t in trades)
        avg_win = sum(t.pnl for t in wins) / len(wins) if wins else 0
        avg_loss = sum(t.pnl for t in losses) / len(losses) if losses else 0
        profit_factor = abs(avg_win / avg_loss) if avg_loss != 0 else 0

        # For full calculation, would need returns series
        # Simplified here
        sharpe = 0.0  # TODO: Calculate from returns
        sortino = 0.0  # TODO: Calculate from downside deviation
        max_dd = 0.0   # TODO: Calculate from equity curve

        return cls(
            total_trades=len(trades),
            win_rate=win_rate,
            sharpe_ratio=sharpe,
            sortino_ratio=sortino,
            max_drawdown=max_dd,
            total_pnl=total_pnl,
            avg_win=avg_win,
            avg_loss=avg_loss,
            profit_factor=profit_factor
        )


class StrategyRegistryManager:
    """
    Manages strategy registry - discovery, tracking, and MEM integration
    """

    def __init__(self, metadata_dir: str, performance_dir: str):
        self.metadata_dir = Path(metadata_dir)
        self.performance_dir = Path(performance_dir)
        self.cache: Dict[str, StrategyMetadata] = {}

        # Ensure directories exist
        self.metadata_dir.mkdir(parents=True, exist_ok=True)
        self.performance_dir.mkdir(parents=True, exist_ok=True)

    # ============================================
    # DISCOVERY & SEARCH
    # ============================================

    def get_all_strategies(self, filter: Optional[StrategyFilter] = None) -> List[StrategyMetadata]:
        """Get all strategies matching filter"""
        strategies = []

        # Load all metadata files
        for metadata_file in self.metadata_dir.glob("*.json"):
            try:
                strategy = self.get_strategy(metadata_file.stem)
                strategies.append(strategy)
            except Exception as e:
                print(f"Error loading {metadata_file}: {e}")
                continue

        # Apply filters
        if filter:
            if filter.category:
                strategies = [s for s in strategies if s.category == filter.category]
            if filter.tags:
                strategies = [s for s in strategies if any(tag in s.tags for tag in filter.tags)]
            if filter.min_sharpe:
                strategies = [s for s in strategies if s.lifetime_sharpe_ratio and s.lifetime_sharpe_ratio >= filter.min_sharpe]
            if filter.status:
                strategies = [s for s in strategies if s.status == filter.status]
            if filter.language:
                strategies = [s for s in strategies if s.language == filter.language]

        # Sort by Sharpe ratio (best first)
        strategies.sort(key=lambda s: s.lifetime_sharpe_ratio or 0, reverse=True)

        return strategies

    def get_strategy(self, strategy_id: str) -> StrategyMetadata:
        """Get strategy by ID with caching"""
        if strategy_id in self.cache:
            return self.cache[strategy_id]

        metadata_path = self.metadata_dir / f"{strategy_id}.json"
        if not metadata_path.exists():
            raise FileNotFoundError(f"Strategy {strategy_id} not found")

        with open(metadata_path, 'r') as f:
            data = json.load(f)

        metadata = StrategyMetadata.from_json(data)
        self.cache[strategy_id] = metadata
        return metadata

    def search_strategies(self, query: str) -> List[StrategyMetadata]:
        """Full-text search across strategies"""
        query_lower = query.lower()
        strategies = self.get_all_strategies()

        matches = []
        for strategy in strategies:
            # Search in name, display_name, description, tags
            if (query_lower in strategy.name.lower() or
                query_lower in strategy.display_name.lower() or
                query_lower in strategy.description.lower() or
                any(query_lower in tag.lower() for tag in strategy.tags)):
                matches.append(strategy)

        return matches

    def get_strategies_by_tags(self, tags: List[str]) -> List[StrategyMetadata]:
        """Get strategies matching any of the given tags"""
        return self.get_all_strategies(StrategyFilter(tags=tags))

    # ============================================
    # REGISTRATION
    # ============================================

    def register_strategy(
        self,
        name: str,
        display_name: str,
        description: str,
        category: str,
        language: str,
        file_path: str,
        class_name: str,
        **kwargs
    ) -> str:
        """Register new strategy"""
        strategy_id = str(uuid.uuid4())

        metadata = {
            "schema_version": "1.0.0",
            "strategy": {
                "id": strategy_id,
                "name": name,
                "display_name": display_name,
                "description": description,
                "version": "1.0.0",
                "status": kwargs.get('status', 'experimental'),

                "classification": {
                    "category": category,
                    "sub_category": kwargs.get('sub_category', ''),
                    "complexity": kwargs.get('complexity', 'moderate'),
                    "tags": kwargs.get('tags', []),
                    "asset_classes": kwargs.get('asset_classes', [])
                },

                "implementation": {
                    "language": language,
                    "file_path": file_path,
                    "class_name": class_name,
                    "entry_point": f"{class_name}.generate_signal",
                    "dependencies": kwargs.get('dependencies', []),
                    "minimum_python_version": "3.10"
                },

                "academic_foundation": kwargs.get('academic_foundation', {}),

                "parameters": kwargs.get('parameters', {}),

                "performance": {
                    "backtest": kwargs.get('backtest_results', {}),
                    "live_trading": {
                        "start_date": None,
                        "total_trades": 0,
                        "live_sharpe": None,
                        "live_win_rate": None
                    }
                },

                "mem_integration": {
                    "enabled": kwargs.get('mem_enabled', True),
                    "enhancement_type": kwargs.get('mem_enhancement_type', 'full'),
                    "learned_patterns_count": 0,
                    "mem_version": "baseline_1.0",
                    "estimated_improvement": kwargs.get('estimated_improvement', {})
                },

                "requirements": kwargs.get('requirements', {}),

                "risk_profile": kwargs.get('risk_profile', {}),

                "metadata": {
                    "created_by": kwargs.get('created_by', 'system'),
                    "created_at": datetime.utcnow().isoformat() + 'Z',
                    "last_updated": datetime.utcnow().isoformat() + 'Z',
                    "last_backtest": None,
                    "deprecation_date": None,
                    "replacement_strategy_id": None
                }
            }
        }

        # Save metadata file
        metadata_path = self.metadata_dir / f"{strategy_id}.json"
        with open(metadata_path, 'w') as f:
            json.dump(metadata, f, indent=2)

        # Clear cache
        self.cache.clear()

        print(f"✅ Registered strategy: {display_name} ({strategy_id})")
        return strategy_id

    # ============================================
    # PERFORMANCE TRACKING
    # ============================================

    def record_trade(self, strategy_id: str, performance: TradePerformance):
        """Record trade performance"""
        perf_file = self.performance_dir / f"{strategy_id}_trades.jsonl"

        # Append to JSONL file (one JSON object per line)
        with open(perf_file, 'a') as f:
            f.write(json.dumps(asdict(performance)) + '\n')

        # Update strategy summary
        self.update_strategy_stats(strategy_id)

    def get_performance(
        self,
        strategy_id: str,
        days: Optional[int] = None
    ) -> PerformanceMetrics:
        """Get performance metrics"""
        perf_file = self.performance_dir / f"{strategy_id}_trades.jsonl"

        if not perf_file.exists():
            return PerformanceMetrics(0, 0, 0, 0, 0, 0, 0, 0, 0)

        trades = []
        cutoff_date = None
        if days:
            cutoff_date = (datetime.utcnow() - timedelta(days=days)).isoformat()

        with open(perf_file, 'r') as f:
            for line in f:
                trade_data = json.loads(line)
                if cutoff_date and trade_data['entry_timestamp'] < cutoff_date:
                    continue
                trades.append(TradePerformance(**trade_data))

        return PerformanceMetrics.calculate(trades)

    def update_strategy_stats(self, strategy_id: str):
        """Update cached performance stats in metadata"""
        metrics = self.get_performance(strategy_id)

        metadata_path = self.metadata_dir / f"{strategy_id}.json"
        with open(metadata_path, 'r') as f:
            data = json.load(f)

        # Update performance section
        data['strategy']['performance']['live_trading'] = {
            'total_trades': metrics.total_trades,
            'live_sharpe': metrics.sharpe_ratio,
            'live_win_rate': metrics.win_rate,
            'total_pnl': metrics.total_pnl
        }

        data['strategy']['metadata']['last_updated'] = datetime.utcnow().isoformat() + 'Z'

        with open(metadata_path, 'w') as f:
            json.dump(data, f, indent=2)

        # Clear cache
        if strategy_id in self.cache:
            del self.cache[strategy_id]

    # ============================================
    # MEM INTEGRATION
    # ============================================

    def is_mem_enhanced(self, strategy_id: str) -> bool:
        """Check if strategy is MEM-enhanced"""
        strategy = self.get_strategy(strategy_id)
        return strategy.mem_enhanced_version or strategy.mem_enabled

    def mark_as_mem_enhanced(self, strategy_id: str):
        """Mark strategy as MEM-enhanced"""
        metadata_path = self.metadata_dir / f"{strategy_id}.json"
        with open(metadata_path, 'r') as f:
            data = json.load(f)

        data['strategy']['mem_integration']['enhancement_type'] = 'full'
        data['strategy']['mem_integration']['enabled'] = True

        with open(metadata_path, 'w') as f:
            json.dump(data, f, indent=2)

        # Clear cache
        if strategy_id in self.cache:
            del self.cache[strategy_id]

    # ============================================
    # UTILITY METHODS
    # ============================================

    def list_strategies_summary(self) -> str:
        """Get formatted summary of all strategies"""
        strategies = self.get_all_strategies()

        output = f"\n{'='*80}\n"
        output += f"STRATEGY REGISTRY - {len(strategies)} Strategies\n"
        output += f"{'='*80}\n\n"

        for strategy in strategies:
            output += f"📊 {strategy.display_name}\n"
            output += f"   ID: {strategy.id}\n"
            output += f"   Category: {strategy.category} | Language: {strategy.language}\n"
            output += f"   Status: {strategy.status} | MEM: {'✅' if strategy.mem_enabled else '❌'}\n"

            if strategy.lifetime_sharpe_ratio:
                output += f"   Sharpe: {strategy.lifetime_sharpe_ratio:.2f} | "
                if strategy.lifetime_win_rate:
                    output += f"Win Rate: {strategy.lifetime_win_rate*100:.1f}% | "
                output += f"Trades: {strategy.total_trades}\n"

            output += f"   Tags: {', '.join(strategy.tags)}\n"
            output += f"\n"

        return output


# ============================================
# USAGE EXAMPLES
# ============================================

def example_usage():
    """Example usage of StrategyRegistryManager"""

    # Initialize registry
    registry = StrategyRegistryManager(
        metadata_dir="/root/AlgoTrendy_v2.6/data/strategy_registry/metadata",
        performance_dir="/root/AlgoTrendy_v2.6/data/strategy_registry/performance"
    )

    # Register a new strategy
    strategy_id = registry.register_strategy(
        name="volatility_managed_momentum",
        display_name="Volatility-Managed Momentum",
        description="Scales momentum positions inversely to realized volatility",
        category="momentum",
        language="python",
        file_path="/root/AlgoTrendy_v2.6/strategyGrpDev02/implementations/strategy1_vol_managed_momentum.py",
        class_name="VolatilityManagedMomentum",
        tags=["momentum", "volatility_scaling", "academic", "trend_following"],
        backtest_results={
            "sharpe_ratio": 1.197,
            "win_rate": 0.472,
            "num_trades": 106,
            "total_return": 4.898,
            "max_drawdown": -0.156
        },
        academic_foundation={
            "papers": [{
                "title": "Momentum Has Its Moments",
                "authors": ["Pedro Barroso", "Pedro Santa-Clara"],
                "year": 2015
            }]
        }
    )

    # Find high-performing strategies
    top_strategies = registry.get_all_strategies(StrategyFilter(
        min_sharpe=1.0,
        category="momentum"
    ))

    print(f"\nFound {len(top_strategies)} high-performing momentum strategies")

    # Search for strategies
    results = registry.search_strategies("volatility")
    print(f"\nFound {len(results)} strategies matching 'volatility'")

    # Get strategy by tags
    academic_strategies = registry.get_strategies_by_tags(["academic"])
    print(f"\nFound {len(academic_strategies)} academic strategies")

    # Record a trade
    registry.record_trade(strategy_id, TradePerformance(
        trade_id=str(uuid.uuid4()),
        symbol="BTCUSDT",
        signal_action="BUY",
        signal_confidence=0.78,
        entry_price=65000.0,
        exit_price=66200.0,
        entry_timestamp=datetime.utcnow().isoformat(),
        exit_timestamp=(datetime.utcnow() + timedelta(minutes=15)).isoformat(),
        pnl=500.0,
        pnl_pct=0.0185,
        is_win=True,
        was_mem_enhanced=True,
        mem_confidence_boost=0.13
    ))

    # Get performance
    performance = registry.get_performance(strategy_id)
    print(f"\nStrategy Performance:")
    print(f"  Win Rate: {performance.win_rate*100:.1f}%")
    print(f"  Total Trades: {performance.total_trades}")
    print(f"  Total P&L: ${performance.total_pnl:.2f}")

    # Print summary
    print(registry.list_strategies_summary())


if __name__ == '__main__':
    example_usage()
