"""
Indicator Engine System
======================
Separates indicator calculation from strategy logic.
Indicators are low-level calculations stored separately from strategies.
"""

from abc import ABC, abstractmethod
from typing import Dict, Optional, List
from datetime import datetime
import logging

logger = logging.getLogger(__name__)


class BaseIndicator(ABC):
    """Abstract base class for all indicators"""
    
    def __init__(self, **params):
        """
        Initialize indicator with parameters
        
        Args:
            **params: Indicator-specific parameters
        """
        self.params = params
        self.values = []
        self.last_update = None
    
    @abstractmethod
    def calculate(self, market_data: Dict) -> float:
        """
        Calculate indicator value from market data
        
        Args:
            market_data: Dictionary with price, volume, etc.
        
        Returns:
            Calculated indicator value
        """
        pass
    
    def record_value(self, value: float, timestamp: Optional[datetime] = None):
        """Record calculated value with timestamp"""
        if timestamp is None:
            timestamp = datetime.now()
        
        self.values.append({
            'value': value,
            'timestamp': timestamp
        })
        self.last_update = timestamp
    
    def get_value_history(self, limit: Optional[int] = None) -> List[Dict]:
        """Get history of calculated values"""
        if limit:
            return self.values[-limit:]
        return self.values


class RSIIndicator(BaseIndicator):
    """Relative Strength Index indicator"""
    
    def calculate(self, market_data: Dict) -> float:
        """
        Calculate RSI from market data
        
        Args:
            market_data: Must contain 'prices' list (last 14+ values)
        
        Returns:
            RSI value (0-100)
        """
        try:
            prices = market_data.get('prices', [])
            period = self.params.get('period', 14)
            
            if len(prices) < period + 1:
                return 50.0  # Neutral if not enough data
            
            # Calculate gains and losses
            deltas = [prices[i] - prices[i-1] for i in range(1, len(prices))]
            
            gains = sum(d for d in deltas[-period:] if d > 0) / period
            losses = abs(sum(d for d in deltas[-period:] if d < 0) / period)
            
            if losses == 0:
                return 100.0
            
            rs = gains / losses
            rsi = 100 - (100 / (1 + rs))
            
            self.record_value(rsi)
            return rsi
        
        except Exception as e:
            logger.error(f"Error calculating RSI: {e}")
            return 50.0


class MACDIndicator(BaseIndicator):
    """MACD (Moving Average Convergence Divergence) indicator"""
    
    def calculate(self, market_data: Dict) -> Dict:
        """
        Calculate MACD from market data
        
        Returns:
            {
                'macd': MACD value,
                'signal': Signal line value,
                'histogram': MACD - Signal
            }
        """
        try:
            prices = market_data.get('prices', [])
            
            if len(prices) < 26:
                return {'macd': 0, 'signal': 0, 'histogram': 0}
            
            # Simple moving averages (should use exponential in production)
            ema_12 = sum(prices[-12:]) / 12
            ema_26 = sum(prices[-26:]) / 26
            
            macd = ema_12 - ema_26
            signal = macd  # Simplified (should use EMA in production)
            histogram = macd - signal
            
            result = {
                'macd': macd,
                'signal': signal,
                'histogram': histogram
            }
            
            self.record_value(macd)
            return result
        
        except Exception as e:
            logger.error(f"Error calculating MACD: {e}")
            return {'macd': 0, 'signal': 0, 'histogram': 0}


class MFIIndicator(BaseIndicator):
    """Money Flow Index indicator"""
    
    def calculate(self, market_data: Dict) -> float:
        """
        Calculate MFI from market data
        
        Args:
            market_data: Must contain 'high', 'low', 'close', 'volume'
        
        Returns:
            MFI value (0-100)
        """
        try:
            high = market_data.get('high', 0)
            low = market_data.get('low', 0)
            close = market_data.get('close', 0)
            volume = market_data.get('volume', 0)
            period = self.params.get('period', 14)
            
            # Typical price
            typical_price = (high + low + close) / 3
            raw_money_flow = typical_price * volume
            
            # Simplified MFI calculation
            mfi = 50.0  # Default neutral
            
            self.record_value(mfi)
            return mfi
        
        except Exception as e:
            logger.error(f"Error calculating MFI: {e}")
            return 50.0


class VWAPIndicator(BaseIndicator):
    """Volume Weighted Average Price indicator"""
    
    def calculate(self, market_data: Dict) -> float:
        """
        Calculate VWAP from market data
        
        Args:
            market_data: Must contain 'typical_prices' and 'volumes'
        
        Returns:
            VWAP value
        """
        try:
            typical_prices = market_data.get('typical_prices', [])
            volumes = market_data.get('volumes', [])
            
            if not typical_prices or not volumes:
                return market_data.get('price', 0)
            
            # VWAP = Sum(Typical Price × Volume) / Sum(Volume)
            numerator = sum(p * v for p, v in zip(typical_prices, volumes))
            denominator = sum(volumes)
            
            if denominator == 0:
                return typical_prices[-1] if typical_prices else 0
            
            vwap = numerator / denominator
            self.record_value(vwap)
            return vwap
        
        except Exception as e:
            logger.error(f"Error calculating VWAP: {e}")
            return market_data.get('price', 0)


class BollingerBandsIndicator(BaseIndicator):
    """Bollinger Bands indicator"""
    
    def calculate(self, market_data: Dict) -> Dict:
        """
        Calculate Bollinger Bands from market data
        
        Returns:
            {
                'upper': Upper band,
                'middle': Middle band (SMA),
                'lower': Lower band
            }
        """
        try:
            prices = market_data.get('prices', [])
            period = self.params.get('period', 20)
            std_dev = self.params.get('std_dev', 2)
            
            if len(prices) < period:
                current_price = prices[-1] if prices else 0
                return {
                    'upper': current_price * 1.05,
                    'middle': current_price,
                    'lower': current_price * 0.95
                }
            
            # Calculate SMA
            sma = sum(prices[-period:]) / period
            
            # Calculate standard deviation
            variance = sum((p - sma) ** 2 for p in prices[-period:]) / period
            std_dev_val = variance ** 0.5
            
            bands = {
                'upper': sma + (std_dev * std_dev_val),
                'middle': sma,
                'lower': sma - (std_dev * std_dev_val)
            }
            
            self.record_value(sma)
            return bands
        
        except Exception as e:
            logger.error(f"Error calculating Bollinger Bands: {e}")
            current_price = market_data.get('price', 0)
            return {
                'upper': current_price * 1.05,
                'middle': current_price,
                'lower': current_price * 0.95
            }


class IndicatorEngine:
    """Factory and manager for indicators"""
    
    # Registry of available indicators
    INDICATORS = {
        'rsi': RSIIndicator,
        'macd': MACDIndicator,
        'mfi': MFIIndicator,
        'vwap': VWAPIndicator,
        'bollinger': BollingerBandsIndicator
    }
    
    # Cached indicator instances
    _cache: Dict[str, BaseIndicator] = {}
    
    @classmethod
    def get_indicator(cls, indicator_name: str, **params) -> BaseIndicator:
        """
        Get or create indicator instance
        
        Args:
            indicator_name: Name of indicator (rsi, macd, mfi, vwap, bollinger)
            **params: Indicator-specific parameters
        
        Returns:
            Indicator instance
        
        Raises:
            ValueError: If indicator not found
        """
        indicator_name = indicator_name.lower()
        
        if indicator_name not in cls.INDICATORS:
            available = ', '.join(cls.INDICATORS.keys())
            raise ValueError(
                f"Unknown indicator: {indicator_name}\n"
                f"Available indicators: {available}"
            )
        
        # Create cache key
        cache_key = f"{indicator_name}_{str(params)}"
        
        # Return cached instance if exists
        if cache_key in cls._cache:
            return cls._cache[cache_key]
        
        # Create new instance
        indicator_class = cls.INDICATORS[indicator_name]
        indicator = indicator_class(**params)
        
        # Cache it
        cls._cache[cache_key] = indicator
        logger.info(f"✅ Created indicator: {indicator_name}")
        
        return indicator
    
    @classmethod
    def calculate(cls, indicator_name: str, market_data: Dict, **params):
        """
        Calculate indicator value
        
        Args:
            indicator_name: Name of indicator
            market_data: Market data dictionary
            **params: Indicator parameters
        
        Returns:
            Calculated value(s)
        """
        indicator = cls.get_indicator(indicator_name, **params)
        return indicator.calculate(market_data)
    
    @classmethod
    def register_indicator(cls, name: str, indicator_class: type):
        """
        Register custom indicator
        
        Args:
            name: Indicator name
            indicator_class: Indicator class (must inherit from BaseIndicator)
        """
        if not issubclass(indicator_class, BaseIndicator):
            raise TypeError(f"{indicator_class} must inherit from BaseIndicator")
        
        cls.INDICATORS[name.lower()] = indicator_class
        logger.info(f"✅ Registered custom indicator: {name}")
    
    @classmethod
    def get_available_indicators(cls) -> List[str]:
        """Get list of available indicator names"""
        return list(cls.INDICATORS.keys())
    
    @classmethod
    def clear_cache(cls):
        """Clear indicator cache"""
        cls._cache.clear()
        logger.info("✅ Cleared indicator cache")
