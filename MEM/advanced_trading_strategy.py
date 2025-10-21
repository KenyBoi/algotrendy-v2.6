"""
Advanced Multi-Indicator Trading Strategy for MEM
==================================================
This strategy combines multiple technical indicators for high-confidence trade signals.

Strategy: Multi-Indicator Confluence (MIC)
- Uses 50+ indicators for signal confirmation
- Multi-timeframe analysis for trend alignment
- Risk-based position sizing
- Dynamic stop-loss and take-profit levels

Author: MEM AI System
Date: 2025-10-21
Version: 1.0
"""

import pandas as pd
import numpy as np
from datetime import datetime
from typing import Dict, List, Tuple, Optional
import logging

from advanced_indicators import get_indicators
from mem_indicator_integration import (
    analyze_market,
    get_trading_signals,
    get_risk_metrics,
    get_support_resistance,
    analyze_multiple_timeframes
)

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


class AdvancedTradingStrategy:
    """
    Multi-Indicator Confluence Trading Strategy

    This strategy uses:
    1. Trend confirmation (MACD, ADX, Moving Averages)
    2. Momentum confirmation (RSI, Stochastic, Williams %R)
    3. Volume confirmation (OBV, MFI, CMF)
    4. Volatility analysis (ATR, Bollinger Bands)
    5. Support/Resistance levels (Pivot Points, Fibonacci)
    6. Multi-timeframe alignment
    """

    def __init__(self,
                 min_confidence: float = 70.0,
                 max_risk_per_trade: float = 0.02,
                 use_multi_timeframe: bool = True):
        """
        Initialize the strategy

        Args:
            min_confidence: Minimum confidence threshold for trades (0-100%)
            max_risk_per_trade: Maximum risk per trade as % of portfolio (0.01 = 1%)
            use_multi_timeframe: Enable multi-timeframe analysis
        """
        self.min_confidence = min_confidence
        self.max_risk_per_trade = max_risk_per_trade
        self.use_multi_timeframe = use_multi_timeframe
        self.indicators = get_indicators()

        logger.info(f"Strategy initialized with min_confidence={min_confidence}%, "
                   f"max_risk={max_risk_per_trade*100}%")

    def analyze_single_timeframe(self, data: pd.DataFrame, timeframe: str = "1h") -> Dict:
        """
        Analyze a single timeframe

        Args:
            data: OHLCV DataFrame with datetime index
            timeframe: Timeframe label (e.g., '1h', '4h', '1d')

        Returns:
            Dictionary with analysis results
        """
        try:
            # Get comprehensive market analysis
            market_analysis = analyze_market(data)

            # Get trading signals
            signals = get_trading_signals(data)

            # Get risk metrics
            risk = get_risk_metrics(data, position_size=1.0)

            # Get support/resistance levels
            levels = get_support_resistance(data)

            # Calculate additional custom metrics
            current_price = data['close'].iloc[-1]

            # Distance from pivot point (%)
            pivot = levels['pivot_standard']['pivot']
            distance_from_pivot = ((current_price - pivot) / pivot) * 100

            # Price position in Bollinger Bands
            close = data['close']
            bb_upper, bb_mid, bb_lower = self.indicators.bollinger_bands(close)
            bb_position = ((current_price - bb_lower.iloc[-1]) /
                          (bb_upper.iloc[-1] - bb_lower.iloc[-1])) * 100

            return {
                'timeframe': timeframe,
                'signal': market_analysis['overall_signal'],
                'confidence': market_analysis['signal_strength'],
                'trend': market_analysis['trend_direction'],
                'volatility': market_analysis['volatility_level'],
                'action': signals['action'],
                'stop_loss': signals.get('stop_loss'),
                'take_profit': signals.get('take_profit'),
                'risk_level': risk['risk_level'],
                'atr': risk['atr'],
                'sharpe_ratio': risk['sharpe_ratio'],
                'support_resistance': levels,
                'current_price': current_price,
                'distance_from_pivot': distance_from_pivot,
                'bb_position': bb_position,
                'analysis': market_analysis
            }

        except Exception as e:
            logger.error(f"Error analyzing {timeframe}: {e}")
            return {'error': str(e), 'timeframe': timeframe}

    def generate_trade_signal(self,
                            data_1h: pd.DataFrame,
                            data_4h: Optional[pd.DataFrame] = None,
                            data_1d: Optional[pd.DataFrame] = None,
                            account_balance: float = 10000.0) -> Dict:
        """
        Generate a trade signal with position sizing

        Args:
            data_1h: 1-hour OHLCV data
            data_4h: 4-hour OHLCV data (optional)
            data_1d: Daily OHLCV data (optional)
            account_balance: Current account balance for position sizing

        Returns:
            Trade signal dictionary with all details
        """
        logger.info("=" * 70)
        logger.info("GENERATING TRADE SIGNAL - MEM ADVANCED STRATEGY")
        logger.info("=" * 70)

        # Analyze 1-hour timeframe (primary)
        analysis_1h = self.analyze_single_timeframe(data_1h, '1h')

        if 'error' in analysis_1h:
            return {'action': 'HOLD', 'reason': f"Analysis error: {analysis_1h['error']}"}

        # Multi-timeframe analysis if enabled and data provided
        confluence_strength = analysis_1h['confidence']
        mtf_aligned = 1
        mtf_total = 1

        if self.use_multi_timeframe and data_4h is not None and data_1d is not None:
            mtf_data = {'1h': data_1h, '4h': data_4h, '1d': data_1d}
            mtf_analysis = analyze_multiple_timeframes(mtf_data)

            confluence_strength = mtf_analysis['confluence_strength']
            mtf_aligned = mtf_analysis['timeframes_aligned']
            mtf_total = mtf_analysis['total_timeframes']

            logger.info(f"Multi-timeframe: {mtf_aligned}/{mtf_total} aligned, "
                       f"strength={confluence_strength:.1f}%")

        # Decision logic
        current_price = analysis_1h['current_price']
        signal = analysis_1h['signal']
        action = analysis_1h['action']

        # Check minimum confidence threshold
        if confluence_strength < self.min_confidence:
            logger.info(f"Signal confidence {confluence_strength:.1f}% below threshold "
                       f"{self.min_confidence}%")
            return {
                'action': 'HOLD',
                'reason': f'Low confidence: {confluence_strength:.1f}% < {self.min_confidence}%',
                'confidence': confluence_strength,
                'signal': signal,
                'timestamp': datetime.now().isoformat()
            }

        # Risk-based position sizing
        atr = analysis_1h['atr']
        stop_loss_distance = 2 * atr  # 2x ATR stop loss

        # Calculate position size based on risk
        risk_amount = account_balance * self.max_risk_per_trade
        position_size = risk_amount / stop_loss_distance

        # Calculate stop loss and take profit
        if signal == 'BUY':
            stop_loss = current_price - stop_loss_distance
            take_profit = current_price + (2 * stop_loss_distance)  # 1:2 risk/reward
        elif signal == 'SELL':
            stop_loss = current_price + stop_loss_distance
            take_profit = current_price - (2 * stop_loss_distance)
        else:
            return {
                'action': 'HOLD',
                'reason': 'Neutral signal',
                'confidence': confluence_strength,
                'signal': signal,
                'timestamp': datetime.now().isoformat()
            }

        # Build trade recommendation
        trade = {
            'action': action,
            'signal': signal,
            'confidence': confluence_strength,
            'entry_price': current_price,
            'stop_loss': stop_loss,
            'take_profit': take_profit,
            'position_size': position_size,
            'risk_amount': risk_amount,
            'risk_percent': self.max_risk_per_trade * 100,
            'risk_reward_ratio': 2.0,
            'atr': atr,
            'trend': analysis_1h['trend'],
            'volatility': analysis_1h['volatility'],
            'risk_level': analysis_1h['risk_level'],
            'sharpe_ratio': analysis_1h['sharpe_ratio'],
            'bb_position': analysis_1h['bb_position'],
            'distance_from_pivot': analysis_1h['distance_from_pivot'],
            'timeframes_aligned': f"{mtf_aligned}/{mtf_total}",
            'timestamp': datetime.now().isoformat(),
            'reasoning': self._generate_reasoning(analysis_1h, confluence_strength, mtf_aligned, mtf_total)
        }

        # Log the trade signal
        logger.info(f"\n{'=' * 70}")
        logger.info(f"TRADE SIGNAL: {trade['action']}")
        logger.info(f"{'=' * 70}")
        logger.info(f"Signal: {trade['signal']} | Confidence: {trade['confidence']:.1f}%")
        logger.info(f"Entry: ${trade['entry_price']:.2f}")
        logger.info(f"Stop Loss: ${trade['stop_loss']:.2f} ({((trade['stop_loss']-current_price)/current_price*100):.2f}%)")
        logger.info(f"Take Profit: ${trade['take_profit']:.2f} ({((trade['take_profit']-current_price)/current_price*100):.2f}%)")
        logger.info(f"Position Size: {trade['position_size']:.4f} units")
        logger.info(f"Risk: ${trade['risk_amount']:.2f} ({trade['risk_percent']:.2f}%)")
        logger.info(f"Risk/Reward: 1:{trade['risk_reward_ratio']:.1f}")
        logger.info(f"Trend: {trade['trend']} | Volatility: {trade['volatility']}")
        logger.info(f"Timeframes Aligned: {trade['timeframes_aligned']}")
        logger.info(f"{'=' * 70}\n")

        return trade

    def _generate_reasoning(self, analysis: Dict, confidence: float,
                           mtf_aligned: int, mtf_total: int) -> List[str]:
        """Generate human-readable reasoning for the trade"""
        reasons = []

        # Signal strength
        if confidence >= 80:
            reasons.append(f"Very strong signal ({confidence:.1f}% confidence)")
        elif confidence >= 70:
            reasons.append(f"Strong signal ({confidence:.1f}% confidence)")
        else:
            reasons.append(f"Moderate signal ({confidence:.1f}% confidence)")

        # Trend
        if analysis['trend'] in ['BULLISH', 'BEARISH']:
            reasons.append(f"{analysis['trend'].capitalize()} trend confirmed")

        # Multi-timeframe
        if mtf_total > 1:
            alignment_pct = (mtf_aligned / mtf_total) * 100
            if alignment_pct >= 66:
                reasons.append(f"High timeframe alignment ({mtf_aligned}/{mtf_total})")
            elif alignment_pct >= 33:
                reasons.append(f"Moderate timeframe alignment ({mtf_aligned}/{mtf_total})")

        # Volatility
        if analysis['volatility'] == 'LOW':
            reasons.append("Low volatility favors position entry")
        elif analysis['volatility'] == 'HIGH':
            reasons.append("High volatility - reduced position size recommended")

        # Bollinger Band position
        bb_pos = analysis['bb_position']
        if bb_pos < 20:
            reasons.append("Price near lower Bollinger Band (potential reversal)")
        elif bb_pos > 80:
            reasons.append("Price near upper Bollinger Band (potential reversal)")

        # Sharpe ratio
        if analysis['sharpe_ratio'] > 2.0:
            reasons.append(f"Excellent risk-adjusted returns (Sharpe: {analysis['sharpe_ratio']:.2f})")
        elif analysis['sharpe_ratio'] > 1.0:
            reasons.append(f"Good risk-adjusted returns (Sharpe: {analysis['sharpe_ratio']:.2f})")

        return reasons

    def backtest_signal(self, historical_data: pd.DataFrame,
                       entry_price: float, stop_loss: float,
                       take_profit: float, signal: str) -> Dict:
        """
        Backtest a trade signal on historical data

        Args:
            historical_data: Historical OHLCV data after signal
            entry_price: Entry price
            stop_loss: Stop loss price
            take_profit: Take profit price
            signal: BUY or SELL

        Returns:
            Trade outcome dictionary
        """
        for i, row in historical_data.iterrows():
            high = row['high']
            low = row['low']

            if signal == 'BUY':
                # Check stop loss hit
                if low <= stop_loss:
                    return {
                        'outcome': 'STOP_LOSS',
                        'exit_price': stop_loss,
                        'pnl': stop_loss - entry_price,
                        'pnl_pct': ((stop_loss - entry_price) / entry_price) * 100,
                        'exit_time': i
                    }
                # Check take profit hit
                if high >= take_profit:
                    return {
                        'outcome': 'TAKE_PROFIT',
                        'exit_price': take_profit,
                        'pnl': take_profit - entry_price,
                        'pnl_pct': ((take_profit - entry_price) / entry_price) * 100,
                        'exit_time': i
                    }

            elif signal == 'SELL':
                # Check stop loss hit
                if high >= stop_loss:
                    return {
                        'outcome': 'STOP_LOSS',
                        'exit_price': stop_loss,
                        'pnl': entry_price - stop_loss,
                        'pnl_pct': ((entry_price - stop_loss) / entry_price) * 100,
                        'exit_time': i
                    }
                # Check take profit hit
                if low <= take_profit:
                    return {
                        'outcome': 'TAKE_PROFIT',
                        'exit_price': take_profit,
                        'pnl': entry_price - take_profit,
                        'pnl_pct': ((entry_price - take_profit) / entry_price) * 100,
                        'exit_time': i
                    }

        # Trade still open
        last_price = historical_data['close'].iloc[-1]
        if signal == 'BUY':
            pnl = last_price - entry_price
        else:
            pnl = entry_price - last_price

        return {
            'outcome': 'OPEN',
            'exit_price': last_price,
            'pnl': pnl,
            'pnl_pct': (pnl / entry_price) * 100,
            'exit_time': historical_data.index[-1]
        }


def demo_strategy():
    """Demo the strategy with sample data"""
    print("\n" + "=" * 70)
    print("MEM ADVANCED TRADING STRATEGY - DEMO")
    print("=" * 70)

    # Generate sample data
    from test_indicators import generate_sample_data

    data_1h = generate_sample_data(periods=200)
    data_4h = generate_sample_data(periods=200)
    data_1d = generate_sample_data(periods=200)

    # Initialize strategy
    strategy = AdvancedTradingStrategy(
        min_confidence=65.0,  # 65% minimum confidence
        max_risk_per_trade=0.02,  # 2% max risk per trade
        use_multi_timeframe=True
    )

    # Generate signal
    signal = strategy.generate_trade_signal(
        data_1h=data_1h,
        data_4h=data_4h,
        data_1d=data_1d,
        account_balance=10000.0
    )

    print("\n" + "=" * 70)
    print("TRADE RECOMMENDATION")
    print("=" * 70)
    print(f"Action: {signal.get('action', 'HOLD')}")

    if signal['action'] != 'HOLD':
        print(f"Entry: ${signal['entry_price']:.2f}")
        print(f"Stop Loss: ${signal['stop_loss']:.2f}")
        print(f"Take Profit: ${signal['take_profit']:.2f}")
        print(f"Position Size: {signal['position_size']:.4f} units")
        print(f"Risk: ${signal['risk_amount']:.2f} ({signal['risk_percent']:.2f}%)")
        print(f"\nReasoning:")
        for i, reason in enumerate(signal['reasoning'], 1):
            print(f"  {i}. {reason}")
    else:
        print(f"Reason: {signal.get('reason', 'Unknown')}")

    print("=" * 70)
    print("✅ Demo completed successfully!")
    print("=" * 70)


if __name__ == "__main__":
    demo_strategy()
