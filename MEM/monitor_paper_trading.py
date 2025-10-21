#!/usr/bin/env python3
"""
Paper Trading Monitor
Check status and performance of paper trading
"""

import json
import pandas as pd
from pathlib import Path
from datetime import datetime
from typing import Dict


def load_state() -> Dict:
    """Load current trading state"""
    state_file = '/root/AlgoTrendy_v2.6/MEM/paper_trading_state.json'
    if Path(state_file).exists():
        with open(state_file, 'r') as f:
            return json.load(f)
    return {}


def load_trades() -> pd.DataFrame:
    """Load executed trades"""
    trades_file = '/root/AlgoTrendy_v2.6/MEM/paper_trades.csv'
    if Path(trades_file).exists():
        return pd.read_csv(trades_file)
    return pd.DataFrame()


def print_dashboard():
    """Print trading dashboard"""
    state = load_state()
    trades_df = load_trades()

    print("="*80)
    print("📊 PAPER TRADING DASHBOARD")
    print("="*80)
    print()

    if not state:
        print("⚠️  No active paper trading session found")
        print("   Start paper trading with: python3 paper_trading_engine.py")
        return

    # Status
    print("🔄 Status:")
    last_updated = state.get('last_updated', 'Unknown')
    print(f"   Last Updated: {last_updated}")
    print(f"   Current Index: {state.get('current_index', 0)}")
    print()

    # Capital
    initial_capital = 10000.0  # Default
    current_capital = state.get('current_capital', initial_capital)
    total_return = ((current_capital - initial_capital) / initial_capital) * 100

    print("💰 Capital:")
    print(f"   Initial: ${initial_capital:,.2f}")
    print(f"   Current: ${current_capital:,.2f}")
    print(f"   P&L: ${current_capital - initial_capital:+,.2f}")
    print(f"   Return: {total_return:+.2f}%")
    print()

    # Trades
    if len(trades_df) > 0:
        wins = len(trades_df[trades_df['pnl'] > 0])
        losses = len(trades_df[trades_df['pnl'] < 0])
        win_rate = (wins / (wins + losses)) * 100 if (wins + losses) > 0 else 0

        print(f"📈 Trading Performance:")
        print(f"   Trades Executed: {len(trades_df)}")
        print(f"   Wins: {wins}")
        print(f"   Losses: {losses}")
        print(f"   Win Rate: {win_rate:.1f}%")
        print(f"   Total PnL: ${trades_df['pnl'].sum():,.2f}")
        print(f"   Avg PnL/Trade: ${trades_df['pnl'].mean():,.2f}")
        print()

        # Recent trades
        print("📋 Recent Trades (Last 5):")
        recent = trades_df.tail(5)[['trade_number', 'action', 'confidence', 'pnl', 'pnl_pct', 'exit_reason']]
        for _, trade in recent.iterrows():
            symbol = "✅" if trade['pnl'] > 0 else "❌"
            print(f"   {symbol} Trade #{int(trade['trade_number'])}: {trade['action']:<4} "
                  f"Conf:{trade['confidence']:.1%} "
                  f"PnL:${trade['pnl']:+7.2f} ({trade['pnl_pct']:+.1f}%) "
                  f"[{trade['exit_reason']}]")
        print()

        # Statistics
        if len(trades_df) >= 3:
            print("📊 Statistics:")

            # Best and worst
            best_trade = trades_df.loc[trades_df['pnl'].idxmax()]
            worst_trade = trades_df.loc[trades_df['pnl'].idxmin()]

            print(f"   Best Trade: ${best_trade['pnl']:.2f} (Trade #{int(best_trade['trade_number'])})")
            print(f"   Worst Trade: ${worst_trade['pnl']:.2f} (Trade #{int(worst_trade['trade_number'])})")

            # Sharpe ratio estimate
            returns = trades_df['pnl_pct']
            if len(returns) > 1 and returns.std() > 0:
                import numpy as np
                sharpe = (returns.mean() / returns.std()) * np.sqrt(len(returns))
                print(f"   Sharpe Ratio: {sharpe:.2f}")

            print()
    else:
        print("📈 No trades executed yet")
        print()

    print("="*80)
    print("Commands:")
    print("  python3 monitor_paper_trading.py     - Show this dashboard")
    print("  python3 paper_trading_engine.py      - Run paper trading")
    print("  tail -f paper_trading.log            - Watch live logs")
    print("="*80)


def print_trade_history():
    """Print detailed trade history"""
    trades_df = load_trades()

    if len(trades_df) == 0:
        print("No trades executed yet")
        return

    print("\n" + "="*120)
    print("📊 COMPLETE TRADE HISTORY")
    print("="*120)

    for _, trade in trades_df.iterrows():
        symbol = "✅" if trade['pnl'] > 0 else "❌"
        print(f"\n{symbol} Trade #{int(trade['trade_number'])} - {trade['action']}")
        print(f"   Confidence: {trade['confidence']:.1%}")
        print(f"   Entry: ${trade['entry_price']:,.2f} | Exit: ${trade['exit_price']:,.2f}")
        print(f"   PnL: ${trade['pnl']:+.2f} ({trade['pnl_pct']:+.2f}%)")
        print(f"   Exit Reason: {trade['exit_reason']}")
        print(f"   Capital After: ${trade['capital_after']:,.2f} (Return: {trade['return_pct']:+.2f}%)")

    print("\n" + "="*120)


if __name__ == '__main__':
    import sys

    if len(sys.argv) > 1 and sys.argv[1] == 'history':
        print_trade_history()
    else:
        print_dashboard()
