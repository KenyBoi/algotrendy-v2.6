"""
Technical Indicators Configuration and Calculation

Provides metadata and calculation functions for supported indicators.
"""

from typing import Dict, List, Any
import numpy as np
import pandas as pd


# Available Indicators Configuration
AVAILABLE_INDICATORS = {
    "sma": {
        "name": "Simple Moving Average",
        "description": "Average price over N periods",
        "category": "trend",
        "params": {
            "period": {
                "type": "int",
                "default": 20,
                "min": 2,
                "max": 200,
                "description": "Number of periods"
            }
        }
    },
    "ema": {
        "name": "Exponential Moving Average",
        "description": "Exponentially weighted moving average",
        "category": "trend",
        "params": {
            "period": {
                "type": "int",
                "default": 12,
                "min": 2,
                "max": 200,
                "description": "Number of periods"
            }
        }
    },
    "rsi": {
        "name": "Relative Strength Index",
        "description": "Momentum oscillator (0-100)",
        "category": "momentum",
        "params": {
            "period": {
                "type": "int",
                "default": 14,
                "min": 2,
                "max": 50,
                "description": "RSI period"
            }
        }
    },
    "macd": {
        "name": "MACD",
        "description": "Moving Average Convergence Divergence",
        "category": "momentum",
        "params": {
            "fast": {
                "type": "int",
                "default": 12,
                "min": 2,
                "max": 50,
                "description": "Fast EMA period"
            },
            "slow": {
                "type": "int",
                "default": 26,
                "min": 2,
                "max": 100,
                "description": "Slow EMA period"
            },
            "signal": {
                "type": "int",
                "default": 9,
                "min": 2,
                "max": 50,
                "description": "Signal line period"
            }
        }
    },
    "bollinger": {
        "name": "Bollinger Bands",
        "description": "Volatility bands around moving average",
        "category": "volatility",
        "params": {
            "period": {
                "type": "int",
                "default": 20,
                "min": 2,
                "max": 100,
                "description": "MA period"
            },
            "std": {
                "type": "float",
                "default": 2.0,
                "min": 0.5,
                "max": 4.0,
                "description": "Standard deviations"
            }
        }
    },
    "atr": {
        "name": "Average True Range",
        "description": "Volatility indicator",
        "category": "volatility",
        "params": {
            "period": {
                "type": "int",
                "default": 14,
                "min": 2,
                "max": 50,
                "description": "ATR period"
            }
        }
    },
    "stochastic": {
        "name": "Stochastic Oscillator",
        "description": "Momentum oscillator comparing close to range",
        "category": "momentum",
        "params": {
            "k": {
                "type": "int",
                "default": 14,
                "min": 2,
                "max": 50,
                "description": "%K period"
            },
            "d": {
                "type": "int",
                "default": 3,
                "min": 2,
                "max": 20,
                "description": "%D period"
            }
        }
    },
    "volume": {
        "name": "Volume",
        "description": "Trading volume indicator",
        "category": "volume",
        "params": {}
    }
}


def calculate_sma(data: pd.Series, period: int) -> pd.Series:
    """Calculate Simple Moving Average"""
    return data.rolling(window=period).mean()


def calculate_ema(data: pd.Series, period: int) -> pd.Series:
    """Calculate Exponential Moving Average"""
    return data.ewm(span=period, adjust=False).mean()


def calculate_rsi(data: pd.Series, period: int = 14) -> pd.Series:
    """Calculate Relative Strength Index"""
    delta = data.diff()
    gain = (delta.where(delta > 0, 0)).rolling(window=period).mean()
    loss = (-delta.where(delta < 0, 0)).rolling(window=period).mean()

    rs = gain / loss
    rsi = 100 - (100 / (1 + rs))
    return rsi


def calculate_macd(data: pd.Series, fast: int = 12, slow: int = 26, signal: int = 9) -> Dict[str, pd.Series]:
    """Calculate MACD"""
    ema_fast = calculate_ema(data, fast)
    ema_slow = calculate_ema(data, slow)
    macd_line = ema_fast - ema_slow
    signal_line = calculate_ema(macd_line, signal)
    histogram = macd_line - signal_line

    return {
        "macd": macd_line,
        "signal": signal_line,
        "histogram": histogram
    }


def calculate_bollinger_bands(data: pd.Series, period: int = 20, std_dev: float = 2.0) -> Dict[str, pd.Series]:
    """Calculate Bollinger Bands"""
    sma = calculate_sma(data, period)
    std = data.rolling(window=period).std()

    upper_band = sma + (std * std_dev)
    lower_band = sma - (std * std_dev)

    return {
        "upper": upper_band,
        "middle": sma,
        "lower": lower_band
    }


def calculate_atr(high: pd.Series, low: pd.Series, close: pd.Series, period: int = 14) -> pd.Series:
    """Calculate Average True Range"""
    high_low = high - low
    high_close = np.abs(high - close.shift())
    low_close = np.abs(low - close.shift())

    tr = pd.concat([high_low, high_close, low_close], axis=1).max(axis=1)
    atr = tr.rolling(window=period).mean()

    return atr


def calculate_stochastic(high: pd.Series, low: pd.Series, close: pd.Series, k_period: int = 14, d_period: int = 3) -> Dict[str, pd.Series]:
    """Calculate Stochastic Oscillator"""
    lowest_low = low.rolling(window=k_period).min()
    highest_high = high.rolling(window=k_period).max()

    k = 100 * ((close - lowest_low) / (highest_high - lowest_low))
    d = k.rolling(window=d_period).mean()

    return {
        "k": k,
        "d": d
    }


def calculate_indicators(df: pd.DataFrame, enabled_indicators: Dict[str, bool], params: Dict[str, Any]) -> pd.DataFrame:
    """
    Calculate all enabled indicators and add them to the dataframe

    Args:
        df: DataFrame with OHLCV data (columns: open, high, low, close, volume)
        enabled_indicators: Dict of indicator names and whether they're enabled
        params: Dict of indicator parameters

    Returns:
        DataFrame with indicator columns added
    """
    result_df = df.copy()

    # SMA
    if enabled_indicators.get("sma", False):
        period = params.get("sma_period", 20)
        result_df[f'sma_{period}'] = calculate_sma(df['close'], period)

    # EMA
    if enabled_indicators.get("ema", False):
        period = params.get("ema_period", 12)
        result_df[f'ema_{period}'] = calculate_ema(df['close'], period)

    # RSI
    if enabled_indicators.get("rsi", False):
        period = params.get("rsi_period", 14)
        result_df[f'rsi_{period}'] = calculate_rsi(df['close'], period)

    # MACD
    if enabled_indicators.get("macd", False):
        fast = params.get("macd_fast", 12)
        slow = params.get("macd_slow", 26)
        signal = params.get("macd_signal", 9)
        macd_result = calculate_macd(df['close'], fast, slow, signal)
        result_df['macd'] = macd_result['macd']
        result_df['macd_signal'] = macd_result['signal']
        result_df['macd_histogram'] = macd_result['histogram']

    # Bollinger Bands
    if enabled_indicators.get("bollinger", False):
        period = params.get("bollinger_period", 20)
        std_dev = params.get("bollinger_std", 2.0)
        bb_result = calculate_bollinger_bands(df['close'], period, std_dev)
        result_df['bb_upper'] = bb_result['upper']
        result_df['bb_middle'] = bb_result['middle']
        result_df['bb_lower'] = bb_result['lower']

    # ATR
    if enabled_indicators.get("atr", False):
        period = params.get("atr_period", 14)
        result_df[f'atr_{period}'] = calculate_atr(df['high'], df['low'], df['close'], period)

    # Stochastic
    if enabled_indicators.get("stochastic", False):
        k_period = params.get("stochastic_k", 14)
        d_period = params.get("stochastic_d", 3)
        stoch_result = calculate_stochastic(df['high'], df['low'], df['close'], k_period, d_period)
        result_df[f'stoch_k'] = stoch_result['k']
        result_df[f'stoch_d'] = stoch_result['d']

    # Volume (already in dataframe, just ensure it's there)
    if enabled_indicators.get("volume", False):
        if 'volume' not in result_df.columns:
            result_df['volume'] = 0

    return result_df
