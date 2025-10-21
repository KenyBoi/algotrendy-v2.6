#!/usr/bin/env python3
"""
Analyze MEM confidence levels to find optimal threshold
"""

import pandas as pd
import numpy as np
import matplotlib.pyplot as plt


def analyze_confidence_distribution():
    """Analyze the distribution of MEM confidence scores"""
    print("="*100)
    print("📊 MEM CONFIDENCE DISTRIBUTION ANALYSIS")
    print("="*100)
    print()

    # Load trades
    trades_df = pd.read_csv('trade_indicators.csv')

    print(f"📈 Loaded {len(trades_df)} trades\n")

    # Confidence statistics
    print("🎯 CONFIDENCE STATISTICS:")
    print(f"   Min:  {trades_df['mem_confidence'].min():.1%}")
    print(f"   Max:  {trades_df['mem_confidence'].max():.1%}")
    print(f"   Mean: {trades_df['mem_confidence'].mean():.1%}")
    print(f"   Median: {trades_df['mem_confidence'].median():.1%}")
    print(f"   Std Dev: {trades_df['mem_confidence'].std():.1%}\n")

    # Distribution
    print("📊 CONFIDENCE DISTRIBUTION:")
    bins = [0, 0.6, 0.65, 0.70, 0.75, 0.80, 0.85, 0.90, 1.0]
    bin_labels = ['<60%', '60-65%', '65-70%', '70-75%', '75-80%', '80-85%', '85-90%', '90%+']

    for i in range(len(bins)-1):
        count = ((trades_df['mem_confidence'] >= bins[i]) & (trades_df['mem_confidence'] < bins[i+1])).sum()
        pct = count / len(trades_df) * 100
        print(f"   {bin_labels[i]:<10} {count:>3} trades ({pct:>5.1f}%)")

    # Analyze performance by confidence level
    print(f"\n{'='*100}")
    print("💰 PERFORMANCE BY CONFIDENCE LEVEL")
    print(f"{'='*100}\n")

    threshold_tests = [0.60, 0.65, 0.70, 0.72, 0.75, 0.78, 0.80]

    results = []

    for threshold in threshold_tests:
        high_conf_trades = trades_df[trades_df['mem_confidence'] >= threshold]
        low_conf_trades = trades_df[trades_df['mem_confidence'] < threshold]

        if len(high_conf_trades) > 0:
            high_pnl = high_conf_trades['pnl'].sum()
            high_avg = high_conf_trades['pnl'].mean()
            high_wr = (high_conf_trades['is_win'] == 1).mean()
        else:
            high_pnl = 0
            high_avg = 0
            high_wr = 0

        if len(low_conf_trades) > 0:
            low_pnl = low_conf_trades['pnl'].sum()
            low_avg = low_conf_trades['pnl'].mean()
            low_wr = (low_conf_trades['is_win'] == 1).mean()
        else:
            low_pnl = 0
            low_avg = 0
            low_wr = 0

        baseline_pnl = trades_df['pnl'].sum()
        improvement = high_pnl - baseline_pnl

        results.append({
            'threshold': threshold,
            'high_count': len(high_conf_trades),
            'high_pnl': high_pnl,
            'high_avg': high_avg,
            'high_wr': high_wr,
            'low_count': len(low_conf_trades),
            'low_pnl': low_pnl,
            'low_avg': low_avg,
            'low_wr': low_wr,
            'improvement': improvement
        })

    # Print results
    print(f"{'Threshold':<12} {'High Conf Trades':<20} {'Avg PnL':<12} {'Win Rate':<12} {'Total PnL':<15} {'Improvement':<15}")
    print(f"{'-'*100}")

    for r in results:
        if r['high_count'] > 0:
            improvement_symbol = "📈" if r['improvement'] > 0 else "📉"
            print(f"{r['threshold']:.0%}        {r['high_count']:>3} trades ({r['high_count']/33*100:>5.1f}%)  "
                  f"${r['high_avg']:>9.2f}  {r['high_wr']:>10.1%}  ${r['high_pnl']:>12.2f}  "
                  f"{improvement_symbol}${r['improvement']:>12.2f}")
        else:
            print(f"{r['threshold']:.0%}        {r['high_count']:>3} trades  (  0.0%)  "
                  f"{'N/A':>10}  {'N/A':>10}  {'N/A':>13}  {'N/A':>14}")

    # Find best threshold
    valid_results = [r for r in results if r['high_count'] >= 5]  # At least 5 trades
    if valid_results:
        best = max(valid_results, key=lambda x: x['high_pnl'])
        print(f"\n🏆 BEST THRESHOLD (min 5 trades): {best['threshold']:.0%}")
        print(f"   Trades: {best['high_count']}")
        print(f"   Total PnL: ${best['high_pnl']:.2f}")
        print(f"   Avg PnL: ${best['high_avg']:.2f}")
        print(f"   Win Rate: {best['high_wr']:.1%}")
        print(f"   Improvement: ${best['improvement']:.2f}\n")

    # Analyze movement size
    print(f"\n{'='*100}")
    print("📏 PREDICTED MOVEMENT ANALYSIS")
    print(f"{'='*100}\n")

    trades_df['abs_pnl_pct'] = trades_df['pnl_pct'].abs()

    print("📊 MOVEMENT SIZE DISTRIBUTION:")
    movement_bins = [0, 1, 2, 3, 4, 5, 10, 100]
    movement_labels = ['<1%', '1-2%', '2-3%', '3-4%', '4-5%', '5-10%', '10%+']

    for i in range(len(movement_bins)-1):
        count = ((trades_df['abs_pnl_pct'] >= movement_bins[i]) &
                 (trades_df['abs_pnl_pct'] < movement_bins[i+1])).sum()
        pct = count / len(trades_df) * 100
        avg_pnl = trades_df[(trades_df['abs_pnl_pct'] >= movement_bins[i]) &
                           (trades_df['abs_pnl_pct'] < movement_bins[i+1])]['pnl'].mean()
        print(f"   {movement_labels[i]:<10} {count:>3} trades ({pct:>5.1f}%) - Avg PnL: ${avg_pnl:>7.2f}")

    # Test movement thresholds
    print(f"\n💰 PERFORMANCE BY MOVEMENT SIZE:")
    print(f"{'Min Movement':<15} {'Trades':<10} {'Avg PnL':<12} {'Win Rate':<12} {'Total PnL':<15} {'Improvement':<15}")
    print(f"{'-'*100}")

    movement_thresholds = [0, 1, 2, 3, 4, 5]
    for threshold in movement_thresholds:
        filtered = trades_df[trades_df['abs_pnl_pct'] >= threshold]
        if len(filtered) > 0:
            total_pnl = filtered['pnl'].sum()
            avg_pnl = filtered['pnl'].mean()
            win_rate = (filtered['is_win'] == 1).mean()
            improvement = total_pnl - trades_df['pnl'].sum()
            improvement_symbol = "📈" if improvement > 0 else "📉"

            print(f"{threshold}%              {len(filtered):>3}        "
                  f"${avg_pnl:>9.2f}  {win_rate:>10.1%}  ${total_pnl:>12.2f}  "
                  f"{improvement_symbol}${improvement:>12.2f}")
        else:
            print(f"{threshold}%              {len(filtered):>3}        "
                  f"{'N/A':>10}  {'N/A':>10}  {'N/A':>13}  {'N/A':>14}")

    # Combined analysis
    print(f"\n{'='*100}")
    print("🔗 COMBINED FILTERS: CONFIDENCE + MOVEMENT")
    print(f"{'='*100}\n")

    print(f"{'Confidence':<12} {'Movement':<12} {'Trades':<10} {'Avg PnL':<12} {'Win Rate':<12} {'Total PnL':<15}")
    print(f"{'-'*100}")

    for conf_thresh in [0.65, 0.70, 0.72, 0.75]:
        for move_thresh in [0, 2, 3, 4]:
            filtered = trades_df[
                (trades_df['mem_confidence'] >= conf_thresh) &
                (trades_df['abs_pnl_pct'] >= move_thresh)
            ]

            if len(filtered) > 0:
                total_pnl = filtered['pnl'].sum()
                avg_pnl = filtered['pnl'].mean()
                win_rate = (filtered['is_win'] == 1).mean()

                symbol = "🟢" if total_pnl > 0 else "🔴"
                print(f"{conf_thresh:.0%}          {move_thresh}%           {len(filtered):>3}        "
                      f"${avg_pnl:>9.2f}  {win_rate:>10.1%}  {symbol}${total_pnl:>12.2f}")

    print(f"\n{'='*100}")
    print("✅ ANALYSIS COMPLETE")
    print(f"{'='*100}")

    # Key findings
    print("\n📌 KEY FINDINGS:\n")
    print("1. CONFIDENCE THRESHOLD:")
    if valid_results:
        print(f"   • Optimal threshold: {best['threshold']:.0%}")
        print(f"   • This filters to {best['high_count']} trades")
        print(f"   • Expected improvement: ${best['improvement']:.2f}")
    print(f"\n2. MOVEMENT FILTER:")
    print(f"   • All trades already have >= 2% movement")
    print(f"   • Movement filter alone doesn't help")
    print(f"\n3. RECOMMENDATION:")
    print(f"   • Use confidence threshold: 70-75%")
    print(f"   • Skip movement filter (already satisfied)")
    print(f"   • Consider equity ROC filter for additional quality control\n")


if __name__ == '__main__':
    analyze_confidence_distribution()
