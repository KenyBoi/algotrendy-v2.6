"""
Reinforcement Learning Action
==============================
Action representation for RL agent.

Mirrors C# implementation in:
backend/AlgoTrendy.TradingEngine/Models/ReinforcementLearning/RLAction.cs

Action space:
- Discrete: 9 actions (0-8) with predefined parameter adjustments
- Continuous: Direct parameter control (gamma, skew, inventory, size)
"""

from dataclasses import dataclass
from datetime import datetime
from typing import List
import json


@dataclass
class RLAction:
    """
    Reinforcement Learning action.
    Represents the action taken by the RL agent to adjust market making parameters.
    """

    action_id: int
    """Action ID (0 to ActionSpaceSize - 1). Used for discrete action spaces."""

    gamma_multiplier: float = 1.0
    """Gamma adjustment (risk aversion multiplier). Range: 0.5-2.0. 1.0=no change"""

    spread_skew: float = 0.0
    """Spread skew (asymmetric bid/ask). Range: -1.0 to +1.0. 0=symmetric"""

    inventory_target_adjustment: float = 0.0
    """Inventory target adjustment. Range: -1.0 to +1.0 (% of max inventory)"""

    size_multiplier: float = 1.0
    """Size multiplier for quote quantities. Range: 0.1-2.0. 1.0=normal"""

    timestamp: datetime = None
    """Action taken timestamp"""

    def __post_init__(self):
        if self.timestamp is None:
            self.timestamp = datetime.utcnow()

    @property
    def is_noop(self) -> bool:
        """Is this a 'do nothing' action"""
        return (
            self.action_id == 0 and
            self.gamma_multiplier == 1.0 and
            self.spread_skew == 0.0 and
            self.inventory_target_adjustment == 0.0 and
            self.size_multiplier == 1.0
        )

    # Action space size for discrete actions
    DEFAULT_ACTION_SPACE_SIZE = 9

    @classmethod
    def from_action_id(cls, action_id: int) -> 'RLAction':
        """
        Creates a discrete action from action ID.
        Maps discrete actions to continuous parameter adjustments.

        Action space (9 actions):
        0: No-op (do nothing)
        1: Increase gamma (more conservative)
        2: Decrease gamma (more aggressive)
        3: Skew bid spread wider (reduce buying)
        4: Skew ask spread wider (reduce selling)
        5: Shift target inventory long
        6: Shift target inventory short
        7: Increase quote size
        8: Decrease quote size

        Args:
            action_id: Discrete action ID (0-8)

        Returns:
            RLAction instance

        Raises:
            ValueError: If action_id is invalid
        """
        action_map = {
            0: {'gamma_multiplier': 1.0, 'spread_skew': 0.0, 'inventory_target_adjustment': 0.0, 'size_multiplier': 1.0},
            1: {'gamma_multiplier': 1.5, 'spread_skew': 0.0, 'inventory_target_adjustment': 0.0, 'size_multiplier': 1.0},
            2: {'gamma_multiplier': 0.7, 'spread_skew': 0.0, 'inventory_target_adjustment': 0.0, 'size_multiplier': 1.0},
            3: {'gamma_multiplier': 1.0, 'spread_skew': -0.3, 'inventory_target_adjustment': 0.0, 'size_multiplier': 1.0},
            4: {'gamma_multiplier': 1.0, 'spread_skew': 0.3, 'inventory_target_adjustment': 0.0, 'size_multiplier': 1.0},
            5: {'gamma_multiplier': 1.0, 'spread_skew': 0.0, 'inventory_target_adjustment': 0.2, 'size_multiplier': 1.0},
            6: {'gamma_multiplier': 1.0, 'spread_skew': 0.0, 'inventory_target_adjustment': -0.2, 'size_multiplier': 1.0},
            7: {'gamma_multiplier': 1.0, 'spread_skew': 0.0, 'inventory_target_adjustment': 0.0, 'size_multiplier': 1.5},
            8: {'gamma_multiplier': 1.0, 'spread_skew': 0.0, 'inventory_target_adjustment': 0.0, 'size_multiplier': 0.7},
        }

        if action_id not in action_map:
            raise ValueError(f"Invalid action ID: {action_id}. Must be 0-8.")

        params = action_map[action_id]
        return cls(action_id=action_id, **params)

    @classmethod
    def from_continuous(
        cls,
        gamma_multiplier: float = 1.0,
        spread_skew: float = 0.0,
        inventory_target_adjustment: float = 0.0,
        size_multiplier: float = 1.0
    ) -> 'RLAction':
        """
        Creates a continuous action from parameter values.

        Args:
            gamma_multiplier: Gamma multiplier (0.1-5.0)
            spread_skew: Spread skew (-1.0 to +1.0)
            inventory_target_adjustment: Inventory target adjustment (-1.0 to +1.0)
            size_multiplier: Size multiplier (0.1-3.0)

        Returns:
            RLAction instance
        """
        # Clamp values to valid ranges
        gamma_multiplier = max(0.1, min(5.0, gamma_multiplier))
        spread_skew = max(-1.0, min(1.0, spread_skew))
        inventory_target_adjustment = max(-1.0, min(1.0, inventory_target_adjustment))
        size_multiplier = max(0.1, min(3.0, size_multiplier))

        return cls(
            action_id=-1,  # Continuous action (no discrete ID)
            gamma_multiplier=gamma_multiplier,
            spread_skew=spread_skew,
            inventory_target_adjustment=inventory_target_adjustment,
            size_multiplier=size_multiplier
        )

    def get_description(self) -> str:
        """
        Gets action description.

        Returns:
            Human-readable description
        """
        if self.is_noop:
            return "No-op (maintain current parameters)"

        parts = []

        if self.gamma_multiplier != 1.0:
            parts.append(f"Gamma×{self.gamma_multiplier:.2f}")

        if self.spread_skew != 0.0:
            parts.append(f"Skew={self.spread_skew:+.1f}")

        if self.inventory_target_adjustment != 0.0:
            parts.append(f"InvTarget={self.inventory_target_adjustment:+.0%}")

        if self.size_multiplier != 1.0:
            parts.append(f"Size×{self.size_multiplier:.2f}")

        return ", ".join(parts)

    def to_dict(self) -> dict:
        """
        Convert to dictionary for JSON serialization.

        Returns:
            Dictionary representation
        """
        return {
            "action_id": self.action_id,
            "gamma_multiplier": self.gamma_multiplier,
            "spread_skew": self.spread_skew,
            "inventory_target_adjustment": self.inventory_target_adjustment,
            "size_multiplier": self.size_multiplier,
            "timestamp": self.timestamp.isoformat(),
            "is_noop": self.is_noop,
            "description": self.get_description()
        }

    def to_json(self) -> str:
        """
        Convert to JSON string.

        Returns:
            JSON string representation
        """
        return json.dumps(self.to_dict(), indent=2)

    @classmethod
    def from_dict(cls, data: dict) -> 'RLAction':
        """
        Create RLAction from dictionary.

        Args:
            data: Dictionary with action data

        Returns:
            RLAction instance
        """
        # Parse timestamp
        timestamp = data.get('timestamp')
        if timestamp and isinstance(timestamp, str):
            timestamp = datetime.fromisoformat(timestamp.replace('Z', '+00:00'))

        return cls(
            action_id=data['action_id'],
            gamma_multiplier=data.get('gamma_multiplier', 1.0),
            spread_skew=data.get('spread_skew', 0.0),
            inventory_target_adjustment=data.get('inventory_target_adjustment', 0.0),
            size_multiplier=data.get('size_multiplier', 1.0),
            timestamp=timestamp
        )

    @classmethod
    def from_json(cls, json_str: str) -> 'RLAction':
        """
        Create RLAction from JSON string.

        Args:
            json_str: JSON string

        Returns:
            RLAction instance
        """
        data = json.loads(json_str)
        return cls.from_dict(data)

    def clone(self) -> 'RLAction':
        """
        Deep clone.

        Returns:
            Copy of RLAction
        """
        return RLAction(
            action_id=self.action_id,
            gamma_multiplier=self.gamma_multiplier,
            spread_skew=self.spread_skew,
            inventory_target_adjustment=self.inventory_target_adjustment,
            size_multiplier=self.size_multiplier,
            timestamp=self.timestamp
        )

    def __str__(self) -> str:
        if self.action_id >= 0:
            return f"RLAction[Discrete] ID={self.action_id}: {self.get_description()}"
        else:
            return f"RLAction[Continuous]: {self.get_description()}"

    def __repr__(self) -> str:
        return f"RLAction(id={self.action_id}, {self.get_description()})"
