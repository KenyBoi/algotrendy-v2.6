"""
Backtesting Module for AlgoTrendy API

This module provides backtesting capabilities with support for:
- Multiple backtesting engines (QuantConnect, Backtester.com, Custom)
- Various asset classes (Crypto, Futures, Equities)
- Technical indicators integration
- MemGPT AI integration
"""

from .models import (
    BacktestConfig,
    BacktestResults,
    IndicatorConfig,
    BacktestStatus,
)
from .engines import (
    BacktestEngine,
    QuantConnectEngine,
    BacktesterComEngine,
    CustomEngine,
)
from .indicators import AVAILABLE_INDICATORS, calculate_indicators

__all__ = [
    "BacktestConfig",
    "BacktestResults",
    "IndicatorConfig",
    "BacktestStatus",
    "BacktestEngine",
    "QuantConnectEngine",
    "BacktesterComEngine",
    "CustomEngine",
    "AVAILABLE_INDICATORS",
    "calculate_indicators",
]
