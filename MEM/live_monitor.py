#!/usr/bin/env python3
"""
Live Paper Trading Monitor
Real-time dashboard for active trades and performance
"""

import pandas as pd
import json
from datetime import datetime
from typing import Dict
import sys


def load_state() -> Dict:
    """Load current trading state"""
    try:
        with open('live_paper_state.json', 'r') as f:
            return json.load(f)
    except FileNotFoundError:
        return {}


def load_trades() -> pd.DataFrame:
    """Load trade history"""
    try:
        return pd.read_csv('live_paper_trades.csv')
    except FileNotFoundError:
        return pd.DataFrame()


def print_dashboard():
    """Print live trading dashboard"""
    state = load_state()
    trades_df = load_trades()

    print("\n" + "="*80)
    print("🚀 LIVE PAPER TRADING DASHBOARD")
    print("="*80)

    # Capital status
    capital = state.get('capital', 10000)
    initial_capital = 10000  # Fixed for now
    total_return = ((capital - initial_capital) / initial_capital) * 100

    print(f"\n💰 CAPITAL:")
    print(f"   Current: ${capital:,.2f}")
    print(f"   Initial: ${initial_capital:,.2f}")
    print(f"   Total Return: {total_return:+.2f}%")

    # Active positions
    positions = state.get('positions', {})
    print(f"\n📊 ACTIVE POSITIONS: {len(positions)}")
    if positions:
        for symbol, pos in positions.items():
            entry_time = datetime.fromisoformat(pos['entry_time'])
            duration = datetime.now() - entry_time
            print(f"\n   {symbol} ({pos['side']}):")
            print(f"      Entry: ${pos['entry_price']:,.2f}")
            print(f"      Target: ${pos['target_price']:,.2f}")
            print(f"      Margin: ${pos['margin']:.2f} (25x)")
            print(f"      Confidence: {pos['confidence']:.1%}")
            print(f"      Duration: {duration.seconds // 3600}h {(duration.seconds % 3600) // 60}m")
    else:
        print("   No active positions")

    # Trade statistics
    if not trades_df.empty:
        total_trades = len(trades_df)
        wins = len(trades_df[trades_df['pnl_usd'] > 0])
        losses = len(trades_df[trades_df['pnl_usd'] < 0])
        win_rate = (wins / total_trades) * 100
        avg_pnl = trades_df['pnl_usd'].mean()

        print(f"\n📈 TRADING STATS:")
        print(f"   Total Trades: {total_trades}")
        print(f"   Wins: {wins} ({win_rate:.1f}%)")
        print(f"   Losses: {losses} ({(losses/total_trades)*100:.1f}%)")
        print(f"   Avg PnL: ${avg_pnl:+.2f}")

        # Recent trades
        print(f"\n📋 RECENT TRADES (Last 5):")
        recent = trades_df.tail(5)
        for idx, trade in recent.iterrows():
            status = "✅" if trade['pnl_usd'] > 0 else "❌"
            if trade.get('liquidated', False):
                status = "💀"
            print(f"   {status} {trade['symbol']} {trade['side']}: ${trade['pnl_usd']:+.2f} ({trade['roi_on_margin']:+.1f}%)")

        # Best/Worst
        best = trades_df.loc[trades_df['pnl_usd'].idxmax()]
        worst = trades_df.loc[trades_df['pnl_usd'].idxmin()]

        print(f"\n🎯 BEST/WORST:")
        print(f"   Best: {best['symbol']} ${best['pnl_usd']:+.2f}")
        print(f"   Worst: {worst['symbol']} ${worst['pnl_usd']:+.2f}")

    # Last update
    last_update = state.get('last_update', 'Never')
    if last_update != 'Never':
        last_update = datetime.fromisoformat(last_update).strftime('%Y-%m-%d %H:%M:%S')

    print(f"\n⏰ Last Update: {last_update}")
    print("="*80)


def print_symbols_performance():
    """Print performance by symbol"""
    trades_df = load_trades()

    if trades_df.empty:
        print("No trades yet")
        return

    print("\n" + "="*80)
    print("📊 PERFORMANCE BY SYMBOL")
    print("="*80)

    by_symbol = trades_df.groupby('symbol').agg({
        'pnl_usd': ['count', 'sum', 'mean'],
        'roi_on_margin': 'mean'
    }).round(2)

    by_symbol.columns = ['Trades', 'Total PnL', 'Avg PnL', 'Avg ROI']
    by_symbol = by_symbol.sort_values('Total PnL', ascending=False)

    print(by_symbol.to_string())
    print("="*80)


def main():
    """Main monitor function"""
    if len(sys.argv) > 1:
        if sys.argv[1] == 'symbols':
            print_symbols_performance()
        elif sys.argv[1] == 'history':
            trades_df = load_trades()
            if not trades_df.empty:
                print(trades_df.to_string())
            else:
                print("No trades yet")
        else:
            print("Usage: python3 live_monitor.py [symbols|history]")
    else:
        print_dashboard()


if __name__ == '__main__':
    main()
