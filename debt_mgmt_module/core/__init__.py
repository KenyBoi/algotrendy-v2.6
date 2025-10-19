"""
Debt Management Module - Core Components
Version: 1.0.0

This module provides margin, leverage, and debt tracking functionality
for cryptocurrency trading systems.
"""

from .broker_abstraction import (
    BrokerInterface,
    BrokerManager,
    BybitBroker,
    BinanceBroker,
    OKXBroker,
    KrakenBroker,
    CoinbaseBroker,
    CryptoComBroker
)

from .fund_manager import (
    FundManager,
    SandboxFunds
)

__version__ = "1.0.0"

__all__ = [
    # Broker classes
    "BrokerInterface",
    "BrokerManager",
    "BybitBroker",
    "BinanceBroker",
    "OKXBroker",
    "KrakenBroker",
    "CoinbaseBroker",
    "CryptoComBroker",

    # Fund management
    "FundManager",
    "SandboxFunds",

    # Version
    "__version__"
]
