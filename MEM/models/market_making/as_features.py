"""
Avellaneda-Stoikov Features
============================
22 features for Reinforcement Learning agent.

Mirrors C# implementation in:
backend/AlgoTrendy.TradingEngine/Models/MarketMaking/ASFeatures.cs

Features are categorized into 4 groups:
- Inventory (4 features)
- Order Book (9 features)
- Microstructure (4 features)
- Volatility/Candles (5 features)
"""

from dataclasses import dataclass, field
from typing import List
import numpy as np
import json


@dataclass
class ASFeatures:
    """
    Avellaneda-Stoikov 22 features for Reinforcement Learning agent.

    Categories:
    - Inventory (4 features)
    - Order Book (9 features)
    - Microstructure (4 features)
    - Volatility/Candles (5 features)
    """

    # ===== Inventory Features (4) =====

    current_inventory: float
    """Current inventory level in base currency"""

    inventory_pct: float
    """Inventory as percentage of max position (0.0 to 1.0)"""

    inventory_distance_from_target: float
    """Distance from inventory target (usually 0)"""

    inventory_change_rate: float
    """Rate of change in inventory"""

    # ===== Order Book Features (9) =====

    best_bid: float
    """Best bid price"""

    best_ask: float
    """Best ask price"""

    bid_volume: float
    """Volume at best bid level"""

    ask_volume: float
    """Volume at best ask level"""

    spread: float
    """Bid-ask spread (absolute)"""

    spread_pct: float
    """Spread as percentage of mid price"""

    order_book_imbalance: float
    """Order book imbalance: (BidVol - AskVol) / (BidVol + AskVol), Range: -1 to +1"""

    microprice: float
    """Microprice (volume-weighted mid price)"""

    weighted_mid_price: float
    """Weighted mid price across multiple levels"""

    # ===== Market Microstructure Features (4) =====

    recent_trade_direction: float
    """Recent trade direction (buy vs sell pressure). Positive = more buying"""

    trade_flow_imbalance: float
    """Trade flow imbalance"""

    quote_update_frequency: float
    """Quote update frequency (updates per second)"""

    time_since_last_trade: float
    """Time since last trade (seconds)"""

    # ===== Volatility & Price Action Features (5) =====

    volatility_1min: float
    """1-minute return volatility"""

    momentum_1min: float
    """Price momentum (1-minute)"""

    volume_1min: float
    """Volume (1-minute)"""

    vwap_distance: float
    """Distance from VWAP"""

    high_low_range_1min: float
    """High-low range (1-minute)"""

    # Feature metadata
    FEATURE_COUNT: int = field(default=22, init=False, repr=False)

    def to_array(self) -> np.ndarray:
        """
        Converts features to numpy array for RL agent input.
        Order must match C# implementation.

        Returns:
            numpy array of 22 features
        """
        return np.array([
            # Inventory (4)
            self.current_inventory,
            self.inventory_pct,
            self.inventory_distance_from_target,
            self.inventory_change_rate,

            # Order Book (9)
            self.best_bid,
            self.best_ask,
            self.bid_volume,
            self.ask_volume,
            self.spread,
            self.spread_pct,
            self.order_book_imbalance,
            self.microprice,
            self.weighted_mid_price,

            # Microstructure (4)
            self.recent_trade_direction,
            self.trade_flow_imbalance,
            self.quote_update_frequency,
            self.time_since_last_trade,

            # Volatility/Candles (5)
            self.volatility_1min,
            self.momentum_1min,
            self.volume_1min,
            self.vwap_distance,
            self.high_low_range_1min
        ], dtype=np.float32)

    @staticmethod
    def get_feature_names() -> List[str]:
        """
        Gets feature names in order.

        Returns:
            List of 22 feature names
        """
        return [
            # Inventory
            "current_inventory", "inventory_pct", "inventory_distance_from_target", "inventory_change_rate",

            # Order Book
            "best_bid", "best_ask", "bid_volume", "ask_volume", "spread", "spread_pct",
            "order_book_imbalance", "microprice", "weighted_mid_price",

            # Microstructure
            "recent_trade_direction", "trade_flow_imbalance", "quote_update_frequency", "time_since_last_trade",

            # Volatility/Candles
            "volatility_1min", "momentum_1min", "volume_1min", "vwap_distance", "high_low_range_1min"
        ]

    @staticmethod
    def get_feature_categories() -> dict:
        """
        Gets features grouped by category.

        Returns:
            Dictionary of category -> feature names
        """
        return {
            "inventory": [
                "current_inventory", "inventory_pct",
                "inventory_distance_from_target", "inventory_change_rate"
            ],
            "order_book": [
                "best_bid", "best_ask", "bid_volume", "ask_volume", "spread",
                "spread_pct", "order_book_imbalance", "microprice", "weighted_mid_price"
            ],
            "microstructure": [
                "recent_trade_direction", "trade_flow_imbalance",
                "quote_update_frequency", "time_since_last_trade"
            ],
            "volatility_candles": [
                "volatility_1min", "momentum_1min", "volume_1min",
                "vwap_distance", "high_low_range_1min"
            ]
        }

    def to_dict(self) -> dict:
        """
        Convert to dictionary for JSON serialization.

        Returns:
            Dictionary representation
        """
        return {
            # Inventory
            "current_inventory": self.current_inventory,
            "inventory_pct": self.inventory_pct,
            "inventory_distance_from_target": self.inventory_distance_from_target,
            "inventory_change_rate": self.inventory_change_rate,

            # Order Book
            "best_bid": self.best_bid,
            "best_ask": self.best_ask,
            "bid_volume": self.bid_volume,
            "ask_volume": self.ask_volume,
            "spread": self.spread,
            "spread_pct": self.spread_pct,
            "order_book_imbalance": self.order_book_imbalance,
            "microprice": self.microprice,
            "weighted_mid_price": self.weighted_mid_price,

            # Microstructure
            "recent_trade_direction": self.recent_trade_direction,
            "trade_flow_imbalance": self.trade_flow_imbalance,
            "quote_update_frequency": self.quote_update_frequency,
            "time_since_last_trade": self.time_since_last_trade,

            # Volatility/Candles
            "volatility_1min": self.volatility_1min,
            "momentum_1min": self.momentum_1min,
            "volume_1min": self.volume_1min,
            "vwap_distance": self.vwap_distance,
            "high_low_range_1min": self.high_low_range_1min
        }

    def to_json(self) -> str:
        """
        Convert to JSON string.

        Returns:
            JSON string representation
        """
        return json.dumps(self.to_dict(), indent=2)

    @classmethod
    def from_dict(cls, data: dict) -> 'ASFeatures':
        """
        Create ASFeatures from dictionary.

        Args:
            data: Dictionary with feature data

        Returns:
            ASFeatures instance
        """
        return cls(
            # Inventory
            current_inventory=data['current_inventory'],
            inventory_pct=data['inventory_pct'],
            inventory_distance_from_target=data['inventory_distance_from_target'],
            inventory_change_rate=data['inventory_change_rate'],

            # Order Book
            best_bid=data['best_bid'],
            best_ask=data['best_ask'],
            bid_volume=data['bid_volume'],
            ask_volume=data['ask_volume'],
            spread=data['spread'],
            spread_pct=data['spread_pct'],
            order_book_imbalance=data['order_book_imbalance'],
            microprice=data['microprice'],
            weighted_mid_price=data['weighted_mid_price'],

            # Microstructure
            recent_trade_direction=data['recent_trade_direction'],
            trade_flow_imbalance=data['trade_flow_imbalance'],
            quote_update_frequency=data['quote_update_frequency'],
            time_since_last_trade=data['time_since_last_trade'],

            # Volatility/Candles
            volatility_1min=data['volatility_1min'],
            momentum_1min=data['momentum_1min'],
            volume_1min=data['volume_1min'],
            vwap_distance=data['vwap_distance'],
            high_low_range_1min=data['high_low_range_1min']
        )

    @classmethod
    def from_json(cls, json_str: str) -> 'ASFeatures':
        """
        Create ASFeatures from JSON string.

        Args:
            json_str: JSON string

        Returns:
            ASFeatures instance
        """
        data = json.loads(json_str)
        return cls.from_dict(data)

    @classmethod
    def from_array(cls, arr: np.ndarray) -> 'ASFeatures':
        """
        Create ASFeatures from numpy array.

        Args:
            arr: Numpy array of 22 features (must match order)

        Returns:
            ASFeatures instance

        Raises:
            ValueError: If array length is not 22
        """
        if len(arr) != 22:
            raise ValueError(f"Expected 22 features, got {len(arr)}")

        return cls(
            # Inventory (4)
            current_inventory=float(arr[0]),
            inventory_pct=float(arr[1]),
            inventory_distance_from_target=float(arr[2]),
            inventory_change_rate=float(arr[3]),

            # Order Book (9)
            best_bid=float(arr[4]),
            best_ask=float(arr[5]),
            bid_volume=float(arr[6]),
            ask_volume=float(arr[7]),
            spread=float(arr[8]),
            spread_pct=float(arr[9]),
            order_book_imbalance=float(arr[10]),
            microprice=float(arr[11]),
            weighted_mid_price=float(arr[12]),

            # Microstructure (4)
            recent_trade_direction=float(arr[13]),
            trade_flow_imbalance=float(arr[14]),
            quote_update_frequency=float(arr[15]),
            time_since_last_trade=float(arr[16]),

            # Volatility/Candles (5)
            volatility_1min=float(arr[17]),
            momentum_1min=float(arr[18]),
            volume_1min=float(arr[19]),
            vwap_distance=float(arr[20]),
            high_low_range_1min=float(arr[21])
        )

    def __str__(self) -> str:
        return (
            f"ASFeatures[22]: "
            f"Inv={self.inventory_pct:.1%}, Spread={self.spread_pct:.2%}, "
            f"OBI={self.order_book_imbalance:.2f}, Vol={self.volatility_1min:.4f}"
        )

    def __repr__(self) -> str:
        return f"ASFeatures(feature_count=22, inventory_pct={self.inventory_pct:.2%})"
