"""
Quick Backtest - Fast Strategy Validation
=========================================
Optimized backtesting for rapid testing
"""

import pandas as pd
import numpy as np
import yfinance as yf
from datetime import datetime, timedelta
import logging

# Suppress verbose logging
logging.basicConfig(level=logging.WARNING)

from advanced_trading_strategy import AdvancedTradingStrategy

def quick_backtest(symbol='BTC-USD', days=90, min_confidence=50.0):
    """Run a quick backtest"""
    print(f"\n{'='*80}")
    print(f"QUICK BACKTEST: {symbol} (Last {days} days)")
    print(f"{'='*80}")

    # Fetch recent data
    ticker = yf.Ticker(symbol)
    end_date = datetime.now()
    start_date = end_date - timedelta(days=days)

    data = ticker.history(interval='1h', start=start_date, end=end_date)
    data.columns = [c.lower() for c in data.columns]

    print(f"Loaded {len(data)} hourly candles")

    # Initialize
    strategy = AdvancedTradingStrategy(
        min_confidence=min_confidence,
        max_risk_per_trade=0.02,
        use_multi_timeframe=False
    )

    initial_capital = 10000.0
    equity = initial_capital
    trades = []
    position = None

    # Backtest
    print("Running backtest...")
    for i in range(100, len(data)-1):
        if i % 100 == 0:
            print(f"  Progress: {i}/{len(data)}")

        data_window = data.iloc[:i+1]
        current_price = data['close'].iloc[i]

        # Generate signal (suppress logs)
        signal = strategy.generate_trade_signal(data_1h=data_window, account_balance=equity)

        # Handle position
        if position:
            next_high = data['high'].iloc[i+1]
            next_low = data['low'].iloc[i+1]

            exit_price = None
            exit_reason = None

            if position['side'] == 'BUY':
                if next_low <= position['sl']:
                    exit_price = position['sl']
                    exit_reason = 'SL'
                elif next_high >= position['tp']:
                    exit_price = position['tp']
                    exit_reason = 'TP'

            if exit_price:
                pnl = (exit_price - position['entry']) * position['size']
                pnl -= (position['entry'] * position['size'] * 0.001 +
                       exit_price * position['size'] * 0.001)

                equity += pnl

                trades.append({
                    'entry': position['entry'],
                    'exit': exit_price,
                    'pnl': pnl,
                    'reason': exit_reason
                })

                position = None

        # Open position
        if not position and signal['action'] in ['BUY', 'SELL']:
            position = {
                'side': signal['action'],
                'entry': current_price,
                'sl': signal['stop_loss'],
                'tp': signal['take_profit'],
                'size': signal['position_size']
            }

    # Results
    print(f"\n{'='*80}")
    print("RESULTS")
    print(f"{'='*80}")
    print(f"Initial Capital: ${initial_capital:,.2f}")
    print(f"Final Equity: ${equity:,.2f}")
    print(f"Total Return: {((equity-initial_capital)/initial_capital*100):.2f}%")
    print(f"Total Trades: {len(trades)}")

    if trades:
        wins = [t for t in trades if t['pnl'] > 0]
        losses = [t for t in trades if t['pnl'] <= 0]

        print(f"Winning Trades: {len(wins)}")
        print(f"Losing Trades: {len(losses)}")
        print(f"Win Rate: {(len(wins)/len(trades)*100):.1f}%")
        print(f"Avg Win: ${np.mean([t['pnl'] for t in wins]):.2f}" if wins else "Avg Win: N/A")
        print(f"Avg Loss: ${np.mean([t['pnl'] for t in losses]):.2f}" if losses else "Avg Loss: N/A")

        print(f"\nBest Trade: ${max(t['pnl'] for t in trades):.2f}")
        print(f"Worst Trade: ${min(t['pnl'] for t in trades):.2f}")

        # Exit reasons
        sl_count = len([t for t in trades if t['reason'] == 'SL'])
        tp_count = len([t for t in trades if t['reason'] == 'TP'])
        print(f"\nStop Loss Hits: {sl_count} ({sl_count/len(trades)*100:.1f}%)")
        print(f"Take Profit Hits: {tp_count} ({tp_count/len(trades)*100:.1f}%)")

    print(f"{'='*80}\n")

    return {
        'trades': len(trades),
        'return_pct': ((equity-initial_capital)/initial_capital*100),
        'win_rate': (len(wins)/len(trades)*100) if trades else 0
    }

if __name__ == "__main__":
    # Test multiple symbols
    symbols = ['BTC-USD', 'ETH-USD', 'AAPL']

    for symbol in symbols:
        try:
            quick_backtest(symbol, days=90, min_confidence=50.0)
        except Exception as e:
            print(f"Error with {symbol}: {e}")
