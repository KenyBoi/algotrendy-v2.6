"""
Avellaneda-Stoikov Trading Signal
==================================
Trading signal with bid/ask prices and quantities.

Mirrors C# implementation in:
backend/AlgoTrendy.TradingEngine/Models/MarketMaking/ASSignal.cs

Contains:
- Bid/Ask prices and quantities
- Reservation price and optimal spread
- Validation and confidence scoring
"""

from dataclasses import dataclass
from datetime import datetime
from typing import Optional
import json


@dataclass
class ASSignal:
    """
    Avellaneda-Stoikov trading signal.
    Contains bid/ask prices and quantities calculated by the strategy.
    """

    symbol: str
    """Symbol for this signal"""

    timestamp: datetime
    """Timestamp when signal was generated"""

    bid_price: float
    """Bid price (price we want to buy at)"""

    ask_price: float
    """Ask price (price we want to sell at)"""

    bid_quantity: float
    """Bid quantity (amount we want to buy)"""

    ask_quantity: float
    """Ask quantity (amount we want to sell)"""

    reservation_price: Optional[float] = None
    """Reservation price (optimal mid price based on inventory)"""

    optimal_spread: Optional[float] = None
    """Optimal spread calculated by AS formula"""

    current_inventory: Optional[float] = None
    """Current inventory level when signal was generated"""

    confidence: float = 1.0
    """Confidence score (0.0 to 1.0). Higher = more confident"""

    is_valid: bool = True
    """Whether this signal is valid for execution"""

    invalid_reason: Optional[str] = None
    """Reason why signal is invalid (if is_valid = False)"""

    @property
    def spread(self) -> float:
        """Spread (ask - bid)"""
        return self.ask_price - self.bid_price

    @property
    def spread_percent(self) -> float:
        """Spread as percentage of mid price"""
        return self.spread / self.mid_price if self.mid_price > 0 else 0.0

    @property
    def mid_price(self) -> float:
        """Mid price ((bid + ask) / 2)"""
        return (self.bid_price + self.ask_price) / 2.0

    @property
    def spread_bps(self) -> float:
        """Spread in basis points (bps)"""
        return self.spread_percent * 10000.0

    @property
    def total_notional(self) -> float:
        """Total notional value (bid qty * bid price + ask qty * ask price)"""
        return (self.bid_quantity * self.bid_price) + (self.ask_quantity * self.ask_price)

    def validate_for_execution(
        self,
        min_spread_bps: float = 1.0,
        max_spread_bps: float = 1000.0
    ) -> bool:
        """
        Validates signal is executable.

        Args:
            min_spread_bps: Minimum allowed spread (bps)
            max_spread_bps: Maximum allowed spread (bps)

        Returns:
            True if signal is valid for execution
        """
        if not self.is_valid:
            return False

        if self.bid_price <= 0 or self.ask_price <= 0:
            return False

        if self.bid_quantity <= 0 or self.ask_quantity <= 0:
            return False

        if self.bid_price >= self.ask_price:
            return False  # Crossed market

        spread_bps = self.spread_bps
        if spread_bps < min_spread_bps or spread_bps > max_spread_bps:
            return False

        return True

    @classmethod
    def create_invalid(cls, symbol: str, reason: str) -> 'ASSignal':
        """
        Creates an invalid signal with reason.

        Args:
            symbol: Trading symbol
            reason: Reason for invalidity

        Returns:
            Invalid ASSignal
        """
        return cls(
            symbol=symbol,
            timestamp=datetime.utcnow(),
            bid_price=0.0,
            ask_price=0.0,
            bid_quantity=0.0,
            ask_quantity=0.0,
            is_valid=False,
            invalid_reason=reason,
            confidence=0.0
        )

    def to_dict(self) -> dict:
        """
        Convert to dictionary for JSON serialization.

        Returns:
            Dictionary representation
        """
        return {
            "symbol": self.symbol,
            "timestamp": self.timestamp.isoformat(),
            "bid_price": self.bid_price,
            "ask_price": self.ask_price,
            "bid_quantity": self.bid_quantity,
            "ask_quantity": self.ask_quantity,
            "reservation_price": self.reservation_price,
            "optimal_spread": self.optimal_spread,
            "current_inventory": self.current_inventory,
            "confidence": self.confidence,
            "is_valid": self.is_valid,
            "invalid_reason": self.invalid_reason,
            # Computed properties
            "spread": self.spread,
            "spread_percent": self.spread_percent,
            "spread_bps": self.spread_bps,
            "mid_price": self.mid_price,
            "total_notional": self.total_notional
        }

    def to_json(self) -> str:
        """
        Convert to JSON string.

        Returns:
            JSON string representation
        """
        return json.dumps(self.to_dict(), indent=2)

    @classmethod
    def from_dict(cls, data: dict) -> 'ASSignal':
        """
        Create ASSignal from dictionary.

        Args:
            data: Dictionary with signal data

        Returns:
            ASSignal instance
        """
        # Parse timestamp
        timestamp = data['timestamp']
        if isinstance(timestamp, str):
            timestamp = datetime.fromisoformat(timestamp.replace('Z', '+00:00'))

        return cls(
            symbol=data['symbol'],
            timestamp=timestamp,
            bid_price=data['bid_price'],
            ask_price=data['ask_price'],
            bid_quantity=data['bid_quantity'],
            ask_quantity=data['ask_quantity'],
            reservation_price=data.get('reservation_price'),
            optimal_spread=data.get('optimal_spread'),
            current_inventory=data.get('current_inventory'),
            confidence=data.get('confidence', 1.0),
            is_valid=data.get('is_valid', True),
            invalid_reason=data.get('invalid_reason')
        )

    @classmethod
    def from_json(cls, json_str: str) -> 'ASSignal':
        """
        Create ASSignal from JSON string.

        Args:
            json_str: JSON string

        Returns:
            ASSignal instance
        """
        data = json.loads(json_str)
        return cls.from_dict(data)

    def clone(self) -> 'ASSignal':
        """
        Deep clone.

        Returns:
            Copy of ASSignal
        """
        return ASSignal(
            symbol=self.symbol,
            timestamp=self.timestamp,
            bid_price=self.bid_price,
            ask_price=self.ask_price,
            bid_quantity=self.bid_quantity,
            ask_quantity=self.ask_quantity,
            reservation_price=self.reservation_price,
            optimal_spread=self.optimal_spread,
            current_inventory=self.current_inventory,
            confidence=self.confidence,
            is_valid=self.is_valid,
            invalid_reason=self.invalid_reason
        )

    def __str__(self) -> str:
        if not self.is_valid:
            return f"ASSignal[{self.symbol}]: INVALID - {self.invalid_reason}"

        return (
            f"ASSignal[{self.symbol}]: "
            f"Bid={self.bid_price:.2f}@{self.bid_quantity:.4f}, "
            f"Ask={self.ask_price:.2f}@{self.ask_quantity:.4f}, "
            f"Spread={self.spread_bps:.1f}bps, Conf={self.confidence:.0%}"
        )

    def __repr__(self) -> str:
        return (
            f"ASSignal(symbol='{self.symbol}', bid={self.bid_price:.2f}, "
            f"ask={self.ask_price:.2f}, spread={self.spread_bps:.1f}bps)"
        )
