#!/usr/bin/env python3
"""
Liquidity Gap Detection System

Identifies and analyzes liquidity gaps in market data:
1. Price gaps (overnight gaps, flash crashes)
2. Low volume zones (thin order books)
3. Spread widening (bid-ask explosion)
4. Impact on trade execution

Helps avoid trading during high-risk, low-liquidity periods.
"""

import pandas as pd
import numpy as np
from typing import Dict, List, Tuple, Optional
from dataclasses import dataclass
from datetime import datetime, timedelta


@dataclass
class LiquidityGap:
    """Represents a detected liquidity gap"""
    start_time: datetime
    end_time: datetime
    gap_type: str  # 'price_gap', 'volume_gap', 'spread_gap'
    severity: str  # 'low', 'medium', 'high', 'critical'
    price_gap_pct: float
    volume_ratio: float  # vs average volume
    estimated_slippage_pct: float
    description: str


@dataclass
class LiquidityMetrics:
    """Liquidity metrics for a time period"""
    timestamp: datetime
    volume: float
    volume_ma: float
    volume_ratio: float  # current / MA
    spread_pct: float
    depth_score: float  # 0-100, higher = more liquid
    liquidity_level: str  # 'high', 'normal', 'low', 'critical'


class LiquidityGapDetector:
    """
    Detects and analyzes liquidity gaps in market data

    Liquidity gaps are periods where:
    - Price jumps significantly (gaps)
    - Volume drops below normal
    - Spreads widen dramatically
    - Order book becomes thin
    """

    def __init__(
        self,
        price_gap_threshold_pct: float = 0.5,  # 0.5% price gap
        volume_threshold_ratio: float = 0.3,   # 30% of average volume
        spread_threshold_pct: float = 0.1,     # 0.1% spread
        volume_ma_period: int = 50
    ):
        self.price_gap_threshold_pct = price_gap_threshold_pct
        self.volume_threshold_ratio = volume_threshold_ratio
        self.spread_threshold_pct = spread_threshold_pct
        self.volume_ma_period = volume_ma_period

        print(f"🔍 Liquidity Gap Detector Initialized")
        print(f"   Price Gap Threshold: {self.price_gap_threshold_pct}%")
        print(f"   Volume Threshold: {self.volume_threshold_ratio:.0%} of MA")
        print(f"   Spread Threshold: {self.spread_threshold_pct}%")

    def generate_sample_ohlcv_data(
        self,
        start_time: datetime,
        periods: int = 720,  # 30 days hourly
        base_price: float = 66000,
        base_volume: float = 1000
    ) -> pd.DataFrame:
        """
        Generate sample OHLCV data with realistic liquidity patterns

        Includes:
        - Normal trading periods
        - Low volume periods (nights, weekends)
        - Price gaps (news events)
        - Gradual trends
        """
        np.random.seed(42)

        timestamps = [start_time + timedelta(hours=i) for i in range(periods)]
        data = []

        current_price = base_price

        for i, ts in enumerate(timestamps):
            # Day of week and hour effects
            hour = ts.hour
            day_of_week = ts.weekday()

            # Volume patterns
            if day_of_week >= 5:  # Weekend
                volume_mult = 0.4
            elif hour < 6 or hour > 22:  # Night
                volume_mult = 0.5
            elif 13 <= hour <= 15:  # Peak trading hours (EST afternoon)
                volume_mult = 1.5
            else:
                volume_mult = 1.0

            volume = base_volume * volume_mult * np.random.uniform(0.7, 1.3)

            # Price movement (trending + noise)
            trend = np.sin(i / 100) * 0.001  # Slow trend
            volatility = 0.003 if volume_mult > 0.7 else 0.005  # Higher vol in low liquidity
            price_change = trend + np.random.normal(0, volatility)

            # Occasional gaps (5% chance)
            if np.random.random() < 0.05:
                gap_size = np.random.uniform(-0.02, 0.02)  # Up to 2% gap
                price_change += gap_size

            current_price *= (1 + price_change)

            # OHLC based on close
            high = current_price * np.random.uniform(1.001, 1.01)
            low = current_price * np.random.uniform(0.99, 0.999)
            open_price = data[-1]['close'] if i > 0 else current_price

            # Spread based on volume (lower volume = wider spread)
            base_spread = 0.0001  # 0.01%
            spread_mult = 1 / volume_mult
            spread_pct = base_spread * spread_mult * np.random.uniform(0.8, 1.2)

            data.append({
                'timestamp': ts,
                'open': open_price,
                'high': high,
                'low': low,
                'close': current_price,
                'volume': volume,
                'spread_pct': spread_pct
            })

        return pd.DataFrame(data)

    def detect_price_gaps(self, df: pd.DataFrame) -> List[LiquidityGap]:
        """
        Detect price gaps between candles
        Gap = close[i] != open[i+1] by significant amount
        """
        gaps = []

        for i in range(len(df) - 1):
            current_close = df.iloc[i]['close']
            next_open = df.iloc[i + 1]['open']

            gap_pct = abs((next_open - current_close) / current_close) * 100

            if gap_pct >= self.price_gap_threshold_pct:
                # Classify severity
                if gap_pct >= 2.0:
                    severity = 'critical'
                elif gap_pct >= 1.0:
                    severity = 'high'
                elif gap_pct >= 0.5:
                    severity = 'medium'
                else:
                    severity = 'low'

                estimated_slippage = gap_pct * 0.3  # Assume 30% of gap as slippage

                gap = LiquidityGap(
                    start_time=df.iloc[i]['timestamp'],
                    end_time=df.iloc[i + 1]['timestamp'],
                    gap_type='price_gap',
                    severity=severity,
                    price_gap_pct=gap_pct,
                    volume_ratio=df.iloc[i + 1]['volume'] / df['volume'].mean(),
                    estimated_slippage_pct=estimated_slippage,
                    description=f"{gap_pct:.2f}% price gap"
                )
                gaps.append(gap)

        return gaps

    def detect_volume_gaps(self, df: pd.DataFrame) -> List[LiquidityGap]:
        """
        Detect low volume periods (thin liquidity)
        Volume gap = volume < threshold * average volume
        """
        df = df.copy()
        df['volume_ma'] = df['volume'].rolling(window=self.volume_ma_period).mean()
        df['volume_ratio'] = df['volume'] / df['volume_ma']

        gaps = []

        for i, row in df.iterrows():
            if pd.isna(row['volume_ratio']):
                continue

            if row['volume_ratio'] < self.volume_threshold_ratio:
                # Classify severity
                if row['volume_ratio'] < 0.1:
                    severity = 'critical'
                elif row['volume_ratio'] < 0.2:
                    severity = 'high'
                elif row['volume_ratio'] < 0.3:
                    severity = 'medium'
                else:
                    severity = 'low'

                # Estimate slippage based on volume deficit
                slippage_mult = (1 / row['volume_ratio']) - 1
                estimated_slippage = min(slippage_mult * 0.1, 1.0)  # Cap at 1%

                gap = LiquidityGap(
                    start_time=row['timestamp'],
                    end_time=row['timestamp'] + timedelta(hours=1),
                    gap_type='volume_gap',
                    severity=severity,
                    price_gap_pct=0,
                    volume_ratio=row['volume_ratio'],
                    estimated_slippage_pct=estimated_slippage,
                    description=f"Volume {row['volume_ratio']:.1%} of average"
                )
                gaps.append(gap)

        return gaps

    def detect_spread_gaps(self, df: pd.DataFrame) -> List[LiquidityGap]:
        """
        Detect spread widening (bid-ask spread explosion)
        Spread gap = spread > threshold
        """
        gaps = []
        avg_spread = df['spread_pct'].mean()

        for i, row in df.iterrows():
            if row['spread_pct'] > self.spread_threshold_pct:
                spread_mult = row['spread_pct'] / avg_spread

                # Classify severity
                if spread_mult >= 5:
                    severity = 'critical'
                elif spread_mult >= 3:
                    severity = 'high'
                elif spread_mult >= 2:
                    severity = 'medium'
                else:
                    severity = 'low'

                gap = LiquidityGap(
                    start_time=row['timestamp'],
                    end_time=row['timestamp'] + timedelta(hours=1),
                    gap_type='spread_gap',
                    severity=severity,
                    price_gap_pct=0,
                    volume_ratio=1.0,
                    estimated_slippage_pct=row['spread_pct'],
                    description=f"Spread {row['spread_pct']:.3f}% ({spread_mult:.1f}x normal)"
                )
                gaps.append(gap)

        return gaps

    def calculate_liquidity_metrics(self, df: pd.DataFrame) -> pd.DataFrame:
        """
        Calculate comprehensive liquidity metrics for each time period
        """
        df = df.copy()

        # Volume metrics
        df['volume_ma'] = df['volume'].rolling(window=self.volume_ma_period).mean()
        df['volume_ratio'] = df['volume'] / df['volume_ma']

        # Depth score (0-100)
        # Higher volume + tighter spread = better liquidity
        df['depth_score'] = df['volume_ratio'] * 50  # 0-100 scale
        df['depth_score'] = np.clip(df['depth_score'], 0, 100)

        # Adjust for spread
        avg_spread = df['spread_pct'].mean()
        df['spread_ratio'] = df['spread_pct'] / avg_spread
        df['depth_score'] = df['depth_score'] / df['spread_ratio']
        df['depth_score'] = np.clip(df['depth_score'], 0, 100)

        # Liquidity level classification
        def classify_liquidity(score):
            if score >= 75:
                return 'high'
            elif score >= 50:
                return 'normal'
            elif score >= 25:
                return 'low'
            else:
                return 'critical'

        df['liquidity_level'] = df['depth_score'].apply(classify_liquidity)

        return df

    def analyze_trade_execution_risk(
        self,
        df: pd.DataFrame,
        trades_df: pd.DataFrame
    ) -> pd.DataFrame:
        """
        Analyze liquidity conditions at trade entry/exit times
        Add risk scores to trades
        """
        # Calculate liquidity metrics
        df_metrics = self.calculate_liquidity_metrics(df)

        trades_enhanced = trades_df.copy()
        trades_enhanced['entry_time'] = pd.to_datetime(trades_enhanced['entry_time'])
        trades_enhanced['exit_time'] = pd.to_datetime(trades_enhanced['exit_time'])

        entry_liquidity = []
        exit_liquidity = []
        entry_risk_score = []
        exit_risk_score = []

        for idx, trade in trades_enhanced.iterrows():
            # Find liquidity at entry
            entry_metrics = df_metrics[df_metrics['timestamp'] <= trade['entry_time']]
            if len(entry_metrics) > 0:
                entry_liq = entry_metrics.iloc[-1]['liquidity_level']
                entry_score = entry_metrics.iloc[-1]['depth_score']
            else:
                entry_liq = 'unknown'
                entry_score = 50

            # Find liquidity at exit
            exit_metrics = df_metrics[df_metrics['timestamp'] <= trade['exit_time']]
            if len(exit_metrics) > 0:
                exit_liq = exit_metrics.iloc[-1]['liquidity_level']
                exit_score = exit_metrics.iloc[-1]['depth_score']
            else:
                exit_liq = 'unknown'
                exit_score = 50

            entry_liquidity.append(entry_liq)
            exit_liquidity.append(exit_liq)
            entry_risk_score.append(100 - entry_score)  # Higher score = higher risk
            exit_risk_score.append(100 - exit_score)

        trades_enhanced['entry_liquidity'] = entry_liquidity
        trades_enhanced['exit_liquidity'] = exit_liquidity
        trades_enhanced['entry_risk_score'] = entry_risk_score
        trades_enhanced['exit_risk_score'] = exit_risk_score
        trades_enhanced['avg_execution_risk'] = (trades_enhanced['entry_risk_score'] +
                                                   trades_enhanced['exit_risk_score']) / 2

        return trades_enhanced


def main():
    """Run liquidity gap analysis"""
    print("="*120)
    print("🌊 LIQUIDITY GAP DETECTION SYSTEM")
    print("="*120)
    print()

    detector = LiquidityGapDetector(
        price_gap_threshold_pct=0.5,
        volume_threshold_ratio=0.3,
        spread_threshold_pct=0.1
    )

    # Generate sample market data
    print("\n📊 Generating sample market data (720 hours)...")
    start_time = datetime(2025, 9, 21)
    df = detector.generate_sample_ohlcv_data(
        start_time=start_time,
        periods=720,
        base_price=66000,
        base_volume=1000
    )

    print(f"   Generated {len(df)} candles")
    print(f"   Time range: {df['timestamp'].min()} to {df['timestamp'].max()}")
    print(f"   Price range: ${df['close'].min():.2f} to ${df['close'].max():.2f}")
    print(f"   Avg volume: {df['volume'].mean():.2f}")

    # Detect gaps
    print("\n🔍 Detecting liquidity gaps...")

    price_gaps = detector.detect_price_gaps(df)
    print(f"\n   Price Gaps Found: {len(price_gaps)}")

    volume_gaps = detector.detect_volume_gaps(df)
    print(f"   Volume Gaps Found: {len(volume_gaps)}")

    spread_gaps = detector.detect_spread_gaps(df)
    print(f"   Spread Gaps Found: {len(spread_gaps)}")

    # Analyze gaps by severity
    all_gaps = price_gaps + volume_gaps + spread_gaps

    print(f"\n📊 Gap Summary by Severity:")
    severity_counts = {}
    for gap in all_gaps:
        severity_counts[gap.severity] = severity_counts.get(gap.severity, 0) + 1

    for severity in ['low', 'medium', 'high', 'critical']:
        count = severity_counts.get(severity, 0)
        if count > 0:
            emoji = {'low': '🟢', 'medium': '🟡', 'high': '🟠', 'critical': '🔴'}[severity]
            print(f"   {emoji} {severity.upper():<10}: {count} gaps")

    # Show top 10 most severe gaps
    print(f"\n{'='*120}")
    print("🚨 TOP 10 MOST SEVERE LIQUIDITY GAPS")
    print(f"{'='*120}\n")

    severity_order = {'critical': 4, 'high': 3, 'medium': 2, 'low': 1}
    sorted_gaps = sorted(all_gaps, key=lambda x: (
        severity_order[x.severity],
        x.estimated_slippage_pct
    ), reverse=True)

    for i, gap in enumerate(sorted_gaps[:10], 1):
        emoji = {'low': '🟢', 'medium': '🟡', 'high': '🟠', 'critical': '🔴'}[gap.severity]
        print(f"{i:2}. {emoji} {gap.gap_type.upper():<12} | {gap.severity.upper():<8} | "
              f"{gap.start_time.strftime('%Y-%m-%d %H:%M')}")
        print(f"    {gap.description}")
        print(f"    Estimated Slippage: {gap.estimated_slippage_pct:.3f}%")
        if gap.price_gap_pct > 0:
            print(f"    Price Gap: {gap.price_gap_pct:.2f}%")
        if gap.volume_ratio > 0:
            print(f"    Volume: {gap.volume_ratio:.1%} of average")
        print()

    # Calculate liquidity metrics
    print(f"{'='*120}")
    print("📈 LIQUIDITY METRICS ANALYSIS")
    print(f"{'='*120}\n")

    df_metrics = detector.calculate_liquidity_metrics(df)

    print("Liquidity Level Distribution:")
    liquidity_dist = df_metrics['liquidity_level'].value_counts()
    total = len(df_metrics)
    for level in ['high', 'normal', 'low', 'critical']:
        count = liquidity_dist.get(level, 0)
        pct = count / total * 100
        emoji = {'high': '🟢', 'normal': '🟡', 'low': '🟠', 'critical': '🔴'}[level]
        print(f"   {emoji} {level.upper():<10}: {count:>3} periods ({pct:>5.1f}%)")

    # Analyze impact on trades
    print(f"\n{'='*120}")
    print("💰 TRADE EXECUTION RISK ANALYSIS")
    print(f"{'='*120}\n")

    # Load actual trades
    try:
        trades_df = pd.read_csv('trade_indicators.csv')
        trades_enhanced = detector.analyze_trade_execution_risk(df_metrics, trades_df)

        print(f"Analyzed {len(trades_enhanced)} trades\n")

        print("Execution Risk Distribution:")
        print(f"{'Risk Level':<15} {'Entry':<10} {'Exit':<10} {'Avg PnL':<12} {'Win Rate':<10}")
        print("-"*60)

        # Group by risk levels
        risk_levels = [
            ('Low Risk (0-25)', 0, 25),
            ('Medium Risk (25-50)', 25, 50),
            ('High Risk (50-75)', 50, 75),
            ('Critical Risk (75+)', 75, 100)
        ]

        for level_name, min_risk, max_risk in risk_levels:
            trades_in_level = trades_enhanced[
                (trades_enhanced['avg_execution_risk'] >= min_risk) &
                (trades_enhanced['avg_execution_risk'] < max_risk)
            ]

            if len(trades_in_level) > 0:
                entry_liquidity_mode = trades_in_level['entry_liquidity'].mode()
                exit_liquidity_mode = trades_in_level['exit_liquidity'].mode()
                avg_pnl = trades_in_level['pnl'].mean()
                win_rate = (trades_in_level['is_win'] == 1).mean()

                entry_liq = entry_liquidity_mode[0] if len(entry_liquidity_mode) > 0 else 'N/A'
                exit_liq = exit_liquidity_mode[0] if len(exit_liquidity_mode) > 0 else 'N/A'

                print(f"{level_name:<15} {entry_liq:<10} {exit_liq:<10} "
                      f"${avg_pnl:>9.2f}  {win_rate:>9.1%}")

        # Check if low liquidity trades performed worse
        print(f"\n📊 Liquidity Impact on Performance:\n")

        high_liq_trades = trades_enhanced[
            (trades_enhanced['entry_liquidity'] == 'high') |
            (trades_enhanced['exit_liquidity'] == 'high')
        ]

        low_liq_trades = trades_enhanced[
            (trades_enhanced['entry_liquidity'].isin(['low', 'critical'])) |
            (trades_enhanced['exit_liquidity'].isin(['low', 'critical']))
        ]

        if len(high_liq_trades) > 0:
            print(f"   High Liquidity Trades: {len(high_liq_trades)}")
            print(f"      Avg PnL: ${high_liq_trades['pnl'].mean():.2f}")
            print(f"      Win Rate: {(high_liq_trades['is_win'] == 1).mean():.1%}")

        if len(low_liq_trades) > 0:
            print(f"\n   Low Liquidity Trades: {len(low_liq_trades)}")
            print(f"      Avg PnL: ${low_liq_trades['pnl'].mean():.2f}")
            print(f"      Win Rate: {(low_liq_trades['is_win'] == 1).mean():.1%}")

        # Save enhanced trades
        trades_enhanced.to_csv('trades_with_liquidity_risk.csv', index=False)
        print(f"\n💾 Enhanced trade data saved to trades_with_liquidity_risk.csv")

    except FileNotFoundError:
        print("⚠️  trade_indicators.csv not found, skipping trade analysis")

    # Save liquidity metrics
    df_metrics.to_csv('liquidity_metrics.csv', index=False)
    print(f"💾 Liquidity metrics saved to liquidity_metrics.csv")

    print(f"\n{'='*120}")
    print("✅ LIQUIDITY GAP ANALYSIS COMPLETE")
    print(f"{'='*120}")

    print("\n📌 Key Findings:")
    print(f"   • Found {len(all_gaps)} total liquidity gaps")
    print(f"   • {severity_counts.get('critical', 0) + severity_counts.get('high', 0)} high/critical severity gaps")
    print(f"   • {liquidity_dist.get('low', 0) + liquidity_dist.get('critical', 0)} periods with low/critical liquidity")
    print(f"   • Recommend avoiding trades during identified gap periods\n")


if __name__ == '__main__':
    main()
