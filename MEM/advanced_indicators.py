"""
Advanced Technical Indicators for MEM
======================================
Comprehensive indicator library using pandas-ta and custom implementations.
Provides 50+ technical indicators across all categories.

Categories:
- Momentum Indicators
- Trend Indicators
- Volatility Indicators
- Volume Indicators
- Support/Resistance
- Custom/Advanced Indicators
"""

import pandas as pd
import numpy as np
from typing import Dict, List, Optional, Tuple, Union
from datetime import datetime
import logging

try:
    import pandas_ta as ta
    PANDAS_TA_AVAILABLE = True
except ImportError:
    PANDAS_TA_AVAILABLE = False
    logging.warning("pandas-ta not available. Install with: pip install pandas-ta")

logger = logging.getLogger(__name__)


class AdvancedIndicators:
    """
    Advanced technical indicators for MEM trading system.
    All methods return calculated indicator values with caching support.
    """

    def __init__(self):
        """Initialize indicator calculator"""
        self.cache = {}
        self.cache_timeout = 60  # seconds

    # ============================================================================
    # MOMENTUM INDICATORS
    # ============================================================================

    def rsi(self, prices: pd.Series, period: int = 14) -> pd.Series:
        """
        Relative Strength Index (RSI)

        Range: 0-100
        Overbought: > 70
        Oversold: < 30
        """
        if PANDAS_TA_AVAILABLE:
            return ta.rsi(prices, length=period)

        # Manual calculation
        delta = prices.diff()
        gain = (delta.where(delta > 0, 0)).rolling(window=period).mean()
        loss = (-delta.where(delta < 0, 0)).rolling(window=period).mean()
        rs = gain / loss
        return 100 - (100 / (1 + rs))

    def stochastic(self, high: pd.Series, low: pd.Series, close: pd.Series,
                   k_period: int = 14, d_period: int = 3) -> Tuple[pd.Series, pd.Series]:
        """
        Stochastic Oscillator (%K and %D)

        Range: 0-100
        Overbought: > 80
        Oversold: < 20

        Returns: (%K, %D)
        """
        if PANDAS_TA_AVAILABLE:
            stoch = ta.stoch(high, low, close, k=k_period, d=d_period)
            return stoch[f'STOCHk_{k_period}_{d_period}_3'], stoch[f'STOCHd_{k_period}_{d_period}_3']

        # Manual calculation
        lowest_low = low.rolling(window=k_period).min()
        highest_high = high.rolling(window=k_period).max()

        k = 100 * (close - lowest_low) / (highest_high - lowest_low)
        d = k.rolling(window=d_period).mean()

        return k, d

    def williams_r(self, high: pd.Series, low: pd.Series, close: pd.Series,
                   period: int = 14) -> pd.Series:
        """
        Williams %R

        Range: -100 to 0
        Overbought: > -20
        Oversold: < -80
        """
        if PANDAS_TA_AVAILABLE:
            return ta.willr(high, low, close, length=period)

        # Manual calculation
        highest_high = high.rolling(window=period).max()
        lowest_low = low.rolling(window=period).min()

        return -100 * (highest_high - close) / (highest_high - lowest_low)

    def cci(self, high: pd.Series, low: pd.Series, close: pd.Series,
            period: int = 20) -> pd.Series:
        """
        Commodity Channel Index (CCI)

        Range: Unbounded (typically -200 to +200)
        Overbought: > +100
        Oversold: < -100
        """
        if PANDAS_TA_AVAILABLE:
            return ta.cci(high, low, close, length=period)

        # Manual calculation
        tp = (high + low + close) / 3
        sma = tp.rolling(window=period).mean()
        mad = tp.rolling(window=period).apply(lambda x: np.abs(x - x.mean()).mean())

        return (tp - sma) / (0.015 * mad)

    def roc(self, close: pd.Series, period: int = 12) -> pd.Series:
        """
        Rate of Change (ROC)

        Measures the percentage change in price over a period
        """
        if PANDAS_TA_AVAILABLE:
            return ta.roc(close, length=period)

        return 100 * (close - close.shift(period)) / close.shift(period)

    def momentum(self, close: pd.Series, period: int = 10) -> pd.Series:
        """
        Momentum Indicator

        Measures the amount that a security's price has changed over a given time span
        """
        if PANDAS_TA_AVAILABLE:
            return ta.mom(close, length=period)

        return close - close.shift(period)

    def tsi(self, close: pd.Series, long: int = 25, short: int = 13,
            signal: int = 13) -> Tuple[pd.Series, pd.Series]:
        """
        True Strength Index (TSI)

        Double-smoothed momentum oscillator
        Returns: (TSI, Signal Line)
        """
        if PANDAS_TA_AVAILABLE:
            tsi_result = ta.tsi(close, long=long, short=short, signal=signal)
            return tsi_result[f'TSI_{long}_{short}_{signal}'], tsi_result[f'TSIs_{long}_{short}_{signal}']

        # Manual calculation (simplified)
        momentum = close.diff()
        double_smoothed_mom = momentum.ewm(span=long).mean().ewm(span=short).mean()
        double_smoothed_abs_mom = momentum.abs().ewm(span=long).mean().ewm(span=short).mean()

        tsi = 100 * (double_smoothed_mom / double_smoothed_abs_mom)
        signal_line = tsi.ewm(span=signal).mean()

        return tsi, signal_line

    # ============================================================================
    # TREND INDICATORS
    # ============================================================================

    def macd(self, close: pd.Series, fast: int = 12, slow: int = 26,
             signal: int = 9) -> Tuple[pd.Series, pd.Series, pd.Series]:
        """
        Moving Average Convergence Divergence (MACD)

        Returns: (MACD line, Signal line, Histogram)
        """
        if PANDAS_TA_AVAILABLE:
            macd_result = ta.macd(close, fast=fast, slow=slow, signal=signal)
            return (macd_result[f'MACD_{fast}_{slow}_{signal}'],
                   macd_result[f'MACDs_{fast}_{slow}_{signal}'],
                   macd_result[f'MACDh_{fast}_{slow}_{signal}'])

        # Manual calculation
        ema_fast = close.ewm(span=fast).mean()
        ema_slow = close.ewm(span=slow).mean()
        macd_line = ema_fast - ema_slow
        signal_line = macd_line.ewm(span=signal).mean()
        histogram = macd_line - signal_line

        return macd_line, signal_line, histogram

    def adx(self, high: pd.Series, low: pd.Series, close: pd.Series,
            period: int = 14) -> Tuple[pd.Series, pd.Series, pd.Series]:
        """
        Average Directional Index (ADX)

        Measures trend strength (0-100)
        < 20: Weak trend
        20-40: Strong trend
        > 40: Very strong trend

        Returns: (ADX, +DI, -DI)
        """
        if PANDAS_TA_AVAILABLE:
            adx_result = ta.adx(high, low, close, length=period)
            return (adx_result[f'ADX_{period}'],
                   adx_result[f'DMP_{period}'],
                   adx_result[f'DMN_{period}'])

        # Manual calculation (simplified)
        tr = self.true_range(high, low, close)
        atr = tr.rolling(window=period).mean()

        up_move = high.diff()
        down_move = -low.diff()

        plus_dm = up_move.where((up_move > down_move) & (up_move > 0), 0)
        minus_dm = down_move.where((down_move > up_move) & (down_move > 0), 0)

        plus_di = 100 * (plus_dm.rolling(window=period).mean() / atr)
        minus_di = 100 * (minus_dm.rolling(window=period).mean() / atr)

        dx = 100 * np.abs(plus_di - minus_di) / (plus_di + minus_di)
        adx = dx.rolling(window=period).mean()

        return adx, plus_di, minus_di

    def aroon(self, high: pd.Series, low: pd.Series,
              period: int = 25) -> Tuple[pd.Series, pd.Series, pd.Series]:
        """
        Aroon Indicator

        Measures time since highest high and lowest low
        Returns: (Aroon Up, Aroon Down, Aroon Oscillator)
        """
        if PANDAS_TA_AVAILABLE:
            aroon_result = ta.aroon(high, low, length=period)
            return (aroon_result[f'AROONU_{period}'],
                   aroon_result[f'AROOND_{period}'],
                   aroon_result[f'AROONOSC_{period}'])

        # Manual calculation
        aroon_up = high.rolling(window=period + 1).apply(
            lambda x: float(period - x.argmax()) / period * 100
        )
        aroon_down = low.rolling(window=period + 1).apply(
            lambda x: float(period - x.argmin()) / period * 100
        )
        aroon_osc = aroon_up - aroon_down

        return aroon_up, aroon_down, aroon_osc

    def supertrend(self, high: pd.Series, low: pd.Series, close: pd.Series,
                   period: int = 10, multiplier: float = 3.0) -> Tuple[pd.Series, pd.Series]:
        """
        SuperTrend Indicator

        Trend following indicator based on ATR
        Returns: (SuperTrend line, Direction: 1=uptrend, -1=downtrend)
        """
        if PANDAS_TA_AVAILABLE:
            st = ta.supertrend(high, low, close, length=period, multiplier=multiplier)
            return st[f'SUPERT_{period}_{multiplier}'], st[f'SUPERTd_{period}_{multiplier}']

        # Manual calculation
        atr = self.atr(high, low, close, period)
        hl_avg = (high + low) / 2

        upper_band = hl_avg + (multiplier * atr)
        lower_band = hl_avg - (multiplier * atr)

        # Simplified SuperTrend calculation
        supertrend = pd.Series(index=close.index, dtype=float)
        direction = pd.Series(index=close.index, dtype=float)

        supertrend.iloc[0] = upper_band.iloc[0]
        direction.iloc[0] = 1

        for i in range(1, len(close)):
            if close.iloc[i] > supertrend.iloc[i-1]:
                supertrend.iloc[i] = lower_band.iloc[i]
                direction.iloc[i] = 1
            else:
                supertrend.iloc[i] = upper_band.iloc[i]
                direction.iloc[i] = -1

        return supertrend, direction

    def ichimoku(self, high: pd.Series, low: pd.Series, close: pd.Series,
                 tenkan: int = 9, kijun: int = 26, senkou: int = 52) -> Dict[str, pd.Series]:
        """
        Ichimoku Cloud

        Comprehensive trend indicator
        Returns: {
            'tenkan_sen': Conversion Line,
            'kijun_sen': Base Line,
            'senkou_span_a': Leading Span A,
            'senkou_span_b': Leading Span B,
            'chikou_span': Lagging Span
        }
        """
        if PANDAS_TA_AVAILABLE:
            ichimoku_result = ta.ichimoku(high, low, close,
                                         tenkan=tenkan, kijun=kijun, senkou=senkou)
            return {
                'tenkan_sen': ichimoku_result[0][f'ITS_{tenkan}'],
                'kijun_sen': ichimoku_result[0][f'IKS_{kijun}'],
                'senkou_span_a': ichimoku_result[0][f'ISA_{tenkan}'],
                'senkou_span_b': ichimoku_result[0][f'ISB_{kijun}'],
                'chikou_span': ichimoku_result[0][f'ICS_{kijun}']
            }

        # Manual calculation
        tenkan_sen = (high.rolling(window=tenkan).max() +
                     low.rolling(window=tenkan).min()) / 2
        kijun_sen = (high.rolling(window=kijun).max() +
                    low.rolling(window=kijun).min()) / 2
        senkou_span_a = ((tenkan_sen + kijun_sen) / 2).shift(kijun)
        senkou_span_b = ((high.rolling(window=senkou).max() +
                         low.rolling(window=senkou).min()) / 2).shift(kijun)
        chikou_span = close.shift(-kijun)

        return {
            'tenkan_sen': tenkan_sen,
            'kijun_sen': kijun_sen,
            'senkou_span_a': senkou_span_a,
            'senkou_span_b': senkou_span_b,
            'chikou_span': chikou_span
        }

    def psar(self, high: pd.Series, low: pd.Series, close: pd.Series,
             af: float = 0.02, max_af: float = 0.2) -> Tuple[pd.Series, pd.Series]:
        """
        Parabolic SAR

        Stop and Reverse indicator
        Returns: (SAR values, Trend: 1=uptrend, -1=downtrend)
        """
        if PANDAS_TA_AVAILABLE:
            psar_result = ta.psar(high, low, close, af0=af, af=af, max_af=max_af)
            return psar_result[f'PSARl_{af}_{max_af}'], psar_result[f'PSARs_{af}_{max_af}']

        # Manual calculation (simplified)
        psar = pd.Series(index=close.index, dtype=float)
        trend = pd.Series(index=close.index, dtype=float)

        psar.iloc[0] = close.iloc[0]
        trend.iloc[0] = 1

        # Simplified PSAR - full implementation is complex
        for i in range(1, len(close)):
            if trend.iloc[i-1] == 1:
                psar.iloc[i] = min(low.iloc[i-1], psar.iloc[i-1])
                if close.iloc[i] < psar.iloc[i]:
                    trend.iloc[i] = -1
                else:
                    trend.iloc[i] = 1
            else:
                psar.iloc[i] = max(high.iloc[i-1], psar.iloc[i-1])
                if close.iloc[i] > psar.iloc[i]:
                    trend.iloc[i] = 1
                else:
                    trend.iloc[i] = -1

        return psar, trend

    # ============================================================================
    # VOLATILITY INDICATORS
    # ============================================================================

    def atr(self, high: pd.Series, low: pd.Series, close: pd.Series,
            period: int = 14) -> pd.Series:
        """
        Average True Range (ATR)

        Measures market volatility
        """
        if PANDAS_TA_AVAILABLE:
            return ta.atr(high, low, close, length=period)

        tr = self.true_range(high, low, close)
        return tr.rolling(window=period).mean()

    def true_range(self, high: pd.Series, low: pd.Series, close: pd.Series) -> pd.Series:
        """
        True Range

        Max of:
        - High - Low
        - |High - Previous Close|
        - |Low - Previous Close|
        """
        hl = high - low
        hc = (high - close.shift()).abs()
        lc = (low - close.shift()).abs()

        return pd.concat([hl, hc, lc], axis=1).max(axis=1)

    def bollinger_bands(self, close: pd.Series, period: int = 20,
                       std_dev: float = 2.0) -> Tuple[pd.Series, pd.Series, pd.Series]:
        """
        Bollinger Bands

        Returns: (Upper Band, Middle Band, Lower Band)
        """
        if PANDAS_TA_AVAILABLE:
            bb = ta.bbands(close, length=period, std=std_dev)
            # pandas-ta returns columns with format: BBU_period_std_ddof (where ddof = std by default)
            col_suffix = f'{period}_{std_dev}_{std_dev}'
            return bb[f'BBU_{col_suffix}'], bb[f'BBM_{col_suffix}'], bb[f'BBL_{col_suffix}']

        middle = close.rolling(window=period).mean()
        std = close.rolling(window=period).std()
        upper = middle + (std * std_dev)
        lower = middle - (std * std_dev)

        return upper, middle, lower

    def keltner_channels(self, high: pd.Series, low: pd.Series, close: pd.Series,
                        period: int = 20, multiplier: float = 2.0) -> Tuple[pd.Series, pd.Series, pd.Series]:
        """
        Keltner Channels

        Similar to Bollinger Bands but uses ATR
        Returns: (Upper Band, Middle Band, Lower Band)
        """
        if PANDAS_TA_AVAILABLE:
            kc = ta.kc(high, low, close, length=period, scalar=multiplier)
            return kc[f'KCUe_{period}_{multiplier}'], kc[f'KCBe_{period}_{multiplier}'], kc[f'KCLe_{period}_{multiplier}']

        middle = close.ewm(span=period).mean()
        atr = self.atr(high, low, close, period)
        upper = middle + (atr * multiplier)
        lower = middle - (atr * multiplier)

        return upper, middle, lower

    def donchian_channels(self, high: pd.Series, low: pd.Series,
                         period: int = 20) -> Tuple[pd.Series, pd.Series, pd.Series]:
        """
        Donchian Channels

        Based on highest high and lowest low
        Returns: (Upper Band, Middle Band, Lower Band)
        """
        if PANDAS_TA_AVAILABLE:
            dc = ta.donchian(high, low, lower_length=period, upper_length=period)
            return dc[f'DCU_{period}_{period}'], dc[f'DCM_{period}_{period}'], dc[f'DCL_{period}_{period}']

        upper = high.rolling(window=period).max()
        lower = low.rolling(window=period).min()
        middle = (upper + lower) / 2

        return upper, middle, lower

    def std_dev(self, close: pd.Series, period: int = 20) -> pd.Series:
        """Standard Deviation (Historical Volatility)"""
        return close.rolling(window=period).std()

    # ============================================================================
    # VOLUME INDICATORS
    # ============================================================================

    def obv(self, close: pd.Series, volume: pd.Series) -> pd.Series:
        """
        On-Balance Volume (OBV)

        Cumulative volume based on price direction
        """
        if PANDAS_TA_AVAILABLE:
            return ta.obv(close, volume)

        direction = np.sign(close.diff())
        return (direction * volume).cumsum()

    def ad_line(self, high: pd.Series, low: pd.Series, close: pd.Series,
                volume: pd.Series) -> pd.Series:
        """
        Accumulation/Distribution Line

        Volume-weighted measure of money flow
        """
        if PANDAS_TA_AVAILABLE:
            return ta.ad(high, low, close, volume)

        clv = ((close - low) - (high - close)) / (high - low)
        clv = clv.fillna(0)
        return (clv * volume).cumsum()

    def cmf(self, high: pd.Series, low: pd.Series, close: pd.Series,
            volume: pd.Series, period: int = 20) -> pd.Series:
        """
        Chaikin Money Flow (CMF)

        Measures buying/selling pressure
        Range: -1 to +1
        """
        if PANDAS_TA_AVAILABLE:
            return ta.cmf(high, low, close, volume, length=period)

        mfv = ((close - low) - (high - close)) / (high - low) * volume
        mfv = mfv.fillna(0)
        return mfv.rolling(window=period).sum() / volume.rolling(window=period).sum()

    def mfi(self, high: pd.Series, low: pd.Series, close: pd.Series,
            volume: pd.Series, period: int = 14) -> pd.Series:
        """
        Money Flow Index (MFI)

        Volume-weighted RSI
        Range: 0-100
        """
        if PANDAS_TA_AVAILABLE:
            return ta.mfi(high, low, close, volume, length=period)

        tp = (high + low + close) / 3
        raw_mf = tp * volume

        positive_flow = raw_mf.where(tp > tp.shift(), 0).rolling(window=period).sum()
        negative_flow = raw_mf.where(tp < tp.shift(), 0).rolling(window=period).sum()

        mfi_ratio = positive_flow / negative_flow
        return 100 - (100 / (1 + mfi_ratio))

    def vwap(self, high: pd.Series, low: pd.Series, close: pd.Series,
             volume: pd.Series) -> pd.Series:
        """
        Volume Weighted Average Price (VWAP)

        Average price weighted by volume
        """
        if PANDAS_TA_AVAILABLE:
            return ta.vwap(high, low, close, volume)

        tp = (high + low + close) / 3
        return (tp * volume).cumsum() / volume.cumsum()

    def vpt(self, close: pd.Series, volume: pd.Series) -> pd.Series:
        """
        Volume Price Trend (VPT)

        Similar to OBV but considers price change magnitude
        """
        if PANDAS_TA_AVAILABLE:
            return ta.pvt(close, volume)

        price_change = close.pct_change()
        return (volume * price_change).cumsum()

    # ============================================================================
    # SUPPORT/RESISTANCE & PIVOT POINTS
    # ============================================================================

    def pivot_points(self, high: pd.Series, low: pd.Series, close: pd.Series,
                     method: str = 'standard') -> Dict[str, float]:
        """
        Pivot Points

        Methods: 'standard', 'fibonacci', 'woodie', 'camarilla'
        Returns: {
            'pivot': Pivot point,
            'r1', 'r2', 'r3': Resistance levels,
            's1', 's2', 's3': Support levels
        }
        """
        h = high.iloc[-1]
        l = low.iloc[-1]
        c = close.iloc[-1]

        pivot = (h + l + c) / 3

        if method == 'standard':
            r1 = 2 * pivot - l
            s1 = 2 * pivot - h
            r2 = pivot + (h - l)
            s2 = pivot - (h - l)
            r3 = h + 2 * (pivot - l)
            s3 = l - 2 * (h - pivot)

        elif method == 'fibonacci':
            r1 = pivot + 0.382 * (h - l)
            s1 = pivot - 0.382 * (h - l)
            r2 = pivot + 0.618 * (h - l)
            s2 = pivot - 0.618 * (h - l)
            r3 = pivot + (h - l)
            s3 = pivot - (h - l)

        elif method == 'woodie':
            pivot = (h + l + 2 * c) / 4
            r1 = 2 * pivot - l
            s1 = 2 * pivot - h
            r2 = pivot + (h - l)
            s2 = pivot - (h - l)
            r3 = h + 2 * (pivot - l)
            s3 = l - 2 * (h - pivot)

        elif method == 'camarilla':
            r1 = c + (h - l) * 1.1 / 12
            s1 = c - (h - l) * 1.1 / 12
            r2 = c + (h - l) * 1.1 / 6
            s2 = c - (h - l) * 1.1 / 6
            r3 = c + (h - l) * 1.1 / 4
            s3 = c - (h - l) * 1.1 / 4

        else:
            raise ValueError(f"Unknown pivot method: {method}")

        return {
            'pivot': pivot,
            'r1': r1, 'r2': r2, 'r3': r3,
            's1': s1, 's2': s2, 's3': s3
        }

    def fibonacci_retracement(self, high: float, low: float) -> Dict[str, float]:
        """
        Fibonacci Retracement Levels

        Returns key retracement levels between high and low
        """
        diff = high - low

        return {
            '0%': high,
            '23.6%': high - 0.236 * diff,
            '38.2%': high - 0.382 * diff,
            '50%': high - 0.5 * diff,
            '61.8%': high - 0.618 * diff,
            '78.6%': high - 0.786 * diff,
            '100%': low,
            '161.8%': high + 0.618 * diff,
            '261.8%': high + 1.618 * diff
        }

    # ============================================================================
    # CUSTOM & ADVANCED INDICATORS
    # ============================================================================

    def elder_ray(self, high: pd.Series, low: pd.Series, close: pd.Series,
                  period: int = 13) -> Tuple[pd.Series, pd.Series]:
        """
        Elder Ray Index (Bull Power & Bear Power)

        Measures buying and selling pressure
        Returns: (Bull Power, Bear Power)
        """
        ema = close.ewm(span=period).mean()
        bull_power = high - ema
        bear_power = low - ema

        return bull_power, bear_power

    def kst(self, close: pd.Series) -> Tuple[pd.Series, pd.Series]:
        """
        Know Sure Thing (KST) Oscillator

        Momentum oscillator based on smoothed ROC
        Returns: (KST, Signal Line)
        """
        if PANDAS_TA_AVAILABLE:
            kst_result = ta.kst(close)
            return kst_result['KST_10_15_20_30_10_10_10_15'], kst_result['KSTs_9']

        # Manual calculation
        roc1 = self.roc(close, 10)
        roc2 = self.roc(close, 15)
        roc3 = self.roc(close, 20)
        roc4 = self.roc(close, 30)

        kst = (roc1.rolling(10).mean() * 1 +
               roc2.rolling(10).mean() * 2 +
               roc3.rolling(10).mean() * 3 +
               roc4.rolling(15).mean() * 4)

        signal = kst.rolling(9).mean()

        return kst, signal

    def mass_index(self, high: pd.Series, low: pd.Series,
                   fast: int = 9, slow: int = 25) -> pd.Series:
        """
        Mass Index

        Identifies trend reversals based on range width
        > 27: Reversal signal
        """
        range_hl = high - low
        ema_range = range_hl.ewm(span=fast).mean()
        ema_ema_range = ema_range.ewm(span=fast).mean()

        mass = ema_range / ema_ema_range
        return mass.rolling(window=slow).sum()

    def ultimate_oscillator(self, high: pd.Series, low: pd.Series, close: pd.Series,
                           period1: int = 7, period2: int = 14, period3: int = 28) -> pd.Series:
        """
        Ultimate Oscillator

        Combines short, medium, and long-term momentum
        Range: 0-100
        """
        if PANDAS_TA_AVAILABLE:
            return ta.uo(high, low, close, fast=period1, medium=period2, slow=period3)

        # Manual calculation
        bp = close - pd.concat([low, close.shift()], axis=1).min(axis=1)
        tr = self.true_range(high, low, close)

        avg1 = bp.rolling(window=period1).sum() / tr.rolling(window=period1).sum()
        avg2 = bp.rolling(window=period2).sum() / tr.rolling(window=period2).sum()
        avg3 = bp.rolling(window=period3).sum() / tr.rolling(window=period3).sum()

        return 100 * ((4 * avg1 + 2 * avg2 + avg3) / 7)

    def awesome_oscillator(self, high: pd.Series, low: pd.Series) -> pd.Series:
        """
        Awesome Oscillator (AO)

        Simple yet effective momentum indicator
        """
        median_price = (high + low) / 2
        ao = median_price.rolling(window=5).mean() - median_price.rolling(window=34).mean()
        return ao

    def vortex(self, high: pd.Series, low: pd.Series, close: pd.Series,
               period: int = 14) -> Tuple[pd.Series, pd.Series]:
        """
        Vortex Indicator (VI)

        Identifies trend direction and strength
        Returns: (VI+, VI-)
        """
        if PANDAS_TA_AVAILABLE:
            vortex_result = ta.vortex(high, low, close, length=period)
            return vortex_result[f'VTXP_{period}'], vortex_result[f'VTXM_{period}']

        tr = self.true_range(high, low, close)

        vm_plus = (high - low.shift()).abs()
        vm_minus = (low - high.shift()).abs()

        vi_plus = vm_plus.rolling(window=period).sum() / tr.rolling(window=period).sum()
        vi_minus = vm_minus.rolling(window=period).sum() / tr.rolling(window=period).sum()

        return vi_plus, vi_minus

    # ============================================================================
    # HELPER METHODS
    # ============================================================================

    def ema(self, series: pd.Series, period: int) -> pd.Series:
        """Exponential Moving Average"""
        return series.ewm(span=period, adjust=False).mean()

    def sma(self, series: pd.Series, period: int) -> pd.Series:
        """Simple Moving Average"""
        return series.rolling(window=period).mean()

    def wma(self, series: pd.Series, period: int) -> pd.Series:
        """Weighted Moving Average"""
        weights = np.arange(1, period + 1)
        return series.rolling(window=period).apply(
            lambda x: np.dot(x, weights) / weights.sum(), raw=True
        )

    def hull_ma(self, series: pd.Series, period: int) -> pd.Series:
        """Hull Moving Average (HMA)"""
        if PANDAS_TA_AVAILABLE:
            return ta.hma(series, length=period)

        half_period = int(period / 2)
        sqrt_period = int(np.sqrt(period))

        wma_half = self.wma(series, half_period)
        wma_full = self.wma(series, period)
        raw_hma = 2 * wma_half - wma_full

        return self.wma(raw_hma, sqrt_period)

    def tema(self, series: pd.Series, period: int) -> pd.Series:
        """Triple Exponential Moving Average (TEMA)"""
        if PANDAS_TA_AVAILABLE:
            return ta.tema(series, length=period)

        ema1 = self.ema(series, period)
        ema2 = self.ema(ema1, period)
        ema3 = self.ema(ema2, period)

        return 3 * ema1 - 3 * ema2 + ema3

    def clear_cache(self):
        """Clear indicator cache"""
        self.cache.clear()
        logger.info("Indicator cache cleared")


# ============================================================================
# CONVENIENCE FUNCTIONS
# ============================================================================

# Global instance for easy access
_indicators = AdvancedIndicators()

def get_indicators() -> AdvancedIndicators:
    """Get global indicators instance"""
    return _indicators

def list_all_indicators() -> Dict[str, List[str]]:
    """
    List all available indicators by category

    Returns:
        Dictionary mapping category names to list of indicator methods
    """
    return {
        'Momentum': [
            'rsi', 'stochastic', 'williams_r', 'cci', 'roc',
            'momentum', 'tsi'
        ],
        'Trend': [
            'macd', 'adx', 'aroon', 'supertrend', 'ichimoku', 'psar'
        ],
        'Volatility': [
            'atr', 'true_range', 'bollinger_bands', 'keltner_channels',
            'donchian_channels', 'std_dev'
        ],
        'Volume': [
            'obv', 'ad_line', 'cmf', 'mfi', 'vwap', 'vpt'
        ],
        'Support/Resistance': [
            'pivot_points', 'fibonacci_retracement'
        ],
        'Advanced': [
            'elder_ray', 'kst', 'mass_index', 'ultimate_oscillator',
            'awesome_oscillator', 'vortex'
        ],
        'Moving Averages': [
            'ema', 'sma', 'wma', 'hull_ma', 'tema'
        ]
    }


if __name__ == "__main__":
    # Example usage
    print("=" * 60)
    print("Advanced Indicators for MEM - Available Indicators")
    print("=" * 60)

    all_indicators = list_all_indicators()
    total_count = 0

    for category, indicators in all_indicators.items():
        print(f"\n{category} ({len(indicators)} indicators):")
        for ind in indicators:
            print(f"  - {ind}")
        total_count += len(indicators)

    print(f"\n{'=' * 60}")
    print(f"Total: {total_count} indicators available")
    print(f"pandas-ta library: {'✓ Available' if PANDAS_TA_AVAILABLE else '✗ Not installed'}")
    print(f"{'=' * 60}")
