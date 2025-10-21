"""
Reinforcement Learning Transition
==================================
State transition representation (SARS: State, Action, Reward, next State).

Mirrors C# implementation in:
backend/AlgoTrendy.TradingEngine/Models/ReinforcementLearning/RLTransition.cs

Used for:
- Training RL agents
- Storing in replay buffer
- Experience replay
"""

from dataclasses import dataclass
from datetime import datetime
from typing import Optional, Dict, Any, Tuple
import numpy as np
import json

from .rl_state import RLState
from .rl_action import RLAction
from .rl_reward import RLReward


@dataclass
class RLTransition:
    """
    Reinforcement Learning transition.
    Represents a single experience tuple (State, Action, Reward, Next State, Done).
    Used for training RL agents and storing in replay buffer.
    """

    state: RLState
    """Current state (before action)"""

    action: RLAction
    """Action taken in current state"""

    reward: RLReward
    """Reward received after taking action"""

    next_state: RLState
    """Next state (after action)"""

    done: bool
    """Is next state terminal (episode ended)"""

    timestamp: datetime = None
    """Timestamp when transition occurred"""

    priority: Optional[float] = None
    """Priority for prioritized experience replay (higher = more important)"""

    metadata: Optional[Dict[str, Any]] = None
    """Additional metadata"""

    def __post_init__(self):
        if self.timestamp is None:
            self.timestamp = datetime.utcnow()

    @classmethod
    def create(
        cls,
        state: RLState,
        action: RLAction,
        reward: RLReward,
        next_state: RLState,
        done: bool
    ) -> 'RLTransition':
        """
        Creates a transition from components.

        Args:
            state: Current state
            action: Action taken
            reward: Reward received
            next_state: Next state
            done: Is episode done

        Returns:
            RLTransition instance
        """
        return cls(
            state=state,
            action=action,
            reward=reward,
            next_state=next_state,
            done=done,
            timestamp=datetime.utcnow()
        )

    @classmethod
    def create_with_priority(
        cls,
        state: RLState,
        action: RLAction,
        reward: RLReward,
        next_state: RLState,
        done: bool,
        priority: float
    ) -> 'RLTransition':
        """
        Creates a transition with priority (for prioritized replay).

        Args:
            state: Current state
            action: Action taken
            reward: Reward received
            next_state: Next state
            done: Is episode done
            priority: Priority value

        Returns:
            RLTransition instance
        """
        return cls(
            state=state,
            action=action,
            reward=reward,
            next_state=next_state,
            done=done,
            timestamp=datetime.utcnow(),
            priority=priority
        )

    def calculate_td_error(self) -> float:
        """
        Calculates TD error (Temporal Difference error) for prioritization.
        TD Error = |reward + gamma * max_Q(next_state) - Q(state, action)|
        Simplified version just uses reward magnitude.

        Returns:
            TD error estimate
        """
        # Simplified: use absolute reward as proxy for TD error
        # Real implementation would require Q-network values
        return abs(self.reward.total_reward)

    def to_arrays(self) -> Tuple[np.ndarray, int, float, np.ndarray, bool]:
        """
        Gets transition as arrays for neural network training.

        Returns:
            Tuple of (state_array, action_id, reward, next_state_array, done)
        """
        return (
            self.state.to_array(),
            self.action.action_id,
            self.reward.normalized_reward,
            self.next_state.to_array(),
            self.done
        )

    def to_extended_arrays(self) -> Tuple[np.ndarray, int, float, np.ndarray, bool]:
        """
        Gets extended transition arrays (with context).

        Returns:
            Tuple of extended arrays
        """
        return (
            self.state.to_extended_array(),
            self.action.action_id,
            self.reward.normalized_reward,
            self.next_state.to_extended_array(),
            self.done
        )

    def to_dict(self) -> dict:
        """
        Convert to dictionary for JSON serialization.

        Returns:
            Dictionary representation
        """
        return {
            "state": self.state.to_dict(),
            "action": self.action.to_dict(),
            "reward": self.reward.to_dict(),
            "next_state": self.next_state.to_dict(),
            "done": self.done,
            "timestamp": self.timestamp.isoformat(),
            "priority": self.priority,
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
    def from_dict(cls, data: dict) -> 'RLTransition':
        """
        Create RLTransition from dictionary.

        Args:
            data: Dictionary with transition data

        Returns:
            RLTransition instance
        """
        # Parse timestamp
        timestamp = data.get('timestamp')
        if timestamp and isinstance(timestamp, str):
            timestamp = datetime.fromisoformat(timestamp.replace('Z', '+00:00'))

        return cls(
            state=RLState.from_dict(data['state']),
            action=RLAction.from_dict(data['action']),
            reward=RLReward.from_dict(data['reward']),
            next_state=RLState.from_dict(data['next_state']),
            done=data['done'],
            timestamp=timestamp,
            priority=data.get('priority'),
            metadata=data.get('metadata')
        )

    @classmethod
    def from_json(cls, json_str: str) -> 'RLTransition':
        """
        Create RLTransition from JSON string.

        Args:
            json_str: JSON string

        Returns:
            RLTransition instance
        """
        data = json.loads(json_str)
        return cls.from_dict(data)

    def clone(self) -> 'RLTransition':
        """
        Deep clone.

        Returns:
            Copy of RLTransition
        """
        return RLTransition(
            state=self.state.clone(),
            action=self.action.clone(),
            reward=self.reward.clone(),
            next_state=self.next_state.clone(),
            done=self.done,
            timestamp=self.timestamp,
            priority=self.priority,
            metadata=dict(self.metadata) if self.metadata else None
        )

    def __str__(self) -> str:
        done_str = " [DONE]" if self.done else ""
        priority_str = f", Pri={self.priority:.2f}" if self.priority is not None else ""

        return (
            f"RLTransition: "
            f"S[Step={self.state.step_number}] -> "
            f"A[{self.action.action_id}] -> "
            f"R[{self.reward.total_reward:.4f}] -> "
            f"S'[Step={self.next_state.step_number}]{done_str}{priority_str}"
        )

    def __repr__(self) -> str:
        return (
            f"RLTransition(state_step={self.state.step_number}, "
            f"action={self.action.action_id}, reward={self.reward.total_reward:.4f}, "
            f"done={self.done})"
        )
