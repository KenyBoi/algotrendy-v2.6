"""
AlgoTrendy Models Package
=========================
Python models mirroring C# backend models for Avellaneda-Stoikov market making strategy.

Modules:
- market_making: Order book, features, parameters, signals
- reinforcement_learning: RL state, action, reward, transition
- liquidity: Liquidity metrics, gaps, spread analysis
"""

__version__ = '1.0.0'

from .market_making import (
    OrderBookLevel,
    OrderBookSnapshot,
    ASFeatures,
    ASParameters,
    ASSignal,
    InventoryState
)

from .reinforcement_learning import (
    RLState,
    RLAction,
    RLReward,
    RLTransition
)

__all__ = [
    # Market Making
    'OrderBookLevel',
    'OrderBookSnapshot',
    'ASFeatures',
    'ASParameters',
    'ASSignal',
    'InventoryState',
    # Reinforcement Learning
    'RLState',
    'RLAction',
    'RLReward',
    'RLTransition'
]
