"""
Reinforcement Learning Reward
==============================
Reward calculation for RL agent.

Mirrors C# implementation in:
backend/AlgoTrendy.TradingEngine/Models/ReinforcementLearning/RLReward.cs

Reward components:
- P&L (profit/loss from trades)
- Inventory penalty (cost of risky positions)
- Spread penalty (cost of wide spreads)
- Volatility penalty (cost of high volatility exposure)
"""

from dataclasses import dataclass
from datetime import datetime
import json
import numpy as np


@dataclass
class RLReward:
    """
    Reinforcement Learning reward.
    Calculates reward signal for RL agent based on P&L and risk penalties.
    """

    timestamp: datetime
    """Timestamp when reward was calculated"""

    pnl: float
    """P&L component (profit/loss from trades)"""

    inventory_penalty: float = 0.0
    """Inventory penalty (cost of holding risky positions)"""

    spread_penalty: float = 0.0
    """Spread penalty (cost of wide spreads / missed fills)"""

    volatility_penalty: float = 0.0
    """Volatility penalty (cost of high volatility exposure)"""

    custom_penalty: float = 0.0
    """Additional custom penalties"""

    normalized_reward: float = 0.0
    """Normalized reward (scaled to [-1, 1] for stable learning)"""

    is_terminal: bool = False
    """Is this reward from a terminal state"""

    @property
    def total_reward(self) -> float:
        """
        Total reward (PnL - all penalties).
        This is the value used to train the RL agent.
        """
        return (
            self.pnl -
            self.inventory_penalty -
            self.spread_penalty -
            self.volatility_penalty -
            self.custom_penalty
        )

    @classmethod
    def calculate(
        cls,
        pnl: float,
        inventory: float,
        max_inventory: float,
        inventory_penalty_weight: float = 0.01
    ) -> 'RLReward':
        """
        Calculates reward from P&L and inventory state.

        Args:
            pnl: Profit/Loss
            inventory: Current inventory (position)
            max_inventory: Maximum allowed inventory
            inventory_penalty_weight: Weight for inventory penalty (default: 0.01)

        Returns:
            RLReward instance
        """
        # Inventory penalty: quadratic penalty for large positions
        # Penalty = weight * (inventory / maxInventory)^2
        inventory_ratio = inventory / max_inventory if max_inventory > 0 else 0.0
        inventory_penalty = inventory_penalty_weight * inventory_ratio * inventory_ratio

        total_reward = pnl - inventory_penalty

        # Simple normalization using tanh approximation
        normalized_reward = total_reward / (1.0 + abs(total_reward))

        return cls(
            timestamp=datetime.utcnow(),
            pnl=pnl,
            inventory_penalty=inventory_penalty,
            normalized_reward=normalized_reward,
            is_terminal=False
        )

    @classmethod
    def calculate_detailed(
        cls,
        pnl: float,
        inventory: float,
        max_inventory: float,
        spread: float,
        volatility: float,
        inventory_penalty_weight: float = 0.01,
        spread_penalty_weight: float = 0.005,
        volatility_penalty_weight: float = 0.002
    ) -> 'RLReward':
        """
        Calculates reward with full penalty breakdown.

        Args:
            pnl: Profit/Loss
            inventory: Current inventory
            max_inventory: Maximum allowed inventory
            spread: Current spread
            volatility: Current volatility
            inventory_penalty_weight: Inventory penalty weight
            spread_penalty_weight: Spread penalty weight
            volatility_penalty_weight: Volatility penalty weight

        Returns:
            RLReward instance
        """
        # Inventory penalty: quadratic
        inventory_ratio = inventory / max_inventory if max_inventory > 0 else 0.0
        inventory_penalty = inventory_penalty_weight * inventory_ratio * inventory_ratio

        # Spread penalty: linear (wider spreads = higher penalty for missed fills)
        spread_penalty = spread_penalty_weight * spread

        # Volatility penalty: linear (higher vol = higher risk)
        volatility_penalty = volatility_penalty_weight * volatility

        total_reward = pnl - inventory_penalty - spread_penalty - volatility_penalty

        # Normalization
        normalized_reward = total_reward / (1.0 + abs(total_reward))

        return cls(
            timestamp=datetime.utcnow(),
            pnl=pnl,
            inventory_penalty=inventory_penalty,
            spread_penalty=spread_penalty,
            volatility_penalty=volatility_penalty,
            normalized_reward=normalized_reward,
            is_terminal=False
        )

    @classmethod
    def create_terminal(
        cls,
        final_pnl: float,
        final_inventory: float,
        max_inventory: float
    ) -> 'RLReward':
        """
        Creates terminal reward (end of episode).

        Args:
            final_pnl: Final P&L for the episode
            final_inventory: Final inventory
            max_inventory: Maximum allowed inventory

        Returns:
            Terminal RLReward instance
        """
        # Terminal penalty: heavily penalize non-zero final inventory
        inventory_ratio = final_inventory / max_inventory if max_inventory > 0 else 0.0
        terminal_inventory_penalty = 0.1 * inventory_ratio * inventory_ratio

        total_reward = final_pnl - terminal_inventory_penalty
        normalized_reward = total_reward / (1.0 + abs(total_reward))

        return cls(
            timestamp=datetime.utcnow(),
            pnl=final_pnl,
            inventory_penalty=terminal_inventory_penalty,
            normalized_reward=normalized_reward,
            is_terminal=True
        )

    def to_dict(self) -> dict:
        """
        Convert to dictionary for JSON serialization.

        Returns:
            Dictionary representation
        """
        return {
            "timestamp": self.timestamp.isoformat(),
            "pnl": self.pnl,
            "inventory_penalty": self.inventory_penalty,
            "spread_penalty": self.spread_penalty,
            "volatility_penalty": self.volatility_penalty,
            "custom_penalty": self.custom_penalty,
            "total_reward": self.total_reward,
            "normalized_reward": self.normalized_reward,
            "is_terminal": self.is_terminal
        }

    def to_json(self) -> str:
        """
        Convert to JSON string.

        Returns:
            JSON string representation
        """
        return json.dumps(self.to_dict(), indent=2)

    @classmethod
    def from_dict(cls, data: dict) -> 'RLReward':
        """
        Create RLReward from dictionary.

        Args:
            data: Dictionary with reward data

        Returns:
            RLReward instance
        """
        # Parse timestamp
        timestamp = data['timestamp']
        if isinstance(timestamp, str):
            timestamp = datetime.fromisoformat(timestamp.replace('Z', '+00:00'))

        return cls(
            timestamp=timestamp,
            pnl=data['pnl'],
            inventory_penalty=data.get('inventory_penalty', 0.0),
            spread_penalty=data.get('spread_penalty', 0.0),
            volatility_penalty=data.get('volatility_penalty', 0.0),
            custom_penalty=data.get('custom_penalty', 0.0),
            normalized_reward=data.get('normalized_reward', 0.0),
            is_terminal=data.get('is_terminal', False)
        )

    @classmethod
    def from_json(cls, json_str: str) -> 'RLReward':
        """
        Create RLReward from JSON string.

        Args:
            json_str: JSON string

        Returns:
            RLReward instance
        """
        data = json.loads(json_str)
        return cls.from_dict(data)

    def clone(self) -> 'RLReward':
        """
        Deep clone.

        Returns:
            Copy of RLReward
        """
        return RLReward(
            timestamp=self.timestamp,
            pnl=self.pnl,
            inventory_penalty=self.inventory_penalty,
            spread_penalty=self.spread_penalty,
            volatility_penalty=self.volatility_penalty,
            custom_penalty=self.custom_penalty,
            normalized_reward=self.normalized_reward,
            is_terminal=self.is_terminal
        )

    def __str__(self) -> str:
        terminal_str = " [TERMINAL]" if self.is_terminal else ""
        return (
            f"RLReward: Total={self.total_reward:.4f} (PnL={self.pnl:.4f}, "
            f"InvPen={self.inventory_penalty:.4f}, Normalized={self.normalized_reward:.4f}){terminal_str}"
        )

    def __repr__(self) -> str:
        return f"RLReward(total={self.total_reward:.4f}, normalized={self.normalized_reward:.4f})"
