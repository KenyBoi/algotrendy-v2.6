"""
Avellaneda-Stoikov Parameters
==============================
Strategy parameters for market making algorithm.

Mirrors C# implementation in:
backend/AlgoTrendy.TradingEngine/Models/MarketMaking/ASParameters.cs

Key Parameters:
- gamma: Risk aversion coefficient
- kappa: Market liquidity parameter
- sigma: Asset volatility
- T: Trading horizon (time remaining)
- max_inventory: Maximum position size
"""

from dataclasses import dataclass, field
from typing import List
import json


@dataclass
class ASParameters:
    """
    Avellaneda-Stoikov strategy parameters.
    Controls market making behavior and risk management.
    """

    gamma: float
    """Risk aversion coefficient. Higher = more conservative (wider spreads). Range: 0.01-1.0"""

    kappa: float
    """Market liquidity parameter. Higher = faster fill rate assumption. Range: 0.1-10.0"""

    sigma: float
    """Volatility of the asset (annualized). Range: 0.1-2.0 (10%-200%)"""

    T: float
    """Trading horizon - time remaining in session. Normalized 0.0-1.0"""

    max_inventory: float
    """Maximum inventory (position size limit) in base currency"""

    target_inventory: float = 0.0
    """Target inventory (usually 0 for market neutral)"""

    min_spread_bps: float = 5.0
    """Minimum spread (bps) to maintain profitability"""

    max_spread_bps: float = 100.0
    """Maximum spread (bps) to stay competitive"""

    def is_valid(self) -> bool:
        """
        Validates parameters are within acceptable ranges.

        Returns:
            True if all parameters valid
        """
        return (
            0 < self.gamma <= 10.0 and
            0 < self.kappa <= 100.0 and
            0 < self.sigma <= 5.0 and
            0 <= self.T <= 1.0 and
            self.max_inventory > 0 and
            self.min_spread_bps >= 0 and
            self.max_spread_bps > self.min_spread_bps
        )

    def get_validation_errors(self) -> List[str]:
        """
        Gets validation errors (if any).

        Returns:
            List of validation error messages
        """
        errors = []

        if not (0 < self.gamma <= 10.0):
            errors.append(f"Gamma must be between 0 and 10.0 (got {self.gamma})")

        if not (0 < self.kappa <= 100.0):
            errors.append(f"Kappa must be between 0 and 100.0 (got {self.kappa})")

        if not (0 < self.sigma <= 5.0):
            errors.append(f"Sigma must be between 0 and 5.0 (got {self.sigma})")

        if not (0 <= self.T <= 1.0):
            errors.append(f"T must be between 0.0 and 1.0 (got {self.T})")

        if self.max_inventory <= 0:
            errors.append(f"MaxInventory must be positive (got {self.max_inventory})")

        if self.min_spread_bps < 0:
            errors.append(f"MinSpreadBps must be non-negative (got {self.min_spread_bps})")

        if self.max_spread_bps <= self.min_spread_bps:
            errors.append(
                f"MaxSpreadBps ({self.max_spread_bps}) must be greater than "
                f"MinSpreadBps ({self.min_spread_bps})"
            )

        return errors

    @classmethod
    def create_conservative(cls, max_inventory: float = 1.0) -> 'ASParameters':
        """
        Creates default conservative parameters for testing.

        Args:
            max_inventory: Maximum position size

        Returns:
            Conservative ASParameters instance
        """
        return cls(
            gamma=0.1,             # Low risk aversion (moderate spreads)
            kappa=1.5,             # Moderate liquidity assumption
            sigma=0.5,             # 50% annualized volatility
            T=1.0,                 # Full session remaining
            max_inventory=max_inventory,
            target_inventory=0.0,
            min_spread_bps=10.0,   # 0.1% minimum spread
            max_spread_bps=50.0    # 0.5% maximum spread
        )

    @classmethod
    def create_aggressive(cls, max_inventory: float = 1.0) -> 'ASParameters':
        """
        Creates aggressive parameters for high-frequency trading.

        Args:
            max_inventory: Maximum position size

        Returns:
            Aggressive ASParameters instance
        """
        return cls(
            gamma=0.01,            # Very low risk aversion (tight spreads)
            kappa=5.0,             # High liquidity assumption
            sigma=0.3,             # 30% annualized volatility
            T=1.0,                 # Full session remaining
            max_inventory=max_inventory,
            target_inventory=0.0,
            min_spread_bps=2.0,    # 0.02% minimum spread
            max_spread_bps=20.0    # 0.2% maximum spread
        )

    def to_dict(self) -> dict:
        """
        Convert to dictionary for JSON serialization.

        Returns:
            Dictionary representation
        """
        return {
            "gamma": self.gamma,
            "kappa": self.kappa,
            "sigma": self.sigma,
            "T": self.T,
            "max_inventory": self.max_inventory,
            "target_inventory": self.target_inventory,
            "min_spread_bps": self.min_spread_bps,
            "max_spread_bps": self.max_spread_bps
        }

    def to_json(self) -> str:
        """
        Convert to JSON string.

        Returns:
            JSON string representation
        """
        return json.dumps(self.to_dict(), indent=2)

    @classmethod
    def from_dict(cls, data: dict) -> 'ASParameters':
        """
        Create ASParameters from dictionary.

        Args:
            data: Dictionary with parameter data

        Returns:
            ASParameters instance
        """
        return cls(
            gamma=data['gamma'],
            kappa=data['kappa'],
            sigma=data['sigma'],
            T=data['T'],
            max_inventory=data['max_inventory'],
            target_inventory=data.get('target_inventory', 0.0),
            min_spread_bps=data.get('min_spread_bps', 5.0),
            max_spread_bps=data.get('max_spread_bps', 100.0)
        )

    @classmethod
    def from_json(cls, json_str: str) -> 'ASParameters':
        """
        Create ASParameters from JSON string.

        Args:
            json_str: JSON string

        Returns:
            ASParameters instance
        """
        data = json.loads(json_str)
        return cls.from_dict(data)

    def clone(self) -> 'ASParameters':
        """
        Deep clone.

        Returns:
            Copy of ASParameters
        """
        return ASParameters(
            gamma=self.gamma,
            kappa=self.kappa,
            sigma=self.sigma,
            T=self.T,
            max_inventory=self.max_inventory,
            target_inventory=self.target_inventory,
            min_spread_bps=self.min_spread_bps,
            max_spread_bps=self.max_spread_bps
        )

    def __str__(self) -> str:
        return (
            f"ASParameters: γ={self.gamma:.3f}, κ={self.kappa:.2f}, σ={self.sigma:.2f}, "
            f"T={self.T:.2f}, MaxInv={self.max_inventory:.4f}, "
            f"Spread=[{self.min_spread_bps:.1f}-{self.max_spread_bps:.1f}] bps"
        )

    def __repr__(self) -> str:
        return (
            f"ASParameters(gamma={self.gamma:.3f}, kappa={self.kappa:.2f}, "
            f"sigma={self.sigma:.2f}, T={self.T:.2f})"
        )
