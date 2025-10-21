"""
Inventory State
===============
Current inventory state for market making strategy.

Mirrors C# implementation in:
backend/AlgoTrendy.TradingEngine/Models/MarketMaking/InventoryState.cs

Tracks:
- Position, risk level, inventory metrics
- P&L (realized and unrealized)
- Risk management indicators
"""

from dataclasses import dataclass
from datetime import datetime
from typing import Optional
import json


@dataclass
class InventoryState:
    """
    Current inventory state for market making strategy.
    Tracks position, risk level, and inventory metrics.
    """

    symbol: str
    """Symbol being tracked"""

    timestamp: datetime
    """Timestamp of this state"""

    current_inventory: float
    """Current inventory (position) in base currency. Positive=long, Negative=short, 0=neutral"""

    max_inventory: float
    """Maximum allowed inventory (position limit)"""

    target_inventory: float = 0.0
    """Target inventory (usually 0 for market neutral)"""

    current_price: Optional[float] = None
    """Current market price (for P&L calculation)"""

    average_entry_price: Optional[float] = None
    """Average entry price for current position"""

    realized_pnl: float = 0.0
    """Realized P&L (from closed positions)"""

    @property
    def inventory_percent(self) -> float:
        """Inventory as percentage of max position (-100% to +100%)"""
        return self.current_inventory / self.max_inventory if self.max_inventory > 0 else 0.0

    @property
    def distance_from_target(self) -> float:
        """Distance from target inventory"""
        return self.current_inventory - self.target_inventory

    @property
    def absolute_inventory(self) -> float:
        """Absolute inventory level (always positive)"""
        return abs(self.current_inventory)

    @property
    def direction(self) -> int:
        """Inventory direction: 1 (long), -1 (short), 0 (neutral)"""
        if self.current_inventory > 0:
            return 1
        elif self.current_inventory < 0:
            return -1
        else:
            return 0

    @property
    def is_near_limit(self) -> bool:
        """Is inventory at or near limit (>90% of max)"""
        return abs(self.inventory_percent) >= 0.9

    @property
    def is_at_limit(self) -> bool:
        """Is inventory at or exceeding limit"""
        return abs(self.current_inventory) >= self.max_inventory

    @property
    def is_neutral(self) -> bool:
        """Is position market neutral (within 5% of target)"""
        return abs(self.inventory_percent) <= 0.05

    @property
    def available_capacity(self) -> float:
        """Available capacity to increase position (in base currency)"""
        return self.max_inventory - self.absolute_inventory

    @property
    def unrealized_pnl(self) -> Optional[float]:
        """Unrealized P&L (mark-to-market)"""
        if self.current_price is None or self.average_entry_price is None:
            return None

        return self.current_inventory * (self.current_price - self.average_entry_price)

    @property
    def total_pnl(self) -> Optional[float]:
        """Total P&L (realized + unrealized)"""
        if self.unrealized_pnl is not None:
            return self.realized_pnl + self.unrealized_pnl
        return None

    @property
    def risk_level(self) -> int:
        """
        Risk level (0-100): Higher = more risky.
        Based on inventory utilization and distance from target.
        """
        utilization = abs(self.inventory_percent)
        distance_pct = abs(self.distance_from_target / self.max_inventory) if self.max_inventory > 0 else 0.0

        # Risk increases exponentially near limits
        risk = (utilization * 0.7 + distance_pct * 0.3) * 100.0

        return int(min(100, max(0, risk)))

    @property
    def risk_category(self) -> str:
        """Risk category: Low, Medium, High, Critical"""
        risk = self.risk_level
        if risk < 30:
            return "Low"
        elif risk < 60:
            return "Medium"
        elif risk < 85:
            return "High"
        else:
            return "Critical"

    def can_increase_long(self, quantity: float) -> bool:
        """
        Can increase position (buy more).

        Args:
            quantity: Quantity to buy

        Returns:
            True if position can be increased
        """
        if quantity <= 0:
            return False

        new_inventory = self.current_inventory + quantity
        return new_inventory <= self.max_inventory

    def can_increase_short(self, quantity: float) -> bool:
        """
        Can decrease position (sell/short).

        Args:
            quantity: Quantity to sell

        Returns:
            True if position can be decreased
        """
        if quantity <= 0:
            return False

        new_inventory = self.current_inventory - quantity
        return abs(new_inventory) <= self.max_inventory

    @property
    def should_reduce_position(self) -> bool:
        """
        Should reduce position (risk management).
        True if inventory is >70% of max or >50% away from target.
        """
        utilization_pct = abs(self.inventory_percent)
        target_distance_pct = abs(self.distance_from_target / self.max_inventory) if self.max_inventory > 0 else 0.0

        return utilization_pct > 0.7 or target_distance_pct > 0.5

    def to_dict(self) -> dict:
        """
        Convert to dictionary for JSON serialization.

        Returns:
            Dictionary representation
        """
        return {
            "symbol": self.symbol,
            "timestamp": self.timestamp.isoformat(),
            "current_inventory": self.current_inventory,
            "target_inventory": self.target_inventory,
            "max_inventory": self.max_inventory,
            "current_price": self.current_price,
            "average_entry_price": self.average_entry_price,
            "realized_pnl": self.realized_pnl,
            # Computed properties
            "inventory_percent": self.inventory_percent,
            "distance_from_target": self.distance_from_target,
            "absolute_inventory": self.absolute_inventory,
            "direction": self.direction,
            "is_near_limit": self.is_near_limit,
            "is_at_limit": self.is_at_limit,
            "is_neutral": self.is_neutral,
            "available_capacity": self.available_capacity,
            "unrealized_pnl": self.unrealized_pnl,
            "total_pnl": self.total_pnl,
            "risk_level": self.risk_level,
            "risk_category": self.risk_category,
            "should_reduce_position": self.should_reduce_position
        }

    def to_json(self) -> str:
        """
        Convert to JSON string.

        Returns:
            JSON string representation
        """
        return json.dumps(self.to_dict(), indent=2)

    @classmethod
    def from_dict(cls, data: dict) -> 'InventoryState':
        """
        Create InventoryState from dictionary.

        Args:
            data: Dictionary with state data

        Returns:
            InventoryState instance
        """
        # Parse timestamp
        timestamp = data['timestamp']
        if isinstance(timestamp, str):
            timestamp = datetime.fromisoformat(timestamp.replace('Z', '+00:00'))

        return cls(
            symbol=data['symbol'],
            timestamp=timestamp,
            current_inventory=data['current_inventory'],
            target_inventory=data.get('target_inventory', 0.0),
            max_inventory=data['max_inventory'],
            current_price=data.get('current_price'),
            average_entry_price=data.get('average_entry_price'),
            realized_pnl=data.get('realized_pnl', 0.0)
        )

    @classmethod
    def from_json(cls, json_str: str) -> 'InventoryState':
        """
        Create InventoryState from JSON string.

        Args:
            json_str: JSON string

        Returns:
            InventoryState instance
        """
        data = json.loads(json_str)
        return cls.from_dict(data)

    def clone(self) -> 'InventoryState':
        """
        Deep clone.

        Returns:
            Copy of InventoryState
        """
        return InventoryState(
            symbol=self.symbol,
            timestamp=self.timestamp,
            current_inventory=self.current_inventory,
            target_inventory=self.target_inventory,
            max_inventory=self.max_inventory,
            current_price=self.current_price,
            average_entry_price=self.average_entry_price,
            realized_pnl=self.realized_pnl
        )

    def __str__(self) -> str:
        pnl_str = f", PnL={self.total_pnl:.2f}" if self.total_pnl is not None else ""

        return (
            f"InventoryState[{self.symbol}]: "
            f"Pos={self.current_inventory:.4f} ({self.inventory_percent:.0%}), "
            f"Risk={self.risk_category} ({self.risk_level}){pnl_str}"
        )

    def __repr__(self) -> str:
        return (
            f"InventoryState(symbol='{self.symbol}', inventory={self.current_inventory:.4f}, "
            f"risk={self.risk_category})"
        )
