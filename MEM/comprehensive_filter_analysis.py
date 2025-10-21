#!/usr/bin/env python3
"""
Comprehensive Filter Analysis

Combines ALL discovered optimizations:
1. Confidence threshold (72%)
2. Movement size filter (4-5%)
3. ROC(20) equity momentum
4. Liquidity risk filtering

Tests all combinations to find optimal strategy.
"""

import pandas as pd
import numpy as np
from typing import Dict, List


def test_all_filter_combinations():
    """
    Test all possible combinations of filters to find optimal setup
    """
    print("="*120)
    print("🔬 COMPREHENSIVE FILTER ANALYSIS")
    print("="*120)
    print("\nTesting ALL combinations of discovered filters:\n")

    # Load trades with liquidity risk
    trades_df = pd.read_csv('trades_with_liquidity_risk.csv')
    trades_df['entry_time'] = pd.to_datetime(trades_df['entry_time'])
    trades_df['exit_time'] = pd.to_datetime(trades_df['exit_time'])
    trades_df['abs_pnl_pct'] = trades_df['pnl_pct'].abs()

    baseline_pnl = trades_df['pnl'].sum()
    baseline_trades = len(trades_df)
    baseline_wr = (trades_df['is_win'] == 1).mean()

    print(f"📊 Baseline Performance:")
    print(f"   Trades: {baseline_trades}")
    print(f"   Total PnL: ${baseline_pnl:.2f}")
    print(f"   Win Rate: {baseline_wr:.1%}\n")

    # Calculate ROC(20) for each trade
    # Use cumulative PnL as equity
    equity = 10000 + trades_df['cumulative_pnl']
    roc_20 = []

    for i in range(len(trades_df)):
        if i < 20:
            roc_20.append(0)  # Not enough history
        else:
            current = equity.iloc[i]
            previous = equity.iloc[i - 20]
            roc = ((current - previous) / previous) * 100 if previous != 0 else 0
            roc_20.append(roc)

    trades_df['roc_20'] = roc_20

    # Define filter configurations
    filter_configs = [
        # Baseline
        {
            'name': 'Baseline (No Filters)',
            'confidence_min': 0.0,
            'movement_min': 0.0,
            'use_roc': False,
            'max_liquidity_risk': 100
        },

        # Single filters
        {
            'name': 'Confidence >= 72%',
            'confidence_min': 0.72,
            'movement_min': 0.0,
            'use_roc': False,
            'max_liquidity_risk': 100
        },
        {
            'name': 'Movement >= 4%',
            'confidence_min': 0.0,
            'movement_min': 4.0,
            'use_roc': False,
            'max_liquidity_risk': 100
        },
        {
            'name': 'Movement >= 5%',
            'confidence_min': 0.0,
            'movement_min': 5.0,
            'use_roc': False,
            'max_liquidity_risk': 100
        },
        {
            'name': 'ROC(20) > 0',
            'confidence_min': 0.0,
            'movement_min': 0.0,
            'use_roc': True,
            'max_liquidity_risk': 100
        },
        {
            'name': 'Liquidity Risk < 50',
            'confidence_min': 0.0,
            'movement_min': 0.0,
            'use_roc': False,
            'max_liquidity_risk': 50
        },

        # Double combinations
        {
            'name': 'Conf 72% + Move 4%',
            'confidence_min': 0.72,
            'movement_min': 4.0,
            'use_roc': False,
            'max_liquidity_risk': 100
        },
        {
            'name': 'Conf 72% + Move 5%',
            'confidence_min': 0.72,
            'movement_min': 5.0,
            'use_roc': False,
            'max_liquidity_risk': 100
        },
        {
            'name': 'Conf 72% + ROC > 0',
            'confidence_min': 0.72,
            'movement_min': 0.0,
            'use_roc': True,
            'max_liquidity_risk': 100
        },
        {
            'name': 'Move 4% + ROC > 0',
            'confidence_min': 0.0,
            'movement_min': 4.0,
            'use_roc': True,
            'max_liquidity_risk': 100
        },
        {
            'name': 'Move 5% + ROC > 0',
            'confidence_min': 0.0,
            'movement_min': 5.0,
            'use_roc': True,
            'max_liquidity_risk': 100
        },
        {
            'name': 'Conf 72% + Liq < 50',
            'confidence_min': 0.72,
            'movement_min': 0.0,
            'use_roc': False,
            'max_liquidity_risk': 50
        },

        # Triple combinations
        {
            'name': 'Conf 72% + Move 4% + ROC',
            'confidence_min': 0.72,
            'movement_min': 4.0,
            'use_roc': True,
            'max_liquidity_risk': 100
        },
        {
            'name': 'Conf 72% + Move 5% + ROC',
            'confidence_min': 0.72,
            'movement_min': 5.0,
            'use_roc': True,
            'max_liquidity_risk': 100
        },
        {
            'name': 'Conf 72% + Move 4% + Liq',
            'confidence_min': 0.72,
            'movement_min': 4.0,
            'use_roc': False,
            'max_liquidity_risk': 50
        },
        {
            'name': 'Move 4% + ROC + Liq',
            'confidence_min': 0.0,
            'movement_min': 4.0,
            'use_roc': True,
            'max_liquidity_risk': 50
        },

        # Quad combination (ALL FILTERS)
        {
            'name': '🏆 ALL FILTERS (Conf 72% + Move 4% + ROC + Liq)',
            'confidence_min': 0.72,
            'movement_min': 4.0,
            'use_roc': True,
            'max_liquidity_risk': 50
        },
    ]

    results = []

    for config in filter_configs:
        # Apply filters
        filtered = trades_df.copy()

        # Confidence filter
        if config['confidence_min'] > 0:
            filtered = filtered[filtered['mem_confidence'] >= config['confidence_min']]

        # Movement filter
        if config['movement_min'] > 0:
            filtered = filtered[filtered['abs_pnl_pct'] >= config['movement_min']]

        # ROC filter
        if config['use_roc']:
            filtered = filtered[filtered['roc_20'] > 0]

        # Liquidity risk filter
        if config['max_liquidity_risk'] < 100:
            filtered = filtered[filtered['avg_execution_risk'] < config['max_liquidity_risk']]

        # Calculate metrics
        if len(filtered) > 0:
            total_pnl = filtered['pnl'].sum()
            avg_pnl = filtered['pnl'].mean()
            win_rate = (filtered['is_win'] == 1).mean()
            final_equity = 10000 + total_pnl
            total_return = (final_equity - 10000) / 10000 * 100
            improvement = total_pnl - baseline_pnl
            improvement_pct = (improvement / abs(baseline_pnl)) * 100
            sharpe_estimate = (avg_pnl / filtered['pnl'].std()) * np.sqrt(len(filtered)) if len(filtered) > 1 else 0

            # Calculate profit factor
            wins = filtered[filtered['pnl'] > 0]['pnl'].sum()
            losses = abs(filtered[filtered['pnl'] < 0]['pnl'].sum())
            profit_factor = wins / losses if losses > 0 else float('inf')
        else:
            total_pnl = 0
            avg_pnl = 0
            win_rate = 0
            final_equity = 10000
            total_return = 0
            improvement = -baseline_pnl
            improvement_pct = 100
            sharpe_estimate = 0
            profit_factor = 0

        results.append({
            'config': config['name'],
            'trades': len(filtered),
            'trades_pct': len(filtered) / baseline_trades * 100,
            'total_pnl': total_pnl,
            'avg_pnl': avg_pnl,
            'win_rate': win_rate,
            'total_return': total_return,
            'improvement': improvement,
            'improvement_pct': improvement_pct,
            'sharpe': sharpe_estimate,
            'profit_factor': profit_factor
        })

    # Sort by improvement
    results_sorted = sorted(results, key=lambda x: x['improvement'], reverse=True)

    # Print results
    print(f"\n{'='*120}")
    print(f"📊 FILTER PERFORMANCE COMPARISON (Ranked by Improvement)")
    print(f"{'='*120}\n")
    print(f"{'Configuration':<45} {'Trades':<10} {'Total PnL':<12} {'Avg PnL':<10} {'Win Rate':<10} {'Return':<10} {'Improve':<12} {'Sharpe':<8}")
    print(f"{'-'*120}")

    for i, r in enumerate(results_sorted, 1):
        symbol = "🏆" if i == 1 else f"{i:2}."
        improvement_symbol = "📈" if r['improvement'] > 0 else "📉"

        print(f"{symbol} {r['config']:<42} {r['trades']:>3} ({r['trades_pct']:>4.0f}%)  "
              f"${r['total_pnl']:>9.2f}  ${r['avg_pnl']:>7.2f}  {r['win_rate']:>8.1%}  "
              f"{r['total_return']:>8.1f}%  {improvement_symbol}${r['improvement']:>9.2f}  {r['sharpe']:>6.2f}")

    # Detailed analysis of top 5
    print(f"\n{'='*120}")
    print(f"🔍 TOP 5 DETAILED ANALYSIS")
    print(f"{'='*120}\n")

    for i, r in enumerate(results_sorted[:5], 1):
        print(f"{i}. {r['config']}")
        print(f"   Trades: {r['trades']} ({r['trades_pct']:.0f}% of baseline)")
        print(f"   Total PnL: ${r['total_pnl']:.2f} (vs ${baseline_pnl:.2f} baseline)")
        print(f"   Avg PnL: ${r['avg_pnl']:.2f} per trade")
        print(f"   Win Rate: {r['win_rate']:.1%} (vs {baseline_wr:.1%} baseline)")
        print(f"   Total Return: {r['total_return']:+.2f}%")
        print(f"   Improvement: ${r['improvement']:.2f} ({r['improvement_pct']:+.0f}%)")
        print(f"   Profit Factor: {r['profit_factor']:.2f}")
        print(f"   Sharpe Ratio: {r['sharpe']:.2f}")
        print()

    # Save results
    results_df = pd.DataFrame(results_sorted)
    results_df.to_csv('comprehensive_filter_results.csv', index=False)
    print(f"💾 Results saved to comprehensive_filter_results.csv\n")

    # Recommendations
    print(f"{'='*120}")
    print(f"💡 RECOMMENDATIONS")
    print(f"{'='*120}\n")

    best = results_sorted[0]
    best_practical = [r for r in results_sorted if r['trades'] >= 10][0] if any(r['trades'] >= 10 for r in results_sorted) else best

    print(f"🏆 BEST OVERALL (Maximum Improvement):")
    print(f"   {best['config']}")
    print(f"   Improvement: ${best['improvement']:.2f} ({best['improvement_pct']:+.0f}%)")
    print(f"   Trades: {best['trades']}")
    print(f"   Win Rate: {best['win_rate']:.1%}\n")

    if best != best_practical:
        print(f"⚖️  BEST PRACTICAL (Min 10 trades):")
        print(f"   {best_practical['config']}")
        print(f"   Improvement: ${best_practical['improvement']:.2f} ({best_practical['improvement_pct']:+.0f}%)")
        print(f"   Trades: {best_practical['trades']}")
        print(f"   Win Rate: {best_practical['win_rate']:.1%}\n")

    # Filter impact breakdown
    print(f"📊 INDIVIDUAL FILTER IMPACT:\n")

    single_filters = [r for r in results_sorted if r['config'] in [
        'Confidence >= 72%', 'Movement >= 4%', 'Movement >= 5%',
        'ROC(20) > 0', 'Liquidity Risk < 50'
    ]]

    for r in sorted(single_filters, key=lambda x: x['improvement'], reverse=True):
        print(f"   {r['config']:<30} ${r['improvement']:>10.2f}  ({r['improvement_pct']:>5.0f}%)  "
              f"{r['trades']:>3} trades  {r['win_rate']:>6.1%} WR")

    print(f"\n{'='*120}")
    print(f"✅ ANALYSIS COMPLETE")
    print(f"{'='*120}")


if __name__ == '__main__':
    test_all_filter_combinations()
