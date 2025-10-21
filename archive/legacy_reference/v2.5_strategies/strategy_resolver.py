"""
Strategy Resolver System
=======================
Dynamically loads and applies trading strategies based on configuration.
Each strategy implements the BaseStrategy interface for consistency.
"""

from abc import ABC, abstractmethod
from typing import Dict, Optional, List
import logging
from datetime import datetime

logger = logging.getLogger(__name__)


class BaseStrategy(ABC):
    """Abstract base class for all trading strategies"""
    
    def __init__(self, **params):
        """
        Initialize strategy with parameters
        
        Args:
            **params: Strategy-specific parameters
        """
        self.params = params
        self.signal_history = []
        self.last_signal_time = None
    
    @abstractmethod
    def analyze(self, market_data: Dict) -> Dict:
        """
        Generate trading signal from market data
        
        Args:
            market_data: Dictionary containing market data
                - price: Current price
                - change_pct: Percentage change
                - volume: Trading volume
                - timestamp: Data timestamp
                - (strategy-specific fields)
        
        Returns:
            Dictionary with signal information:
            {
                'action': 'BUY' | 'SELL' | 'HOLD',
                'confidence': 0.0-1.0,
                'entry_price': float,
                'stop_loss': float,
                'target_price': float,
                'reason': str
            }
        """
        pass
    
    def get_signal_history(self) -> List[Dict]:
        """Get history of generated signals"""
        return self.signal_history
    
    def record_signal(self, signal: Dict):
        """Record a generated signal"""
        signal['timestamp'] = datetime.now()
        self.signal_history.append(signal)
        self.last_signal_time = datetime.now()


class MomentumStrategy(BaseStrategy):
    """Momentum-based trading strategy"""
    
    def analyze(self, market_data: Dict) -> Dict:
        """
        Analyze market data using momentum indicators
        
        Strategy Logic:
        - Buy if price change > buy threshold
        - Sell if price change < sell threshold
        - Hold otherwise
        """
        try:
            price_change = market_data.get('change_pct', 0)
            volume = market_data.get('volume', 0)
            price = market_data.get('price', 0)
            
            threshold_buy = self.params.get('threshold_buy', 2.0)
            threshold_sell = self.params.get('threshold_sell', -2.0)
            volatility_filter = self.params.get('volatility_filter', 0.15)
            
            # Calculate volatility (simplified)
            volatility = abs(price_change)
            
            # Base signal
            action = 'HOLD'
            confidence = 0.3
            
            # Momentum-based decision
            if price_change > threshold_buy and volatility < volatility_filter:
                action = 'BUY'
                confidence = min(abs(price_change) / 5.0, 0.95)  # Normalize to 0.95 max
            elif price_change < threshold_sell and volatility < volatility_filter:
                action = 'SELL'
                confidence = min(abs(price_change) / 5.0, 0.95)
            
            # Volume confirmation
            if volume < 100000:  # Low volume reduces confidence
                confidence *= 0.7
            
            signal = {
                'action': action,
                'confidence': confidence,
                'entry_price': price,
                'stop_loss': price * (0.98 if action == 'BUY' else 1.02),
                'target_price': price * (1.05 if action == 'BUY' else 0.95),
                'reason': f"Momentum: {price_change:+.2f}% change, Volatility: {volatility:.2f}%"
            }
            
            self.record_signal(signal)
            return signal
            
        except Exception as e:
            logger.error(f"Error in momentum strategy analysis: {e}")
            return {'action': 'HOLD', 'confidence': 0.0}


class RSIStrategy(BaseStrategy):
    """Relative Strength Index (RSI) strategy"""
    
    def analyze(self, market_data: Dict) -> Dict:
        """
        Analyze market data using RSI indicator
        
        Strategy Logic:
        - Buy if RSI < oversold level
        - Sell if RSI > overbought level
        - Hold otherwise
        """
        try:
            rsi = market_data.get('rsi', 50)
            price = market_data.get('price', 0)
            
            oversold_level = self.params.get('oversold_level', 30)
            overbought_level = self.params.get('overbought_level', 70)
            upper_threshold = self.params.get('upper_threshold', 0.65)
            lower_threshold = self.params.get('lower_threshold', 0.35)
            
            # Normalize RSI to 0-1 range
            rsi_normalized = (rsi - lower_threshold * 100) / ((upper_threshold - lower_threshold) * 100)
            rsi_normalized = max(0.0, min(1.0, rsi_normalized))
            
            action = 'HOLD'
            confidence = 0.4
            reason = f"RSI: {rsi:.1f}"
            
            if rsi < oversold_level:
                action = 'BUY'
                confidence = (oversold_level - rsi) / oversold_level  # Higher confidence for deeper oversold
                confidence = min(confidence, 0.9)
                reason = f"RSI: {rsi:.1f} (OVERSOLD)"
            elif rsi > overbought_level:
                action = 'SELL'
                confidence = (rsi - overbought_level) / (100 - overbought_level)
                confidence = min(confidence, 0.9)
                reason = f"RSI: {rsi:.1f} (OVERBOUGHT)"
            
            signal = {
                'action': action,
                'confidence': confidence,
                'entry_price': price,
                'stop_loss': price * (0.97 if action == 'BUY' else 1.03),
                'target_price': price * (1.06 if action == 'BUY' else 0.94),
                'reason': reason
            }
            
            self.record_signal(signal)
            return signal
            
        except Exception as e:
            logger.error(f"Error in RSI strategy analysis: {e}")
            return {'action': 'HOLD', 'confidence': 0.0}


class MACDStrategy(BaseStrategy):
    """MACD (Moving Average Convergence Divergence) strategy"""
    
    def analyze(self, market_data: Dict) -> Dict:
        """
        Analyze market data using MACD indicator
        
        Strategy Logic:
        - Buy if MACD crosses above signal line
        - Sell if MACD crosses below signal line
        - Hold otherwise
        """
        try:
            macd = market_data.get('macd', 0)
            signal_line = market_data.get('macd_signal', 0)
            histogram = market_data.get('macd_histogram', 0)
            price = market_data.get('price', 0)
            
            threshold = self.params.get('threshold', 0.0001)
            
            action = 'HOLD'
            confidence = 0.3
            reason = f"MACD: {macd:.6f}, Signal: {signal_line:.6f}"
            
            # MACD crossover detection
            if histogram > threshold:
                action = 'BUY'
                confidence = min(abs(histogram) / 0.001, 0.9)  # Normalize confidence
                reason = f"MACD bullish crossover (Histogram: {histogram:.6f})"
            elif histogram < -threshold:
                action = 'SELL'
                confidence = min(abs(histogram) / 0.001, 0.9)
                reason = f"MACD bearish crossover (Histogram: {histogram:.6f})"
            
            signal = {
                'action': action,
                'confidence': confidence,
                'entry_price': price,
                'stop_loss': price * (0.96 if action == 'BUY' else 1.04),
                'target_price': price * (1.07 if action == 'BUY' else 0.93),
                'reason': reason
            }
            
            self.record_signal(signal)
            return signal
            
        except Exception as e:
            logger.error(f"Error in MACD strategy analysis: {e}")
            return {'action': 'HOLD', 'confidence': 0.0}


class MFIStrategy(BaseStrategy):
    """Money Flow Index (MFI) strategy"""
    
    def analyze(self, market_data: Dict) -> Dict:
        """
        Analyze market data using Money Flow Index
        
        Strategy Logic:
        - Buy if MFI < 20 (oversold)
        - Sell if MFI > 80 (overbought)
        - Hold otherwise
        """
        try:
            mfi = market_data.get('mfi', 50)
            price = market_data.get('price', 0)
            
            action = 'HOLD'
            confidence = 0.35
            reason = f"MFI: {mfi:.1f}"
            
            if mfi < 20:
                action = 'BUY'
                confidence = (20 - mfi) / 20 * 0.9
                reason = f"MFI: {mfi:.1f} (OVERSOLD)"
            elif mfi > 80:
                action = 'SELL'
                confidence = (mfi - 80) / 20 * 0.9
                reason = f"MFI: {mfi:.1f} (OVERBOUGHT)"
            
            signal = {
                'action': action,
                'confidence': confidence,
                'entry_price': price,
                'stop_loss': price * (0.97 if action == 'BUY' else 1.03),
                'target_price': price * (1.06 if action == 'BUY' else 0.94),
                'reason': reason
            }
            
            self.record_signal(signal)
            return signal
            
        except Exception as e:
            logger.error(f"Error in MFI strategy analysis: {e}")
            return {'action': 'HOLD', 'confidence': 0.0}


class VWAPStrategy(BaseStrategy):
    """Volume Weighted Average Price (VWAP) strategy"""
    
    def analyze(self, market_data: Dict) -> Dict:
        """
        Analyze market data using VWAP
        
        Strategy Logic:
        - Buy if price < VWAP
        - Sell if price > VWAP
        - Hold if price near VWAP
        """
        try:
            price = market_data.get('price', 0)
            vwap = market_data.get('vwap', price)
            volume = market_data.get('volume', 0)
            
            deviation_pct = abs(price - vwap) / vwap * 100
            vwap_distance = self.params.get('vwap_distance', 1.0)  # % deviation
            
            action = 'HOLD'
            confidence = 0.3
            reason = f"Price: {price:.2f}, VWAP: {vwap:.2f}, Dev: {deviation_pct:.2f}%"
            
            if price < vwap and deviation_pct > vwap_distance:
                action = 'BUY'
                confidence = min(deviation_pct / 3.0, 0.9)
                reason = f"Price below VWAP by {deviation_pct:.2f}%"
            elif price > vwap and deviation_pct > vwap_distance:
                action = 'SELL'
                confidence = min(deviation_pct / 3.0, 0.9)
                reason = f"Price above VWAP by {deviation_pct:.2f}%"
            
            # Volume confirmation
            if volume < 50000:
                confidence *= 0.6
            
            signal = {
                'action': action,
                'confidence': confidence,
                'entry_price': price,
                'stop_loss': vwap * (0.99 if action == 'BUY' else 1.01),
                'target_price': vwap * (1.03 if action == 'BUY' else 0.97),
                'reason': reason
            }
            
            self.record_signal(signal)
            return signal
            
        except Exception as e:
            logger.error(f"Error in VWAP strategy analysis: {e}")
            return {'action': 'HOLD', 'confidence': 0.0}


class StrategyResolver:
    """Factory for resolving and loading trading strategies"""
    
    # Registry of available strategies
    STRATEGIES = {
        'momentum': MomentumStrategy,
        'rsi': RSIStrategy,
        'macd': MACDStrategy,
        'mfi': MFIStrategy,
        'vwap': VWAPStrategy
    }
    
    @classmethod
    def get_available_strategies(cls) -> List[str]:
        """Get list of available strategy names"""
        return list(cls.STRATEGIES.keys())
    
    @classmethod
    def get_strategy(cls, strategy_name: str, **params) -> BaseStrategy:
        """
        Get strategy instance by name
        
        Args:
            strategy_name: Name of strategy (momentum, rsi, macd, mfi, vwap)
            **params: Strategy-specific parameters
        
        Returns:
            Strategy instance
        
        Raises:
            ValueError: If strategy not found
        """
        strategy_name = strategy_name.lower()
        
        if strategy_name not in cls.STRATEGIES:
            available = ', '.join(cls.get_available_strategies())
            raise ValueError(
                f"Unknown strategy: {strategy_name}\n"
                f"Available strategies: {available}"
            )
        
        strategy_class = cls.STRATEGIES[strategy_name]
        logger.info(f"✅ Loaded strategy: {strategy_name}")
        return strategy_class(**params)
    
    @classmethod
    def register_strategy(cls, name: str, strategy_class: type):
        """
        Register a custom strategy
        
        Args:
            name: Strategy name
            strategy_class: Strategy class (must inherit from BaseStrategy)
        """
        if not issubclass(strategy_class, BaseStrategy):
            raise TypeError(f"{strategy_class} must inherit from BaseStrategy")
        
        cls.STRATEGIES[name.lower()] = strategy_class
        logger.info(f"✅ Registered custom strategy: {name}")
    
    @classmethod
    def generate_signal(cls, strategy: BaseStrategy, market_data: Dict) -> Dict:
        """
        Generate trading signal using strategy
        
        Args:
            strategy: Strategy instance
            market_data: Market data dictionary
        
        Returns:
            Trading signal
        """
        return strategy.analyze(market_data)
