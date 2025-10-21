"""
Bid-Ask Spread Modeling
Adds realistic execution costs based on market conditions
"""

import pandas as pd
import numpy as np
from typing import Dict

class SpreadModel:
    """Models bid-ask spread based on volatility and volume"""

    def __init__(self, base_spread_pct: float = 0.0001):
        """
        Args:
            base_spread_pct: Base spread in % (default 0.01% = 1 bps)
        """
        self.base_spread_pct = base_spread_pct

    def calculate_spread(
        self,
        volatility: float,
        volume: float,
        avg_volume: float
    ) -> float:
        """
        Calculate dynamic spread based on market conditions

        Args:
            volatility: Current volatility (e.g., ATR / price)
            volume: Current period volume
            avg_volume: Average volume (e.g., 24h average)

        Returns:
            Spread percentage (e.g., 0.0002 = 0.02%)
        """
        # Base spread
        spread = self.base_spread_pct

        # Volatility impact (higher volatility = wider spread)
        vol_multiplier = 1 + (volatility * 10)  # 1% vol → 1.1x spread
        spread *= vol_multiplier

        # Liquidity impact (low volume = wider spread)
        if volume < avg_volume * 0.5:
            liquidity_multiplier = 2.0  # Double spread in low liquidity
        elif volume < avg_volume:
            liquidity_multiplier = 1.5
        else:
            liquidity_multiplier = 1.0

        spread *= liquidity_multiplier

        # Cap spread at 0.5%
        return min(spread, 0.005)

    def get_execution_price(
        self,
        market_price: float,
        side: str,
        spread_pct: float
    ) -> float:
        """
        Get realistic execution price including spread

        Args:
            market_price: Mid price
            side: 'buy' or 'sell'
            spread_pct: Spread percentage

        Returns:
            Execution price
        """
        half_spread = spread_pct / 2

        if side.lower() == 'buy':
            # Buying at ask = pay more
            return market_price * (1 + half_spread)
        else:
            # Selling at bid = get less
            return market_price * (1 - half_spread)


# Example usage
if __name__ == "__main__":
    spread_model = SpreadModel(base_spread_pct=0.0001)

    # Example: BTC during normal conditions
    volatility = 0.02  # 2% daily volatility
    volume = 1000000   # Current volume
    avg_volume = 1200000  # Average volume

    spread = spread_model.calculate_spread(volatility, volume, avg_volume)
    print(f"Spread: {spread * 100:.4f}% ({spread * 10000:.2f} bps)")

    # Execution prices
    market_price = 66000
    buy_price = spread_model.get_execution_price(market_price, 'buy', spread)
    sell_price = spread_model.get_execution_price(market_price, 'sell', spread)

    print(f"Market: ${market_price}")
    print(f"Buy (ask): ${buy_price:.2f} (+${buy_price - market_price:.2f})")
    print(f"Sell (bid): ${sell_price:.2f} (-${market_price - sell_price:.2f})")
