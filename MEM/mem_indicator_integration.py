"""
MEM Indicator Integration
==========================
Integrates advanced indicators with MEM trading system.
Provides high-level functions for indicator-based trading signals.
"""

import pandas as pd
import numpy as np
from typing import Dict, List, Optional, Tuple, Union
from datetime import datetime
import logging

from advanced_indicators import AdvancedIndicators, get_indicators

logger = logging.getLogger(__name__)


class MEMIndicatorAnalyzer:
    """
    High-level indicator analysis for MEM trading system.
    Combines multiple indicators to generate trading signals and market insights.
    """

    def __init__(self):
        """Initialize MEM Indicator Analyzer"""
        self.indicators = get_indicators()
        self.signal_strength_weights = {
            'momentum': 0.25,
            'trend': 0.35,
            'volatility': 0.20,
            'volume': 0.20
        }

    def analyze_market_conditions(self, data: pd.DataFrame) -> Dict:
        """
        Comprehensive market analysis using multiple indicators.

        Args:
            data: DataFrame with columns ['open', 'high', 'low', 'close', 'volume']

        Returns:
            Dictionary with market analysis including:
            - overall_signal: 'BUY', 'SELL', or 'NEUTRAL'
            - signal_strength: 0-100
            - trend_direction: 'UPTREND', 'DOWNTREND', or 'SIDEWAYS'
            - volatility_level: 'LOW', 'MEDIUM', or 'HIGH'
            - indicators: Dictionary of all indicator values
            - reasoning: List of reasons for the signal
        """
        try:
            high = data['high']
            low = data['low']
            close = data['close']
            volume = data['volume']

            analysis = {
                'timestamp': datetime.now().isoformat(),
                'indicators': {},
                'signals': {},
                'reasoning': []
            }

            # MOMENTUM INDICATORS
            rsi = self.indicators.rsi(close)
            stoch_k, stoch_d = self.indicators.stochastic(high, low, close)
            williams_r = self.indicators.williams_r(high, low, close)
            cci = self.indicators.cci(high, low, close)

            analysis['indicators']['momentum'] = {
                'rsi': float(rsi.iloc[-1]) if not rsi.empty else 50.0,
                'stochastic_k': float(stoch_k.iloc[-1]) if not stoch_k.empty else 50.0,
                'stochastic_d': float(stoch_d.iloc[-1]) if not stoch_d.empty else 50.0,
                'williams_r': float(williams_r.iloc[-1]) if not williams_r.empty else -50.0,
                'cci': float(cci.iloc[-1]) if not cci.empty else 0.0
            }

            # Momentum signal
            momentum_score = 0
            rsi_val = analysis['indicators']['momentum']['rsi']
            if rsi_val < 30:
                momentum_score += 2
                analysis['reasoning'].append("RSI oversold (bullish)")
            elif rsi_val > 70:
                momentum_score -= 2
                analysis['reasoning'].append("RSI overbought (bearish)")

            stoch_val = analysis['indicators']['momentum']['stochastic_k']
            if stoch_val < 20:
                momentum_score += 1
                analysis['reasoning'].append("Stochastic oversold (bullish)")
            elif stoch_val > 80:
                momentum_score -= 1
                analysis['reasoning'].append("Stochastic overbought (bearish)")

            analysis['signals']['momentum'] = momentum_score

            # TREND INDICATORS
            macd, macd_signal, macd_hist = self.indicators.macd(close)
            adx, plus_di, minus_di = self.indicators.adx(high, low, close)
            aroon_up, aroon_down, aroon_osc = self.indicators.aroon(high, low)

            analysis['indicators']['trend'] = {
                'macd': float(macd.iloc[-1]) if not macd.empty else 0.0,
                'macd_signal': float(macd_signal.iloc[-1]) if not macd_signal.empty else 0.0,
                'macd_histogram': float(macd_hist.iloc[-1]) if not macd_hist.empty else 0.0,
                'adx': float(adx.iloc[-1]) if not adx.empty else 0.0,
                'plus_di': float(plus_di.iloc[-1]) if not plus_di.empty else 0.0,
                'minus_di': float(minus_di.iloc[-1]) if not minus_di.empty else 0.0,
                'aroon_up': float(aroon_up.iloc[-1]) if not aroon_up.empty else 50.0,
                'aroon_down': float(aroon_down.iloc[-1]) if not aroon_down.empty else 50.0
            }

            # Trend signal
            trend_score = 0
            if analysis['indicators']['trend']['macd_histogram'] > 0:
                trend_score += 2
                analysis['reasoning'].append("MACD histogram positive (bullish)")
            else:
                trend_score -= 2
                analysis['reasoning'].append("MACD histogram negative (bearish)")

            adx_val = analysis['indicators']['trend']['adx']
            if adx_val > 25:
                if analysis['indicators']['trend']['plus_di'] > analysis['indicators']['trend']['minus_di']:
                    trend_score += 2
                    analysis['reasoning'].append("ADX shows strong uptrend")
                else:
                    trend_score -= 2
                    analysis['reasoning'].append("ADX shows strong downtrend")

            aroon_val = analysis['indicators']['trend']['aroon_up'] - analysis['indicators']['trend']['aroon_down']
            if aroon_val > 50:
                trend_score += 1
                analysis['reasoning'].append("Aroon indicates uptrend")
            elif aroon_val < -50:
                trend_score -= 1
                analysis['reasoning'].append("Aroon indicates downtrend")

            analysis['signals']['trend'] = trend_score

            # Determine trend direction
            if trend_score > 2:
                analysis['trend_direction'] = 'UPTREND'
            elif trend_score < -2:
                analysis['trend_direction'] = 'DOWNTREND'
            else:
                analysis['trend_direction'] = 'SIDEWAYS'

            # VOLATILITY INDICATORS
            atr = self.indicators.atr(high, low, close)

            try:
                bb_upper, bb_middle, bb_lower = self.indicators.bollinger_bands(close)
                bb_upper_val = float(bb_upper.iloc[-1]) if not bb_upper.empty else 0.0
                bb_middle_val = float(bb_middle.iloc[-1]) if not bb_middle.empty else 0.0
                bb_lower_val = float(bb_lower.iloc[-1]) if not bb_lower.empty else 0.0
                bb_width_val = float((bb_upper_val - bb_lower_val) / bb_middle_val * 100) if bb_middle_val > 0 else 0.0
            except Exception as e:
                logger.warning(f"Error calculating Bollinger Bands: {e}, using defaults")
                bb_upper_val = bb_middle_val = bb_lower_val = bb_width_val = 0.0

            analysis['indicators']['volatility'] = {
                'atr': float(atr.iloc[-1]) if not atr.empty else 0.0,
                'bb_upper': bb_upper_val,
                'bb_middle': bb_middle_val,
                'bb_lower': bb_lower_val,
                'bb_width': bb_width_val
            }

            # Volatility level
            bb_width = analysis['indicators']['volatility']['bb_width']
            if bb_width < 5:
                analysis['volatility_level'] = 'LOW'
            elif bb_width < 15:
                analysis['volatility_level'] = 'MEDIUM'
            else:
                analysis['volatility_level'] = 'HIGH'

            # Volatility signal (Bollinger Bands squeeze/breakout)
            volatility_score = 0
            current_price = float(close.iloc[-1])
            if current_price < analysis['indicators']['volatility']['bb_lower']:
                volatility_score += 2
                analysis['reasoning'].append("Price below lower Bollinger Band (bullish)")
            elif current_price > analysis['indicators']['volatility']['bb_upper']:
                volatility_score -= 2
                analysis['reasoning'].append("Price above upper Bollinger Band (bearish)")

            analysis['signals']['volatility'] = volatility_score

            # VOLUME INDICATORS
            obv = self.indicators.obv(close, volume)
            mfi = self.indicators.mfi(high, low, close, volume)
            cmf = self.indicators.cmf(high, low, close, volume)

            analysis['indicators']['volume'] = {
                'obv': float(obv.iloc[-1]) if not obv.empty else 0.0,
                'obv_trend': 'UP' if len(obv) > 5 and obv.iloc[-1] > obv.iloc[-5] else 'DOWN',
                'mfi': float(mfi.iloc[-1]) if not mfi.empty else 50.0,
                'cmf': float(cmf.iloc[-1]) if not cmf.empty else 0.0
            }

            # Volume signal
            volume_score = 0
            mfi_val = analysis['indicators']['volume']['mfi']
            if mfi_val < 20:
                volume_score += 2
                analysis['reasoning'].append("MFI oversold (bullish)")
            elif mfi_val > 80:
                volume_score -= 2
                analysis['reasoning'].append("MFI overbought (bearish)")

            if analysis['indicators']['volume']['cmf'] > 0.1:
                volume_score += 1
                analysis['reasoning'].append("Chaikin Money Flow positive (bullish)")
            elif analysis['indicators']['volume']['cmf'] < -0.1:
                volume_score -= 1
                analysis['reasoning'].append("Chaikin Money Flow negative (bearish)")

            analysis['signals']['volume'] = volume_score

            # CALCULATE OVERALL SIGNAL
            total_score = (
                momentum_score * self.signal_strength_weights['momentum'] +
                trend_score * self.signal_strength_weights['trend'] +
                volatility_score * self.signal_strength_weights['volatility'] +
                volume_score * self.signal_strength_weights['volume']
            )

            # Normalize to 0-100 scale
            signal_strength = min(100, max(0, (total_score + 10) * 5))

            if total_score > 2:
                overall_signal = 'BUY'
            elif total_score < -2:
                overall_signal = 'SELL'
            else:
                overall_signal = 'NEUTRAL'

            analysis['overall_signal'] = overall_signal
            analysis['signal_strength'] = float(signal_strength)
            analysis['total_score'] = float(total_score)

            logger.info(f"Market analysis complete: {overall_signal} (strength: {signal_strength:.1f}%)")

            return analysis

        except Exception as e:
            logger.error(f"Error in market analysis: {e}")
            return {
                'overall_signal': 'NEUTRAL',
                'signal_strength': 50.0,
                'trend_direction': 'SIDEWAYS',
                'volatility_level': 'MEDIUM',
                'error': str(e)
            }

    def get_entry_exit_signals(self, data: pd.DataFrame) -> Dict:
        """
        Generate specific entry and exit signals.

        Returns:
            Dictionary with:
            - action: 'ENTER_LONG', 'ENTER_SHORT', 'EXIT_LONG', 'EXIT_SHORT', or 'HOLD'
            - confidence: 0-100
            - stop_loss: Suggested stop loss price
            - take_profit: Suggested take profit price
            - reasoning: List of reasons
        """
        try:
            high = data['high']
            low = data['low']
            close = data['close']
            current_price = float(close.iloc[-1])

            signals = {
                'timestamp': datetime.now().isoformat(),
                'current_price': current_price,
                'reasoning': []
            }

            # Get comprehensive market analysis
            market_analysis = self.analyze_market_conditions(data)

            # Calculate ATR for stop loss/take profit
            atr = self.indicators.atr(high, low, close)
            atr_value = float(atr.iloc[-1]) if not atr.empty else current_price * 0.02

            # Get SuperTrend for trend confirmation
            supertrend, st_direction = self.indicators.supertrend(high, low, close)
            st_val = float(supertrend.iloc[-1]) if not supertrend.empty else current_price
            st_dir = float(st_direction.iloc[-1]) if not st_direction.empty else 0

            # Determine action
            if market_analysis['overall_signal'] == 'BUY' and market_analysis['signal_strength'] > 60:
                signals['action'] = 'ENTER_LONG'
                signals['confidence'] = market_analysis['signal_strength']
                signals['stop_loss'] = current_price - (2 * atr_value)
                signals['take_profit'] = current_price + (3 * atr_value)
                signals['reasoning'] = market_analysis['reasoning']

            elif market_analysis['overall_signal'] == 'SELL' and market_analysis['signal_strength'] > 60:
                signals['action'] = 'ENTER_SHORT'
                signals['confidence'] = market_analysis['signal_strength']
                signals['stop_loss'] = current_price + (2 * atr_value)
                signals['take_profit'] = current_price - (3 * atr_value)
                signals['reasoning'] = market_analysis['reasoning']

            else:
                signals['action'] = 'HOLD'
                signals['confidence'] = 50.0
                signals['stop_loss'] = None
                signals['take_profit'] = None
                signals['reasoning'] = ['Signal strength insufficient for entry']

            return signals

        except Exception as e:
            logger.error(f"Error generating entry/exit signals: {e}")
            return {
                'action': 'HOLD',
                'confidence': 0,
                'error': str(e)
            }

    def calculate_risk_metrics(self, data: pd.DataFrame, position_size: float = 1.0) -> Dict:
        """
        Calculate risk metrics for position sizing and management.

        Returns:
            Dictionary with risk metrics including volatility, value at risk, etc.
        """
        try:
            high = data['high']
            low = data['low']
            close = data['close']

            # Calculate ATR for volatility-based position sizing
            atr = self.indicators.atr(high, low, close)
            atr_value = float(atr.iloc[-1]) if not atr.empty else 0.0

            # Calculate historical volatility
            returns = close.pct_change().dropna()
            std_dev = float(returns.std())
            volatility_annualized = std_dev * np.sqrt(252)  # Assuming daily data

            # Calculate Value at Risk (95% confidence)
            var_95 = float(returns.quantile(0.05)) * position_size

            # Calculate Sharpe Ratio (simplified)
            mean_return = float(returns.mean())
            sharpe = (mean_return * 252) / (std_dev * np.sqrt(252)) if std_dev > 0 else 0

            # Bollinger Band width for volatility regime
            bb_upper, bb_middle, bb_lower = self.indicators.bollinger_bands(close)
            bb_width = float((bb_upper.iloc[-1] - bb_lower.iloc[-1]) / bb_middle.iloc[-1] * 100) if not bb_upper.empty else 0.0

            return {
                'atr': atr_value,
                'volatility_daily': std_dev,
                'volatility_annualized': volatility_annualized,
                'value_at_risk_95': var_95,
                'sharpe_ratio': sharpe,
                'bollinger_width_pct': bb_width,
                'risk_level': 'HIGH' if volatility_annualized > 0.4 else 'MEDIUM' if volatility_annualized > 0.2 else 'LOW'
            }

        except Exception as e:
            logger.error(f"Error calculating risk metrics: {e}")
            return {'error': str(e)}

    def get_support_resistance_levels(self, data: pd.DataFrame) -> Dict:
        """
        Calculate support and resistance levels using multiple methods.

        Returns:
            Dictionary with support/resistance levels from:
            - Pivot points (standard, fibonacci, woodie, camarilla)
            - Fibonacci retracements
        """
        try:
            high = data['high']
            low = data['low']
            close = data['close']

            # Get recent swing high and low for Fibonacci
            swing_high = float(high.rolling(20).max().iloc[-1])
            swing_low = float(low.rolling(20).min().iloc[-1])

            levels = {
                'pivot_standard': self.indicators.pivot_points(high, low, close, method='standard'),
                'pivot_fibonacci': self.indicators.pivot_points(high, low, close, method='fibonacci'),
                'pivot_woodie': self.indicators.pivot_points(high, low, close, method='woodie'),
                'pivot_camarilla': self.indicators.pivot_points(high, low, close, method='camarilla'),
                'fibonacci': self.indicators.fibonacci_retracement(swing_high, swing_low),
                'swing_high': swing_high,
                'swing_low': swing_low
            }

            return levels

        except Exception as e:
            logger.error(f"Error calculating support/resistance levels: {e}")
            return {'error': str(e)}

    def get_multi_timeframe_analysis(self, data_dict: Dict[str, pd.DataFrame]) -> Dict:
        """
        Analyze multiple timeframes to get confluence signals.

        Args:
            data_dict: Dictionary mapping timeframe names to DataFrames
                      e.g., {'1h': df_1h, '4h': df_4h, '1d': df_1d}

        Returns:
            Dictionary with multi-timeframe analysis
        """
        try:
            mtf_analysis = {
                'timestamp': datetime.now().isoformat(),
                'timeframes': {},
                'confluence_signal': 'NEUTRAL',
                'confluence_strength': 0
            }

            signals = []
            strengths = []

            for timeframe, data in data_dict.items():
                analysis = self.analyze_market_conditions(data)
                mtf_analysis['timeframes'][timeframe] = {
                    'signal': analysis['overall_signal'],
                    'strength': analysis['signal_strength'],
                    'trend': analysis['trend_direction']
                }

                # Convert signal to numeric for confluence calculation
                signal_value = 1 if analysis['overall_signal'] == 'BUY' else -1 if analysis['overall_signal'] == 'SELL' else 0
                signals.append(signal_value)
                strengths.append(analysis['signal_strength'])

            # Calculate confluence
            avg_signal = sum(signals) / len(signals)
            avg_strength = sum(strengths) / len(strengths)

            if avg_signal > 0.5:
                mtf_analysis['confluence_signal'] = 'BUY'
            elif avg_signal < -0.5:
                mtf_analysis['confluence_signal'] = 'SELL'
            else:
                mtf_analysis['confluence_signal'] = 'NEUTRAL'

            mtf_analysis['confluence_strength'] = avg_strength

            # Count aligned timeframes
            buy_count = sum(1 for s in signals if s > 0)
            sell_count = sum(1 for s in signals if s < 0)
            mtf_analysis['timeframes_aligned'] = max(buy_count, sell_count)
            mtf_analysis['total_timeframes'] = len(signals)

            return mtf_analysis

        except Exception as e:
            logger.error(f"Error in multi-timeframe analysis: {e}")
            return {'error': str(e)}


# ============================================================================
# CONVENIENCE FUNCTIONS
# ============================================================================

# Global instance
_analyzer = MEMIndicatorAnalyzer()

def analyze_market(data: pd.DataFrame) -> Dict:
    """Quick market analysis"""
    return _analyzer.analyze_market_conditions(data)

def get_trading_signals(data: pd.DataFrame) -> Dict:
    """Get entry/exit signals"""
    return _analyzer.get_entry_exit_signals(data)

def get_risk_metrics(data: pd.DataFrame, position_size: float = 1.0) -> Dict:
    """Calculate risk metrics"""
    return _analyzer.calculate_risk_metrics(data, position_size)

def get_support_resistance(data: pd.DataFrame) -> Dict:
    """Get support/resistance levels"""
    return _analyzer.get_support_resistance_levels(data)

def analyze_multiple_timeframes(data_dict: Dict[str, pd.DataFrame]) -> Dict:
    """Multi-timeframe analysis"""
    return _analyzer.get_multi_timeframe_analysis(data_dict)


if __name__ == "__main__":
    # Example usage
    print("=" * 60)
    print("MEM Indicator Integration - Ready")
    print("=" * 60)
    print("\nAvailable functions:")
    print("  - analyze_market(data)")
    print("  - get_trading_signals(data)")
    print("  - get_risk_metrics(data, position_size)")
    print("  - get_support_resistance(data)")
    print("  - analyze_multiple_timeframes(data_dict)")
    print("=" * 60)
