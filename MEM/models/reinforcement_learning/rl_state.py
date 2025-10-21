"""
Reinforcement Learning State
=============================
State representation for RL agent.

Mirrors C# implementation in:
backend/AlgoTrendy.TradingEngine/Models/ReinforcementLearning/RLState.cs

Contains:
- 22 features from ASFeatures
- Inventory state
- Episode context (time remaining, step number)
"""

from dataclasses import dataclass, field
from datetime import datetime
from typing import Optional, Dict, Any
import numpy as np
import json

from ..market_making import ASFeatures, InventoryState


@dataclass
class RLState:
    """
    Reinforcement Learning state representation.
    Contains all features and context needed by the RL agent to make decisions.
    """

    symbol: str
    """Symbol being traded"""

    timestamp: datetime
    """Timestamp of this state"""

    features: ASFeatures
    """Features for RL agent (22 features)"""

    inventory: InventoryState
    """Current inventory state"""

    current_price: float
    """Current market price (mid price from order book)"""

    time_remaining: float = 1.0
    """Time remaining in trading session (0.0 to 1.0). 1.0=start, 0.0=end"""

    step_number: int = 0
    """Episode step number (increments each action)"""

    is_terminal: bool = False
    """Is this a terminal state (end of episode)"""

    metadata: Optional[Dict[str, Any]] = None
    """Additional metadata (optional)"""

    # Constants
    STATE_DIMENSION: int = field(default=22, init=False, repr=False)
    """State dimensionality (should always be 22)"""

    def to_array(self) -> np.ndarray:
        """
        Converts state to array for RL agent input.
        Uses ASFeatures.to_array() - returns 22 features.

        Returns:
            Array of 22 state features
        """
        return self.features.to_array()

    def to_extended_array(self) -> np.ndarray:
        """
        Gets extended state array including additional context.
        22 features + 3 context = 25 dimensions.

        Returns:
            Extended state array (25 dimensions)
        """
        features = self.features.to_array()

        # Add context (3 additional dimensions)
        context = np.array([
            self.time_remaining,
            self.inventory.risk_level / 100.0,  # Normalized 0-1
            float(self.step_number)
        ], dtype=np.float32)

        return np.concatenate([features, context])

    @classmethod
    def create(
        cls,
        symbol: str,
        features: ASFeatures,
        inventory: InventoryState,
        current_price: float,
        time_remaining: float = 1.0,
        step_number: int = 0
    ) -> 'RLState':
        """
        Creates RLState from ASFeatures and InventoryState.

        Args:
            symbol: Trading symbol
            features: AS features
            inventory: Inventory state
            current_price: Current market price
            time_remaining: Time remaining in session (default: 1.0)
            step_number: Episode step number (default: 0)

        Returns:
            New RLState instance
        """
        return cls(
            symbol=symbol,
            timestamp=datetime.utcnow(),
            features=features,
            inventory=inventory,
            current_price=current_price,
            time_remaining=time_remaining,
            step_number=step_number,
            is_terminal=False
        )

    @classmethod
    def create_terminal(cls, current_state: 'RLState') -> 'RLState':
        """
        Creates a terminal state (end of episode).

        Args:
            current_state: Current state to mark as terminal

        Returns:
            Terminal state
        """
        return cls(
            symbol=current_state.symbol,
            timestamp=datetime.utcnow(),
            features=current_state.features,
            inventory=current_state.inventory,
            current_price=current_state.current_price,
            time_remaining=0.0,
            step_number=current_state.step_number,
            is_terminal=True,
            metadata=current_state.metadata
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
            "features": self.features.to_dict(),
            "inventory": self.inventory.to_dict(),
            "current_price": self.current_price,
            "time_remaining": self.time_remaining,
            "step_number": self.step_number,
            "is_terminal": self.is_terminal,
            "metadata": self.metadata
        }

    def to_json(self) -> str:
        """
        Convert to JSON string.

        Returns:
            JSON string representation
        """
        return json.dumps(self.to_dict(), indent=2)

    @classmethod
    def from_dict(cls, data: dict) -> 'RLState':
        """
        Create RLState from dictionary.

        Args:
            data: Dictionary with state data

        Returns:
            RLState instance
        """
        # Parse timestamp
        timestamp = data['timestamp']
        if isinstance(timestamp, str):
            timestamp = datetime.fromisoformat(timestamp.replace('Z', '+00:00'))

        return cls(
            symbol=data['symbol'],
            timestamp=timestamp,
            features=ASFeatures.from_dict(data['features']),
            inventory=InventoryState.from_dict(data['inventory']),
            current_price=data['current_price'],
            time_remaining=data.get('time_remaining', 1.0),
            step_number=data.get('step_number', 0),
            is_terminal=data.get('is_terminal', False),
            metadata=data.get('metadata')
        )

    @classmethod
    def from_json(cls, json_str: str) -> 'RLState':
        """
        Create RLState from JSON string.

        Args:
            json_str: JSON string

        Returns:
            RLState instance
        """
        data = json.loads(json_str)
        return cls.from_dict(data)

    def clone(self) -> 'RLState':
        """
        Deep clone.

        Returns:
            Copy of RLState
        """
        return RLState(
            symbol=self.symbol,
            timestamp=self.timestamp,
            features=self.features,  # ASFeatures is immutable (dataclass)
            inventory=self.inventory.clone(),
            current_price=self.current_price,
            time_remaining=self.time_remaining,
            step_number=self.step_number,
            is_terminal=self.is_terminal,
            metadata=dict(self.metadata) if self.metadata else None
        )

    def __str__(self) -> str:
        terminal_str = " [TERMINAL]" if self.is_terminal else ""
        return (
            f"RLState[{self.symbol}] Step={self.step_number}, "
            f"Price={self.current_price:.2f}, Inv={self.inventory.inventory_percent:.0%}, "
            f"Risk={self.inventory.risk_category}, T={self.time_remaining:.0%}{terminal_str}"
        )

    def __repr__(self) -> str:
        return (
            f"RLState(symbol='{self.symbol}', step={self.step_number}, "
            f"price={self.current_price:.2f}, terminal={self.is_terminal})"
        )
