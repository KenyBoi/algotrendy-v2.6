#!/usr/bin/env python3
"""
Register Freqtrade bot instances as deployable strategies in the Strategy Registry
Enables MEM to orchestrate and manage Freqtrade bots dynamically
"""

import sys
import os
from pathlib import Path

# Add the registry module to path
sys.path.insert(0, str(Path(__file__).parent / "backend" / "AlgoTrendy.Core" / "Services" / "StrategyRegistry"))

from registry_manager import StrategyRegistryManager


def register_freqtrade_bots():
    """Register the 3 Freqtrade bot instances as deployable strategies"""

    registry = StrategyRegistryManager(
        metadata_dir="/root/AlgoTrendy_v2.6/data/strategy_registry/metadata",
        performance_dir="/root/AlgoTrendy_v2.6/data/strategy_registry/performance"
    )

    print("\n" + "="*80)
    print("REGISTERING FREQTRADE BOTS AS MEM-DEPLOYABLE STRATEGIES")
    print("="*80 + "\n")

    # ========================================
    # 1. Conservative RSI Bot (Port 8082)
    # ========================================
    print("📊 Registering Conservative RSI Bot...")

    conservative_id = registry.register_strategy(
        name="freqtrade_conservative_rsi",
        display_name="Freqtrade Conservative RSI",
        description="Conservative RSI-based trading strategy via Freqtrade. "
                    "Uses standard RSI overbought/oversold levels with strict risk management. "
                    "Suitable for stable, low-volatility market conditions.",
        category="mean_reversion",
        language="freqtrade",
        file_path="/freqtrade/user_data/strategies/RSI_Conservative.py",
        class_name="RSI_Conservative",
        status="active",
        sub_category="rsi_mean_reversion",
        complexity="low",
        tags=["freqtrade", "rsi", "mean_reversion", "conservative", "low_risk"],
        asset_classes=["crypto"],

        # Deployment configuration (Freqtrade-specific)
        deployment={
            "type": "freqtrade",
            "port": 8082,
            "config_file": "/freqtrade/user_data/configs/conservative_rsi.json",
            "dry_run": True,
            "strategy_name": "RSI_Conservative"
        },

        # Backtest results (from historical Freqtrade backtests)
        backtest_results={
            "sharpe_ratio": 1.15,
            "win_rate": 0.62,
            "num_trades": 145,
            "total_return": 0.28,
            "max_drawdown": -0.08,
            "avg_trade_duration": "4h 23m",
            "best_pair": "BTC/USDT",
            "timeframe": "5m"
        },

        # Parameters
        parameters={
            "rsi_period": 14,
            "rsi_overbought": 70,
            "rsi_oversold": 30,
            "minimal_roi": {
                "0": 0.10,
                "30": 0.05,
                "60": 0.02
            },
            "stoploss": -0.05
        },

        # MEM integration
        mem_enabled=True,
        mem_enhancement_type="recommendation",
        estimated_improvement={
            "expected_sharpe_boost": 0.15,
            "expected_win_rate_improvement": 0.05
        },

        # Risk profile
        risk_profile={
            "risk_level": "low",
            "max_position_size_pct": 0.15,
            "max_leverage": 1.0,
            "stop_loss_pct": 0.05,
            "requires_margin": False,
            "recommended_capital": 1000
        },

        # Requirements
        requirements={
            "minimum_capital": 500,
            "supported_exchanges": ["binance", "kraken", "coinbase"],
            "minimum_liquidity": 100000,
            "market_hours": "24/7",
            "preferred_volatility": "low_to_medium"
        },

        dependencies=["freqtrade>=2023.8", "ta-lib"],
        created_by="system_migration_v2.5"
    )

    print(f"   ✅ Registered: {conservative_id}\n")

    # ========================================
    # 2. MACD Hunter Bot (Port 8083)
    # ========================================
    print("📊 Registering MACD Hunter Bot...")

    macd_id = registry.register_strategy(
        name="freqtrade_macd_hunter",
        display_name="Freqtrade MACD Hunter",
        description="Aggressive MACD-based momentum strategy via Freqtrade. "
                    "Hunts for strong momentum signals with fast MACD crossovers. "
                    "Best suited for trending markets with medium-to-high volatility.",
        category="momentum",
        language="freqtrade",
        file_path="/freqtrade/user_data/strategies/MACD_Aggressive.py",
        class_name="MACD_Aggressive",
        status="active",
        sub_category="macd_momentum",
        complexity="moderate",
        tags=["freqtrade", "macd", "momentum", "aggressive", "trend_following"],
        asset_classes=["crypto"],

        # Deployment configuration
        deployment={
            "type": "freqtrade",
            "port": 8083,
            "config_file": "/freqtrade/user_data/configs/macd_hunter.json",
            "dry_run": True,
            "strategy_name": "MACD_Aggressive"
        },

        # Backtest results
        backtest_results={
            "sharpe_ratio": 1.45,
            "win_rate": 0.58,
            "num_trades": 203,
            "total_return": 0.42,
            "max_drawdown": -0.12,
            "avg_trade_duration": "2h 15m",
            "best_pair": "ETH/USDT",
            "timeframe": "5m"
        },

        # Parameters
        parameters={
            "macd_fast": 12,
            "macd_slow": 26,
            "macd_signal": 9,
            "minimal_roi": {
                "0": 0.15,
                "20": 0.08,
                "40": 0.03
            },
            "stoploss": -0.08
        },

        # MEM integration
        mem_enabled=True,
        mem_enhancement_type="recommendation",
        estimated_improvement={
            "expected_sharpe_boost": 0.20,
            "expected_win_rate_improvement": 0.07
        },

        # Risk profile
        risk_profile={
            "risk_level": "medium_high",
            "max_position_size_pct": 0.25,
            "max_leverage": 2.0,
            "stop_loss_pct": 0.08,
            "requires_margin": False,
            "recommended_capital": 2000
        },

        # Requirements
        requirements={
            "minimum_capital": 1000,
            "supported_exchanges": ["binance", "kraken", "coinbase"],
            "minimum_liquidity": 200000,
            "market_hours": "24/7",
            "preferred_volatility": "medium_to_high"
        },

        dependencies=["freqtrade>=2023.8", "ta-lib"],
        created_by="system_migration_v2.5"
    )

    print(f"   ✅ Registered: {macd_id}\n")

    # ========================================
    # 3. Aggressive RSI Bot (Port 8084)
    # ========================================
    print("📊 Registering Aggressive RSI Bot...")

    aggressive_id = registry.register_strategy(
        name="freqtrade_aggressive_rsi",
        display_name="Freqtrade Aggressive RSI",
        description="Aggressive RSI-based scalping strategy via Freqtrade. "
                    "Uses tighter RSI thresholds and faster exits for quick profits. "
                    "Optimized for high-frequency trading in volatile markets.",
        category="mean_reversion",
        language="freqtrade",
        file_path="/freqtrade/user_data/strategies/RSI_Aggressive.py",
        class_name="RSI_Aggressive",
        status="active",
        sub_category="rsi_scalping",
        complexity="moderate",
        tags=["freqtrade", "rsi", "scalping", "aggressive", "high_frequency"],
        asset_classes=["crypto"],

        # Deployment configuration
        deployment={
            "type": "freqtrade",
            "port": 8084,
            "config_file": "/freqtrade/user_data/configs/aggressive_rsi.json",
            "dry_run": True,
            "strategy_name": "RSI_Aggressive"
        },

        # Backtest results
        backtest_results={
            "sharpe_ratio": 1.28,
            "win_rate": 0.65,
            "num_trades": 387,
            "total_return": 0.35,
            "max_drawdown": -0.10,
            "avg_trade_duration": "1h 42m",
            "best_pair": "BNB/USDT",
            "timeframe": "1m"
        },

        # Parameters
        parameters={
            "rsi_period": 9,
            "rsi_overbought": 75,
            "rsi_oversold": 25,
            "minimal_roi": {
                "0": 0.08,
                "15": 0.04,
                "30": 0.01
            },
            "stoploss": -0.06
        },

        # MEM integration
        mem_enabled=True,
        mem_enhancement_type="recommendation",
        estimated_improvement={
            "expected_sharpe_boost": 0.18,
            "expected_win_rate_improvement": 0.06
        },

        # Risk profile
        risk_profile={
            "risk_level": "high",
            "max_position_size_pct": 0.20,
            "max_leverage": 3.0,
            "stop_loss_pct": 0.06,
            "requires_margin": False,
            "recommended_capital": 2500
        },

        # Requirements
        requirements={
            "minimum_capital": 1500,
            "supported_exchanges": ["binance", "kraken", "coinbase"],
            "minimum_liquidity": 150000,
            "market_hours": "24/7",
            "preferred_volatility": "high"
        },

        dependencies=["freqtrade>=2023.8", "ta-lib"],
        created_by="system_migration_v2.5"
    )

    print(f"   ✅ Registered: {aggressive_id}\n")

    # ========================================
    # Summary
    # ========================================
    print("\n" + "="*80)
    print("✅ REGISTRATION COMPLETE")
    print("="*80)
    print(f"\nRegistered {3} Freqtrade strategies:")
    print(f"  1. Conservative RSI (8082): {conservative_id}")
    print(f"  2. MACD Hunter (8083):      {macd_id}")
    print(f"  3. Aggressive RSI (8084):   {aggressive_id}")
    print("\nThese strategies can now be deployed via MEM using MemStrategyDeploymentService")
    print("API Endpoints:")
    print("  - GET  /api/mem/strategies          (list all deployable strategies)")
    print("  - POST /api/mem/strategies/{id}/deploy  (deploy a strategy)")
    print("  - POST /api/mem/strategies/{id}/stop    (stop a deployed strategy)")
    print("  - GET  /api/mem/recommendations     (get MEM strategy recommendations)")
    print("\n")

    return [conservative_id, macd_id, aggressive_id]


if __name__ == '__main__':
    try:
        strategy_ids = register_freqtrade_bots()
        print("✅ Script completed successfully!")
        sys.exit(0)
    except Exception as e:
        print(f"\n❌ ERROR: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)
