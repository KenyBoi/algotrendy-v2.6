"""
Order Book Models
=================
Level 2 order book data structures for market making.

Mirrors C# implementation in:
backend/AlgoTrendy.TradingEngine/Models/MarketMaking/OrderBookSnapshot.cs
"""

from dataclasses import dataclass, field
from datetime import datetime
from typing import List, Optional
import json


@dataclass
class OrderBookLevel:
    """
    Represents a single price level in the order book (Level 2 data).

    Attributes:
        price: Price at this level
        quantity: Total quantity available at this price level
        order_count: Number of orders at this price level (optional)
    """

    price: float
    quantity: float
    order_count: Optional[int] = None

    @property
    def value(self) -> float:
        """Dollar value at this level (price * quantity)"""
        return self.price * self.quantity

    def clone(self) -> 'OrderBookLevel':
        """Creates a copy of this order book level"""
        return OrderBookLevel(
            price=self.price,
            quantity=self.quantity,
            order_count=self.order_count
        )

    def to_dict(self) -> dict:
        """Convert to dictionary for JSON serialization"""
        return {
            'price': self.price,
            'quantity': self.quantity,
            'order_count': self.order_count,
            'value': self.value
        }

    def __str__(self) -> str:
        return f"{self.price:.8f} @ {self.quantity:.4f} (${self.value:.2f})"


@dataclass
class OrderBookSnapshot:
    """
    Represents a Level 2 order book snapshot with bid and ask levels.
    Used for market making strategies (Avellaneda-Stoikov).

    Attributes:
        symbol: Trading symbol (e.g., "BTCUSDT")
        exchange: Exchange providing the order book data
        timestamp: Timestamp of the snapshot (UTC)
        bids: Bid levels (sorted by price descending)
        asks: Ask levels (sorted by price ascending)
    """

    symbol: str
    exchange: str
    timestamp: datetime
    bids: List[OrderBookLevel] = field(default_factory=list)
    asks: List[OrderBookLevel] = field(default_factory=list)

    def __post_init__(self):
        """Validate after initialization"""
        # Ensure bids and asks are lists of OrderBookLevel
        if self.bids and not isinstance(self.bids[0], OrderBookLevel):
            # Convert from dict if needed
            self.bids = [OrderBookLevel(**b) if isinstance(b, dict) else b for b in self.bids]
        if self.asks and not isinstance(self.asks[0], OrderBookLevel):
            self.asks = [OrderBookLevel(**a) if isinstance(a, dict) else a for a in self.asks]

    @property
    def best_bid(self) -> float:
        """Best bid price (highest buy price)"""
        return self.bids[0].price if self.bids else 0.0

    @property
    def best_ask(self) -> float:
        """Best ask price (lowest sell price)"""
        return self.asks[0].price if self.asks else 0.0

    @property
    def spread(self) -> float:
        """Bid-ask spread (absolute)"""
        return self.best_ask - self.best_bid

    @property
    def spread_percent(self) -> float:
        """Bid-ask spread as percentage of mid price"""
        return self.spread / self.mid_price if self.mid_price > 0 else 0.0

    @property
    def mid_price(self) -> float:
        """Mid price (simple average of best bid and ask)"""
        return (self.best_bid + self.best_ask) / 2

    @property
    def microprice(self) -> float:
        """
        Microprice (volume-weighted mid price).
        More accurate than simple mid price for predicting next trade price.

        Formula: (BestBid * AskVolume + BestAsk * BidVolume) / (BidVolume + AskVolume)
        """
        if not self.bids or not self.asks:
            return self.mid_price

        bid_volume = self.bids[0].quantity
        ask_volume = self.asks[0].quantity

        if bid_volume + ask_volume == 0:
            return self.mid_price

        return (self.best_bid * ask_volume + self.best_ask * bid_volume) / (bid_volume + ask_volume)

    def get_bid_depth(self, levels: int = 5) -> float:
        """
        Total bid depth (dollar value) at N levels.

        Args:
            levels: Number of levels to include (default 5)

        Returns:
            Total dollar value of bids
        """
        return sum(b.value for b in self.bids[:levels])

    def get_ask_depth(self, levels: int = 5) -> float:
        """
        Total ask depth (dollar value) at N levels.

        Args:
            levels: Number of levels to include (default 5)

        Returns:
            Total dollar value of asks
        """
        return sum(a.value for a in self.asks[:levels])

    def get_total_depth(self, levels: int = 5) -> float:
        """
        Total depth (bid + ask) at N levels.

        Args:
            levels: Number of levels to include (default 5)

        Returns:
            Total dollar value of bids and asks
        """
        return self.get_bid_depth(levels) + self.get_ask_depth(levels)

    def get_order_book_imbalance(self, levels: int = 5) -> float:
        """
        Order book imbalance (OBI) at N levels.

        OBI = (BidVolume - AskVolume) / (BidVolume + AskVolume)
        Range: -1 (all asks) to +1 (all bids)
        Positive = buy pressure, Negative = sell pressure

        Args:
            levels: Number of levels to include (default 5)

        Returns:
            Order book imbalance ratio
        """
        bid_volume = sum(b.quantity for b in self.bids[:levels])
        ask_volume = sum(a.quantity for a in self.asks[:levels])

        if bid_volume + ask_volume == 0:
            return 0.0

        return (bid_volume - ask_volume) / (bid_volume + ask_volume)

    def get_weighted_mid_price(self, levels: int = 5) -> float:
        """
        Weighted mid price (volume-weighted across multiple levels).

        Args:
            levels: Number of levels to include

        Returns:
            Volume-weighted mid price
        """
        bid_volume = sum(b.quantity for b in self.bids[:levels])
        ask_volume = sum(a.quantity for a in self.asks[:levels])

        if bid_volume + ask_volume == 0:
            return self.mid_price

        bid_price = sum(b.price * b.quantity for b in self.bids[:levels]) / bid_volume if bid_volume > 0 else self.best_bid
        ask_price = sum(a.price * a.quantity for a in self.asks[:levels]) / ask_volume if ask_volume > 0 else self.best_ask

        return (bid_price * ask_volume + ask_price * bid_volume) / (bid_volume + ask_volume)

    def is_valid(self) -> bool:
        """
        Validates the order book snapshot.

        Returns:
            True if valid, False otherwise
        """
        # Must have at least one bid and one ask
        if not self.bids or not self.asks:
            return False

        # Best bid must be less than best ask (no crossed market)
        if self.best_bid >= self.best_ask:
            return False

        # Bids must be sorted descending
        for i in range(1, len(self.bids)):
            if self.bids[i].price >= self.bids[i - 1].price:
                return False

        # Asks must be sorted ascending
        for i in range(1, len(self.asks)):
            if self.asks[i].price <= self.asks[i - 1].price:
                return False

        return True

    def clone(self) -> 'OrderBookSnapshot':
        """Creates a deep copy of this order book snapshot"""
        return OrderBookSnapshot(
            symbol=self.symbol,
            exchange=self.exchange,
            timestamp=self.timestamp,
            bids=[b.clone() for b in self.bids],
            asks=[a.clone() for a in self.asks]
        )

    def to_dict(self) -> dict:
        """
        Convert to dictionary for JSON serialization.

        Returns:
            Dictionary representation
        """
        return {
            'symbol': self.symbol,
            'exchange': self.exchange,
            'timestamp': self.timestamp.isoformat(),
            'bids': [b.to_dict() for b in self.bids],
            'asks': [a.to_dict() for a in self.asks],
            'best_bid': self.best_bid,
            'best_ask': self.best_ask,
            'spread': self.spread,
            'spread_percent': self.spread_percent,
            'mid_price': self.mid_price,
            'microprice': self.microprice,
            'total_depth': self.get_total_depth(),
            'order_book_imbalance': self.get_order_book_imbalance()
        }

    def to_json(self) -> str:
        """
        Convert to JSON string.

        Returns:
            JSON string representation
        """
        return json.dumps(self.to_dict(), indent=2)

    @classmethod
    def from_dict(cls, data: dict) -> 'OrderBookSnapshot':
        """
        Create OrderBookSnapshot from dictionary.

        Args:
            data: Dictionary with order book data

        Returns:
            OrderBookSnapshot instance
        """
        # Handle timestamp
        timestamp = data.get('timestamp')
        if isinstance(timestamp, str):
            timestamp = datetime.fromisoformat(timestamp.replace('Z', '+00:00'))
        elif not isinstance(timestamp, datetime):
            timestamp = datetime.utcnow()

        # Handle bids and asks
        bids = [
            OrderBookLevel(**b) if isinstance(b, dict) else b
            for b in data.get('bids', [])
        ]
        asks = [
            OrderBookLevel(**a) if isinstance(a, dict) else a
            for a in data.get('asks', [])
        ]

        return cls(
            symbol=data['symbol'],
            exchange=data['exchange'],
            timestamp=timestamp,
            bids=bids,
            asks=asks
        )

    @classmethod
    def from_json(cls, json_str: str) -> 'OrderBookSnapshot':
        """
        Create OrderBookSnapshot from JSON string.

        Args:
            json_str: JSON string

        Returns:
            OrderBookSnapshot instance
        """
        data = json.loads(json_str)
        return cls.from_dict(data)

    def __str__(self) -> str:
        return (
            f"{self.symbol} @ {self.exchange} | "
            f"Spread: {self.spread:.8f} ({self.spread_percent:.2%}) | "
            f"Bid: {self.best_bid:.8f} | Ask: {self.best_ask:.8f} | "
            f"Micro: {self.microprice:.8f} | "
            f"Depth: ${self.get_total_depth():.2f} | "
            f"OBI: {self.get_order_book_imbalance():.2f}"
        )
