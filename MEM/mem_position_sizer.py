"""
MEM-Aware Position Sizing
Combines MEM confidence with v2.5 risk management
"""

import pandas as pd
import numpy as np
from typing import Dict
from regime_detector import RegimeDetector, MarketRegime

class MEMPositionSizer:
    """
    Adaptive position sizing based on:
    - MEM confidence
    - Market regime
    - Volatility
    - v2.5 risk limits
    """

    def __init__(
        self,
        base_capital: float,
        base_risk_pct: float = 0.02,
        max_position_pct: float = 0.10,
        min_confidence: float = 0.5
    ):
        """
        Args:
            base_capital: Total trading capital
            base_risk_pct: Base risk per trade (default 2%)
            max_position_pct: Maximum position size (default 10%)
            min_confidence: Minimum MEM confidence to trade
        """
        self.base_capital = base_capital
        self.base_risk_pct = base_risk_pct
        self.max_position_pct = max_position_pct
        self.min_confidence = min_confidence

    def calculate_position_size(
        self,
        mem_confidence: float,
        current_price: float,
        volatility: float,
        regime: MarketRegime,
        v2_5_risk_settings: Dict
    ) -> Dict[str, float]:
        """
        Calculate optimal position size

        Args:
            mem_confidence: MEM prediction confidence (0.0-1.0)
            current_price: Current asset price
            volatility: Current volatility (ATR/price)
            regime: Current market regime
            v2_5_risk_settings: v2.5 risk limits from BrokerManager

        Returns:
            {
                'position_size_usd': float,
                'quantity': float,
                'confidence_used': float,
                'regime_mult': float,
                'vol_mult': float,
                'reason': str
            }
        """
        # Check minimum confidence
        if mem_confidence < self.min_confidence:
            return {
                'position_size_usd': 0,
                'quantity': 0,
                'confidence_used': mem_confidence,
                'regime_mult': 0,
                'vol_mult': 0,
                'confidence_mult': 0,
                'reason': f"Confidence {mem_confidence:.2f} < minimum {self.min_confidence:.2f}"
            }

        # Check regime confidence threshold
        if mem_confidence < regime.confidence_threshold:
            return {
                'position_size_usd': 0,
                'quantity': 0,
                'confidence_used': mem_confidence,
                'regime_mult': regime.position_size_multiplier,
                'vol_mult': 0,
                'confidence_mult': 0,
                'reason': f"Confidence {mem_confidence:.2f} < regime threshold {regime.confidence_threshold:.2f}"
            }

        # Base position size (2% of capital)
        base_size = self.base_capital * self.base_risk_pct

        # Confidence multiplier (0.0-1.0 → 0.5-2.0)
        # Higher confidence = larger position
        confidence_mult = 0.5 + (mem_confidence * 1.5)

        # Volatility multiplier (reduce size in high volatility)
        if volatility > 0.05:  # >5% volatility
            vol_mult = 0.5
        elif volatility > 0.03:  # >3% volatility
            vol_mult = 0.75
        elif volatility < 0.01:  # <1% volatility
            vol_mult = 1.2
        else:
            vol_mult = 1.0

        # Regime multiplier (from regime detector)
        regime_mult = regime.position_size_multiplier

        # Calculate final size
        position_size_usd = base_size * confidence_mult * vol_mult * regime_mult

        # Apply v2.5 risk limits
        max_position = v2_5_risk_settings.get('max_position_per_symbol', float('inf'))
        min_position = v2_5_risk_settings.get('min_position_size', 0)
        position_size_usd = np.clip(position_size_usd, min_position, max_position)

        # Apply max position percentage
        max_size_by_pct = self.base_capital * self.max_position_pct
        position_size_usd = min(position_size_usd, max_size_by_pct)

        # Convert to quantity
        quantity = position_size_usd / current_price

        return {
            'position_size_usd': position_size_usd,
            'quantity': quantity,
            'confidence_used': mem_confidence,
            'regime_mult': regime_mult,
            'vol_mult': vol_mult,
            'confidence_mult': confidence_mult,
            'reason': f"Confidence {mem_confidence:.2f} | Regime {regime.trend.value} | Vol {volatility:.3f}"
        }

    def get_stop_loss_price(
        self,
        entry_price: float,
        side: str,
        volatility: float,
        regime: MarketRegime,
        atr: float
    ) -> float:
        """
        Calculate adaptive stop loss based on volatility and regime

        Args:
            entry_price: Entry price
            side: 'buy' or 'sell'
            volatility: Current volatility
            regime: Current market regime
            atr: Average True Range

        Returns:
            Stop loss price
        """
        # Base stop at 2x ATR
        base_stop_distance = atr * 2

        # Regime adjustment
        regime_stop_mult = 1.5 if regime.volatility.value == 'high_volatility' else 1.0

        # Final stop distance
        stop_distance = base_stop_distance * regime_stop_mult

        # Calculate stop price
        if side.lower() == 'buy':
            stop_price = entry_price - stop_distance
        else:
            stop_price = entry_price + stop_distance

        return stop_price

    def get_take_profit_price(
        self,
        entry_price: float,
        side: str,
        mem_confidence: float,
        volatility: float,
        atr: float
    ) -> float:
        """
        Calculate adaptive take profit target

        Args:
            entry_price: Entry price
            side: 'buy' or 'sell'
            mem_confidence: MEM confidence
            volatility: Current volatility
            atr: Average True Range

        Returns:
            Take profit price
        """
        # Base target at 3x ATR
        base_target_distance = atr * 3

        # Confidence adjustment (higher confidence = larger target)
        confidence_mult = 0.8 + (mem_confidence * 0.4)  # 0.8-1.2x

        # Final target distance
        target_distance = base_target_distance * confidence_mult

        # Calculate target price
        if side.lower() == 'buy':
            target_price = entry_price + target_distance
        else:
            target_price = entry_price - target_distance

        return target_price


# Example usage
if __name__ == "__main__":
    from regime_detector import RegimeDetector, VolatilityRegime, TrendRegime, LiquidityRegime, MarketRegime

    # Setup
    capital = 10000
    position_sizer = MEMPositionSizer(base_capital=capital)

    # v2.5 risk settings (from BrokerManager)
    v2_5_risk = {
        'max_position_per_symbol': 2500,
        'min_position_size': 100,
        'max_total_exposure': 10000
    }

    # Market conditions
    current_price = 66000
    volatility = 0.02  # 2%
    atr = 1320  # $1320 ATR

    # MEM prediction
    mem_confidence = 0.78

    # Current regime (example)
    regime = MarketRegime(
        volatility=VolatilityRegime.NORMAL,
        trend=TrendRegime.TRENDING_UP,
        liquidity=LiquidityRegime.NORMAL,
        position_size_multiplier=1.2,
        confidence_threshold=0.6,
        timestamp=pd.Timestamp.now()
    )

    # Calculate position size
    result = position_sizer.calculate_position_size(
        mem_confidence=mem_confidence,
        current_price=current_price,
        volatility=volatility,
        regime=regime,
        v2_5_risk_settings=v2_5_risk
    )

    print("=== MEM Position Sizing ===")
    print(f"Capital: ${capital:,}")
    print(f"Current Price: ${current_price:,}")
    print(f"MEM Confidence: {mem_confidence:.2f}")
    print(f"Regime: {regime}")
    print(f"\n=== Results ===")
    print(f"Position Size: ${result['position_size_usd']:,.2f}")
    print(f"Quantity: {result['quantity']:.6f}")
    print(f"% of Capital: {result['position_size_usd'] / capital * 100:.1f}%")
    print(f"\n=== Multipliers ===")
    print(f"Confidence: {result['confidence_mult']:.2f}x")
    print(f"Volatility: {result['vol_mult']:.2f}x")
    print(f"Regime: {result['regime_mult']:.2f}x")
    print(f"\n=== Stop/Target ===")

    stop_loss = position_sizer.get_stop_loss_price(current_price, 'buy', volatility, regime, atr)
    take_profit = position_sizer.get_take_profit_price(current_price, 'buy', mem_confidence, volatility, atr)

    print(f"Entry: ${current_price:,}")
    print(f"Stop Loss: ${stop_loss:,.2f} (-{(current_price - stop_loss):.2f} | {(stop_loss/current_price - 1)*100:.2f}%)")
    print(f"Take Profit: ${take_profit:,.2f} (+{(take_profit - current_price):.2f} | {(take_profit/current_price - 1)*100:.2f}%)")
    print(f"Risk/Reward: 1:{(take_profit - current_price) / (current_price - stop_loss):.2f}")
