"""
Market Regime Detection System
Detects volatility, trend, and liquidity regimes
"""

import pandas as pd
import numpy as np
from typing import Dict, Tuple
from dataclasses import dataclass
from enum import Enum

class VolatilityRegime(Enum):
    LOW = "low_volatility"
    NORMAL = "normal_volatility"
    HIGH = "high_volatility"

class TrendRegime(Enum):
    TRENDING_UP = "trending_up"
    RANGING = "ranging"
    TRENDING_DOWN = "trending_down"

class LiquidityRegime(Enum):
    HIGH = "high_liquidity"
    NORMAL = "normal_liquidity"
    LOW = "low_liquidity"

@dataclass
class MarketRegime:
    """Complete market regime state"""
    volatility: VolatilityRegime
    trend: TrendRegime
    liquidity: LiquidityRegime
    position_size_multiplier: float
    confidence_threshold: float
    timestamp: pd.Timestamp

    def __str__(self):
        return f"{self.trend.value} | {self.volatility.value} | {self.liquidity.value}"


class RegimeDetector:
    """Detects market regimes for adaptive trading"""

    def __init__(
        self,
        vol_lookback: int = 20,
        trend_fast: int = 20,
        trend_slow: int = 50
    ):
        self.vol_lookback = vol_lookback
        self.trend_fast = trend_fast
        self.trend_slow = trend_slow

    def detect_volatility_regime(
        self,
        returns: pd.Series
    ) -> VolatilityRegime:
        """
        Detect volatility regime using rolling std

        Args:
            returns: Price returns series

        Returns:
            VolatilityRegime
        """
        # Current volatility (short-term)
        current_vol = returns.rolling(self.vol_lookback).std().iloc[-1]

        # Historical volatility (long-term)
        historical_vol = returns.rolling(252).std().iloc[-1]

        # Regime classification
        if current_vol > historical_vol * 1.5:
            return VolatilityRegime.HIGH
        elif current_vol < historical_vol * 0.5:
            return VolatilityRegime.LOW
        else:
            return VolatilityRegime.NORMAL

    def detect_trend_regime(
        self,
        prices: pd.Series
    ) -> TrendRegime:
        """
        Detect trend regime using moving averages

        Args:
            prices: Price series

        Returns:
            TrendRegime
        """
        # Fast and slow SMAs
        sma_fast = prices.rolling(self.trend_fast).mean().iloc[-1]
        sma_slow = prices.rolling(self.trend_slow).mean().iloc[-1]

        # Trend strength
        price = prices.iloc[-1]
        trend_strength = abs(sma_fast - sma_slow) / sma_slow

        # Classification
        if sma_fast > sma_slow and trend_strength > 0.02:
            return TrendRegime.TRENDING_UP
        elif sma_fast < sma_slow and trend_strength > 0.02:
            return TrendRegime.TRENDING_DOWN
        else:
            return TrendRegime.RANGING

    def detect_liquidity_regime(
        self,
        volume: pd.Series
    ) -> LiquidityRegime:
        """
        Detect liquidity regime using volume

        Args:
            volume: Volume series

        Returns:
            LiquidityRegime
        """
        # Current volume vs average
        current_vol = volume.iloc[-1]
        avg_vol = volume.rolling(24).mean().iloc[-1]

        # Classification
        if current_vol > avg_vol * 1.5:
            return LiquidityRegime.HIGH
        elif current_vol < avg_vol * 0.5:
            return LiquidityRegime.LOW
        else:
            return LiquidityRegime.NORMAL

    def get_regime_multipliers(
        self,
        regime: MarketRegime
    ) -> Dict[str, float]:
        """
        Get trading parameter multipliers for current regime

        Returns:
            {
                'position_size': float,  # Multiply base position size
                'confidence_threshold': float,  # Required confidence to trade
                'stop_loss': float,  # Multiply stop loss distance
            }
        """
        # Default multipliers
        multipliers = {
            'position_size': 1.0,
            'confidence_threshold': 0.6,
            'stop_loss': 1.0
        }

        # Volatility adjustments
        if regime.volatility == VolatilityRegime.HIGH:
            multipliers['position_size'] *= 0.5  # Half size in high vol
            multipliers['confidence_threshold'] = 0.75  # Need higher confidence
            multipliers['stop_loss'] *= 1.5  # Wider stops
        elif regime.volatility == VolatilityRegime.LOW:
            multipliers['position_size'] *= 1.2  # Slightly larger in low vol
            multipliers['confidence_threshold'] = 0.5  # Can trade lower confidence

        # Trend adjustments
        if regime.trend == TrendRegime.RANGING:
            multipliers['position_size'] *= 0.8  # Smaller in ranging
            multipliers['confidence_threshold'] = 0.7  # Need higher confidence
        elif regime.trend in [TrendRegime.TRENDING_UP, TrendRegime.TRENDING_DOWN]:
            multipliers['position_size'] *= 1.2  # Larger in trending

        # Liquidity adjustments
        if regime.liquidity == LiquidityRegime.LOW:
            multipliers['position_size'] *= 0.6  # Much smaller in low liquidity
            multipliers['confidence_threshold'] = 0.8  # Very high confidence needed

        return multipliers

    def detect_regime(
        self,
        prices: pd.Series,
        returns: pd.Series,
        volume: pd.Series
    ) -> MarketRegime:
        """
        Detect complete market regime

        Args:
            prices: Price series
            returns: Return series
            volume: Volume series

        Returns:
            MarketRegime
        """
        vol_regime = self.detect_volatility_regime(returns)
        trend_regime = self.detect_trend_regime(prices)
        liquidity_regime = self.detect_liquidity_regime(volume)

        regime = MarketRegime(
            volatility=vol_regime,
            trend=trend_regime,
            liquidity=liquidity_regime,
            position_size_multiplier=1.0,
            confidence_threshold=0.6,
            timestamp=prices.index[-1] if isinstance(prices.index, pd.DatetimeIndex) else pd.Timestamp.now()
        )

        # Get multipliers
        multipliers = self.get_regime_multipliers(regime)
        regime.position_size_multiplier = multipliers['position_size']
        regime.confidence_threshold = multipliers['confidence_threshold']

        return regime


# Example usage
if __name__ == "__main__":
    # Generate sample data
    np.random.seed(42)
    dates = pd.date_range('2024-01-01', periods=300, freq='1h')

    # Simulate price data
    returns = np.random.normal(0.0001, 0.02, 300)
    prices = pd.Series((1 + returns).cumprod() * 66000, index=dates)
    volume = pd.Series(np.random.uniform(500000, 2000000, 300), index=dates)
    returns_series = pd.Series(returns, index=dates)

    # Detect regime
    detector = RegimeDetector()
    regime = detector.detect_regime(prices, returns_series, volume)

    print(f"Current Regime: {regime}")
    print(f"Position Size Multiplier: {regime.position_size_multiplier:.2f}x")
    print(f"Confidence Threshold: {regime.confidence_threshold:.2f}")

    multipliers = detector.get_regime_multipliers(regime)
    print(f"\nMultipliers: {multipliers}")
