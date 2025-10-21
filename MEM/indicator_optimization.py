#!/usr/bin/env python3
"""
Indicator Optimization Framework
Tests all available indicators on equity curve to find which predict profitable periods best
"""

import pandas as pd
import numpy as np
from typing import Dict, List, Tuple
from dataclasses import dataclass
import itertools


@dataclass
class IndicatorResult:
    """Results from testing an indicator"""
    name: str
    trades_when_bullish: int
    trades_when_bearish: int
    avg_pnl_bullish: float
    avg_pnl_bearish: float
    win_rate_bullish: float
    win_rate_bearish: float
    total_pnl_bullish: float
    total_pnl_bearish: float
    improvement_vs_baseline: float
    score: float  # Overall quality score


class IndicatorOptimizer:
    """
    Comprehensive indicator optimization framework
    Tests all indicators to find best predictors of equity performance
    """

    def __init__(self, trades_csv: str = 'trade_indicators.csv'):
        self.trades_df = pd.read_csv(trades_csv)
        self.trades_df['exit_time'] = pd.to_datetime(self.trades_df['exit_time'])
        self.trades_df['entry_time'] = pd.to_datetime(self.trades_df['entry_time'])

        # Create equity curve from cumulative PnL
        self.equity_df = pd.DataFrame({
            'timestamp': self.trades_df['exit_time'],
            'equity': 10000 + self.trades_df['cumulative_pnl']
        }).set_index('timestamp')

        # Baseline metrics (no filtering)
        self.baseline_trades = len(self.trades_df)
        self.baseline_pnl = self.trades_df['pnl'].sum()
        self.baseline_win_rate = (self.trades_df['is_win'] == 1).mean()

        print(f"📊 Loaded {len(self.trades_df)} trades")
        print(f"📈 Baseline: {self.baseline_trades} trades, ${self.baseline_pnl:.2f} total PnL, {self.baseline_win_rate:.1%} win rate\n")

    def calculate_rsi(self, series: pd.Series, period: int) -> pd.Series:
        """Calculate RSI indicator"""
        delta = series.diff()
        gain = (delta.where(delta > 0, 0)).rolling(window=period).mean()
        loss = (-delta.where(delta < 0, 0)).rolling(window=period).mean()
        rs = gain / loss
        rsi = 100 - (100 / (1 + rs))
        return rsi

    def calculate_macd(self, series: pd.Series, fast: int, slow: int, signal: int) -> Tuple[pd.Series, pd.Series, pd.Series]:
        """Calculate MACD indicator"""
        ema_fast = series.ewm(span=fast, adjust=False).mean()
        ema_slow = series.ewm(span=slow, adjust=False).mean()
        macd = ema_fast - ema_slow
        macd_signal = macd.ewm(span=signal, adjust=False).mean()
        macd_hist = macd - macd_signal
        return macd, macd_signal, macd_hist

    def calculate_bollinger_bands(self, series: pd.Series, period: int, std_dev: float) -> Tuple[pd.Series, pd.Series, pd.Series]:
        """Calculate Bollinger Bands"""
        sma = series.rolling(window=period).mean()
        std = series.rolling(window=period).std()
        upper = sma + (std * std_dev)
        lower = sma - (std * std_dev)
        return upper, sma, lower

    def calculate_stochastic(self, series: pd.Series, period: int) -> pd.Series:
        """Calculate Stochastic Oscillator"""
        low_min = series.rolling(window=period).min()
        high_max = series.rolling(window=period).max()
        stoch = 100 * (series - low_min) / (high_max - low_min)
        return stoch

    def calculate_roc(self, series: pd.Series, period: int) -> pd.Series:
        """Calculate Rate of Change"""
        roc = ((series - series.shift(period)) / series.shift(period)) * 100
        return roc

    def calculate_williams_r(self, series: pd.Series, period: int) -> pd.Series:
        """Calculate Williams %R"""
        high_max = series.rolling(window=period).max()
        low_min = series.rolling(window=period).min()
        wr = -100 * (high_max - series) / (high_max - low_min)
        return wr

    def calculate_cci(self, series: pd.Series, period: int) -> pd.Series:
        """Calculate Commodity Channel Index"""
        tp = series  # Using equity as typical price
        sma = tp.rolling(window=period).mean()
        mad = tp.rolling(window=period).apply(lambda x: np.abs(x - x.mean()).mean())
        cci = (tp - sma) / (0.015 * mad)
        return cci

    def test_indicator(self, signals: pd.Series, indicator_name: str) -> IndicatorResult:
        """
        Test an indicator's performance
        signals: Boolean series where True = bullish, False = bearish
        """
        # Match signals to trades
        trades_with_signals = self.trades_df.copy()

        # For each trade, find the signal at entry time
        trade_signals = []
        for idx, trade in trades_with_signals.iterrows():
            # Get signal at or before entry time
            signal_at_entry = signals[signals.index <= trade['entry_time']]
            if len(signal_at_entry) > 0:
                trade_signals.append(signal_at_entry.iloc[-1])
            else:
                trade_signals.append(np.nan)

        trades_with_signals['signal'] = trade_signals
        trades_with_signals = trades_with_signals.dropna(subset=['signal'])

        # Calculate metrics
        bullish_trades = trades_with_signals[trades_with_signals['signal'] == True]
        bearish_trades = trades_with_signals[trades_with_signals['signal'] == False]

        if len(bullish_trades) == 0 or len(bearish_trades) == 0:
            return IndicatorResult(
                name=indicator_name,
                trades_when_bullish=len(bullish_trades),
                trades_when_bearish=len(bearish_trades),
                avg_pnl_bullish=0,
                avg_pnl_bearish=0,
                win_rate_bullish=0,
                win_rate_bearish=0,
                total_pnl_bullish=0,
                total_pnl_bearish=0,
                improvement_vs_baseline=0,
                score=0
            )

        # Bullish metrics
        avg_pnl_bull = bullish_trades['pnl'].mean()
        win_rate_bull = (bullish_trades['is_win'] == 1).mean()
        total_pnl_bull = bullish_trades['pnl'].sum()

        # Bearish metrics
        avg_pnl_bear = bearish_trades['pnl'].mean()
        win_rate_bear = (bearish_trades['is_win'] == 1).mean()
        total_pnl_bear = bearish_trades['pnl'].sum()

        # Improvement if we only traded during bullish signals
        improvement = total_pnl_bull - self.baseline_pnl

        # Quality score: combination of improvement and trade count
        # Higher score = better indicator
        score = improvement + (len(bullish_trades) / self.baseline_trades * 100)

        return IndicatorResult(
            name=indicator_name,
            trades_when_bullish=len(bullish_trades),
            trades_when_bearish=len(bearish_trades),
            avg_pnl_bullish=avg_pnl_bull,
            avg_pnl_bearish=avg_pnl_bear,
            win_rate_bullish=win_rate_bull,
            win_rate_bearish=win_rate_bear,
            total_pnl_bullish=total_pnl_bull,
            total_pnl_bearish=total_pnl_bear,
            improvement_vs_baseline=improvement,
            score=score
        )

    def test_all_indicators(self) -> List[IndicatorResult]:
        """Test all indicators with various parameters"""
        results = []
        equity = self.equity_df['equity']

        print("🔍 Testing indicators...\n")

        # 1. RSI with different periods
        print("Testing RSI...")
        for period in [7, 14, 21, 28]:
            rsi = self.calculate_rsi(equity, period)

            # Test different thresholds
            for threshold in [40, 50, 60]:
                signals = rsi > threshold
                result = self.test_indicator(signals, f"RSI({period}) > {threshold}")
                results.append(result)

        # 2. MACD with different configurations
        print("Testing MACD...")
        for fast, slow, signal in [(12, 26, 9), (5, 13, 5), (8, 21, 5)]:
            macd, macd_signal, macd_hist = self.calculate_macd(equity, fast, slow, signal)

            # Bullish: MACD > Signal
            signals = macd > macd_signal
            result = self.test_indicator(signals, f"MACD({fast},{slow},{signal}) > Signal")
            results.append(result)

            # Bullish: Histogram > 0
            signals = macd_hist > 0
            result = self.test_indicator(signals, f"MACD({fast},{slow},{signal}) Hist > 0")
            results.append(result)

        # 3. SMA Crossovers
        print("Testing SMA Crossovers...")
        for fast, slow in [(5, 10), (10, 20), (20, 50), (10, 30)]:
            sma_fast = equity.rolling(window=fast).mean()
            sma_slow = equity.rolling(window=slow).mean()
            signals = sma_fast > sma_slow
            result = self.test_indicator(signals, f"SMA({fast}) > SMA({slow})")
            results.append(result)

        # 4. EMA Crossovers
        print("Testing EMA Crossovers...")
        for fast, slow in [(5, 10), (10, 20), (12, 26)]:
            ema_fast = equity.ewm(span=fast, adjust=False).mean()
            ema_slow = equity.ewm(span=slow, adjust=False).mean()
            signals = ema_fast > ema_slow
            result = self.test_indicator(signals, f"EMA({fast}) > EMA({slow})")
            results.append(result)

        # 5. Bollinger Bands
        print("Testing Bollinger Bands...")
        for period in [10, 20]:
            for std_dev in [1.5, 2.0, 2.5]:
                upper, middle, lower = self.calculate_bollinger_bands(equity, period, std_dev)

                # Above middle band
                signals = equity > middle
                result = self.test_indicator(signals, f"BB({period},{std_dev}) Above Middle")
                results.append(result)

                # In upper half
                signals = equity > (middle + (upper - middle) * 0.5)
                result = self.test_indicator(signals, f"BB({period},{std_dev}) Upper Half")
                results.append(result)

        # 6. Stochastic
        print("Testing Stochastic...")
        for period in [14, 21]:
            stoch = self.calculate_stochastic(equity, period)
            for threshold in [40, 50, 60]:
                signals = stoch > threshold
                result = self.test_indicator(signals, f"Stoch({period}) > {threshold}")
                results.append(result)

        # 7. Rate of Change
        print("Testing ROC...")
        for period in [5, 10, 20]:
            roc = self.calculate_roc(equity, period)
            signals = roc > 0
            result = self.test_indicator(signals, f"ROC({period}) > 0")
            results.append(result)

        # 8. Williams %R
        print("Testing Williams %R...")
        for period in [14, 21]:
            wr = self.calculate_williams_r(equity, period)
            for threshold in [-60, -50, -40]:
                signals = wr > threshold
                result = self.test_indicator(signals, f"Williams%R({period}) > {threshold}")
                results.append(result)

        # 9. CCI
        print("Testing CCI...")
        for period in [14, 20]:
            cci = self.calculate_cci(equity, period)
            for threshold in [-100, 0, 100]:
                signals = cci > threshold
                result = self.test_indicator(signals, f"CCI({period}) > {threshold}")
                results.append(result)

        # 10. Simple equity momentum
        print("Testing Momentum...")
        for period in [3, 5, 10]:
            momentum = equity.pct_change(period)
            signals = momentum > 0
            result = self.test_indicator(signals, f"Momentum({period}) > 0")
            results.append(result)

        print(f"\n✅ Tested {len(results)} indicator configurations")
        return results

    def test_indicator_combinations(self, top_indicators: List[str]) -> List[IndicatorResult]:
        """Test combinations of top indicators"""
        results = []
        equity = self.equity_df['equity']

        print(f"\n🔗 Testing combinations of top {len(top_indicators)} indicators...")

        # Parse indicator names and recreate signals
        indicator_signals = {}

        for ind_name in top_indicators:
            if 'RSI' in ind_name:
                # Extract period and threshold
                parts = ind_name.replace('RSI(', '').replace(')', '').split('>')
                period = int(parts[0].strip())
                threshold = int(parts[1].strip())
                rsi = self.calculate_rsi(equity, period)
                indicator_signals[ind_name] = rsi > threshold

            elif 'MACD' in ind_name and 'Hist' in ind_name:
                parts = ind_name.replace('MACD(', '').replace(')', '').split(',')
                fast = int(parts[0])
                slow = int(parts[1])
                signal = int(parts[2].split(')')[0])
                macd, macd_signal, macd_hist = self.calculate_macd(equity, fast, slow, signal)
                indicator_signals[ind_name] = macd_hist > 0

            elif 'MACD' in ind_name:
                parts = ind_name.replace('MACD(', '').replace(')', '').split(',')
                fast = int(parts[0])
                slow = int(parts[1])
                signal = int(parts[2].split(')')[0])
                macd, macd_signal, macd_hist = self.calculate_macd(equity, fast, slow, signal)
                indicator_signals[ind_name] = macd > macd_signal

            elif 'SMA' in ind_name:
                parts = ind_name.replace('SMA(', '').replace(')', '').split('>')
                fast = int(parts[0].strip())
                slow = int(parts[1].strip().replace('SMA(', '').replace(')', ''))
                sma_fast = equity.rolling(window=fast).mean()
                sma_slow = equity.rolling(window=slow).mean()
                indicator_signals[ind_name] = sma_fast > sma_slow

        # Test 2-way combinations (AND logic)
        for ind1, ind2 in itertools.combinations(top_indicators[:5], 2):
            if ind1 in indicator_signals and ind2 in indicator_signals:
                combined_signals = indicator_signals[ind1] & indicator_signals[ind2]
                result = self.test_indicator(combined_signals, f"{ind1} AND {ind2}")
                results.append(result)

        return results

    def print_results(self, results: List[IndicatorResult], top_n: int = 20):
        """Print formatted results"""
        # Sort by score
        sorted_results = sorted(results, key=lambda x: x.score, reverse=True)

        print(f"\n{'='*120}")
        print(f"🏆 TOP {top_n} INDICATORS (Ranked by Score)")
        print(f"{'='*120}")
        print(f"{'Indicator':<45} {'Trades':<8} {'Avg PnL':<12} {'Win Rate':<10} {'Total PnL':<12} {'Improvement':<12} {'Score':<8}")
        print(f"{'-'*120}")

        for i, result in enumerate(sorted_results[:top_n], 1):
            improvement_symbol = "📈" if result.improvement_vs_baseline > 0 else "📉"
            print(f"{i:2}. {result.name:<42} {result.trades_when_bullish:<8} "
                  f"${result.avg_pnl_bullish:>9.2f}  {result.win_rate_bullish:>8.1%}  "
                  f"${result.total_pnl_bullish:>10.2f}  {improvement_symbol}${result.improvement_vs_baseline:>9.2f}  {result.score:>7.1f}")

        print(f"\n{'='*120}")
        print(f"📊 DETAILED COMPARISON - TOP 5")
        print(f"{'='*120}\n")

        for i, result in enumerate(sorted_results[:5], 1):
            print(f"{i}. {result.name}")
            print(f"   When BULLISH: {result.trades_when_bullish} trades, ${result.avg_pnl_bullish:.2f} avg, "
                  f"{result.win_rate_bullish:.1%} win rate, ${result.total_pnl_bullish:.2f} total")
            print(f"   When BEARISH: {result.trades_when_bearish} trades, ${result.avg_pnl_bearish:.2f} avg, "
                  f"{result.win_rate_bearish:.1%} win rate, ${result.total_pnl_bearish:.2f} total")
            print(f"   Improvement: ${result.improvement_vs_baseline:.2f} ({result.improvement_vs_baseline/abs(self.baseline_pnl)*100:+.1f}%)")
            print()

        # Best improvements
        best_improvement = max(sorted_results, key=lambda x: x.improvement_vs_baseline)
        print(f"🎯 BEST IMPROVEMENT: {best_improvement.name}")
        print(f"   Would improve PnL from ${self.baseline_pnl:.2f} to ${best_improvement.total_pnl_bullish:.2f}")
        print(f"   Gain: ${best_improvement.improvement_vs_baseline:.2f}\n")

        # Best win rate
        best_wr = max(sorted_results, key=lambda x: x.win_rate_bullish if x.trades_when_bullish > 5 else 0)
        print(f"🎯 BEST WIN RATE (min 5 trades): {best_wr.name}")
        print(f"   Win Rate: {best_wr.win_rate_bullish:.1%} ({best_wr.trades_when_bullish} trades)")
        print(f"   Avg PnL: ${best_wr.avg_pnl_bullish:.2f}\n")

        return sorted_results


def main():
    """Run comprehensive indicator optimization"""
    print("="*120)
    print("🚀 INDICATOR OPTIMIZATION FRAMEWORK")
    print("="*120)
    print("Testing all indicators to find best predictors of profitable trading periods\n")

    optimizer = IndicatorOptimizer('trade_indicators.csv')

    # Test all individual indicators
    results = optimizer.test_all_indicators()

    # Print results
    sorted_results = optimizer.print_results(results, top_n=20)

    # Test combinations of top 5
    top_5_names = [r.name for r in sorted_results[:5]]
    combo_results = optimizer.test_indicator_combinations(top_5_names)

    if combo_results:
        print(f"\n{'='*120}")
        print(f"🔗 TOP INDICATOR COMBINATIONS")
        print(f"{'='*120}\n")

        combo_results_sorted = sorted(combo_results, key=lambda x: x.score, reverse=True)
        for i, result in enumerate(combo_results_sorted[:10], 1):
            print(f"{i}. {result.name}")
            print(f"   Trades: {result.trades_when_bullish}, Avg PnL: ${result.avg_pnl_bullish:.2f}, "
                  f"Win Rate: {result.win_rate_bullish:.1%}, Improvement: ${result.improvement_vs_baseline:.2f}\n")

    # Save results
    results_df = pd.DataFrame([
        {
            'indicator': r.name,
            'trades_bullish': r.trades_when_bullish,
            'trades_bearish': r.trades_when_bearish,
            'avg_pnl_bullish': r.avg_pnl_bullish,
            'avg_pnl_bearish': r.avg_pnl_bearish,
            'win_rate_bullish': r.win_rate_bullish,
            'win_rate_bearish': r.win_rate_bearish,
            'total_pnl_bullish': r.total_pnl_bullish,
            'total_pnl_bearish': r.total_pnl_bearish,
            'improvement': r.improvement_vs_baseline,
            'score': r.score
        }
        for r in sorted_results
    ])

    results_df.to_csv('indicator_optimization_results.csv', index=False)
    print(f"\n💾 Results saved to indicator_optimization_results.csv")

    print("\n" + "="*120)
    print("✅ OPTIMIZATION COMPLETE")
    print("="*120)


if __name__ == '__main__':
    main()
